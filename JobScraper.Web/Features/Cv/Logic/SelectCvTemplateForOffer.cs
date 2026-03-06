using ErrorOr;
using JobScraper.Web.Integration.AiProvider;
using JobScraper.Web.Modules.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JobScraper.Web.Features.Cv.Logic;

public class SelectCvTemplateForOffer
{
    public record Request(
        string OfferUrl,
        string ProviderName
    ) : IRequest<ErrorOr<Response>>;

    public record Response(int CvId);

    public class Handler(
        JobsDbContext dbContext,
        IServiceProvider serviceProvider
    ) : IRequestHandler<Request, ErrorOr<Response>>
    {
        public async ValueTask<ErrorOr<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var userOffer = await dbContext.UserOffers
                .Include(u => u.Details)
                .FirstOrDefaultAsync(u => u.OfferUrl == request.OfferUrl, cancellationToken);

            if (userOffer is null)
                return Error.NotFound(description: "User offer not found");

            var offerDescription = userOffer.Details?.Description;
            if (string.IsNullOrWhiteSpace(offerDescription))
                return Error.Validation(description: "Offer has no description to match against");

            var templates = await dbContext.Cvs
                .Where(c => c.IsTemplate)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.MarkdownContent,
                })
                .ToListAsync(cancellationToken);

            if (templates.Count == 0)
                return Error.NotFound(description: "No CV templates found");

            if (templates.Count == 1)
                return new Response((int)templates[0].Id);

            var templatesDescription = string.Join("\n\n",
                templates.Select(t =>
                    $"--- Template ID: {t.Id}, Name: {t.Name} ---\n{t.MarkdownContent}"));

            var kernel = serviceProvider.GetAiKernel(request.ProviderName);
            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(
                $"""
                 You are an expert recruiter assistant that selects the best CV template for a job offer.
                 Your task is to analyze a job offer description and a list of CV templates, then decide which template is the best match.

                 The most important criteria is language match - the CV template language should match the language of the offer description.
                 After language match, consider the relevance of skills, experience, and keywords between the CV content and the offer.

                 You MUST respond with ONLY the numeric ID of the best matching template. No explanation, no other text - just the number.

                 Available CV templates:
                 {templatesDescription}
                 """);

            chatHistory.AddUserMessage($"Select the best CV template for this offer:\n{offerDescription}");

            var response = await chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                kernel: kernel,
                cancellationToken: cancellationToken);

            var responseText = response.Content?.Trim();
            if (string.IsNullOrWhiteSpace(responseText))
                return Error.Failure(description: "AI returned empty response");

            if (!long.TryParse(responseText, out var selectedId))
                return Error.Failure(description: $"AI returned invalid template ID: {responseText}");

            var matchingTemplate = templates.FirstOrDefault(t => t.Id == selectedId);
            if (matchingTemplate is null)
                return Error.Failure(description: $"AI selected non-existent template ID: {selectedId}");

            return new Response((int)matchingTemplate.Id);
        }
    }
}
