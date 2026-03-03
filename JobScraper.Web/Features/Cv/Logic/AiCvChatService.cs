using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.AiSummary.Logic;
using JobScraper.Web.Integration.AiProvider;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace JobScraper.Web.Features.Cv.Logic;

#pragma warning disable SKEXP0110

public interface IAiCvChatService
{
    /// <summary>
    /// Starts a new AI conversation to adjust CV content for a specific offer.
    /// Returns the initial response with adjusted CV content.
    /// </summary>
    Task<AiCvChatResult> AdjustCvAsync(AiCvChatRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends a follow-up message in an existing conversation.
    /// The user can ask questions or request modifications to the CV.
    /// </summary>
    Task<AiCvChatResult> SendMessageAsync(AiCvChatFollowUpRequest request, CancellationToken ct = default);
}

public record AiCvChatRequest(
    string CvContent,
    string OfferContent,
    string? OfferSummary,
    string UserRequirements = "",
    string ProviderName = AiProvidersConfig.MainProvider
);

public record AiCvChatFollowUpRequest(
    string UserMessage,
    string CurrentCvContent,
    string OfferContent,
    string? OfferSummary,
    List<ChatItem> ExistingChatHistory,
    string UserRequirements = "",
    string ProviderName = AiProvidersConfig.MainProvider
);

public record AiCvChatResult(
    bool Success,
    string? AdjustedCvContent,
    List<ChatItem> ChatHistory
);

public class AiCvChatService(
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory
) : IAiCvChatService
{
    private const string DoneSignal = "[DONE]";
    private const string FailSignal = "[FAIL]";
    private const int MaxRetries = 2;

    private readonly ILogger<AiCvChatService> _logger = loggerFactory.CreateLogger<AiCvChatService>();

    public async Task<AiCvChatResult> AdjustCvAsync(AiCvChatRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting AI CV adjustment conversation");

        var offerMessage = new ChatMessageContent(AuthorRole.User, $"offerContent: {request.OfferContent}");

        string? finalContent = null;
        var retryCount = 0;
        var chatHistory = new List<ChatItem>();
        do
        {
            var chat = PrepareAgentsChat(request.CvContent, request.UserRequirements, request.ProviderName);
            chat.AddChatMessage(offerMessage);

            if (request.OfferSummary is not null)
            {
                var summaryMessage = new ChatMessageContent(AuthorRole.User, $"offerSummary: {request.OfferSummary}");
                chat.AddChatMessage(summaryMessage);
            }

            await foreach (var response in chat.InvokeAsync(ct))
            {
                chatHistory.Add(ChatItem.From(response));
                finalContent = response.Content;
            }
        } while (ShouldRetry(finalContent, retryCount++));

        if (finalContent is null || !finalContent.Contains(DoneSignal))
            return new AiCvChatResult(false, null, chatHistory);

        var adjustedCv = finalContent.Replace(DoneSignal, "").Trim();

        // Remove the last item because it contains the raw CV content
        if (chatHistory.Count > 0)
            chatHistory.RemoveAt(chatHistory.Count - 1);

        return new AiCvChatResult(true, adjustedCv, chatHistory);
    }

    public async Task<AiCvChatResult> SendMessageAsync(AiCvChatFollowUpRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Sending follow-up message in AI CV chat");

        var kernel = serviceProvider.GetAiKernel(request.ProviderName);

        var agent = new ChatCompletionAgent
        {
            Name = "CvAssistant",
            Kernel = kernel,
            Instructions =
                $"""
                 You are a professional CV consultant. You help users modify and improve their CV content
                 based on a specific job offer. You can suggest changes, answer questions about CV optimization,
                 and when asked to modify the CV, return the complete updated CV in markdown format.

                 When you modify the CV content, include the COMPLETE updated CV in your response
                 wrapped between [CV_START] and [CV_END] markers.

                 Current CV content:
                 {request.CurrentCvContent}

                 Job offer content:
                 {request.OfferContent}

                 {(request.OfferSummary is not null ? $"Offer summary:\n{request.OfferSummary}" : "")}
                 {(request.UserRequirements != "" ? $"User requirements:\n{request.UserRequirements}" : "")}
                 """,
            LoggerFactory = loggerFactory,
        };

        var chatHistory = new ChatHistory();

        // Add existing chat history as context
        foreach (var item in request.ExistingChatHistory)
        {
            if (item.Role == AuthorRole.User.Label)
                chatHistory.AddUserMessage(item.Content ?? "");
            else
                chatHistory.AddAssistantMessage(item.Content ?? "");
        }

        chatHistory.AddUserMessage(request.UserMessage);

        var chatItems = new List<ChatItem>(request.ExistingChatHistory)
        {
            new(AuthorRole.User.Label, "User", request.UserMessage)
        };

        try
        {
            await foreach (var response in agent.InvokeAsync(chatHistory, cancellationToken: ct))
            {
                var responseContent = response.Message.Content ?? "";

                chatItems.Add(new ChatItem(AuthorRole.Assistant.Label, "CvAssistant", responseContent));

                // Check if response contains CV modification
                string? adjustedCv = null;
                if (responseContent.Contains("[CV_START]") && responseContent.Contains("[CV_END]"))
                {
                    var startIdx = responseContent.IndexOf("[CV_START]", StringComparison.Ordinal) + "[CV_START]".Length;
                    var endIdx = responseContent.IndexOf("[CV_END]", StringComparison.Ordinal);
                    if (endIdx > startIdx)
                        adjustedCv = responseContent[startIdx..endIdx].Trim();
                }

                return new AiCvChatResult(true, adjustedCv, chatItems);
            }

            return new AiCvChatResult(true, null, chatItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get AI response in follow-up chat");
            chatItems.Add(new ChatItem(AuthorRole.Assistant.Label, "System", $"Error: {ex.Message}"));
            return new AiCvChatResult(false, null, chatItems);
        }
    }

    private static bool ShouldRetry(string? lastContent, int retryCount)
    {
        if (retryCount >= MaxRetries)
            return false;

        if (string.IsNullOrEmpty(lastContent))
            return true;

        if (lastContent.Contains(FailSignal, StringComparison.OrdinalIgnoreCase))
            return true;

        if (lastContent.Length < DoneSignal.Length + 50)
            return true;

        return false;
    }

    private AgentGroupChat PrepareAgentsChat(string cvContent, string userRequirements, string providerName)
    {
        var kernel = serviceProvider.GetAiKernel(providerName);

        var analyzerAgent = new ChatCompletionAgent
        {
            Name = "CvAnalyzer",
            Kernel = kernel,
            Instructions =
                $"""
                 You are a professional CV analyst. Your goal is to analyze the job offer and the user's CV to find the best alignment.
                 Before any CV editing happens, you must provide a detailed analysis.
                 User prompts only first messages, do not ask for any more information.

                 Your task:
                 - Analyze the 'Job offer content' and 'Requirements for an offer' (if provided).
                 - Compare them with the 'Original CV content'.
                 - Analyze CV for ATS compliance.
                 - Identify key skills, experiences, and keywords from the offer that are present (or should be highlighted) in the CV.
                 - List specific suggestions for the CV editor on how to optimize each section.
                 - DO NOT rewrite the CV yourself. Just provide the analysis and suggestions.
                 - End your response with a clear instruction for the CvEditor to proceed.

                 Requirements for an offer defined by user (optional):
                 {userRequirements}

                 The original CV content:
                 {cvContent}

                 OfferSummary will be provided in first analyst prompt and is optional.

                 Job offer content will be provided in first user prompt.
                 """,
            LoggerFactory = loggerFactory,
        };

        var cvEditorAgent = new ChatCompletionAgent
        {
            Name = "CvEditor",
            Kernel = kernel,
            Instructions =
                $"""
                 You are a professional CV editor specializing in tailoring CVs to job offers.
                 You MUST wait for the CvAnalyzer to provide an analysis before you start your work.
                 Use the analysis provided by CvAnalyzer to guide your rewriting.
                 User prompts only first messages, do not ask for any more information.
                 Final response should be in markdown format, containing only the final CV content
                 with appended {DoneSignal}, that ends the conversation.

                 Your task is to rewrite the user's CV (provided in markdown).

                 Rules:
                 - DO NOT invent experiences or skills that are not in the original CV.
                 - DO NOT remove core contact information
                 - DO optimize the 'Summary', 'Experience', or 'Skills' sections to include relevant matches based on the analysis.
                 - DO use terminology from the job offer where appropriate to increase alignment.
                 - MAINTAIN the original markdown structure, keep line length about 90 characters.
                 - In this section, provide concise bullet-points about how the CV was adjusted and what further improvements can be made.
                 - RETURN the COMPLETE adjusted CV in markdown format followed by the summary section.
                 - FINISH your response with {DoneSignal}, which ends the conversation.

                 If something is wrong or required data is missing, return reason and {FailSignal}, which terminates the conversation.

                 Requirements for an offer defined by user (optional):
                 {userRequirements}

                 The original CV content:
                 {cvContent}

                 Job offer content will be provided in first user prompt.
                 """,
            LoggerFactory = loggerFactory,
        };

        var chat = new AgentGroupChat(analyzerAgent, cvEditorAgent)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                TerminationStrategy = new ApprovalTerminationStrategy
                {
                    Agents = [cvEditorAgent],
                    DoneSignal = DoneSignal,
                    FailSignal = FailSignal,
                    MaximumIterations = 2,
                },
                SelectionStrategy = new SequentialSelectionStrategy(),
            },
            LoggerFactory = loggerFactory,
        };
        return chat;
    }
}
