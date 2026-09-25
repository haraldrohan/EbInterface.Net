using System;
using System.Collections.Generic;
using System.Linq;

namespace EbInterface.Model
{
    /// <summary>
    /// Eine ebInterface-Rechnung, unabhängig von der Schema-Version (4.3 bis 6.1).
    /// Nicht abgebildet sind derzeit Erweiterungen (<c>Extension</c>), Signaturen, <c>AdditionalInformation</c>,
    /// <c>Classification</c>, Fremdwährungsangaben und <c>PresentationDetails</c>; sie gehen beim Lesen verloren.
    /// </summary>
    public sealed class EbInvoice
    {
        /// <summary>Version, aus der die Rechnung gelesen wurde; <see cref="EbInterfaceVersion.Unknown"/> bei neu erstellten Rechnungen.</summary>
        public EbInterfaceVersion SourceVersion { get; set; }

        /// <summary>Erzeugendes System (Attribut <c>GeneratingSystem</c>).</summary>
        public string GeneratingSystem { get; set; } = string.Empty;

        /// <summary>Art des Dokuments.</summary>
        public DocumentType DocumentType { get; set; } = DocumentType.Invoice;

        /// <summary>Rechnungswährung nach ISO 4217, z. B. „EUR“.</summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>Sprache (4.3/5.0: dreistellig, z. B. „ger“; ab 6.0: zweistellig, z. B. „de“).</summary>
        public string? Language { get; set; }

        /// <summary>Titel des Dokuments.</summary>
        public string? DocumentTitle { get; set; }

        /// <summary>Kennzeichen „Duplikat“.</summary>
        public bool? IsDuplicate { get; set; }

        /// <summary>Kennzeichen „manuelle Bearbeitung“.</summary>
        public bool? ManualProcessing { get; set; }

        /// <summary>Rechnungsnummer.</summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>Rechnungsdatum.</summary>
        public DateTime InvoiceDate { get; set; }

        /// <summary>Stornierte Originalrechnung.</summary>
        public DocumentReference? CancelledOriginalDocument { get; set; }

        /// <summary>Bezogene Dokumente.</summary>
        public List<DocumentReference> RelatedDocuments { get; } = new List<DocumentReference>();

        /// <summary>Lieferung.</summary>
        public Delivery? Delivery { get; set; }

        /// <summary>Rechnungssteller.</summary>
        public Biller Biller { get; set; } = new Biller();

        /// <summary>Rechnungsempfänger.</summary>
        public InvoiceRecipient InvoiceRecipient { get; set; } = new InvoiceRecipient();

        /// <summary>Auftraggeber, falls abweichend vom Rechnungsempfänger.</summary>
        public OrderingParty? OrderingParty { get; set; }

        /// <summary>Kopftext der Details.</summary>
        public string? HeaderDescription { get; set; }

        /// <summary>Fußtext der Details.</summary>
        public string? FooterDescription { get; set; }

        /// <summary>Positionslisten mit den Rechnungszeilen.</summary>
        public List<ItemList> ItemLists { get; } = new List<ItemList>();

        /// <summary>Alle Rechnungszeilen aller Positionslisten in Dokumentreihenfolge.</summary>
        public IEnumerable<LineItem> AllLineItems => ItemLists.SelectMany(list => list.LineItems);

        /// <summary>Beträge nach der Steuer, z. B. Pfand (4.3 und 6.1).</summary>
        public List<BelowTheLineItem> BelowTheLineItems { get; } = new List<BelowTheLineItem>();

        /// <summary>Auf- und Abschläge auf Rechnungsebene.</summary>
        public List<ReductionOrSurcharge> ReductionsAndSurcharges { get; } = new List<ReductionOrSurcharge>();

        /// <summary>Umsatzsteuer je Steuersatz.</summary>
        public List<TaxItem> TaxItems { get; } = new List<TaxItem>();

        /// <summary>Sonstige Steuern.</summary>
        public List<OtherTax> OtherTaxes { get; } = new List<OtherTax>();

        /// <summary>Gesamtbetrag brutto.</summary>
        public decimal TotalGrossAmount { get; set; }

        /// <summary>Bereits bezahlter Betrag (ab 5.0).</summary>
        public decimal? PrepaidAmount { get; set; }

        /// <summary>Rundungsbetrag (ab 5.0).</summary>
        public decimal? RoundingAmount { get; set; }

        /// <summary>Zu zahlender Betrag.</summary>
        public decimal PayableAmount { get; set; }

        /// <summary>Zahlungsart.</summary>
        public PaymentMethod? PaymentMethod { get; set; }

        /// <summary>Zahlungsbedingungen.</summary>
        public PaymentConditions? PaymentConditions { get; set; }

        /// <summary>Freitext zur Rechnung.</summary>
        public string? Comment { get; set; }
    }

    /// <summary>Art des Dokuments (Attribut <c>DocumentType</c>).</summary>
    public enum DocumentType
    {
        /// <summary>Rechnung.</summary>
        Invoice,

        /// <summary>Anzahlungsrechnung.</summary>
        InvoiceForAdvancePayment,

        /// <summary>Teilrechnung.</summary>
        InvoiceForPartialDelivery,

        /// <summary>Schlussrechnung.</summary>
        FinalSettlement,

        /// <summary>Gutschrift.</summary>
        CreditMemo,

        /// <summary>Gutschriftsverfahren (Selbstfakturierung).</summary>
        SelfBilling,

        /// <summary>Nachträgliche Gutschrift.</summary>
        SubsequentCredit,

        /// <summary>Nachträgliche Belastung.</summary>
        SubsequentDebit,
    }

    /// <summary>Verweis auf ein anderes Dokument (stornierte oder bezogene Rechnung).</summary>
    public sealed class DocumentReference
    {
        /// <summary>Rechnungsnummer des Dokuments.</summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>Datum des Dokuments.</summary>
        public DateTime? InvoiceDate { get; set; }

        /// <summary>Art des Dokuments.</summary>
        public DocumentType? DocumentType { get; set; }

        /// <summary>Freitext.</summary>
        public string? Comment { get; set; }
    }
}
