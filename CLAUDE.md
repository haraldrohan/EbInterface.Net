# CLAUDE.md – Projektkontext für EbInterface.Net

## Worum es geht

Unabhängige Open-Source-Bibliothek (MIT) für den österreichischen E-Rechnungsstandard **ebInterface** auf .NET.
Sie soll ebInterface lesen, schreiben und prüfen – inklusive der Sonderregeln von e-Rechnung.gv.at – und
ebInterface 7.0 unterstützen, sobald es erscheint (geplant Ende 2026, mit formalem Syntax-Binding an EN 16931).

**Zielgruppe:** österreichische Softwarehersteller mit .NET-Produkten (ERP, Buchhaltung, Warenwirtschaft,
Hausverwaltung), Anbieter von Zusatzmodulen, Integratoren. Nicht Endanwender.

**Kernnutzen:** Hersteller müssen Format, Bundesregeln und Versionswechsel nicht selbst pflegen.
Es gibt aktuell keine gepflegte ebInterface-Bibliothek für .NET (geprüft: NuGet, GitHub; Stand 09/2026).

Hintergrund und Begründungen: `docs/hintergrund.md`.

## Feste Entscheidungen

- **Namen:** Repository und NuGet-Paket `EbInterface.Net`; Namespace `EbInterface` (ohne `.Net`, weil
  `Net` im Namespace nach Netzwerk klingt). Unterbereiche z. B. `EbInterface.Validation`.
  Keine Klasse darf `EbInterface` heißen (Konflikt mit Namespace) – zentrale Klasse heißt `EbInvoice`.
- **Ein Paket**, keine Abhängigkeiten. Aufteilen erst, wenn ein Teil fremde Abhängigkeiten braucht:
  später `EbInterface.Net.EInvoicing` (Adapter zu International.EInvoicing) und `EbInterface.Net.Mcp`
  (MCP-Server als dotnet tool, Befehl `ebinterface-mcp`). Kein `.Core`-Paket.
- **Zielframeworks:** `netstandard2.0` und `net8.0`. netstandard2.0 ist Pflicht, weil viele Hersteller
  noch .NET Framework 4.x (oft VB.NET) einsetzen. Daher: keine APIs/Sprachfeatures, die unter
  netstandard2.0 nicht verfügbar sind (z. B. keine `record`-Typen ohne Polyfill, kein `init`).
- **Schemas:** Git-Submodule `external/ebinterface-standards` (austriapro/ebinterface-standards),
  auf festen Commit fixiert, beim Build als Ressourcen eingebettet (`EbInterface.Schemas.<ver>.Invoice.xsd`).
  **Niemals** zur Laufzeit aus dem Netz laden. Update des Submodules = bewusster Schritt mit CHANGELOG-Eintrag
  und Anpassung des Commits in `THIRD-PARTY-NOTICES.md`.
- **Keine Netzwerkzugriffe** in der Bibliothek. Rechnungsdaten verlassen nie das System des Nutzers.
- **Sicheres XML:** immer über `Internal.XmlSettings.CreateSecureReaderSettings` lesen (keine DTD,
  kein XmlResolver) – Schutz vor XXE.

## Prüfstufen (Architektur)

Die Prüfung läuft in Stufen; eine Stufe läuft nur, wenn die vorige sinnvoll bestanden ist:

1. **XML** – wohlgeformt? (Code `XML-xx`)
2. **Version** – Wurzelelement `Invoice` in bekanntem Namespace? (Code `VER-xx`)
3. **Schema** – XSD der erkannten Version (Code `XSD-xx`) ✅ vorhanden für 5.0/6.0/6.1
4. **Bundesregeln** – Sonderregeln von e-Rechnung.gv.at (Code `ERB-xx`) ⏳ nächster Schritt
5. später: EN-16931-Regeln für 7.0

Fehlercodes sind **stabil** (dürfen sich nach Veröffentlichung nicht ändern), Meldungen auf **Deutsch**,
verständlich, möglichst mit Hinweis zur Behebung. Zeile/Spalte angeben, wo möglich.

Geplantes Modell (Muster aus dem Word-Plugin von AUSTRIAPRO): versionsspezifische Klassen je
Schema-Version + ein gemeinsames Rechnungsmodell `EbInvoice` + Mapper in beide Richtungen
(`V6p1 -> Modell`, `Modell -> V6p1` usw.). Versions-Upgrade = einlesen in Modell, ausgeben in neuer Version.

## Wichtige Quellen

- **Schemas & Beispiele:** `external/ebinterface-standards/schemas/ebInterface<ver>/` (Invoice.xsd, Invoice.xslt, samples/).
  Hinweis: 4.3 importiert `ebInterfaceExtension.xsd`, `ext/ebInterfaceExtension_SV.xsd` und das W3C-xmldsig-Schema
  (per URL) – für 4.3 muss das xmldsig-Schema lokal eingebettet werden. 5.0/6.0/6.1 sind in sich geschlossen.
