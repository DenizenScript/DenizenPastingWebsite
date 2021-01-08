using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite.Models
{
    public class NewPasteModel
    {
        /// <summary>
        /// The type of paste to be submitted.
        /// </summary>
        public string NewType = "Script";

        /// <summary>
        /// Used to enable only the correct paste type radio button.
        /// </summary>
        public HtmlString Checked(string type)
        {
            return new HtmlString(NewType == type ? "Checked" : "");
        }
    }
}
