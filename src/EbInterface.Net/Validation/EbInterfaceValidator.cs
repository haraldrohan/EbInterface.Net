using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using EbInterface.Internal;

namespace EbInterface.Validation
{
    /// <summary>
    /// Prüft ebInterface-Dokumente in Stufen: wohlgeformtes XML, Version, XML-Schema und – je nach
    /// <see cref="ValidationProfile"/> – die Sonderregeln von e-Rechnung.gv.at.
    /// Eine Stufe läuft nur, wenn die vorige ohne Fehler bestanden wurde.
    /// </summary>
    public static class EbInterfaceValidator
    {
        private const string EbInterfaceNamespacePrefix = "http://www.ebinterface.at/schema/";

        /// <summary>Prüft eine Datei.</summary>
        /// <param name="path">Pfad zur XML-Datei.</param>
        /// <param name="options">Einstellungen; <c>null</c> = Vorgaben.</param>
        public static ValidationResult ValidateFile(string path, ValidationOptions? options = null)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            using FileStream stream = File.OpenRead(path);
            return Validate(stream, options);
        }

        /// <summary>Prüft ein Dokument aus einem Stream. Der Stream wird nicht geschlossen.</summary>
        /// <param name="xml">Das Dokument.</param>
        /// <param name="options">Einstellungen; <c>null</c> = Vorgaben.</param>
        public static ValidationResult Validate(Stream xml, ValidationOptions? options = null)
        {
            if (xml is null) throw new ArgumentNullException(nameof(xml));
            options ??= new ValidationOptions();

            var messages = new List<ValidationMessage>();

            // Stufe 1: wohlgeformtes XML
            XDocument document;
            try
            {
                using var reader = XmlReader.Create(xml, XmlSettings.CreateSecureReaderSettings(closeInput: false));
                document = XDocument.Load(reader, LoadOptions.SetLineInfo);
            }
            catch (XmlException ex)
            {
                messages.Add(new ValidationMessage(ValidationSeverity.Error, "XML-01",
                    $"Das Dokument ist kein wohlgeformtes XML: {ex.Message}", ex.LineNumber, ex.LinePosition));
                return new ValidationResult(EbInterfaceVersion.Unknown, messages);
            }

            // Stufe 2: Version
            XElement root = document.Root!;
            EbInterfaceVersion version = root.Name.LocalName == "Invoice"
                ? EbInterfaceVersionExtensions.FromNamespace(root.Name.NamespaceName)
                : EbInterfaceVersion.Unknown;

            if (version == EbInterfaceVersion.Unknown)
            {
                bool olderEbInterface = root.Name.LocalName == "Invoice" &&
                    root.Name.NamespaceName.StartsWith(EbInterfaceNamespacePrefix, StringComparison.Ordinal);
                messages.Add(olderEbInterface
                    ? new ValidationMessage(ValidationSeverity.Error, "VER-03",
                        $"Die ebInterface-Version mit dem Namespace '{root.Name.NamespaceName}' wird nicht unterstützt. " +
                        "Unterstützt werden 4.3, 5.0, 6.0 und 6.1; e-Rechnung.gv.at nimmt ältere Versionen nicht mehr an.",
                        LineInfo.Line(root), LineInfo.Position(root))
                    : new ValidationMessage(ValidationSeverity.Error, "VER-01",
                        "Kein ebInterface-Dokument erkannt: Das Wurzelelement muss 'Invoice' in einem ebInterface-Namespace sein.",
                        LineInfo.Line(root), LineInfo.Position(root)));
                return new ValidationResult(version, messages);
            }

            if (!SchemaCache.IsSupported(version))
            {
                messages.Add(new ValidationMessage(ValidationSeverity.Error, "VER-02",
                    $"{version.ToDisplayString()} wird von dieser Version der Bibliothek noch nicht geprüft."));
                return new ValidationResult(version, messages);
            }

            // Stufe 3: XML-Schema
            SchemaCheck.Run(document, SchemaCache.Get(version), version, messages);

            // Stufe 4: Regeln von e-Rechnung.gv.at
            if (options.Profile == ValidationProfile.ERechnungGvAt &&
                !messages.Any(m => m.Severity == ValidationSeverity.Error))
            {
                DateTime referenceDate = (options.ReferenceDate ?? DateTime.Today).Date;
                ERechnungRules.Check(root, version, referenceDate, messages);
            }

            return new ValidationResult(version, messages);
        }
    }
}
