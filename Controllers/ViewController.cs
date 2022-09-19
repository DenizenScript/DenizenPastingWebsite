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
    public class ViewController : PasteController
    {
        public IActionResult Index()
        {
            Setup();
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
            bool priv = !raw && pasteIdText.EndsWith(".priv.json");
            if (priv)
            {
                if (Request.Method != "POST")
                {
                    Console.Error.WriteLine("Refused view: non-POST access of private info");
                    return Redirect("/Error/Error404");
                }
                if (!(bool)ViewData["auth_isloggedin"])
                {
                    Console.Error.WriteLine("Refused view: non-admin access of private info");
                    return Redirect("/Error/Error404");
                }
                pasteIdText = pasteIdText[0..^".priv.json".Length];
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
            else if (priv)
            {
                PasteUser user = PasteDatabase.GetUser(paste.PostSourceData);
                Response.ContentType = "application/json";
                string staffData = paste.StaffInfo ?? "{}";
                return Ok("{" + $"\"paste\":{staffData},\"userStatus\":\"{user.CurrentStatus}\"" + "}");
            }
            return View(new ViewPasteModel() { Paste = paste });
        }
    }
}
