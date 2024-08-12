using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using FreneticUtilities.FreneticToolkit;
using FreneticUtilities.FreneticExtensions;
using DenizenPastingWebsite.Models;
using DenizenPastingWebsite.Highlighters;
using DenizenPastingWebsite.Utilities;
using DenizenPastingWebsite.Pasting;
using Newtonsoft.Json;

namespace DenizenPastingWebsite.Controllers
{
    [RequestFormLimits(ValueLengthLimit = 1024 * 1024 * 30)]
    public class NewController : PasteController
    {
        public NewController()
        {
        }

        [NonAction]
        public IActionResult RejectPaste(string type, string reason)
        {
            LogRefusal(reason);
            return View("Index", new NewPasteModel() { ShowRejection = true, NewType = type });
        }

        /// <summary>Quickly cleans control codes from text and replaces them with spaces.</summary>
        public static string ForceCleanText(string text)
        {
            char[] chars = [.. text];
            for (int i = 0; i < chars.Length; i++)
            {
                // ASCII Control codes
                if (chars[i] < 32 || chars[i] == 127)
                {
                    chars[i] = ' ';
                }
            }
            return new string(chars);
        }

        [NonAction]
        public IActionResult HandlePost(string type, Paste edits = null)
        {
            if (Request.Method != "POST" || Request.Form.IsEmpty())
            {
                return RejectPaste(type, "Refused paste: Non-Post");
            }
            (string sender, string realOrigin) = GetSenderAndOrigin();
            Console.WriteLine($"Attempted paste from {realOrigin} as {sender}");
            IFormCollection form = Request.Form;
            if (!form.TryGetValue("pastetitle", out StringValues pasteTitle) || !form.TryGetValue("pastecontents", out StringValues pasteContents))
            {
                return RejectPaste(type, "Refused paste: Form missing keys");
            }
            if (pasteTitle.Count != 1 || pasteContents.Count != 1)
            {
                return RejectPaste(type, "Refused paste: Improper form data");
            }
            if (edits == null && form.TryGetValue("editing", out StringValues editValue) && editValue.Count == 1 && editValue[0] != "")
            {
                if (!long.TryParse(editValue[0], out long editingID))
                {
                    return RejectPaste(type, "Refused paste: Improper form data (editing key)");
                }
                if (!PasteDatabase.TryGetPaste(editingID, out edits))
                {
                    return RejectPaste(type, "Refused edit paste: unlisted ID");
                }
            }
            string[] filters = form.Keys.Where(s => s.StartsWith("privacy_filter_") && form.TryGetValue(s, out StringValues val) && val.Count == 1 && val[0].ToLowerFast() == "on").Select(s => s["privacy_filter_".Length..]).ToArray();
            bool micro = form.TryGetValue("response", out StringValues responseValue) && responseValue.Count == 1 && responseValue[0].ToLowerFast() == "micro";
            bool microv2 = false;
            if (micro)
            {
                sender += ", response=micro";
                if (form.TryGetValue("v", out StringValues versionValue) && versionValue.Count == 1)
                {
                    if (versionValue[0] == "200")
                    {
                        sender += "v2_Denizen";
                        microv2 = true;
                    }
                    else if (versionValue[0] == "300")
                    {
                        sender += "v3_Swarm";
                        microv2 = true;
                    }
                }
            }
            PasteUser user = PasteDatabase.GetUser(sender); // Note: intentionally use 'sender' not 'realOrigin'
            if (user.CurrentStatus == PasteUser.Status.BLOCKED)
            {
                return RejectPaste(type, "Refused paste: blocked sender");
            }
            if (!PasteType.ValidPasteTypes.TryGetValue(type, out PasteType actualType))
            {
                return RejectPaste(type, $"Refused paste: Unknown type {type}");
            }
            string pasteTitleText = ForceCleanText(pasteTitle[0]);
            if (string.IsNullOrWhiteSpace(pasteTitleText))
            {
                pasteTitleText = $"Unnamed {actualType.DisplayName} Paste";
            }
            if (pasteTitleText.Length > 200)
            {
                pasteTitleText = pasteTitleText[0..200];
            }
            string pasteContentText = pasteContents[0];
            if (pasteContentText.Length > PasteServer.MaxPasteRawLength)
            {
                pasteContentText = pasteContentText[0..PasteServer.MaxPasteRawLength];
            }
            pasteContentText = pasteContentText.Replace('\0', ' ').Replace(PasteType.FilterChar, ' ');
            if (!IsValidPaste(pasteTitleText, pasteContentText))
            {
                if (user.CurrentStatus != PasteUser.Status.WHITELIST)
                {
                    return RejectPaste(type, "Invalid paste (spam)");
                }
                Console.WriteLine("Paste allowed to bypass due to whitelisted sender");
            }
            if (user.CurrentStatus != PasteUser.Status.WHITELIST && !RateLimiter.TryUser(realOrigin)) // Note: intentionally use 'realOrigin'
            {
                return RejectPaste(type, "Refused paste: spam (RateLimiter)");
            }
            pasteContentText = pasteContentText.Replace("\r\n", "\n");
            string[] filterOutput = null;
            if (filters != null && filters.Any() && actualType.Filter is not null)
            {
                (pasteContentText, filterOutput) = actualType.Filter(pasteContentText, filters);
            }
            Paste newPaste = new()
            {
                Title = pasteTitleText,
                Type = actualType.Name.ToLowerFast(),
                PostSourceData = sender,
                Date = StringConversionHelper.DateTimeToString(DateTimeOffset.Now, false),
                Raw = pasteContentText,
                Formatted = actualType.Highlight(pasteContentText),
                Edited = (edits == null ? 0 : edits.ID),
                StaffInfo = GenerateStaffInfo(sender, filterOutput)
            };
            if (newPaste.Formatted.Length > PasteServer.MaxPasteRawLength * 5)
            {
                return RejectPaste(type, "Refused paste: Massive formatted-content length");
            }
            if (newPaste.Formatted == null)
            {
                return RejectPaste(type, "Refused paste: format failed");
            }
            string diffText = null;
            if (edits != null)
            {
                diffText = DiffHighlighter.GenerateDiff(edits.Raw, newPaste.Raw, out bool hasDifferences);
                if (!hasDifferences && edits.Type == newPaste.Type)
                {
                    return RejectPaste(type, "Refused paste: edits nothing");
                }
            }
            newPaste.ID = PasteDatabase.GetNextPasteID();
            if (diffText != null)
            {
                Paste diffPaste = new()
                {
                    Title = $"Diff Report Between Paste #{newPaste.ID} and #{edits.ID}",
                    Type = "diff",
                    PostSourceData = "(GENERATED), " + sender,
                    Date = StringConversionHelper.DateTimeToString(DateTimeOffset.Now, false),
                    Raw = diffText,
                    Formatted = DiffHighlighter.Highlight(diffText),
                    Edited = newPaste.ID,
                    ID = PasteDatabase.GetNextPasteID(),
                    StaffInfo = GenerateStaffInfo(sender, null)
                };
                newPaste.DiffReport = diffPaste.ID;
                PasteDatabase.SubmitPaste(diffPaste);
                diffPaste.Raw = diffText;
                PasteServer.RunNewPasteWebhook(diffPaste, user);
            }
            PasteDatabase.SubmitPaste(newPaste);
            newPaste.Raw = pasteContentText;
            PasteServer.RunNewPasteWebhook(newPaste, user);
            Console.Error.WriteLine($"Accepted new paste: {newPaste.ID} from {newPaste.PostSourceData}");
            if (micro)
            {
                Response.ContentType = "text/plain";
                return Ok(microv2 ? $"{PasteServer.URL_BASE}/View/{newPaste.ID}\n" : $"/paste/{newPaste.ID}\n");
            }
            return Redirect($"/View/{newPaste.ID}");
        }

