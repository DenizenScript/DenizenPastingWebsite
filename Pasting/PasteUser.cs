using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;

namespace DenizenPastingWebsite.Pasting
{
    public class PasteUser
    {
        [BsonId]
        public string SenderID { get; set; }

        public enum Status : int
        {
            NORMAL = 0,
            BLOCKED = 1,
            POTENTIAL_SPAMMER = 2,
            WHITELIST = 3
        }

        public Status CurrentStatus { get; set; }
    }
}
