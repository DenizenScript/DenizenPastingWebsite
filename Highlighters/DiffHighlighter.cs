using FreneticUtilities.FreneticExtensions;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>
    /// Helper class to highlight a git-style diff report.
    /// </summary>
    public static class DiffHighlighter
    {
        /// <summary>
        /// Highlights a git-style diff.
        /// </summary>
        public static string Highlight(string text)
        {
            text = HighlighterCore.EscapeForHTML(text);
            text = ColorDiff(text);
            return HighlighterCore.HandleLines(text);
        }

        public static string ColorDiff(string diff)
        {
            string[] lines = diff.SplitFast('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWithFast('+'))
                {
                    line = $"<span class=\"diff_added\">{line}</span>";
                }
                else if (line.StartsWithFast('-'))
                {
                    line = $"<span class=\"diff_removed\">{line}</span>";
                }
                else if (line.StartsWithFast('@'))
                {
                    line = $"<span class=\"diff_special\">{line}</span>";
                }
                lines[i] = line;
            }
            return string.Join('\n', lines);
        }

        public static string GenerateDiff(string oldText, string newText)
        {
            DiffPaneModel result = new InlineDiffBuilder(new Differ()).BuildDiffModel(oldText, newText);
            StringBuilder output = new StringBuilder(oldText.Length + newText.Length);
            foreach (var line in result.Lines)
            {
                string lineRaw = line.Text;
                output.Append(line.Type switch { ChangeType.Inserted => '+', ChangeType.Deleted => '-', ChangeType.Imaginary => '@', ChangeType.Modified => '+', ChangeType.Unchanged => ' ', _ => ' ' });
                output.Append(lineRaw).Append('\n');
            }
            return output.ToString();
        }
    }
}
