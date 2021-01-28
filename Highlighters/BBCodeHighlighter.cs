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

        public static AsciiMatcher HexMatcher = new AsciiMatcher("0123456789abcdefABCDEF");

        public static string ColorBBCode(string text)
        {
            StringBuilder built = new StringBuilder(text.Length);
            bool inBrackets = false;
            string bracketContents = "";
            int spans = 0;
            bool bold = false;
            bool italic = false;
            bool underline = false;
            bool strike = false;
            bool url = false;
            bool build_url = false;
            StringBuilder url_link = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                if (!inBrackets && text[i] == '[')
                {
                    inBrackets = true;
                }
                else if (inBrackets && text[i] == ']')
                {
                    string[] datum = bracketContents.Split(new char[] { '=' }, 2);
                    switch (datum[0])
                    {
                        case "b":
                        case "/b":
                            bold = !bold;
                            built.Append(bold ? "<b>" : "</b>");
                            break;
                        case "u":
                        case "/u":
                            underline = !underline;
                            built.Append(underline ? "<u>" : "</u>");
                            break;
                        case "s":
                        case "/s":
                            strike = !strike;
                            built.Append(strike ? "<s>" : "</s>");
                            break;
                        case "i":
                        case "/i":
                            italic = !italic;
                            built.Append(italic ? "<i>" : "</i>");
                            break;
                        case "color":
                            if (datum.Length == 2 && datum[1].Length == 6 && HexMatcher.IsOnlyMatches(datum[1]))
                            {
                                built.Append("<span style=\"color:#" + datum[1] + "\">");
                            }
                            else
                            {
                                built.Append("[color=" + datum[1] + "]");
                            }
                            spans++;
                            break;
                        case "/color":
                            if (spans > 0)
                            {
                                built.Append("</span>");
                                spans--;
                            }
                            break;
                        case "url":
                        case "/url":
                            url = !url;
                            if (datum.Length == 2)
                            {
                                if (datum[1].StartsWith("http://") || datum[1].StartsWith("https://") || datum[1].StartsWith("ftp://") || datum[1].StartsWith("/"))
                                {
                                    built.Append("<a href=\"" + datum[1] + "\">");
                                }
                                else
                                {
                                    url = !url;
                                }
                            }
                            else if (build_url)
                            {
                                string urllink = url_link.ToString();
                                if (urllink.StartsWith("http://") || urllink.StartsWith("https://") || urllink.StartsWith("ftp://"))
                                {
                                    built.Append("<a href=\"" + urllink + "\">").Append(urllink).Append("</a>");
                                }
                                else
                                {
                                    built.Append(urllink);
                                }
                                build_url = false;
                                url_link.Clear();
                            }
                            else if (!url)
                            {
                                built.Append("</a>");
                            }
                            else
                            {
                                build_url = true;
                            }
                            break;
                        default:
                            built.Append('[').Append(bracketContents).Append(']');
                            break;
                    }
                    inBrackets = false;
                    bracketContents = "";
                }
                else if (inBrackets)
                {
                    bracketContents += text[i];
                }
                else if (build_url)
                {
                    url_link.Append(text[i]);
                }
                else
                {
                    built.Append(text[i]);
                }
            }
            if (bold)
            {
                built.Append("</b>");
            }
            if (italic)
            {
                built.Append("</i>");
            }
            if (underline)
            {
                built.Append("</u>");
            }
            if (strike)
            {
                built.Append("</s>");
            }
            while (spans > 0)
            {
                built.Append("</span>");
                spans--;
            }
            if (url)
            {
                if (build_url)
                {
                    built.Append(url_link);
                }
                else
                {
                    built.Append(url_link).Append("</a>");
                }
            }
            return built.ToString();
        }
    }
}
