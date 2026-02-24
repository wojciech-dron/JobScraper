using System.Diagnostics.CodeAnalysis;
using ErrorOr;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Mediator;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Unit = QuestPDF.Infrastructure.Unit;

namespace JobScraper.Web.Features.Cv;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("ReSharper", "CognitiveComplexity")]
public class GenerateCvPdfFromMarkdown
{
    public record Command(
        CvContent Content,
        LayoutConfig LayoutConfig
    ) : IRequest<ErrorOr<byte[]>>;

    public record CvContent(
        string Markdown,
        byte[]? Image = null,
        string? Disclaimer = null
    );

    public class Handler : IRequestHandler<Command, ErrorOr<byte[]>>
    {
        public ValueTask<ErrorOr<byte[]>> Handle(Command request, CancellationToken cancellationToken)
        {
            var content = request.Content;
            ArgumentNullException.ThrowIfNull(content);
            ArgumentException.ThrowIfNullOrWhiteSpace(content.Markdown);

            var layoutConfig = request.LayoutConfig;
            ArgumentNullException.ThrowIfNull(layoutConfig);

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var mdDocument = Markdown.Parse(content.Markdown, pipeline);

            var pdfDocument = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(layoutConfig.MarginCm, Unit.Centimetre);
                    page.PageColor(layoutConfig.PageColor);
                    page.DefaultTextStyle(x =>
                        x.FontSize(layoutConfig.DefaultFontSize)
                            .FontFamily(layoutConfig.FontFamily)
                            .LineHeight(layoutConfig.LineHeight));

                    page.Content()
                        .PaddingTop(layoutConfig.HeaderPaddingTop)
                        .Column(mainColumn =>
                        {
                            // Split document into "header" blocks (to be next to image) and "body" blocks.
                            // Let's take the first N blocks for the header (Title, Profile heading, and Profile text).
                            // This avoids the whole document being constrained to the side of the image.
                            var blocksBesideImage = mdDocument.Take(layoutConfig.TextBlocksAlignedToImage).ToArray();

                            mainColumn.Item().Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    foreach (var block in blocksBesideImage)
                                        RenderBlock(column, block, layoutConfig);
                                });

                                // insert image
                                if (content.Image is { Length: > 0 })
                                    row.AutoItem().AlignRight()
                                        .PaddingLeft(layoutConfig.ImagePaddingLeft)
                                        // force a square container to avoid extra vertical whitespace
                                        .Height(layoutConfig.ImageHeight)
                                        .Width(layoutConfig.ImageWidth)
                                        // draw circular border
                                        .Border(layoutConfig.ImageBorderThickness)
                                        .BorderColor(layoutConfig.ImageBorderColor)
                                        .CornerRadius(layoutConfig.ImageBorderRadius)
                                        // scale image to fill the square area
                                        .Image(content.Image)
                                        .FitArea();
                            });


                            var bodyBlocks = mdDocument.Skip(layoutConfig.TextBlocksAlignedToImage).ToArray();

