using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static Dictionary<string, char> MinecraftLogColorMap = new Dictionary<string, char>()
        {
            { "0;30;22", '0' },
            { "0;34;22", '1' },
            { "0;32;22", '2' },
            { "0;36;22", '3' },
            { "0;31;22", '4' },
            { "0;35;22", '5' },
            { "0;33;22", '6' },
            { "0;37;22", '7' },
            { "0;30;1", '8' },
            { "0;34;1", '9' },
            { "0;32;1", 'a' },
            { "0;36;1", 'b' },
            { "0;31;1", 'c' },
            { "0;35;1", 'd' },
            { "0;33;1", 'e' },
            { "0;37;1", 'f' },
            { "5", 'k' },
            { "21", 'l' },
            { "9", 'm' },
            { "4", 'n' },
            { "3", 'o' },
            { "", 'r' }
        };

        public const char ESCAPE_CHAR = (char)0x1b;
        public const char ALT_COLOR_CHAR = (char)0x01;

        public const string HEX_LETTERS = "0123456789abcdefABCDEF";

        public const string COLOR_RESET_CODES = HEX_LETTERS + "rRxX";

        public static AsciiMatcher HexMatcher = new AsciiMatcher(HEX_LETTERS);

        public static AsciiMatcher ColorSymbolMatcher = new AsciiMatcher(COLOR_RESET_CODES + "klmnoKLMNO");

        public static AsciiMatcher ResettersMatcher = new AsciiMatcher(COLOR_RESET_CODES);

        public static string StandardizeColoration(string text)
        {
            text = text.Replace('§', ALT_COLOR_CHAR);
            if (text.Contains(ESCAPE_CHAR))
            {
                StringBuilder patched = new StringBuilder(text.Length);
                string[] split = text.Split(ESCAPE_CHAR);
                for (int i = 0; i < split.Length; i++)
                {
                    int ind = split[i].IndexOf('m');
                    if (ind == -1 || !MinecraftLogColorMap.TryGetValue(split[i][1..ind], out char replaceChar))
                    {
                        patched.Append(ESCAPE_CHAR).Append(split[i]);
                        continue;
                    }
                    patched.Append(ALT_COLOR_CHAR).Append(replaceChar).Append(split[i][(ind + 1)..]);
                }
                text = patched.ToString();
            }
            return text;
        }

        public static bool TryGetHex(string text, out string hex)
        {
            hex = "";
            if (text.Length < 12)
            {
                return false;
            }
            Span<char> outHex = stackalloc char[6];
            for (int i = 0; i < 6; i++)
            {
                if (text[i * 2] != ALT_COLOR_CHAR)
                {
                    return false;
                }
                char symbol = text[i * 2 + 1];
                if (!HexMatcher.IsMatch(symbol))
                {
                    return false;
                }
                outHex[i] = symbol;
            }
            hex = new string(outHex);
            return true;
        }

        public static string ColorLog(string text)
        {
            text = StandardizeColoration(text);
            string[] lines = text.SplitFast('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int index = line.IndexOf(ALT_COLOR_CHAR);
                if (index != -1)
                {
                    StringBuilder output = new StringBuilder(line.Length * 2);
                    int spans = 0;
                    int lastIndex = 0;
                    while (index != -1)
                    {
                        if (index + 1 < line.Length && ColorSymbolMatcher.IsMatch(line[index + 1]))
                        {
                            output.Append(line[lastIndex..index]);
                            if (ResettersMatcher.IsMatch(line[index + 1]))
                            {
                                for (int s = 0; s < spans; s++)
                                {
                                    output.Append("</span>");
                                }
                                spans = 0;
                            }
                            string code = line[index + 1].ToString().ToLowerFast();
                            if (code == "x" && TryGetHex(line[(index + 2)..], out string hex))
                            {
                                index += 12;
                                output.Append($"<span style=\"color:#{hex};\">");
                            }
                            else
                            {
                                output.Append($"<span class=\"mc_{code}\">");
                            }
                            spans++;
                            lastIndex = index + 2;
                        }
                        index = line.IndexOf(ALT_COLOR_CHAR, index + 1);
                    }
                    if (lastIndex < line.Length)
                    {
                        output.Append(line[lastIndex..]);
                        for (int s = 0; s < spans; s++)
                        {
                            output.Append("</span>");
                        }
                    }
                    lines[i] = output.ToString();
                }
            }
            return $"<span class=\"mc_f\">{string.Join('\n', lines)}</span>";
        }
    }
}
