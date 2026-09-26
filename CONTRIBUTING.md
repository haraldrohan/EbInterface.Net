# Mitwirken

Danke für dein Interesse an EbInterface.Net! Beiträge sind willkommen – Fehlermeldungen, Hinweise auf geänderte
Regeln von e-Rechnung.gv.at und Code.

## Fehler und Abweichungen melden

- **Das Portal urteilt anders als die Bibliothek?** Bitte die Vorlage „Abweichung zu e-Rechnung.gv.at“ verwenden.
  Diese Rückmeldungen sind besonders wertvoll: Viele Prüfungen des Portals stehen auf keiner öffentlichen Seite.
- **Sonstige Fehler:** Vorlage „Fehler melden“.
- **Sicherheitslücken bitte nie öffentlich melden**, sondern wie in [SECURITY.md](SECURITY.md) beschrieben.

**Keine echten Rechnungsdaten** in Issues oder Pull Requests: Beispiel-XML bitte auf das Nötigste kürzen und Namen,
UID-Nummern, IBANs und Beträge durch erfundene Werte ersetzen.

## Code beitragen

1. Repository mit Submodule klonen: `git clone --recurse-submodules https://github.com/haraldrohan/EbInterface.Net.git`
2. Bauen und testen: `dotnet build` und `dotnet test` – beides muss ohne Warnungen und Fehler durchlaufen
   (`TreatWarningsAsErrors` ist aktiv).
3. Kleine, in sich geschlossene Pull Requests gegen `main`.

### Regeln für Code und Tests

- **Zielframeworks** sind `netstandard2.0` und `net8.0`. Keine APIs oder Sprachmittel, die unter netstandard2.0 fehlen
  (z. B. keine `record`-Typen, kein `init`).
- **Keine Abhängigkeiten** zu anderen Paketen und **keine Netzwerkzugriffe** in der Bibliothek.
- XML immer über die sicheren Einstellungen in `Internal/XmlSettings` lesen.
- Öffentliche Typen und Mitglieder bekommen XML-Doku-Kommentare auf Deutsch; alles andere bleibt `internal`.
- **Jede Regel braucht Tests** – mindestens einen positiven und einen negativen Fall.
- **Fehlercodes sind stabil.** Neue Regeln bekommen einen neuen Code; bestehende Codes ändern ihre Bedeutung nie.
  Neue Codes in [docs/pruefregeln.md](docs/pruefregeln.md) eintragen.
- **Meldungen auf Deutsch**, verständlich, mit Hinweis zur Behebung.
- Wo eine Quelle Spielraum lässt, so prüfen, dass gültige Rechnungen **nicht** abgelehnt werden, und die Auslegung in
  `docs/pruefregeln.md` festhalten.
- Nutzerrelevante Änderungen in [CHANGELOG.md](CHANGELOG.md) unter „Unveröffentlicht“ eintragen.

### Testdaten und fremdes Material

- Eigene Testdaten liegen unter `tests/EbInterface.Net.Tests/TestData/<Version>/` und sind **synthetisch**
  (erfundene Namen, gültige, aber erfundene UID-Nummern, E-Mail-Adressen nur mit `example.org`).
- Die angepassten Schemas und Beispielrechnungen von e-Rechnung.gv.at (BRZ) **nicht** ins Repository übernehmen –
  sie sind nicht frei lizenziert. Eigene Testdaten dürfen sich an ihrem Aufbau orientieren.
- Übernommenen Code aus anderen Projekten nur mit verträglicher Lizenz, mit Herkunftsvermerk im Code und Eintrag in
  [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

### Abgleich mit dem Test-Upload

Regeln, die auf keiner öffentlichen Seite stehen, lassen sich mit dem
[Test-Upload](https://test.erechnung.gv.at/go/test_upload) von e-Rechnung.gv.at prüfen (ohne Anmeldung). Bitte nur
synthetische Rechnungen hochladen und höchstens etwa eine Datei pro 45 Sekunden – sonst sperrt das Portal
vorübergehend. Ergebnisse gehören in den Abschnitt „Abgleich mit dem Test-Upload“ in `docs/pruefregeln.md`.

## Lizenz

Mit einem Beitrag erklärst du dich einverstanden, dass er unter der [MIT-Lizenz](LICENSE) dieses Projekts
veröffentlicht wird.