        public static string GenerateStaffInfo(string submitter, string[] filteredOutput)
        {
            Dictionary<string, object> jsonObj = new()
            {
                ["submitter"] = submitter
            };
            if (filteredOutput is not null && filteredOutput.Any())
            {
                jsonObj["filtered"] = filteredOutput;
            }
            return JsonConvert.SerializeObject(jsonObj);
        }

        public static bool IsValidPaste(string title, string content)
        {
            if (content.Length < 100)
            {
                Console.Error.WriteLine($"Refused paste: too-short content {content.Length}");
                return false;
            }
            if (content.Length < 1024)
            {
                int nonEmptyLines = content.SplitFast('\n').Select(s => s.Trim().Length > 5).Count();
                if (nonEmptyLines < 3)
                {
                    Console.Error.WriteLine($"Refused paste: too-few lines {nonEmptyLines} (c.Len = {content.Length})");
                    return false;
                }
            }
            string titleLow = title.ToLowerFast();
            if (titleLow.Contains("<a href="))
            {
                Console.Error.WriteLine("Refused paste: spam-bot title");
                return false;
            }
            foreach (string block in PasteServer.SpamBlockPartialTitles)
            {
                if (titleLow.Contains(block))
                {
                    Console.Error.WriteLine("Refused paste: spam-block-partial-titles in title");
                    return false;
                }
            }
            foreach (string block in PasteServer.SpamBlockTitles)
            {
                if (titleLow == block)
                {
                    Console.Error.WriteLine("Refused paste: spam-block-titles in title");
                    return false;
                }
            }
            if (PasteServer.SpamBlockKeywords.Length > 0)
            {
                string contentLow = content.ToLowerFast();
                foreach (string block in PasteServer.SpamBlockKeywords)
                {
                    if (titleLow.Contains(block))
                    {
                        Console.Error.WriteLine("Refused paste: spam-block-keyphrase in title");
                        return false;
                    }
                    if (contentLow.Contains(block))
                    {
                        Console.Error.WriteLine("Refused paste: spam-block-keyphrase in paste content");
                        return false;
                    }
                }
            }
            if (content.Length > 100 * 1024)
            {
                int newLines = content.CountCharacter('\n');
                if (newLines < 20)
                {
                    Console.Error.WriteLine($"Refused paste: massive paste with too few lines {newLines} (c.Len = {content.Length})");
                    return false;
                }
                return true;
            }
            string[] lines = content.SplitFast('\n');
            int linkLines = 0;
            int normalLines = 0;
            foreach (string line in lines)
            {
                if (line.Trim().Length < 5)
                {
                    continue;
                }
                if (line.Contains("http"))
                {
                    linkLines++;
                }
                else
                {
                    normalLines++;
                }
            }
            if (linkLines >= normalLines || (linkLines > 0 && normalLines < 4))
            {
                Console.Error.WriteLine($"Refused paste: link spambot? {linkLines} linkLines and {normalLines} normal lines");
                return false;
            }
            if (normalLines < 25 && PasteServer.SpamBlockShortKeywords.Length > 0)
            {
                string contentLow = content.ToLowerFast();
                foreach (string block in PasteServer.SpamBlockShortKeywords)
                {
                    if (titleLow.Contains(block))
                    {
                        Console.Error.WriteLine("Refused paste: spam-block-short-keyphrase in title");
                        return false;
                    }
                    if (contentLow.Contains(block))
                    {
                        Console.Error.WriteLine("Refused paste: spam-block-short-keyphrase in paste content");
                        return false;
                    }
                }
            }
            return true;
        }

