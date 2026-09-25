# Sicherheit

## Schwachstellen melden

Bitte melde Sicherheitslücken **nicht** über öffentliche Issues, sondern vertraulich über
[GitHub Security Advisories](../../security/advisories/new) dieses Repositorys oder per E-Mail an [E-Mail-Adresse].

Du erhältst innerhalb von 7 Tagen eine Rückmeldung.

## Unterstützte Versionen

Sicherheitsupdates gibt es für die jeweils neueste veröffentlichte Version.

## Sicherheitsmerkmale

- XML wird ohne DTD-Verarbeitung und ohne externe Ressourcen gelesen (Schutz vor XXE).
- Die Prüfung erfolgt vollständig lokal; die Bibliothek baut keine Netzwerkverbindungen auf.
