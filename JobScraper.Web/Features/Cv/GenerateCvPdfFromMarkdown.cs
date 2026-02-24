using ErrorOr;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Mediator;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Unit = QuestPDF.Infrastructure.Unit;

namespace JobScraper.Web.Features.Cv;

public class GenerateCvPdfFromMarkdown
{
    public record Command(string CvContent, LayoutConfig Config) : IRequest<ErrorOr<byte[]>>;

    public class Handler : IRequestHandler<Command, ErrorOr<byte[]>>
    {
        public ValueTask<ErrorOr<byte[]>> Handle(Command request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.CvContent);

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var mdDocument = Markdown.Parse(request.CvContent, pipeline);

            var pdfDocument = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                    page.Content().PaddingTop(10).Column(mainColumn =>
                    {
                        // Split document into "header" blocks (to be next to image) and "body" blocks.
                        // Let's take the first 3 blocks for the header (Title, Profile heading, and Profile text).
                        // This avoids the whole document being constrained to the side of the image.
                        var headerBlocks = mdDocument.Take(3).ToList();
                        var bodyBlocks = mdDocument.Skip(3).ToList();

                        mainColumn.Item().Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                foreach (var block in headerBlocks)
                                    RenderBlock(column, block);
                            });

                            // row.ConstantItem(100).PaddingLeft(10).Image(imagePath);
                        });

                        foreach (var block in bodyBlocks)
                            mainColumn.Item().Column(column => RenderBlock(column, block));
                    });

                });
            });

            var result = pdfDocument.GeneratePdf();
            return ValueTask.FromResult<ErrorOr<byte[]>>(result);
        }


        private void RenderBlock(ColumnDescriptor column, Block block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    column.Item().PaddingTop(heading.Level == 1 ? 0 : 10).PaddingBottom(5).Text(text =>
                    {
                        float fontSize = heading.Level switch
                        {
                            1 => 24,
                            2 => 18,
                            3 => 14,
                            _ => 12,
                        };
                        text.Span(GetLeafText(heading)).FontSize(fontSize).Bold()
                            .FontColor(heading.Level == 1 ? Colors.Blue.Medium : Colors.Black);
                    });
                    break;

                case ParagraphBlock paragraph:
                    column.Item().PaddingBottom(5).Text(text =>
                    {
                        RenderInlines(text, paragraph.Inline);
                    });
                    break;

                case ListBlock listBlock:
                    foreach (var item in listBlock)
                        if (item is ListItemBlock listItem)
                            column.Item().PaddingLeft(15).Row(row =>
                            {
                                row.ConstantItem(15).Text("•");
                                row.RelativeItem().Column(itemColumn =>
                                {
                                    foreach (var subBlock in listItem)
                                        RenderBlock(itemColumn, subBlock);
                                });
                            });

                    break;

                case ThematicBreakBlock:
                    column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    break;

                default:
                    if (block is LeafBlock leaf)
                        column.Item().Text(GetLeafText(leaf));

                    break;
            }
        }

        private void RenderInlines(TextDescriptor text, ContainerInline? container)
        {
            if (container == null)
                return;

            foreach (var inline in container)
                switch (inline)
                {
                    case LiteralInline literal:
                        text.Span(literal.Content.ToString());
                        break;
                    case EmphasisInline emphasis:
                        var textSpan = text.Span(GetInlineText(emphasis));
                        if (emphasis.DelimiterCount == 2) textSpan.Bold();
                        if (emphasis.DelimiterCount == 1) textSpan.Italic();
                        break;
                    case LineBreakInline:
                        break;
                    case not null when inline.GetType().Name == "SoftlineBreakInline":
                        text.Span(" ");
                        break;
                    case HtmlInline html:
                        text.Span(html.Tag);
                        break;
                    default:
                        text.Span(GetInlineText(inline));
                        break;
                }
        }

        private string GetLeafText(LeafBlock leaf)
        {
            if (leaf.Inline == null) return "";

            return GetInlineText(leaf.Inline);
        }

        private string GetInlineText(Inline inline)
        {
            var sw = new StringWriter();
            RenderInlineToString(sw, inline);
            return sw.ToString();
        }

        private void RenderInlineToString(StringWriter sw, Inline inline)
        {
            if (inline is LiteralInline literal) sw.Write(literal.Content.ToString());
            else if (inline.GetType().Name == "SoftlineBreakInline") sw.Write(" ");
            else if (inline is HtmlInline html) sw.Write(html.Tag);
            else if (inline is ContainerInline container)
                foreach (var sub in container)
                    RenderInlineToString(sw, sub);
        }
    }

}

public class LayoutConfig
{ }
