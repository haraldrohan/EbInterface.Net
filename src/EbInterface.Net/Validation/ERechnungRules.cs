using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using EbInterface.Internal;

namespace EbInterface.Validation
{
    /// <summary>
    /// Sonderregeln von e-Rechnung.gv.at für ebInterface 4.3, 5.0, 6.0 und 6.1.
    /// Primärquelle: https://www.erechnung.gv.at/erb/tec_formats_ebinterface (Stand 2026-09-25),
    /// Auftragsreferenzen: https://www.erechnung.gv.at/go/orderref_fedgov und .../go/orderref_others.
    /// Die Codes sind stabil; die Übersicht steht in docs/pruefregeln.md.
    /// </summary>
    internal static class ERechnungRules
    {
        private static readonly HashSet<string> AllowedDocumentTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Invoice", "InvoiceForAdvancePayment", "InvoiceForPartialDelivery", "FinalSettlement", "CreditMemo",
        };

        // Bund: Bestellnummer (10 Ziffern), Einkäufergruppe (3 alphanumerische Zeichen, auch Umlaute wie „63Ü“)
        // oder Einkäufergruppe:Referenz (höchstens 50 Zeichen nach dem Doppelpunkt).
        // [0-9] statt \d, weil \d in .NET auch andere Unicode-Ziffern zulässt.
        private static readonly Regex FederalOrderNumber = new Regex(@"^[0-9]{10}$", RegexOptions.CultureInvariant);
        private static readonly Regex FederalBuyerGroup = new Regex(@"^[\p{L}\p{N}]{3}$", RegexOptions.CultureInvariant);
        private static readonly Regex FederalBuyerGroupReference = new Regex(@"^[\p{L}\p{N}]{3}:(?<ref>.*)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);

        // Andere Empfänger: Empfängeridentifikation + verpflichtender „/“ + optionale interne Referenz (≤ 50 Zeichen).
        // Die Quelle nennt „zumindest 3stellig, z. B. Z0/“ – der Schrägstrich zählt also mit. Die Identifikation
        // wird bewusst nicht auf Buchstaben/Ziffern beschränkt, um empfohlene Verwaltungskennzeichen (VKZ) mit
        // Sonderzeichen nicht abzulehnen; Leerzeichen sind vor dem Schrägstrich nicht erlaubt.
        private static readonly Regex OtherRecipientReference = new Regex(@"^(?<id>[^/\s]{2,})/(?<ref>.*)$", RegexOptions.CultureInvariant | RegexOptions.Singleline);

        private static readonly Regex Digits = new Regex(@"^[0-9]+$", RegexOptions.CultureInvariant);

        private const int MaxReferenceLength = 50;

        internal static void Check(XElement invoice, EbInterfaceVersion version, DateTime referenceDate, IList<ValidationMessage> messages)
        {
            XNamespace ns = invoice.Name.Namespace;
            var ctx = new Context(invoice, ns, version, messages);

            CheckDocumentType(ctx);
            CheckOrderReference(ctx);
            CheckBiller(ctx);
            CheckLineCount(ctx);
            CheckDiscounts(ctx);
            CheckPaymentMethod(ctx);
            CheckDueDate(ctx, referenceDate);
            CheckDeliveryDescriptions(ctx);
            CheckBaseQuantity(ctx);
            ReportIgnoredElements(ctx);
        }

        // ERB-01
        private static void CheckDocumentType(Context ctx)
        {
            string documentType = ctx.DocumentType;
            if (!AllowedDocumentTypes.Contains(documentType))
            {
                ctx.Error("ERB-01", ctx.Invoice,
                    $"Der Dokumenttyp '{documentType}' wird von e-Rechnung.gv.at nicht angenommen. " +
                    "Zulässig sind Invoice, InvoiceForAdvancePayment, InvoiceForPartialDelivery, FinalSettlement und CreditMemo.");
            }
        }

        // ERB-02 bis ERB-06
        private static void CheckOrderReference(Context ctx)
        {
            XElement? recipient = ctx.Invoice.Element(ctx.Ns + "InvoiceRecipient");
            XElement? orderIdElement = recipient?.Element(ctx.Ns + "OrderReference")?.Element(ctx.Ns + "OrderID");
            string orderId = orderIdElement?.Value.Trim() ?? string.Empty;

            if (orderId.Length == 0)
            {
                ctx.Error("ERB-02", (XElement?)orderIdElement ?? recipient ?? ctx.Invoice,
                    "Die Auftragsreferenz fehlt. Sie gehört in InvoiceRecipient/OrderReference/OrderID " +
                    "und wird von der beauftragenden Stelle bekanntgegeben.");
                return;
            }

            OrderReferenceKind kind = Classify(orderId, out string? formatProblem);
            if (kind == OrderReferenceKind.Invalid)
            {
                ctx.Error("ERB-03", orderIdElement!, $"Die Auftragsreferenz '{orderId}' hat kein gültiges Format. {formatProblem}");
                return;
            }

            var lineReferences = ctx.LineItems
                .Select(line => new { Line = line, Reference = line.Element(ctx.Ns + "InvoiceRecipientsOrderReference") })
                .ToList();

            var lineOrderIds = lineReferences
                .Select(x => x.Reference?.Element(ctx.Ns + "OrderID")?.Value.Trim() ?? string.Empty)
                .Where(id => id.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (lineOrderIds.Count > 1)
            {
                ctx.Error("ERB-04", ctx.Invoice,
                    "Die Rechnung bezieht sich auf mehrere Bestellungen (" + string.Join(", ", lineOrderIds) + "). " +
                    "e-Rechnung.gv.at erlaubt nur eine Bestellung pro Rechnung.");
            }

            if (kind == OrderReferenceKind.FederalOrderNumber)
            {
                foreach (var item in lineReferences)
                {
                    string lineOrderId = item.Reference?.Element(ctx.Ns + "OrderID")?.Value.Trim() ?? string.Empty;
                    string position = item.Reference?.Element(ctx.Ns + "OrderPositionNumber")?.Value.Trim() ?? string.Empty;

                    if (lineOrderId.Length > 0 && lineOrderId != orderId && lineOrderIds.Count == 1)
                    {
                        ctx.Error("ERB-04", item.Reference!,
                            $"Die Rechnungszeile verweist auf die Bestellung '{lineOrderId}', die Rechnung auf '{orderId}'. " +
                            "e-Rechnung.gv.at erlaubt nur eine Bestellung pro Rechnung.");
                    }

                    if (lineOrderId.Length == 0 || !Digits.IsMatch(position))
                    {
                        ctx.Error("ERB-05", item.Line,
                            "Bei einer Bestellnummer des Bundes braucht jede Rechnungszeile " +
                            "InvoiceRecipientsOrderReference mit OrderID (gleich der Bestellnummer) und einer " +
                            "numerischen OrderPositionNumber (Bestellpositionsnummer).");
                    }
                }
            }
            else if (lineOrderIds.Count == 1 && lineOrderIds[0] != orderId)
            {
                ctx.Warning("ERB-06", ctx.Invoice,
                    $"Die Rechnungszeilen verweisen auf '{lineOrderIds[0]}', die Auftragsreferenz lautet '{orderId}'. " +
                    "e-Rechnung.gv.at empfiehlt denselben Wert.");
            }
        }

        private static OrderReferenceKind Classify(string orderId, out string? problem)
        {
            problem = null;

            if (orderId.IndexOf('/') >= 0)
            {
                Match other = OtherRecipientReference.Match(orderId);
                if (!other.Success)
                {
                    problem = "Für Länder, Gemeinden und andere Empfänger gilt: mindestens zwei Zeichen " +
                        "Empfängeridentifikation ohne Leerzeichen, dann ein Schrägstrich, z. B. 'Z0/' oder 'Z0/Aktenzahl'.";
                    return OrderReferenceKind.Invalid;
                }

                if (other.Groups["ref"].Value.Length > MaxReferenceLength)
                {
                    problem = $"Die interne Referenz nach dem Schrägstrich darf höchstens {MaxReferenceLength} Zeichen lang sein " +
                        $"(hier {other.Groups["ref"].Value.Length}).";
                    return OrderReferenceKind.Invalid;
                }

                return OrderReferenceKind.OtherRecipient;
            }

            if (FederalOrderNumber.IsMatch(orderId)) return OrderReferenceKind.FederalOrderNumber;
            if (FederalBuyerGroup.IsMatch(orderId)) return OrderReferenceKind.FederalBuyerGroup;

            Match groupReference = FederalBuyerGroupReference.Match(orderId);
            if (groupReference.Success)
            {
                if (groupReference.Groups["ref"].Value.Length > MaxReferenceLength)
                {
                    problem = $"Nach dem Doppelpunkt sind höchstens {MaxReferenceLength} Zeichen erlaubt " +
                        $"(hier {groupReference.Groups["ref"].Value.Length}).";
                    return OrderReferenceKind.Invalid;
                }

                return OrderReferenceKind.FederalBuyerGroupReference;
            }

            problem = "Zulässig sind für den Bund eine 10-stellige Bestellnummer (z. B. 4700000001), eine Einkäufergruppe " +
                "(3 Zeichen, z. B. Z01) oder Einkäufergruppe:Referenz (z. B. Z01:111599); für andere Empfänger " +
                "Empfängeridentifikation mit Schrägstrich (z. B. Z0/ oder Z0/Aktenzahl).";
            return OrderReferenceKind.Invalid;
        }

        // ERB-07 bis ERB-09
        private static void CheckBiller(Context ctx)
        {
            XElement? biller = ctx.Invoice.Element(ctx.Ns + "Biller");
            if (biller == null) return; // vom Schema erzwungen

            XElement? supplierId = biller.Element(ctx.Ns + "InvoiceRecipientsBillerID");
            if (supplierId == null || string.IsNullOrWhiteSpace(supplierId.Value))
            {
                ctx.Error("ERB-07", biller,
                    "Die Lieferantennummer fehlt. Sie gehört in Biller/InvoiceRecipientsBillerID und wird vom Auftraggeber vergeben.");
            }

            bool hasEmail = biller.Descendants(ctx.Ns + "Email").Any(e => !string.IsNullOrWhiteSpace(e.Value));
            if (!hasEmail)
            {
                ctx.Error("ERB-08", biller,
                    "Beim Rechnungssteller (Biller) muss mindestens eine E-Mail-Adresse angegeben sein, z. B. in Biller/Address/Email.");
            }

            var missing = new[] { "FS", "FN", "FBG" }
                .Where(type => !biller.Elements(ctx.Ns + "FurtherIdentification").Any(fi =>
                    fi.Attribute(ctx.Attr("IdentificationType"))?.Value == type && !string.IsNullOrWhiteSpace(fi.Value)))
                .ToList();
            if (missing.Count > 0)
            {
                ctx.Warning("ERB-09", biller,
                    "Firmenangaben nach § 14 UGB fehlen: " + string.Join(", ", missing) + ". " +
                    "Für Unternehmen im Firmenbuch gehören Firmensitz (FS), Firmenbuchnummer (FN) und Firmenbuchgericht (FBG) " +
                    "in Biller/FurtherIdentification mit dem jeweiligen IdentificationType.");
            }
        }

        // ERB-10
        private static void CheckLineCount(Context ctx)
        {
            // 6.1 und 4.3: Rechnungs- und Below-The-Line-Zeilen zusammen; 5.0 und 6.0: nur Rechnungszeilen.
            bool includeBelowTheLine = ctx.Version == EbInterfaceVersion.V6p1 || ctx.Version == EbInterfaceVersion.V4p3;
            int count = ctx.LineItems.Count +
                (includeBelowTheLine ? ctx.Invoice.Descendants(ctx.Ns + "BelowTheLineItem").Count() : 0);

            if (count > 999)
            {
                ctx.Error("ERB-10", ctx.Invoice, includeBelowTheLine
                    ? $"Die Rechnung hat {count} Rechnungs- und Below-The-Line-Zeilen; e-Rechnung.gv.at erlaubt höchstens 999."
                    : $"Die Rechnung hat {count} Rechnungszeilen; e-Rechnung.gv.at erlaubt höchstens 999.");
            }
        }

        // ERB-11, ERB-12
        private static void CheckDiscounts(Context ctx)
        {
            XElement? conditions = ctx.Invoice.Element(ctx.Ns + "PaymentConditions");
            if (conditions == null) return;

            var discounts = conditions.Elements(ctx.Ns + "Discount").ToList();
            if (discounts.Count > 2)
            {
                ctx.Error("ERB-11", discounts[2],
                    $"Die Rechnung enthält {discounts.Count} Skonto-Angaben (Discount); e-Rechnung.gv.at erlaubt höchstens 2.");
            }

            foreach (XElement discount in discounts)
            {
                XElement? percentage = discount.Element(ctx.Ns + "Percentage");
                if (percentage != null && TryParseDecimal(percentage.Value, out decimal value) && (value <= 0 || value >= 100))
                {
                    ctx.Error("ERB-12", percentage,
                        $"Der Skonto-Prozentsatz {percentage.Value.Trim()} ist unzulässig; er muss größer als 0 und kleiner als 100 sein.");
                }
            }
        }

        // ERB-13 bis ERB-17, ERB-19
        private static void CheckPaymentMethod(Context ctx)
        {
            bool isCreditMemo = ctx.DocumentType == "CreditMemo";
            XElement? paymentMethod = ctx.Invoice.Element(ctx.Ns + "PaymentMethod");

            if (paymentMethod == null)
            {
                if (!isCreditMemo)
                {
                    ctx.Error("ERB-14", ctx.Invoice,
                        "Die Zahlungsart fehlt. Rechnungen brauchen PaymentMethod mit UniversalBankTransaction, SEPADirectDebit " +
                        (ctx.Version == EbInterfaceVersion.V4p3 ? "DirectDebit " : string.Empty) +
                        "oder NoPayment; nur Gutschriften (CreditMemo) dürfen ohne Zahlungsart sein.");
                }

                return;
            }

            foreach (XElement payment in paymentMethod.Elements())
            {
                string name = payment.Name == ctx.Ns + payment.Name.LocalName ? payment.Name.LocalName : string.Empty;
                switch (name)
                {
                    case "PaymentCard":
                    case "OtherPayment":
                        ctx.Error("ERB-13", payment,
                            $"Die Zahlungsart {name} wird von e-Rechnung.gv.at nicht unterstützt. " +
                            "Verwenden Sie UniversalBankTransaction, SEPADirectDebit oder NoPayment.");
                        break;

                    case "UniversalBankTransaction":
                        var accounts = payment.Elements(ctx.Ns + "BeneficiaryAccount").ToList();
                        if (accounts.Count != 1)
                        {
                            ctx.Error("ERB-15", payment,
                                $"Bei UniversalBankTransaction ist genau ein Empfängerkonto (BeneficiaryAccount) erforderlich, gefunden: {accounts.Count}.");
                        }

                        foreach (XElement account in accounts)
                        {
                            if (string.IsNullOrWhiteSpace(account.Element(ctx.Ns + "IBAN")?.Value))
                            {
                                ctx.Error("ERB-16", account,
                                    "Das Empfängerkonto braucht eine IBAN; Kontonummer und Bankleitzahl allein genügen nicht.");
                            }
                        }

                        break;

                    case "SEPADirectDebit":
                        var missing = new[] { "Type", "IBAN", "BankAccountOwner", "CreditorID", "MandateReference", "DebitCollectionDate" }
                            .Where(field => string.IsNullOrWhiteSpace(payment.Element(ctx.Ns + field)?.Value))
                            .ToList();
                        if (missing.Count > 0)
                        {
                            ctx.Error("ERB-17", payment,
                                "Bei SEPADirectDebit fehlen Pflichtangaben: " + string.Join(", ", missing) + ".");
                        }

                        break;

                    case "NoPayment":
                        XElement? payable = ctx.Invoice.Element(ctx.Ns + "PayableAmount");
                        bool isZero = payable != null && TryParseDecimal(payable.Value, out decimal amount) && amount == 0;
                        if (!isCreditMemo && !isZero)
                        {
                            ctx.Error("ERB-19", payment,
                                "NoPayment ist nur bei Gutschriften (CreditMemo) und Rechnungen über 0 Euro zulässig.");
                        }

                        break;
                }
            }
        }

        // ERB-18
        private static void CheckDueDate(Context ctx, DateTime referenceDate)
        {
            XElement? dueDate = ctx.Invoice.Element(ctx.Ns + "PaymentConditions")?.Element(ctx.Ns + "DueDate");
            string text = dueDate?.Value.Trim() ?? string.Empty;
            if (text.Length < 10 ||
                !DateTime.TryParseExact(text.Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return;
            }

            DateTime latest = referenceDate.AddDays(999);
            if (date > latest)
            {
                ctx.Error("ERB-18", dueDate!,
                    $"Das Zahlungsziel {text} liegt mehr als 999 Tage in der Zukunft (spätestens {latest:yyyy-MM-dd}).");
            }
        }

        // ERB-20
        private static void CheckDeliveryDescriptions(Context ctx)
        {
            foreach (XElement delivery in ctx.Invoice.Descendants(ctx.Ns + "Delivery"))
            {
                // Die Primärquelle nennt „Delivery/Comment“; das Element heißt im Schema Description.
                foreach (XElement description in delivery.Elements(ctx.Ns + "Description"))
                {
                    if (description.Value.Length > 500)
                    {
                        ctx.Error("ERB-20", description,
                            $"Die Lieferbeschreibung ist {description.Value.Length} Zeichen lang; e-Rechnung.gv.at erlaubt höchstens 500.");
                    }
                }
            }
        }

        // ERB-21
        private static void CheckBaseQuantity(Context ctx)
        {
            foreach (XElement line in ctx.LineItems)
            {
                XElement? unitPrice = line.Element(ctx.Ns + "UnitPrice");
                XElement? quantity = line.Element(ctx.Ns + "Quantity");
                string? baseText = unitPrice?.Attribute(ctx.Attr("BaseQuantity"))?.Value;

                if (baseText == null || quantity == null ||
                    !TryParseDecimal(baseText, out decimal baseQuantity) || baseQuantity == 0 ||
                    !TryParseDecimal(unitPrice!.Value, out decimal price) ||
                    !TryParseDecimal(quantity.Value, out decimal qty))
                {
                    continue;
                }

                // Abgelehnt wird nur, wenn BEIDE Divisionen mehr als vier Nachkommastellen ergeben.
                if (DecimalMath.DecimalPlaces(qty / baseQuantity) > 4 && DecimalMath.DecimalPlaces(price / baseQuantity) > 4)
                {
                    ctx.Error("ERB-21", unitPrice,
                        $"Mit BaseQuantity {baseText} ergeben weder Menge ({quantity.Value.Trim()}) noch Einzelpreis " +
                        $"({unitPrice.Value.Trim()}) geteilt durch BaseQuantity höchstens vier Nachkommastellen. " +
                        "e-Rechnung.gv.at kann die Zeile so nicht übernehmen; Menge oder Preis auf BaseQuantity 1 umrechnen.");
                }
            }
        }

        // ERB-30 bis ERB-38: Felder, die e-Rechnung.gv.at nicht auswertet (nur Warnungen, je Feld einmal).
        private static void ReportIgnoredElements(Context ctx)
        {
            EbInterfaceVersion v = ctx.Version;
            bool v43 = v == EbInterfaceVersion.V4p3;
            bool v5Plus = !v43;
            bool v6Plus = v == EbInterfaceVersion.V6p0 || v == EbInterfaceVersion.V6p1;

            WarnIfPresent(ctx, v5Plus, "ERB-30", ctx.Invoice.Descendants(ctx.Ns + "TradingName"), "TradingName");
            WarnIfPresent(ctx, true, "ERB-31", ctx.Invoice.Descendants(ctx.Ns + "AddressExtension"), "AddressExtension");
            WarnIfPresent(ctx, v6Plus, "ERB-32", ctx.Invoice.Descendants(ctx.Ns + "Extension"), "Erweiterungselemente (Extension)");
            WarnIfPresent(ctx, true, "ERB-33", ctx.Invoice.Descendants(ctx.Ns + "MinimumPayment"), "MinimumPayment");
            WarnIfPresent(ctx, true, "ERB-34", ctx.LineItems.Elements(ctx.Ns + "DiscountFlag"), "ListLineItem/DiscountFlag");
            WarnIfPresent(ctx, v5Plus, "ERB-35", ctx.Invoice.Elements(ctx.Ns + "PrepaidAmount"), "PrepaidAmount");
            WarnIfPresent(ctx, v43, "ERB-36", ctx.LineItems.Elements(ctx.Ns + "AdditionalInformation"), "ListLineItem/AdditionalInformation");
            WarnIfPresent(ctx, v43, "ERB-37", ctx.Invoice.Descendants(ctx.Ns + "PresentationDetails"), "PresentationDetails");
            WarnIfPresent(ctx, v43, "ERB-38",
                ctx.Invoice.Descendants(ctx.Ns + "VATRate").Where(e => e.Attribute(ctx.Attr("TaxCode")) != null), "VATRate/@TaxCode");
        }

        private static void WarnIfPresent(Context ctx, bool applies, string code, IEnumerable<XElement> elements, string field)
        {
            if (!applies) return;

            XElement? first = elements.FirstOrDefault();
            if (first != null)
            {
                ctx.Warning(code, first,
                    $"{field} wird von e-Rechnung.gv.at nicht ausgewertet. Die Angabe ist erlaubt, geht dort aber verloren.");
            }
        }

        private static bool TryParseDecimal(string text, out decimal value) =>
            decimal.TryParse(text.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out value);

        private enum OrderReferenceKind
        {
            Invalid,
            FederalOrderNumber,
            FederalBuyerGroup,
            FederalBuyerGroupReference,
            OtherRecipient,
        }

        private sealed class Context
        {
            internal Context(XElement invoice, XNamespace ns, EbInterfaceVersion version, IList<ValidationMessage> messages)
            {
                Invoice = invoice;
                Ns = ns;
                Version = version;
                Messages = messages;
                DocumentType = invoice.Attribute(Attr("DocumentType"))?.Value ?? string.Empty;
                LineItems = invoice.Descendants(ns + "ListLineItem").ToList();
            }

            internal XElement Invoice { get; }
            internal XNamespace Ns { get; }
            internal EbInterfaceVersion Version { get; }
            internal IList<ValidationMessage> Messages { get; }
            internal string DocumentType { get; }
            internal IReadOnlyList<XElement> LineItems { get; }

            /// <summary>Attributname: In 4.3 sind Attribute qualifiziert (attributeFormDefault="qualified"), ab 5.0 nicht.</summary>
            internal XName Attr(string localName) => Version == EbInterfaceVersion.V4p3 ? Ns + localName : XName.Get(localName);

            internal void Error(string code, XElement at, string message) => Add(ValidationSeverity.Error, code, at, message);

            internal void Warning(string code, XElement at, string message) => Add(ValidationSeverity.Warning, code, at, message);

            private void Add(ValidationSeverity severity, string code, XElement at, string message) =>
                Messages.Add(new ValidationMessage(severity, code, message, LineInfo.Line(at), LineInfo.Position(at)));
        }
    }
}
