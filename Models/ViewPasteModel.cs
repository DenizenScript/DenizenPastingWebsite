using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;
using DenizenPastingWebsite.Highlighters;

namespace DenizenPastingWebsite.Models
{
    public class ViewPasteModel
    {
        /// <summary>The paste being viewed.</summary>
        public Paste Paste;

        public PasteType Type => PasteType.ValidPasteTypes[Paste.Type];

        /// <summary>True if the paste type is an 'other' type, or false if a core type.</summary>
        public bool IsOtherType => Type.Name.StartsWith("other-");

        public static AsciiMatcher CleanNameCharactersMatcher = new("abcdefghijklmnopqrstuvwxyz0123456789_");

        public static string LimitLength(string str, int len)
        {
            if (str.Length > len)
            {
                return str[0..len];
            }
            return str;
        }

        public string DownloadName => $"paste_{Paste.ID}_{LimitLength(CleanNameCharactersMatcher.TrimToMatches(Paste.Title.ToLowerFast().Replace(' ', '_')), 32)}.{Type.FileExtension}";

        public string RawLink => $"/View/{Paste.ID}.txt";

        public string PrivateInfoLink => $"/View/{Paste.ID}.priv.json";

        public HtmlString Content => new(Paste.Formatted);

        public string LengthText => $"{Paste.Raw.Length} characters across {Paste.Raw.CountCharacter('\n')} lines";

        public bool IsMarkedAsSpam => Paste.HistoricalContent is not null;

        public HtmlString RenderHistorical => new(HighlighterCore.HighlightPlainText(Paste.HistoricalContent));

        public static AsciiMatcher PreviewExcludeText = new("\n\r\t'`*~");

        public string Title => HighlighterCore.EscapeForHTML(Paste.Title);

        public string PreviewContent()
        {
            string[] split = Paste.Raw.SplitFast('\n', 5);
            string combined = split[0].Take(4).JoinString("  ");
            string cleaned = PreviewExcludeText.TrimToNonMatches(HighlighterCore.EscapeForHTML(combined.Replace('\r', '\n').Replace("\n", "  ")).Replace("\"", "&quot;").Replace("  ", "&nbsp;&nbsp;"));
            if (cleaned.Length > 200)
            {
                cleaned = cleaned[0..200] + "...";
            }
            return cleaned;
        }
    }
}
