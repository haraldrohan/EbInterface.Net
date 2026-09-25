# Fremdmaterial

Dieses Projekt enthält bzw. verwendet Material Dritter, das **nicht** unter der MIT-Lizenz dieses Projekts steht.

## ebInterface-Schemas und Beispielrechnungen

- **Urheber:** AUSTRIAPRO (Wirtschaftskammer Österreich)
- **Quelle:** https://github.com/austriapro/ebinterface-standards
- **Eingebundener Stand:** Commit `787f0825df3dd8374b486cd4eaabcf606c45e670`
- **Verwendung:** Die XML-Schemas der Versionen 4.3 (`Invoice.xsd`, `ebInterfaceExtension.xsd`,
  `ext/ebInterfaceExtension_SV.xsd`), 5.0, 6.0 und 6.1 (`Invoice.xsd`) sind unverändert als Ressourcen in die
  Bibliothek eingebettet und dienen der Prüfung. Die Beispielrechnungen werden ausschließlich als Testdaten verwendet
  und nicht mit dem Paket ausgeliefert.
- **Lizenz:** Das Quell-Repository enthält keine eigene Lizenzdatei. ebInterface wird von AUSTRIAPRO zur kostenlosen
  Implementierung bereitgestellt. Alle Rechte an den Schemas verbleiben bei AUSTRIAPRO.

## XML-Signatur-Schema des W3C

- **Datei:** `src/EbInterface.Net/Schemas/w3c/xmldsig-core-schema.xsd`, von ebInterface 4.3 importiert
- **Quelle:** http://www.w3.org/TR/2002/REC-xmldsig-core-20020212/xmldsig-core-schema.xsd
- **Änderungen:** keine; die Datei ist byte-genau übernommen
  (SHA-256 `35cf8197da812c85e40d57891b35c94187569ed474a2dac813ce5090dafcd35c`)
- **Lizenz:** W3C Software Notice and License (1998-07-20),
  https://www.w3.org/Consortium/Legal/copyright-software-19980720 – vollständiger Text:

> W3C® SOFTWARE NOTICE AND LICENSE
>
> Copyright © 1994-2002 World Wide Web Consortium, (Massachusetts Institute of Technology, Institut National de
> Recherche en Informatique et en Automatique, Keio University). All Rights Reserved. http://www.w3.org/Consortium/Legal/
>
> This W3C work (including software, documents, or other related items) is being provided by the copyright holders
> under the following license. By obtaining, using and/or copying this work, you (the licensee) agree that you have
> read, understood, and will comply with the following terms and conditions:
>
> Permission to use, copy, modify, and distribute this software and its documentation, with or without modification,
> for any purpose and without fee or royalty is hereby granted, provided that you include the following on ALL copies
> of the software and documentation or portions thereof, including modifications, that you make:
>
> 1. The full text of this NOTICE in a location viewable to users of the redistributed or derivative work.
> 2. Any pre-existing intellectual property disclaimers, notices, or terms and conditions. If none exist, a short
>    notice of the following form (hypertext is preferred, text is permitted) should be used within the body of any
>    redistributed or derivative code: "Copyright © [$date-of-software] World Wide Web Consortium, (Massachusetts
>    Institute of Technology, Institut National de Recherche en Informatique et en Automatique, Keio University).
>    All Rights Reserved. http://www.w3.org/Consortium/Legal/"
> 3. Notice of any changes or modifications to the W3C files, including the date changes were made. (We recommend
>    you provide URIs to the location from which the code is derived.)
>
> THIS SOFTWARE AND DOCUMENTATION IS PROVIDED "AS IS," AND COPYRIGHT HOLDERS MAKE NO REPRESENTATIONS OR WARRANTIES,
> EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO, WARRANTIES OF MERCHANTABILITY OR FITNESS FOR ANY PARTICULAR PURPOSE
> OR THAT THE USE OF THE SOFTWARE OR DOCUMENTATION WILL NOT INFRINGE ANY THIRD PARTY PATENTS, COPYRIGHTS, TRADEMARKS
> OR OTHER RIGHTS.
>
> COPYRIGHT HOLDERS WILL NOT BE LIABLE FOR ANY DIRECT, INDIRECT, SPECIAL OR CONSEQUENTIAL DAMAGES ARISING OUT OF ANY
> USE OF THE SOFTWARE OR DOCUMENTATION.
>
> The name and trademarks of copyright holders may NOT be used in advertising or publicity pertaining to the software
> without specific, written prior permission. Title to copyright in this software and any associated documentation
> will at all times remain with copyright holders.

Die Datei selbst enthält den Hinweis: "Copyright 2001 The Internet Society and W3C (Massachusetts Institute of
Technology, Institut National de Recherche en Informatique et en Automatique, Keio University). All Rights Reserved."

## Regeln von e-Rechnung.gv.at

Die Prüfregeln sind eigenständig nach den öffentlichen Angaben von e-Rechnung.gv.at umgesetzt
(siehe `docs/pruefregeln.md`). Die angepassten Schemas und Beispielrechnungen des Bundes werden weder eingebettet
noch mitgeliefert.

## Referenzimplementierung (derzeit kein übernommener Code)

Das archivierte [ebInterface Word Plugin](https://github.com/austriapro/ebinterface-word-plugin),
Copyright (c) 2015 AUSTRIAPRO, steht unter der MIT-Lizenz. Derzeit ist daraus kein Code übernommen. Falls das
künftig geschieht, wird es an der jeweiligen Stelle vermerkt und der folgende Hinweis beibehalten:

> The MIT License (MIT) – Copyright (c) 2015 AUSTRIAPRO
