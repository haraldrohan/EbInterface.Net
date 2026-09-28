# EbInterface.Net.Mcp

Lokaler MCP-Server (Model Context Protocol) für [EbInterface.Net](https://github.com/haraldrohan/EbInterface.Net):
ebInterface-Rechnungen (4.3 bis 6.1) aus Claude oder anderen MCP-fähigen Programmen heraus prüfen, lesen und auf 6.1
aktualisieren.

> **Hinweis:** Unabhängiges Open-Source-Projekt, nicht von AUSTRIAPRO oder der Wirtschaftskammer Österreich.

## Installation

```
dotnet tool install -g EbInterface.Net.Mcp --prerelease
```

Voraussetzung ist .NET 8 oder neuer. Danach steht der Befehl `ebinterface-mcp` zur Verfügung.

### In Claude Code

```
claude mcp add --scope user ebinterface -- ebinterface-mcp
```

In der VS-Code-Erweiterung von Claude Code steht der Befehl `claude` meist nicht im Terminal zur Verfügung. Dann den
Server im Chat über `/mcp` hinzufügen (Befehl `ebinterface-mcp`, Bereich „user“). Neue Server wirken erst in einer
**neu begonnenen** Unterhaltung; mit `/mcp` oder `/status` lässt sich prüfen, ob `ebinterface` verbunden ist.

### In Claude Desktop

In der Konfigurationsdatei von Claude Desktop (`claude_desktop_config.json`) ergänzen:

```json
{
  "mcpServers": {
    "ebinterface": { "command": "ebinterface-mcp" }
  }
}
```

## Werkzeuge

| Werkzeug | Zweck |
|---|---|
| `validate_invoice` | Prüft eine Datei – Profil `standard` (nur ebInterface) oder `erechnung` (zusätzlich die Regeln von e-Rechnung.gv.at). Liefert alle Meldungen mit Code, Zeile und deutschem Text. |
| `read_invoice` | Zusammenfassung einer Rechnung: Kopfdaten, Parteien, Auftragsreferenz, Zeilen, Steuer, Summen, Zahlung. |
| `upgrade_invoice` | Schreibt eine Rechnung aus 4.3, 5.0 oder 6.0 als ebInterface 6.1 in eine neue Datei (überschreibt nichts ungefragt). |
| `explain_code` | Erklärt einen Fehlercode wie `ERB-27` oder listet alle Codes einer Gruppe. |

Beispiele für Anfragen an Claude:

- „Prüf die Rechnungen im Ordner C:\Export gegen die Regeln von e-Rechnung.gv.at und erklär mir die Fehler.“
- „Aktualisier rechnung-4p3.xml auf ebInterface 6.1 und prüf das Ergebnis.“
- „Was bedeutet ERB-27, und wie behebe ich es?“

## Datenschutz

Der Server läuft **nur lokal** und baut keine Netzwerkverbindungen auf. Rechnungen werden über Dateipfade angegeben.
Was ein Werkzeug zurückgibt – Meldungen oder die Zusammenfassung von `read_invoice` mit Namen und Beträgen – sieht
aber das Sprachmodell, mit dem Sie arbeiten, und es wird wie jede andere Eingabe an dessen Anbieter übertragen.
`read_invoice` kürzt IBANs, `validate_invoice` gibt keine vollständigen Rechnungsdaten zurück. Ob Sie echte
Kundenrechnungen auf diesem Weg verarbeiten dürfen, entscheiden Sie bzw. Ihr Unternehmen.

## Lizenz

MIT – siehe [LICENSE](https://github.com/haraldrohan/EbInterface.Net/blob/main/LICENSE) und
[THIRD-PARTY-NOTICES.md](https://github.com/haraldrohan/EbInterface.Net/blob/main/THIRD-PARTY-NOTICES.md).
