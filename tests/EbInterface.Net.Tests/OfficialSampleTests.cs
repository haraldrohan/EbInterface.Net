using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EbInterface.Validation;
using Xunit;

namespace EbInterface.Tests
{
    public class OfficialSampleTests
    {
        private static readonly string SamplesRoot = Path.Combine(AppContext.BaseDirectory, "standards");

        public static IEnumerable<object[]> Samples() =>
            new[] { "5p0", "6p0", "6p1" }
                .SelectMany(v => Directory.GetFiles(Path.Combine(SamplesRoot, $"ebInterface{v}", "samples"), "*.xml"))
                .Select(f => new object[] { Path.GetRelativePath(SamplesRoot, f) });

        [Theory]
        [MemberData(nameof(Samples))]
        public void OfficialSample_IsSchemaValid_UnlessMarkedInvalid(string relativePath)
        {
            var result = EbInterfaceValidator.ValidateFile(Path.Combine(SamplesRoot, relativePath));
            bool expectedSchemaValid = !relativePath.Contains("_invalid");
            bool schemaValid = !result.Errors.Any(message => message.Code == "XSD-01");

            Assert.True(expectedSchemaValid == schemaValid,
                $"{relativePath}: erwartet {(expectedSchemaValid ? "XSD-gültig" : "XSD-ungültig")}, Meldungen:\n" +
                string.Join("\n", result.Messages));
        }

        [Theory]
        [MemberData(nameof(Samples))]
        public void OfficialSample_VersionIsDetectedFromFolder(string relativePath)
        {
            string folder = relativePath.Split(Path.DirectorySeparatorChar)[0]; // z. B. "ebInterface6p1"
            var expected = (EbInterfaceVersion)Enum.Parse(typeof(EbInterfaceVersion), "V" + folder.Substring("ebInterface".Length));

            using var stream = File.OpenRead(Path.Combine(SamplesRoot, relativePath));
            Assert.Equal(expected, EbInterfaceDetector.DetectVersion(stream));
        }

        [Fact]
        public void InvalidSample_ReportsLineAndSchemaCode()
        {
            var result = EbInterfaceValidator.ValidateFile(
                Path.Combine(SamplesRoot, "ebInterface6p1", "samples", "ebinterface_6p1_sample_ecosio_invalid.xml"));

            var error = Assert.Single(result.Errors);
            Assert.Equal("XSD-01", error.Code);
            Assert.True(error.Line > 0);
        }
    }

    public class ERechnungValidationTests
    {
        private static readonly string SamplePath = Path.Combine(
            AppContext.BaseDirectory,
            "standards",
            "ebInterface6p1",
            "samples",
            "ebinterface_6p1_sample_ph1.xml");

        [Fact]
        public void DisallowedDocumentType_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            invoice.SetAttributeValue("DocumentType", "SelfBilling");

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-01");
        }

