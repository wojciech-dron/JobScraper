using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.AiSummary.Logic;
using JobScraper.Web.Features.Cv.Helpers;
using JobScraper.Web.Integration.AiProvider;
using Mediator;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JobScraper.Web.Features.Cv.Logic;

#pragma warning disable SKEXP0110

public class AdjustCvForOffer
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
        string? AdjustedCvContent,
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

        public async ValueTask<Response> Handle(Request request, CancellationToken ct = default)
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
                return new Response(false, null, chatHistory);

            var adjustedCv = finalContent
                .Replace(DoneSignal, "")
                .RemoveAiChars();

            // Remove the last item because it contains the raw CV content
            if (chatHistory.Count > 0)
                chatHistory.RemoveAt(chatHistory.Count - 1);

            return new Response(true, adjustedCv, chatHistory);
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
                     Before any CV editing happens, you must provide short analysis.
                     User prompts only first messages, do not ask for any more information.

                     Your task:
                     - Be concise and to the point.
                     - Analyze CV for ATS compliance.
                     - Focus only on the developer title and 'Summary', 'Experience', and 'Skills' sections.
                     - Infer missing skills and experience on if they are required in offer and the evidence in the CV strongly suggests they exist.
                     - Remove unnecessary skills and experiences if they are NOT RELEVANT to the job offer.
                     - Return the analysis in a simple format, with clear headings and bullet points.
                     - List specific suggestions for the CV editor on how to optimize each section.
                     - DO NOT rewrite the CV yourself. Just provide the analysis and suggestions.
                     - End your response with a clear instruction for the CvEditor to proceed.

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
                     QuestPDF is used for generating pdf for markdown, keep that in mind when you generate markdown.
                     Use the analysis provided by CvAnalyzer to guide your rewriting.
                     Make a subtle but significant change to the CV content, do not rewrite the whole thing.
                     User prompts only first messages, do not ask for any more information.

                     Your workflow:
                     - Start with the original CV content provided below.
                     - Make conversation with AiAnalyzer to get a tailored CV for offer.
                     - Return the COMPLETE final CV in markdown format, with {DoneSignal}, which ends the conversation.

                     Rules:
                     - DO NOT invent experiences or skills that are not in the original CV.
                     - DO NOT remove core contact information.
                     - DO optimize the 'Summary', 'Experience', or 'Skills' sections to include relevant matches based on the analysis.
                     - DO use terminology from the job offer where appropriate to increase alignment.
                     - Remove unnecessary skills and experiences if they are NOT RELEVANT to the job offer.
                     - MAINTAIN the original markdown structure, keep line length about 90 characters.

                     If something is wrong or required data is missing, return reason and {FailSignal}, which terminates the conversation.

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
                        MaximumIterations = 10,
                    },
                    SelectionStrategy = new SequentialSelectionStrategy(),
                },
                LoggerFactory = loggerFactory,
            };
            return chat;
        }
    }
}
