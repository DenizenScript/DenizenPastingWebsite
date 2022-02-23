using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Models;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;

namespace DenizenPastingWebsite.Controllers
{
    public class ViewController : Controller
    {
        public IActionResult Index()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            if (!Request.HttpContext.Items.TryGetValue("viewable", out object pasteIdObject) || pasteIdObject is not string pasteIdText)
            {
                Console.Error.WriteLine("Refused view: ID missing");
                return Redirect("/");
            }
            bool raw = pasteIdText.EndsWith(".txt");
            if (raw)
            {
                pasteIdText = pasteIdText[0..^".txt".Length];
            }
            if (!long.TryParse(pasteIdText, out long pasteId))
            {
                Console.Error.WriteLine("Refused view: non-numeric ID");
                return Redirect("/Error/Error404");
            }
            if (!PasteDatabase.TryGetPaste(pasteId, out Paste paste))
            {
                Console.Error.WriteLine("Refused view: unlisted ID");
                return Redirect("/Error/Error404");
            }
            if (raw)
            {
                Response.ContentType = "text/plain";
                return Ok(paste.Raw);
            }
            return View(new ViewPasteModel() { Paste = paste });
        }
    }
}
