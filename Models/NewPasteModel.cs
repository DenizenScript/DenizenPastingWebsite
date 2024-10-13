using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;
using System.Security.Cryptography;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;

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

        /// <summary>Gets a simple validation code (to help prevent automated spam).</summary>
        public static string GetValidationCode()
        {
            return GetValidationCode(DateTimeOffset.UtcNow.ToString($"yyyy-MM-dd"));
        }

        /// <summary>Gets a simple validation code (to help prevent automated spam) for any given validation string.</summary>
        public static string GetValidationCode(string text)
        {
            return Convert.ToHexString(SHA256.HashData($"dpaste:{text}".EncodeUTF8())[0..8]);
        }

        /// <summary>Checks if the given validation code looks correct.</summary>
        public static bool IsValidValidationCode(string text)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset yesterday = now.AddDays(-1);
            return text == GetValidationCode(now.ToString("yyyy-MM-dd")) || text == GetValidationCode(yesterday.ToString("yyyy-MM-dd"));
        }
    }
}
