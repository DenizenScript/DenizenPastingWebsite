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
    [AutoValidateAntiforgeryToken]
    public class NewController : Controller
    {
        public NewController()
        {
        }

        public IActionResult Index()
        {
            return View(new NewPasteModel());
        }

        public IActionResult Script()
        {
            return View("Index", new NewPasteModel() { NewType = "Script" });
        }

        public IActionResult Log()
        {
            return View("Index", new NewPasteModel() { NewType = "Log" });
        }

        public IActionResult BBCode()
        {
            return View("Index", new NewPasteModel() { NewType = "BBCode" });
        }

        public IActionResult Text()
        {
            return View("Index", new NewPasteModel() { NewType = "Text" });
        }
    }
}