        [Fact]
        public void MissingRecipientOrderReference_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement recipient = invoice.Element(ns + "InvoiceRecipient") ?? throw new InvalidOperationException();
            XElement orderReference = recipient.Element(ns + "OrderReference") ?? throw new InvalidOperationException();
            orderReference.Remove();

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-02");
        }

        [Fact]
        public void InvalidRecipientOrderReference_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement orderId = GetRecipientOrderId(document);
            orderId.Value = "Z0";

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-02");
        }

        [Fact]
        public void BuyerGroupOrderReference_IsAccepted()
        {
            var document = XDocument.Load(SamplePath);
            XElement orderId = GetRecipientOrderId(document);
            orderId.Value = "Z01";

            var result = Validate(document);

            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-02");
            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-07");
        }

        [Fact]
        public void FederalOrderNumberWithoutLineReference_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement orderId = GetRecipientOrderId(document);
            orderId.Value = "4700000001";

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-07");
        }

        [Fact]
        public void MissingBillerRegisteredOffice_ReportsErbCode()
        {
            var result = Validate(XDocument.Load(SamplePath));

            Assert.Contains(result.Errors, message => message.Code == "ERB-03");
        }

        [Fact]
        public void BillerRegisteredOffice_IsAccepted()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement biller = invoice.Element(ns + "Biller") ?? throw new InvalidOperationException();
            biller.Add(new XElement(
                ns + "FurtherIdentification",
                new XAttribute("IdentificationType", "FS"),
                "FN 123456a"));

            var result = Validate(document);

            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-03");
        }

        [Fact]
        public void BaseQuantityWithMoreThanFourDecimalPlaces_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement unitPrice = invoice.Descendants(ns + "UnitPrice").First();
            unitPrice.SetAttributeValue("BaseQuantity", "3");
            unitPrice.Value = "1";

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-04");
        }

        [Fact]
        public void BaseQuantityWithFourDecimalPlaces_IsAccepted()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement unitPrice = invoice.Descendants(ns + "UnitPrice").First();
            unitPrice.SetAttributeValue("BaseQuantity", "2");
            unitPrice.Value = "1";

            var result = Validate(document);

            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-04");
        }

        [Fact]
        public void MoreThan999Lines_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement itemList = invoice.Descendants(ns + "ItemList").First();
            XElement line = itemList.Element(ns + "ListLineItem") ?? throw new InvalidOperationException();
            for (int index = 0; index < 998; index++)
            {
                itemList.Add(new XElement(line));
            }

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-08");
        }

        [Fact]
        public void MoreThanTwoDiscounts_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement paymentConditions = invoice.Element(ns + "PaymentConditions") ?? throw new InvalidOperationException();
            for (int index = 0; index < 3; index++)
            {
                paymentConditions.Add(new XElement(
                    ns + "Discount",
                    new XElement(ns + "PaymentDate", "2026-01-01"),
                    new XElement(ns + "Percentage", "3")));
            }

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-09");
        }

        [Fact]
        public void InvalidDiscountPercentage_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement paymentConditions = invoice.Element(ns + "PaymentConditions") ?? throw new InvalidOperationException();
            paymentConditions.Add(new XElement(
                ns + "Discount",
                new XElement(ns + "PaymentDate", "2026-01-01"),
                new XElement(ns + "Percentage", "100")));

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-10");
        }

        [Fact]
        public void UnsupportedPaymentCard_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement paymentMethod = invoice.Element(ns + "PaymentMethod") ?? throw new InvalidOperationException();
            paymentMethod.RemoveNodes();
            paymentMethod.Add(new XElement(
                ns + "PaymentCard",
                new XElement(ns + "PrimaryAccountNumber", "4111111111111111")));

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-11");
        }

        [Fact]
        public void MissingTransferIban_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            invoice.Descendants(ns + "IBAN").First().Remove();

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-13");
        }

        [Fact]
        public void TransferWithOneAccount_IsAccepted()
        {
            var result = Validate(XDocument.Load(SamplePath));

            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-11");
            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-12");
            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-13");
        }

        [Fact]
        public void SepaDirectDebitWithoutRequiredFields_ReportsErbCode()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement paymentMethod = invoice.Element(ns + "PaymentMethod") ?? throw new InvalidOperationException();
            paymentMethod.RemoveNodes();
            paymentMethod.Add(new XElement(ns + "SEPADirectDebit"));

            var result = Validate(document);

            Assert.Contains(result.Errors, message => message.Code == "ERB-15");
        }

        [Fact]
        public void SepaDirectDebitWithRequiredFields_IsAccepted()
        {
            var document = XDocument.Load(SamplePath);
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement paymentMethod = invoice.Element(ns + "PaymentMethod") ?? throw new InvalidOperationException();
            paymentMethod.RemoveNodes();
            paymentMethod.Add(new XElement(
                ns + "SEPADirectDebit",
                new XElement(ns + "Type", "CORE"),
                new XElement(ns + "IBAN", "AT611904300234573201"),
                new XElement(ns + "BankAccountOwner", "Maxima Kontofrau"),
                new XElement(ns + "CreditorID", "AT98ZZZ09999999999"),
                new XElement(ns + "MandateReference", "MANDATE-1"),
                new XElement(ns + "DebitCollectionDate", "2026-10-01")));

            var result = Validate(document);

            Assert.DoesNotContain(result.Errors, message => message.Code == "ERB-15");
        }

        private static ValidationResult Validate(XDocument document)
        {
            using var stream = new MemoryStream();
            document.Save(stream);
            stream.Position = 0;
            return EbInterfaceValidator.Validate(stream);
        }

        private static XElement GetRecipientOrderId(XDocument document)
        {
            XElement invoice = document.Root ?? throw new InvalidOperationException();
            XNamespace ns = invoice.Name.Namespace;
            XElement recipient = invoice.Element(ns + "InvoiceRecipient") ?? throw new InvalidOperationException();
            XElement orderReference = recipient.Element(ns + "OrderReference") ?? throw new InvalidOperationException();
            return orderReference.Element(ns + "OrderID") ?? throw new InvalidOperationException();
        }
    }

    public class RobustnessTests
    {
        private static ValidationResult Validate(string xml) =>
            EbInterfaceValidator.Validate(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

        [Fact]
        public void ForeignXml_IsRejected()
        {
            var result = Validate("<Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\"/>");
            Assert.False(result.IsValid);
            Assert.Equal("VER-01", result.Errors.First().Code);
        }

        [Fact]
        public void MalformedXml_IsRejected()
        {
            Assert.False(Validate("<Invoice xmlns=\"http://www.ebinterface.at/schema/6p1/\">").IsValid);
            Assert.False(Validate("das ist kein xml").IsValid);
        }

        [Fact]
        public void Dtd_IsNotProcessed_XxeProtection()
        {
            const string xxe = "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///etc/passwd\">]>" +
                               "<Invoice xmlns=\"http://www.ebinterface.at/schema/6p1/\">&e;</Invoice>";
            Assert.False(Validate(xxe).IsValid);
        }
    }
}
