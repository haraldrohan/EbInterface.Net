using EbInterface.Validation;

namespace EbInterface.Mcp.Tests
{
    public class InvoiceToolsTests
    {
        private static readonly string TestData = Path.Combine(AppContext.BaseDirectory, "TestData");

        private static string File6p1 => Path.Combine(TestData, "6p1", "gueltig-bestellnummer.xml");

        private static string File4p3 => Path.Combine(TestData, "4p3", "gueltig-bestellnummer.xml");

        // --- validate_invoice ------------------------------------------------------------------

        [Fact]
        public void Validate_ValidInvoice_ReportsValid()
        {
            string text = InvoiceTools.ValidateInvoice(File6p1, "erechnung", "2026-09-25");

            Assert.Contains("Version: ebInterface 6.1", text);
            Assert.Contains("Profil: e-Rechnung.gv.at", text);
            Assert.Contains("Ergebnis: gültig (0 Warnung(en))", text);
        }

        [Fact]
        public void Validate_InvalidInvoice_ListsCodes()
        {
            string text = InvoiceTools.ValidateInvoice(
                Path.Combine(TestData, "6p1", "fehler-bestellnummer-ohne-position.xml"), "erechnung", "2026-09-25");

            Assert.Contains("Ergebnis: UNGÜLTIG (1 Fehler", text);
            Assert.Contains("[ERB-05]", text);
        }

        [Fact]
        public void Validate_StandardProfile_IgnoresFederalRules()
        {
            string text = InvoiceTools.ValidateInvoice(
                Path.Combine(TestData, "6p1", "fehler-bestellnummer-ohne-position.xml"), "standard");

            Assert.Contains("Ergebnis: gültig", text);
        }

        [Theory]
        [InlineData("gibt-es-nicht.xml", "gibt es nicht")]
        [InlineData("", "Bitte den Pfad")]
        public void Validate_MissingFile_ExplainsProblem(string path, string expected)
        {
            Assert.Contains(expected, InvoiceTools.ValidateInvoice(path));
        }

        [Fact]
        public void Validate_UnknownProfileOrDate_ExplainsProblem()
        {
            Assert.Contains("Unbekanntes Profil", InvoiceTools.ValidateInvoice(File6p1, "bund"));
            Assert.Contains("kein Datum", InvoiceTools.ValidateInvoice(File6p1, "erechnung", "25.09.2026"));
        }

        // --- read_invoice ----------------------------------------------------------------------

        [Fact]
        public void Read_SummarizesInvoice_AndMasksIban()
        {
            string text = InvoiceTools.ReadInvoice(File6p1);

            Assert.Contains("Invoice T-2026-0001 vom 01.09.2026 (ebInterface 6.1, EUR)", text);
            Assert.Contains("Beispiel Handels GmbH, 4020 Linz, UID ATU12345675, Lieferantennummer 50012345", text);
            Assert.Contains("Auftragsreferenz 4700000001", text);
            Assert.Contains("Zeilen: 2", text);
            Assert.Contains("Druckerpapier A4, 500 Blatt [Pos. 10]", text);
            Assert.Contains("zu zahlen: 240,00 EUR", text);
            Assert.Contains("Überweisung auf AT61…3201", text);
            Assert.DoesNotContain("AT611904300234573201", text);
        }

        [Fact]
        public void Read_LimitsListedLines()
        {
            string text = InvoiceTools.ReadInvoice(File6p1, maxLines: 1);

            Assert.Contains("… 1 weitere Zeile(n)", text);
        }

        [Fact]
        public void Read_InvalidDocument_ExplainsWhy()
        {
            string file = Path.Combine(Path.GetTempPath(), $"ebi-mcp-{Guid.NewGuid():N}.xml");
            File.WriteAllText(file, "<Order/>");
            try
            {
                string text = InvoiceTools.ReadInvoice(file);

                Assert.Contains("kein gültiges ebInterface", text);
                Assert.Contains("[VER-01]", text);
            }
            finally
            {
                File.Delete(file);
            }
        }

        // --- upgrade_invoice -------------------------------------------------------------------

        [Fact]
        public void Upgrade_4p3_WritesValid6p1_AndDoesNotOverwrite()
        {
            string dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ebi-mcp-{Guid.NewGuid():N}")).FullName;
            try
            {
                string source = Path.Combine(dir, "alt.xml");
                File.Copy(File4p3, source);

                string text = InvoiceTools.UpgradeInvoice(source);

                string target = Path.Combine(dir, "alt-6p1.xml");
                Assert.Contains($"Geschrieben: {target}", text);
                Assert.Contains("Aus ebInterface 4.3 nach ebInterface 6.1", text);
                ValidationResult result = EbInterfaceValidator.ValidateFile(target);
                Assert.Equal(EbInterfaceVersion.V6p1, result.Version);
                Assert.True(result.IsValid);

                Assert.Contains("gibt es bereits", InvoiceTools.UpgradeInvoice(source));
                Assert.Contains("Geschrieben", InvoiceTools.UpgradeInvoice(source, overwrite: true));
                Assert.Contains("dieselbe Datei", InvoiceTools.UpgradeInvoice(source, source, overwrite: true));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        // --- explain_code ----------------------------------------------------------------------

        [Fact]
        public void Explain_KnownCode()
        {
            string text = InvoiceTools.ExplainCode("erb-27");

            Assert.StartsWith("ERB-27 (Fehler)", text);
            Assert.Contains("Zahlbetrag", text);
            Assert.Contains("docs/pruefregeln.md", text);
        }

        [Fact]
        public void Explain_PrefixListsCodes_UnknownCodeIsReported()
        {
            Assert.Contains("XSD-06:", InvoiceTools.ExplainCode("XSD"));
            Assert.Contains("gibt es nicht", InvoiceTools.ExplainCode("ABC-99"));
        }

        [Fact]
        public void Explain_CatalogCoversAllCodeFamilies()
        {
            var codes = RuleCatalog.All.Keys.ToList();

            Assert.Contains("XML-01", codes);
            Assert.Contains("VER-03", codes);
            Assert.Contains("XSD-08", codes);
            Assert.Contains("WRT-01", codes);
            Assert.Contains("ERB-40", codes);
            Assert.Equal(40, codes.Count(c => c.StartsWith("ERB-", StringComparison.Ordinal)));
        }
    }
}
