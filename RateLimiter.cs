using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DenizenPastingWebsite
{
    /// <summary>
    /// Very simplistic ratelimiting helper.
    /// </summary>
    public class RateLimiter
    {
        public static ConcurrentDictionary<string, long> Users = new ConcurrentDictionary<string, long>();

        public static bool TryUser(string origin)
        {
            long count = Users.AddOrUpdate(origin, 1, (s, l) => l + 1);
            return count <= PasteServer.MaxPastesPerMinute;
        }

        static RateLimiter()
        {
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(60_000);
                    Users.Clear();
                }
            });
        }
    }
}
