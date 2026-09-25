using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace EbInterface.Validation
{
    internal static class ERechnungValidator
    {
        private static readonly HashSet<string> AllowedDocumentTypes = new HashSet<string>
        {
            "Invoice",
            "InvoiceForAdvancePayment",
            "InvoiceForPartialDelivery",
            "FinalSettlement",
            "CreditMemo",
        };

        private static readonly Regex FederalOrderNumber = new Regex(
            @"^[0-9]{10}$",
            RegexOptions.CultureInvariant);

        private static readonly Regex FederalBuyerGroup = new Regex(
            @"^[\p{L}\p{N}]{3}$",
            RegexOptions.CultureInvariant);

        private static readonly Regex FederalBuyerGroupReference = new Regex(
            @"^[\p{L}\p{N}]{3}:[^\r\n]{0,50}$",
            RegexOptions.CultureInvariant);

        private static readonly Regex OtherRecipientReference = new Regex(
            @"^[\p{L}\p{N}]{2,}/[^\r\n]{0,50}$",
            RegexOptions.CultureInvariant);

        private static readonly Regex Numeric = new Regex(
            @"^[0-9]+$",
            RegexOptions.CultureInvariant);

        internal static void Validate(Stream xml, IList<ValidationMessage> messages)
        {
            var settings = Internal.XmlSettings.CreateSecureReaderSettings(closeInput: false);
            using var reader = XmlReader.Create(xml, settings);
            var document = XDocument.Load(reader, LoadOptions.SetLineInfo);
            XElement invoice = document.Root ?? throw new XmlException("Das Dokument hat kein Wurzelelement.");
            XNamespace ns = invoice.Name.Namespace;

            ValidateDocumentType(invoice, messages);
            ValidateRecipientOrderReference(invoice, ns, messages);
            ValidateBillerRegisteredOffice(invoice, ns, messages);
            ValidateBaseQuantityPrecision(invoice, ns, messages);
            ValidateSupplierNumber(invoice, ns, messages);
            ValidateBillerEmail(invoice, ns, messages);
        }

        private static void ValidateDocumentType(XElement invoice, IList<ValidationMessage> messages)
        {
            string documentType = invoice.Attribute("DocumentType")?.Value ?? string.Empty;
            if (!AllowedDocumentTypes.Contains(documentType))
            {
                messages.Add(new ValidationMessage(
                    ValidationSeverity.Error,
                    "ERB-01",
                    "Der Dokumenttyp ist für e-Rechnung.gv.at nicht zulässig.",
                    GetLine(invoice),
                    GetPosition(invoice)));
            }
        }

        private static void ValidateRecipientOrderReference(
            XElement invoice,
            XNamespace ns,
            IList<ValidationMessage> messages)
        {
            XElement? recipient = invoice.Element(ns + "InvoiceRecipient");
            XElement? orderReference = recipient?.Element(ns + "OrderReference") ??
                recipient?.Element(ns + "InvoiceRecipientsOrderReference") ??
                invoice.Element(ns + "InvoiceRecipientsOrderReference");
            XElement? orderId = orderReference?
                .Element(ns + "OrderID");

            if (orderId == null || string.IsNullOrWhiteSpace(orderId.Value))
            {
                messages.Add(new ValidationMessage(
                    ValidationSeverity.Error,
                    "ERB-02",
                    "Die Auftragsreferenz des Rechnungsempfängers (OrderID) ist erforderlich.",
                    GetLine(recipient ?? invoice),
                    GetPosition(recipient ?? invoice)));
                return;
            }

            string orderIdValue = orderId.Value.Trim();
            if (!FederalOrderNumber.IsMatch(orderIdValue) &&
                !FederalBuyerGroup.IsMatch(orderIdValue) &&
                !FederalBuyerGroupReference.IsMatch(orderIdValue) &&
                !OtherRecipientReference.IsMatch(orderIdValue))
            {
                messages.Add(new ValidationMessage(
                    ValidationSeverity.Error,
                    "ERB-02",
                    "Die Auftragsreferenz hat kein zulässiges Format für den Rechnungsempfänger.",
                    GetLine(orderId),
                    GetPosition(orderId)));
                return;
            }

            if (FederalOrderNumber.IsMatch(orderIdValue))
            {
                ValidateFederalOrderPositions(invoice, ns, orderIdValue, messages);
            }
        }

        private static void ValidateBillerRegisteredOffice(
            XElement invoice,
            XNamespace ns,
            IList<ValidationMessage> messages)
        {
            XElement? biller = invoice.Element(ns + "Biller");
            bool hasRegisteredOffice = biller?
                .Elements(ns + "FurtherIdentification")
                .Any(identification =>
                    string.Equals(
                        identification.Attribute("IdentificationType")?.Value,
                        "FS",
                        StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(identification.Value)) == true;

            if (!hasRegisteredOffice)
            {
                messages.Add(new ValidationMessage(
                    ValidationSeverity.Error,
                    "ERB-03",
                    "Der Firmensitz des Rechnungsstellers muss als FurtherIdentification mit IdentificationType=\"FS\" angegeben werden.",
                    GetLine(biller ?? invoice),
                    GetPosition(biller ?? invoice)));
            }

            foreach (string identificationType in new[] { "FN", "FBG" })
            {
                bool hasIdentification = biller?
                    .Elements(ns + "FurtherIdentification")
                    .Any(identification =>
                        string.Equals(
                            identification.Attribute("IdentificationType")?.Value,
                            identificationType,
                            StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(identification.Value)) == true;

                if (!hasIdentification)
                {
                    messages.Add(new ValidationMessage(
                        ValidationSeverity.Error,
                        "ERB-03",
                        $"Die Firmenangabe mit IdentificationType=\"{identificationType}\" fehlt.",
                        GetLine(biller ?? invoice),
                        GetPosition(biller ?? invoice)));
                }
            }
        }

        private static void ValidateFederalOrderPositions(
            XElement invoice,
            XNamespace ns,
            string orderId,
            IList<ValidationMessage> messages)
        {
            foreach (XElement lineItem in invoice.Descendants(ns + "ListLineItem"))
            {
                XElement? reference = lineItem.Element(ns + "InvoiceRecipientsOrderReference");
                string lineOrderId = reference?.Element(ns + "OrderID")?.Value.Trim() ?? string.Empty;
                string positionNumber = reference?.Element(ns + "OrderPositionNumber")?.Value.Trim() ?? string.Empty;

                if (lineOrderId != orderId || !Numeric.IsMatch(positionNumber))
                {
                    messages.Add(new ValidationMessage(
                        ValidationSeverity.Error,
                        "ERB-07",
                        "Bei einer zehnstelligen Bundes-Bestellnummer muss jede Rechnungszeile eine passende numerische Bestellpositionsnummer enthalten.",
                        GetLine(lineItem),
                        GetPosition(lineItem)));
                }
            }
        }

        private static void ValidateSupplierNumber(
            XElement invoice,
            XNamespace ns,
            IList<ValidationMessage> messages)
        {
            XElement? biller = invoice.Element(ns + "Biller");
            XElement? supplierNumber = biller?.Element(ns + "InvoiceRecipientsBillerID");
            if (supplierNumber == null || string.IsNullOrWhiteSpace(supplierNumber.Value))
            {
                messages.Add(new ValidationMessage(
                    ValidationSeverity.Error,
                    "ERB-05",
                    "Die Lieferantennummer (InvoiceRecipientsBillerID) ist erforderlich.",
                    GetLine(biller ?? invoice),
                    GetPosition(biller ?? invoice)));
            }
        }

        private static void ValidateBillerEmail(
            XElement invoice,
            XNamespace ns,
            IList<ValidationMessage> messages)
        {
            XElement? biller = invoice.Element(ns + "Biller");
            bool hasEmail = biller?.Descendants(ns + "Email")
                .Any(email => !string.IsNullOrWhiteSpace(email.Value)) == true;

            if (!hasEmail)
            {
                messages.Add(new ValidationMessage(
                    ValidationSeverity.Error,
                    "ERB-06",
                    "Im Biller muss mindestens eine E-Mail-Adresse angegeben werden.",
                    GetLine(biller ?? invoice),
                    GetPosition(biller ?? invoice)));
            }
        }

        private static void ValidateBaseQuantityPrecision(
            XElement invoice,
            XNamespace ns,
            IList<ValidationMessage> messages)
        {
            foreach (XElement amount in invoice.Descendants().Where(element =>
                element.Name == ns + "Quantity" || element.Name == ns + "UnitPrice"))
            {
                string baseQuantityText = amount.Attribute("BaseQuantity")?.Value ?? string.Empty;
                if (!decimal.TryParse(baseQuantityText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal baseQuantity) ||
                    baseQuantity == 0 ||
                    !decimal.TryParse(amount.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
                {
                    continue;
                }

                decimal quotient = value / baseQuantity;
                if (GetDecimalScale(quotient) > 4)
                {
                    messages.Add(new ValidationMessage(
                        ValidationSeverity.Error,
                        "ERB-04",
                        "Die Division durch BaseQuantity darf höchstens vier Nachkommastellen ergeben.",
                        GetLine(amount),
                        GetPosition(amount)));
                }
            }
        }

        private static int GetDecimalScale(decimal value)
        {
            int flags = Decimal.GetBits(value)[3];
            return (flags >> 16) & 0x7F;
        }

        private static int GetLine(XElement element)
        {
            IXmlLineInfo lineInfo = element;
            return lineInfo.HasLineInfo()
                ? lineInfo.LineNumber
                : 0;
        }

        private static int GetPosition(XElement element)
        {
            IXmlLineInfo lineInfo = element;
            return lineInfo.HasLineInfo()
                ? lineInfo.LinePosition
                : 0;
        }
    }
}
