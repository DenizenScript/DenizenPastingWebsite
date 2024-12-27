using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticExtensions;
using LiteDB;
using System.Collections.Concurrent;
using System.Threading;

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
            ConcurrentQueue<(Paste, int)> results = [];
            const int jump = 501;
            Task task = null;
            long msFind = 0, msFill = 0, msFillString = 0, msContains = 0;
            for (long index = firstInd; index >= lastInd; index -= jump)
            {
                Paste[] pastes;
                lock (PasteDatabase.Internal.PasteLock)
                {
                    long tickFindStart = Environment.TickCount64;
                    pastes = PasteDatabase.Internal.PasteCollection.Find(Query.And(Query.GT("_id", Math.Max(lastInd, index - jump)), Query.LTE("_id", index))).ToArray();
                    long tickFindEnd = Environment.TickCount64;
                    foreach (Paste paste in pastes)
                    {
                        PasteDatabase.FillPasteFromStorage(paste, false);
                    }
                    long tickFillEnd = Environment.TickCount64;
                    msFind += tickFindEnd - tickFindStart;
                    msFill += tickFillEnd - tickFindEnd;
                }
                task?.Wait();
                task = Task.Run(() =>
                {
                    long tickFillStart = Environment.TickCount64;
                    Parallel.ForEach(pastes, paste =>
                    {
                        PasteDatabase.FillPasteStrings(paste, false);
                    });
                    long tickFillEnd = Environment.TickCount64;
                    Parallel.ForEach(pastes, paste =>
                    {
                        for (int i = 0; i < terms.Length; i++)
                        {
                            if (terms[i] == "$spam")
                            {
                                if (paste.HistoricalContent is not null)
                                {
                                    continue;
                                }
                                PasteUser user = PasteDatabase.GetUser(paste.PostSourceData);
                                if (PasteServer.GetSpamFlag(paste, user) is not null)
                                {
                                    Console.WriteLine($"Marked {paste.ID} as spam because {PasteServer.GetSpamFlag(paste, user)}");
                                    results.Enqueue((paste, i));
                                    break;
                                }
                            }
                            else if (paste.ContainsSearchText(terms[i]))
                            {
                                results.Enqueue((paste, i));
                                break;
                            }
                        }
                    });
                    long tickContainsEnd = Environment.TickCount64;
                    Interlocked.Add(ref msFillString, tickFillEnd - tickFillStart);
                    Interlocked.Add(ref msContains, tickContainsEnd - tickFillEnd);
                });
            }
            task?.Wait();
            (Paste, int)[] final = [.. results.OrderByDescending(p => p.Item1.ID)];
            Console.WriteLine($"Search found {final.Length} results, using {msFind}ms to find pastes, {msFill}ms to fill from dbstore, {msFillString}ms to fill strings, {msContains}ms to check containment.");
            return final;
        }
    }
}
