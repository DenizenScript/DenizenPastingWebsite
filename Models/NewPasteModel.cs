using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite.Models
{
    public class NewPasteModel
    {
        /// <summary>The type of paste to be submitted.</summary>
        public string NewType = "script";

        /// <summary>If true, a rejection message will show to the user.</summary>
        public bool ShowRejection = false;

        /// <summary>The existing paste that is being edited (if any).</summary>
        public Paste Edit = null;

        /// <summary>The content to prefill into the title area (if any).</summary>
        public string PreFillTitle => Edit == null ? "" : $"Edit of paste {Edit.ID}: {Edit.Title}";

        /// <summary>The content to prefill into the text area (if any).</summary>
        public string PreFillContents => Edit == null ? "" : Edit.Raw;

        /// <summary>The sub-URL for the edit paste action.</summary>
        public string PasteURL => Edit == null ? $"/New/{NewType}" : $"/View/{Edit.ID}";

        /// <summary>Used to enable only the correct paste type radio button.</summary>
        public HtmlString Checked(string type)
        {
            return new HtmlString(NewType == type ? "Checked" : "");
        }
    }
}
