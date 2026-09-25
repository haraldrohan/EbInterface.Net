using System.Xml.Linq;
using EbInterface.Validation;
using static EbInterface.Tests.TestSupport;

namespace EbInterface.Tests
{
    /// <summary>
    /// Rechenprüfungen (ERB-26 bis ERB-29, ERB-39). Jeder Fall wurde am 26.09.2026 so auch in den Test-Upload von
    /// e-Rechnung.gv.at geladen; das dortige Ergebnis steht im Kommentar. Basis: 2 Zeilen (20 × 4,50 und 2 × 55,00),
    /// 20 % USt, netto 200,00, Steuer 40,00, zu zahlen 240,00.
    /// </summary>
    public class ERechnungArithmeticTests
    {
        private const string BaseFile = "6p1/gueltig-bestellnummer.xml";

        [Theory]
        [InlineData("0.05", false)] // Portal: angenommen
        [InlineData("0.10", true)]  // Portal: AF-0026 (brutto 0,12 daneben)
        [InlineData("0.50", true)]  // Portal: AF-0025
        public void Erb26_LineAmountTolerance(string plus, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            decimal delta = decimal.Parse(plus, System.Globalization.CultureInfo.InvariantCulture);
            SetFirstLineAmount(document, 90.00m + delta);

            AssertCode(Validate(document, ERechnung), "ERB-26", expectError);
        }

        [Theory]
        [InlineData(true, false)]  // Portal: angenommen (90 − 9 = 81)
        [InlineData(false, true)]  // Portal: AF-0089 „Anzahl * Nettoeinzelpreis - Abschläge + Aufschläge“
        public void Erb26_LineReductionIsDeducted(bool deducted, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            XNamespace ns = document.Root!.Name.Namespace;
            document.El("UnitPrice").AddAfterSelf(new XElement(ns + "ReductionAndSurchargeListLineItemDetails",
                new XElement(ns + "ReductionListLineItem",
                    new XElement(ns + "BaseAmount", "90.00"),
                    new XElement(ns + "Percentage", "10"),
                    new XElement(ns + "Amount", "9.00"))));
            if (deducted)
            {
                SetFirstLineAmount(document, 81.00m);
                SetTotals(document, taxable: 191.00m, tax: 38.20m, payable: 229.20m);
            }

            AssertCode(Validate(document, ERechnung), "ERB-26", expectError);
        }

        [Theory]
        [InlineData(240.10, false)] // Portal: angenommen
        [InlineData(240.20, false)] // Portal: angenommen (2 Zeilen: Toleranz 0,20)
        [InlineData(240.25, true)]  // Portal: AF-0028
        [InlineData(240.50, true)]  // Portal: AF-0028
        [InlineData(241.00, true)]  // Portal: AF-0028
        public void Erb27_PayableAmountTolerance(double payable, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            document.El("PayableAmount").Value = ((decimal)payable).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            AssertCode(Validate(document, ERechnung), "ERB-27", expectError);
        }

        [Theory]
        [InlineData("108.10", false)]
        [InlineData("108.15", true)] // Portal: AF-0028 (1 Zeile: Toleranz 0,10)
        public void Erb27_ToleranceWithSingleLine(string payable, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            document.Descendants(document.N("ListLineItem")).Last().Remove();
            SetTotals(document, taxable: 90.00m, tax: 18.00m, payable: 108.00m);
            document.El("PayableAmount").Value = payable;

            AssertCode(Validate(document, ERechnung), "ERB-27", expectError);
        }

        [Theory]
        [InlineData(245.00, false)] // Portal: angenommen
        [InlineData(240.00, true)]  // Portal: AF-0028 (erwartet 245)
        public void Erb27_BelowTheLineItemIsPartOfPayableAmount(double payable, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            XNamespace ns = document.Root!.Name.Namespace;
            document.El("Details").Add(new XElement(ns + "BelowTheLineItem",
                new XElement(ns + "Description", "Pfand"), new XElement(ns + "LineItemAmount", "5.00")));
            document.El("PayableAmount").Value = ((decimal)payable).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            AssertCode(Validate(document, ERechnung), "ERB-27", expectError);
        }

