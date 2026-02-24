using QuestPDF.Helpers;

namespace JobScraper.Web.Features.Cv;

public class LayoutConfig
{
    // general
    public string FontFamily { get; set; } = Fonts.Verdana;
    public float MarginCm { get; set; } = 0.8f;
    public string PageColor { get; set; } = Colors.White;
    public string UrlColor { get; set; } = Colors.Blue.Darken4;

    // image
    public float ImageWidth { get; set; } = 115;
    public float ImageHeight { get; set; } = 150;
    public float ImagePaddingLeft { get; set; } = 15;
    public float ImageBorderRadius { get; set; } = 15;
    public float ImageBorderThickness { get; set; } = 1;
    public string ImageBorderColor { get; set; } = Colors.Grey.Lighten2;
    public int TextBlocksAlignedToImage { get; set; } = 7;

    // font sizes & headers
    public float DefaultFontSize { get; set; } = 8.6f;
    public float H1FontSize { get; set; } = 15;
    public string H1FontColor { get; set; } = Colors.Indigo.Darken3;
    public float H2FontSize { get; set; } = 13;
    public float H3FontSize { get; set; } = 12;
    public float H4FontSize { get; set; } = 10;
    public float HeadingFontSizeDefault { get; set; } = 9;
    public string HeadingFontColorDefault { get; set; } = Colors.Black;

    // disclaimer
    public float DisclaimerFontSize { get; set; } = 7;
    public string DisclaimerColor { get; set; } = Colors.Grey.Darken3;
    public float FooterPaddingBottom { get; set; } = -20;

    // spaces
    public float HeaderPaddingTop { get; set; } = 0;
    public float LineHeight { get; set; } = 1.1f;
    public float HeadingPaddingTop { get; set; } = 5;
    public float HeadingPaddingBottom { get; set; } = 3;
    public float ParagraphPaddingBottom { get; set; } = 3;

    // lists
    public float ListPaddingLeft { get; set; } = 15;
    public string ListBulletText { get; set; } = "•";
    public float ListBulletWidth { get; set; } = 15;

    public float ThematicBreakPaddingVertical { get; set; } = 10;
    public float ThematicBreakThickness { get; set; } = 1;
    public string ThematicBreakColor { get; set; } = Colors.Orange.Lighten2;
}
