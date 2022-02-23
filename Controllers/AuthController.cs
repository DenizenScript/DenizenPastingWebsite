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
                Console.Error.WriteLine("Refused Discord auth confirm: auth not enabled");
                return Redirect("/Error/Error404");
            }
            StringValues state = Request.Query["state"];
            StringValues codeVal = Request.Query["code"];
            if (!state.Any() || !codeVal.Any())
            {
                Console.Error.WriteLine("Refused Discord auth confirm: missing state or code");
                return Redirect("/Error/Error404");
            }
            string cookieState = Request.Cookies["discord_auth_state_key"];
            if (cookieState is null)
            {
                Console.Error.WriteLine("Refused Discord auth confirm: missing cookie state");
                return Redirect("/Error/Error404");
            }
            if (cookieState != state)
            {
                Console.Error.WriteLine("Refused Discord auth confirm: mismatched states");
                return Redirect("/Error/Error404");
            }
            if (!AuthHelper.CheckAndClearState(state))
            {
                Console.Error.WriteLine("Refused Discord auth confirm: state does not exist on server");
                return Redirect("/Error/Error404");
            }
            string code = codeVal[0];
            if (code.Length < 10 || code.Length > 50)
            {
                Console.Error.WriteLine("Refused Discord auth confirm: code looks invalid");
                return Redirect("/Error/Error404");
            }
            AuthHelper.TokenResults token = AuthHelper.RequestTokenFor(code);
            if (token is null)
            {
                Console.Error.WriteLine("Refused Discord auth confirm: can't get token");
                return Redirect("/Error/Error404");
            }
            if (token.AccessTok.Length < 10 || token.AccessTok.Length > 100 || token.RefreshTok.Length < 10 || token.RefreshTok.Length > 100)
            {
                Console.Error.WriteLine("Refused Discord auth confirm: token looks invalid");
                return Redirect("/Error/Error404");
            }
            if (token.ExpiresSeconds <= 0)
            {
                Console.Error.WriteLine("Refused Discord auth confirm: invalid expiration");
                return Redirect("/Error/Error404");
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
                Console.Error.WriteLine("Refused auth login: auth not enabled");
                return Redirect("/Error/Error404");
            }
            if (Request.Cookies["paste_session_token"] is not null)
            {
                Console.Error.WriteLine("Refused auth login: already logged in?");
                return Redirect("/");
            }
            string redirUrl = AuthHelper.GenerateAuthorizationURL(out string state);
            Response.Cookies.Append("discord_auth_state_key", state, new CookieOptions() { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.None, Secure = true, MaxAge = TimeSpan.FromHours(1) });
            return Redirect(redirUrl);
        }

        public IActionResult Logout()
        {
            if (!AuthHelper.Enabled)
            {
                Console.Error.WriteLine("Refused auth log out: auth not enabled");
                return Redirect("/Error/Error404");
            }
            if (Request.Method != "POST")
            {
                Console.Error.WriteLine("Refused auth log out: not POST");
                return Redirect("/Error/Error404");
            }
            string sessTok = Request.Cookies["paste_session_token"];
            if (string.IsNullOrWhiteSpace(sessTok))
            {
                Console.Error.WriteLine("Refused auth log out: no session");
                return Redirect("/Error/Error404");
            }
            AuthHelper.Logout(sessTok);
            Response.Cookies.Delete("discord_oauth_token");
            Response.Cookies.Delete("paste_session_token");
            Setup();
            return View("LogoutSuccess");
        }
    }
}
