using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>Helper class to highlight other languages.</summary>
    public class OtherLanguageHighlighter
    {
        /// <summary>Highlights a generic alternate language.</summary>
        public static string Highlight(string language, string text)
        {
            text = HighlighterCore.EscapeForHTML(text);
            text = $"<span id=\"js_higlight_codeblock\" class=\"language-{language}\">{text}</span>";
            return HighlighterCore.HandleLines(text);
        }
    }
}
