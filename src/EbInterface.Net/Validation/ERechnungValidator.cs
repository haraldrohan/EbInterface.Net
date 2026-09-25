using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

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
