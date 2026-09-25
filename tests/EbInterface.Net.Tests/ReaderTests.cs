using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using EbInterface.Model;
using static EbInterface.Tests.TestSupport;

namespace EbInterface.Tests
{
    public class ReaderTests
    {
        public static IEnumerable<object[]> ValidSamples() =>
            OfficialSampleTests.Samples("4p3,5p0,6p0,6p1").Where(s => !((string)s[0]).Contains("_invalid"));

        [Theory]
        [MemberData(nameof(ValidSamples))]
        public void OfficialSample_IsReadCompletely(string relativePath)
        {
            string path = Path.Combine(StandardsRoot, relativePath);
            XDocument xml = XDocument.Load(path);
            XNamespace ns = xml.Root!.Name.Namespace;

            EbInvoice invoice = EbInterfaceReader.ReadFile(path);

            Assert.NotEqual(EbInterfaceVersion.Unknown, invoice.SourceVersion);
            Assert.Equal(xml.Root.Element(ns + "InvoiceNumber")!.Value, invoice.InvoiceNumber);
            Assert.Equal(xml.Descendants(ns + "ListLineItem").Count(), invoice.AllLineItems.Count());
            Assert.Equal(decimal.Parse(xml.Root.Element(ns + "PayableAmount")!.Value, System.Globalization.CultureInfo.InvariantCulture), invoice.PayableAmount);
            Assert.NotEmpty(invoice.Biller.VatIdentificationNumber);
        }

        [Fact]
        public void TestInvoice6p1_AllMainFieldsAreRead()
        {
            EbInvoice invoice = EbInterfaceReader.ReadFile(TestFile("6p1/gueltig-bestellnummer.xml"));

            Assert.Equal(EbInterfaceVersion.V6p1, invoice.SourceVersion);
            Assert.Equal("EbInterface.Net Tests", invoice.GeneratingSystem);
            Assert.Equal(DocumentType.Invoice, invoice.DocumentType);
            Assert.Equal("EUR", invoice.Currency);
            Assert.Equal("de", invoice.Language);
            Assert.Equal("T-2026-0001", invoice.InvoiceNumber);
            Assert.Equal(new DateTime(2026, 9, 1), invoice.InvoiceDate);
            Assert.Equal(new DateTime(2026, 8, 28), invoice.Delivery!.Date);

            Biller biller = invoice.Biller;
            Assert.Equal("ATU12345675", biller.VatIdentificationNumber);
            Assert.Equal("50012345", biller.InvoiceRecipientsBillerId);
            Assert.Equal(new[] { "FS:Linz", "FN:123456a", "FBG:Landesgericht Linz" },
                biller.FurtherIdentifications.Select(f => $"{f.IdentificationType}:{f.Value}"));
            Assert.Equal("Beispiel Handels GmbH", biller.Address!.Name);
            Assert.Equal("4020", biller.Address.Zip);
            Assert.Equal("AT", biller.Address.CountryCode);
            Assert.Equal("Österreich", biller.Address.CountryName);
            Assert.Equal(new[] { "rechnung@example.org" }, biller.Address.Emails);

            Assert.Equal("4700000001", invoice.InvoiceRecipient.OrderReference!.OrderId);

            LineItem first = invoice.AllLineItems.First();
            Assert.Equal(new[] { "Druckerpapier A4, 500 Blatt" }, first.Descriptions);
            Assert.Equal(20m, first.Quantity);
            Assert.Equal("STK", first.Unit);
            Assert.Equal(4.50m, first.UnitPrice);
            Assert.Null(first.BaseQuantity);
            Assert.Equal("4700000001", first.InvoiceRecipientsOrderReference!.OrderId);
            Assert.Equal("10", first.InvoiceRecipientsOrderReference.OrderPositionNumber);
            Assert.Equal(90.00m, first.TaxableAmount);
            Assert.Equal(20m, first.TaxPercent);
            Assert.Equal("S", first.TaxCategoryCode);
            Assert.Equal(90.00m, first.LineItemAmount);

            TaxItem tax = Assert.Single(invoice.TaxItems);
            Assert.Equal(200.00m, tax.TaxableAmount);
            Assert.Equal(40.00m, tax.TaxAmount);
            Assert.Equal(240.00m, invoice.TotalGrossAmount);
            Assert.Equal(240.00m, invoice.PayableAmount);

            var transfer = Assert.IsType<UniversalBankTransaction>(invoice.PaymentMethod);
            Assert.Equal("AT611904300234573201", Assert.Single(transfer.BeneficiaryAccounts).Iban);
            Assert.Equal(new DateTime(2026, 10, 31), invoice.PaymentConditions!.DueDate);
            Discount discount = Assert.Single(invoice.PaymentConditions.Discounts);
            Assert.Equal(new DateTime(2026, 10, 15), discount.PaymentDate);
            Assert.Equal(2.00m, discount.Percentage);
        }

