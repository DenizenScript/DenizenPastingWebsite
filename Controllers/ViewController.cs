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
                return Refuse("ID missing", "/");
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
                    return Refuse("non-POST access of private info");
                }
                if (!(bool)ViewData["auth_isloggedin"])
                {
                    return Refuse("non-admin access of private info");
                }
                pasteIdText = pasteIdText[0..^".priv.json".Length];
            }
            if (!long.TryParse(pasteIdText, out long pasteId))
            {
                return Refuse($"non-numeric ID `{pasteIdText}`");
            }
            if (!PasteDatabase.TryGetPaste(pasteId, out Paste paste))
            {
                return Refuse($"non-numeric unlisted ID `{pasteId}`");
            }
            if (paste.TakedownFrom is not null)
            {
                return View("Error451", new View451Model() { IssuingParty = paste.TakedownFrom });
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
