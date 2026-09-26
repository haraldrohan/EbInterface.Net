using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using EbInterface.Model;

namespace EbInterface.Internal
{
    /// <summary>
    /// Erzeugt ebInterface 6.1 aus dem Modell. Die Elementreihenfolge folgt dem Schema; leere optionale Angaben
    /// werden weggelassen. Werte werden nicht gerundet oder erfunden – was das Schema verlangt, aber im Modell fehlt,
    /// meldet anschließend die Schema-Prüfung. Einzige Ergänzungen für ältere Quellen: Steuerkategorie und Sprache.
    /// </summary>
    internal sealed class InvoiceWriter61
    {
        private static readonly XNamespace Ns = EbInterfaceVersion.V6p1.GetNamespace();

        /// <summary>Dreistellige Sprachcodes (ISO 639-2, 4.3/5.0) nach zweistelligen (ISO 639-1, ab 6.0).</summary>
        private static readonly Dictionary<string, string> LanguageCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ger"] = "de", ["deu"] = "de", ["eng"] = "en", ["fre"] = "fr", ["fra"] = "fr", ["ita"] = "it",
            ["spa"] = "es", ["por"] = "pt", ["dut"] = "nl", ["nld"] = "nl", ["hun"] = "hu", ["cze"] = "cs",
            ["ces"] = "cs", ["slo"] = "sk", ["slk"] = "sk", ["slv"] = "sl", ["hrv"] = "hr", ["pol"] = "pl",
            ["rum"] = "ro", ["ron"] = "ro", ["bul"] = "bg", ["gre"] = "el", ["ell"] = "el", ["swe"] = "sv",
            ["dan"] = "da", ["fin"] = "fi", ["tur"] = "tr", ["rus"] = "ru", ["srp"] = "sr", ["bos"] = "bs",
        };

        internal static XDocument Write(EbInvoice invoice) =>
            new XDocument(new XDeclaration("1.0", "utf-8", null), new InvoiceWriter61().WriteInvoice(invoice));

        private XElement WriteInvoice(EbInvoice i)
        {
            var root = new XElement(Ns + "Invoice",
                new XAttribute("GeneratingSystem", i.GeneratingSystem),
                new XAttribute("DocumentType", i.DocumentType.ToString()),
                new XAttribute("InvoiceCurrency", i.Currency),
                OptAttr("ManualProcessing", Bool(i.ManualProcessing)),
                OptAttr("DocumentTitle", i.DocumentTitle),
                OptAttr("Language", Language(i.Language)),
                OptAttr("IsDuplicate", Bool(i.IsDuplicate)),
                El("InvoiceNumber", i.InvoiceNumber),
                El("InvoiceDate", Date(i.InvoiceDate)),
                i.CancelledOriginalDocument == null ? null : WriteDocumentReference("CancelledOriginalDocument", i.CancelledOriginalDocument),
                i.RelatedDocuments.Select(d => WriteDocumentReference("RelatedDocument", d)),
                i.Delivery == null ? null : WriteDelivery(i.Delivery),
                WriteParty("Biller", i.Biller, OptEl("InvoiceRecipientsBillerID", i.Biller.InvoiceRecipientsBillerId)),
                WriteParty("InvoiceRecipient", i.InvoiceRecipient,
                    OptEl("BillersInvoiceRecipientID", i.InvoiceRecipient.BillersInvoiceRecipientId),
                    OptEl("AccountingArea", i.InvoiceRecipient.AccountingArea),
                    OptEl("SubOrganizationID", i.InvoiceRecipient.SubOrganizationId)),
                i.OrderingParty == null ? null : WriteParty("OrderingParty", i.OrderingParty,
                    El("BillersOrderingPartyID", i.OrderingParty.BillersOrderingPartyId)),
                WriteDetails(i),
                i.ReductionsAndSurcharges.Count == 0 ? null : new XElement(Ns + "ReductionAndSurchargeDetails",
                    i.ReductionsAndSurcharges.Select(WriteHeaderReductionOrSurcharge)),
                new XElement(Ns + "Tax", i.TaxItems.Select(WriteTaxItem), i.OtherTaxes.Select(WriteOtherTax)),
                El("TotalGrossAmount", Dec(i.TotalGrossAmount)),
                OptEl("PrepaidAmount", Dec(i.PrepaidAmount)),
                OptEl("RoundingAmount", Dec(i.RoundingAmount)),
                El("PayableAmount", Dec(i.PayableAmount)),
                i.PaymentMethod == null ? null : WritePaymentMethod(i.PaymentMethod),
                i.PaymentConditions == null ? null : WritePaymentConditions(i.PaymentConditions),
                OptEl("Comment", i.Comment));
            return root;
        }

