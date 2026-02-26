using ErrorOr;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Integration.AiProvider;
using Mediator;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JobScraper.Web.Features.AiSummary;

#pragma warning disable SKEXP0110

public class AdjustCvContentToOffer
{
    public record Request(
        string CvContent,
        string OfferContent,
        string OfferSummary,
        string UserRequirements,
        string ProviderName
    ) : IRequest<ErrorOr<Response>>;

    public record Response(string? AdjustedCvContent, List<ChatItem> ChatHistory);

    internal class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    ) : IRequestHandler<Request, ErrorOr<Response>>
    {
        private const string DoneSignal = "[DONE]";
        private const string FailSignal = "[FAIL]";
        private const int MaxRetries = 5;

        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var offerMessage = new ChatMessageContent(AuthorRole.User, $"offerContent: {request.OfferContent}");
            var summaryMessage = new ChatMessageContent(AuthorRole.User, $"offerSummary: {request.OfferSummary}");

            string? finalContent = null;
            var retryCount = 0;
            var chatHistory = new List<ChatItem>();
            do
            {
                var chat = PrepareAgentsChat(request);
                chat.AddChatMessages([offerMessage, summaryMessage]);

                var asyncResponse = chat.InvokeAsync(cancellationToken);

                await foreach (var response in asyncResponse)
                {
                    chatHistory.Add(ChatItem.From(response));
                    finalContent = response.Content;
                }
            } while (ShouldRetry(finalContent, retryCount++));

            var adjustedCv = finalContent?.Replace(DoneSignal, "").Trim();
            return new Response(adjustedCv, chatHistory);
        }

        private static bool ShouldRetry(string? lastContent, int retryCount)
        {
            if (retryCount > MaxRetries) // accept content after max retries
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
            var kernel = serviceProvider.GetRequiredKeyedService<Kernel>(request.ProviderName);

            var analyzerAgent = new ChatCompletionAgent
            {
                Name = "CvAnalyzer",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are a professional CV analyst. Your goal is to analyze the job offer and the user's CV to find the best alignment.
                     Before any CV editing happens, you must provide a detailed analysis.

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

                     OfferSummary will be provided in first analyst prompt.

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

                     If you are generating final CV, finish it with {DoneSignal}, that ends the conversation.

                     Your task is to rewrite the user's CV (provided in markdown).

                     Rules:
                     - DO NOT invent experiences or skills that are not in the original CV, unless user explicitly asks for it.
                     - DO NOT remove core contact information, unless user explicitly asks for it.
                     - DO optimize the 'Summary', 'Experience', or 'Skills' sections to highlight relevant matches based on the analysis.
                     - DO use terminology from the job offer where appropriate to increase alignment.
                     - MAINTAIN the original markdown structure.
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
                        MaximumIterations = 6,
                    },
                    SelectionStrategy = new SequentialSelectionStrategy(),
                },
                LoggerFactory = loggerFactory,
            };
            return chat;
        }
    }
}
