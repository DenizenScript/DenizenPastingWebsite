using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DenizenPastingWebsite.Highlighters;
using FreneticUtilities.FreneticToolkit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;

namespace DenizenPastingWebsite
{
    public class Program
    {
        public static IHost CurrentHost;

        public static void Main(string[] args)
        {
            SpecialTools.Internationalize();
            CancellationTokenSource cancel = new();
            Task consoleThread = Task.Run(RunConsole, cancel.Token);
            CurrentHost = CreateHostBuilder(args).Build();
            CurrentHost.Run();
            cancel.Cancel();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
        }

        public static async void RunConsole()
        {
            while (true)
            {
                string line = await Console.In.ReadLineAsync();
                if (line == null)
                {
                    return;
                }
                string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (split.Length == 0)
                {
                    continue;
                }
                switch (split[0])
                {
                    case "remove_bot_post":
                        {
                            if (split.Length == 2 && long.TryParse(split[1], out long pasteId) && PasteDatabase.TryGetPaste(pasteId, out Paste paste))
                            {
                                paste.Type = "text";
                                paste.Title = "REMOVED BOT POST";
                                if (string.IsNullOrWhiteSpace(paste.HistoricalContent))
                                {
                                    paste.HistoricalContent = paste.Title + "\n\n" + paste.Raw;
                                }
                                paste.Raw = "Bot post removed from view.";
                                paste.Formatted = HighlighterCore.HighlightPlainText("Bot post removed from view.");
                                PasteDatabase.SubmitPaste(paste);
                                Console.WriteLine($"paste {pasteId} removed");
                            }
                            else
                            {
                                Console.WriteLine("remove_bot_post (ID HERE)");
                            }
                        }
                        break;
                    case "rerender_type":
                        {
                            if (split.Length == 2 && PasteType.ValidPasteTypes.TryGetValue(split[1], out PasteType type))
                            {
                                foreach (Paste paste in PasteDatabase.Internal.PasteCollection.Find(p => p.Type == type.Name))
                                {
                                    try
                                    {
                                        Console.WriteLine($"Rerender paste {paste.ID}...");
                                        PasteDatabase.FillPaste(paste);
                                        string origFormat = paste.Formatted;
                                        paste.Formatted = type.Highlight(paste.Raw);
                                        if (origFormat.TrimEnd() != paste.Formatted.TrimEnd())
                                        {
                                            Console.WriteLine($"Updating paste {paste.ID} (was {origFormat.Length} now {paste.Formatted.Length})...");
                                            PasteDatabase.SubmitPaste(paste);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Failed to rerender paste {paste.ID}: {ex}");
                                    }
                                }
                                Console.WriteLine("Rerender done.");
                            }
                            else
                            {
                                Console.WriteLine("rerender_type (TYPE HERE)");
                            }
                        }
                        break;
                    case "resubmit_all":
                        {
                            int count = 0;
                            long cap = PasteDatabase.GetTotalPasteCount();
                            for (long i = 0; i < cap; i++)
                            {
                                try
                                {
                                    if (PasteDatabase.TryGetPaste(i, out Paste paste))
                                    {
                                        if (count % 1000 == 0)
                                        {
                                            Console.WriteLine($"Resubmitted {count} pastes thus far...");
                                            PasteDatabase.Internal.DB.Checkpoint();
                                        }
                                        count++;
                                        PasteDatabase.SubmitPaste(paste);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to resubmit paste {i}: {ex}");
                                }
                            }
                            Console.WriteLine($"Resubmitting done, {count} total.");
                        }
                        break;
                    case "flush":
                        {
                            PasteDatabase.Internal.DB.Checkpoint();
                            Console.WriteLine("Flushed.");
                        }
                        break;
                    case "rebuild":
                        {
                            PasteDatabase.Internal.DB.Rebuild();
                            Console.WriteLine("Rebuild complete.");
                        }
                        break;
                    case "stop":
                        {
                            PasteDatabase.Shutdown();
                            CurrentHost.StopAsync().Wait();
                            return;
                        }
                    default:
                        Console.WriteLine("Unknown command. Use 'remove_bot_post', 'rerender_type', or 'stop'");
                        break;
                }
            }
        }
    }
}
