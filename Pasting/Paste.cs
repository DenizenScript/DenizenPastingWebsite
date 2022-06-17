using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace DenizenPastingWebsite.Pasting
{
    public class Paste
    {
        /// <summary>The ID number of the paste.</summary>
        [BsonId]
        public long ID { get; set; }

        /// <summary>The user-defined title string.</summary>
        public string Title { get; set; }

        /// <summary>Any internal post source data (IP address or other data).</summary>
        public string PostSourceData { get; set; }

        /// <summary>Post type name, all lowercase, like "script".</summary>
        public string Type { get; set; }

        /// <summary>Date that the paste was made.</summary>
        public string Date { get; set; }

        /// <summary>Raw post text.</summary>
        public string Raw { get; set; }

        /// <summary>Formatted post text (colors and all applied).</summary>
        public string Formatted { get; set; }

        /// <summary>The ID of the paste that was edited to produce this paste (if any).</summary>
        public long Edited { get; set; }

        /// <summary>The ID of the paste that was generated as a diff report for a paste edit.</summary>
        public long DiffReport { get; set; }

        /// <summary>If true, the internal data of this paste is in file storage and must be retrieved from there.</summary>
        public bool IsInFileStore { get; set; }

        /// <summary>Some deleted pastes have a historical record of their content.</summary>
        public string HistoricalContent { get; set; }

        /// <summary>An internal JSON object of staff info to be loaded by javascript from an admin.</summary>
        public string StaffInfo { get; set; }

        /// <summary>If true, the internal data of this paste is compressed.</summary>
        public bool IsCompressed { get; set; }

        /// <summary>Raw text, stored with GZip compression.</summary>
        public byte[] StoredRaw { get; set; }

        /// <summary>Formatted text, stored with GZip compression.</summary>
        public byte[] StoredFormatted { get; set; }
    }
}
