using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticExtensions;
using DenizenPastingWebsite.Models;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;

namespace DenizenPastingWebsite.Pasting
{
    public static class DBSearchHelper
    {
        public static (Paste, int)[] GetSearchResults(string[] terms, long start, long max)
        {
            long firstInd = PasteDatabase.GetTotalPasteCount() - start;
            long lastInd = Math.Max(0, firstInd - max);
            if (firstInd < 0)
            {
                return [(null, -1)];
            }
            List<(Paste, int)> results = [];
            for (long index = firstInd; index >= lastInd; index--)
            {
                if (PasteDatabase.TryGetPaste(index, out Paste paste))
                {
                    for (int i = 0; i < terms.Length; i++)
                    {
                        if (paste.ContainsSearchText(terms[i]))
                        {
                            results.Add((paste, i));
                            if (results.Count > 500)
                            {
                                return [.. results];
                            }
                            break;
                        }
                    }
                }
            }
            return [.. results];
        }
    }
}
