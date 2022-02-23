using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DenizenPastingWebsite.Highlighters;
using Microsoft.AspNetCore.Html;

namespace DenizenPastingWebsite.Pasting
{
    /// <summary>Helper class for the different paste types available.</summary>
    public class PasteType
    {
        /// <summary>A map of all valid paste types (lowercased) to their paste type object.</summary>
        public static Dictionary<string, PasteType> ValidPasteTypes = new();

        /// <summary>HTMLString option list for other language selection.</summary>
        public static HtmlString OtherLangOptions;

        static PasteType()
        {
            ValidPasteTypes["script"] = new PasteType() { Name = "Script", DisplayName = "Denizen Script", FileExtension = "dsc", Highlight = ScriptHighlighter.Highlight, MetaColor = "#00FFFF" };
            ValidPasteTypes["log"] = new PasteType() { Name = "Log", DisplayName = "Server Log", FileExtension = "log", Highlight = LogHighlighter.Highlight, MetaColor = "#2050FF" };
            ValidPasteTypes["diff"] = new PasteType() { Name = "Diff", DisplayName = "Diff Report", FileExtension = "diff", Highlight = DiffHighlighter.Highlight, MetaColor = "#00FF00" };
            ValidPasteTypes["bbcode"] = new PasteType() { Name = "BBCode", DisplayName = "BBCode", FileExtension = "txt", Highlight = BBCodeHighlighter.Highlight, MetaColor = "#FFFFFF" };
            ValidPasteTypes["text"] = new PasteType() { Name = "Text", DisplayName = "Plain Text", FileExtension = "txt", Highlight = HighlighterCore.HighlightPlainText, MetaColor = "#A0A0A0" };
            StringBuilder optionsBuilder = new();
            foreach ((string rawLang, string ext, string display) in AltLanguages)
            {
                ValidPasteTypes[$"other-{rawLang}"] = new PasteType() { Name = $"other-{rawLang}", DisplayName = $"Other: {display}", FileExtension = ext, Highlight = (s) => OtherLanguageHighlighter.Highlight(rawLang, s), MetaColor = "#55BB88" };
                optionsBuilder.Append($"<option name=\"other-{rawLang}\">{display}</option>");
            }
            OtherLangOptions = new HtmlString(optionsBuilder.ToString());
        }

        public string Name;

        public string DisplayName;

        public Func<string, string> Highlight;

        public string FileExtension;

        public string MetaColor;

        public static List<(string, string, string)> AltLanguages = new()
        {
            ("csharp", "cs", "C#"),
            ("java", "java", "Java"),
            ("properties", "properties", "Properties File"),
            ("xml", "html", "HTML or XML"),
            ("cpp", "cpp", "C++"),
            ("c", "c", "C"),
            ("python", "py", "Python"),
            ("ini", "ini", "Ini File"),
            ("yaml", "yml", "YAML"),
            ("json", "json", "JSON"),
            ("markdown", "md", "Markdown"),
            ("javascript", "js", "JavaScript"),
            ("bash", "sh", "Bash Script"),
            ("css", "css", "CSS"),
            ("wasm", "wasm", "WebAssembly"),
            ("php", "php", "PHP"),
            ("swift", "swift", "Swift"),
            ("vim", "vim", "Vim Script"),
            ("http", "http", "HTTP"),
            ("ruby", "rb", "Ruby"),
            ("fsharp", "fs", "F#"),
            ("scss", "scss", "SCSS"),
            ("x86asm", "asm", "x86 Assembly"),
            ("powershell", "ps1", "PowerShell"),
            ("dos", "bat", "Batch File (DOS)"),
            ("perl", "perl", "Perl"),
            ("go", "go", "Go"),
            ("sql", "sql", "SQL"),
            ("typescript", "ts", "TypeScript"),
            ("kotlin", "kt", "Kotlin"),
            ("vbnet", "vb", "VB.NET"),
            ("rust", "rs", "Rust"),
            ("less", "less", "LESS"),
            ("lua", "lua", "lua"),
            ("python-repl", "py", "Python REPL"),
            ("r", "r", "R"),
            ("shell", "shell", "Shell Session"),
            ("objectivec", "objc", "Objective C")
        };
    }
}
