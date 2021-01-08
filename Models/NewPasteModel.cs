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
        /// If true, emphasize the option of changing to a different paste type.
        /// </summary>
        public bool RecommendChangeType = false;
    }
}
