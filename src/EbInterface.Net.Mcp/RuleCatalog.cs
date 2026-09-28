using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EbInterface.Mcp
{
    /// <summary>
    /// Erklärungen der Fehlercodes, gelesen aus docs/pruefregeln.md (als Ressource eingebettet) – dieselbe Quelle wie
    /// die Doku, damit beides nie auseinanderläuft.
    /// </summary>
    internal static class RuleCatalog
    {
        private static readonly Regex TableRow = new Regex(@"^\|\s*(?<code>[A-Z]{3}-\d{2})\s*\|(?<rest>.*)\|\s*$", RegexOptions.CultureInvariant);

        private static readonly Lazy<IReadOnlyDictionary<string, Entry>> Entries = new Lazy<IReadOnlyDictionary<string, Entry>>(Load);

        internal static IReadOnlyDictionary<string, Entry> All => Entries.Value;

        internal static bool TryGet(string code, out Entry entry) =>
            All.TryGetValue(code.Trim().ToUpperInvariant(), out entry!);

        private static IReadOnlyDictionary<string, Entry> Load()
        {
            using Stream stream = typeof(RuleCatalog).Assembly.GetManifestResourceStream("EbInterface.Mcp.pruefregeln.md")
                ?? throw new InvalidOperationException("Die eingebettete Regelübersicht fehlt.");
            using var reader = new StreamReader(stream);

            var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
            string section = string.Empty;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    section = line.Substring(3).Trim();
                    continue;
                }

                Match match = TableRow.Match(line);
                if (!match.Success) continue;

                string[] cells = match.Groups["rest"].Value.Split('|').Select(c => c.Trim()).ToArray();
                string description = cells[cells.Length - 1];
                string? kind = cells.Length > 1 ? cells[0] : null; // „Fehler“/„Warnung“ bzw. Prüfstufe
                result[match.Groups["code"].Value] = new Entry(match.Groups["code"].Value, section, kind, description);
            }

            return result;
        }

        internal sealed class Entry
        {
            internal Entry(string code, string section, string? kind, string description)
            {
                Code = code;
                Section = section;
                Kind = kind;
                Description = description;
            }

            internal string Code { get; }

            internal string Section { get; }

            internal string? Kind { get; }

            internal string Description { get; }
        }
    }
}
