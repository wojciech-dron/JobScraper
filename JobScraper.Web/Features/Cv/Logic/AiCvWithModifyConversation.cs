using JobScraper.Web.Common.Models;
using JobScraper.Web.Integration.AiProvider;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace JobScraper.Web.Features.Cv.Logic;

#pragma warning disable SKEXP0110

public class AiCvWithModifyConversation
{
    public record Request(
        string UserMessage,
        string CurrentCvContent,
        string OfferContent,
        string? OfferSummary,
        List<ChatItem> ExistingChatHistory,
        string UserRequirements = "",
        string ProviderName = AiProvidersConfig.MainProvider
    );

    public record Response(
        bool Success,
        string? AdjustedCvContent,
        List<ChatItem> ChatHistory
    );

    public class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    )
    {
        private const string DoneSignal = "[DONE]";

        private readonly ILogger<Handler> _logger = loggerFactory.CreateLogger<Handler>();

        public async Task<Response> Handle(Request request, CancellationToken ct = default)
        {
            _logger.LogInformation("Sending follow-up message in AI CV with modifications chat");
            var replaceTextPlugin = new ReplaceTextPlugin(request.CurrentCvContent);

            var kernel = serviceProvider.GetAiKernel(request.ProviderName);
            kernel.Plugins.AddFromObject(replaceTextPlugin);
            var agent = PrepareAgent(request, kernel);

            var chatHistory = CreateHistoryWithPreviousContext(request);
            chatHistory.AddUserMessage(request.UserMessage);

            try
            {
                var lastResponse = "";
                do
                    await foreach (var response in agent.InvokeAsync(chatHistory, cancellationToken: ct))
                        lastResponse = response.Message.Content ?? "";
                while (!lastResponse.Contains(DoneSignal));

                var chatItems = chatHistory.Select(ChatItem.From).ToList();
                var modifiedCv = replaceTextPlugin.GetModifiedText();

                return new Response(true, modifiedCv, chatItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI response in follow-up chat");
                var chatItems = chatHistory.Select(ChatItem.From).ToList();
                chatItems.Add(new ChatItem(AuthorRole.Assistant.Label, "System", $"Error: {ex.Message}"));

                return new Response(false, null, chatItems);
            }
        }
        private static ChatHistory CreateHistoryWithPreviousContext(Request request)
        {
            var chatHistory = new ChatHistory();

            // Add existing chat history as context
            foreach (var item in request.ExistingChatHistory)
                if (item.Role == AuthorRole.User.Label)
                    chatHistory.AddUserMessage(item.Content ?? "");
                else
                    chatHistory.AddAssistantMessage(item.Content ?? "");
            return chatHistory;
        }

        private ChatCompletionAgent PrepareAgent(Request request, Kernel kernel)
        {
            var agent = new ChatCompletionAgent
            {
                Name = "CvAssistant",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are a professional CV consultant. You help users modify and improve their CV content
                     based on a specific job offer.
                     You can suggest changes, answer questions about CV optimization,
                     When asked to modify the CV, use the tools provided to make the changes.
                     You can work in loop with yourself to find the best solution.
                     When you have final answer, FINISH your response with {DoneSignal}.

                     You have access to the following tools:
                     - replace_text: Use this to make targeted edits to the CV content. Call it multiple times for each change.
                     - diff_text: Use this to compare original and modified content to verify your changes.
                     - get_original_text: Use this to get original CV content, if needed.
                     - get_modified_text: Use this to get current CV content, if needed.

                     Rules:
                     - DO NOT invent experiences or skills that are not in the original CV.
                     - DO NOT remove core contact information.
                     - DO optimize the 'Summary', 'Experience', or 'Skills' sections to include relevant matches.
                     - DO use terminology from the job offer where appropriate to increase alignment.
                     - MAINTAIN the original markdown structure, keep line length about 90 characters.
                     - Use replace_text for EACH modification instead of rewriting the entire CV at once.
                     - It is OK to call tools multiple times in multiple iterations.

                     CV content:
                     {request.CurrentCvContent}

                     Job offer content:
                     {request.OfferContent}

                     {(request.OfferSummary is not null ? $"Offer summary:\n{request.OfferSummary}" : "")}
                     {(request.UserRequirements != "" ? $"User requirements:\n{request.UserRequirements}" : "")}
                     """,
                LoggerFactory = loggerFactory,
            };
            return agent;
        }
    }
}
