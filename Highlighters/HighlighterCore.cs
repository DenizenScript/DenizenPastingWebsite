using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using DenizenPastingWebsite.Pasting;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>Helper class for all highlighter types.</summary>
    public static class HighlighterCore
    {
        /// <summary>A helper matcher for characters that need HTML escaping.</summary>
        public static AsciiMatcher NeedsEscapeMatcher = new("&<>");

        /// <summary>A helper matcher for characters that need general cleanup.</summary>
        public static AsciiMatcher NeedsCleanupMatcher = new("\0\t\r");

        /// <summary>Escapes some text to be safe to put into HTML.</summary>
        public static string EscapeForHTML(string text)
        {
            if (NeedsCleanupMatcher.ContainsAnyMatch(text))
            {
                text = text.Replace("\0", " ").Replace("\t", "    ").Replace("\r\n", "\n").Replace("\r", "");
            }
            if (NeedsEscapeMatcher.ContainsAnyMatch(text))
            {
                text = text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
            }
            return text;
        }

        /// <summary>Formats plain text.</summary>
        public static string HighlightPlainText(string text)
        {
            text = EscapeForHTML(text);
            return HandleLines(text);
        }

        /// <summary>The final stage of most highlighters, turns newlines into HTML newlines and generates a line number sidebar.</summary>
        public static string HandleLines(string text)
        {
            int lineCount = text.CountCharacter('\n') + 2;
            StringBuilder lineNumbers = new(lineCount * 40);
            for (int i = 1; i < lineCount; i++)
            {
                lineNumbers.Append($"<a id=\"{i}\" href=\"#{i}\">{i}</a>\n");
            }
            text = PatchForFilter(text);
            return $"<div class=\"line_numbers\"><pre><code>\n{lineNumbers}\n</code></pre></div>\n<div class=\"paste_body\"><pre><code>\n{text}\n</code></pre></div>\n";
        }

        /// <summary>Applies privacy-filter formatting as-needed to the paste format.</summary>
        public static string PatchForFilter(string text)
        {
            int filterIndex = text.IndexOf(PasteType.FilterChar);
            if (filterIndex == -1)
            {
                return text;
            }
            int start = 0;
            StringBuilder output = new(text.Length * 2);
            while (filterIndex != -1)
            {
                output.Append(text[start..filterIndex]);
                int endIndex = text.IndexOf(PasteType.FilterChar, filterIndex + 1);
                if (endIndex == -1 || endIndex > filterIndex + 20)
                {
                    return text;
                }
                string[] filterInfo = text[(filterIndex + 1)..endIndex].SplitFast('=');
                if (filterInfo.Length != 3)
                {
                    return text;
                }
                int index = int.Parse(filterInfo[0]);
                int length = int.Parse(filterInfo[1]);
                string reason = filterInfo[2];
                string filterHolder = new(' ', length);
                if (length > reason.Length)
                {
                    int halfLen = length / 2;
                    string prefix = new(' ', halfLen);
                    filterHolder = prefix + reason + prefix + (halfLen * 2 == length ? "" : " ");
                }
                output.Append($"<span class=\"filtered_block\" id=\"filtered_block_{index}\" title=\"This section hidden by privacy filter '{reason}'\">{filterHolder}</span>");
                start = endIndex + 1;
                filterIndex = text.IndexOf(PasteType.FilterChar, start);
            }
            output.Append(text[start..]);
            return output.ToString();
        }
    }
}
