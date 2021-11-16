using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticExtensions;
using DenizenPastingWebsite.Models;
using DenizenPastingWebsite.Highlighters;
using System.Net;

namespace DenizenPastingWebsite.Controllers
{
    [RequestFormLimits(ValueLengthLimit = 1024 * 1024 * 30)]
    public class NewController : Controller
    {
        public NewController()
        {
        }

        public static IActionResult RejectPaste(NewController controller, string type)
        {
            return controller.View("Index", new NewPasteModel() { ShowRejection = true, NewType = type });
        }

        public static HashSet<string> IgnoredOrigins = new()
        {
            "127.0.0.1", "::1", "[::1]"
        };

        public static IActionResult HandlePost(NewController controller, string type, Paste edits = null)
        {
            if (controller.Request.Method != "POST" || controller.Request.Form.IsEmpty())
            {
                Console.Error.WriteLine("Refused paste: Non-Post");
                return RejectPaste(controller, type);
            }
            IPAddress remoteAddress = controller.Request.HttpContext.Connection.RemoteIpAddress;
            string realOrigin = remoteAddress.ToString();
            string sender = IgnoredOrigins.Contains(realOrigin) ? "" : $"Remote IP: {realOrigin}";
            if (controller.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwardHeader))
            {
                sender += ", X-Forwarded-For: " + string.Join(" / ", forwardHeader);
                if (PasteServer.TrustXForwardedFor && forwardHeader.Count > 0)
                {
                    realOrigin = string.Join(" / ", forwardHeader);
                }
            }
            if (controller.Request.Headers.TryGetValue("REMOTE_ADDR", out StringValues remoteAddr))
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
            Console.WriteLine($"Attempted paste from {realOrigin} as {sender}");
            IFormCollection form = controller.Request.Form;
            if (!form.TryGetValue("pastetype", out StringValues pasteType) || !form.TryGetValue("pastetitle", out StringValues pasteTitle) || !form.TryGetValue("pastecontents", out StringValues pasteContents))
            {
                Console.Error.WriteLine("Refused paste: Form missing keys");
                return RejectPaste(controller, type);
            }
            if (pasteType.Count != 1 || pasteTitle.Count != 1 || pasteContents.Count != 1)
            {
                Console.Error.WriteLine("Refused paste: Improper form data");
                return RejectPaste(controller, type);
            }
            bool micro = form.TryGetValue("response", out StringValues responseValue) && responseValue.Count == 1 && responseValue[0].ToLowerFast() == "micro";
            bool microv2 = form.TryGetValue("v", out StringValues versionValue) && versionValue.Count == 1 && versionValue[0].ToLowerFast() == "200";
            if (micro)
            {
                sender += ", response=micro";
                if (microv2)
                {
                    sender += "v2";
                }
            }
            string pasteTypeName = pasteType[0].ToLowerFast();
            if (!PasteType.ValidPasteTypes.TryGetValue(pasteTypeName, out PasteType actualType))
            {
                Console.Error.WriteLine("Refused paste: Unknown type");
                return RejectPaste(controller, type);
            }
            string pasteTitleText = pasteTitle[0];
            if (string.IsNullOrWhiteSpace(pasteTitleText))
            {
                pasteTitleText = $"Unnamed {actualType.DisplayName} Paste";
            }
            if (pasteTitleText.Length > 200)
            {
                pasteTitleText = pasteTitleText[0..200];
            }
            string pasteContentText = pasteContents[0];
            if (pasteContentText.Length > PasteServer.MaxPasteRawLength)
            {
                pasteContentText = pasteContentText[0..PasteServer.MaxPasteRawLength];
            }
            pasteContentText = pasteContentText.Replace('\0', ' ');
            if (!IsValidPaste(pasteTitleText, pasteContentText))
            {
                return RejectPaste(controller, type);
            }
            if (!RateLimiter.TryUser(realOrigin))
            {
                Console.Error.WriteLine("Refused paste: spam");
                return RejectPaste(controller, type);
            }
            Paste newPaste = new()
            {
                Title = pasteTitleText,
                Type = actualType.Name.ToLowerFast(),
                PostSourceData = sender,
                Date = StringConversionHelper.DateTimeToString(DateTimeOffset.Now, false),
                Raw = pasteContentText,
                Formatted = actualType.Highlight(pasteContentText),
                Edited = (edits == null ? 0 : edits.ID)
            };
            if (newPaste.Formatted.Length > PasteServer.MaxPasteRawLength * 5)
            {
                Console.Error.WriteLine("Refused paste: Massive formatted-content length");
                return RejectPaste(controller, type);
            }
            if (newPaste.Formatted == null)
            {
                Console.Error.WriteLine("Refused paste: format failed");
                return RejectPaste(controller, type);
            }
            string diffText = null;
            if (edits != null)
            {
                diffText = DiffHighlighter.GenerateDiff(edits.Raw, newPaste.Raw, out bool hasDifferences);
                if (!hasDifferences)
                {
                    Console.Error.WriteLine("Refused paste: edits nothing");
                    return RejectPaste(controller, type);
                }
            }
            newPaste.ID = PasteDatabase.GetNextPasteID();
            if (diffText != null)
            {
                Paste diffPaste = new()
                {
                    Title = $"Diff Report Between Paste #{newPaste.ID} and #{edits.ID}",
                    Type = "diff",
                    PostSourceData = "(GENERATED), " + sender,
                    Date = StringConversionHelper.DateTimeToString(DateTimeOffset.Now, false),
                    Raw = diffText,
                    Formatted = DiffHighlighter.Highlight(diffText),
                    Edited = newPaste.ID,
                    ID = PasteDatabase.GetNextPasteID()
                };
                newPaste.DiffReport = diffPaste.ID;
                PasteDatabase.SubmitPaste(diffPaste);
                PasteServer.RunNewPasteWebhook(diffPaste);
            }
            PasteDatabase.SubmitPaste(newPaste);
            PasteServer.RunNewPasteWebhook(newPaste);
            Console.Error.WriteLine($"Accepted new paste: {newPaste.ID} from {newPaste.PostSourceData}");
            if (micro)
            {
                controller.Response.ContentType = "text/plain";
                return controller.Ok(microv2 ? $"{PasteServer.URL_BASE}/View/{newPaste.ID}\n" : $"/paste/{newPaste.ID}\n");
            }
            return controller.Redirect($"/View/{newPaste.ID}");
        }

