using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;
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

        public HtmlString Content => new(Paste.Formatted);

        public string LengthText => $"{Paste.Raw.Length} characters across {Paste.Raw.CountCharacter('\n')} lines";
    }
}
