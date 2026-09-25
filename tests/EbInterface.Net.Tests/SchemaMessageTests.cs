using EbInterface.Validation;
using static EbInterface.Tests.TestSupport;

namespace EbInterface.Tests
{
    /// <summary>Deutsche Meldungen der Schema-Prüfung (XSD-xx) für die häufigsten Fehler.</summary>
    public class SchemaMessageTests
    {
        private const string BaseFile = "6p1/gueltig-bestellnummer.xml";

        [Theory]
        // Element fehlt oder steht falsch (XSD-02/03)
        [InlineData("<InvoiceNumber>T-2026-0001</InvoiceNumber>", "", "XSD-03",
            "Das Element <InvoiceDate> ist in <Invoice> an dieser Stelle nicht erlaubt. Erwartet wird hier: <InvoiceNumber>.")]
        [InlineData("<LineItemAmount>90.00</LineItemAmount>", "", "XSD-02",
            "In <ListLineItem> fehlt ein Pflichtelement: <LineItemAmount>.")]
        // unbekanntes Element (XSD-04)
        [InlineData("<InvoiceDate>", "<Rechnungsdatum>x</Rechnungsdatum><InvoiceDate>", "XSD-04",
            "Das Element <Rechnungsdatum> gibt es in ebInterface 6.1 nicht (Tippfehler?).")]
        // Text statt Unterelementen (XSD-05)
        [InlineData("<Delivery>", "<Delivery>morgen", "XSD-05",
            "<Delivery> darf keinen Text enthalten, nur Unterelemente")]
        // ungültige Werte (XSD-06)
        [InlineData("<InvoiceDate>2026-09-01</InvoiceDate>", "<InvoiceDate>01.09.2026</InvoiceDate>", "XSD-06",
            "Der Wert '01.09.2026' in <InvoiceDate> ist ungültig: Erwartet wird ein Datum im Format JJJJ-MM-TT")]
        [InlineData("<UnitPrice>4.50</UnitPrice>", "<UnitPrice>4,50</UnitPrice>", "XSD-06",
            "Der Wert '4,50' in <UnitPrice> ist ungültig: Erwartet wird eine Zahl mit Punkt als Dezimaltrennzeichen")]
        [InlineData("<UnitPrice>4.50</UnitPrice>", "<UnitPrice>4.123456</UnitPrice>", "XSD-06",
            "zu viele Nachkommastellen; erlaubt sind höchstens 4.")]
        [InlineData("DocumentType=\"Invoice\"", "DocumentType=\"Rechnung\"", "XSD-06",
            "Der Wert 'Rechnung' im Attribut DocumentType von <Invoice> ist ungültig: Zulässig sind nur: CreditMemo,")]
        [InlineData("InvoiceCurrency=\"EUR\"", "InvoiceCurrency=\"EURO\"", "XSD-06",
            "Er darf höchstens 3 Zeichen lang sein (hier 4).")]
        [InlineData("<Country CountryCode=\"AT\">", "<Country CountryCode=\"Österreich\">", "XSD-06",
            "im Attribut CountryCode von <Country> ist ungültig")]
        // Attribute (XSD-07/08)
        [InlineData(" InvoiceCurrency=\"EUR\"", "", "XSD-07",
            "Bei <Invoice> fehlt das Pflichtattribut InvoiceCurrency.")]
        [InlineData(" Language=\"de\"", " Language=\"de\" Waehrung=\"EUR\"", "XSD-08",
            "Das Attribut Waehrung ist bei <Invoice> nicht vorgesehen.")]
        public void CommonSchemaErrors_HaveGermanMessages(string find, string replace, string code, string expectedText)
        {
            string xml = File.ReadAllText(TestFile(BaseFile));
            Assert.Contains(find, xml);

            var result = Validate(ReplaceFirst(xml, find, replace));

            var error = Assert.Single(result.Errors);
            Assert.Equal(code, error.Code);
            Assert.Contains(expectedText, error.Message);
            Assert.True(error.Line > 0, "Zeile fehlt");
        }

        [Fact]
        public void ErrorPositionPointsToTheOffendingNode()
        {
            string xml = File.ReadAllText(TestFile(BaseFile)).Replace("\r\n", "\n");
            var result = Validate(xml.Replace("<UnitPrice>55.00</UnitPrice>", "<UnitPrice>55,00</UnitPrice>"));

            var error = Assert.Single(result.Errors);
            int expectedLine = xml.Substring(0, xml.IndexOf("<UnitPrice>55.00", StringComparison.Ordinal)).Count(c => c == '\n') + 1;
            Assert.Equal(expectedLine, error.Line);
        }

        [Fact]
        public void SeveralIndependentErrors_AreAllReported()
        {
            string xml = File.ReadAllText(TestFile(BaseFile))
                .Replace("<InvoiceDate>2026-09-01</InvoiceDate>", "<InvoiceDate>01.09.2026</InvoiceDate>")
                .Replace("<UnitPrice>4.50</UnitPrice>", "<UnitPrice>4,50</UnitPrice>");

            var result = Validate(xml);

            Assert.Equal(2, result.Errors.Count());
            Assert.All(result.Errors, e => Assert.Equal("XSD-06", e.Code));
        }

        private static string ReplaceFirst(string text, string find, string replace)
        {
            int index = text.IndexOf(find, StringComparison.Ordinal);
            return text.Substring(0, index) + replace + text.Substring(index + find.Length);
        }
    }
}
