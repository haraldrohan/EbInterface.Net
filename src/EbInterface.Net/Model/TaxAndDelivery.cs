using System;

namespace EbInterface.Model
{
    /// <summary>Umsatzsteuer zu einem Steuersatz (5.0+: <c>Tax/TaxItem</c>, 4.3: <c>Tax/VAT/VATItem</c>).</summary>
    public sealed class TaxItem
    {
        /// <summary>Bemessungsgrundlage.</summary>
        public decimal TaxableAmount { get; set; }

        /// <summary>Steuersatz in Prozent; 0 bei Steuerbefreiung.</summary>
        public decimal TaxPercent { get; set; }

        /// <summary>Steuerkategorie nach UNCL 5305, z. B. „S“ (ab 5.0).</summary>
        public string? TaxCategoryCode { get; set; }

        /// <summary>Steuerbetrag (ab 5.0 optional).</summary>
        public decimal? TaxAmount { get; set; }

        /// <summary>Begründung der Steuerbefreiung (4.3).</summary>
        public string? TaxExemptionReason { get; set; }

        /// <summary>Code der Steuerbefreiung (4.3).</summary>
        public string? TaxExemptionCode { get; set; }

        /// <summary>Freitext (ab 5.0).</summary>
        public string? Comment { get; set; }
    }

    /// <summary>Sonstige Steuer (Element <c>OtherTax</c>).</summary>
    public sealed class OtherTax
    {
        /// <summary>Bemessungsgrundlage (ab 6.0).</summary>
        public decimal? TaxableAmount { get; set; }

        /// <summary>Steuersatz in Prozent (ab 6.0).</summary>
        public decimal? TaxPercent { get; set; }

        /// <summary>Steuerkategorie (ab 6.0).</summary>
        public string? TaxCategoryCode { get; set; }

        /// <summary>Betrag.</summary>
        public decimal TaxAmount { get; set; }

        /// <summary>Bezeichnung.</summary>
        public string Comment { get; set; } = string.Empty;
    }

    /// <summary>Lieferung (Element <c>Delivery</c>).</summary>
    public sealed class Delivery
    {
        /// <summary>Lieferscheinnummer o. Ä.</summary>
        public string? DeliveryId { get; set; }

        /// <summary>Lieferdatum; <c>null</c>, wenn ein Zeitraum angegeben ist.</summary>
        public DateTime? Date { get; set; }

        /// <summary>Beginn des Leistungszeitraums.</summary>
        public DateTime? PeriodFrom { get; set; }

        /// <summary>Ende des Leistungszeitraums.</summary>
        public DateTime? PeriodTo { get; set; }

        /// <summary>Lieferanschrift.</summary>
        public Address? Address { get; set; }

        /// <summary>Ansprechperson.</summary>
        public Contact? Contact { get; set; }

        /// <summary>Beschreibung (bei e-Rechnung.gv.at höchstens 500 Zeichen).</summary>
        public string? Description { get; set; }
    }
}
