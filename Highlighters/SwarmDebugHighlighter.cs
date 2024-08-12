using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticExtensions;

namespace DenizenPastingWebsite.Highlighters
{
    /// <summary>Helper class to highlight a Swarm Debug log.</summary>
    public static class SwarmDebugHighlighter
    {
        /// <summary>Highlights a Swarm Debug log.</summary>
        public static string Highlight(string text)
        {
            text = HighlighterCore.EscapeForHTML(text);
            text = ColorSwarmLog(text);
            return HighlighterCore.HandleLines(text);
        }

        public static string ColorSwarmLog(string log)
        {
            string[] lines = log.SplitFast('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] parts = line.SplitFast(' ', 3);
                if (parts.Length == 4 && parts[2].StartsWithFast('[') && parts[2].EndsWithFast(']'))
                {
                    string color = "mc_7";
                    switch (parts[2])
                    {
                        case "[Verbose]": color = "mc_8"; break;
                        case "[Debug]": color = "mc_7"; break;
                        case "[Info]": color = "mc_3"; break;
                        case "[Init]": color = "mc_2"; break;
                        case "[Warning]": color = "mc_6"; break;
                        case "[Error]": color = "mc_4"; break;
                    }
                    line = $"{parts[0]} {parts[1]} [<span class=\"{color}\">{parts[2][1..^1]}</span>] {parts[3]}";
                }
                if (i % 2 == 1)
                {
                    line = $"<span class=\"lineback_alt\">{line}</span>";
                }
                lines[i] = line;
            }
            return string.Join('\n', lines);
        }
    }
}
