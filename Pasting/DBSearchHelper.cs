using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticExtensions;
using LiteDB;

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
            const int jump = 250;
            Task task = null;
            LockObject locker = new();
            for (long index = firstInd; index >= lastInd; index -= jump)
            {
                Paste[] pastes;
                lock (locker)
                {
                    pastes = PasteDatabase.Internal.PasteCollection.Find(Query.And(Query.GT("_id", index - jump), Query.LTE("_id", index))).ToArray();
                    foreach (Paste paste in pastes)
                    {
                        PasteDatabase.FillPasteFromStorage(paste, false);
                    }
                }
                task?.Wait();
                task = Task.Run(() =>
                {
                    Parallel.ForEach(pastes, paste =>
                    {
                        PasteDatabase.FillPasteStrings(paste, false);
                    });
                    foreach (Paste paste in pastes.OrderByDescending(p => p.ID))
                    {
                        for (int i = 0; i < terms.Length; i++)
                        {
                            if (paste.ContainsSearchText(terms[i]))
                            {
                                results.Add((paste, i));
                                if (results.Count > 500)
                                {
                                    return;
                                }
                                break;
                            }
                        }
                    }
                });
            }
            return [.. results];
        }
    }
}
