using FreneticUtilities.FreneticDataSyntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite
{
    public static class PasteServer
    {
        /// <summary>
        /// The URL base for this paste server.
        /// </summary>
        public static string URL_BASE;

        /// <summary>
        /// Maximum raw length (in characters) of a paste.
        /// Defaults to 5 MiB.
        /// </summary>
        public static int MaxPasteRawLength = 5 * 1024 * 1024;

        /// <summary>
        /// Whether X-Forwarded-For headers are trustworthy.
        /// </summary>
        public static bool TrustXForwardedFor;

        /// <summary>
        /// The maximum number of pastes per user per minute.
        /// </summary>
        public static int MaxPastesPerMinute;

        /// <summary>
        /// Loads the paste config.
        /// </summary>
        public static void LoadConfig()
        {
            FDSSection Config = FDSUtility.ReadFile("config/config.fds");
            URL_BASE = Config.GetString("url-base");
            MaxPasteRawLength = Config.GetInt("max-paste-size").Value;
            TrustXForwardedFor = Config.GetBool("trust-x-forwarded-for").Value;
            MaxPastesPerMinute = Config.GetInt("max-pastes-per-minute").Value;
            Console.WriteLine($"Loaded at URL-base {URL_BASE} with max length {MaxPasteRawLength} with ratelimit {MaxPastesPerMinute} and x-forwarded-for set {TrustXForwardedFor}");
        }
    }
}
