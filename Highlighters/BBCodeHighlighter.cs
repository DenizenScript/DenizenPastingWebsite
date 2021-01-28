using FreneticUtilities.FreneticExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>
    /// Helper class to highlight a server log.
    /// </summary>
    public static class BBCodeHighlighter
    {
        /// <summary>
        /// Highlights a server log.
        /// </summary>
        public static string Highlight(string text)
        {
            text = HighlighterCore.EscapeForHTML(text);
            text = ColorBBCode(text);
            return HighlighterCore.HandleLines(text);
        }

        public static string ColorBBCode(string text)
        {
            string[] lines = text.SplitFast('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
#warning TODO
            }
            return string.Join('\n', lines);
        }
    }
}
