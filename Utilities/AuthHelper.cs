using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticDataSyntax;
using System.Security.Cryptography;
using System.Runtime.Caching;
using System.Net.Http;
using System.Web;
using System.Net.Http.Headers;
using System.Text.Json;
using DenizenPastingWebsite.Pasting;

namespace DenizenPastingWebsite.Utilities
{
    public class AuthHelper
    {
        public static double HardReverifyDelay = TimeSpan.FromDays(3).TotalSeconds, SoftReverifyDelay = TimeSpan.FromHours(3).TotalSeconds,
            InvalidateDelay = TimeSpan.FromDays(2 * 31).TotalSeconds, RefreshEarlyBy = TimeSpan.FromDays(3).TotalSeconds;

        public static void HandleAuth(HttpRequest request, HttpResponse response, ViewDataDictionary viewData)
        {
            viewData["auth_canlogin"] = Enabled;
            if (!Enabled)
            {
                return;
            }
            viewData["auth_canlogin"] = true;
            if (DebugAlwaysOn)
            {
                viewData["auth_isloggedin"] = true;
                viewData["auth_userid"] = 0UL;
                return;
            }
            string sessTok = request.Cookies["paste_session_token"];
            if (sessTok is null)
            {
                viewData["auth_isloggedin"] = false;
                return;
            }
            UserDatabaseEntry user = PasteDatabase.Internal.UserCollection.FindById(sessTok);
            if (user is null)
            {
                Console.WriteLine("Ignored invalid session");
                viewData["auth_isloggedin"] = false;
                response.Cookies.Delete("paste_session_token");
                return;
            }
            if (user.RefreshTime - RefreshEarlyBy < CurrentTimestamp())
            {
                UserReverifyHelper locker = Locks[Math.Abs((int)(user.UserID % 32))];
                lock (locker)
                {
                    user = PasteDatabase.Internal.UserCollection.FindById(sessTok);
                    if (user is not null && user.RefreshTime - RefreshEarlyBy < CurrentTimestamp())
                    {
                        TokenResults token = RefreshToken(user.RefreshToken);
                        if (token is null)
                        {
                            viewData["auth_isloggedin"] = false;
                            PasteDatabase.Internal.UserCollection.Delete(sessTok);
                            response.Cookies.Delete("paste_session_token");
                            return;
                        }
                        Console.WriteLine("Token is valid for " + TimeSpan.FromSeconds(token.ExpiresSeconds).SimpleFormat(false));
                        user.RefreshTime = CurrentTimestamp() + token.ExpiresSeconds;
                        user.DiscordToken = token.AccessTok;
                        user.RefreshToken = token.RefreshTok;
                        TimeSpan maxAge = TimeSpan.FromSeconds(token.ExpiresSeconds + InvalidateDelay);
                        response.Cookies.Append("paste_session_token", sessTok, new() { MaxAge = maxAge, IsEssential = true, SameSite = SameSiteMode.Lax, HttpOnly = true });
                        PasteDatabase.Internal.UserCollection.Upsert(user);
                    }
                }
            }
            if (user.LastTimeVerified + HardReverifyDelay < CurrentTimestamp())
            {
                if (!Reverify(user))
                {
                    viewData["auth_isloggedin"] = false;
                    response.Cookies.Delete("paste_session_token");
                    return;
                }
            }
            else if (user.LastTimeVerified + SoftReverifyDelay < CurrentTimestamp())
            {
                Task.Factory.StartNew(() =>
                {
                    Reverify(user);
                });
            }
            viewData["auth_isloggedin"] = true;
            viewData["auth_userid"] = user.UserID;
        }

        public static long CurrentTimestamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public class UserReverifyHelper
        {
            public ulong LastReverified;

            public long ReverifiedTime;

            public bool Result;
        }

        public static UserReverifyHelper[] Locks = new UserReverifyHelper[32];

        static AuthHelper()
        {
            for (int i = 0; i < Locks.Length; i++)
            {
                Locks[i] = new UserReverifyHelper();
            }
        }

        public static bool Reverify(UserDatabaseEntry user)
        {
            UserReverifyHelper locker = Locks[Math.Abs((int)(user.UserID % 32))];
            lock (locker)
            {
                if (locker.LastReverified == user.UserID && locker.ReverifiedTime < CurrentTimestamp() + 5 * 60)
                {
                    return locker.Result;
                }
                locker.LastReverified = user.UserID;
                locker.ReverifiedTime = CurrentTimestamp();
                UserGuildData data = GetUserGuildData(user.DiscordToken);
                bool Fail(string reason)
                {
                    Console.Error.WriteLine($"Auth force-disable: {reason}");
                    locker.Result = false;
                    PasteDatabase.Internal.UserCollection.Delete(user.SessionToken);
                    return false;
                }
                if (data is null)
                {
                    return Fail("token no longer valid");
                }
                if (data.ID != user.UserID)
                {
                    return Fail("token points to different user");
                }
                if (!data.Roles.Any(r => AdminRoleIDs.Contains(r)))
                {
                    return Fail("user no longer staff");
                }
                user.LastTimeVerified = CurrentTimestamp();
                PasteDatabase.Internal.UserCollection.Upsert(user);
                locker.Result = true;
                return true;
            }
        }

