using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using EbInterface.Model;
using EbInterface.Validation;
using static EbInterface.Tests.TestSupport;

namespace EbInterface.Tests
{
    public class WriterTests
    {
        public static IEnumerable<object[]> ValidSamples(string versions) =>
            OfficialSampleTests.Samples(versions).Where(s => !((string)s[0]).Contains("_invalid"));

        public static IEnumerable<object[]> Samples6p1() => ValidSamples("6p1");

        public static IEnumerable<object[]> OlderSamples() => ValidSamples("4p3,5p0,6p0");

        public static IEnumerable<object[]> OwnTestFiles() =>
            new[] { "6p1/gueltig-bestellnummer.xml", "6p1/gueltig-einkaeufergruppe.xml", "6p1/gueltig-anderer-empfaenger.xml" }
                .Select(f => new object[] { f });

        [Theory]
        [MemberData(nameof(Samples6p1))]
        public void Official6p1Sample_RoundTripsWithoutLoss(string relativePath)
        {
            EbInvoice original = EbInterfaceReader.ReadFile(Path.Combine(StandardsRoot, relativePath));

            EbInvoice copy = RoundTrip(original);

            Assert.Equal(Json(original), Json(copy));
        }

        [Theory]
        [MemberData(nameof(OwnTestFiles))]
        public void OwnTestInvoice_RoundTripsAndStillPassesFederalRules(string file)
        {
            EbInvoice original = EbInterfaceReader.ReadFile(TestFile(file));

            byte[] xml = WriteToBytes(original);

            Assert.Equal(Json(original), Json(Read(xml)));
            using var stream = new MemoryStream(xml);
            ValidationResult result = EbInterfaceValidator.Validate(stream, ERechnung);
            Assert.True(result.Messages.Count == 0, Describe(result));
        }

        [Theory]
        [MemberData(nameof(OlderSamples))]
        public void OlderSample_IsUpgradedToValid6p1(string relativePath)
        {
            EbInvoice original = EbInterfaceReader.ReadFile(Path.Combine(StandardsRoot, relativePath));

            EbInvoice upgraded = RoundTrip(original);

            Assert.Equal(EbInterfaceVersion.V6p1, upgraded.SourceVersion);
            Assert.Equal(original.InvoiceNumber, upgraded.InvoiceNumber);
            Assert.Equal(original.PayableAmount, upgraded.PayableAmount);
            Assert.Equal(original.AllLineItems.Count(), upgraded.AllLineItems.Count());
            Assert.Equal(original.AllLineItems.Select(l => l.LineItemAmount), upgraded.AllLineItems.Select(l => l.LineItemAmount));
            Assert.Equal(original.TaxItems.Select(t => t.TaxAmount), upgraded.TaxItems.Select(t => t.TaxAmount));
            Assert.All(upgraded.AllLineItems, l => Assert.NotNull(l.TaxCategoryCode));
        }

        [Theory]
        [InlineData("http://www.ebinterface.at/schema/6p0/")]
        [InlineData("http://www.ebinterface.at/schema/5p0/")]
        public void Upgrade_From5p0And6p0_KeepsEverythingButVersion(string ns)
        {
            XDocument document = LoadTestData("6p1/gueltig-bestellnummer.xml").WithNamespace(ns);
            if (ns.EndsWith("5p0/"))
                document.Root!.SetAttributeValue("Language", "ger");
            EbInvoice original = Read(Encoding.UTF8.GetBytes(document.ToString()));

            EbInvoice upgraded = RoundTrip(original);

            Assert.Equal("de", upgraded.Language); // 5.0: „ger“ → 6.1: „de“
            Assert.Equal(Json(original, ignoreLanguage: true), Json(upgraded, ignoreLanguage: true));
        }

        [Fact]
        public void Upgrade_From4p3_DerivesTaxCategoryAndKeepsExemptionReason()
        {
            EbInvoice original = EbInterfaceReader.ReadFile(
                Path.Combine(StandardsRoot, "ebInterface4p3", "samples", "ebInterface_4p3_sample.xml"));

            EbInvoice upgraded = RoundTrip(original);

            LineItem exempt = upgraded.AllLineItems.First(l => l.TaxExemptionReason != null);
            Assert.Equal("E", exempt.TaxCategoryCode);
            Assert.Equal("Steuerfrei (gemäß § x UStG)", exempt.TaxExemptionReason);
            Assert.All(upgraded.AllLineItems.Where(l => l.TaxPercent > 0), l => Assert.Equal("S", l.TaxCategoryCode));
            Assert.Equal("Herr Karl-Heinz Riegler", upgraded.Biller.Contact!.Name);
        }

