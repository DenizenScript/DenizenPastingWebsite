using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticExtensions;
using DenizenPastingWebsite.Models;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

namespace DenizenPastingWebsite.Controllers
{
    public class InfoController : PasteController
    {
        public IActionResult Terms()
        {
            Setup();
            return View(new InfoModel());
        }

        public IActionResult Search()
        {
            Setup();
            if (!(bool)ViewData["auth_isloggedin"])
            {
                return Refuse("Refused Search: non-admin access");
            }
            return View(new InfoModel());
        }

        public IActionResult PostSearchJson()
        {
            Setup();
            if (!(bool)ViewData["auth_isloggedin"])
            {
                return Refuse("Refused Search JSON: non-admin access");
            }
            if (Request.Method != "POST" || Request.Query.IsEmpty())
            {
                return Refuse("Refused Search JSON: non-post access");
            }
            if (!Request.Query.TryGetValue("search-term", out StringValues searchTerm) || searchTerm.Count != 1)
            {
                return Refuse("Refused Search JSON: missing 'search-term' query");
            }
            if (!Request.Query.TryGetValue("search-start-ind", out StringValues startIndString) || startIndString.Count != 1)
            {
                return Refuse("Refused Search JSON: missing 'search-start-ind' query");
            }
            if (!long.TryParse(startIndString[0], out long startInd))
            {
                Console.Error.WriteLine("Refused Search JSON: query invalid data");
                return Ok("{\"error\": \"invalid input\"}");
            }
            if (searchTerm[0].Length < 3)
            {
                Console.Error.WriteLine("Refused Search JSON: Invalid search term");
                return Ok("{\"error\": \"search term too short\"}");
            }
            if (startInd < 0)
            {
                Console.Error.WriteLine("Refused Search JSON: invalid start ind");
                return Ok("{\"error\": \"cannot start negative\"}");
            }
            if (searchTerm[0].Length > 10_000)
            {
                Console.Error.WriteLine("Refused Search JSON: too-long search");
                return Ok("{'error': \"search term too long\"}");
            }
            string[] searches = searchTerm[0].Split("|||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(s => s.Length >= 3).ToArray();
            if (searches.Length == 0)
            {
                Console.Error.WriteLine("Refused Search JSON: empty search");
                return Ok("{\"error\": \"no valid searches given\"}");
            }
            if (searches.Length > 25)
            {
                Console.Error.WriteLine("Refused Search JSON: too many searches");
                return Ok("{\"error\": \"too many search terms\"}");
            }
            Console.WriteLine($"User ID {ViewData["auth_userid"]} is searching term `{string.Join("`, `", searches)}` at ind {startInd}...");
            (Paste, int)[] res = DBSearchHelper.GetSearchResults(searches, startInd, 1000);
            if (res is null)
            {
                Console.Error.WriteLine("Refused Search JSON: Search rejected by helper, invalid input");
                return Ok("{\"error\": \"invalid input\"}");
            }
            Console.WriteLine($"User ID {ViewData["auth_userid"]} searched term {searchTerm[0]} at ind {startInd} and got {res.Length} results.");
            JArray output = [];
            foreach ((Paste paste, int matchId) in res)
            {
                if (paste is null)
                {
                    output.Add(new JObject()
                    {
                        ["match_id"] = matchId,
                        ["id"] = -1
                    });
                }
                else
                {
                    output.Add(new JObject()
                    {
                        ["match_id"] = matchId,
                        ["id"] = paste.ID,
                        ["title"] = paste.Title,
                        ["type"] = paste.Type,
                        ["date"] = paste.Date,
                        ["source"] = paste.PostSourceData,
                        ["edited"] = paste.Edited
                    });
                }
            }
            return Ok(new JObject() { ["result"] = output }.ToString());
        }
    }
}
