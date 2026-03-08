using JobScraper.Web.Common.Entities;
using JobScraper.Web.Common.Models;
using JobScraper.Web.Integration.AiProvider;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JobScraper.Web.Features.Cv.Logic;

public partial class SelectCvTemplateForOffer
{
    public record Request(
        string OfferContent,
        string AiModel
    ) : IRequest<Response>;

    public record Response(
        long CvId = 0,
        string Name = "",
        List<ChatItem>? ChatItems = null,
        bool Success = true,
        string? ErrorMessage = null);

    public partial class Handler(
        JobsDbContext dbContext,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider
    ) : IRequestHandler<Request, Response>
    {
        private readonly ILogger<Handler> _logger = loggerFactory.CreateLogger<Handler>();

        public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            LogSelectingCvTemplateForOffer(request.AiModel);

            if (string.IsNullOrWhiteSpace(request.OfferContent))
                return new Response(Success: false, ErrorMessage: "Offer has no description to match against");

            var templates = await dbContext.Cvs
                .Where(c => c.IsTemplate)
                .ToListAsync(cancellationToken);

            if (templates.Count == 0)
                return new Response(Success: false, ErrorMessage: "No CV templates found");

            if (templates.Count == 1)
            {
                var template = templates[0];
                return new Response(template.Id, template.Name, []);
            }

            return await SelectCvWithAi(request, templates, cancellationToken);
        }

        private async ValueTask<Response> SelectCvWithAi(Request request,
            List<CvEntity> templates,
            CancellationToken cancellationToken)
        {
            var kernel = serviceProvider.GetAiKernel(request.AiModel);

            var templatesDescription = string.Join("\n\n-----------------------------------\n\n",
                templates.Select(t =>
                    $"--- Template ID: {t.Id}, Name: {t.Name} ---\n{t.MarkdownContent}"));

            var agent = new ChatCompletionAgent
            {
                Name = "CvAssistant",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are an expert recruiter assistant that selects the best CV template for a job offer.
                     Your task is to analyze a job offer description and a list of CV templates, then decide which template is the best match.

                     The most important criteria is language match - the CV template language should match the language of the offer description.
                     After language match, consider the relevance of skills, experience, and keywords between the CV content and the offer.

                     You MUST respond with the numeric ID of the best matching template.
                     No explanation, no other text - just the number.

                     Available CV templates:
                     {templatesDescription}
                     """,
                LoggerFactory = loggerFactory,
            };

            var chatHistory = new ChatHistory();

            chatHistory.AddUserMessage($"Select the best CV template for this offer:\n{request.OfferContent}");

            try
            {
                ChatMessageContent? lastResponse = null;
                await foreach (var response in agent.InvokeAsync(chatHistory, cancellationToken: cancellationToken))
                    lastResponse = response.Message;

                var chatItems = lastResponse is null
                    ? new List<ChatItem>()
                    : [ChatItem.From(lastResponse)];

                if (!long.TryParse(lastResponse?.Content?.Trim(), out var selectedId))
                    return new Response(
                        Success: false,
                        ChatItems: chatItems,
                        ErrorMessage: "AI did not return a valid template ID");

                var matchingTemplate = templates.FirstOrDefault(t => t.Id == selectedId);
                if (matchingTemplate is null)
                    return new Response(
                        Success: false,
                        ChatItems: chatItems,
                        ErrorMessage: $"AI selected non-existent template ID: {selectedId}");

                return new Response(matchingTemplate.Id, matchingTemplate.Name, chatItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI response in follow-up chat");
                var chatItems = chatHistory.Select(ChatItem.From).ToList();
                chatItems.Add(new ChatItem(AuthorRole.Assistant.Label, "System", $"Error: {ex.Message}"));

                return new Response(Success: false, ChatItems: chatItems, ErrorMessage: "Failed to get AI response");
            }
        }

        [LoggerMessage(LogLevel.Information, "Selecting CV template for offer. AiModel: {AiModel}")]
        partial void LogSelectingCvTemplateForOffer(string AiModel);
    }
}
