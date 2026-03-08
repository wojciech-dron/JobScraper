using JobScraper.Web.Common.Models;
using JobScraper.Web.Features.AiSummary.Logic;
using JobScraper.Web.Features.Cv.Helpers;
using JobScraper.Web.Integration.AiProvider;
using Mediator;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable OPENAI001

namespace JobScraper.Web.Features.Cv.Logic;

#pragma warning disable SKEXP0110

public partial class AdjustCvForOffer
{
    public record Request(
        string CvContent,
        string OfferContent,
        string? OfferSummary,
        string AiModel
    ) : IRequest<Response>;

    public record Response(
        bool Success,
        string? AdjustedCvContent,
        List<ChatItem> ChatHistory
    );

    public partial class Handler(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory
    ) : IRequestHandler<Request, Response>
    {
        private const string DoneSignal = "[DONE]";
        private const string FailSignal = "[FAIL]";
        private const string CvStartMarker = "[CV_START]";
        private const string CvEndMarker = "[CV_END]";
        private const int MaxRetries = 2;

        private readonly ILogger<Handler> _logger = loggerFactory.CreateLogger<Handler>();

        public async ValueTask<Response> Handle(Request request, CancellationToken ct = default)
        {
            LogStartingAiCvAdjustment(request.AiModel);

            var offerMessage = new ChatMessageContent(AuthorRole.User, $"offerContent: {request.OfferContent}");

            string? finalContent = null;
            var retryCount = 0;
            var chatHistory = new List<ChatItem>();
            do
            {
                var chat = PrepareAgentsChat(request.CvContent, request.AiModel);
                chat.AddChatMessage(offerMessage);

                if (request.OfferSummary is not null)
                {
                    var summaryMessage = new ChatMessageContent(AuthorRole.User, $"offerSummary: {request.OfferSummary}");
                    chat.AddChatMessage(summaryMessage);
                }

                await foreach (var response in chat.InvokeAsync(ct))
                {
                    chatHistory.Add(ChatItem.From(response));
                    finalContent = response.Content;
                }
            } while (ShouldRetry(finalContent, retryCount++));

            if (finalContent is null || !finalContent.Contains(DoneSignal))
                return new Response(false, null, chatHistory);

            var adjustedCv = ExtractCvContent(finalContent);
            if (string.IsNullOrWhiteSpace(adjustedCv))
                return new Response(false, null, chatHistory);

            ReplaceCvContentInHistory(chatHistory);

            return new Response(true, adjustedCv, chatHistory);
        }

        private static string? ExtractCvContent(string content)
        {
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
                    Content = $"{before}{after}".Replace(DoneSignal, "").Trim(),
                };
            }
        }

        private static bool ShouldRetry(string? lastContent, int retryCount)
        {
            if (retryCount >= MaxRetries)
                return false;

            if (string.IsNullOrEmpty(lastContent))
                return true;

            if (lastContent.Contains(FailSignal, StringComparison.OrdinalIgnoreCase))
                return true;

            if (lastContent.Length < DoneSignal.Length + 50)
                return true;

            return false;
        }

        private AgentGroupChat PrepareAgentsChat(string cvContent, string aiModel)
        {
            var kernel = serviceProvider.GetAiKernel(aiModel);

            var analyzerAgent = new ChatCompletionAgent
            {
                Name = "CvAnalyzer",
                Kernel = kernel,
                Instructions =
                    $"""
                     You are a professional CV analyst.
                     Your goal is to analyze the job offer and the user's CV to find the best alignment.
                     Before any CV editing happens, you must provide short analysis.
                     User prompts only first messages, do not ask for any more information.
                     Use language of the offer for analysis.

                     Your task:
                     - Be concise and to the point.
                     - Analyze CV for ATS compliance with the offer.
                     - Prioritize: 1) Skills alignment, 2) Experience relevance, 3) Summary optimization.
                     - Focus only on the developer title, job positions and 'Summary', 'Experience', 'Skills' sections.
                     - Infer missing skills and experience only if they are required in the offer and the CV evidence strongly suggests they exist.
                     - Remove unnecessary skills and experiences if they are NOT RELEVANT to the job offer.
                     - Return the analysis in a simple format, with clear headings and bullet points.
                     - List specific suggestions for the CV editor on how to optimize each section.
                     - DO NOT rewrite the CV yourself. Just provide the analysis and suggestions.
                     - End your response with a clear instruction for the CvEditor to proceed.

                     The original CV content:
                     {cvContent}

                     An optional OfferSummary may follow the offer content in the conversation.

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
                     The output markdown will be rendered to PDF via QuestPDF. Avoid complex markdown features (no tables, no HTML, no images). Stick to headings, bold, italic, and bullet lists.
                     Apply the suggestions from CvAnalyzer's analysis to produce the final CV.
                     Apply targeted, meaningful edits to the relevant sections — do not rewrite the whole CV.
                     User prompts only first messages, do not ask for any more information.
                     Use language of the offer for response.

                     Your workflow:
                     - Start with the original CV content provided below.
                     - Apply CvAnalyzer's suggestions to tailor the CV for the offer.
                     - Return the COMPLETE final CV (not just changed sections) in markdown format.
                     - Wrap the CV content between {CvStartMarker} and {CvEndMarker} markers.
                     - After {CvEndMarker}, include a concise summary of all applied changes with a short reason for each change.
                     - End the message with {DoneSignal} to finish the conversation.

                     Example output format:
                     {CvStartMarker}
                     (complete CV markdown here)
                     {CvEndMarker}

                     **Changes applied:**
                     - (change 1) — (short reason)
                     - (change 2) — (short reason)

                     {DoneSignal}

                     Rules:
                     - DO NOT invent experiences or skills that are not in the original CV.
                     - DO NOT remove core contact information.
                     - DO NOT modify languages and interests sections.
                     - DO optimize the 'Summary', 'Experience', or 'Skills' sections to include relevant matches based on the analysis.
                     - DO use terminology from the job offer where appropriate to increase alignment.
                     - Remove unnecessary skills and experiences if they are NOT RELEVANT to the job offer.
                     - KEEP the original markdown structure.

                     If something is wrong or required data is missing, return reason and {FailSignal}, which terminates the conversation.

                     The original CV content:
                     {cvContent}

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
                        MaximumIterations = 10,
                    },
                    SelectionStrategy = new SequentialSelectionStrategy(),
                },
            };
            return chat;
        }

        [LoggerMessage(LogLevel.Information, "Starting AI CV adjustment conversation. Selected model: {aiModel}")]
        partial void LogStartingAiCvAdjustment(string aiModel);
    }
}
