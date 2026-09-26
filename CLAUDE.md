# CLAUDE.md – Projektkontext für EbInterface.Net

## Worum es geht

Unabhängige Open-Source-Bibliothek (MIT) für den österreichischen E-Rechnungsstandard **ebInterface** auf .NET:
lesen, schreiben und prüfen – inklusive der Sonderregeln von e-Rechnung.gv.at – und ebInterface 7.0, sobald es
erscheint (geplant Ende 2026, mit formalem Syntax-Binding an EN 16931).

**Zielgruppe:** österreichische Softwarehersteller mit .NET-Produkten (ERP, Buchhaltung, Warenwirtschaft,
Hausverwaltung), Anbieter von Zusatzmodulen, Integratoren. Nicht Endanwender.

**Kernnutzen:** Hersteller müssen Format, Bundesregeln und Versionswechsel nicht selbst pflegen.

Private Hintergrundnotizen (nur lokal, nicht im Repository): @~/.claude/ebinterface-hintergrund.md

## Feste Entscheidungen

- **Namen:** Repository und NuGet-Paket `EbInterface.Net`; Namespace `EbInterface` (ohne `.Net`). Unterbereiche
  z. B. `EbInterface.Validation`. Keine Klasse darf `EbInterface` heißen; die zentrale Modellklasse heißt `EbInvoice`.
- **Ein Paket**, keine Abhängigkeiten. Aufteilen erst, wenn ein Teil fremde Abhängigkeiten braucht: später
  `EbInterface.Net.EInvoicing` (Adapter zu International.EInvoicing) und `EbInterface.Net.Mcp`
  (MCP-Server als dotnet tool, Befehl `ebinterface-mcp`). Kein `.Core`-Paket.
- **Zielframeworks:** `netstandard2.0` und `net8.0`. netstandard2.0 ist Pflicht (.NET Framework 4.x, oft VB.NET).
  Keine APIs/Sprachfeatures, die dort fehlen (kein `record`, kein `init`, keine Default-Interface-Methoden).
- **Schemas:** Git-Submodule `external/ebinterface-standards` (austriapro/ebinterface-standards) auf festem Commit,
  beim Build als Ressourcen eingebettet (`EbInterface.Schemas.<ver>.Invoice.xsd`). **Niemals** zur Laufzeit aus dem
  Netz laden. Submodule-Update = bewusster Schritt mit CHANGELOG-Eintrag und neuem Commit in `THIRD-PARTY-NOTICES.md`.
- **Keine Netzwerkzugriffe** in der Bibliothek. Rechnungsdaten verlassen nie das System des Nutzers.
- **Sicheres XML:** immer über `Internal.XmlSettings.CreateSecureReaderSettings` lesen (keine DTD, kein XmlResolver).
  Einzige Ausnahme: `CreateTrustedSchemaReaderSettings` (DTD wird übersprungen) für das mitgelieferte, unveränderte
  W3C-xmldsig-Schema – niemals für Eingabedokumente.
- **Prüfprofile:** `ValidationProfile.Standard` (Vorgabe) prüft nur den ebInterface-Standard, weil viele Rechnungen
  an Unternehmen gehen und dort die Bundesregeln nicht gelten. `ValidationProfile.ERechnungGvAt` schaltet die
  Regeln von e-Rechnung.gv.at zu.
- **Falsch-Positive vermeiden:** Wo eine Quelle Spielraum lässt, so prüfen, dass gültige Rechnungen nicht abgelehnt
  werden; die Auslegung in `docs/pruefregeln.md` dokumentieren. „Wird ignoriert / nicht ausgewertet“ ist nie ein
  Fehler, höchstens eine Warnung.

## Prüfstufen

Eine Stufe läuft nur, wenn die vorige ohne Fehler bestanden wurde:

1. **XML** – wohlgeformt? (`XML-xx`)
2. **Version** – `Invoice` in bekanntem Namespace? (`VER-xx`)
3. **Schema** – XSD der erkannten Version (`XSD-xx`); vorhanden für 4.3/5.0/6.0/6.1. Eigener Durchlauf mit
   `XmlSchemaValidator` (`Internal/SchemaCheck`), damit die Meldungen deutsch und laufzeitunabhängig sind –
   .NET-Meldungstexte nie parsen (unter .NET Framework mit Sprachpaket sind sie bereits deutsch)
4. **e-Rechnung.gv.at** – nur im Profil `ERechnungGvAt` (`ERB-xx`); Regeln für 4.3–6.1. In 4.3 sind
   Attribute qualifiziert (`eb:DocumentType`) – Attributnamen immer über `Context.Attr` bilden
