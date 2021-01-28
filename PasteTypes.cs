using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Highlighters;

namespace DenizenPastingWebsite
{
    /// <summary>
    /// Helper class for the different paste types available.
    /// </summary>
    public class PasteType
    {
        /// <summary>
        /// A map of all valid paste types (lowercased) to their paste type object.
        /// </summary>
        public static Dictionary<string, PasteType> ValidPasteTypes = new Dictionary<string, PasteType>();

        static PasteType()
        {
            ValidPasteTypes["script"] = new PasteType() { Name = "Script", DisplayName = "Denizen Script", FileExtension = "dsc", Highlight = ScriptHighlighter.Highlight };
            ValidPasteTypes["log"] = new PasteType() { Name = "Log", DisplayName = "Server Log", FileExtension = "log", Highlight = LogHighlighter.Highlight };
            ValidPasteTypes["diff"] = new PasteType() { Name = "Diff", DisplayName = "Diff Report", FileExtension = "diff", Highlight = DiffHighlighter.Highlight };
            ValidPasteTypes["bbcode"] = new PasteType() { Name = "BBCode", DisplayName = "BBCode", FileExtension = "txt", Highlight = BBCodeHighlighter.Highlight };
            ValidPasteTypes["text"] = new PasteType() { Name = "Text", DisplayName = "Plain Text", FileExtension = "txt", Highlight = HighlighterCore.HighlightPlainText };
        }

        public string Name;

        public string DisplayName;

        public Func<string, string> Highlight;

        public string FileExtension;
    }
}
