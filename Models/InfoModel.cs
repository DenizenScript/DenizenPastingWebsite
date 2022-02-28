using System;
using DenizenPastingWebsite.Pasting;
using Microsoft.AspNetCore.Html;

namespace DenizenPastingWebsite.Models
{
    public class InfoModel
    {
        public HtmlString Contact => new(PasteServer.ContactInfo);

        public HtmlString Terms => new(PasteServer.TermsOfService);
    }
}
