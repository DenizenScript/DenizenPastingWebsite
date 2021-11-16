using FreneticUtilities.FreneticDataSyntax;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DenizenPastingWebsite
{
    public static class PasteServer
    {
        /// <summary>The URL base for this paste server.</summary>
        public static string URL_BASE;

        /// <summary>
        /// Maximum raw length (in characters) of a paste.
        /// Defaults to 5 MiB.
        /// </summary>
        public static int MaxPasteRawLength = 5 * 1024 * 1024;

        /// <summary>Whether X-Forwarded-For headers are trustworthy.</summary>
        public static bool TrustXForwardedFor;

        /// <summary>The maximum number of pastes per user per minute.</summary>
        public static int MaxPastesPerMinute;

        /// <summary>Webhook URLs to announce new pastes to, if any.</summary>
        public static string[] NewPasteWebhooks;

        /// <summary>A list of keywords to automatically block when present in a new paste.</summary>
        public static string[] SpamBlockKeywords;

        /// <summary>Loads the paste config.</summary>
        public static void LoadConfig()
        {
            FDSSection Config = FDSUtility.ReadFile("config/config.fds");
            URL_BASE = Config.GetString("url-base");
            MaxPasteRawLength = Config.GetInt("max-paste-size").Value;
            TrustXForwardedFor = Config.GetBool("trust-x-forwarded-for").Value;
            MaxPastesPerMinute = Config.GetInt("max-pastes-per-minute").Value;
            NewPasteWebhooks = (Config.GetStringList("webhooks.new-paste") ?? new List<string>()).ToArray();
            SpamBlockKeywords = (Config.GetStringList("spam-block-keyphrases") ?? new List<string>()).Select(s => s.ToLowerFast()).ToArray();
            Console.WriteLine($"Loaded at URL-base {URL_BASE} with max length {MaxPasteRawLength} with ratelimit {MaxPastesPerMinute} and x-forwarded-for set {TrustXForwardedFor}");
        }

        /// <summary>Webclient used for webhooks.</summary>
        public static HttpClient ReusableWebClient = new();

        /// <summary>Locker for running webhooks.</summary>
        public static LockObject WebhookLock = new();

        /// <summary>What text is allowed to be included in the sender of a paste when showing to a webhook.</summary>
        public static AsciiMatcher AllowedSenderText = new("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.:-[](),/= ");

        /// <summary>Runs the new-paste webhooks, if needed.</summary>
        public static void RunNewPasteWebhook(Paste paste)
        {
            if (NewPasteWebhooks.Length == 0)
            {
                return;
            }
            Task.Run(() =>
            {
                lock (WebhookLock)
                {
                    foreach (string hookURL in NewPasteWebhooks)
                    {
                        try
                        {
                            HttpRequestMessage request = new(HttpMethod.Post, hookURL);
                            request.Headers.UserAgent.ParseAdd("DenizenPastingWebsite/1.0");
                            string sender = AllowedSenderText.TrimToMatches(paste.PostSourceData);
                            if (sender.Length > 512)
                            {
                                sender = sender[..512];
                            }
                            string content = $"New **{PasteType.ValidPasteTypes[paste.Type].Name}** paste: {URL_BASE}/View/{paste.ID} sent by `{sender}`";
                            request.Content = new ByteArrayContent(StringConversionHelper.UTF8Encoding.GetBytes("{\"content\":\"" + content + "\"}"));
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                            ReusableWebClient.Send(request);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.Write($"Error while running webhook {hookURL} for paste {paste.ID}: {ex}");
                        }
                    }
                }
            });
        }
    }
}
