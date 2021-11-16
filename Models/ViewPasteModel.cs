using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite.Models
{
    public class ViewPasteModel
    {
        /// <summary>The paste being viewed.</summary>
        public Paste Paste;

        public PasteType Type => PasteType.ValidPasteTypes[Paste.Type];

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

        public HtmlString Content => new(Paste.Formatted);

        public string LengthText => $"{Paste.Raw.Length} characters across {Paste.Raw.CountCharacter('\n')} lines";
    }
}
