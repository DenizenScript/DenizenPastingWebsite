using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Utilities;
using Microsoft.Extensions.Primitives;
using System.Web;

namespace DenizenPastingWebsite.Controllers
{
    public class AuthController : PasteController
    {
        public IActionResult DiscordAuthConfirm()
        {
            if (!AuthHelper.Enabled)
            {
                return Refuse("Refused Discord auth confirm: auth not enabled");
            }
            StringValues state = Request.Query["state"];
            StringValues codeVal = Request.Query["code"];
            if (!state.Any() || !codeVal.Any())
            {
                return Refuse("Refused Discord auth confirm: missing state or code");
            }
            string cookieState = Request.Cookies["discord_auth_state_key"];
            if (cookieState is null)
            {
                return Refuse("Refused Discord auth confirm: missing cookie state");
            }
            if (cookieState != state)
            {
                return Refuse("Refused Discord auth confirm: mismatched states");
            }
            if (!AuthHelper.CheckAndClearState(state))
            {
                return Refuse("Refused Discord auth confirm: state does not exist on server");
            }
            string code = codeVal[0];
            if (code.Length < 10 || code.Length > 50)
            {
                return Refuse("Refused Discord auth confirm: code looks invalid");
            }
            AuthHelper.TokenResults token = AuthHelper.RequestTokenFor(code);
            if (token is null)
            {
                return Refuse("Refused Discord auth confirm: can't get token");
            }
            if (token.AccessTok.Length < 10 || token.AccessTok.Length > 100 || token.RefreshTok.Length < 10 || token.RefreshTok.Length > 100)
            {
                return Refuse("Refused Discord auth confirm: token looks invalid");
            }
            if (token.ExpiresSeconds <= 0)
            {
                return Refuse("Refused Discord auth confirm: invalid expiration");
            }
            AuthHelper.UserGuildData user = AuthHelper.GetUserGuildData(token.AccessTok);
            if (user is null)
            {
                Console.Error.WriteLine("Refused Discord auth confirm: invalid user");
                Setup();
                return View("ErrorInvalidAuth");
            }
            if (!user.Roles.Any(r => AuthHelper.AdminRoleIDs.Contains(r)))
            {
                Console.Error.WriteLine("Refused Discord auth confirm: non-staff user");
                Setup();
                return View("ErrorBadAuth");
            }
            string session = AuthHelper.GenerateAuthenticationSession(user.ID, token.ExpiresSeconds, token.AccessTok, token.RefreshTok);
            TimeSpan maxAge = TimeSpan.FromSeconds(token.ExpiresSeconds + AuthHelper.InvalidateDelay);
            Response.Cookies.Append("paste_session_token", session, new() { MaxAge = maxAge, IsEssential = true, SameSite = SameSiteMode.Strict, Secure = true, HttpOnly = true });
            Setup();
            ViewData["auth_isloggedin"] = true;
            return View("LoginSuccess");
        }

        public IActionResult Login()
        {
            if (!AuthHelper.Enabled)
            {
                return Refuse("Refused auth login: auth not enabled");
            }
            if (Request.Cookies["paste_session_token"] is not null)
            {
                return Refuse("Refused auth login: already logged in?", "/");
            }
            string redirUrl = AuthHelper.GenerateAuthorizationURL(out string state);
            Response.Cookies.Append("discord_auth_state_key", state, new CookieOptions() { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.None, Secure = true, MaxAge = TimeSpan.FromHours(1) });
            return Redirect(redirUrl);
        }

        public IActionResult Logout()
        {
            if (!AuthHelper.Enabled)
            {
                return Refuse("Refused auth log out: auth not enabled");
            }
            if (Request.Method != "POST")
            {
                return Refuse("Refused auth log out: not POST");
            }
            string sessTok = Request.Cookies["paste_session_token"];
            if (string.IsNullOrWhiteSpace(sessTok))
            {
                return Refuse("Refused auth log out: no session");
            }
            AuthHelper.Logout(sessTok);
            Response.Cookies.Delete("discord_oauth_token");
            Response.Cookies.Delete("paste_session_token");
            Setup();
            return View("LogoutSuccess");
        }
    }
}
