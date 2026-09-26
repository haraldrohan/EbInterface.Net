# Sicherheit

## Schwachstellen melden

Bitte melde Sicherheitslücken ausschließlich vertraulich über die private
Schwachstellenmeldung von GitHub: Öffne im Repository den Bereich **Security** und
wähle **Report a vulnerability**. Diese Funktion muss in den Repository-Einstellungen
aktiviert sein.

Du erhältst innerhalb von 7 Tagen eine Rückmeldung.

## Unterstützte Versionen

Sicherheitsupdates gibt es für die jeweils neueste veröffentlichte Version.

## Sicherheitsmerkmale

- XML wird ohne DTD-Verarbeitung und ohne externe Ressourcen gelesen (Schutz vor XXE).
- Die Prüfung erfolgt vollständig lokal; die Bibliothek baut keine Netzwerkverbindungen auf.
- Dokumente werden vollständig in den Speicher geladen. Wer Dateien aus unsicheren Quellen annimmt (z. B. Uploads),
  sollte ihre Größe vorher begrenzen.
