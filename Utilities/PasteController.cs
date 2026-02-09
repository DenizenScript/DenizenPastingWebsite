using DenizenPastingWebsite.Pasting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DenizenPastingWebsite.Utilities
{
    public class PasteController : Controller
    {
        public static HashSet<string> IgnoredOrigins = ["127.0.0.1", "::1", "[::1]"];

        [NonAction]
        public string FixIP(string ip)
        {
            if (ip.StartsWith("::ffff:"))
            {
                return ip["::ffff:".Length..];
            }
            // Trim v6 to the first half block
            if (ip.Contains(':'))
            {
                string[] bits = ip.Split(':');
                if (bits.Length == 8)
                {
                    return $"{bits[0]}:{bits[1]}:{bits[2]}:{bits[3]}::0";
                }
            }
            return ip;
        }

        [NonAction]
        public (string, string) GetSenderAndOrigin()
        {
            IPAddress remoteAddress = Request.HttpContext.Connection.RemoteIpAddress;
            string realOrigin = FixIP(remoteAddress.ToString());
            string sender = IgnoredOrigins.Contains(realOrigin) || CheckExclusion(PasteServer.ExcludeForwardAddresses, realOrigin) ? "" : $"Remote IP: {realOrigin}";
            if (Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwardHeader))
            {
                string[] forwards = [.. forwardHeader.Select(FixIP).Where(f => !CheckExclusion(PasteServer.ExcludeForwardAddresses, f))];
                if (PasteServer.TrustXForwardedFor && forwards.Length > 0)
                {
                    sender += ", X-Forwarded-For: " + string.Join(" / ", forwards);
                    realOrigin = string.Join(" / ", forwards);
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
        public static bool CheckExclusion(string[] set, string realIp)
        {
            try
            {
                foreach (string compare in set)
                {
                    if (CheckContains(realIp, compare))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking IP exclusion for `{realIp}`: {ex}");
            }
            return false;
        }

        [NonAction]
        public static bool CheckContains(string realIp, string compare)
        {
            if (compare.Contains('/') && !realIp.Contains(':'))
            {
                return IsIpInRange(realIp, compare);
            }
            return realIp == compare;
        }

        [NonAction]
        public static bool IsIpInRange(string ipAddress, string cidrRange)
        {
            string[] parts = cidrRange.Split('/');
            string rangeIp = parts[0];
            int prefixLength = int.Parse(parts[1]);
            uint ipInt = IpToUInt(ipAddress);
            uint rangeIpInt = IpToUInt(rangeIp);
            uint mask = 0xFFFFFFFF << (32 - prefixLength);
            return (ipInt & mask) == (rangeIpInt & mask);
        }

        [NonAction]
        public static uint IpToUInt(string ipAddress)
        {
            IPAddress ip = IPAddress.Parse(ipAddress);
            byte[] bytes = ip.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToUInt32(bytes, 0);
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
