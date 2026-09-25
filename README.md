# EbInterface.Net

Unabhängige .NET-Implementierung des österreichischen E-Rechnungsstandards **ebInterface** – lesen, schreiben und prüfen, inklusive der Sonderregeln von e-Rechnung.gv.at.

> **Hinweis:** Dies ist ein unabhängiges Open-Source-Projekt. Es wird nicht von AUSTRIAPRO oder der Wirtschaftskammer Österreich herausgegeben oder unterstützt. Der offizielle Standard ist unter [ebinterface.at](https://www.ebinterface.at) zu finden.

## Status

Frühe Entwicklungsphase (`0.1.0-alpha`). Die API kann sich noch ändern.

| Funktion | 4.3 | 5.0 | 6.0 | 6.1 | 7.0 |
|---|---|---|---|---|---|
| Versionserkennung | ✅ | ✅ | ✅ | ✅ | geplant |
| Schema-Prüfung (XSD) | ✅ | ✅ | ✅ | ✅ | geplant |
| Regeln von e-Rechnung.gv.at | ✅ | ✅ | ✅ | ✅ | geplant |
| Lesen in ein Modell | ✅ | ✅ | ✅ | ✅ | geplant |
| Schreiben | – | – | geplant | geplant | geplant |
| Versions-Upgrade | geplant | geplant | geplant | – | – |

## Installation

```
dotnet add package EbInterface.Net --prerelease
```

Unterstützt .NET Standard 2.0 (damit auch .NET Framework ab 4.6.1) und .NET 8. Keine Abhängigkeiten zu anderen Paketen.

## Verwendung

```csharp
using EbInterface;
using EbInterface.Validation;

// Nur der ebInterface-Standard (XML, Version, Schema) – z. B. für Rechnungen an Unternehmen:
var result = EbInterfaceValidator.ValidateFile("rechnung.xml");

// Zusätzlich die Regeln von e-Rechnung.gv.at – für Rechnungen an Bund, Länder und Gemeinden:
result = EbInterfaceValidator.ValidateFile("rechnung.xml",
    new ValidationOptions { Profile = ValidationProfile.ERechnungGvAt });

Console.WriteLine($"Version: {result.Version.ToDisplayString()}");
foreach (var message in result.Messages)
    Console.WriteLine(message);   // [ERB-05] Zeile 42, Spalte 8: Bei einer Bestellnummer des Bundes ...
```

`IsValid` ist `true`, solange es keine Fehler gibt. Warnungen stehen zusätzlich in `Messages`, zum Beispiel für
Felder, die e-Rechnung.gv.at nicht auswertet. Alle Codes und Regeln stehen in [docs/pruefregeln.md](docs/pruefregeln.md).

### Rechnung lesen

```csharp
using EbInterface;
using EbInterface.Model;

EbInvoice invoice = EbInterfaceReader.ReadFile("rechnung.xml");   // 4.3, 5.0, 6.0 oder 6.1

Console.WriteLine($"{invoice.InvoiceNumber} vom {invoice.InvoiceDate:d}: {invoice.PayableAmount} {invoice.Currency}");
foreach (LineItem line in invoice.AllLineItems)
    Console.WriteLine($"  {line.Quantity} {line.Unit} {line.Descriptions[0]} à {line.UnitPrice} ({line.TaxPercent} % USt)");
```

Gelesen werden nur schemagültige Dokumente; andernfalls kommt eine `EbInterfaceReadException` mit allen Meldungen
in `Validation`. Alle Versionen landen im selben Modell. Noch nicht abgebildet sind Erweiterungen (`Extension`),
Signaturen, `AdditionalInformation`, `Classification`, Fremdwährungsangaben und `PresentationDetails`.

Die Bibliothek prüft vollständig lokal und baut keine Netzwerkverbindungen auf. Den endgültigen Nachweis liefert der
[Test-Upload von e-Rechnung.gv.at](https://test.erechnung.gv.at/go/test_upload).

## Entwicklung

Die offiziellen Schemas und Beispielrechnungen stammen aus dem Repository [austriapro/ebinterface-standards](https://github.com/austriapro/ebinterface-standards) und sind als Git-Submodule eingebunden:

```
git clone --recurse-submodules https://github.com/haraldrohan/EbInterface.Net.git
dotnet test
```

Wer schon ohne Submodule geklont hat: `git submodule update --init`.

## Lizenz

Der Code steht unter der [MIT-Lizenz](LICENSE). Die eingebetteten Schemas von AUSTRIAPRO sind davon ausgenommen – siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
