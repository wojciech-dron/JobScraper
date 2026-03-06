using JobScraper.Web.Common.Models;
using JobScraper.Web.Integration.AiProvider;
using Mediator;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace JobScraper.Web.Features.Cv.Logic;

#pragma warning disable SKEXP0110

public class AiSimpleCvChatConversation
{
    public record Request(
        string UserMessage,
        string CurrentCvContent,
        string OfferContent,
        string? OfferSummary,
        List<ChatItem> ExistingChatHistory,
        string UserRequirements = "",
        string ProviderName = AiProvidersConfig.MainProvider
    ) : IRequest<Response>;

    public record Response(List<ChatItem> ChatHistory);

    public class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    ) : IRequestHandler<Request, Response>
    {

        private readonly ILogger<Handler> _logger = loggerFactory.CreateLogger<Handler>();

        public async ValueTask<Response> Handle(Request request, CancellationToken ct = default)
        {
            _logger.LogInformation("Sending follow-up message in simple AI CV chat");

            var kernel = serviceProvider.GetAiKernel(request.ProviderName);
            var agent = PrepareAgent(request, kernel);

            var chatHistory = CreateHistoryWithPreviousContext(request);
            chatHistory.AddUserMessage(request.UserMessage);

            try
            {
                ChatMessageContent? lastResponse = null;
                await foreach (var response in agent.InvokeAsync(chatHistory, cancellationToken: ct))
                    lastResponse = response.Message;

                var chatItems = chatHistory.Select(ChatItem.From).ToList();

                if (lastResponse is not null)
                    chatItems.Add(ChatItem.From(lastResponse));

                return new Response(chatItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI response in follow-up chat");
                var chatItems = chatHistory.Select(ChatItem.From).ToList();
                chatItems.Add(new ChatItem(AuthorRole.Assistant.Label, "System", $"Error: {ex.Message}"));

                return new Response(chatItems);
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
                     You can suggest changes, answer questions about CV optimization.
                     Be concise and to the point.
                     Return simple bullet points unless user asks for more details.
                     Use user's language for responses.
                     USE SIMPLE TEXT ONLY, DO NOT USE MARKDOWN, HTML, or any other formatting.
                     Use multiple line breaks and - with spaces for bullet points.

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
