using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DenizenPastingWebsite.Highlighters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DenizenPastingWebsite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CancellationTokenSource cancel = new CancellationTokenSource();
            Task consoleThread = Task.Run(RunConsole, cancel.Token);
            CreateHostBuilder(args).Build().Run();
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
                string[] split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (split[0])
                {
                    case "remove_bot_post":
                        if (split.Length == 2 && long.TryParse(split[1], out long pasteId) && PasteDatabase.TryGetPaste(pasteId, out Paste paste))
                        {
                            paste.Type = "text";
                            paste.Formatted = HighlighterCore.HighlightPlainText("Bot post removed from view.");
                            Console.WriteLine($"paste {pasteId} removed");
                        }
                        else
                        {
                            Console.WriteLine("remove_bot_post (ID HERE)");
                        }
                        break;
                    default:
                        Console.WriteLine("Unknown command.");
                        break;
                }
            }
        }
    }
}
