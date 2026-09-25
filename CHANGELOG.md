# Changelog

Alle wesentlichen Änderungen werden hier dokumentiert. Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/), die Versionierung [Semantic Versioning](https://semver.org/lang/de/).

## [Unveröffentlicht]

### Hinzugefügt
- Erkennung der ebInterface-Version 4.3, 5.0, 6.0 und 6.1 anhand des Namespace.
- Schema-Prüfung (XSD) für 5.0, 6.0 und 6.1 mit Zeilen- und Spaltenangabe.
- Sicheres Einlesen ohne DTD-Verarbeitung (XXE-Schutz).
- Tests gegen die offiziellen Beispielrechnungen von AUSTRIAPRO.
- Erste Bundesregeln von e-Rechnung.gv.at: zulässige Dokumenttypen und Empfänger-Auftragsreferenz.
- Prüfung des Firmensitzes des Rechnungsstellers über `FurtherIdentification` mit `IdentificationType="FS"`.
- Prüfung der maximal vier Nachkommastellen bei Division durch `BaseQuantity`.
- Prüfung der Auftragsreferenzformate, der Bundespositionsnummern sowie von Lieferantennummer und Biller-E-Mail.
- Prüfung der maximal 999 Rechnungszeilen sowie der Anzahl und Grenzen von Skonto-Elementen.
- Prüfung unterstützter Zahlungsarten sowie der Konten- und IBAN-Regeln für Überweisungen.
