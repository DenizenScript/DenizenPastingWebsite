using FreneticUtilities.FreneticExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                    line = $"<div class=\"green\">{line}</div>";
                }
                else if (line.StartsWithFast('-'))
                {
                    line = $"<div class=\"red\">{line}</div>";
                }
                else if (line.StartsWithFast('@'))
                {
                    line = $"<div class=\"blue\">{line}</div>";
                }
                lines[i] = line;
            }
            return string.Join('\n', lines);
        }
    }
}
