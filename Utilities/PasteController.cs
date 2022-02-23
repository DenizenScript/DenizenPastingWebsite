using Microsoft.AspNetCore.Mvc;

namespace DenizenPastingWebsite.Utilities
{
    public class PasteController : Controller
    {
        public void Setup()
        {
            ThemeHelper.HandleTheme(Request, ViewData);
            AuthHelper.HandleAuth(Request, Response, ViewData);
        }
    }
}
