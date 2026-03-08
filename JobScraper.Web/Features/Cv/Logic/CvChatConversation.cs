using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.Cv.Helpers;
using JobScraper.Web.Integration.AiProvider;
using Mediator;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace JobScraper.Web.Features.Cv.Logic;

#pragma warning disable SKEXP0110

public partial class CvChatConversation
{
    public record Request(
        string UserMessage,
        string CurrentCvContent,
        string? OfferContent,
        string? OfferSummary,
        List<ChatItem> ExistingChatHistory,
        string ProviderName,
        string? UserCvRules = null
    ) : IRequest<Response>;

    public record Response(List<ChatItem> ChatHistory, string? AdjustedCvContent = null);

    public partial class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    ) : IRequestHandler<Request, Response>
    {
        private const string CvStartMarker = "[CV_START]";
        private const string CvEndMarker = "[CV_END]";

        private readonly ILogger<Handler> _logger = loggerFactory.CreateLogger<Handler>();

        public async ValueTask<Response> Handle(Request request, CancellationToken ct = default)
        {
            LogSendingFollowUpMessage(request.ProviderName);

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

                var adjustedCv = ExtractCvContent(lastResponse?.Content);

                if (adjustedCv is not null)
                    ReplaceCvContentInHistory(chatItems);

                return new Response(chatItems, adjustedCv);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI response in follow-up chat");
                var chatItems = chatHistory.Select(ChatItem.From).ToList();
                chatItems.Add(new ChatItem(AuthorRole.Assistant.Label, "System", $"Error: {ex.Message}"));

                return new Response(chatItems);
            }
        }

        private static string? ExtractCvContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            var startIdx = content.IndexOf(CvStartMarker, StringComparison.OrdinalIgnoreCase);
            var endIdx = content.IndexOf(CvEndMarker, StringComparison.OrdinalIgnoreCase);

            if (startIdx < 0 || endIdx < 0 || endIdx <= startIdx)
                return null;

            return content[(startIdx + CvStartMarker.Length)..endIdx]
                .RemoveAiChars();
        }

        private static void ReplaceCvContentInHistory(List<ChatItem> chatHistory)
        {
            for (var i = 0; i < chatHistory.Count; i++)
            {
                var item = chatHistory[i];
                if (item.Content is null)
                    continue;

                var startIdx = item.Content.IndexOf(CvStartMarker, StringComparison.OrdinalIgnoreCase);
                var endIdx = item.Content.IndexOf(CvEndMarker, StringComparison.OrdinalIgnoreCase);

                if (startIdx < 0 || endIdx < 0 || endIdx <= startIdx)
                    continue;

                var before = item.Content[..startIdx];
                var after = item.Content[(endIdx + CvEndMarker.Length)..];
                chatHistory[i] = item with
                {
                    Content = $"{before}{after}",
                };
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
                     You are a professional CV consultant and editor. You help users modify and improve their CV content.
                     {(request.OfferContent is not null
                         ? "You are helping the user tailor their CV for a specific job offer."
                         : "No specific job offer is provided. Focus on general CV improvements, formatting, and best practices.")}
                     You can suggest changes, answer questions about CV optimization, and edit the CV when asked.
                     Be concise and to the point.
                     Return simple bullet points unless user asks for more details.
                     Use user's language for responses.
                     USE SIMPLE TEXT ONLY for regular responses. DO NOT USE MARKDOWN, HTML, or any other formatting.
                     Use multiple line breaks and - with spaces for bullet points.

                     IMPORTANT: When the user explicitly asks you to modify, edit, rewrite, update, or adjust the CV content,
                     you MUST return the COMPLETE modified CV wrapped between {CvStartMarker} and {CvEndMarker} markers.
                     The CV content inside markers MUST be in markdown format (headings, bold, italic, bullet lists).
                     Only include the markers when returning modified CV content. For regular conversation, analysis,
                     or suggestions, do NOT include the markers.

                     Example when user asks to modify the CV:
                     Here is the updated CV:
                     {CvStartMarker}
                     (complete CV markdown here)
                     {CvEndMarker}

                     Rules for CV editing:
                     - DO NOT invent experiences or skills that are not in the original CV, unless user explicitly asks for them.
                     - DO NOT remove core contact information.
                     - Apply targeted, meaningful edits — do not rewrite the whole CV unless asked.
                     - KEEP the original markdown structure.
                     {(string.IsNullOrWhiteSpace(request.UserCvRules) ? "" : $"\nAdditional user-defined CV rules that MUST be followed:\n{request.UserCvRules}")}

                     CV content:
                     {request.CurrentCvContent}

                     {(request.OfferContent is not null ? $"Job offer content:\n{request.OfferContent}" : "")}

                     {(request.OfferSummary is not null ? $"Offer summary:\n{request.OfferSummary}" : "")}
                     """,
                LoggerFactory = loggerFactory,
            };
            return agent;
        }

        [LoggerMessage(LogLevel.Information, "Sending follow-up message in AI CV chat. Selected model: {aiModel}")]
        partial void LogSendingFollowUpMessage(string aiModel);
    }
}
