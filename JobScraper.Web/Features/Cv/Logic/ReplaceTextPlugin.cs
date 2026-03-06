using System.ComponentModel;
using System.Text;
using Microsoft.SemanticKernel;

namespace JobScraper.Web.Features.Cv.Logic;

#pragma warning disable SKEXP0110

public class ReplaceTextPlugin(string originalText)
{
    public string OriginalText { get; } = originalText;
    public string ModifiedText { get; private set; } = originalText;

    [KernelFunction("get_original_text")]
    [Description("Returns the original text content")]
    public string GetOriginalText() => OriginalText;

    [KernelFunction("get_modified_text")]
    [Description("Returns the modified text content")]
    public string GetModifiedText() => ModifiedText;


    [KernelFunction("replace_text")]
    [Description("Replaces a chunk of text of the provided content with new text." +
        " Returns information if the text was not found or if the replacement was successful.")]
    public string ReplaceText(
        [Description("The exact text chunk to find and replace")]
        string oldText,
        [Description("The new text to replace it with")]
        string newText)
    {
        if (string.IsNullOrEmpty(OriginalText) || string.IsNullOrEmpty(oldText))
            return "No text to replace.";

        if (!OriginalText.Contains(oldText))
            return "[WARN] The specified text was not found in the content. No changes made.";

        ModifiedText = OriginalText.Replace(oldText, newText);

        return "Success";
    }


    [KernelFunction("diff_text")]
    [Description("Compares two text contents and returns a line-by-line diff showing additions, removals, and unchanged lines")]
    public string DiffText()
    {
        if (string.IsNullOrEmpty(OriginalText) && string.IsNullOrEmpty(ModifiedText))
            return "Both texts are empty.";

        var originalLines = (OriginalText ?? "").Split('\n');
        var modifiedLines = (ModifiedText ?? "").Split('\n');

        var diff = new StringBuilder();
        diff.AppendLine("--- Original");
        diff.AppendLine("+++ Modified");
        diff.AppendLine();

        var maxLines = Math.Max(originalLines.Length, modifiedLines.Length);
        for (var i = 0; i < maxLines; i++)
        {
            var origLine = i < originalLines.Length ? originalLines[i].TrimEnd('\r') : null;
            var modLine = i  < modifiedLines.Length ? modifiedLines[i].TrimEnd('\r') : null;

            if (origLine == modLine)
                diff.AppendLine($"  {origLine}");
            else
            {
                if (origLine is not null)
                    diff.AppendLine($"- {origLine}");
                if (modLine is not null)
                    diff.AppendLine($"+ {modLine}");
            }
        }

        return diff.ToString();
    }
}