        public IActionResult Index()
        {
            Setup();
            if (Request.Method == "POST")
            {
                return HandlePost("script");
            }
            return View(new NewPasteModel() { NewType = "script" });
        }

        public IActionResult Script()
        {
            Setup();
            if (Request.Method == "POST")
            {
                return HandlePost("script");
            }
            return View("Index", new NewPasteModel() { NewType = "script" });
        }

        public IActionResult Log()
        {
            Setup();
            if (Request.Method == "POST")
            {
                return HandlePost("log");
            }
            return View("Index", new NewPasteModel() { NewType = "log" });
        }

        public IActionResult Swarm()
        {
            Setup();
            if (Request.Method == "POST")
            {
                return HandlePost("swarm");
            }
            return View("Index", new NewPasteModel() { NewType = "swarm" });
        }

        public IActionResult BBCode()
        {
            Setup();
            if (Request.Method == "POST")
            {
                return HandlePost("bbcode");
            }
            return View("Index", new NewPasteModel() { NewType = "bbcode" });
        }

        public IActionResult Text()
        {
            Setup();
            if (Request.Method == "POST")
            {
                return HandlePost("text");
            }
            return View("Index", new NewPasteModel() { NewType = "text" });
        }

        public IActionResult Other()
        {
            Setup();
            string otherType = "csharp";
            if (Request.Query.TryGetValue("selected", out StringValues selectionValues) && selectionValues.Any())
            {
                otherType = selectionValues[0];
            }
            if (!PasteType.ValidPasteTypes.TryGetValue(selectionValues[0], out PasteType actualType))
            {
                return Refuse("Refused new-other: invalid type");
            }
            if (Request.Method == "POST")
            {
                return HandlePost(otherType);
            }
            return View("Index", new NewPasteModel() { NewType = "other", OtherType = actualType.DisplayName });
        }

