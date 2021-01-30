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
    /// Helper class to highlight a Denizen script.
    /// </summary>
    public static class ScriptHighlighter
    {
        /// <summary>
        /// Highlights a Denizen script.
        /// </summary>
        public static string Highlight(string text)
        {
            text = HighlighterCore.EscapeForHTML(text);
            text = ColorScript(text);
            return HighlighterCore.HandleLines(text);
        }

        public static string ColorScript(string text)
        {
            string[] lines = text.SplitFast('\n');
            string lastKey = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (trimmed.EndsWithFast(':') && !trimmed.StartsWithFast('-'))
                {
                    lastKey = trimmed[0..^1].ToLowerFast();
                }
                lines[i] = ColorLine(line, lastKey);
            }
            return string.Join('\n', lines);
        }

        public static AsciiMatcher CommentHeaderMatcher = new AsciiMatcher("|+=#_@/");

        public static HashSet<string> DefiniteNotScriptKeys = new HashSet<string>()
        { "interact scripts", "default constants", "data", "constants", "text", "lore", "aliases", "slots", "enchantments", "input" };

        /// <summary>
        /// Special helper characters to avoid HTML validity mixups.
        /// </summary>
        public const char CHAR_TAG_START = (char)0x01, CHAR_TAG_END = (char)0x02;

        public static HashSet<string> IfOperators = new HashSet<string>() { CHAR_TAG_START.ToString(), CHAR_TAG_END.ToString(), CHAR_TAG_START + "=", CHAR_TAG_END + "=", "==", "!=", "||", "&&", "(", ")" };

        public static string ColorLine(string line, string lastKey)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                return "";
            }
            string trimmedEnd = line.TrimEnd();
            if (trimmedEnd.Length != line.Length)
            {
                return ColorLine(trimmedEnd, lastKey) + "<span class=\"script_bad_space\">" + line[(trimmedEnd.Length)..] + "</span>";
            }
            int preSpaces = line.Length - trimmed.Length;
            if (trimmed.StartsWithFast('#'))
            {
                string afterComment = trimmed[1..].Trim();
                if (afterComment.Length > 0)
                {
                    if (CommentHeaderMatcher.IsMatch(afterComment[0]))
                    {
                        return $"<span class=\"script_comment_header\">{line}</span>";
                    }
                    if (afterComment.ToLowerFast().StartsWith("todo"))
                    {
                        return $"<span class=\"script_comment_todo\">{line}</span>";
                    }
                    if (afterComment[0] == '-')
                    {
                        return $"<span class=\"script_comment_code\">{line}</span>";
                    }
                }
                return $"<span class=\"script_comment_normal\">{line}</span>";
            }
            if (trimmed.StartsWithFast('-'))
            {
                StringBuilder result = new StringBuilder(line.Length * 2);
                result.Append("<span class=\"script_normal\">").Append(line[0..(preSpaces + 1)]).Append("</span>");
                if (DefiniteNotScriptKeys.Contains(lastKey))
                {
                    result.Append(ColorArgument(line[(preSpaces + 1)..], false));
                    return result.ToString();
                }
                bool appendColon = trimmed.EndsWithFast(':');
                if (appendColon)
                {
                    trimmed = trimmed[0..^1];
                }
                string afterDash = trimmed[1..];
                int commandEnd = afterDash.IndexOf(' ', 1) + 1;
                string commandText = commandEnd == 0 ? afterDash : afterDash[0..commandEnd];
                if (!afterDash.StartsWithFast(' '))
                {
                    result.Append("<span class=\"script_bad_space\">").Append(commandText).Append("</span>");
                    result.Append(ColorArgument(afterDash[commandText.Length..], false));
                }
                else
                {
                    if (commandText.Contains('\'') || commandText.Contains('"') || commandText.Contains('['))
                    {
                        result.Append(ColorArgument(afterDash[commandText.Length..], false));
                    }
                    else
                    {
                        result.Append($"<span class=\"script_command\">{commandText}</span>");
                        if (commandEnd > 0)
                        {
                            result.Append(ColorArgument(afterDash[commandText.Length..], true));
                        }
                    }
                }
                if (appendColon)
                {
                    result.Append("<span class=\"script_colon\">:</span>");
                }
                return result.ToString();
            }
            if (line.EndsWithFast(':'))
            {
                return $"<span class=\"script_key\">{line[0..^1]}</span><span class=\"script_colon\">:</span>";
            }
            int colonIndex = line.IndexOf(':');
            if (colonIndex != -1)
            {
                return $"<span class=\"script_key\">{line[0..colonIndex]}</span><span class=\"script_colon\">:</span>{ColorArgument(line[(colonIndex + 1)..], false)}";
            }
            return $"<span class=\"script_bad_space\">{line}</span>";
        }

        public static string ColorArgument(string arg, bool canQuote)
        {
            arg = arg.Replace("&lt;", CHAR_TAG_START.ToString()).Replace("&gt;", CHAR_TAG_END.ToString());
            StringBuilder output = new StringBuilder(arg.Length * 2);
            bool quoted = false;
            char quoteMode = 'x';
            int inTagCounter = 0;
            int tagStart = 0;
            string defaultColor = "normal";
            int lastColor = 0;
            bool hasTagEnd = CheckIfHasTagEnd(arg, false, 'x', canQuote);
            for (int i = 0; i < arg.Length; i++)
            {
                char c = arg[i];
                if (canQuote && (c == '"' || c == '\''))
                {
                    if (quoted && c == quoteMode)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..(i + 1)]}</span>");
                        lastColor = i + 1;
                        defaultColor = "normal";
                        quoted = false;
                    }
                    else if (!quoted)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..i]}</span>");
                        lastColor = i;
                        quoted = true;
                        defaultColor = c == '"' ? "quote_double" : "quote_single";
                        quoteMode = c;
                    }
                }
                else if (hasTagEnd && c == CHAR_TAG_START && i + 1 < arg.Length && arg[i + 1] != '-')
                {
                    inTagCounter++;
                    if (inTagCounter == 1)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..i]}</span>");
                        output.Append($"<span class=\"script_tag\">{CHAR_TAG_START}</span>");
                        lastColor = i + 1;
                        tagStart = i;
                        defaultColor = "tag";
                    }
                }
                else if (hasTagEnd && c == CHAR_TAG_END && inTagCounter > 0)
                {
                    inTagCounter--;
                    if (inTagCounter == 0)
                    {
                        output.Append(ColorTag(arg[(tagStart + 1)..i]));
                        output.Append($"<span class=\"script_tag\">{arg[i..(i + 1)]}</span>");
                        defaultColor = quoted ? (quoteMode == '"' ? "quote_double" : "quote_single") : "normal";
                        lastColor = i + 1;
                    }
                }
                else if (c == ' ' && ((!quoted && canQuote) || inTagCounter == 0))
                {
                    hasTagEnd = CheckIfHasTagEnd(arg[(i + 1)..], quoted, quoteMode, canQuote);
                    output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..i]}</span> ");
                    lastColor = i + 1;
                    if (!quoted)
                    {
                        inTagCounter = 0;
                        defaultColor = "normal";
                    }
                    int nextSpace = arg.IndexOf(' ', i + 1);
                    string nextArg = nextSpace == -1 ? arg[(i + 1)..] : arg[(i + 1)..nextSpace];
                    if (IfOperators.Contains(nextArg))
                    {
                        output.Append($"<span class=\"script_colon\">{arg[(i + 1)..(i + 1 + nextArg.Length)]}</span>");
                        i += nextArg.Length;
                        lastColor = i + 1;
                    }
                }
            }
            if (lastColor < arg.Length)
            {
                output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..]}</span>");
            }
            return output.ToString().Replace(CHAR_TAG_START.ToString(), "&lt;").Replace(CHAR_TAG_END.ToString(), "&gt;");
        }

        public static bool CheckIfHasTagEnd(string arg, bool quoted, char quoteMode, bool canQuote)
        {
            foreach (char c in arg)
            {
                if (canQuote && (c == '"' || c == '\''))
                {
                    if (quoted && c == quoteMode)
                    {
                        quoted = false;
                    }
                    else if (!quoted)
                    {
                        quoted = true;
                        quoteMode = c;
                    }
                }
                else if (c == CHAR_TAG_END)
                {
                    return true;
                }
                else if (c == ' ' && !quoted && canQuote)
                {
                    return false;
                }
            }
            return false;
        }

        public static string ColorTag(string tag)
        {
            StringBuilder output = new StringBuilder(tag.Length * 2);
            int inTagCounter = 0;
            int tagStart = 0;
            int inTagParamCounter = 0;
            string defaultColor = "tag";
            int lastColor = 0;
            for (int i = 0; i < tag.Length; i++)
            {
                char c = tag[i];
                if (c == CHAR_TAG_START)
                {
                    inTagCounter++;
                    if (inTagCounter == 1)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{tag[lastColor..i]}</span>");
                        output.Append($"<span class=\"script_tag\">{CHAR_TAG_START}</span>");
                        lastColor = i + 1;
                        defaultColor = "tag";
                        tagStart = i;
                    }
                }
                else if (c == CHAR_TAG_END && inTagCounter > 0)
                {
                    inTagCounter--;
                    if (inTagCounter == 0)
                    {
                        output.Append(ColorTag(tag[(tagStart + 1)..i]));
                        output.Append($"<span class=\"script_tag\">{tag[i..(i + 1)]}</span>");
                        defaultColor = inTagParamCounter > 0 ? "tag_param" : "tag";
                        lastColor = i + 1;
                    }
                }
                else if (c == '[' && inTagCounter == 0)
                {
                    inTagParamCounter++;
                    if (inTagParamCounter == 1)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{tag[lastColor..i]}</span>");
                        lastColor = i;
                        defaultColor = "tag_param";
                    }
                }
                else if (c == ']' && inTagCounter == 0)
                {
                    inTagParamCounter--;
                    if (inTagParamCounter == 0)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{tag[lastColor..(i + 1)]}</span>");
                        defaultColor = "tag";
                        lastColor = i + 1;
                    }
                }
                else if (c == '.' && inTagParamCounter == 0)
                {
                    output.Append($"<span class=\"script_{defaultColor}\">{tag[lastColor..i]}</span>");
                    lastColor = i + 1;
                    output.Append($"<span class=\"script_tag_dot\">{tag[i..(i + 1)]}</span>");
                }
            }
            if (lastColor < tag.Length)
            {
                output.Append($"<span class=\"script_{defaultColor}\">{tag[lastColor..]}</span>");
            }
            return output.ToString();
        }
    }
}
