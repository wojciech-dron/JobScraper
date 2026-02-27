using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

namespace JobScraper.Web.Features.AiSummary.Logic;
#pragma warning disable SKEXP0110

internal class ApprovalTerminationStrategy : TerminationStrategy
{
    public string? FinalAgentName { get; init; }
    public required string DoneSignal { get; init; }
    public required string FailSignal { get; init; }

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent,
        IReadOnlyList<ChatMessageContent> history,
        CancellationToken cancellationToken) =>
        Task.FromResult(ShouldAgentTerminate(agent, history));

    private bool ShouldAgentTerminate(Agent agent, IReadOnlyList<ChatMessageContent> history)
    {
        var content = history[^1].Content;

        if (content?.Contains(FailSignal) == true)
            return true;

        if (!string.IsNullOrWhiteSpace(FinalAgentName) && agent.Name != FinalAgentName)
            return false;

        if (string.IsNullOrEmpty(DoneSignal))
            return true;

        var terminationSignal = content?.Contains(DoneSignal, StringComparison.OrdinalIgnoreCase) == true;

        return terminationSignal;
    }
}
