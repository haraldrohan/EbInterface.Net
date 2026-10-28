# Changelog

Alle wesentlichen Änderungen werden hier dokumentiert. Das Format folgt [Keep a Changelog](https://keepachangelog.com/de/1.1.0/), die Versionierung [Semantic Versioning](https://semver.org/lang/de/).

## [Unveröffentlicht]

## [0.1.0-alpha] – 2026-10-28

Erste Vorabversion.

### Hinzugefügt

**Prüfen**
- Erkennung der ebInterface-Version 4.3, 5.0, 6.0 und 6.1 anhand des Namespace; ältere Versionen werden als nicht unterstützt gemeldet (`VER-03`).
- Schema-Prüfung (XSD) für 4.3, 5.0, 6.0 und 6.1 mit Zeilen- und Spaltenangabe und verständlichen deutschen Meldungen (`XSD-01` bis `XSD-08`), z. B. mit den zulässigen Werten oder dem erwarteten Datumsformat – unabhängig von Sprache und Version der .NET-Laufzeit.
- Prüfprofil `ValidationProfile.ERechnungGvAt` mit den Regeln von e-Rechnung.gv.at für 4.3 bis 6.1 (`ERB-01` bis `ERB-40`): Auftragsreferenzen für Bund und andere Empfänger, Bestellpositionsnummern, Zahlungsarten, IBAN- und UID-Prüfziffern, Skonto- und Fälligkeitsdaten, Zeilenanzahl, `BaseQuantity` sowie Rechenprüfungen für Zeilenbetrag, Zahlbetrag, Rundung, Steuergrundlage und Steuerbetrag. Abgeglichen mit rund 80 synthetischen Rechnungen im Test-Upload von e-Rechnung.gv.at; Übersicht und Auslegungen in `docs/pruefregeln.md`.
- `ValidationOptions.ReferenceDate` als Stichtag für datumsabhängige Regeln; `ValidationResult.Errors` und `Warnings`.
- Sicheres Einlesen ohne DTD-Verarbeitung (XXE-Schutz); keine Netzwerkzugriffe.

**Lesen, Schreiben, Upgrade**
- Rechnungsmodell `EbInvoice` (Namespace `EbInterface.Model`) und `EbInterfaceReader` zum Lesen von 4.3, 5.0, 6.0 und 6.1 in dasselbe Modell.
- `EbInterfaceWriter` schreibt ein `EbInvoice` als ebInterface 6.1 (UTF-8, nur schemagültig); leere Pflichtangaben meldet er als `WRT-01` mit Pfad. Zusammen mit dem Reader ergibt das das Versions-Upgrade von 4.3, 5.0 und 6.0 auf 6.1.
- Gemeinsame Basisklasse `EbInterfaceException` mit dem vollständigen Prüfergebnis für Lese- und Schreibfehler.

**MCP-Server**
- Zusatzpaket `EbInterface.Net.Mcp`: lokaler MCP-Server (.NET-Tool `ebinterface-mcp`) mit `validate_invoice`, `read_invoice`, `upgrade_invoice` und `explain_code`, z. B. für Claude Code und Claude Desktop.

[Unveröffentlicht]: https://github.com/haraldrohan/EbInterface.Net/compare/v0.1.0-alpha...HEAD
[0.1.0-alpha]: https://github.com/haraldrohan/EbInterface.Net/releases/tag/v0.1.0-alpha
