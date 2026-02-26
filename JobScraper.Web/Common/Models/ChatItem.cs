using Microsoft.SemanticKernel;

namespace JobScraper.Web.Common.Models;

public record ChatItem(
    string? Role,
    string? AuthorName,
    string? Content
)
{
    public static ChatItem From(ChatMessageContent content) =>
        new(content.Role.Label, content.AuthorName, content.Content);
}
