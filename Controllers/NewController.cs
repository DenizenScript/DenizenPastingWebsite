using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DenizenPastingWebsite.Models;

namespace DenizenPastingWebsite.Controllers
{
    public class NewController : Controller
    {
        public NewController()
        {
        }

        public static IActionResult HandlePost(NewController controller)
        {
            return controller.View(new NewPasteModel() { ShowRejection = true });
        }

        public IActionResult Index()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this);
            }
            return View(new NewPasteModel());
        }

        public IActionResult Script()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this);
            }
            return View("Index", new NewPasteModel() { NewType = "Script" });
        }

        public IActionResult Log()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this);
            }
            return View("Index", new NewPasteModel() { NewType = "Log" });
        }

        public IActionResult BBCode()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this);
            }
            return View("Index", new NewPasteModel() { NewType = "BBCode" });
        }

        public IActionResult Text()
        {
            if (Request.Method == "POST")
            {
                return HandlePost(this);
            }
            return View("Index", new NewPasteModel() { NewType = "Text" });
        }
    }
}
