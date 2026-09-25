# Changelog

Alle wesentlichen Änderungen werden hier dokumentiert. Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/), die Versionierung [Semantic Versioning](https://semver.org/lang/de/).

## [Unveröffentlicht]

### Hinzugefügt
- Rechnungsmodell `EbInvoice` (Namespace `EbInterface.Model`) und `EbInterfaceReader` zum Lesen von 4.3, 5.0, 6.0 und 6.1 in dasselbe Modell; ungültige Dokumente lösen `EbInterfaceReadException` mit dem Prüfergebnis aus.
- Erkennung der ebInterface-Version 4.3, 5.0, 6.0 und 6.1 anhand des Namespace; ältere Versionen werden als nicht unterstützt gemeldet (`VER-03`).
- Schema-Prüfung (XSD) für 4.3, 5.0, 6.0 und 6.1 mit Zeilen- und Spaltenangabe. Verständliche deutsche Meldungen mit eigenen Codes (`XSD-02` bis `XSD-08`) für fehlende oder falsch platzierte Elemente, ungültige Werte (mit Grund, z. B. zulässige Werte oder Datumsformat) und Attribute – unabhängig von Sprache und Version der .NET-Laufzeit.
- Sicheres Einlesen ohne DTD-Verarbeitung (XXE-Schutz).
- Prüfprofil `ValidationProfile.ERechnungGvAt` mit den Regeln von e-Rechnung.gv.at für 4.3, 5.0, 6.0 und 6.1 (`ERB-01` bis `ERB-39`), darunter Auftragsreferenzen für Bund und andere Empfänger, Bestellpositionsnummern, Zahlungsarten, Skonto, Zeilenanzahl und `BaseQuantity`. Übersicht in `docs/pruefregeln.md`.
- `ValidationOptions.ReferenceDate` als Stichtag für datumsabhängige Regeln.
- Regeln, die der Test-Upload von e-Rechnung.gv.at zusätzlich zur Regelseite prüft: Skontodatum nach dem Stichtag und vor dem Zahlungsziel (`ERB-22`, `ERB-25`), Prüfziffer österreichischer UID-Nummern (`ERB-23`), Zahlungsziel nicht in der Vergangenheit (`ERB-24`). Abweichende Zeilenreferenzen (`ERB-04`) sind nur noch eine Warnung, weil das Portal sie annimmt.
- Rechenprüfungen wie im Test-Upload: Zeilenbetrag (`ERB-26`), Zahlbetrag (`ERB-27`), Rundungsbetrag (`ERB-28`), Steuergrundlage (`ERB-29`) und Steuerbetrag (`ERB-39`), mit den dort ermittelten Toleranzen.
- Tests gegen die offiziellen Beispielrechnungen von AUSTRIAPRO und eigene Testrechnungen.
