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

namespace DenizenPastingWebsite.Controllers
{
    public class NewController : Controller
    {
        public NewController()
        {
        }

        public static IActionResult RejectPaste(NewController controller, string type)
        {
            return controller.View(new NewPasteModel() { ShowRejection = true, NewType = type });
        }

        public static IActionResult HandlePost(NewController controller, string type)
        {
            if (controller.Request.Method != "POST" || controller.Request.HasFormContentType)
            {
                return RejectPaste(controller, type);
            }
            IFormCollection form = controller.Request.Form;
            if (!form.TryGetValue("pastetype", out StringValues pasteType) || !form.TryGetValue("pastetitle", out StringValues pasteTitle) || !form.TryGetValue("pastecontents", out StringValues pasteContents))
            {
                return RejectPaste(controller, type);
            }
            if (pasteType.Count != 1 || pasteTitle.Count != 1 || pasteContents.Count != 1)
            {
                return RejectPaste(controller, type);
            }
            string pasteTypeName = pasteType[0].ToLowerFast();
            if (!PasteType.ValidPasteTypes.TryGetValue(pasteTypeName, out PasteType actualType))
            {
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
            if (pasteContentText.Length > PasteDatabase.MaxPasteRawLength)
            {
                pasteContentText = pasteContentText[0..PasteDatabase.MaxPasteRawLength];
            }
            if (!IsValidPaste(pasteTitleText, pasteContentText))
            {
                return RejectPaste(controller, type);
            }
            string sender = $"Remote IP: {controller.Request.HttpContext.Connection.RemoteIpAddress}";
            if (controller.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues forwardHeader))
            {
                sender += ", X-Forwarded-For: " + string.Join(" / ", controller.Request.Headers["X-Forwarded-For"]);
            }
            if (controller.Request.Headers.TryGetValue("REMOTE_ADDR", out StringValues remoteAddr))
            {
                sender += ", REMOTE_ADDR: " + string.Join(" / ", remoteAddr);
            }
            Paste newPaste = new Paste()
            {
                Title = pasteTitleText,
                Type = actualType.Name.ToLowerFast(),
                PostSourceData = sender,
                Date = StringConversionHelper.DateTimeToString(DateTimeOffset.Now, false),
                Raw = pasteContentText,
                Formatted = actualType.Highlight(pasteContentText)
            };
            if (newPaste.Formatted == null)
            {
                return RejectPaste(controller, type);
            }
            PasteDatabase.SubmitPaste(newPaste);
            return controller.Redirect($"/View/{newPaste.ID}");
        }

        public static bool IsValidPaste(string title, string content)
        {
            if (content.Length < 100)
            {
                return false;
            }
            if (content.Length < 1024 && content.SplitFast('\n').Select(s => s.Trim().Length > 5).Count() < 3)
            {
                return false;
            }
            // Basic spambot detection:
            string titleLow = title.ToLowerFast();
            if (titleLow.Contains("<a href=") || titleLow.Contains("viagra") || titleLow.Contains("cialis"))
            {
                return false;
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
                if (linkLines >= normalLines)
                {
                    return false;
                }
            }
            return true;
        }

        public IActionResult Index()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this, "Script");
            }
            return View(new NewPasteModel() { NewType = "Script" });
        }

        public IActionResult Script()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this, "Script");
            }
            return View("Index", new NewPasteModel() { NewType = "Script" });
        }

        public IActionResult Log()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this, "Log");
            }
            return View("Index", new NewPasteModel() { NewType = "Log" });
        }

        public IActionResult BBCode()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this, "BBCode");
            }
            return View("Index", new NewPasteModel() { NewType = "BBCode" });
        }

        public IActionResult Text()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this, "Text");
            }
            return View("Index", new NewPasteModel() { NewType = "Text" });
        }
    }
}
