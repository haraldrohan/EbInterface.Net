using System;
using System.Collections.Generic;

namespace EbInterface.Model
{
    /// <summary>Zahlungsart. Konkrete Arten: <see cref="UniversalBankTransaction"/>, <see cref="SepaDirectDebit"/>,
    /// <see cref="NoPayment"/>, <see cref="DirectDebit"/>, <see cref="PaymentCard"/>, <see cref="OtherPayment"/>.</summary>
    public abstract class PaymentMethod
    {
        // Nur die Zahlungsarten dieser Bibliothek – eine eigene Ableitung würde sonst beim Schreiben falsch abgebildet.
        internal PaymentMethod()
        {
        }

        /// <summary>Freitext zur Zahlung.</summary>
        public string? Comment { get; set; }
    }

    /// <summary>Überweisung.</summary>
    public sealed class UniversalBankTransaction : PaymentMethod
    {
        /// <summary>Empfängerkonten (bei e-Rechnung.gv.at genau eines).</summary>
        public List<BankAccount> BeneficiaryAccounts { get; } = new List<BankAccount>();

        /// <summary>Zahlungsreferenz.</summary>
        public string? PaymentReference { get; set; }

        /// <summary>Prüfsumme der Zahlungsreferenz.</summary>
        public string? PaymentReferenceCheckSum { get; set; }

        /// <summary>Zahlung an einen Konsolidator.</summary>
        public bool? ConsolidatorPayable { get; set; }
    }

    /// <summary>Bankverbindung.</summary>
    public sealed class BankAccount
    {
        /// <summary>Name der Bank.</summary>
        public string? BankName { get; set; }

        /// <summary>Bankleitzahl.</summary>
        public string? BankCode { get; set; }

        /// <summary>Art der Bankleitzahl (Ländercode).</summary>
        public string? BankCodeType { get; set; }

        /// <summary>BIC.</summary>
        public string? Bic { get; set; }

        /// <summary>Kontonummer.</summary>
        public string? BankAccountNumber { get; set; }

        /// <summary>IBAN.</summary>
        public string? Iban { get; set; }

        /// <summary>Kontoinhaber.</summary>
        public string? BankAccountOwner { get; set; }
    }

    /// <summary>SEPA-Lastschrift.</summary>
    public sealed class SepaDirectDebit : PaymentMethod
    {
        /// <summary>Art: „B2C“ oder „B2B“.</summary>
        public string? Type { get; set; }

        /// <summary>BIC.</summary>
        public string? Bic { get; set; }

        /// <summary>IBAN des Zahlungspflichtigen.</summary>
        public string? Iban { get; set; }

        /// <summary>Kontoinhaber.</summary>
        public string? BankAccountOwner { get; set; }

        /// <summary>Gläubiger-ID.</summary>
        public string? CreditorId { get; set; }

        /// <summary>Mandatsreferenz.</summary>
        public string? MandateReference { get; set; }

        /// <summary>Einzugsdatum.</summary>
        public DateTime? DebitCollectionDate { get; set; }
    }

    /// <summary>Keine Zahlung (Gutschrift oder 0-Euro-Rechnung).</summary>
    public sealed class NoPayment : PaymentMethod
    {
    }

    /// <summary>Lastschrift ohne weitere Angaben (nur 4.3).</summary>
    public sealed class DirectDebit : PaymentMethod
    {
    }

    /// <summary>Kartenzahlung (ab 5.0).</summary>
    public sealed class PaymentCard : PaymentMethod
    {
        /// <summary>Kartennummer (üblicherweise gekürzt).</summary>
        public string PrimaryAccountNumber { get; set; } = string.Empty;

        /// <summary>Karteninhaber.</summary>
        public string? CardHolderName { get; set; }
    }

    /// <summary>Andere Zahlungsart (ab 5.0).</summary>
    public sealed class OtherPayment : PaymentMethod
    {
    }

    /// <summary>Zahlungsbedingungen.</summary>
    public sealed class PaymentConditions
    {
        /// <summary>Zahlungsziel.</summary>
        public DateTime? DueDate { get; set; }

        /// <summary>Skonti.</summary>
        public List<Discount> Discounts { get; } = new List<Discount>();

        /// <summary>Mindestzahlung.</summary>
        public decimal? MinimumPayment { get; set; }

        /// <summary>Freitext.</summary>
        public string? Comment { get; set; }
    }

    /// <summary>Skonto.</summary>
    public sealed class Discount
    {
        /// <summary>Zahlung bis zu diesem Datum.</summary>
        public DateTime PaymentDate { get; set; }

        /// <summary>Basisbetrag.</summary>
        public decimal? BaseAmount { get; set; }

        /// <summary>Prozentsatz.</summary>
        public decimal? Percentage { get; set; }

        /// <summary>Betrag.</summary>
        public decimal? Amount { get; set; }
    }
}
