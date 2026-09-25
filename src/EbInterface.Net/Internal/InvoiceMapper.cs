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
    /// Überträgt ein schemagültiges ebInterface-Dokument (4.3 bis 6.1) in das gemeinsame Modell.
    /// 5.0, 6.0 und 6.1 haben dieselben Elementnamen; 4.3 weicht bei Steuer, Adresse/Kontakt und qualifizierten
    /// Attributen ab. Weil das Dokument vorher gegen das Schema geprüft wurde, sind Pflichtelemente vorhanden
    /// und Werte lexikalisch gültig.
    /// </summary>
    internal sealed class InvoiceMapper
    {
        private readonly XNamespace _ns;
        private readonly EbInterfaceVersion _version;
        private readonly bool _is43;

        private InvoiceMapper(XNamespace ns, EbInterfaceVersion version)
        {
            _ns = ns;
            _version = version;
            _is43 = version == EbInterfaceVersion.V4p3;
        }

        internal static EbInvoice Map(XElement root, EbInterfaceVersion version) =>
            new InvoiceMapper(root.Name.Namespace, version).MapInvoice(root);

        private EbInvoice MapInvoice(XElement e)
        {
            var invoice = new EbInvoice
            {
                SourceVersion = _version,
                GeneratingSystem = Attr(e, "GeneratingSystem") ?? string.Empty,
                DocumentType = ParseDocumentType(Attr(e, "DocumentType")) ?? DocumentType.Invoice,
                Currency = Attr(e, "InvoiceCurrency") ?? string.Empty,
                Language = Attr(e, "Language"),
                DocumentTitle = Attr(e, "DocumentTitle"),
                IsDuplicate = Bool(Attr(e, "IsDuplicate")),
                ManualProcessing = Bool(Attr(e, "ManualProcessing")),
                InvoiceNumber = Text(e, "InvoiceNumber") ?? string.Empty,
                InvoiceDate = Date(e, "InvoiceDate") ?? default,
                CancelledOriginalDocument = Map(El(e, "CancelledOriginalDocument"), MapDocumentReference),
                Delivery = Map(El(e, "Delivery"), MapDelivery),
                Biller = MapBiller(El(e, "Biller")!),
                InvoiceRecipient = MapInvoiceRecipient(El(e, "InvoiceRecipient")!),
                OrderingParty = Map(El(e, "OrderingParty"), MapOrderingParty),
                TotalGrossAmount = Dec(e, "TotalGrossAmount") ?? 0m,
                PrepaidAmount = Dec(e, "PrepaidAmount"),
                RoundingAmount = Dec(e, "RoundingAmount"),
                PayableAmount = Dec(e, "PayableAmount") ?? 0m,
                PaymentMethod = Map(El(e, "PaymentMethod"), MapPaymentMethod),
                PaymentConditions = Map(El(e, "PaymentConditions"), MapPaymentConditions),
                Comment = Text(e, "Comment"),
            };

            invoice.RelatedDocuments.AddRange(Els(e, "RelatedDocument").Select(MapDocumentReference));

            XElement details = El(e, "Details")!;
            invoice.HeaderDescription = Text(details, "HeaderDescription");
            invoice.FooterDescription = Text(details, "FooterDescription");
            invoice.ItemLists.AddRange(Els(details, "ItemList").Select(MapItemList));
            invoice.BelowTheLineItems.AddRange(Els(details, "BelowTheLineItem").Select(MapBelowTheLineItem));

            XElement? reductions = El(e, "ReductionAndSurchargeDetails");
            if (reductions != null)
                invoice.ReductionsAndSurcharges.AddRange(reductions.Elements().Select(MapHeaderReductionOrSurcharge).OfType<ReductionOrSurcharge>());

            XElement tax = El(e, "Tax")!;
            IEnumerable<XElement> taxItems = _is43 ? Els(El(tax, "VAT"), "VATItem") : Els(tax, "TaxItem");
            invoice.TaxItems.AddRange(taxItems.Select(MapTaxItem));
            invoice.OtherTaxes.AddRange(Els(tax, "OtherTax").Select(MapOtherTax));

            return invoice;
        }

        // --- Dokumente, Lieferung -----------------------------------------------------------------

        private DocumentReference MapDocumentReference(XElement e) => new DocumentReference
        {
            InvoiceNumber = Text(e, "InvoiceNumber") ?? string.Empty,
            InvoiceDate = Date(e, "InvoiceDate"),
            DocumentType = ParseDocumentType(Text(e, "DocumentType")),
            Comment = Text(e, "Comment"),
        };

        private Delivery MapDelivery(XElement e)
        {
            XElement? period = El(e, "Period");
            XElement? address = El(e, "Address");
            return new Delivery
            {
                DeliveryId = Text(e, "DeliveryID"),
                Date = Date(e, "Date"),
                PeriodFrom = period != null ? Date(period, "FromDate") : null,
                PeriodTo = period != null ? Date(period, "ToDate") : null,
                Address = Map(address, MapAddress),
                Contact = _is43 ? MapContact43(address) : Map(El(e, "Contact"), MapContact),
                Description = Text(e, "Description"),
            };
        }

        // --- Parteien -----------------------------------------------------------------------------

        private Biller MapBiller(XElement e)
        {
            var biller = new Biller { InvoiceRecipientsBillerId = Text(e, "InvoiceRecipientsBillerID") };
            FillParty(biller, e);
            return biller;
        }

        private InvoiceRecipient MapInvoiceRecipient(XElement e)
        {
            var recipient = new InvoiceRecipient
            {
                BillersInvoiceRecipientId = Text(e, "BillersInvoiceRecipientID"),
                AccountingArea = Text(e, "AccountingArea"),
                SubOrganizationId = Text(e, "SubOrganizationID"),
            };
            FillParty(recipient, e);
            return recipient;
        }

        private OrderingParty MapOrderingParty(XElement e)
        {
            var party = new OrderingParty { BillersOrderingPartyId = Text(e, "BillersOrderingPartyID") ?? string.Empty };
            FillParty(party, e);
            return party;
        }

        private void FillParty(Party party, XElement e)
        {
            party.VatIdentificationNumber = Text(e, "VATIdentificationNumber") ?? string.Empty;
            party.FurtherIdentifications.AddRange(Els(e, "FurtherIdentification").Select(fi => new FurtherIdentification
            {
                IdentificationType = Attr(fi, "IdentificationType") ?? string.Empty,
                Value = fi.Value,
            }));
            party.OrderReference = Map(El(e, "OrderReference"), MapOrderReference);

            XElement? address = El(e, "Address");
            party.Address = Map(address, MapAddress);
            party.Contact = _is43 ? MapContact43(address) : Map(El(e, "Contact"), MapContact);
        }

        private OrderReference MapOrderReference(XElement e) => new OrderReference
        {
            OrderId = Text(e, "OrderID") ?? string.Empty,
            ReferenceDate = Date(e, "ReferenceDate"),
            Description = Text(e, "Description"),
        };

        private Address MapAddress(XElement e)
        {
            XElement? country = El(e, "Country");
            var address = new Address
            {
                Name = Text(e, "Name") ?? string.Empty,
                TradingName = Text(e, "TradingName"),
                Street = Text(e, "Street"),
                POBox = Text(e, "POBox"),
                Town = Text(e, "Town") ?? string.Empty,
                Zip = Text(e, "ZIP") ?? string.Empty,
                CountryCode = country != null ? Attr(country, "CountryCode") : null,
                CountryName = country?.Value ?? string.Empty,
            };
            address.AddressIdentifiers.AddRange(Els(e, "AddressIdentifier").Select(ai => new AddressIdentifier
            {
                Type = Attr(ai, "AddressIdentifierType"),
                Value = ai.Value,
            }));
            address.Phones.AddRange(Els(e, "Phone").Select(p => p.Value));
            address.Emails.AddRange(Els(e, "Email").Select(m => m.Value));
            return address;
        }

        private Contact MapContact(XElement e)
        {
            var contact = new Contact { Salutation = Text(e, "Salutation"), Name = Text(e, "Name") ?? string.Empty };
            contact.Phones.AddRange(Els(e, "Phone").Select(p => p.Value));
            contact.Emails.AddRange(Els(e, "Email").Select(m => m.Value));
            return contact;
        }

        /// <summary>4.3 kennt kein eigenes Contact-Element; Anrede und Name stehen in der Adresse.</summary>
        private Contact? MapContact43(XElement? address)
        {
            if (address == null) return null;
            string? salutation = Text(address, "Salutation");
            string? name = Text(address, "Contact");
            return salutation == null && name == null ? null : new Contact { Salutation = salutation, Name = name ?? string.Empty };
        }

        // --- Details ------------------------------------------------------------------------------

        private ItemList MapItemList(XElement e)
        {
            var list = new ItemList { HeaderDescription = Text(e, "HeaderDescription"), FooterDescription = Text(e, "FooterDescription") };
            list.LineItems.AddRange(Els(e, "ListLineItem").Select(MapLineItem));
            return list;
        }

        private LineItem MapLineItem(XElement e)
        {
            XElement quantity = El(e, "Quantity")!;
            XElement unitPrice = El(e, "UnitPrice")!;
            var line = new LineItem
            {
                PositionNumber = Text(e, "PositionNumber"),
                Quantity = ParseDecimal(quantity.Value),
                Unit = Attr(quantity, "Unit") ?? string.Empty,
                UnitPrice = ParseDecimal(unitPrice.Value),
                BaseQuantity = ParseNullableDecimal(Attr(unitPrice, "BaseQuantity")),
                Delivery = Map(El(e, "Delivery"), MapDelivery),
                BillersOrderReference = Map(El(e, "BillersOrderReference"), MapLineOrderReference),
                InvoiceRecipientsOrderReference = Map(El(e, "InvoiceRecipientsOrderReference"), MapLineOrderReference),
                LineItemAmount = Dec(e, "LineItemAmount") ?? 0m,
            };
            line.Descriptions.AddRange(Els(e, "Description").Select(d => d.Value));
            line.ArticleNumbers.AddRange(Els(e, "ArticleNumber").Select(a => new ArticleNumber
            {
                Type = Attr(a, "ArticleNumberType"),
                Value = a.Value,
            }));

            XElement? reductions = El(e, "ReductionAndSurchargeListLineItemDetails");
            if (reductions != null)
                line.ReductionsAndSurcharges.AddRange(reductions.Elements().Select(MapLineReductionOrSurcharge).OfType<ReductionOrSurcharge>());

            if (_is43)
            {
                XElement? exemption = El(e, "TaxExemption");
                line.TaxPercent = Dec(e, "VATRate") ?? 0m;
                line.TaxExemptionReason = exemption?.Value;
                line.TaxExemptionCode = exemption != null ? Attr(exemption, "TaxExemptionCode") : null;
            }
            else
            {
                XElement taxItem = El(e, "TaxItem")!;
                line.TaxableAmount = Dec(taxItem, "TaxableAmount");
                line.TaxPercent = Dec(taxItem, "TaxPercent") ?? 0m;
                line.TaxCategoryCode = TaxCategory(taxItem);
                line.TaxAmount = Dec(taxItem, "TaxAmount");
            }

            return line;
        }

        private LineOrderReference MapLineOrderReference(XElement e) => new LineOrderReference
        {
            OrderId = Text(e, "OrderID") ?? string.Empty,
            ReferenceDate = Date(e, "ReferenceDate"),
            Description = Text(e, "Description"),
            OrderPositionNumber = Text(e, "OrderPositionNumber"),
        };

        private BelowTheLineItem MapBelowTheLineItem(XElement e)
        {
            XElement? reason = El(e, "Reason");
            return new BelowTheLineItem
            {
                Description = Text(e, "Description") ?? string.Empty,
                LineItemAmount = Dec(e, "LineItemAmount") ?? 0m,
                Reason = reason?.Value,
                ReasonDate = reason != null ? ParseNullableDate(Attr(reason, "Date")) : null,
            };
        }

        private ReductionOrSurcharge? MapLineReductionOrSurcharge(XElement e)
        {
            switch (e.Name.LocalName)
            {
                case "ReductionListLineItem": return MapReductionBase(e, ReductionOrSurchargeKind.Reduction);
                case "SurchargeListLineItem": return MapReductionBase(e, ReductionOrSurchargeKind.Surcharge);
                case "OtherVATableTaxListLineItem": return MapOtherVatableTax(e);
                default: return null; // Erweiterungen
            }
        }

        private ReductionOrSurcharge? MapHeaderReductionOrSurcharge(XElement e)
        {
            ReductionOrSurcharge? item;
            switch (e.Name.LocalName)
            {
                case "Reduction": item = MapReductionBase(e, ReductionOrSurchargeKind.Reduction); break;
                case "Surcharge": item = MapReductionBase(e, ReductionOrSurchargeKind.Surcharge); break;
                case "OtherVATableTax": return MapOtherVatableTax(e);
                default: return null; // Erweiterungen
            }

            // Rechnungsebene: Steuersatz des Auf-/Abschlags (4.3: VATRate, ab 5.0: TaxItem)
            if (_is43)
            {
                item.TaxPercent = Dec(e, "VATRate");
            }
            else
            {
                XElement? taxItem = El(e, "TaxItem");
                item.TaxPercent = taxItem != null ? Dec(taxItem, "TaxPercent") : null;
                item.TaxCategoryCode = taxItem != null ? TaxCategory(taxItem) : null;
            }

            return item;
        }

        private ReductionOrSurcharge MapReductionBase(XElement e, ReductionOrSurchargeKind kind) => new ReductionOrSurcharge
        {
            Kind = kind,
            BaseAmount = Dec(e, "BaseAmount"),
            Percentage = Dec(e, "Percentage"),
            Amount = Dec(e, "Amount"),
            Comment = Text(e, "Comment"),
        };

        /// <summary>4.3: BaseAmount/Percentage/Amount/TaxID/Comment (+VATRate); ab 5.0: wie TaxItem plus TaxID.</summary>
        private ReductionOrSurcharge MapOtherVatableTax(XElement e)
        {
            if (_is43)
            {
                ReductionOrSurcharge item = MapReductionBase(e, ReductionOrSurchargeKind.OtherVatableTax);
                item.TaxId = Text(e, "TaxID");
                item.TaxPercent = Dec(e, "VATRate");
                return item;
            }

            return new ReductionOrSurcharge
            {
                Kind = ReductionOrSurchargeKind.OtherVatableTax,
                BaseAmount = Dec(e, "TaxableAmount"),
                Amount = Dec(e, "TaxAmount"),
                Comment = Text(e, "Comment"),
                TaxId = Text(e, "TaxID"),
                TaxPercent = Dec(e, "TaxPercent"),
                TaxCategoryCode = TaxCategory(e),
            };
        }

        // --- Steuer -------------------------------------------------------------------------------

        private TaxItem MapTaxItem(XElement e)
        {
            if (_is43)
            {
                XElement? exemption = El(e, "TaxExemption");
                return new TaxItem
                {
                    TaxableAmount = Dec(e, "TaxedAmount") ?? 0m,
                    TaxPercent = Dec(e, "VATRate") ?? 0m,
                    TaxAmount = Dec(e, "Amount"),
                    TaxExemptionReason = exemption?.Value,
                    TaxExemptionCode = exemption != null ? Attr(exemption, "TaxExemptionCode") : null,
                };
            }

            return new TaxItem
            {
                TaxableAmount = Dec(e, "TaxableAmount") ?? 0m,
                TaxPercent = Dec(e, "TaxPercent") ?? 0m,
                TaxCategoryCode = TaxCategory(e),
                TaxAmount = Dec(e, "TaxAmount"),
                Comment = Text(e, "Comment"),
            };
        }

        /// <summary>4.3 und 5.0: Comment + Amount; ab 6.0: TaxableAmount, TaxPercent, TaxAmount, Comment.</summary>
        private OtherTax MapOtherTax(XElement e) => new OtherTax
        {
            TaxableAmount = Dec(e, "TaxableAmount"),
            TaxPercent = Dec(e, "TaxPercent"),
            TaxCategoryCode = El(e, "TaxPercent") != null ? TaxCategory(e) : null,
            TaxAmount = Dec(e, "TaxAmount") ?? Dec(e, "Amount") ?? 0m,
            Comment = Text(e, "Comment") ?? string.Empty,
        };

        private string? TaxCategory(XElement parentOfTaxPercent)
        {
            XElement? percent = El(parentOfTaxPercent, "TaxPercent");
            return percent != null ? Attr(percent, "TaxCategoryCode") : null;
        }

        // --- Zahlung ------------------------------------------------------------------------------

        private PaymentMethod? MapPaymentMethod(XElement e)
        {
            PaymentMethod? method = null;
            foreach (XElement child in e.Elements().Where(c => c.Name.Namespace == _ns))
            {
                switch (child.Name.LocalName)
                {
                    case "NoPayment": method = new NoPayment(); break;
                    case "DirectDebit": method = new DirectDebit(); break;
                    case "OtherPayment": method = new OtherPayment(); break;
                    case "UniversalBankTransaction": method = MapUniversalBankTransaction(child); break;
                    case "SEPADirectDebit": method = MapSepaDirectDebit(child); break;
                    case "PaymentCard":
                        method = new PaymentCard
                        {
                            PrimaryAccountNumber = Text(child, "PrimaryAccountNumber") ?? string.Empty,
                            CardHolderName = Text(child, "CardHolderName"),
                        };
                        break;
                }
            }

            if (method != null) method.Comment = Text(e, "Comment");
            return method;
        }

        private UniversalBankTransaction MapUniversalBankTransaction(XElement e)
        {
            XElement? reference = El(e, "PaymentReference");
            var transaction = new UniversalBankTransaction
            {
                PaymentReference = reference?.Value,
                PaymentReferenceCheckSum = reference != null ? Attr(reference, "CheckSum") : null,
                ConsolidatorPayable = Bool(Attr(e, "ConsolidatorPayable")),
            };
            transaction.BeneficiaryAccounts.AddRange(Els(e, "BeneficiaryAccount").Select(account =>
            {
                XElement? bankCode = El(account, "BankCode");
                return new BankAccount
                {
                    BankName = Text(account, "BankName"),
                    BankCode = bankCode?.Value,
                    BankCodeType = bankCode != null ? Attr(bankCode, "BankCodeType") : null,
                    Bic = Text(account, "BIC"),
                    BankAccountNumber = Text(account, "BankAccountNr"),
                    Iban = Text(account, "IBAN"),
                    BankAccountOwner = Text(account, "BankAccountOwner"),
                };
            }));
            return transaction;
        }

        private SepaDirectDebit MapSepaDirectDebit(XElement e) => new SepaDirectDebit
        {
            Type = Text(e, "Type"),
            Bic = Text(e, "BIC"),
            Iban = Text(e, "IBAN"),
            BankAccountOwner = Text(e, "BankAccountOwner"),
            CreditorId = Text(e, "CreditorID"),
            MandateReference = Text(e, "MandateReference"),
            DebitCollectionDate = Date(e, "DebitCollectionDate"),
        };

        private PaymentConditions MapPaymentConditions(XElement e)
        {
            var conditions = new PaymentConditions
            {
                DueDate = Date(e, "DueDate"),
                MinimumPayment = Dec(e, "MinimumPayment"),
                Comment = Text(e, "Comment"),
            };
            conditions.Discounts.AddRange(Els(e, "Discount").Select(d => new Discount
            {
                PaymentDate = Date(d, "PaymentDate") ?? default,
                BaseAmount = Dec(d, "BaseAmount"),
                Percentage = Dec(d, "Percentage"),
                Amount = Dec(d, "Amount"),
            }));
            return conditions;
        }

        // --- Hilfen -------------------------------------------------------------------------------

        private XElement? El(XElement? parent, string name) => parent?.Element(_ns + name);

        private IEnumerable<XElement> Els(XElement? parent, string name) =>
            parent?.Elements(_ns + name) ?? Enumerable.Empty<XElement>();

        private string? Text(XElement parent, string name) => El(parent, name)?.Value;

        private decimal? Dec(XElement parent, string name) => ParseNullableDecimal(Text(parent, name));

        private DateTime? Date(XElement parent, string name) => ParseNullableDate(Text(parent, name));

        /// <summary>In 4.3 sind Attribute qualifiziert (attributeFormDefault="qualified"), ab 5.0 nicht.</summary>
        private string? Attr(XElement element, string localName) =>
            element.Attribute(_is43 ? _ns + localName : XName.Get(localName))?.Value;

        private static T? Map<T>(XElement? element, Func<XElement, T?> map) where T : class =>
            element == null ? null : map(element);

        private static decimal ParseDecimal(string text) => XmlConvert.ToDecimal(text.Trim());

        private static decimal? ParseNullableDecimal(string? text) =>
            string.IsNullOrWhiteSpace(text) ? null : XmlConvert.ToDecimal(text!.Trim());

        /// <summary>xs:date, ggf. mit Zeitzone; übernommen wird nur das Datum.</summary>
        private static DateTime? ParseNullableDate(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return DateTime.ParseExact(text!.Trim().Substring(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        private static bool? Bool(string? text) =>
            string.IsNullOrWhiteSpace(text) ? null : XmlConvert.ToBoolean(text!.Trim());

        private static DocumentType? ParseDocumentType(string? text) =>
            text != null && Enum.TryParse(text.Trim(), ignoreCase: false, out DocumentType type) ? type : (DocumentType?)null;
    }
}
