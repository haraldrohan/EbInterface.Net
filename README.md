# EbInterface.Net

Unabhängige .NET-Implementierung des österreichischen E-Rechnungsstandards **ebInterface** – lesen, schreiben und prüfen, inklusive der Sonderregeln von e-Rechnung.gv.at.

> **Hinweis:** Dies ist ein unabhängiges Open-Source-Projekt. Es wird nicht von AUSTRIAPRO oder der Wirtschaftskammer Österreich herausgegeben oder unterstützt. Der offizielle Standard ist unter [ebinterface.at](https://www.ebinterface.at) zu finden.

## Status

Frühe Entwicklungsphase (`0.1.0-alpha`). Die API kann sich noch ändern.

| Funktion | 4.3 | 5.0 | 6.0 | 6.1 | 7.0 |
|---|---|---|---|---|---|
| Versionserkennung | ✅ | ✅ | ✅ | ✅ | geplant |
| Schema-Prüfung (XSD) | geplant | ✅ | ✅ | ✅ | geplant |
| Regeln von e-Rechnung.gv.at | – | geplant | geplant | geplant | geplant |
| Lesen in ein Modell | – | geplant | geplant | geplant | geplant |
| Schreiben | – | – | geplant | geplant | geplant |
| Versions-Upgrade | geplant | geplant | geplant | – | – |

## Installation

```
dotnet add package EbInterface.Net --prerelease
```

Unterstützt .NET Standard 2.0 (damit auch .NET Framework ab 4.6.1) und .NET 8. Keine Abhängigkeiten zu anderen Paketen.

## Verwendung

```csharp
using EbInterface.Validation;

var result = EbInterfaceValidator.ValidateFile("rechnung.xml");

Console.WriteLine($"Version: {result.Version.ToDisplayString()}");
if (!result.IsValid)
{
    foreach (var error in result.Errors)
        Console.WriteLine(error);   // [XSD-01] Zeile 12, Spalte 4: Verstoß gegen das Schema ...
}
```

## Entwicklung

Die offiziellen Schemas und Beispielrechnungen stammen aus dem Repository [austriapro/ebinterface-standards](https://github.com/austriapro/ebinterface-standards) und sind als Git-Submodule eingebunden:

```
git clone --recurse-submodules https://github.com/haraldrohan/EbInterface.Net.git
dotnet test
```

Wer schon ohne Submodule geklont hat: `git submodule update --init`.

## Lizenz

Der Code steht unter der [MIT-Lizenz](LICENSE). Die eingebetteten Schemas von AUSTRIAPRO sind davon ausgenommen – siehe [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
