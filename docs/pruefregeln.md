# Prüfregeln und Fehlercodes

Die Prüfung läuft in Stufen. Eine Stufe läuft nur, wenn die vorige ohne Fehler bestanden wurde.
Die Codes sind stabil: Nach der ersten veröffentlichten Version ändert sich ihre Bedeutung nicht mehr.

## Standard (immer)

| Code | Stufe | Bedeutung |
|---|---|---|
| XML-01 | XML | Das Dokument ist kein wohlgeformtes XML (DTDs werden aus Sicherheitsgründen abgelehnt). |
| VER-01 | Version | Kein ebInterface: Das Wurzelelement ist nicht `Invoice` in einem ebInterface-Namespace. |
| VER-02 | Version | Die Version wird erkannt, aber von dieser Bibliotheksversion noch nicht geprüft (derzeit 4.3). |
| VER-03 | Version | Veraltete ebInterface-Version (z. B. 4.2 oder 3.0), die nicht unterstützt wird. |
| XSD-01 | Schema | Verstoß gegen das XML-Schema der erkannten Version. |

## e-Rechnung.gv.at (`ValidationProfile.ERechnungGvAt`)

Quelle: [e-Rechnung.gv.at – ebInterface](https://www.erechnung.gv.at/erb/tec_formats_ebinterface) sowie die Seiten
zur Auftragsreferenz für den [Bund](https://www.erechnung.gv.at/go/orderref_fedgov) und für
[andere Empfänger](https://www.erechnung.gv.at/go/orderref_others). Stand: 25.09.2026.

Ob eine Bestellnummer, Einkäufergruppe oder Lieferantennummer tatsächlich existiert, kann nur e-Rechnung.gv.at
selbst prüfen. Die Bibliothek prüft das Format.

| Code | Art | Regel |
|---|---|---|
| ERB-01 | Fehler | `DocumentType` nur Invoice, InvoiceForAdvancePayment, InvoiceForPartialDelivery, FinalSettlement oder CreditMemo. |
| ERB-02 | Fehler | `InvoiceRecipient/OrderReference/OrderID` (Auftragsreferenz) fehlt. |
| ERB-03 | Fehler | Auftragsreferenz hat kein gültiges Format (siehe unten). |
| ERB-04 | Fehler | Die Rechnung bezieht sich auf mehrere Bestellungen. |
| ERB-05 | Fehler | Bestellnummer des Bundes: Eine Zeile hat keine `InvoiceRecipientsOrderReference` mit `OrderID` und numerischer `OrderPositionNumber`. |
| ERB-06 | Warnung | Die Zeilen verweisen auf eine andere Referenz als die Auftragsreferenz im Kopf. |
| ERB-07 | Fehler | `Biller/InvoiceRecipientsBillerID` (Lieferantennummer) fehlt. |
| ERB-08 | Fehler | Im `Biller` ist keine E-Mail-Adresse angegeben. |
| ERB-09 | Warnung | Firmenangaben nach § 14 UGB (`FurtherIdentification` FS, FN, FBG) fehlen. |
| ERB-10 | Fehler | Mehr als 999 Zeilen (6.1 und 4.3: Rechnungs- und Below-The-Line-Zeilen zusammen; 5.0 und 6.0: Rechnungszeilen). |
| ERB-11 | Fehler | Mehr als zwei `Discount`-Elemente. |
| ERB-12 | Fehler | Skonto-Prozentsatz nicht größer als 0 und kleiner als 100. |
| ERB-13 | Fehler | Zahlungsart `PaymentCard` oder `OtherPayment`. |
| ERB-14 | Fehler | Rechnung ohne Zahlungsart (nur Gutschriften dürfen ohne sein). |
| ERB-15 | Fehler | `UniversalBankTransaction` ohne genau ein `BeneficiaryAccount`. |
| ERB-16 | Fehler | Empfängerkonto ohne IBAN. |
| ERB-17 | Fehler | `SEPADirectDebit` ohne Type, IBAN, BankAccountOwner, CreditorID, MandateReference oder DebitCollectionDate. |
| ERB-18 | Fehler | Zahlungsziel (`PaymentConditions/DueDate`) mehr als 999 Tage nach dem Stichtag. |
| ERB-19 | Fehler | `NoPayment` bei einer Rechnung, die weder Gutschrift ist noch 0 Euro beträgt. |
| ERB-20 | Fehler | Lieferbeschreibung (`Delivery/Description`) länger als 500 Zeichen. |
| ERB-21 | Fehler | Weder Menge noch Einzelpreis geteilt durch `BaseQuantity` ergibt höchstens vier Nachkommastellen. |
| ERB-30 | Warnung | `TradingName` wird nicht ausgewertet (5.0, 6.0, 6.1). |
| ERB-31 | Warnung | `AddressExtension` wird nicht ausgewertet. |
| ERB-32 | Warnung | Erweiterungselemente (`Extension`) werden nicht ausgewertet (6.0, 6.1). |
| ERB-33 | Warnung | `MinimumPayment` wird nicht ausgewertet. |
| ERB-34 | Warnung | `ListLineItem/DiscountFlag` wird nicht ausgewertet. |
| ERB-35 | Warnung | `PrepaidAmount` wird nicht ausgewertet (5.0, 6.0, 6.1). |
| ERB-36 | Warnung | `ListLineItem/AdditionalInformation` wird nicht ausgewertet (4.3). |
| ERB-37 | Warnung | `PresentationDetails` wird nicht ausgewertet (4.3). |
| ERB-38 | Warnung | `VATRate/@TaxCode` wird nicht ausgewertet (4.3). |

### Auftragsreferenz (ERB-03)

Die Art des Empfängers ergibt sich allein aus dem Format der Auftragsreferenz:

1. Enthält sie einen `/`, ist sie die Referenz eines **anderen Empfängers** (Land, Gemeinde, ausgegliederte Stelle):
   mindestens zwei Zeichen Empfängeridentifikation ohne Leerzeichen, dann `/`, dann optional eine interne Referenz
   mit höchstens 50 Zeichen. Beispiele: `Z0/`, `Z0/Aktenzahl 123`.
2. Sonst muss sie eine Form des **Bundes** haben:
   - Bestellnummer: genau zehn Ziffern, z. B. `4700000001`. Dann braucht jede Zeile eine Bestellpositionsnummer (ERB-05).
   - Einkäufergruppe: genau drei Buchstaben oder Ziffern, auch Umlaute, z. B. `Z01` oder `63Ü`.
   - Einkäufergruppe:Referenz: Einkäufergruppe, Doppelpunkt, höchstens 50 Zeichen, z. B. `Z01:111599`.
3. Alles andere ist ungültig.

### Auslegungen

Wo die Quelle Spielraum lässt, prüft die Bibliothek so, dass sie gültige Rechnungen nicht fälschlich ablehnt:

- **Andere Empfänger:** Die Quelle verlangt „zumindest 3stellig, z. B. `Z0/`“. Der Schrägstrich zählt also mit, und die
  Empfängeridentifikation braucht mindestens zwei Zeichen. Sie wird nicht auf Buchstaben und Ziffern beschränkt,
  damit die empfohlenen Verwaltungskennzeichen nicht an Sonderzeichen scheitern.
- **Zeilenreferenzen:** Die Zeilen-`OrderID` „sollte“ laut Quelle gleich der Auftragsreferenz sein. Ein Fehler (ERB-04)
  entsteht nur bei einer Bestellnummer des Bundes oder wenn die Zeilen auf verschiedene Bestellungen zeigen; sonst gibt
  es eine Warnung (ERB-06).
- **Firmenangaben (ERB-09):** § 14 UGB gilt nur für Unternehmen im Firmenbuch. Das ist offline nicht feststellbar,
  daher gibt es eine Warnung statt eines Fehlers.
- **BaseQuantity (ERB-21):** Abgelehnt wird nur, wenn *beide* Divisionen mehr als vier Nachkommastellen ergeben.
  Abschließende Nullen zählen nicht.
- **Lieferbeschreibung (ERB-20):** Die Quelle nennt `Delivery/Comment`; im Schema heißt das Element `Description`.
- **Stichtag (ERB-18):** Vorgabe ist das heutige Datum; über `ValidationOptions.ReferenceDate` einstellbar.
- **Nicht ausgewertete Felder (ERB-30 ff.):** Das sind keine Fehler. Die Warnung erscheint je Feld einmal.
