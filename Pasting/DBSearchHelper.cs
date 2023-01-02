using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticExtensions;
using DenizenPastingWebsite.Models;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;
using System.Drawing;
using Microsoft.AspNetCore.Http.Connections;

namespace DenizenPastingWebsite.Pasting
{
    public static class DBSearchHelper
    {
        public static long[] GetSearchResults(string term, long start, long max)
        {
            if (start < 0 || term.Length > 10_000)
            {
                return null;
            }
            string[] searches = term.Split("|||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(s => s.Length >= 3).ToArray();
            if (searches.Length == 0)
            {
                return null;
            }
            long firstInd = PasteDatabase.GetTotalPasteCount() - start;
            long lastInd = Math.Max(0, firstInd - max);
            if (firstInd < 0)
            {
                return new[] { -1L };
            }
            List<long> results = new();
            for (long index = firstInd; index >= lastInd; index--)
            {
                if (PasteDatabase.TryGetPaste(index, out Paste paste))
                {
                    if (paste.ContainsSearchText(term))
                    {
                        results.Add(index);
                        if (results.Count > 500)
                        {
                            return results.ToArray();
                        }
                    }
                }
            }
            return results.ToArray();
        }
    }
}
