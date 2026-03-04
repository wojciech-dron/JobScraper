using ErrorOr;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Integration.AiProvider;
using Mediator;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JobScraper.Web.Features.AiSummary.Logic;

#pragma warning disable SKEXP0110

public class AdjustCvContentToOffer
{
    public record Request(
        string CvContent,
        string OfferContent,
        string? OfferSummary,
        string UserRequirements = "",
        string ProviderName = AiProvidersConfig.MainProvider
    ) : IRequest<Response>;

    public record Response(
        bool Success,
        string AdjustedCvContent,
        List<ChatItem> ChatHistory
        );

    public class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    ) : IRequestHandler<Request, Response>
    {
        private const string DoneSignal = "[DONE]";
        private const string FailSignal = "[FAIL]";
        private const int MaxRetries = 2;

        private readonly ILogger<Handler> _logger = loggerFactory.CreateLogger<Handler>();

        public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Adjusting CV content to offer");

            var offerMessage = new ChatMessageContent(AuthorRole.User, $"offerContent: {request.OfferContent}");

            string? finalContent = null;
            var retryCount = 0;
            var chatHistory = new List<ChatItem>();
            do
            {
                var chat = PrepareAgentsChat(request);
                chat.AddChatMessage(offerMessage);

                if (request.OfferSummary is not null)
                {
                    var summaryMessage = new ChatMessageContent(AuthorRole.User, $"offerSummary: {request.OfferSummary}");
                    chat.AddChatMessage(summaryMessage);
                }

                var asyncResponse = chat.InvokeAsync(cancellationToken);

                await foreach (var response in asyncResponse)
                {
                    chatHistory.Add(ChatItem.From(response));
                    finalContent = response.Content;
                }
            } while (ShouldRetry(finalContent, retryCount++));

            if (finalContent is null || !finalContent.Contains(DoneSignal))
                return new Response(false, "", chatHistory);

            var adjustedCv = finalContent.Replace(DoneSignal, "").Trim();

            return new Response(true, adjustedCv, chatHistory);
        }

        private static bool ShouldRetry(string? lastContent, int retryCount)
        {
            if (retryCount >= MaxRetries) // accept content after max retries
                return false;

            if (string.IsNullOrEmpty(lastContent))
                return true;

            if (lastContent.Contains(FailSignal, StringComparison.OrdinalIgnoreCase))
                return true;

            if (lastContent.Length < DoneSignal.Length + 50) // CV should be significantly longer than just the signal
                return true;

            return false;
        }

        private AgentGroupChat PrepareAgentsChat(Request request)
        {
            var kernel = serviceProvider.GetAiKernel(request.ProviderName);

            var analyzerAgent = new ChatCompletionAgent
            {
                Name = "CvAnalyzer",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are a professional CV analyst. Your goal is to analyze the job offer and the user's CV to find the best alignment.
                     Before any CV editing happens, you must provide a detailed analysis.
                     User prompts only first messages, do not ask for any more information.
                     If CvEditor returns message only with {DoneSignal}, instruct it to return final CV content with that signal, signal only.

                     Your task:
                     - Analyze the 'Job offer content' and 'Requirements for an offer' (if provided).
                     - Compare them with the 'Original CV content'.
                     - Analyze CV for ATS compliance.
                     - Identify key skills, experiences, and keywords from the offer that are present (or should be highlighted) in the CV.
                     - List specific suggestions for the CV editor on how to optimize each section.
                     - DO NOT rewrite the CV yourself. Just provide the analysis and suggestions.
                     - End your response with a clear instruction for the CvEditor to proceed.

                     Requirements for an offer defined by user (optional):
                     {request.UserRequirements}

                     The original CV content:
                     {request.CvContent}

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
                     {request.UserRequirements}

                     The original CV content:
                     {request.CvContent}

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
}
