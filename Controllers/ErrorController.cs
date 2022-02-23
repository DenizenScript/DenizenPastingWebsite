using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Utilities;

namespace DenizenPastingWebsite.Controllers
{
    public class ErrorController : PasteController
    {
        public IActionResult Any()
        {
            Setup();
            if (Response.StatusCode == 200 && !Response.HasStarted)
            {
                Response.StatusCode = 400;
            }
            return View();
        }

        public IActionResult Error404()
        {
            Setup();
            if (Response.StatusCode == 200 && !Response.HasStarted)
            {
                Response.StatusCode = 404;
            }
            return View();
        }
    }
}
