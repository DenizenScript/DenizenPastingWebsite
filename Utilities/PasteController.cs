using DenizenPastingWebsite.Pasting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Net;

namespace DenizenPastingWebsite.Utilities
{
    public class PasteController : Controller
    {
        public static HashSet<string> IgnoredOrigins = ["127.0.0.1", "::1", "[::1]"];

        [NonAction]
        public (string, string) GetSenderAndOrigin()
        {
            IPAddress remoteAddress = Request.HttpContext.Connection.RemoteIpAddress;
            string realOrigin = remoteAddress.ToString();
            string sender = IgnoredOrigins.Contains(realOrigin) ? "" : $"Remote IP: {realOrigin}";
            if (Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwardHeader))
            {
                sender += ", X-Forwarded-For: " + string.Join(" / ", forwardHeader);
                if (PasteServer.TrustXForwardedFor && forwardHeader.Count > 0)
                {
                    realOrigin = string.Join(" / ", forwardHeader);
                }
            }
            if (Request.Headers.TryGetValue("REMOTE_ADDR", out StringValues remoteAddr))
            {
                sender += ", REMOTE_ADDR: " + string.Join(" / ", remoteAddr);
            }
            if (sender.StartsWith(", "))
            {
                sender = sender[(", ".Length)..];
            }
            if (sender.Length == 0)
            {
                sender = "Unknown";
            }
            return (sender, realOrigin);
        }

        [NonAction]
        public void LogRefusal(string reason)
        {
            (string sender, string realOrigin) = GetSenderAndOrigin();
            Console.Error.WriteLine($"Refuse page `{Request.Method}` call from sender=`{sender}`, realOrigin=`{realOrigin}`, path=`{Request.Path.Value}`, UA=`{string.Join(", ", Request.Headers.UserAgent)}`, because: {reason}");
        }

        [NonAction]
        public RedirectResult Refuse(string reason, string route="/Error/Error404")
        {
            LogRefusal(reason);
            return Redirect(route);
        }

        [NonAction]
        public void Setup()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            AuthHelper.HandleAuth(Request, Response, ViewData);
        }
    }
}
