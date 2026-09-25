using System.Xml.Linq;
using EbInterface.Validation;
using static EbInterface.Tests.TestSupport;

namespace EbInterface.Tests
{
    /// <summary>
    /// Regeln von e-Rechnung.gv.at: je Regel mindestens ein positiver und ein negativer Fall.
    /// Basis sind eigene Testrechnungen unter TestData/.
    /// </summary>
    public class ERechnungRuleTests
    {
        private const string OrderNumberFile = "6p1/gueltig-bestellnummer.xml";
        private const string BuyerGroupFile = "6p1/gueltig-einkaeufergruppe.xml";

        // --- Testdateien -------------------------------------------------------------------------

        [Theory]
        [InlineData("6p1/gueltig-bestellnummer.xml", "")]
        [InlineData("6p1/gueltig-einkaeufergruppe.xml", "")]
        [InlineData("6p1/gueltig-anderer-empfaenger.xml", "")]
        [InlineData("6p1/fehler-bestellnummer-ohne-position.xml", "ERB-05")]
        [InlineData("6p1/fehler-einkaeufergruppe-referenz-zu-lang.xml", "ERB-03")]
        [InlineData("6p1/fehler-anderer-empfaenger-ohne-schraegstrich.xml", "ERB-03")]
        public void TestFile_ProducesExactlyTheExpectedMessages(string file, string expectedCodes)
        {
            var result = EbInterfaceValidator.ValidateFile(TestFile(file), ERechnung);

            var expected = expectedCodes.Split(',', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(expected.SequenceEqual(result.Messages.Select(m => m.Code)), $"{file}:{Environment.NewLine}{Describe(result)}");
        }

        [Theory]
        [InlineData("http://www.ebinterface.at/schema/6p0/")]
        [InlineData("http://www.ebinterface.at/schema/5p0/")]
        public void ValidTestFile_IsAlsoValidAsOlderVersion(string ns)
        {
            var document = LoadTestData(OrderNumberFile).WithNamespace(ns);
            if (ns.EndsWith("5p0/"))
                document.Root!.SetAttributeValue("Language", "ger"); // 5.0 verwendet dreistellige Sprachcodes

            var result = Validate(document, ERechnung);

            Assert.True(result.Messages.Count == 0, Describe(result));
        }

        // --- ERB-01 Dokumenttyp ------------------------------------------------------------------

        [Theory]
        [InlineData("Invoice", true)]
        [InlineData("CreditMemo", true)]
        [InlineData("FinalSettlement", true)]
        [InlineData("SelfBilling", false)]
        [InlineData("SubsequentDebit", false)]
        public void Erb01_DocumentType(string documentType, bool allowed)
        {
            var document = LoadTestData(OrderNumberFile);
            document.Root!.SetAttributeValue("DocumentType", documentType);

            AssertCode(Validate(document, ERechnung), "ERB-01", !allowed);
        }

        // --- ERB-02/03 Auftragsreferenz ----------------------------------------------------------

        [Fact]
        public void Erb02_MissingOrderReference()
        {
            var document = LoadTestData(BuyerGroupFile);
            document.El("OrderReference").Remove();

            AssertCode(Validate(document, ERechnung), "ERB-02", true);
        }

        [Theory]
        // Bund: Bestellnummer, Einkäufergruppe, Einkäufergruppe:Referenz
        [InlineData("4700000001", true)]
        [InlineData("Z01", true)]
        [InlineData("63Ü", true)]
        [InlineData("Z01:111599", true)]
        [InlineData("Z01:12345678901234567890123456789012345678901234567890", true)] // 50 Zeichen
        [InlineData("Z01:123456789012345678901234567890123456789012345678901", false)] // 51 Zeichen
        [InlineData("470000001", false)] // 9 Ziffern
        [InlineData("47000000011", false)] // 11 Ziffern
        [InlineData("Z0", false)]
        [InlineData("Z012", false)]
        [InlineData("Z-1", false)]
        // Andere Empfänger
        [InlineData("Z0/", true)]
        [InlineData("Z0/Aktenzahl 2026-123", true)]
        [InlineData("Z0/12345678901234567890123456789012345678901234567890", true)] // 50 Zeichen
        [InlineData("Z0/123456789012345678901234567890123456789012345678901", false)] // 51 Zeichen
        [InlineData("Z/", false)]
        [InlineData("/Aktenzahl", false)]
        [InlineData("Z0 /Aktenzahl", false)]
        public void Erb03_OrderReferenceFormat(string orderId, bool valid)
        {
            var document = LoadTestData(BuyerGroupFile);
            document.El("OrderID").Value = orderId;

            AssertCode(Validate(document, ERechnung), "ERB-03", !valid);
        }

        // --- ERB-04/05/06 Bestellung und Positionen ----------------------------------------------

        [Fact]
        public void Erb04_LinesReferToDifferentOrders()
        {
            var document = LoadTestData(OrderNumberFile);
            document.Descendants(document.N("InvoiceRecipientsOrderReference")).Last()
                .Element(document.N("OrderID"))!.Value = "4700000002";

            AssertCode(Validate(document, ERechnung), "ERB-04", true);
        }

        [Fact]
        public void Erb04_LinesReferToOtherOrderThanHeader()
        {
            var document = LoadTestData(OrderNumberFile);
            foreach (XElement id in document.Descendants(document.N("InvoiceRecipientsOrderReference")).Elements(document.N("OrderID")))
                id.Value = "4700000002";

            AssertCode(Validate(document, ERechnung), "ERB-04", true);
        }

        [Theory]
        [InlineData("10", false)]
        [InlineData("10a", true)]
        [InlineData("", true)]
        public void Erb05_OrderPositionNumberMustBeNumeric(string position, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            document.El("OrderPositionNumber").Value = position;

            AssertCode(Validate(document, ERechnung), "ERB-05", expectError);
        }

        [Fact]
        public void Erb05_NotRequiredForBuyerGroup()
        {
            AssertCode(EbInterfaceValidator.ValidateFile(TestFile(BuyerGroupFile), ERechnung), "ERB-05", false);
        }

        [Fact]
        public void Erb06_LineReferenceDiffersFromBuyerGroup_IsOnlyAWarning()
        {
            var document = LoadTestData(BuyerGroupFile);
            XNamespace ns = document.Root!.Name.Namespace;
            foreach (XElement unitPrice in document.Descendants(ns + "UnitPrice"))
                unitPrice.AddAfterSelf(new XElement(ns + "InvoiceRecipientsOrderReference", new XElement(ns + "OrderID", "Z02")));

            var result = Validate(document, ERechnung);

            Assert.True(result.IsValid, Describe(result));
            Assert.Contains(result.Messages, m => m.Code == "ERB-06" && m.Severity == ValidationSeverity.Warning);
        }

        // --- ERB-07/08/09 Rechnungssteller -------------------------------------------------------

        [Fact]
        public void Erb07_MissingSupplierNumber()
        {
            var document = LoadTestData(OrderNumberFile);
            document.El("InvoiceRecipientsBillerID").Remove();

            AssertCode(Validate(document, ERechnung), "ERB-07", true);
        }

        [Fact]
        public void Erb08_MissingBillerEmail()
        {
            var document = LoadTestData(OrderNumberFile);
            document.El("Email").Remove();

            AssertCode(Validate(document, ERechnung), "ERB-08", true);
        }

        [Fact]
        public void Erb09_MissingCompanyRegisterData_IsOnlyAWarning()
        {
            var document = LoadTestData(OrderNumberFile);
            document.Descendants(document.N("FurtherIdentification"))
                .Where(e => (string?)e.Attribute("IdentificationType") != "FS").Remove();

            var result = Validate(document, ERechnung);

            Assert.True(result.IsValid, Describe(result));
            var warning = Assert.Single(result.Messages);
            Assert.Equal("ERB-09", warning.Code);
            Assert.Contains("FN, FBG", warning.Message);
        }

        // --- ERB-10 Zeilenanzahl -----------------------------------------------------------------

        [Theory]
        [InlineData(999, false)]
        [InlineData(1000, true)]
        public void Erb10_LineLimit(int lines, bool expectError)
        {
            var document = LoadTestData(BuyerGroupFile);
            SetLineCount(document, lines);

            AssertCode(Validate(document, ERechnung), "ERB-10", expectError);
        }

        [Theory]
        [InlineData("http://www.ebinterface.at/schema/6p1/", true)] // 6.1 zählt Below-The-Line-Zeilen mit
        [InlineData("http://www.ebinterface.at/schema/6p0/", false)] // 6.0 nur Rechnungszeilen
        public void Erb10_BelowTheLineItemsCountOnlyIn6p1(string ns, bool expectError)
        {
            var document = LoadTestData(BuyerGroupFile);
            SetLineCount(document, 999);
            XNamespace n = document.Root!.Name.Namespace;
            document.El("Details").Add(new XElement(n + "BelowTheLineItem",
                new XElement(n + "Description", "Pfand"), new XElement(n + "LineItemAmount", "0.00")));

            AssertCode(Validate(document.WithNamespace(ns), ERechnung), "ERB-10", expectError);
        }

        // --- ERB-11/12 Skonto --------------------------------------------------------------------

        [Theory]
        [InlineData(2, false)]
        [InlineData(3, true)]
        public void Erb11_AtMostTwoDiscounts(int count, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            XElement discount = document.El("Discount");
            for (int i = 1; i < count; i++)
                discount.AddAfterSelf(new XElement(discount));

            AssertCode(Validate(document, ERechnung), "ERB-11", expectError);
        }

        [Theory]
        [InlineData("0.01", false)]
        [InlineData("99.99", false)]
        [InlineData("0", true)]
        [InlineData("100", true)]
        public void Erb12_DiscountPercentage(string percentage, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            document.El("Percentage").Value = percentage;

            AssertCode(Validate(document, ERechnung), "ERB-12", expectError);
        }

        [Fact]
        public void Erb12_DiscountWithAmountInsteadOfPercentage_IsAllowed()
        {
            var document = LoadTestData(OrderNumberFile);
            XElement percentage = document.El("Percentage");
            percentage.ReplaceWith(new XElement(document.N("Amount"), "4.80"));

            var result = Validate(document, ERechnung);

            Assert.True(result.Messages.Count == 0, Describe(result));
        }

        // --- ERB-13 bis ERB-19 Zahlung -----------------------------------------------------------

        [Fact]
        public void Erb13_PaymentCardIsNotSupported()
        {
            var document = LoadTestData(OrderNumberFile);
            XNamespace ns = document.Root!.Name.Namespace;
            document.El("UniversalBankTransaction").ReplaceWith(
                new XElement(ns + "PaymentCard", new XElement(ns + "PrimaryAccountNumber", "1234")));

            AssertCode(Validate(document, ERechnung), "ERB-13", true);
        }

        [Theory]
        [InlineData("Invoice", true)]
        [InlineData("CreditMemo", false)]
        public void Erb14_PaymentMethodRequiredExceptForCreditMemo(string documentType, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            document.Root!.SetAttributeValue("DocumentType", documentType);
            document.El("PaymentMethod").Remove();

            AssertCode(Validate(document, ERechnung), "ERB-14", expectError);
        }

        [Fact]
        public void Erb15_ExactlyOneBeneficiaryAccount()
        {
            var document = LoadTestData(OrderNumberFile);
            XElement account = document.El("BeneficiaryAccount");
            account.AddAfterSelf(new XElement(account));

            AssertCode(Validate(document, ERechnung), "ERB-15", true);
        }

        [Fact]
        public void Erb16_IbanRequired()
        {
            var document = LoadTestData(OrderNumberFile);
            XElement iban = document.El("IBAN");
            iban.ReplaceWith(new XElement(document.N("BankAccountNr"), "234573201"));

            AssertCode(Validate(document, ERechnung), "ERB-16", true);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Erb17_SepaDirectDebitRequiredFields(bool complete)
        {
            var document = LoadTestData(OrderNumberFile);
            XNamespace ns = document.Root!.Name.Namespace;
            var sepa = new XElement(ns + "SEPADirectDebit",
                new XElement(ns + "Type", "B2B"),
                new XElement(ns + "IBAN", "AT611904300234573201"),
                new XElement(ns + "BankAccountOwner", "Beispielamt"),
                new XElement(ns + "CreditorID", "AT12ZZZ00000000001"),
                new XElement(ns + "MandateReference", "M-1"),
                complete ? new XElement(ns + "DebitCollectionDate", "2026-10-01") : null);
            document.El("UniversalBankTransaction").ReplaceWith(sepa);

            var result = Validate(document, ERechnung);

            AssertCode(result, "ERB-17", !complete);
            if (!complete)
                Assert.Contains("DebitCollectionDate", result.Errors.Single().Message);
        }

        [Theory]
        [InlineData(999, false)]
        [InlineData(1000, true)]
        public void Erb18_DueDateAtMost999DaysAhead(int days, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            document.El("DueDate").Value = ReferenceDate.AddDays(days).ToString("yyyy-MM-dd");

            AssertCode(Validate(document, ERechnung), "ERB-18", expectError);
        }

        [Theory]
        [InlineData("Invoice", "240.00", true)]
        [InlineData("Invoice", "0.00", false)]
        [InlineData("CreditMemo", "240.00", false)]
        public void Erb19_NoPaymentOnlyForCreditMemoOrZero(string documentType, string payable, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            document.Root!.SetAttributeValue("DocumentType", documentType);
            document.El("PayableAmount").Value = payable;
            document.El("UniversalBankTransaction").ReplaceWith(new XElement(document.N("NoPayment")));

            AssertCode(Validate(document, ERechnung), "ERB-19", expectError);
        }

        // --- ERB-20 Lieferbeschreibung -----------------------------------------------------------

        [Theory]
        [InlineData(500, false)]
        [InlineData(501, true)]
        public void Erb20_DeliveryDescriptionLength(int length, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            document.El("Delivery").Add(new XElement(document.N("Description"), new string('x', length)));

            AssertCode(Validate(document, ERechnung), "ERB-20", expectError);
        }

        // --- ERB-21 BaseQuantity -----------------------------------------------------------------

        [Theory]
        [InlineData("20", "4.50", "1", false)]
        [InlineData("20", "4.50", "3", false)]     // 20/3 zu genau, aber 4.50/3 = 1.5 genügt
        [InlineData("10", "1.00", "4", false)]     // 2.5 und 0.25
        [InlineData("10.0000", "1.0000", "1.0000", false)] // abschließende Nullen zählen nicht
        [InlineData("10", "1.00", "3", true)]      // 3.333… und 0.333…
        public void Erb21_BaseQuantityPrecision(string quantity, string unitPrice, string baseQuantity, bool expectError)
        {
            var document = LoadTestData(OrderNumberFile);
            document.El("Quantity").Value = quantity;
            XElement price = document.El("UnitPrice");
            price.Value = unitPrice;
            price.SetAttributeValue("BaseQuantity", baseQuantity);

            AssertCode(Validate(document, ERechnung), "ERB-21", expectError);
        }

        // --- ERB-30 ff. nicht ausgewertete Felder ------------------------------------------------

        [Fact]
        public void IgnoredElements_AreReportedOnceAsWarnings()
        {
            var document = LoadTestData(OrderNumberFile);
            XNamespace ns = document.Root!.Name.Namespace;
            foreach (XElement name in document.Descendants(ns + "Address").Elements(ns + "Name"))
                name.AddAfterSelf(new XElement(ns + "TradingName", "Handelsname"));
            document.El("Discount").AddAfterSelf(new XElement(ns + "MinimumPayment", "10.00"));
            document.El("TotalGrossAmount").AddAfterSelf(new XElement(ns + "PrepaidAmount", "0.00"));

            var result = Validate(document, ERechnung);

            Assert.True(result.IsValid, Describe(result));
            Assert.Equal(new[] { "ERB-30", "ERB-33", "ERB-35" }, result.Messages.Select(m => m.Code).ToArray());
            Assert.All(result.Messages, m => Assert.Equal(ValidationSeverity.Warning, m.Severity));
        }

        // --- Hilfen ------------------------------------------------------------------------------

        private static void AssertCode(ValidationResult result, string code, bool expected)
        {
            bool found = result.Messages.Any(m => m.Code == code);
            Assert.True(found == expected,
                $"{code} {(expected ? "erwartet, aber nicht gemeldet" : "unerwartet gemeldet")}:{Environment.NewLine}{Describe(result)}");
        }

        private static void SetLineCount(XDocument document, int count)
        {
            XElement template = document.El("ListLineItem");
            XElement itemList = template.Parent!;
            itemList.Elements(document.N("ListLineItem")).Remove();
            for (int i = 0; i < count; i++)
                itemList.Add(new XElement(template));
        }
    }
}
