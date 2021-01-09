using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite
{
    public class Paste
    {
        /// <summary>
        /// The ID number of the paste.
        /// </summary>
        public long _id;

        /// <summary>
        /// The user-defined title string.
        /// </summary>
        public string Title;

        /// <summary>
        /// Any internal post source data (IP address or other data).
        /// </summary>
        public string PostSourceData;

        /// <summary>
        /// Post type eg "Script".
        /// </summary>
        public string Type;

        /// <summary>
        /// Date that the paste was made.
        /// </summary>
        public string Date;

        /// <summary>
        /// Raw post text.
        /// </summary>
        public string Raw;

        /// <summary>
        /// Formatted post text (colors and all applied).
        /// </summary>
        public string Formatted;
    }
}
