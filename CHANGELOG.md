# Changelog

Alle wesentlichen Änderungen werden hier dokumentiert. Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/), die Versionierung [Semantic Versioning](https://semver.org/lang/de/).

## [Unveröffentlicht]

### Hinzugefügt
- Erkennung der ebInterface-Version 4.3, 5.0, 6.0 und 6.1 anhand des Namespace; ältere Versionen werden als nicht unterstützt gemeldet (`VER-03`).
- Schema-Prüfung (XSD) für 4.3, 5.0, 6.0 und 6.1 mit Zeilen- und Spaltenangabe. Verständliche deutsche Meldungen mit eigenen Codes (`XSD-02` bis `XSD-08`) für fehlende oder falsch platzierte Elemente, ungültige Werte (mit Grund, z. B. zulässige Werte oder Datumsformat) und Attribute – unabhängig von Sprache und Version der .NET-Laufzeit.
- Sicheres Einlesen ohne DTD-Verarbeitung (XXE-Schutz).
- Prüfprofil `ValidationProfile.ERechnungGvAt` mit den Regeln von e-Rechnung.gv.at für 4.3, 5.0, 6.0 und 6.1 (`ERB-01` bis `ERB-38`), darunter Auftragsreferenzen für Bund und andere Empfänger, Bestellpositionsnummern, Zahlungsarten, Skonto, Zeilenanzahl und `BaseQuantity`. Übersicht in `docs/pruefregeln.md`.
- `ValidationOptions.ReferenceDate` als Stichtag für datumsabhängige Regeln.
- Tests gegen die offiziellen Beispielrechnungen von AUSTRIAPRO und eigene Testrechnungen.
