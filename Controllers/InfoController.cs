using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Models;

namespace DenizenPastingWebsite.Controllers
{
    public class InfoController : PasteController
    {
        public IActionResult Terms()
        {
            Setup();
            if (Response.StatusCode == 200 && !Response.HasStarted)
            {
                Response.StatusCode = 400;
            }
            return View(new InfoModel());
        }
    }
}
