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

        public IActionResult Index()
        {
            return View(new NewPasteModel() { RecommendChangeType = true });
        }

        public IActionResult Script()
        {
            return View(new NewPasteModel() { NewType = "Script" });
        }

        public IActionResult Log()
        {
            return View(new NewPasteModel() { NewType = "Log" });
        }

        public IActionResult BBCode()
        {
            return View(new NewPasteModel() { NewType = "BBCode" });
        }

        public IActionResult Text()
        {
            return View(new NewPasteModel() { NewType = "Text" });
        }
    }
}
