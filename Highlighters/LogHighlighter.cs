using FreneticUtilities.FreneticExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>
    /// Helper class to highlight BBCode.
    /// </summary>
    public static class LogHighlighter
    {
        /// <summary>
        /// Highlights BBCode.
        /// </summary>
        public static string Highlight(string text)
        {
            text = HighlighterCore.EscapeForHTML(text);
            text = ColorLog(text);
            return HighlighterCore.HandleLines(text);
        }

        public static string ColorLog(string text)
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
