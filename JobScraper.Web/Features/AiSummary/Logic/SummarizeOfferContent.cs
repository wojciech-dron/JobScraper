using ErrorOr;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Integration.AiProvider;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JobScraper.Web.Features.AiSummary.Logic;
#pragma warning disable SKEXP0110

public class SummarizeOfferContent
{
    public record Request(
        string CvContent,
        string OfferContent,
        string UserRequirementsForOffer,
        string ProviderName = AiProvidersConfig.MainProvider
    );

    public record Response(string? AiSummary, List<ChatItem> ChatHistory);

    public class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    )
    {
        private const string DoneSignal = "[DONE]";
        private const string FailSignal = "[FAIL]";
        private const int MaxRetries = 5;

        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
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

            var summary = finalContent.Replace(DoneSignal, "").Trim();

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
            var kernel = serviceProvider.GetAiKernel(request.ProviderName);

            var analystAgent = new ChatCompletionAgent
            {
                Name = "Analyst",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are an analyst designed to deeply analyze a job offer in the context of a user's CV and specific requirements.
                     The order of agents is sequential, you first, then Summarizer.

                     Your task is to perform a multi-step reasoning:
                     1. Analyze the job responsibilities and technical requirements from the offer.
                     2. Compare them with the user's CV content to identify matches and gaps.
                     3. Evaluate if the offer meets the user's provided requirements.
                     4. Identify any interesting trivia or unique aspects of the offer.

                     Provide a detailed internal analysis. This analysis will be used by the Summarizer to create the final response.
                     If something is wrong or required data is missing (like cv or offer content), return reason and {FailSignal}.

                     Requirements for an offer defined by user (optional):
                     {request.UserRequirementsForOffer}

                     The CV content:
                     {request.CvContent}


                     Job offer content will be provided in user prompt.
                     """,
                LoggerFactory = loggerFactory,
            };

            var summarizerAgent = new ChatCompletionAgent
            {
                Name = "Summarizer",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are an agent that holds a conversation with analyst agent to provide a final summary of the job offer.
                     - The order of agents is sequential, analyst first, then summarizer.
                     - You can ask questions to analyst agent to clarify details.
                     - When you generate a final summary, finish it with {DoneSignal}, that ends the conversation.
                     - Use language of the offer for summary.
                     - Use plain text only.
                     - If something is wrong or required data is missing (like cv or offer content),
                     - Return reason and {FailSignal}, that terminates conversation.

                     Final summary must contain sections with concise bullet-points defined below:
                     - Job abstract - most important information and responsibilities of the job
                     - User requirements - if offer matches requirements given by the user
                     - CV gaps - technologies, skills and responsibilities from offer that are not present in CV
                     - CV irrelevant - technologies, skills and responsibilities from CV that are not relevant for offer
                     - CV matches - technologies, skills, and responsibilities from offer that are strongly aligned with CV
                     - Suggestions - what to improve in CV content
                     - Trivia - interesting information, if there is any

                     Requirements for an offer defined by user (optional):
                     {request.UserRequirementsForOffer}

                     The CV content:
                     {request.CvContent}


                     Job offer content will be provided in user prompt.
                     """,
                LoggerFactory = loggerFactory,
            };

            var chat = new AgentGroupChat(analystAgent, summarizerAgent)
            {
                ExecutionSettings = new AgentGroupChatSettings
                {
                    TerminationStrategy = new ApprovalTerminationStrategy
                    {
                        FinalAgentName = summarizerAgent.Name,
                        DoneSignal = DoneSignal,
                        FailSignal = FailSignal,
                        MaximumIterations = 3,
                    },
                    SelectionStrategy = new SequentialSelectionStrategy(),
                },
                LoggerFactory = loggerFactory,
            };
            return chat;
        }
    }
}
