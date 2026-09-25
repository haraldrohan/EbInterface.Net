using System;
using System.Collections.Generic;

namespace EbInterface.Model
{
    /// <summary>Gemeinsame Angaben von Rechnungssteller, Rechnungsempfänger und Auftraggeber.</summary>
    public abstract class Party
    {
        /// <summary>UID-Nummer; „00000000“, wenn keine vorhanden ist.</summary>
        public string VatIdentificationNumber { get; set; } = string.Empty;

        /// <summary>Weitere Kennungen, z. B. Firmenbuchnummer (FN), Firmenbuchgericht (FBG), Firmensitz (FS).</summary>
        public List<FurtherIdentification> FurtherIdentifications { get; } = new List<FurtherIdentification>();

        /// <summary>Auftragsreferenz dieser Partei (beim Rechnungsempfänger: die Auftragsreferenz für e-Rechnung.gv.at).</summary>
        public OrderReference? OrderReference { get; set; }

        /// <summary>Anschrift.</summary>
        public Address? Address { get; set; }

        /// <summary>Ansprechperson (in 4.3 aus <c>Address/Salutation</c> und <c>Address/Contact</c>).</summary>
        public Contact? Contact { get; set; }
    }

    /// <summary>Rechnungssteller.</summary>
    public sealed class Biller : Party
    {
        /// <summary>Lieferantennummer beim Rechnungsempfänger.</summary>
        public string? InvoiceRecipientsBillerId { get; set; }
    }

    /// <summary>Rechnungsempfänger.</summary>
    public sealed class InvoiceRecipient : Party
    {
        /// <summary>Kundennummer beim Rechnungssteller.</summary>
        public string? BillersInvoiceRecipientId { get; set; }

        /// <summary>Buchungskreis.</summary>
        public string? AccountingArea { get; set; }

        /// <summary>Kennung der Unterorganisation.</summary>
        public string? SubOrganizationId { get; set; }
    }

    /// <summary>Auftraggeber.</summary>
    public sealed class OrderingParty : Party
    {
        /// <summary>Kennung des Auftraggebers beim Rechnungssteller.</summary>
        public string BillersOrderingPartyId { get; set; } = string.Empty;
    }

    /// <summary>Weitere Kennung einer Partei (Element <c>FurtherIdentification</c>).</summary>
    public sealed class FurtherIdentification
    {
        /// <summary>Art der Kennung, z. B. „FN“, „FBG“, „FS“, „DVR“.</summary>
        public string IdentificationType { get; set; } = string.Empty;

        /// <summary>Wert.</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Auftragsreferenz (Element <c>OrderReference</c>).</summary>
    public sealed class OrderReference
    {
        /// <summary>Auftrags- bzw. Bestellnummer.</summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>Datum der Bestellung.</summary>
        public DateTime? ReferenceDate { get; set; }

        /// <summary>Beschreibung.</summary>
        public string? Description { get; set; }
    }

    /// <summary>Anschrift.</summary>
    public sealed class Address
    {
        /// <summary>Kennungen der Anschrift, z. B. GLN.</summary>
        public List<AddressIdentifier> AddressIdentifiers { get; } = new List<AddressIdentifier>();

        /// <summary>Name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Handelsname (ab 5.0).</summary>
        public string? TradingName { get; set; }

        /// <summary>Straße und Hausnummer.</summary>
        public string? Street { get; set; }

        /// <summary>Postfach.</summary>
        public string? POBox { get; set; }

        /// <summary>Ort.</summary>
        public string Town { get; set; } = string.Empty;

        /// <summary>Postleitzahl.</summary>
        public string Zip { get; set; } = string.Empty;

        /// <summary>Ländercode nach ISO 3166-1, z. B. „AT“.</summary>
        public string? CountryCode { get; set; }

        /// <summary>Ländername, z. B. „Österreich“.</summary>
        public string CountryName { get; set; } = string.Empty;

        /// <summary>Telefonnummern.</summary>
        public List<string> Phones { get; } = new List<string>();

        /// <summary>E-Mail-Adressen.</summary>
        public List<string> Emails { get; } = new List<string>();
    }

    /// <summary>Kennung einer Anschrift (Element <c>AddressIdentifier</c>).</summary>
    public sealed class AddressIdentifier
    {
        /// <summary>Art, z. B. „GLN“, „DUNS“, „ProprietaryAddressID“.</summary>
        public string? Type { get; set; }

        /// <summary>Wert.</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Ansprechperson.</summary>
    public sealed class Contact
    {
        /// <summary>Anrede.</summary>
        public string? Salutation { get; set; }

        /// <summary>Name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Telefonnummern.</summary>
        public List<string> Phones { get; } = new List<string>();

        /// <summary>E-Mail-Adressen.</summary>
        public List<string> Emails { get; } = new List<string>();
    }
}