        public static void LoadConfig(FDSSection section)
        {
            Enabled = section.GetBool("enabled", false).Value;
            if (!Enabled)
            {
                return;
            }
            DebugAlwaysOn = section.GetBool("debug_always_on", false).Value;
            ClientID = section.GetString("client-id");
            ClientSecret = section.GetString("client-secret");
            RedirectURL = HttpUtility.UrlEncode(section.GetString("redirect-url"));
            GuildID = section.GetUlong("guild-id").Value;
            AdminRoleIDs = [.. section.GetDataList("guild-roles-admin").Select(d => d.AsULong.Value)];
        }

        public static bool Enabled = false;

        public static bool DebugAlwaysOn = false;

        public const string DISCORD_OAUTH_BASE = "https://discord.com/api/oauth2",
            DISCORD_API_BASE = "https://discord.com/api/v8";

        public static string ClientID;

        public static string ClientSecret;

        public static string RedirectURL;

        public static ulong GuildID;

        public static HashSet<ulong> AdminRoleIDs;

        public static string GenerateRandomHexString(int byteLen)
        {
            byte[] val = RandomNumberGenerator.GetBytes(byteLen);
            return BitConverter.ToString(val).Replace("-", "");
        }

        public class UserDatabaseEntry
        {
            [LiteDB.BsonId]
            public string SessionToken { get; set; }

            public ulong UserID { get; set; }

            public long LastTimeVerified { get; set; }

            public long RefreshTime { get; set; }

            public string DiscordToken { get; set; }

            public string RefreshToken { get; set; }
        }

        public static void Logout(string sessTok)
        {
            PasteDatabase.Internal.UserCollection.Delete(sessTok);
        }

        public static string GenerateAuthenticationSession(ulong userId, long expireSeconds, string discTok, string refTok)
        {
            UserDatabaseEntry user = new()
            {
                SessionToken = GenerateRandomHexString(32),
                UserID = userId,
                LastTimeVerified = CurrentTimestamp(),
                RefreshTime = CurrentTimestamp() + expireSeconds,
                DiscordToken = discTok,
                RefreshToken = refTok
            };
            PasteDatabase.Internal.UserCollection.Upsert(user);
            return user.SessionToken;
        }

        public class UserGuildData
        {
            public ulong ID;

            public ulong[] Roles;
        }

        public static UserGuildData GetUserGuildData(string token)
        {
            Console.WriteLine("Doing an auth user data check");
            HttpRequestMessage request = new(HttpMethod.Get, $"{DISCORD_API_BASE}/users/@me/guilds/{GuildID}/member");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                HttpResponseMessage response = PasteServer.ReusableWebClient.Send(request);
                response.EnsureSuccessStatusCode();
                string body = response.Content.ReadAsStringAsync().Result;
                JsonElement result = JsonSerializer.Deserialize<JsonElement>(body);
                return new UserGuildData()
                {
                    ID = ulong.Parse(result.GetProperty("user").GetProperty("id").GetString()),
                    Roles = [.. result.GetProperty("roles").EnumerateArray().Select(e => ulong.Parse(e.GetString()))]
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UserID request failed: {ex}");
                return null;
            }
        }

        public class TokenResults
        {
            public string AccessTok;

            public long ExpiresSeconds;

            public string RefreshTok;
        }

        public static TokenResults DoTokenPost(string content)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{DISCORD_OAUTH_BASE}/token")
            {
                Content = new ByteArrayContent(StringConversionHelper.UTF8Encoding.GetBytes(content))
            };
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");
            try
            {
                HttpResponseMessage response = PasteServer.ReusableWebClient.Send(request);
                response.EnsureSuccessStatusCode();
                string body = response.Content.ReadAsStringAsync().Result;
                JsonElement result = JsonSerializer.Deserialize<JsonElement>(body);
                return new TokenResults()
                {
                    AccessTok = result.GetProperty("access_token").GetString(),
                    ExpiresSeconds = result.GetProperty("expires_in").GetInt64(),
                    RefreshTok = result.GetProperty("refresh_token").GetString()
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Token request failed: {ex}");
                return null;
            }
        }

        public static TokenResults RefreshToken(string refreshTok)
        {
            Console.WriteLine("Doing an auth token refresh");
            return DoTokenPost($"client_id={ClientID}&client_secret={ClientSecret}&grant_type=refresh_token&refresh_token={HttpUtility.UrlEncode(refreshTok)}&redirect_uri={RedirectURL}");
        }

        public static TokenResults RequestTokenFor(string code)
        {
            Console.WriteLine("Doing an auth token request");
            return DoTokenPost($"client_id={ClientID}&client_secret={ClientSecret}&grant_type=authorization_code&code={HttpUtility.UrlEncode(code)}&redirect_uri={RedirectURL}");
        }

        public static bool CheckAndClearState(string state)
        {
            if (state.Length != 32)
            {
                return false;
            }
            return (MemoryCache.Default.Remove($"auth_state_{state}") as string) == "active";
        }

        public static string GenerateAuthorizationURL(out string state)
        {
            state = GenerateRandomHexString(16);
            MemoryCache.Default.Add($"auth_state_{state}", "active", new CacheItemPolicy() { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(15) });
            return $"{DISCORD_OAUTH_BASE}/authorize?response_type=code&client_id={ClientID}&scope=identify%20guilds%20guilds.members.read&state={state}&redirect_uri={RedirectURL}&prompt=consent";
        }
    }
}
