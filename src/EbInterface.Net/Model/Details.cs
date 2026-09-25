using System;
using System.Collections.Generic;

namespace EbInterface.Model
{
    /// <summary>Positionsliste (Element <c>ItemList</c>).</summary>
    public sealed class ItemList
    {
        /// <summary>Kopftext.</summary>
        public string? HeaderDescription { get; set; }

        /// <summary>Rechnungszeilen.</summary>
        public List<LineItem> LineItems { get; } = new List<LineItem>();

        /// <summary>Fußtext.</summary>
        public string? FooterDescription { get; set; }
    }

    /// <summary>Rechnungszeile (Element <c>ListLineItem</c>).</summary>
    public sealed class LineItem
    {
        /// <summary>Positionsnummer.</summary>
        public string? PositionNumber { get; set; }

        /// <summary>Bezeichnungen der Leistung.</summary>
        public List<string> Descriptions { get; } = new List<string>();

        /// <summary>Artikelnummern.</summary>
        public List<ArticleNumber> ArticleNumbers { get; } = new List<ArticleNumber>();

        /// <summary>Menge.</summary>
        public decimal Quantity { get; set; }

        /// <summary>Mengeneinheit, z. B. „STK“ oder UN/ECE-Code „C62“.</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>Einzelpreis netto.</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>Preiseinheit: Der Einzelpreis gilt für diese Menge.</summary>
        public decimal? BaseQuantity { get; set; }

        /// <summary>Auf- und Abschläge dieser Zeile.</summary>
        public List<ReductionOrSurcharge> ReductionsAndSurcharges { get; } = new List<ReductionOrSurcharge>();

        /// <summary>Lieferung dieser Zeile.</summary>
        public Delivery? Delivery { get; set; }

        /// <summary>Auftragsreferenz des Rechnungsstellers.</summary>
        public LineOrderReference? BillersOrderReference { get; set; }

        /// <summary>Auftragsreferenz des Rechnungsempfängers (für den Bund mit Bestellpositionsnummer).</summary>
        public LineOrderReference? InvoiceRecipientsOrderReference { get; set; }

        /// <summary>Steuerbemessungsgrundlage der Zeile (ab 5.0).</summary>
        public decimal? TaxableAmount { get; set; }

        /// <summary>Steuersatz in Prozent; 0 bei Steuerbefreiung.</summary>
        public decimal TaxPercent { get; set; }

        /// <summary>Steuerkategorie nach UNCL 5305, z. B. „S“ (ab 5.0).</summary>
        public string? TaxCategoryCode { get; set; }

        /// <summary>Steuerbetrag der Zeile (ab 5.0, optional).</summary>
        public decimal? TaxAmount { get; set; }

        /// <summary>Begründung der Steuerbefreiung (4.3: <c>TaxExemption</c>).</summary>
        public string? TaxExemptionReason { get; set; }

        /// <summary>Code der Steuerbefreiung (4.3: Attribut <c>TaxExemptionCode</c>).</summary>
        public string? TaxExemptionCode { get; set; }

        /// <summary>Zeilenbetrag netto.</summary>
        public decimal LineItemAmount { get; set; }
    }

    /// <summary>Artikelnummer.</summary>
    public sealed class ArticleNumber
    {
        /// <summary>Art, z. B. „GTIN“, „InvoiceRecipientsArticleNumber“, „BillersArticleNumber“.</summary>
        public string? Type { get; set; }

        /// <summary>Wert.</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Auftragsreferenz auf Zeilenebene.</summary>
    public sealed class LineOrderReference
    {
        /// <summary>Auftrags- bzw. Bestellnummer.</summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>Datum der Bestellung.</summary>
        public DateTime? ReferenceDate { get; set; }

        /// <summary>Beschreibung.</summary>
        public string? Description { get; set; }

        /// <summary>Bestellpositionsnummer.</summary>
        public string? OrderPositionNumber { get; set; }
    }

    /// <summary>Betrag nach der Steuer (Element <c>BelowTheLineItem</c>, 4.3 und 6.1).</summary>
    public sealed class BelowTheLineItem
    {
        /// <summary>Bezeichnung.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Betrag.</summary>
        public decimal LineItemAmount { get; set; }

        /// <summary>Grund.</summary>
        public string? Reason { get; set; }

        /// <summary>Datum zum Grund.</summary>
        public DateTime? ReasonDate { get; set; }
    }

    /// <summary>Art eines Auf- oder Abschlags.</summary>
    public enum ReductionOrSurchargeKind
    {
        /// <summary>Abschlag (Rabatt).</summary>
        Reduction,

        /// <summary>Aufschlag.</summary>
        Surcharge,

        /// <summary>Sonstige umsatzsteuerpflichtige Abgabe.</summary>
        OtherVatableTax,
    }

    /// <summary>Auf- oder Abschlag auf Zeilen- oder Rechnungsebene.</summary>
    public sealed class ReductionOrSurcharge
    {
        /// <summary>Art.</summary>
        public ReductionOrSurchargeKind Kind { get; set; }

        /// <summary>Basisbetrag.</summary>
        public decimal? BaseAmount { get; set; }

        /// <summary>Prozentsatz.</summary>
        public decimal? Percentage { get; set; }

        /// <summary>Betrag.</summary>
        public decimal? Amount { get; set; }

        /// <summary>Freitext.</summary>
        public string? Comment { get; set; }

        /// <summary>Kennung der Abgabe (nur <see cref="ReductionOrSurchargeKind.OtherVatableTax"/>).</summary>
        public string? TaxId { get; set; }

        /// <summary>Steuersatz in Prozent (Rechnungsebene und sonstige Abgaben).</summary>
        public decimal? TaxPercent { get; set; }

        /// <summary>Steuerkategorie nach UNCL 5305 (ab 5.0).</summary>
        public string? TaxCategoryCode { get; set; }
    }
}