        public static bool IsValidPaste(string title, string content)
        {
            if (content.Length < 100)
            {
                Console.Error.WriteLine($"Refused paste: too-short content {content.Length}");
                return false;
            }
            if (content.Length < 1024)
            {
                int lines = content.SplitFast('\n').Select(s => s.Trim().Length > 5).Count();
                if (lines < 3)
                {
                    Console.Error.WriteLine($"Refused paste: too-few lines {lines} (c.Len = {content.Length})");
                    return false;
                }
            }
            string titleLow = title.ToLowerFast();
            if (titleLow.Contains("<a href=") || titleLow.Contains("viagra") || titleLow.Contains("cialis"))
            {
                Console.Error.WriteLine("Refused paste: spam-bot title");
                return false;
            }
            if (PasteServer.SpamBlockKeywords.Length > 0)
            {
                String contentLow = content.ToLowerFast();
                foreach (string block in PasteServer.SpamBlockKeywords)
                {
                    if (titleLow.Contains(block))
                    {
                        Console.Error.WriteLine("Refused paste: spam-block-keyphrase in title");
                        return false;
                    }
                    if (contentLow.Contains(block))
                    {
                        Console.Error.WriteLine("Refused paste: spam-block-keyphrase in paste content");
                        return false;
                    }
                }
            }
            if (content.Length < 100 * 1024)
            {
                string[] lines = content.SplitFast('\n');
                int linkLines = 0;
                int normalLines = 0;
                foreach (string line in lines)
                {
                    if (line.Trim().Length < 5)
                    {
                        continue;
                    }
                    if (line.Contains("http"))
                    {
                        linkLines++;
                    }
                    else
                    {
                        normalLines++;
                    }
                }
                if (linkLines >= normalLines || (linkLines > 0 && normalLines < 4))
                {
                    Console.Error.WriteLine($"Refused paste: link spambot? {linkLines} linkLines and {normalLines} normal lines");
                    return false;
                }
            }
            return true;
        }

        public IActionResult Index()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            if (Request.Method == "POST")
            {
                return HandlePost(this, "script");
            }
            return View(new NewPasteModel() { NewType = "script" });
        }

        public IActionResult Script()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            if (Request.Method == "POST")
            {
                return HandlePost(this, "script");
            }
            return View("Index", new NewPasteModel() { NewType = "script" });
        }

        public IActionResult Log()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            if (Request.Method == "POST")
            {
                return HandlePost(this, "log");
            }
            return View("Index", new NewPasteModel() { NewType = "log" });
        }

        public IActionResult BBCode()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            if (Request.Method == "POST")
            {
                return HandlePost(this, "bbcode");
            }
            return View("Index", new NewPasteModel() { NewType = "bbcode" });
        }

        public IActionResult Text()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            if (Request.Method == "POST")
            {
                return HandlePost(this, "text");
            }
            return View("Index", new NewPasteModel() { NewType = "text" });
        }

        public IActionResult Edit()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            if (!Request.HttpContext.Items.TryGetValue("viewable", out object pasteIdObject) || pasteIdObject is not string pasteIdText)
            {
                Console.Error.WriteLine("Refused edit: ID missing");
                return Redirect("/");
            }
            if (!long.TryParse(pasteIdText, out long pasteId))
            {
                Console.Error.WriteLine("Refused edit: non-numeric ID");
                return Redirect("/Error/Error404");
            }
            if (!PasteDatabase.TryGetPaste(pasteId, out Paste paste))
            {
                Console.Error.WriteLine("Refused view: unlisted ID");
                return Redirect("/Error/Error404");
            }
            if (Request.Method != "POST")
            {
                return Redirect($"/View/{paste.ID}");
            }
            if (Request.Form.TryGetValue("is_edit_button", out StringValues editButtonVal) && editButtonVal.Count == 1 && editButtonVal[0] == "yes")
            {
                return View("Index", new NewPasteModel() { NewType = paste.Type, Edit = paste });
            }
            else
            {
                return HandlePost(this, paste.Type, paste);
            }
        }
    }
}
