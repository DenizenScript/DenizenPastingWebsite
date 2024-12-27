using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using DenizenPastingWebsite.Pasting;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>Helper class to highlight BBCode.</summary>
    public static class LogHighlighter
    {
        /// <summary>Highlights BBCode.</summary>
        public static string Highlight(string text)
        {
            text = HighlighterCore.EscapeForHTML(text);
            text = ColorLog(text);
            return HighlighterCore.HandleLines(text);
        }

        public static (string, string[]) DoFilterMethod(string text, string[] filters)
        {
            if (filters is null || filters.IsEmpty())
            {
                return (text, null);
            }
            bool filterIPs = filters.Contains("playerip"), filterChat = filters.Contains("playerchat");
            if (!filterIPs && !filterChat)
            {
                return (text, null);
            }
            string[] lines = text.SplitFast('\n');
            List<string> filtered = [];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (filterIPs)
                {
                    line = FilterIPs(line, filtered);
                }
                if (filterChat)
                {
                    line = FilterChat(line, filtered);
                }
                lines[i] = line;

            }
            if (filtered.IsEmpty())
            {
                return (text, null);
            }
            return (string.Join('\n', lines), [.. filtered]);
        }

        /// <summary>Text that can appear in an IP string.</summary>
        public static AsciiMatcher IP_TEXT_MATCHER = new(AsciiMatcher.Digits + ".:");

        public static string FilterizeText(string line, int start, int end, string reason, List<string> output)
        {
            output.Add(line[start..end]);
            int index = output.Count;
            return string.Concat(line[..start], PasteType.FilterChar, index, '=', (end - start), '=', reason, PasteType.FilterChar, line[end..]);
        }

        public static string FilterIPs(string line, List<string> output)
        {
            if (line.Contains(PasteType.FilterChar))
            {
                return line;
            }
            // 'latest.log' as of 1.18.x: [01:01:01] [Server thread/INFO]: name[/123.123.123.123:1234] logged in with entity id 123 at ([world]1.0, 2.0, 3.0)
            // in-console as of 1.18.x: [01:01:01 INFO]: name[/123.123.123.123:1234] logged in with entity id 123 at ([world]1.0, 2.0, 3.0)
            int loggedIn = line.IndexOf("] logged in with entity id");
            if (loggedIn == -1)
            {
                return line;
            }
            int endName = line.IndexOf("[/");
            if (endName == -1 || endName > loggedIn)
            {
                return line;
            }
            endName += 2;
            if (loggedIn <= endName + 5)
            {
                return line;
            }
            if (!IP_TEXT_MATCHER.IsOnlyMatches(line[endName..loggedIn]))
            {
                return line;
            }
            return FilterizeText(line, endName, loggedIn, "ip", output);
        }

        public static AsciiMatcher TimePrefixMatcher = new(AsciiMatcher.Digits + ":");

        public static string FilterChat(string line, List<string> output)
        {
            if (line.Contains(PasteType.FilterChar))
            {
                return line;
            }
            // Denizen debug as of 1.18.x: 01:02:03 §e+> [BukkitWorldS...] §f§2CHAT: name: words <chat=123abc-123abc-123abc>
            const string debugMatchText = " §e+> [BukkitWorldS...] §f§2CHAT: ";
            int denizenDebugChat = line.IndexOf(debugMatchText);
            // Note: these matchers are intentionally a bit loose to compensate for possible format differences in some pastes
            if (denizenDebugChat != -1 && denizenDebugChat < 12)
            {
                return FilterizeText(line, denizenDebugChat + debugMatchText.Length, line.Length, "chat", output);
            }
            // 'latest.log' as of 1.18.x: [01:02:03] [Async Chat Thread - #1/INFO]: <name> words
            int asyncChat = line.IndexOf("[Async Chat Thread -");
            if (asyncChat != -1 && asyncChat < 15)
            {
                int endChatPrefix = line.IndexOf("]: ");
                if (endChatPrefix > asyncChat)
                {
                    return FilterizeText(line, endChatPrefix + 3, line.Length, "chat", output);
                }
            }
            // in-console as of 1.18.x: [15:07:27 INFO]: <name> words
            if (line.IndexOf(" INFO]: <") == 9 && line.StartsWith('[') && TimePrefixMatcher.IsOnlyMatches(line[1..9]))
            {
                return FilterizeText(line, 17, line.Length, "chat", output);
            }
            return line;
        }

        public static Dictionary<string, char> MinecraftLogColorMap = new()
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

        public static AsciiMatcher HexMatcher = new(HEX_LETTERS);

        public static AsciiMatcher ColorSymbolMatcher = new(COLOR_RESET_CODES + "klmnoKLMNO");

        public static AsciiMatcher ResettersMatcher = new(COLOR_RESET_CODES);

        public static string StandardizeColoration(string text)
        {
            text = text.Replace('§', ALT_COLOR_CHAR);
            if (text.Contains(ESCAPE_CHAR))
            {
                StringBuilder patched = new(text.Length);
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
                    StringBuilder output = new(line.Length * 2);
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
                else
                {
                    lines[i] = ColorizeGenericLine(line);
                }
            }
            return $"<span class=\"mc_f\">{string.Join('\n', lines)}</span>";
        }

        /// <summary>Character validator for the timestamp portion of a log.</summary>
        public static AsciiMatcher TimeStampValidator = new(AsciiMatcher.Digits + ":");

        /// <summary>Generates a unique ID for the text, within a given limit, using a highly simplified hash-like function.
        /// Doesn't use native hash function for the reason of ensuring long-term consistency.
        /// Returns a value between 0 (inclusive) and 'max' (exclusive).</summary>
        public static int ChooseIDFor(string text, int max)
        {
            int code = text.Length;
            int offset = 5;
            for (int i = 0; i < text.Length; i++)
            {
                code += text[i] ^ (offset);
                offset++;
            }
            return code % max;
        }

        /// <summary>Formats and colorizes a non-pre-colored line.</summary>
        public static string ColorizeGenericLine(string line)
        {
            if (!line.StartsWithFast('['))
            {
                if (line.Contains("Exception: ") || line.StartsWith("    at "))
                {
                    return $"<span class=\"log_error\">{line}</span>";
                }
                return line;
            }
            // Part 1: validate critical format and gather details
            int endBracket = line.IndexOf(']');
            if (endBracket == -1)
            {
                return line;
            }
            string timeStamp = line[1..endBracket];
            if (timeStamp.CountCharacter(':') != 2 || !TimeStampValidator.IsOnlyMatches(timeStamp) || timeStamp.Length > 20)
            {
                return line;
            }
            int nextBracket = line.IndexOf('[', endBracket);
            if (nextBracket == -1)
            {
                return line;
            }
            int nextEndBracket = line.IndexOf(']', nextBracket);
            if (nextEndBracket == -1 || nextEndBracket + 3 > line.Length || line[nextEndBracket + 1] != ':' || line[nextEndBracket + 2] != ' ' || nextEndBracket > nextEndBracket + 80)
            {
                return line;
            }
            string logModeParts = line[(nextBracket + 1)..nextEndBracket];
            int logModeSlash = logModeParts.IndexOf('/');
            if (logModeSlash == -1)
            {
                return line;
            }
            string threadName = logModeParts[0..logModeSlash];
            string logMode = logModeParts[(logModeSlash + 1)..];
            string text = line[(nextEndBracket + 3)..];
            // Part 2: For all modes other than "Server thread/INFO", just do a generic format with color determined by thread + mode
            if (threadName != "Server thread")
            {
                string threadSpan;
                if (threadName.StartsWith("Async Chat Thread"))
                {
                    threadSpan = "log_user_chat";
                }
                else if (threadName == "main" || threadName.StartsWith("Worker-Main-"))
                {
                    threadSpan = "log_thread_main";
                }
                else if (threadName.StartsWith("User Authenticator"))
                {
                    threadSpan = "log_thread_user_auth";
                }
                else if (threadName.StartsWith("Craft Scheduler") || threadName.StartsWith("Thread-"))
                {
                    threadSpan = "log_thread_async";
                }
                else if (logMode == "WARN" || logMode == "ERROR")
                {
                    threadSpan = "log_" + logMode.ToLowerFast();
                }
                else
                {
                    threadSpan = "log_autocolor_" + ChooseIDFor(logModeParts, 20);
                }
                return FormatCoreLine(timeStamp, threadName, logMode, threadSpan, threadSpan, text);
            }
            if (logMode != "INFO")
            {
                string spanMode;
                if (logMode == "WARN" || logMode == "ERROR")
                {
                    spanMode = "log_" + logMode.ToLowerFast();
                }
                else
                {
                    spanMode = "log_autocolor_" + ChooseIDFor(logModeParts, 20);
                }
                return FormatCoreLine(timeStamp, threadName, logMode, spanMode, spanMode, text);
            }
            // Part 3: Format INFO messages
            (string span, string body) = ColorizePluginMessage(text);
            if (span == null) { (span, body) = ColorizeUserMessage(text); }
            if (span == null) { (span, body) = ColorizeDenizenMessage(text); }
            if (span != null)
            {
                return FormatCoreLine(timeStamp, threadName, logMode, "log_info", span, body);
            }
            return FormatCoreLine(timeStamp, threadName, logMode, "log_info", "log_info", text);
        }

        /// <summary>Helper to verify the output doesn't lose spans.</summary>
        [Conditional("DEBUG")]
        public static void ValidateSpans(string text)
        {
            int spans = 0;
            int index = text.IndexOf("<span");
            while (index != -1)
            {
                spans++;
                index = text.IndexOf("<span", index + 2);
            }
            index = text.IndexOf("</span>");
            while (index != -1)
            {
                spans--;
                index = text.IndexOf("</span>", index + 2);
            }
            if (spans != 0)
            {
                Console.Error.WriteLine("Inconsistent spans " + spans + " for " + text);
            }
        }

        /// <summary>Converts core line format data to an actual HTML string.</summary>
        public static string FormatCoreLine(string timestamp, string thread, string logMode, string modeSpan, string spanFormat, string text)
        {
            //ValidateSpans(text);
            return $"<span class=\"log_timestamp\">[{timestamp}]</span> <span class=\"{modeSpan} log_modepart\">[{thread}/{logMode}]:</span> <span class=\"{spanFormat}\">{text}</span>";
        }

        /// <summary>Formats and colorizes a message from a plugin. Returns null if not a plugin message.</summary>
        public static (string, string) ColorizePluginMessage(string text)
        {
            if (!text.StartsWithFast('['))
            {
                return (null, null);
            }
            int endBracket = text.IndexOf(']');
            if (endBracket == -1 || endBracket > 100 || endBracket + 3 > text.Length || text[endBracket + 1] != ' ')
            {
                return (null, null);
            }
            string pluginName = text[1..endBracket];
            string message = text[(endBracket + 2)..];
            string span = "log_plugin_generic";
            int id = ChooseIDFor(pluginName, 20);
            if (message.StartsWith($"Loading {pluginName} v"))
            {
                span = "log_plugin_load";
            }
            else if (message.StartsWith($"Enabling {pluginName} v"))
            {
                span = "log_plugin_enable";
            }
            else if (message.StartsWith($"Disabling {pluginName} v"))
            {
                span = "log_plugin_disable";
            }
            string pluginID = "log_autocolor_" + id.ToString();
            if (SpecialPlugins.Contains(pluginName))
            {
                pluginID = "log_plugin_" + pluginName.ToLowerFast();
            }
            return (span, $"<span class=\"{pluginID}\">[{pluginName}]</span> {message}");
        }

        /// <summary>Special plugins for <see cref="ColorizePluginMessage(string)"/>.</summary>
        public static HashSet<string> SpecialPlugins = ["Denizen", "Citizens", "Sentinel"];

        /// <summary>Formats and colorizes a message from a user.</summary>
        public static (string, string) ColorizeUserMessage(string text)
        {
            string firstWord = text.BeforeAndAfter(' ', out string afterFirstWord);
            if (firstWord.StartsWith("&lt;") && firstWord.EndsWith("&gt;"))
            {
                return ("log_user_chat", FormatUserMessage(firstWord, " " + afterFirstWord));
            }
            else if (afterFirstWord == "joined the game" || afterFirstWord == "left the game" || afterFirstWord.StartsWith("lost connection: "))
            {
                return ("log_user_join", FormatUserMessage(firstWord, " " + afterFirstWord));
            }
            else if (afterFirstWord.StartsWith("logged in with entity id") && firstWord.Contains('['))
            {
                string user = firstWord.BeforeAndAfter('[', out string userData);
                return ("log_user_join", FormatUserMessage(user, $"[{userData} {afterFirstWord}"));
            }
            else if (afterFirstWord.StartsWith("issued server command:"))
            {
                return ("log_user_cmd", FormatUserMessage(firstWord, " " + afterFirstWord));
            }
            return (null, null);
        }

        public static string FormatUserMessage(string user, string message)
        {
            return $"<span class=\"log_autocolor_{ChooseIDFor(user, 20)}\">{user}</span>{message}";
        }

        /// <summary>Formats and colorizes a message from Denizen.</summary>
        public static (string, string) ColorizeDenizenMessage(string text)
        {
            if (text.StartsWith("+&gt; ") || text.StartsWith(" +&gt; "))
            {
                string preBody = text.StartsWithFast('+') ? "+&gt;" : " +&gt;";
                if (text.StartsWith($"{preBody} Executing '"))
                {
                    StringBuilder output = new(text.Length * 2);
                    bool mode = false;
                    string toScan = text[preBody.Length..];
                    for (int i = 0; i < toScan.Length; i++)
                    {
                        char c = toScan[i];
                        if (c == '\'')
                        {
                            if (!mode)
                            {
                                output.Append('\'');
                            }
                            output.Append($"</span><span class=\"{(mode ? "mc_8" : "mc_e")}\">");
                            mode = !mode;
                            if (!mode)
                            {
                                output.Append('\'');
                            }
                        }
                        else
                        {
                            output.Append(c);
                        }
                    }
                    return ("mc_e", $"{preBody}<span class=\"mc_8\">{output}</span>");
                }
                else if (text.StartsWith($"{preBody} ["))
                {
                    int endBracket = text.IndexOf(']');
                    if (endBracket != -1)
                    {
                        string clName = text[(preBody.Length + 2)..endBracket];
                        string messageBody = text[(endBracket + 1)..];
                        string span = "mc_8";
                        if (messageBody.StartsWith(" +---"))
                        {
                            span = "mc_5";
                        }
                        else if (messageBody.StartsWith(" Depenizen loaded"))
                        {
                            span = "mc_f";
                        }
                        else if (messageBody.StartsWith(" Warning:"))
                        {
                            span = "mc_c";
                        }
                        else
                        {
                            bool isFilledTag = messageBody.StartsWith(" Pre-Filled tag &lt;");
                            if (isFilledTag || messageBody.StartsWith(" Pre-Filled partial tag '&lt;"))
                            {
                                string baseText = isFilledTag ? " Pre-Filled tag &lt;" : " Pre-Filled partial tag '&lt;";
                                string endText = isFilledTag ? "&gt; with '" : "...' with '";
                                int endTag = messageBody.IndexOf(endText);
                                if (endTag != -1)
                                {
                                    int endWith = messageBody.IndexOf("', and ", endTag + endText.Length);
                                    if (endWith != -1)
                                    {
                                        string tag = messageBody[baseText.Length..endTag];
                                        string with = messageBody[(endTag + endText.Length)..endWith];
                                        string after = messageBody[endWith..];
                                        messageBody = $"{baseText}<span class=\"mc_f\">{tag}</span>{endText}<span class=\"mc_f\">{with}</span>{after}";
                                    }
                                }
                            }
                        }
                        return (span, $"<span class=\"mc_e\">{preBody} [{clName}]</span>{messageBody}");
                    }
                }
                return ("mc_e", text);
            }
            else if (text.StartsWith("+-"))
            {
                if (text.StartsWith("+- Queue '") && text.TrimEnd().EndsWith("-+"))
                {
                    int executeIndex = text.IndexOf("' Executing: (");
                    if (executeIndex != -1)
                    {
                        int endLine = text.IndexOf(") ", executeIndex);
                        if (endLine != -1)
                        {
                            string queueName = text["+- Queue '".Length..executeIndex];
                            string line = text[(executeIndex + "' Executing: (".Length)..endLine];
                            int lastSpace = text.TrimEnd().LastIndexOf(' ');
                            if (lastSpace > endLine)
                            {
                                string cmd = text[(endLine + ") ".Length)..lastSpace];
                                string endOfLine = text[lastSpace..];
                                string built = $"+- Queue '<span class=\"mc_7\">{queueName}</span>' Executing: <span class=\"mc_8\">({line})</span> <span class=\"mc_f\">{cmd}</span>{endOfLine}";
                                return ("mc_5", built);
                            }
                        }
                    }
                }
                return ("mc_5", text);
            }
            else if (text.StartsWith(" ERROR in queue"))
            {
                return ("log_error", text);
            }
            else if (text.StartsWith(" Filled tag &lt;"))
            {
                string afterOpen = text.After("&lt;");
                int withStart = afterOpen.IndexOf("&gt; with '");
                int endQuote = afterOpen.LastIndexOf('\'');
                if (withStart != -1 && endQuote > withStart + "&gt; with '".Length)
                {
                    string tag = afterOpen[0..withStart];
                    string with = afterOpen[(withStart + "&gt; with '".Length)..endQuote];
                    string endBlock = afterOpen[endQuote..];
                    return ("mc_8", $"Filled tag &lt;<span class=\"mc_2\">{tag}</span>&gt; with '<span class=\"mc_f\">{with}</span>{endBlock}");
                }
            }
            else if (text.StartsWith(" Starting InstantQueue '") || text.StartsWith(" Starting TimedQueue '") || text.StartsWith(" Completing queue '"))
            {
                return ("mc_6", text);
            }
            return (null, null);
        }
    }
}