        private XElement WriteDocumentReference(string name, DocumentReference d) => new XElement(Ns + name,
            El("InvoiceNumber", d.InvoiceNumber),
            OptEl("InvoiceDate", Date(d.InvoiceDate)),
            OptEl("DocumentType", d.DocumentType?.ToString()),
            OptEl("Comment", d.Comment));

        private XElement WriteDelivery(Delivery d) => new XElement(Ns + "Delivery",
            OptEl("DeliveryID", d.DeliveryId),
            d.Date.HasValue || !(d.PeriodFrom.HasValue || d.PeriodTo.HasValue)
                ? El("Date", Date(d.Date))
                : new XElement(Ns + "Period", El("FromDate", Date(d.PeriodFrom)), El("ToDate", Date(d.PeriodTo))),
            d.Address == null ? null : WriteAddress(d.Address),
            d.Contact == null ? null : WriteContact(d.Contact),
            OptEl("Description", d.Description));

        // --- Parteien -----------------------------------------------------------------------------

        private XElement WriteParty(string name, Party p, params XElement?[] specific) => new XElement(Ns + name,
            El("VATIdentificationNumber", p.VatIdentificationNumber),
            p.FurtherIdentifications.Select(f => new XElement(Ns + "FurtherIdentification",
                new XAttribute("IdentificationType", f.IdentificationType), f.Value)),
            p.OrderReference == null ? null : new XElement(Ns + "OrderReference",
                El("OrderID", p.OrderReference.OrderId),
                OptEl("ReferenceDate", Date(p.OrderReference.ReferenceDate)),
                OptEl("Description", p.OrderReference.Description)),
            p.Address == null ? null : WriteAddress(p.Address),
            p.Contact == null ? null : WriteContact(p.Contact),
            specific);

        private XElement WriteAddress(Address a) => new XElement(Ns + "Address",
            a.AddressIdentifiers.Select(ai => new XElement(Ns + "AddressIdentifier",
                OptAttr("AddressIdentifierType", ai.Type), ai.Value)),
            El("Name", a.Name),
            OptEl("TradingName", a.TradingName),
            OptEl("Street", a.Street),
            OptEl("POBox", a.POBox),
            El("Town", a.Town),
            El("ZIP", a.Zip),
            new XElement(Ns + "Country", OptAttr("CountryCode", a.CountryCode), a.CountryName),
            a.Phones.Select(p => El("Phone", p)),
            a.Emails.Select(m => El("Email", m)));

        private XElement WriteContact(Contact c) => new XElement(Ns + "Contact",
            OptEl("Salutation", c.Salutation),
            El("Name", c.Name),
            c.Phones.Select(p => El("Phone", p)),
            c.Emails.Select(m => El("Email", m)));

        // --- Details ------------------------------------------------------------------------------

