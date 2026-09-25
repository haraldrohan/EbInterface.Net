# Prüfregeln und Fehlercodes

Die Prüfung läuft in Stufen. Eine Stufe läuft nur, wenn die vorige ohne Fehler bestanden wurde.
Die Codes sind stabil: Nach der ersten veröffentlichten Version ändert sich ihre Bedeutung nicht mehr.
Die Meldungen sind deutsch, unabhängig von Sprache und Version der .NET-Laufzeit; nur XSD-01 enthält zusätzlich den Originaltext von .NET.

## Standard (immer)

| Code | Stufe | Bedeutung |
|---|---|---|
| XML-01 | XML | Das Dokument ist kein wohlgeformtes XML (DTDs werden aus Sicherheitsgründen abgelehnt). |
| VER-01 | Version | Kein ebInterface: Das Wurzelelement ist nicht `Invoice` in einem ebInterface-Namespace. |
| VER-02 | Version | Die Version wird erkannt, aber von dieser Bibliotheksversion noch nicht geprüft (derzeit keine; vorgesehen für neue Versionen wie 7.0). |
| VER-03 | Version | Veraltete ebInterface-Version (z. B. 4.2 oder 3.0), die nicht unterstützt wird. |
| XSD-01 | Schema | Sonstiger Verstoß gegen das XML-Schema (Meldung von .NET, wenn keine genauere Einordnung möglich ist). |
| XSD-02 | Schema | Pflichtelement fehlt am Ende eines Elements. |
| XSD-03 | Schema | Element an dieser Stelle nicht erlaubt: falsche Reihenfolge, fehlendes Pflichtelement davor oder zu viele Wiederholungen. |
| XSD-04 | Schema | Element ist in dieser ebInterface-Version nicht definiert (Tippfehler, fremder Namespace). |
| XSD-05 | Schema | Text in einem Element, das nur Unterelemente enthalten darf. |
| XSD-06 | Schema | Ungültiger Wert in einem Element oder Attribut, mit Grund (Format, zulässige Werte, Länge, Nachkommastellen, Grenzen). |
| XSD-07 | Schema | Pflichtattribut fehlt. |
| XSD-08 | Schema | Attribut ist an diesem Element nicht vorgesehen. |

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
| ERB-04 | Warnung | Die Zeilen verweisen auf mehrere Bestellungen (laut Regelseite unzulässig; der Test-Upload lehnt es nicht ab). |
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
| ERB-22 | Fehler | Skontodatum (`Discount/PaymentDate`) liegt nicht nach dem Stichtag. *Vom Test-Upload gemeldet (EBI61-0121).* |
| ERB-23 | Fehler | Österreichische UID-Nummer (`ATU…`) von Rechnungssteller oder -empfänger mit falscher Prüfziffer. *Vom Test-Upload gemeldet (AF-0069).* |
| ERB-24 | Fehler | Zahlungsziel (`PaymentConditions/DueDate`) liegt vor dem Stichtag. *Vom Test-Upload gemeldet (EBI61-0120).* |
| ERB-25 | Fehler | Skontodatum liegt nicht vor dem Zahlungsziel. *Vom Test-Upload gemeldet (EBI61-0017).* |
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
  Empfängeridentifikation braucht mindestens zwei Zeichen – echte Kennungen wie `L5` (Salzburg) und `L6`
  (Steiermark) bestätigen das. Nach dem Schrägstrich akzeptiert der Test-Upload auch weitere Schrägstriche,
  Bindestriche und Unterstriche (`L6/LRW/ABT08/COVID_IMPFEN`, `L5/REF-1000-165`). Welche interne Referenz ein
  Empfänger annimmt, legt er selbst fest; das ist offline nicht prüfbar.
- **Zeilenreferenzen:** Die Zeilen-`OrderID` „sollte“ laut Quelle gleich der Auftragsreferenz sein, und eine Rechnung
  darf sich nur auf eine Bestellung beziehen. Der Test-Upload lehnt abweichende oder mehrere Zeilenreferenzen aber
  nicht ab – auch nicht bei Bestellnummern des Bundes. Daher nur Warnungen (ERB-04, ERB-06).
- **Firmenangaben (ERB-09):** § 14 UGB gilt nur für Unternehmen im Firmenbuch. Das ist offline nicht feststellbar,
  daher gibt es eine Warnung statt eines Fehlers.
- **BaseQuantity (ERB-21):** Abgelehnt wird nur, wenn *beide* Divisionen mehr als vier Nachkommastellen ergeben.
  Abschließende Nullen zählen nicht.
- **Lieferbeschreibung (ERB-20):** Die Quelle nennt `Delivery/Comment`; im Schema heißt das Element `Description`.
- **Stichtag (ERB-18):** Vorgabe ist das heutige Datum; über `ValidationOptions.ReferenceDate` einstellbar.
- **Nicht ausgewertete Felder (ERB-30 ff.):** Das sind keine Fehler. Die Warnung erscheint je Feld einmal.

## Abgleich mit dem Test-Upload

Am 25./26.09.2026 wurden rund 45 eigene, synthetische Testrechnungen (6.1) in den
[Test-Upload](https://test.erechnung.gv.at/go/test_upload) geladen. Ergebnisse:

**Bestätigt:** Dokumenttypen, Auftragsreferenzen des Bundes (Bestellnummer, EKG auch mit Umlaut wie `63Ü`,
EKG:Referenz bis 50 Zeichen), Bestellpositionsnummern (ERB-05), 999 Zeilen einschließlich Below-The-Line (ERB-10),
BaseQuantity nur bei beiden Divisionen (ERB-21), fehlende FS/FN/FBG werden angenommen (daher Warnung ERB-09),
`TotalGrossAmount` wird nicht ausgewertet. 5.0 und 6.0 kennen keine Below-The-Line-Zeilen.

**Zusätzlich vom Portal geprüft und hier umgesetzt:** ERB-22 bis ERB-25.

**Vom Portal geprüft, offline nicht möglich:**
- ob eine Einkäufergruppe existiert (AF-0094) und ob Empfängerkennung und interne Referenz eines anderen Empfängers
  gültig sind (AF-0126). Andere Empfänger sind im Testsystem nur teilweise hinterlegt.
- Bestellnummern des Bundes werden im Testsystem nicht auf Existenz geprüft.

**Vom Portal geprüft, noch nicht umgesetzt:** Rechenprüfungen – Zeilenbetrag = Menge × Einzelpreis (AF-0025),
Zeilen-Bruttobetrag (AF-0026), `PayableAmount` = Summe der Bruttobeträge ± Auf- und Abschläge (AF-0028).
