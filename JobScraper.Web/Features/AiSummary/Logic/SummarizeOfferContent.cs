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

public partial class SummarizeOfferContent
{
    public record Request(
        string CvContent,
        string OfferContent,
        string UserRequirementsForOffer,
        string AiModel
    ) : IRequest<ErrorOr<Response>>;

    public record Response(string? AiSummary, List<ChatItem> ChatHistory);

    public partial class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    ) : IRequestHandler<Request, ErrorOr<Response>>
    {
        private readonly ILogger<Handler> _logger = loggerFactory.CreateLogger<Handler>();

        private const string DoneSignal = "[DONE]";
        private const string FailSignal = "[FAIL]";
        private const int MaxRetries = 5;

        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            LogSummarizingOfferContent(request.AiModel);

            var message = new ChatMessageContent(AuthorRole.User, $"offerContent: {request.OfferContent}");

            string? finalContent = null;
            var retryCount = 0;
            var chatHistory = new List<ChatItem>();
            do
            {
                var chat = PrepareAgentsChat(request);
                chat.AddChatMessage(message);

                var asyncResponse = chat.InvokeAsync(cancellationToken);

                await foreach (var response in asyncResponse)
                {
                    chatHistory.Add(ChatItem.From(response));
                    finalContent = response.Content;
                }
            } while (ShouldRetry(finalContent, retryCount++));

            if (finalContent is null || !finalContent.Contains(DoneSignal))
                return Error.Failure(description: "Failed to summarize offer content");

            var summary = finalContent.Replace(DoneSignal, "").Replace("---\n", "").Trim();

            return new Response(summary, chatHistory);
        }

        private static bool ShouldRetry(string? lastContent, int retryCount)
        {
            if (retryCount >= MaxRetries) // accept content after max retries
                return false;

            if (string.IsNullOrEmpty(lastContent))
                return true;

            if (lastContent.Contains(FailSignal, StringComparison.OrdinalIgnoreCase))
                return true;

            if (lastContent.Length < DoneSignal.Length + 20) // sometimes the model returns only the done signal
                return true;

            return false;
        }

        private AgentGroupChat PrepareAgentsChat(Request request)
        {
            var kernel = serviceProvider.GetAiKernel(request.AiModel);

            var analystAgent = new ChatCompletionAgent
            {
                Name = "Analyst",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are a professional job offer analyst.
                     Your goal is to deeply analyze a job offer in the context of the user's CV and specific requirements.
                     The order of agents is sequential: you first, then Summarizer.
                     User prompts only first messages, do not ask for any more information.
                     Use language of the offer for analysis.

                     Your task:
                     - Be concise and to the point.
                     - Analyze the job responsibilities and technical requirements from the offer.
                     - Compare them with the user's CV content to identify matches and gaps.
                     - Evaluate if the offer meets the user's provided requirements (if any).
                     - Identify any interesting trivia or unique aspects of the offer.
                     - Provide a detailed internal analysis with clear headings and bullet points.
                     - This analysis will be used by the Summarizer to create the final response.
                     - End your response with a clear instruction for the Summarizer to proceed.

                     If something is wrong or required data is missing (like CV or offer content), return reason and {FailSignal}.

                     Requirements for an offer defined by user (optional):
                     {request.UserRequirementsForOffer}

                     The CV content:
                     {request.CvContent}

                     Job offer content will be provided in first user prompt.
                     """,
                LoggerFactory = loggerFactory,
            };

            var summarizerAgent = new ChatCompletionAgent
            {
                Name = "Summarizer",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are a professional job offer summarizer.
                     You MUST wait for the Analyst to provide an analysis before you start your work.
                     Apply the analysis from Analyst to produce the final summary.
                     User prompts only first messages, do not ask for any more information.
                     Use language of the offer for summary.

                     Formatting rules:
                     - USE SIMPLE TEXT ONLY, DO NOT USE MARKDOWN, HTML, or any other formatting.
                     - Use multiple line breaks and - with spaces for bullet points.
                     - Be concise and to the point.

                     Final summary must contain sections with concise bullet-points defined below:
                     - Job abstract - most important information and responsibilities of the job
                     - User requirements - if offer matches requirements given by the user
                     - CV gaps - technologies, skills and responsibilities from offer that are not present in CV
                     - CV irrelevant - technologies, skills and responsibilities from CV that are not relevant for offer
                     - CV matches - technologies, skills, and responsibilities from offer that are strongly aligned with CV
                     - Suggestions - what to improve in CV content
                     - Trivia - interesting information, if there is any

                     End the message with {DoneSignal} to finish the conversation.

                     If something is wrong or required data is missing (like CV or offer content), return reason and {FailSignal}, which terminates the conversation.

                     Requirements for an offer defined by user (optional):
                     {request.UserRequirementsForOffer}

                     The CV content:
                     {request.CvContent}

                     Job offer content will be provided in first user prompt.
                     """,
                LoggerFactory = loggerFactory,
            };

            var chat = new AgentGroupChat(analystAgent, summarizerAgent)
            {
                ExecutionSettings = new AgentGroupChatSettings
                {
                    TerminationStrategy = new ApprovalTerminationStrategy
                    {
                        Agents = [summarizerAgent],
                        DoneSignal = DoneSignal,
                        FailSignal = FailSignal,
                        MaximumIterations = 3,
                    },
                    SelectionStrategy = new SequentialSelectionStrategy(),
                },
            };
            return chat;
        }

        [LoggerMessage(LogLevel.Information, "Summarizing offer content. Selected AI model: {aiModel}")]
        partial void LogSummarizingOfferContent(string aiModel);
    }
}
