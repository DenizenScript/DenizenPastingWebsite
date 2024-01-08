﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>Helper class to highlight a Denizen script.</summary>
    public static class ScriptHighlighter
    {
        /// <summary>Highlights a Denizen script.</summary>
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
                string trimmedStart = line.TrimStart();
                string trimmed = trimmedStart.TrimEnd();
                if (trimmed.EndsWithFast(':') && !trimmed.StartsWithFast('-'))
                {
                    lastKey = trimmed[0..^1].ToLowerFast();
                }
                if (trimmed.StartsWithFast('-') && !trimmed.EndsWithFast(':'))
                {
                    int spaces = line.Length - trimmedStart.Length;
                    while (i + 1 < lines.Length)
                    {
                        string line2 = lines[i + 1];
                        string trimmedStart2 = line2.TrimStart();
                        int spaces2 = line2.Length - trimmedStart2.Length;
                        string trimmed2 = trimmedStart2.TrimEnd();
                        if (spaces2 > spaces && !trimmedStart2.StartsWith("- "))
                        {
                            line += "\n" + line2;
                            lines[i] = null;
                            i++;
                            if (trimmed2.EndsWith(':'))
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                lines[i] = ColorLine(line, lastKey);
            }
            return lines.Where(i => i is not null).JoinString("\n");
        }

        public static AsciiMatcher CommentHeaderMatcher = new("|+=#_@/");

        public static HashSet<string> DefiniteNotScriptKeys = ["interact scripts", "default constants", "data", "constants", "text", "lore", "aliases", "slots", "enchantments", "input"];

        /// <summary>Special helper characters to avoid HTML validity mixups.</summary>
        public const char CHAR_TAG_START = (char)0x01, CHAR_TAG_END = (char)0x02;

        public static HashSet<string> IfOperators = [CHAR_TAG_START.ToString(), CHAR_TAG_END.ToString(), CHAR_TAG_START + "=", CHAR_TAG_END + "=", "==", "!=", "||", "&amp;&amp;", "(", ")", "or", "and", "not", "in", "contains", "!in", "!contains", "matches", "!matches"];

        public static HashSet<string> IfCommandLabels = ["cmd:if", "cmd:else", "cmd:while", "cmd:waituntil"];

        public static HashSet<string> DeffableCommandLabels = ["cmd:run", "cmd:runlater", "cmd:clickable", "cmd:bungeerun"];

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
                StringBuilder result = new(line.Length * 2);
                result.Append("<span class=\"script_normal\">").Append(line[0..(preSpaces + 1)]).Append("</span>");
                if (DefiniteNotScriptKeys.Contains(lastKey))
                {
                    result.Append(ColorArgument(line[(preSpaces + 1)..], false, "non-script"));
                    return result.ToString();
                }
                bool appendColon = trimmed.EndsWithFast(':');
                if (appendColon)
                {
                    trimmed = trimmed[0..^1];
                }
                string afterDash = trimmed[1..];
                if (afterDash.Length != 0)
                {
                    int commandEnd = afterDash.IndexOf(' ', 1);
                    string commandText = commandEnd == -1 ? afterDash : afterDash[0..commandEnd];
                    if (!afterDash.StartsWithFast(' '))
                    {
                        result.Append("<span class=\"script_bad_space\">").Append(commandText).Append("</span>");
                        result.Append(ColorArgument(afterDash[commandEnd..], false, "cmd:" + commandText.Trim()));
                    }
                    else
                    {
                        if (commandText.Contains('\'') || commandText.Contains('"') || commandText.Contains('['))
                        {
                            result.Append(ColorArgument(afterDash, false, "non-cmd"));
                        }
                        else
                        {
                            result.Append($"<span class=\"script_command\">{commandText}</span>");
                            if (commandEnd > 0)
                            {
                                result.Append(ColorArgument(afterDash[commandEnd..], true, "cmd:" + commandText.Trim()));
                            }
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
                string key = line[0..colonIndex];
                return $"<span class=\"script_key\">{key}</span><span class=\"script_colon\">:</span>{ColorArgument(line[(colonIndex + 1)..], false, "key:" + key)}";
            }
            return $"<span class=\"script_bad_space\">{line}</span>";
        }

        /// <summary>Symbols that are allowed as the first character of a tag.</summary>
        public static AsciiMatcher VALID_TAG_FIRST_CHAR = new(AsciiMatcher.BothCaseLetters + AsciiMatcher.Digits + "&_[");

        public static string ColorArgument(string arg, bool canQuote, string contextualLabel)
        {
            arg = arg.Replace("&lt;", CHAR_TAG_START.ToString()).Replace("&gt;", CHAR_TAG_END.ToString());
            StringBuilder output = new(arg.Length * 2);
            bool quoted = false;
            char quoteMode = 'x';
            int inTagCounter = 0;
            int tagStart = 0;
            string referenceDefault = contextualLabel == "key:definitions" ? "def_name" : "normal";
            string defaultColor = referenceDefault;
            int lastColor = 0;
            bool hasTagEnd = CheckIfHasTagEnd(arg, false, 'x', canQuote);
            int spaces = 0;
            for (int i = 0; i < arg.Length; i++)
            {
                char c = arg[i];
                if (canQuote && (c == '"' || c == '\''))
                {
                    if (quoted && c == quoteMode)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..(i + 1)]}</span>");
                        lastColor = i + 1;
                        defaultColor = referenceDefault;
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
                else if (hasTagEnd && c == CHAR_TAG_START && i + 1 < arg.Length && VALID_TAG_FIRST_CHAR.IsMatch(arg[i + 1]))
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
                        defaultColor = quoted ? (quoteMode == '"' ? "quote_double" : "quote_single") : referenceDefault;
                        lastColor = i + 1;
                    }
                }
                else if (inTagCounter == 0 && c == '|' && contextualLabel == "key:definitions")
                {
                    output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..i]}</span><span class=\"script_normal\">|</span>");
                    lastColor = i + 1;
                }
                else if (inTagCounter == 0 && c == ':' && DeffableCommandLabels.Contains(contextualLabel))
                {
                    string part = arg[lastColor..i];
                    if (part.StartsWith("def.") && !part.Contains('<') && !part.Contains(' '))
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">def.</span><span class=\"script_def_name\">{arg[(lastColor + "def.".Length)..i]}</span>");
                        lastColor = i;
                    }
                }
                else if (c == ' ' && !quoted && canQuote && inTagCounter == 0)
                {
                    hasTagEnd = CheckIfHasTagEnd(arg[(i + 1)..], quoted, quoteMode, canQuote);
                    output.Append($"<span class=\"script_{defaultColor}\">{arg[lastColor..i]}</span> ");
                    lastColor = i + 1;
                    if (!quoted)
                    {
                        inTagCounter = 0;
                        defaultColor = referenceDefault;
                        spaces++;
                    }
                    int nextSpace = arg.IndexOf(' ', i + 1);
                    string nextArg = nextSpace == -1 ? arg[(i + 1)..] : arg[(i + 1)..nextSpace];
                    if (!quoted && canQuote)
                    {
                        if (IfOperators.Contains(nextArg) && IfCommandLabels.Contains(contextualLabel))
                        {
                            output.Append($"<span class=\"script_colon\">{arg[(i + 1)..(i + 1 + nextArg.Length)]}</span>");
                            i += nextArg.Length;
                            lastColor = i + 1;
                        }
                        else if (nextArg.StartsWith("as:") && !nextArg.Contains('<') && (contextualLabel == "cmd:foreach" || contextualLabel == "cmd:repeat"))
                        {
                            output.Append($"<span class=\"script_normal\">as:</span><span class=\"script_def_name\">{arg[(i + 1 + "as:".Length)..(i + 1 + nextArg.Length)]}</span>");
                            i += nextArg.Length;
                            lastColor = i + 1;
                        }
                        else if (nextArg.StartsWith("key:") && !nextArg.Contains('<') && contextualLabel == "cmd:foreach")
                        {
                            output.Append($"<span class=\"script_normal\">key:</span><span class=\"script_def_name\">{arg[(i + 1 + "key:".Length)..(i + 1 + nextArg.Length)]}</span>");
                            i += nextArg.Length;
                            lastColor = i + 1;
                        }
                        else if (spaces == 1 && (contextualLabel == "cmd:define" || contextualLabel == "cmd:definemap"))
                        {
                            int colonIndex = nextArg.IndexOf(':');
                            if (colonIndex == -1)
                            {
                                colonIndex = nextArg.Length;
                            }
                            int tagMark = nextArg.IndexOf('<');
                            if (tagMark == -1 || tagMark > colonIndex)
                            {
                                output.Append($"<span class=\"script_def_name\">{arg[(i + 1)..(i + 1 + colonIndex)]}</span>");
                                i += colonIndex;
                                lastColor = i + 1;
                                char argStart = nextArg[0];
                                if (!quoted && canQuote && (argStart == '"' || argStart == '\''))
                                {
                                    quoted = true;
                                    defaultColor = argStart == '"' ? "quote_double" : "quote_single";
                                    quoteMode = argStart;
                                }
                            }
                        }
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
            int paramCount = 0;
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
                else if (c == '[')
                {
                    paramCount++;
                }
                else if (c == ']' && paramCount > 0) {
                    paramCount--;
                }
                else if (c == CHAR_TAG_END)
                {
                    return true;
                }
            }
            return false;
        }

        public static string ColorTag(string tag)
        {
            StringBuilder output = new(tag.Length * 2);
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
                        output.Append($"<span class=\"script_{defaultColor}\">{tag[lastColor..i]}</span><span class=\"script_tag_param_bracket\">[</span>");
                        lastColor = i + 1;
                        if (i == 0)
                        {
                            defaultColor = "def_name";
                        }
                        else
                        {
                            defaultColor = "tag_param";
                        }
                    }
                }
                else if (c == ']' && inTagCounter == 0)
                {
                    inTagParamCounter--;
                    if (inTagParamCounter == 0)
                    {
                        output.Append($"<span class=\"script_{defaultColor}\">{tag[lastColor..i]}</span><span class=\"script_tag_param_bracket\">]</span>");
                        defaultColor = "tag";
                        lastColor = i + 1;
                    }
                }
                else if ((c == '.' || c == '|') && inTagCounter == 0 && inTagParamCounter == 0)
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