        private XElement WriteDetails(EbInvoice i) => new XElement(Ns + "Details",
            OptEl("HeaderDescription", i.HeaderDescription),
            i.ItemLists.Select(list => new XElement(Ns + "ItemList",
                OptEl("HeaderDescription", list.HeaderDescription),
                list.LineItems.Select(WriteLineItem),
                OptEl("FooterDescription", list.FooterDescription))),
            OptEl("FooterDescription", i.FooterDescription),
            i.BelowTheLineItems.Select(b => new XElement(Ns + "BelowTheLineItem",
                El("Description", b.Description),
                El("LineItemAmount", Dec(b.LineItemAmount)),
                b.Reason == null && !b.ReasonDate.HasValue ? null
                    : new XElement(Ns + "Reason", OptAttr("Date", Date(b.ReasonDate)), b.Reason ?? string.Empty))));

        private XElement WriteLineItem(LineItem l) => new XElement(Ns + "ListLineItem",
            OptEl("PositionNumber", l.PositionNumber),
            l.Descriptions.Select(d => El("Description", d)),
            l.ArticleNumbers.Select(a => new XElement(Ns + "ArticleNumber", OptAttr("ArticleNumberType", a.Type), a.Value)),
            new XElement(Ns + "Quantity", new XAttribute("Unit", l.Unit), Dec(l.Quantity)),
            new XElement(Ns + "UnitPrice", OptAttr("BaseQuantity", Dec(l.BaseQuantity)), Dec(l.UnitPrice)),
            l.ReductionsAndSurcharges.Count == 0 ? null : new XElement(Ns + "ReductionAndSurchargeListLineItemDetails",
                l.ReductionsAndSurcharges.Select(r => WriteLineReductionOrSurcharge(r, l))),
            l.Delivery == null ? null : WriteDelivery(l.Delivery),
            l.BillersOrderReference == null ? null : WriteLineOrderReference("BillersOrderReference", l.BillersOrderReference),
            l.InvoiceRecipientsOrderReference == null ? null
                : WriteLineOrderReference("InvoiceRecipientsOrderReference", l.InvoiceRecipientsOrderReference),
            new XElement(Ns + "TaxItem",
                El("TaxableAmount", Dec(l.TaxableAmount ?? l.LineItemAmount)),
                TaxPercent(l.TaxPercent, l.TaxCategoryCode, l.TaxExemptionReason != null),
                OptEl("TaxAmount", Dec(l.TaxAmount)),
                OptEl("Comment", l.TaxExemptionReason)),
            El("LineItemAmount", Dec(l.LineItemAmount)));

        private XElement WriteLineOrderReference(string name, LineOrderReference r) => new XElement(Ns + name,
            El("OrderID", r.OrderId),
            OptEl("ReferenceDate", Date(r.ReferenceDate)),
            OptEl("Description", r.Description),
            OptEl("OrderPositionNumber", r.OrderPositionNumber));

        /// <summary>
        /// In 4.3 hat eine sonstige Abgabe auf Zeilenebene keinen eigenen Steuersatz, ab 5.0 ist er Pflicht. Sie gehört
        /// zur Zeile und wird mit ihr versteuert – daher gelten fehlend Steuersatz und -kategorie der Zeile.
        /// </summary>
        private XElement WriteLineReductionOrSurcharge(ReductionOrSurcharge r, LineItem line) => r.Kind switch
        {
            ReductionOrSurchargeKind.Reduction => WriteReductionBase("ReductionListLineItem", r),
            ReductionOrSurchargeKind.Surcharge => WriteReductionBase("SurchargeListLineItem", r),
            _ => WriteOtherVatableTax("OtherVATableTaxListLineItem", r,
                r.TaxPercent ?? line.TaxPercent, r.TaxCategoryCode ?? line.TaxCategoryCode),
        };

        private XElement WriteHeaderReductionOrSurcharge(ReductionOrSurcharge r)
        {
            if (r.Kind == ReductionOrSurchargeKind.OtherVatableTax)
                return WriteOtherVatableTax("OtherVATableTax", r);

            XElement element = WriteReductionBase(r.Kind == ReductionOrSurchargeKind.Reduction ? "Reduction" : "Surcharge", r);
            decimal net = r.Amount ?? (r.BaseAmount ?? 0m) * (r.Percentage ?? 0m) / 100m;
            element.Add(new XElement(Ns + "TaxItem",
                El("TaxableAmount", Dec(net)),
                TaxPercent(r.TaxPercent, r.TaxCategoryCode, exempt: false)));
            return element;
        }