5. später: EN-16931-Regeln für 7.0

Fehlercodes sind **stabil**: ein Code je Regel, nach Veröffentlichung keine Bedeutungsänderung, keine Wiederverwendung.
Meldungen auf **Deutsch**, verständlich, mit Hinweis zur Behebung, mit Zeile/Spalte wo möglich.

**Modell:** ein gemeinsames Rechnungsmodell `EbInvoice` (Namespace `EbInterface.Model`) für alle Versionen – keine
generierten Klassen je Schema-Version. `Internal/InvoiceMapper` übersetzt direkt aus dem XML (5.0–6.1 haben dieselben
Elementnamen; 4.3 weicht bei Steuer `VATRate`/`TaxExemption`/`VAT/VATItem`, Kontakt in der Adresse und qualifizierten
Attributen ab). Der Reader liest nur schemagültige Dokumente. Klassen mit `get; set;` und vorbelegten Listen
(netstandard2.0, VB.NET). Versions-Upgrade = einlesen ins Modell, ausgeben in neuer Version. `Internal/InvoiceWriter61` schreibt in
Schemareihenfolge, erfindet keine Werte (Ausnahme beim Upgrade: Steuerkategorie S/E, Sprachcode) und
`EbInterfaceWriter` gibt nur schemagültiges XML aus; leere Pflichtwerte und `default(DateTime)` meldet er als `WRT-01`
(das Schema lässt leere Texte zu). `Party` und `PaymentMethod` haben interne Konstruktoren – keine fremden Ableitungen.

## Regeln von e-Rechnung.gv.at

Vollständige, umgesetzte Regelliste mit Codes und Auslegungen: @docs/pruefregeln.md

Grundsätze dazu:

- Primärquelle ist https://www.erechnung.gv.at/erb/tec_formats_ebinterface – je Version ein eigener Reiter (4.3, 5.0,
  6.0, 6.1) mit kleinen Unterschieden. Vor jeder Änderung gegen die Seite abgleichen und das Datum in
  `docs/pruefregeln.md` aktualisieren.