        public IActionResult Edit()
        {
            Setup();
            if (!Request.HttpContext.Items.TryGetValue("viewable", out object pasteIdObject) || pasteIdObject is not string pasteIdText)
            {
                return Refuse("Refused edit: ID missing", "/");
            }
            if (!long.TryParse(pasteIdText, out long pasteId))
            {
                return Refuse($"Refused edit: non-numeric ID `{pasteIdText}`");
            }
            if (!PasteDatabase.TryGetPaste(pasteId, out Paste paste))
            {
                return Refuse($"Refused edit: unlisted ID `{pasteId}`");
            }
            if (Request.Method != "POST")
            {
                return Redirect($"/View/{paste.ID}");
            }
            if (Request.Form.TryGetValue("button_type", out StringValues buttonTypeVal) && buttonTypeVal.Count == 1)
            {
                switch (buttonTypeVal[0])
                {
                    case "edit":
                        return View("Index", new NewPasteModel() { NewType = paste.Type, Edit = paste });
                    case "spamblock":
                        if ((bool)ViewData["auth_isloggedin"] && paste.HistoricalContent is null)
                        {
                            paste.Type = "text";
                            paste.Title = "REMOVED SPAM POST";
                            if (string.IsNullOrWhiteSpace(paste.HistoricalContent))
                            {
                                paste.HistoricalContent = paste.Title + "\n\n" + paste.Raw;
                            }
                            paste.Raw = "Spam post removed from view.";
                            paste.Formatted = HighlighterCore.HighlightPlainText("Spam post removed from view.");
                            PasteDatabase.SubmitPaste(paste);
                            Console.WriteLine($"paste {pasteId} removed by logged in staff - {(ulong)ViewData["auth_userid"]}");
                            PasteUser user = PasteDatabase.GetUser(paste.PostSourceData);
                            if (user.CurrentStatus == PasteUser.Status.NORMAL)
                            {
                                user.CurrentStatus = PasteUser.Status.POTENTIAL_SPAMMER;
                                PasteDatabase.ResubmitUser(user);
                            }
                            return Redirect($"/View/{paste.ID}");
                        }
                        break;
                    case "statuschange":
                        if ((bool)ViewData["auth_isloggedin"] && Request.Form.TryGetValue("status", out StringValues statusVal) && statusVal.Count == 1 && Enum.TryParse(statusVal[0], true, out PasteUser.Status statusEnum))
                        {
                            Console.WriteLine($"User {paste.PostSourceData} status changed to {statusEnum} by logged in staff - {(ulong)ViewData["auth_userid"]}");
                            PasteUser user = PasteDatabase.GetUser(paste.PostSourceData);
                            user.CurrentStatus = statusEnum;
                            PasteDatabase.ResubmitUser(user);
                            return Redirect($"/View/{paste.ID}");
                        }
                        break;
                    case "rerender":
                        if ((bool)ViewData["auth_isloggedin"] && paste.HistoricalContent is null)
                        {
                            try
                            {
                                Console.WriteLine($"Rerender paste {paste.ID} on behalf of staff {(ulong)ViewData["auth_userid"]}");
                                PasteDatabase.FillPaste(paste);
                                string origFormat = paste.Formatted;
                                if (PasteType.ValidPasteTypes.TryGetValue(paste.Type, out PasteType type))
                                {
                                    paste.Formatted = type.Highlight(paste.Raw);
                                    if (origFormat.TrimEnd() != paste.Formatted.TrimEnd())
                                    {
                                        Console.WriteLine($"Updating paste {paste.ID} (was {origFormat.Length} now {paste.Formatted.Length})...");
                                        PasteDatabase.SubmitPaste(paste);
                                    }
                                    return Redirect($"/View/{paste.ID}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to rerender paste {paste.ID}: {ex}");
                            }
                        }
                        break;
                }
            }
            return HandlePost(paste.Type, paste);
        }
    }
}
