using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;

namespace DenizenPastingWebsite.Models
{
    public class NewPasteModel
    {
        /// <summary>The type of paste to be submitted.</summary>
        public string NewType = "script";

        /// <summary>The "other" type, if any.</summary>
        public string OtherType = null;

        /// <summary>A display name of the type to be submitted.</summary>
        public string NewTypeDisplay => OtherType is null ? NewType : OtherType;

        /// <summary>If true, a rejection message will show to the user.</summary>
        public bool ShowRejection = false;

        /// <summary>The existing paste that is being edited (if any).</summary>
        public Paste Edit = null;

        /// <summary>The ID of the existing paste that is being edited (if any).</summary>
        public string EditID => Edit is null ? "" : Edit.ID.ToString();

        /// <summary>The content to prefill into the title area (if any).</summary>
        public string PreFillTitle => Edit is null ? "" : $"Edit of paste {Edit.ID}: {Edit.Title}";

        /// <summary>The content to prefill into the text area (if any).</summary>
        public string PreFillContents => Edit is null ? "" : Edit.Raw;

        /// <summary>The sub-URL for the edit paste action.</summary>
        public string PasteURL => Edit is null ? $"/New/{NewType}" : $"/View/{Edit.ID}";

        /// <summary>The limit on paste character length.</summary>
        public int MaxLength => PasteServer.MaxPasteRawLength;

        /// <summary>HTMLString option list for other language selection.</summary>
        public HtmlString OtherLangOptions => PasteType.OtherLangOptions;

        /// <summary>Used to enable only the correct paste type radio button.</summary>
        public HtmlString Checked(string type)
        {
            return new HtmlString(NewType == type ? "Checked" : "");
        }
    }
}