        [Fact]
        public void IncompleteInvoice_ReportsMissingValuesWithPath()
        {
            var invoice = new EbInvoice(); // ohne Rechnungsnummer, Datum, UID …

            var ex = Assert.Throws<EbInterfaceWriteException>(() => WriteToBytes(invoice));

            Assert.False(ex.Validation.IsValid);
            Assert.All(ex.Validation.Errors, e => Assert.Equal("WRT-01", e.Code));
            Assert.Contains(ex.Validation.Errors, e => e.Message.Contains("Invoice/InvoiceNumber"));
            Assert.Contains(ex.Validation.Errors, e => e.Message.Contains("Invoice/InvoiceDate"));
            Assert.Contains("wurde nicht geschrieben", ex.Message);
        }

        [Fact]
        public void ForgottenDiscountDate_IsNotWrittenAsYearOne()
        {
            EbInvoice invoice = EbInterfaceReader.ReadFile(TestFile("6p1/gueltig-bestellnummer.xml"));
            invoice.PaymentConditions!.Discounts.Add(new Discount { Percentage = 1m });

            var ex = Assert.Throws<EbInterfaceWriteException>(() => WriteToBytes(invoice));

            Assert.Equal("Pflichtangabe fehlt: Invoice/PaymentConditions/Discount/PaymentDate. Bitte im Modell befüllen.",
                Assert.Single(ex.Validation.Errors).Message);
        }

        [Fact]
        public void SchemaViolation_IsReportedAfterMissingValuesAreFilled()
        {
            EbInvoice invoice = EbInterfaceReader.ReadFile(TestFile("6p1/gueltig-bestellnummer.xml"));
            invoice.Currency = "EURO"; // nicht leer, aber zu lang

            var ex = Assert.Throws<EbInterfaceWriteException>(() => WriteToBytes(invoice));

            Assert.Equal("XSD-06", Assert.Single(ex.Validation.Errors).Code);
        }

        [Fact]
        public void ReadAndWriteErrors_ShareOneBaseException()
        {
            using var notEbInterface = new MemoryStream(Encoding.UTF8.GetBytes("<Order/>"));

            EbInterfaceException read = Assert.ThrowsAny<EbInterfaceException>(() => EbInterfaceReader.Read(notEbInterface));
            EbInterfaceException write = Assert.ThrowsAny<EbInterfaceException>(() => WriteToBytes(new EbInvoice()));

            Assert.IsType<EbInterfaceReadException>(read);
            Assert.IsType<EbInterfaceWriteException>(write);
        }

        [Fact]
        public void IncompleteInvoice_WriteFile_CreatesNoFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"ebinterface-{Guid.NewGuid():N}.xml");

            Assert.Throws<EbInterfaceWriteException>(() => EbInterfaceWriter.WriteFile(new EbInvoice(), path));

            Assert.False(File.Exists(path));
        }

        [Fact]
        public void DirectDebitFrom4p3_IsNotSupportedIn6p1()
        {
            EbInvoice invoice = EbInterfaceReader.ReadFile(TestFile("6p1/gueltig-bestellnummer.xml"));
            invoice.PaymentMethod = new DirectDebit();

            Assert.Throws<NotSupportedException>(() => WriteToBytes(invoice));
        }

        [Fact]
        public void Output_IsUtf8WithoutBom_AndIndented()
        {
            byte[] xml = WriteToBytes(EbInterfaceReader.ReadFile(TestFile("6p1/gueltig-bestellnummer.xml")));

            Assert.NotEqual(0xEF, xml[0]);
            string text = Encoding.UTF8.GetString(xml);
            Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", text);
            Assert.Contains("\n  <InvoiceNumber>T-2026-0001</InvoiceNumber>", text.Replace("\r\n", "\n"));
            Assert.Contains("Österreich", text);
        }

        private static byte[] WriteToBytes(EbInvoice invoice)
        {
            using var stream = new MemoryStream();
            EbInterfaceWriter.Write(invoice, stream);
            return stream.ToArray();
        }

        private static EbInvoice Read(byte[] xml)
        {
            using var stream = new MemoryStream(xml);
            return EbInterfaceReader.Read(stream);
        }

        private static EbInvoice RoundTrip(EbInvoice invoice) => Read(WriteToBytes(invoice));

        private static string Json(EbInvoice invoice, bool ignoreLanguage = false)
        {
            EbInterfaceVersion version = invoice.SourceVersion;
            string? language = invoice.Language;
            invoice.SourceVersion = EbInterfaceVersion.Unknown;
            if (ignoreLanguage) invoice.Language = null;
            try
            {
                return JsonSerializer.Serialize(invoice, new JsonSerializerOptions { WriteIndented = true });
            }
            finally
            {
                invoice.SourceVersion = version;
                invoice.Language = language;
            }
        }
    }
}
