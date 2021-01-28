using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Models;

namespace DenizenPastingWebsite.Controllers
{
    public class ViewController : Controller
    {
        public IActionResult Index()
        {
            if (!Request.Query.TryGetValue("paste_id", out StringValues pasteIdValue) || pasteIdValue.Count != 1)
            {
                return Redirect("/");
            }
            string pasteIdText = pasteIdValue[0];
            bool raw = pasteIdText.EndsWith(".txt");
            if (raw)
            {
                pasteIdText = pasteIdText[0..^".txt".Length];
            }
            if (!long.TryParse(pasteIdText, out long pasteId))
            {
                return View("/Error/Error404");
            }
            if (!PasteDatabase.TryGetPaste(pasteId, out Paste paste))
            {
                Response.StatusCode = 404;
                return View("/Error/Error404");
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
