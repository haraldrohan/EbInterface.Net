using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EbInterface.Model;
using EbInterface.Validation;
using ModelContextProtocol.Server;

namespace EbInterface.Mcp
{
    /// <summary>
    /// Die Werkzeuge des MCP-Servers. Rechnungen werden über Dateipfade angegeben, damit nicht der ganze Inhalt durch
    /// das Gespräch mit dem Sprachmodell läuft; zurück kommen nur Meldungen bzw. eine Zusammenfassung.
    /// </summary>
    [McpServerToolType]
    public static class InvoiceTools
    {
        private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-AT");

        /// <summary>Prüft eine ebInterface-Rechnung.</summary>
        [McpServerTool(Name = "validate_invoice", Title = "ebInterface-Rechnung prüfen", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Prüft eine ebInterface-Rechnung (4.3 bis 6.1) aus einer Datei: wohlgeformtes XML, Version, XML-Schema und – " +
            "im Profil 'erechnung' – die Regeln von e-Rechnung.gv.at für Rechnungen an Bund, Länder und Gemeinden. " +
            "Liefert gültig/ungültig und alle Meldungen mit stabilem Code (z. B. ERB-27), Zeile und deutschem Text.")]
        public static string ValidateInvoice(
            [Description("Vollständiger Pfad zur XML-Datei.")] string path,
            [Description("'standard' = nur ebInterface-Standard (z. B. für Rechnungen an Unternehmen); " +
                "'erechnung' = zusätzlich die Regeln von e-Rechnung.gv.at.")] string profile = "standard",
            [Description("Optionaler Stichtag im Format JJJJ-MM-TT für datumsabhängige Regeln; leer = heute.")] string? referenceDate = null)
        {
            if (!TryResolveFile(path, out string file, out string? problem)) return problem!;

            ValidationProfile validationProfile;
            switch (profile.Trim().ToLowerInvariant())
            {
                case "standard": validationProfile = ValidationProfile.Standard; break;
                case "erechnung":
                case "e-rechnung":
                case "erechnunggvat": validationProfile = ValidationProfile.ERechnungGvAt; break;
                default: return $"Unbekanntes Profil '{profile}'. Zulässig sind 'standard' und 'erechnung'.";
            }

            DateTime? date = null;
            if (!string.IsNullOrWhiteSpace(referenceDate))
            {
                if (!DateTime.TryParseExact(referenceDate!.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                    return $"Stichtag '{referenceDate}' ist kein Datum im Format JJJJ-MM-TT.";
                date = parsed;
            }

            ValidationResult result = EbInterfaceValidator.ValidateFile(file,
                new ValidationOptions { Profile = validationProfile, ReferenceDate = date });

            var text = new StringBuilder();
            int errors = result.Errors.Count();
            int warnings = result.Warnings.Count();
            text.AppendLine($"Datei: {file}");
            text.AppendLine($"Version: {result.Version.ToDisplayString()}");
            text.AppendLine($"Profil: {(validationProfile == ValidationProfile.ERechnungGvAt ? "e-Rechnung.gv.at" : "Standard")}");
            text.AppendLine(result.IsValid
                ? $"Ergebnis: gültig ({warnings} Warnung(en))"
                : $"Ergebnis: UNGÜLTIG ({errors} Fehler, {warnings} Warnung(en))");
            foreach (ValidationMessage message in result.Messages)
                text.AppendLine($"- {(message.Severity == ValidationSeverity.Error ? "Fehler" : "Warnung")} {message}");
            if (!result.IsValid && validationProfile == ValidationProfile.Standard)
                text.AppendLine("Hinweis: Die Regeln von e-Rechnung.gv.at werden erst geprüft, wenn der Standard fehlerfrei ist.");
            return text.ToString().TrimEnd();
        }

        /// <summary>Liest eine Rechnung und fasst sie zusammen.</summary>
        [McpServerTool(Name = "read_invoice", Title = "ebInterface-Rechnung lesen", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Liest eine schemagültige ebInterface-Rechnung (4.3 bis 6.1) und liefert eine Zusammenfassung: Kopfdaten, " +
            "Rechnungssteller und -empfänger, Auftragsreferenz, Zeilen, Steuer, Summen und Zahlungsart. IBANs werden gekürzt.")]
        public static string ReadInvoice(
            [Description("Vollständiger Pfad zur XML-Datei.")] string path,
            [Description("Höchstzahl der aufgelisteten Zeilen (Vorgabe 50).")] int maxLines = 50)
        {
            if (!TryResolveFile(path, out string file, out string? problem)) return problem!;

            EbInvoice invoice;
            try
            {
                invoice = EbInterfaceReader.ReadFile(file);
            }
            catch (EbInterfaceReadException ex)
            {
                return "Die Datei kann nicht gelesen werden, weil sie kein gültiges ebInterface ist:" + Environment.NewLine +
                    string.Join(Environment.NewLine, ex.Validation.Errors.Select(e => "- " + e)) + Environment.NewLine +
                    "Mit validate_invoice lassen sich die Fehler im Einzelnen prüfen.";
            }

            var text = new StringBuilder();
            text.AppendLine($"{invoice.DocumentType} {invoice.InvoiceNumber} vom {Date(invoice.InvoiceDate)} ({invoice.SourceVersion.ToDisplayString()}, {invoice.Currency})");
            text.AppendLine($"Rechnungssteller: {PartyLine(invoice.Biller)}" +
                (invoice.Biller.InvoiceRecipientsBillerId != null ? $", Lieferantennummer {invoice.Biller.InvoiceRecipientsBillerId}" : string.Empty));
            text.AppendLine($"Rechnungsempfänger: {PartyLine(invoice.InvoiceRecipient)}" +
                (invoice.InvoiceRecipient.OrderReference != null ? $", Auftragsreferenz {invoice.InvoiceRecipient.OrderReference.OrderId}" : string.Empty));
            if (invoice.Delivery != null)
                text.AppendLine($"Lieferung: {(invoice.Delivery.Date.HasValue ? Date(invoice.Delivery.Date.Value) : $"{Date(invoice.Delivery.PeriodFrom)} bis {Date(invoice.Delivery.PeriodTo)}")}");

            LineItem[] lines = invoice.AllLineItems.ToArray();
            text.AppendLine($"Zeilen: {lines.Length}");
            foreach (LineItem line in lines.Take(Math.Max(0, maxLines)))
            {
                string reference = line.InvoiceRecipientsOrderReference?.OrderPositionNumber is string position ? $" [Pos. {position}]" : string.Empty;
                text.AppendLine($"- {line.PositionNumber ?? "-"}: {string.Join(" / ", line.Descriptions)}{reference} – " +
                    $"{Num(line.Quantity)} {line.Unit} × {Num(line.UnitPrice)}{(line.BaseQuantity is decimal b && b != 1m ? $" je {Num(b)}" : string.Empty)} " +
                    $"= {Num(line.LineItemAmount)} ({Num(line.TaxPercent)} % {line.TaxCategoryCode})");
            }

            if (lines.Length > maxLines)
                text.AppendLine($"  … {lines.Length - maxLines} weitere Zeile(n)");
            foreach (BelowTheLineItem item in invoice.BelowTheLineItems)
                text.AppendLine($"- nach Steuer: {item.Description} {Num(item.LineItemAmount)}");

            foreach (TaxItem tax in invoice.TaxItems)
                text.AppendLine($"Steuer {Num(tax.TaxPercent)} % {tax.TaxCategoryCode}: Grundlage {Num(tax.TaxableAmount)}, Steuer {Num(tax.TaxAmount)}");
            text.AppendLine($"Gesamt brutto: {Num(invoice.TotalGrossAmount)}, zu zahlen: {Num(invoice.PayableAmount)} {invoice.Currency}");
            text.AppendLine($"Zahlung: {PaymentLine(invoice.PaymentMethod)}");
            if (invoice.PaymentConditions?.DueDate is DateTime due)
                text.AppendLine($"Fällig: {Date(due)}" + string.Concat(invoice.PaymentConditions.Discounts.Select(d =>
                    $"; Skonto {Num(d.Percentage)} % bis {Date(d.PaymentDate)}")));
            return text.ToString().TrimEnd();
        }

        /// <summary>Aktualisiert eine Rechnung auf ebInterface 6.1.</summary>
        [McpServerTool(Name = "upgrade_invoice", Title = "ebInterface-Rechnung auf 6.1 aktualisieren", ReadOnly = false, Destructive = false, Idempotent = true, OpenWorld = false)]
        [Description("Liest eine Rechnung in ebInterface 4.3, 5.0, 6.0 oder 6.1 und schreibt sie als ebInterface 6.1 in eine neue Datei. " +
            "Überschreibt keine bestehende Datei, außer overwrite ist true. Beim Upgrade aus 4.3 wird die Steuerkategorie ergänzt " +
            "(S bzw. E); DirectDebit aus 4.3 gibt es in 6.1 nicht.")]
        public static string UpgradeInvoice(
            [Description("Vollständiger Pfad zur Quelldatei.")] string sourcePath,
            [Description("Zieldatei; leer = gleicher Ordner und Name mit '-6p1.xml'.")] string? targetPath = null,
            [Description("Bestehende Zieldatei überschreiben.")] bool overwrite = false)
        {
            if (!TryResolveFile(sourcePath, out string source, out string? problem)) return problem!;

            string target = string.IsNullOrWhiteSpace(targetPath)
                ? Path.Combine(Path.GetDirectoryName(source) ?? ".", Path.GetFileNameWithoutExtension(source) + "-6p1.xml")
                : Path.GetFullPath(targetPath!);
            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
                return "Quelle und Ziel sind dieselbe Datei. Bitte eine andere Zieldatei angeben.";
            if (File.Exists(target) && !overwrite)
                return $"Die Zieldatei {target} gibt es bereits. Zum Überschreiben overwrite = true angeben.";

            try
            {
                EbInvoice invoice = EbInterfaceReader.ReadFile(source);
                EbInterfaceWriter.WriteFile(invoice, target);
                return $"Geschrieben: {target}" + Environment.NewLine +
                    $"Aus {invoice.SourceVersion.ToDisplayString()} nach ebInterface 6.1 aktualisiert. " +
                    "Mit validate_invoice (Profil 'erechnung') lässt sich das Ergebnis gegen die Regeln von e-Rechnung.gv.at prüfen.";
            }
            catch (EbInterfaceException ex)
            {
                return ex.Message + Environment.NewLine + string.Join(Environment.NewLine, ex.Validation.Errors.Select(e => "- " + e));
            }
            catch (NotSupportedException ex)
            {
                return ex.Message;
            }
        }

        /// <summary>Erklärt einen Fehlercode.</summary>
        [McpServerTool(Name = "explain_code", Title = "Fehlercode erklären", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Erklärt einen Fehlercode von EbInterface.Net (z. B. XSD-06, ERB-27, WRT-01). Ohne Code oder mit einem " +
            "Präfix wie 'ERB' kommt die Liste der passenden Codes.")]
        public static string ExplainCode([Description("Code wie 'ERB-27' oder Präfix wie 'ERB'; leer = alle Codes.")] string? code = null)
        {
            string wanted = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (wanted.Length > 0 && RuleCatalog.TryGet(wanted, out RuleCatalog.Entry entry))
            {
                return $"{entry.Code}{(entry.Kind != null ? $" ({entry.Kind})" : string.Empty)} – {entry.Section}" + Environment.NewLine +
                    entry.Description + Environment.NewLine +
                    "Übersicht und Auslegungen: https://github.com/haraldrohan/EbInterface.Net/blob/main/docs/pruefregeln.md";
            }

            var matches = RuleCatalog.All.Values.Where(e => wanted.Length == 0 || e.Code.StartsWith(wanted, StringComparison.Ordinal)).ToList();
            if (matches.Count == 0)
                return $"Den Code '{code}' gibt es nicht. Präfixe: XML, VER, XSD, WRT, ERB.";
            return string.Join(Environment.NewLine, matches.Select(e => $"{e.Code}: {e.Description}"));
        }

        private static bool TryResolveFile(string path, out string file, out string? problem)
        {
            file = string.Empty;
            problem = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                problem = "Bitte den Pfad zur XML-Datei angeben.";
                return false;
            }

            try
            {
                file = Path.GetFullPath(path.Trim().Trim('"'));
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                problem = $"Der Pfad '{path}' ist ungültig.";
                return false;
            }

            if (!File.Exists(file))
            {
                problem = $"Die Datei {file} gibt es nicht.";
                return false;
            }

            return true;
        }

        private static string PartyLine(Party party)
        {
            string name = party.Address?.Name ?? party.Contact?.Name ?? "(ohne Namen)";
            string place = party.Address != null ? $", {party.Address.Zip} {party.Address.Town}" : string.Empty;
            return $"{name}{place}, UID {party.VatIdentificationNumber}";
        }

        private static string PaymentLine(PaymentMethod? payment) => payment switch
        {
            null => "keine Angabe",
            UniversalBankTransaction u => "Überweisung auf " + string.Join(", ", u.BeneficiaryAccounts.Select(a => MaskIban(a.Iban))) +
                (u.PaymentReference != null ? $", Zahlungsreferenz {u.PaymentReference}" : string.Empty),
            SepaDirectDebit s => $"SEPA-Lastschrift ({s.Type}) von {MaskIban(s.Iban)}",
            NoPayment _ => "keine Zahlung",
            DirectDebit _ => "Lastschrift",
            PaymentCard _ => "Kartenzahlung",
            _ => "andere Zahlungsart",
        };

        /// <summary>Nur Länderkennung und die letzten vier Stellen – genug zum Wiedererkennen.</summary>
        private static string MaskIban(string? iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return "(ohne IBAN)";
            string compact = iban!.Replace(" ", string.Empty);
            return compact.Length <= 8 ? compact : $"{compact.Substring(0, 4)}…{compact.Substring(compact.Length - 4)}";
        }

        private static string Num(decimal? value) => value?.ToString("#,##0.00##", German) ?? "-";

        private static string Date(DateTime? value) => value?.ToString("dd.MM.yyyy", German) ?? "-";
    }
}