        [Theory]
        [InlineData(240.00, false)] // Portal: angenommen
        [InlineData(200.00, true)]  // Portal: AF-0028 – PrepaidAmount wird nicht abgezogen
        public void Erb27_PrepaidAmountIsNotDeducted(double payable, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            document.El("TotalGrossAmount").AddAfterSelf(new XElement(document.N("PrepaidAmount"), "40.00"));
            document.El("PayableAmount").Value = ((decimal)payable).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            AssertCode(Validate(document, ERechnung), "ERB-27", expectError);
        }

        [Fact]
        public void Erb27_HeaderSurchargeIsIncludedGross()
        {
            // Portal: angenommen (10,00 netto + 20 % = 12,00 zusätzlich)
            var document = LoadTestData(BaseFile);
            XNamespace ns = document.Root!.Name.Namespace;
            document.El("Tax").AddBeforeSelf(new XElement(ns + "ReductionAndSurchargeDetails",
                new XElement(ns + "Surcharge",
                    new XElement(ns + "BaseAmount", "200.00"),
                    new XElement(ns + "Amount", "10.00"),
                    new XElement(ns + "Comment", "Versand"),
                    new XElement(ns + "TaxItem",
                        new XElement(ns + "TaxableAmount", "10.00"),
                        new XElement(ns + "TaxPercent", new XAttribute("TaxCategoryCode", "S"), "20")))));
            SetTotals(document, taxable: 210.00m, tax: 42.00m, payable: 252.00m);

            var result = Validate(document, ERechnung);

            Assert.True(result.IsValid, Describe(result));
        }

        [Theory]
        [InlineData("-0.05", true)] // Portal: AF-0122 „muss zwischen -0,02 und 0,02 liegen“
        [InlineData("0.02", false)]
        public void Erb28_RoundingAmountRange(string rounding, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            document.El("TotalGrossAmount").AddAfterSelf(new XElement(document.N("RoundingAmount"), rounding));

            AssertCode(Validate(document, ERechnung), "ERB-28", expectError);
        }

        [Theory]
        [InlineData("200.05", false)] // Portal: angenommen
        [InlineData("201.00", true)]  // Portal: AF-0032
        public void Erb29_TaxableAmountMatchesLines(string taxable, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            document.Element(document.N("Invoice"))!.Element(document.N("Tax"))!.Element(document.N("TaxItem"))!
                .Element(document.N("TaxableAmount"))!.Value = taxable;

            var result = Validate(document, ERechnung);

            AssertCode(result, "ERB-29", expectError);
            AssertCode(result, "ERB-39", false); // Portal meldet hier kein AF-0033
        }

        [Theory]
        [InlineData("40.05", false)] // Portal: angenommen
        [InlineData("41.00", true)]  // Portal: AF-0033
        public void Erb39_TaxAmountMatchesRate(string taxAmount, bool expectError)
        {
            var document = LoadTestData(BaseFile);
            document.El("TaxAmount").Value = taxAmount;

            AssertCode(Validate(document, ERechnung), "ERB-39", expectError);
        }

        private static void SetFirstLineAmount(XDocument document, decimal amount)
        {
            XElement line = document.El("ListLineItem");
            string text = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            line.Element(document.N("LineItemAmount"))!.Value = text;
            line.Element(document.N("TaxItem"))!.Element(document.N("TaxableAmount"))!.Value = text;
        }

        private static void SetTotals(XDocument document, decimal taxable, decimal tax, decimal payable)
        {
            XElement taxItem = document.Root!.Element(document.N("Tax"))!.Element(document.N("TaxItem"))!;
            taxItem.Element(document.N("TaxableAmount"))!.Value = taxable.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            taxItem.Element(document.N("TaxAmount"))!.Value = tax.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            document.El("TotalGrossAmount").Value = payable.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            document.El("PayableAmount").Value = payable.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static void AssertCode(ValidationResult result, string code, bool expected)
        {
            bool found = result.Messages.Any(m => m.Code == code);
            Assert.True(found == expected,
                $"{code} {(expected ? "erwartet, aber nicht gemeldet" : "unerwartet gemeldet")}:{Environment.NewLine}{Describe(result)}");
        }
    }
}