- Die Art des Empfängers ergibt sich allein aus dem Format der Auftragsreferenz
  (https://www.erechnung.gv.at/go/orderref_fedgov, https://www.erechnung.gv.at/go/orderref_others).
  „Alphanumerisch“ schließt Umlaute ein (`\p{L}\p{N}`, z. B. EKG „63Ü“). Maßgeblich ist die aktuelle Seite; ältere
  Implementierungen nennen teils abweichende Längen.
- Ob Bestellnummer, EKG oder Lieferantennummer existieren, ist offline nicht prüfbar (das Portal prüft EKGs und
  andere Empfänger samt interner Referenz empfängerspezifisch). Unbekannte EKGs nie als Fehler werten. Lieferantennummer nur auf Vorhandensein prüfen (Beispiele zeigen 8 und 10 Stellen).
- Akzeptierte Versionen: 4.3, 5.0, 6.0, 6.1. 4.0–4.2 seit April 2022 nicht mehr, 3.x seit Jänner 2016 nicht mehr.

## Quellen

- **Schemas & Beispiele (AUSTRIAPRO):** `external/ebinterface-standards/schemas/ebInterface<ver>/`. 4.3 importiert
  `ebInterfaceExtension.xsd`, `ext/ebInterfaceExtension_SV.xsd` und das W3C-xmldsig-Schema per URL. Das xmldsig-Schema
  liegt byte-genau unter `src/EbInterface.Net/Schemas/w3c/` (W3C Software License, Text in THIRD-PARTY-NOTICES.md,
  `.gitattributes` verhindert Zeilenende-Umwandlung); alle importierten Schemas werden vorab in dasselbe
  `XmlSchemaSet` geladen. 5.0/6.0/6.1 sind in sich geschlossen.
- **Bund-Schemas** (https://www.erechnung.gv.at/files/xsd/ebinterface-6.1-bund.xsd, ebenso 6.0, 5.0, 4.3):
  **nur Referenz, nicht einbetten, nicht committen** („Copyright BRZ GmbH – All rights reserved“). Lokal mit dem
  Original vergleichen; die `[CHANGE]`-Stellen sind als eigene C#-Regeln umgesetzt.
- **Bund-Beispiele** (https://www.erechnung.gv.at/files/xml/example-ebi61.xml, ebenso ebi60, ebi50, ebi43,
  ebi43-finalsettlement): **nur Referenz, nicht committen.** Eigene Testdaten nach diesem Vorbild unter
  `tests/EbInterface.Net.Tests/TestData/<ver>/`. Die Beispiele bestehen das Profil ohne Fehler; nur ebi43-finalsettlement
  erzeugt die Warnung ERB-09, weil dort FS/FN/FBG fehlen (Stand 2026-09-25) – nach Regeländerungen lokal erneut prüfen.
- **Einkäufergruppen-Liste** (https://www.erechnung.gv.at/go/ekgrlist-excel): **nur Referenz, nicht einbetten**
  (Lizenz ungeklärt, ändert sich).
- **Endgültiger Test:** https://test.erechnung.gv.at/go/test_upload – ohne Anmeldung, ohne USP. Nur eigene,
  synthetische Testrechnungen hochladen, keine echten Rechnungsdaten. Formular: POST mit `invoice` (Datei),
  `action=perform` und `$ph_nonce` aus der Seite (Sitzungs-Cookie nötig). Höchstens etwa eine Anfrage pro 45 Sekunden,
  sonst sperrt das Portal vorübergehend. Testdaten brauchen gültige UIDs (Prüfziffer) und Datumswerte in der Zukunft.
  Ergebnisse des Abgleichs vom 25./26.09.2026 stehen in `docs/pruefregeln.md`.
- **Referenzimplementierung .NET (archiviert, MIT, (c) 2015 AUSTRIAPRO):**
  https://github.com/austriapro/ebinterface-word-plugin – u. a. `ebIModels/Models/erbInvoiceValidation.cs`,
  `ebIValidation/Validation/*` (IBAN, BIC, UID, GLN). Deckt 4.0–5.0 ab. **Neu schreiben, nicht forken.** Wo Code oder
  Logik übernommen wird: Herkunft im Code vermerken, MIT-Hinweis beibehalten (siehe THIRD-PARTY-NOTICES.md).
- **Java-Referenz:** https://github.com/phax/ph-ebinterface – zum Abgleich von Verhalten.
- **7.0:** https://github.com/austriapro/ebi7 und https://www.ebinterface.org/forum/ (Bereich „ebInterface 7.0“).
- AUSTRIAPRO veröffentlicht **kein** Schematron; die Prüfung läuft über das XSD plus eigene Regeln.

## Arbeitsweise

- Jede Regel bekommt Tests: mindestens ein positiver und ein negativer Fall.
- `TreatWarningsAsErrors` ist aktiv. XML-Doku-Kommentare für alle öffentlichen Typen (Deutsch).
- Öffentliche API klein halten; alles andere `internal`. Nach 1.0 gilt Semantic Versioning streng.
- Jede nutzerrelevante Änderung in `CHANGELOG.md` unter „Unveröffentlicht“ eintragen.
- README-Hinweis „nicht offiziell von AUSTRIAPRO“ nicht entfernen.
- Keine E-Mail-Adressen in Dateien (Testdaten nur mit reservierten Beispieldomains wie `example.org`); Commits nur mit
  der GitHub-Noreply-Adresse 14447036+haraldrohan@users.noreply.github.com; Sicherheitsmeldungen über GitHub Private
  Vulnerability Reporting.
- Keine Wertungen über andere Firmen, Projekte oder Websites in öffentlichen Dateien – auch nicht in
  Commit-Nachrichten. Solche Notizen gehören in ~/.claude/ebinterface-hintergrund.md.

## Befehle

```
git submodule update --init   # einmalig nach dem Klonen
dotnet build
dotnet test
dotnet pack src/EbInterface.Net -c Release -o artifacts
```

## Stand und nächste Schritte

Erledigt: Versionserkennung (4.3–6.1), XSD-Prüfung 4.3–6.1 mit deutschen Meldungen, Rechnungsmodell und Reader 4.3–6.1, Writer 6.1 und Versions-Upgrade auf 6.1, XXE-Schutz, Prüfprofile, Regeln von
e-Rechnung.gv.at (ERB-01 bis ERB-39) samt Abgleich mit dem Test-Upload und Rechenprüfungen (ERB-26 bis ERB-29, ERB-39), eigene Testdaten, CI.

Als Nächstes, in dieser Reihenfolge:
1. ebInterface 7.0, sobald veröffentlicht (Schema, Reader, Writer, EN-16931-Regeln).
2. Nach Bedarf der Hersteller: fehlende Modellteile (Extension, AdditionalInformation, Classification, Fremdwährung).