        private XElement WriteReductionBase(string name, ReductionOrSurcharge r) => new XElement(Ns + name,
            El("BaseAmount", Dec(r.BaseAmount)),
            OptEl("Percentage", Dec(r.Percentage)),
            OptEl("Amount", Dec(r.Amount)),
            OptEl("Comment", r.Comment));

        /// <summary>Ab 5.0 wie ein TaxItem plus TaxID.</summary>
        private XElement WriteOtherVatableTax(string name, ReductionOrSurcharge r) =>
            WriteOtherVatableTax(name, r, r.TaxPercent, r.TaxCategoryCode);

        private XElement WriteOtherVatableTax(string name, ReductionOrSurcharge r, decimal? taxPercent, string? taxCategory) => new XElement(Ns + name,
            El("TaxableAmount", Dec(r.BaseAmount)),
            TaxPercent(taxPercent, taxCategory, exempt: false),
            OptEl("TaxAmount", Dec(r.Amount)),
            OptEl("Comment", r.Comment),
            El("TaxID", r.TaxId));

        // --- Steuer -------------------------------------------------------------------------------

        private XElement WriteTaxItem(TaxItem t) => new XElement(Ns + "TaxItem",
            El("TaxableAmount", Dec(t.TaxableAmount)),
            TaxPercent(t.TaxPercent, t.TaxCategoryCode, t.TaxExemptionReason != null),
            OptEl("TaxAmount", Dec(t.TaxAmount)),
            OptEl("Comment", t.Comment ?? t.TaxExemptionReason));

        private XElement WriteOtherTax(OtherTax t) => new XElement(Ns + "OtherTax",
            OptEl("TaxableAmount", Dec(t.TaxableAmount)),
            t.TaxPercent.HasValue ? TaxPercent(t.TaxPercent.Value, t.TaxCategoryCode, exempt: false) : null,
            El("TaxAmount", Dec(t.TaxAmount)),
            El("Comment", t.Comment));

        /// <summary>
        /// TaxCategoryCode ist ab 5.0 Pflicht. Fehlt er (Quelle 4.3), wird er abgeleitet: „S“ (Normalsatz) bei einem
        /// Steuersatz über 0, sonst „E“ (steuerbefreit). Andere Fälle (z. B. Reverse Charge „AE“) muss der Aufrufer setzen.
        /// </summary>
        private XElement TaxPercent(decimal? percent, string? category, bool exempt)
        {
            XElement element = El("TaxPercent", Dec(percent));
            element.Add(new XAttribute("TaxCategoryCode", category ?? (percent > 0m && !exempt ? "S" : "E")));
            return element;
        }

        // --- Zahlung ------------------------------------------------------------------------------

