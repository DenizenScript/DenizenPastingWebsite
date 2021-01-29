using FreneticUtilities.FreneticExtensions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DenizenPastingWebsite
{
    /// <summary>
    /// Helper class that handles theme processing.
    /// </summary>
    public class ThemeHelper
    {
        public static void HandleTheme(HttpRequest request, ViewDataDictionary viewData)
        {
            if (!request.Cookies.TryGetValue("cookie_theme", out string optTheme) || optTheme == null || !Themes.TryGetValue(optTheme.ToLowerFast(), out ThemeHelper theme))
            {
                theme = Dark;
            }
            viewData["Bootstrap_URL"] = theme.BootstrapURL;
            viewData["Bootstrap_Footer"] = theme.BootstrapFooterText;
            viewData["Theme_Colors"] = theme.ColorCSS;
        }

        public static Dictionary<string, ThemeHelper> Themes = new Dictionary<string, ThemeHelper>();

        public static ThemeHelper Dark;

        static ThemeHelper()
        {
            Dark = new ThemeHelper()
            {
                BootstrapURL = "/css/bootstrap_dark.min.css",
                BootstrapFooterText = new HtmlString("<a href=\"https://bootswatch.com/darkly/\">Darkly Bootstrap Theme</a>"),
                ColorCSS = "/css/theme/colors_dark.css"
            };
            ThemeHelper lightRef = new ThemeHelper()
            {
                BootstrapURL = "/css/bootstrap_light.min.css",
                BootstrapFooterText = new HtmlString("<a href=\"https://bootswatch.com/litera/\">Lightera Bootstrap Theme</a>"),
                ColorCSS = "/css/theme/colors_light.css"
            };
            Themes.Add("dark", Dark);
            Themes.Add("light", lightRef);
        }

        public string BootstrapURL;

        public HtmlString BootstrapFooterText;

        public string ColorCSS;
    }
}