                            foreach (var block in bodyBlocks)
                                RenderBlock(mainColumn, block, layoutConfig);
                        });

                    if (!string.IsNullOrWhiteSpace(content.Disclaimer))
                        page.Footer()
                            .PaddingBottom(layoutConfig.FooterPaddingBottom)
                            .AlignCenter()
                            .Text(content.Disclaimer)
                            .FontSize(layoutConfig.DisclaimerFontSize)
                            .FontColor(layoutConfig.DisclaimerColor);
                });
            });

            var result = pdfDocument.GeneratePdf();
            return ValueTask.FromResult<ErrorOr<byte[]>>(result);
        }

        private void RenderBlock(ColumnDescriptor column, Block block, LayoutConfig layoutConfig)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    column.Item()
                        .PaddingTop(heading.Level == 1 ? 0 : layoutConfig.HeadingPaddingTop)
                        .PaddingBottom(layoutConfig.HeadingPaddingBottom)
                        .Row(row => row.AutoItem().Column(col =>
                        {
                            col.Item().Text(text =>
                            {
                                var fontSize = heading.Level switch
                                {
                                    1 => layoutConfig.H1FontSize,
                                    2 => layoutConfig.H2FontSize,
                                    3 => layoutConfig.H3FontSize,
                                    4 => layoutConfig.H4FontSize,
                                    _ => layoutConfig.HeadingFontSizeDefault,
                                };

                                var headerColor = heading.Level == 1
                                    ? layoutConfig.H1FontColor
                                    : layoutConfig.HeadingFontColorDefault;

                                text.Span(GetLeafText(heading))
                                    .FontSize(fontSize)
                                    .Bold()
                                    .FontColor(headerColor);
                            });

                            // decorative underline for section headings
                            if (heading.Level is 1 or 2)
                                col.Item()
                                    .PaddingTop(2)
                                    .LineHorizontal(2f)
                                    .LineColor(heading.Level == 1
                                        ? layoutConfig.H1FontColor
                                        : layoutConfig.ThematicBreakColor);
                        }));
                    break;

                case ParagraphBlock paragraph:
                    column.Item()
                        .PaddingBottom(layoutConfig.ParagraphPaddingBottom)
                        .Text(text => RenderInlines(text, paragraph.Inline, layoutConfig));
                    break;

                case ListBlock listBlock:
                    foreach (var item in listBlock)
                        if (item is ListItemBlock listItem)
                            column.Item()
                                .PaddingLeft(layoutConfig.ListPaddingLeft)
                                .Row(row =>
                                {
                                    row.ConstantItem(layoutConfig.ListBulletWidth)
                                        .Text(layoutConfig.ListBulletText)
                                        .FontColor(Colors.Grey.Darken2);

                                    row.RelativeItem().Column(itemColumn =>
                                    {
                                        foreach (var subBlock in listItem)
                                            RenderBlock(itemColumn, subBlock, layoutConfig);
                                    });
                                });
                    break;

                case ThematicBreakBlock:
                    column.Item()
                        .PaddingVertical(layoutConfig.ThematicBreakPaddingVertical)
                        .LineHorizontal(layoutConfig.ThematicBreakThickness)
                        .LineColor(layoutConfig.ThematicBreakColor);
                    break;

                case CodeBlock codeBlock:
                    column.Item()
                        .PaddingVertical(4)
                        .Background(Colors.Grey.Lighten4)
                        .Padding(8)
                        .Border(0.5f)
                        .BorderColor(Colors.Grey.Lighten2)
                        .CornerRadius(4)
                        .Text(GetLeafText(codeBlock))
                        .FontFamily(Fonts.Consolas)
                        .FontSize(layoutConfig.DefaultFontSize - 0.5f);
                    break;


                case QuoteBlock quote:
                    column.Item()
                        .Row(r =>
                        {
                            r.ConstantItem(3)
                                .Background(Colors.Indigo.Lighten2);

                            r.RelativeItem()
                                .Background(Colors.Grey.Lighten5)
                                .PaddingLeft(8)
                                .Padding(6)
                                .Column(qc =>
                                {
                                    foreach (var sub in quote)
                                        RenderBlock(qc, sub, layoutConfig);
                                });
                        });
                    break;

                default:
                    if (block is LeafBlock leaf)
                        column.Item().Text(GetLeafText(leaf));

                    break;
            }
        }

        private void RenderInlines(TextDescriptor text, ContainerInline? container, LayoutConfig layoutConfig)
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
                    case HtmlInline html:
                        text.Span(html.Tag);
                        break;
                    case not null when inline.GetType().Name == "SoftlineBreakInline":
                        text.Span(" ");
                        break;
                    case LinkInline link:
                        text.Span(GetInlineText(link))
                            .FontColor(layoutConfig.UrlColor)
                            .Underline();
                        break;
                    default:
                        text.Span(GetInlineText(inline));
                        break;
                }
        }

        private string GetLeafText(LeafBlock leaf)
        {
            if (leaf.Inline == null)
                return "";

            return GetInlineText(leaf.Inline);
        }

        private string GetInlineText(Inline? inline)
        {
            if (inline is null)
                return "";

            var sw = new StringWriter();
            RenderInlineToString(sw, inline);
            return sw.ToString();
        }

        private void RenderInlineToString(StringWriter sw, Inline inline)
        {
            if (inline is LiteralInline literal)
                sw.Write(literal.Content.ToString());

            else if (inline.GetType().Name == "SoftlineBreakInline")
                sw.Write(" ");

            else if (inline is HtmlInline html)
                sw.Write(html.Tag);

            else if (inline is ContainerInline container)
                foreach (var sub in container)
                    RenderInlineToString(sw, sub);
        }
    }
}