        private XElement WritePaymentMethod(PaymentMethod p) => new XElement(Ns + "PaymentMethod",
            OptEl("Comment", p.Comment),
            p switch
            {
                UniversalBankTransaction u => new XElement(Ns + "UniversalBankTransaction",
                    OptAttr("ConsolidatorPayable", Bool(u.ConsolidatorPayable)),
                    u.BeneficiaryAccounts.Select(a => new XElement(Ns + "BeneficiaryAccount",
                        OptEl("BankName", a.BankName),
                        a.BankCode == null ? null : new XElement(Ns + "BankCode", OptAttr("BankCodeType", a.BankCodeType), a.BankCode),
                        OptEl("BIC", a.Bic),
                        OptEl("BankAccountNr", a.BankAccountNumber),
                        OptEl("IBAN", a.Iban),
                        OptEl("BankAccountOwner", a.BankAccountOwner))),
                    u.PaymentReference == null ? null
                        : new XElement(Ns + "PaymentReference", OptAttr("CheckSum", u.PaymentReferenceCheckSum), u.PaymentReference)),
                SepaDirectDebit s => new XElement(Ns + "SEPADirectDebit",
                    OptEl("Type", s.Type),
                    OptEl("BIC", s.Bic),
                    OptEl("IBAN", s.Iban),
                    OptEl("BankAccountOwner", s.BankAccountOwner),
                    OptEl("CreditorID", s.CreditorId),
                    OptEl("MandateReference", s.MandateReference),
                    OptEl("DebitCollectionDate", Date(s.DebitCollectionDate))),
                PaymentCard c => new XElement(Ns + "PaymentCard",
                    El("PrimaryAccountNumber", c.PrimaryAccountNumber),
                    OptEl("CardHolderName", c.CardHolderName)),
                OtherPayment _ => new XElement(Ns + "OtherPayment"),
                // DirectDebit (nur 4.3) gibt es in 6.1 nicht; NoPayment bleibt NoPayment.
                DirectDebit _ => throw new NotSupportedException(
                    "Die Zahlungsart DirectDebit gibt es nur in ebInterface 4.3. Für 6.1 bitte SepaDirectDebit verwenden."),
                NoPayment _ => new XElement(Ns + "NoPayment"),
                _ => throw new NotSupportedException($"Unbekannte Zahlungsart {p.GetType().Name}."),
            });

        private XElement WritePaymentConditions(PaymentConditions c) => new XElement(Ns + "PaymentConditions",
            OptEl("DueDate", Date(c.DueDate)),
            c.Discounts.Select(d => new XElement(Ns + "Discount",
                El("PaymentDate", Date(d.PaymentDate)),
                OptEl("BaseAmount", Dec(d.BaseAmount)),
                OptEl("Percentage", Dec(d.Percentage)),
                OptEl("Amount", Dec(d.Amount)))),
            OptEl("MinimumPayment", Dec(c.MinimumPayment)),
            OptEl("Comment", c.Comment));

        // --- Hilfen -------------------------------------------------------------------------------

        /// <summary>Pflichtelement. Ist der Wert leer, wird das Element markiert und später als fehlend gemeldet (WRT-01) –
        /// das Schema lässt leere Texte oft zu, eine leere Rechnungsnummer ist aber nie gewollt.</summary>
        private static XElement El(string name, string? value)
        {
            var element = new XElement(Ns + name, value ?? string.Empty);
            if (string.IsNullOrWhiteSpace(value)) element.AddAnnotation(MissingValue.Instance);
            return element;
        }

        /// <summary>Pfade der Pflichtelemente ohne Wert, z. B. „Invoice/Biller/Address/Town“.</summary>
        internal static IEnumerable<string> MissingValues(XDocument document) =>
            document.Descendants()
                .Where(e => e.Annotation<MissingValue>() != null)
                .Select(e => string.Join("/", e.AncestorsAndSelf().Reverse().Select(a => a.Name.LocalName)));

        private sealed class MissingValue
        {
            internal static readonly MissingValue Instance = new MissingValue();
        }

        private static XElement? OptEl(string name, string? value) =>
            string.IsNullOrEmpty(value) ? null : new XElement(Ns + name, value);

        private static XAttribute? OptAttr(string name, string? value) =>
            string.IsNullOrEmpty(value) ? null : new XAttribute(name, value);

        private static string? Dec(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);

        /// <summary>default(DateTime) gilt als nicht gesetzt – sonst entstünde unbemerkt „0001-01-01“.</summary>
        private static string? Date(DateTime? value) =>
            value.HasValue && value.Value != default ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;

        private static string? Bool(bool? value) => value.HasValue ? XmlConvert.ToString(value.Value) : null;

        /// <summary>6.1 verlangt zweistellige Codes; dreistellige werden übersetzt, unbekannte weggelassen (optional).</summary>
        private static string? Language(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            code = code!.Trim();
            if (code.Length == 2) return code.ToLowerInvariant();
            return LanguageCodes.TryGetValue(code, out string? two) ? two : null;
        }
    }
}
