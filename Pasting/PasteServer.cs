using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DenizenPastingWebsite.Utilities;
using FreneticUtilities.FreneticDataSyntax;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;

namespace DenizenPastingWebsite.Pasting
{
    public static class PasteServer
    {
        /// <summary>The URL base for this paste server.</summary>
        public static string URL_BASE;

        public const string USER_AGENT = "DenizenPastingWebsite/1.0";

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

        /// <summary>A list of short keywords to automatically block when present in a new short paste.</summary>
        public static string[] SpamBlockShortKeywords;

        /// <summary>A list of titles to automatically block when set as a new paste title.</summary>
        public static string[] SpamBlockTitles;

        /// <summary>A list of titles to automatically block when contained within a new paste title.</summary>
        public static string[] SpamBlockPartialTitles;

        /// <summary>Contact information for the terms page.</summary>
        public static string ContactInfo = "Contact info unset!";

        /// <summary>Terms of Service information for the terms page.</summary>
        public static string TermsOfService = "Terms of Service info unset!";

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
            SpamBlockShortKeywords = (Config.GetStringList("spam-block-short-keyphrases") ?? new List<string>()).Select(s => s.ToLowerFast()).ToArray();
            SpamBlockTitles = (Config.GetStringList("spam-block-titles") ?? new List<string>()).Select(s => s.ToLowerFast()).ToArray();
            SpamBlockPartialTitles = (Config.GetStringList("spam-block-partial-titles") ?? new List<string>()).Select(s => s.ToLowerFast()).ToArray();
            ContactInfo = Config.GetString("tos_contact", ContactInfo);
            TermsOfService = Config.GetString("tos_text", TermsOfService);
            if (Config.HasKey("discord_oauth"))
            {
                AuthHelper.LoadConfig(Config.GetSection("discord_oauth"));
            }
            Console.WriteLine($"Loaded at URL-base {URL_BASE} with max length {MaxPasteRawLength} with ratelimit {MaxPastesPerMinute} and x-forwarded-for set {TrustXForwardedFor}");
        }

        /// <summary>Webclient used for webhooks.</summary>
        public static HttpClient ReusableWebClient = new();

        static PasteServer()
        {
            ReusableWebClient.DefaultRequestHeaders.UserAgent.ParseAdd(USER_AGENT);
        }

        /// <summary>Locker for running webhooks.</summary>
        public static LockObject WebhookLock = new();

        /// <summary>What text is allowed to be included in the sender of a paste when showing to a webhook.</summary>
        public static AsciiMatcher AllowedSenderText = new("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.:-[](),/= ");

        /// <summary>Helper to count number of submissions a specific sender has sent.</summary>
        public static Dictionary<string, List<long>> SubmissionCounter = new();

        /// <summary>Returns the count of how many pastes a specific sender has sent in the past 24 hours, adding 1 to the count for a new submission.</summary>
        public static int CountSubmitter(string submitter)
        {
            List<long> submissions = SubmissionCounter.GetOrCreate(submitter, () => new List<long>());
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long yesterday = now - (60 * 60 * 24);
            if (submissions.Count > 0 && submissions[0] < yesterday)
            {
                int skip = submissions.Count(d => d < yesterday);
                submissions.RemoveRange(0, skip);
            }
            submissions.Add(now);
            return submissions.Count;
        }

        /// <summary>Runs the new-paste webhooks, if needed.</summary>
        public static void RunNewPasteWebhook(Paste paste, PasteUser user)
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
                            string sender = AllowedSenderText.TrimToMatches(paste.PostSourceData);
                            if (sender.Length > 512)
                            {
                                sender = sender[..512];
                            }
                            string content = $"New **{PasteType.ValidPasteTypes[paste.Type].Name}** paste: {URL_BASE}/View/{paste.ID} sent by `{sender}` (`{CountSubmitter(sender)}` today)";
                            if (paste.Raw.Length < 1024 * 10 && paste.Raw.CountCharacter('\n') < 15 && (paste.Raw.Contains("http://") || paste.Raw.Contains("https://")))
                            {
                                content += "... 🚩 potential spam - paste contains URLs.";
                            }
                            else if (user.CurrentStatus == PasteUser.Status.POTENTIAL_SPAMMER)
                            {
                                content += "... 🚩 potential spam - user has previously had spam blocked.";
                            }
                            request.Content = new ByteArrayContent(StringConversionHelper.UTF8Encoding.GetBytes("{\"content\":\"" + content.Replace('\\', '/').Replace('"', '\'') + "\"}"));
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