- **Bundesregeln (Primärquelle):** https://www.erechnung.gv.at/erb/tec_formats_ebinterface
- **Referenzimplementierung .NET (archiviert, MIT, (c) 2015 AUSTRIAPRO):**
  https://github.com/austriapro/ebinterface-word-plugin – besonders
  `eRechnungWordPlugIn/ebIModels/Models/erbInvoiceValidation.cs` (Bundesregeln mit Fehlercodes),
  `ebIModels/XmlData/ErrorCodes.xml`, `ebIModels/Mapping/*`, `ebIValidation/Validation/*`
  (IBAN, BIC, UID, GLN). Deckt nur 4.0–5.0 ab, .NET Framework 4.5.
  **Neu schreiben, nicht forken.** Wo Code/Logik übernommen wird: Herkunft im Code vermerken und
  MIT-Hinweis von AUSTRIAPRO beibehalten (siehe THIRD-PARTY-NOTICES.md).
- **Java-Referenz:** https://github.com/phax/ph-ebinterface (Philip Helger) – zum Abgleich von Verhalten.
- **7.0:** https://github.com/austriapro/ebi7 und Forum https://www.ebinterface.org/forum/ (Bereich "ebInterface 7.0").
- AUSTRIAPRO veröffentlicht **kein** Schematron; die Prüfung läuft über das XSD plus eigene Regeln.

## Bekannte Bundesregeln (aus e-Rechnung.gv.at, vor Umsetzung gegen Primärquelle prüfen)

- Akzeptierte Versionen: 4.3, 5.0, 6.0, 6.1. 4.0–4.2 seit April 2022 nicht mehr, 3.x seit Jänner 2016 nicht mehr.
- Eine Rechnung darf sich nur auf **eine** Bestellung beziehen.
- `DocumentType` nur: Invoice, InvoiceForAdvancePayment, InvoiceForPartialDelivery, FinalSettlement, CreditMemo.
- `InvoiceRecipient/OrderReference/OrderID` ist Pflicht (Auftragsreferenz).
- Ist Empfänger der Bund und die Auftragsreferenz eine Bestellnummer (10-stellig numerisch), muss jede Zeile
  eine passende Bestellpositionsnummer in `InvoiceRecipientsOrderReference/OrderPositionNumber` haben (numerisch).
- Firmensitz (§ 14 UGB) in `/Invoice/Biller/FurtherIdentification` mit `IdentificationType="FS"`.
- `BaseQuantity` wird nicht ausgewertet; ergibt die Division von Quantity oder UnitPrice durch BaseQuantity
  mehr als 4 Nachkommastellen, wird die Rechnung abgelehnt.
- `PrepaidAmount` wird nicht ausgewertet, `TotalGrossAmount` weder geprüft noch ausgewertet; nur `PayableAmount` wird validiert.

## Arbeitsweise

- Jede Regel bekommt Tests: mindestens ein positiver und ein negativer Fall. Offizielle Beispiele aus dem
  Submodule sind die Basis; eigene Testdateien unter `tests/EbInterface.Net.Tests/TestData/`.
- `TreatWarningsAsErrors` ist aktiv. XML-Doku-Kommentare für alle öffentlichen Typen (Deutsch).
- Öffentliche API klein halten; alles andere `internal`. Nach 1.0 gilt Semantic Versioning streng.
- Jede nutzerrelevante Änderung in `CHANGELOG.md` unter "Unveröffentlicht" eintragen.
- README-Hinweis "nicht offiziell von AUSTRIAPRO" nicht entfernen.
- Keine E-Mail-Adressen in Dateien; Commits nur mit der GitHub-Noreply-Adresse 14447036+haraldrohan@users.noreply.github.com; Sicherheitsmeldungen über GitHub Private Vulnerability Reporting.

## Befehle

```
git submodule update --init   # einmalig nach dem Klonen
dotnet build
dotnet test
dotnet pack src/EbInterface.Net -c Release -o artifacts
```

## Stand und nächste Schritte

Erledigt: Versionserkennung (4.3–6.1), XSD-Prüfung 5.0/6.0/6.1, XXE-Schutz, Tests gegen offizielle Beispiele, CI.

Als Nächstes, in dieser Reihenfolge:
1. Prüfstufe 4 (Bundesregeln, Codes `ERB-xx`) – Regeln aus e-Rechnung.gv.at und erbInvoiceValidation.cs,
   zunächst direkt auf dem XML (XPath/XDocument), bevor das Modell existiert.
2. Deutsche, verständliche Texte für die häufigsten XSD-Fehler (statt der englischen .NET-Meldungen).
3. XSD-Prüfung für 4.3 (xmldsig-Schema lokal einbetten).
4. Rechnungsmodell `EbInvoice` + Reader für 5.0/6.0/6.1.
5. Writer für 6.1, danach Versions-Upgrade.
