using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using EbInterface.Internal;

namespace EbInterface.Validation
{
    /// <summary>
    /// Prüft ebInterface-Dokumente auf Wohlgeformtheit, Version, XML-Schema und Bundesregeln.
    /// </summary>
    public static class EbInterfaceValidator
    {
        /// <summary>Prüft eine Datei.</summary>
        public static ValidationResult ValidateFile(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return Validate(stream);
        }

        /// <summary>Prüft ein Dokument aus einem Stream. Der Stream muss suchbar sein.</summary>
        public static ValidationResult Validate(Stream xml)
        {
            if (xml is null) throw new ArgumentNullException(nameof(xml));
            if (!xml.CanSeek) throw new ArgumentException("Der Stream muss suchbar sein (CanSeek).", nameof(xml));

            var messages = new List<ValidationMessage>();
            long start = xml.Position;

            EbInterfaceVersion version = EbInterfaceDetector.DetectVersion(xml);
            xml.Position = start;

            if (version == EbInterfaceVersion.Unknown)
            {
                messages.Add(new ValidationMessage(ValidationSeverity.Error, "VER-01",
                    "Kein ebInterface-Dokument erkannt: Das Wurzelelement muss 'Invoice' in einem ebInterface-Namespace sein."));
                return new ValidationResult(version, messages);
            }

            if (!SchemaCache.IsSupported(version))
            {
                messages.Add(new ValidationMessage(ValidationSeverity.Error, "VER-02",
                    $"{version.ToDisplayString()} wird von dieser Version der Bibliothek noch nicht geprüft."));
                return new ValidationResult(version, messages);
            }

            XmlReaderSettings settings = XmlSettings.CreateSecureReaderSettings(closeInput: false);
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = SchemaCache.Get(version);
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += (_, e) => messages.Add(new ValidationMessage(
                e.Severity == XmlSeverityType.Error ? ValidationSeverity.Error : ValidationSeverity.Warning,
                "XSD-01",
                $"Verstoß gegen das Schema von {version.ToDisplayString()}: {e.Message}",
                e.Exception?.LineNumber ?? 0,
                e.Exception?.LinePosition ?? 0));

            try
            {
                using var reader = XmlReader.Create(xml, settings);
                while (reader.Read()) { }
            }
            catch (XmlException ex)
            {
                messages.Add(new ValidationMessage(ValidationSeverity.Error, "XML-01",
                    $"Das Dokument ist kein wohlgeformtes XML: {ex.Message}", ex.LineNumber, ex.LinePosition));
            }

            bool hasErrors = false;
            foreach (ValidationMessage message in messages)
            {
                if (message.Severity == ValidationSeverity.Error)
                {
                    hasErrors = true;
                    break;
                }
            }

            if (!hasErrors)
            {
                xml.Position = start;
                ERechnungValidator.Validate(xml, messages);
            }

            return new ValidationResult(version, messages);
        }
    }
}