        [Theory]
        [InlineData("http://www.ebinterface.at/schema/6p0/")]
        [InlineData("http://www.ebinterface.at/schema/5p0/")]
        public void SameInvoiceInOlderVersion_GivesSameModel(string ns)
        {
            XDocument document = LoadTestData("6p1/gueltig-bestellnummer.xml").WithNamespace(ns);
            if (ns.EndsWith("5p0/"))
                document.Root!.SetAttributeValue("Language", "ger");

            EbInvoice expected = EbInterfaceReader.ReadFile(TestFile("6p1/gueltig-bestellnummer.xml"));
            EbInvoice actual = Read(document);

            Assert.Equal(Normalize(expected), Normalize(actual));
        }

        [Fact]
        public void TestInvoice4p3_IsMappedToTheSameModel()
        {
            EbInvoice invoice = EbInterfaceReader.ReadFile(TestFile("4p3/gueltig-bestellnummer.xml"));

            Assert.Equal(EbInterfaceVersion.V4p3, invoice.SourceVersion);
            Assert.Equal(DocumentType.Invoice, invoice.DocumentType); // qualifiziertes Attribut eb:DocumentType
            Assert.Equal("EUR", invoice.Currency);
            Assert.Equal(new[] { "FS", "FN", "FBG" }, invoice.Biller.FurtherIdentifications.Select(f => f.IdentificationType));

            LineItem first = invoice.AllLineItems.First();
            Assert.Equal(20m, first.TaxPercent);     // aus VATRate
            Assert.Null(first.TaxCategoryCode);      // gibt es in 4.3 nicht
            Assert.Equal("10", first.InvoiceRecipientsOrderReference!.OrderPositionNumber);

            TaxItem tax = Assert.Single(invoice.TaxItems); // aus Tax/VAT/VATItem
            Assert.Equal(200.00m, tax.TaxableAmount);
            Assert.Equal(20m, tax.TaxPercent);
            Assert.Equal(40.00m, tax.TaxAmount);
        }

        [Fact]
        public void Official4p3Sample_ContactAndTaxExemptionAreMapped()
        {
            string path = Path.Combine(StandardsRoot, "ebInterface4p3", "samples", "ebInterface_4p3_sample.xml");
            XDocument xml = XDocument.Load(path);
            XNamespace ns = xml.Root!.Name.Namespace;

            EbInvoice invoice = EbInterfaceReader.ReadFile(path);

            string? contactInXml = xml.Root.Element(ns + "Biller")!.Element(ns + "Address")!.Element(ns + "Contact")?.Value;
            Assert.Equal(contactInXml, invoice.Biller.Contact?.Name);

            Assert.Equal("Herr Karl-Heinz Riegler", contactInXml);
            Assert.Equal("Firma", invoice.Biller.Contact!.Salutation);

            int exemptionsInXml = xml.Descendants(ns + "ListLineItem").Count(l => l.Element(ns + "TaxExemption") != null);
            Assert.Equal(exemptionsInXml, invoice.AllLineItems.Count(l => l.TaxExemptionReason != null));

            LineItem exempt = invoice.AllLineItems.First(l => l.TaxExemptionReason != null);
            Assert.Equal("S69", exempt.TaxExemptionCode);
            Assert.Equal("Steuerfrei (gemäß § x UStG)", exempt.TaxExemptionReason);
            Assert.Equal(0m, exempt.TaxPercent);
            Assert.Equal(new[] { "BillersArticleNumber:BAN 123", "InvoiceRecipientsArticleNumber:SAN 345", "PZN:PZN39483", "GTIN:54563256987542" }, exempt.ArticleNumbers.Select(a => $"{a.Type}:{a.Value}"));
            Assert.Equal("kg", exempt.Unit);
            Assert.Equal(1m, exempt.BaseQuantity);
            ReductionOrSurcharge reduction = exempt.ReductionsAndSurcharges.First();
            Assert.Equal(ReductionOrSurchargeKind.Reduction, reduction.Kind);
            Assert.Equal(241.89m, reduction.BaseAmount);
        }

        [Fact]
        public void InvalidDocument_ThrowsWithValidationResult()
        {
            string path = Path.Combine(StandardsRoot, "ebInterface6p1", "samples", "ebinterface_6p1_sample_ecosio_invalid.xml");

            var ex = Assert.Throws<EbInterfaceReadException>(() => EbInterfaceReader.ReadFile(path));

            Assert.False(ex.Validation.IsValid);
            Assert.StartsWith("XSD-", ex.Validation.Errors.First().Code);
            Assert.Contains("[XSD-", ex.Message);
        }

        [Fact]
        public void NotEbInterface_Throws()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<Order/>"));

            var ex = Assert.Throws<EbInterfaceReadException>(() => EbInterfaceReader.Read(stream));

            Assert.Equal("VER-01", ex.Validation.Errors.Single().Code);
        }

        private static EbInvoice Read(XDocument document)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToString()));
            return EbInterfaceReader.Read(stream);
        }

        /// <summary>Modell als JSON, ohne die versionsabhängigen Angaben.</summary>
        private static string Normalize(EbInvoice invoice)
        {
            invoice.SourceVersion = EbInterfaceVersion.Unknown;
            invoice.Language = null;
            return JsonSerializer.Serialize(invoice, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
