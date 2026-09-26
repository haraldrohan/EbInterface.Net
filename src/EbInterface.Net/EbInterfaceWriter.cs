using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EbInterface.Internal;
using EbInterface.Model;
using EbInterface.Validation;

namespace EbInterface
{
    /// <summary>Schreibt eine <see cref="EbInvoice"/> als ebInterface 6.1.</summary>
    public static class EbInterfaceWriter
    {
        /// <summary>
        /// Schreibt die Rechnung als ebInterface 6.1 (UTF-8) in den Stream. Das Ergebnis wird vorher gegen das Schema
        /// geprüft; es wird nur schemagültiges XML geschrieben. Der Stream wird nicht geschlossen.
        /// Stammt die Rechnung aus einer älteren Version, entsteht damit ein Versions-Upgrade.
        /// </summary>
        /// <exception cref="EbInterfaceWriteException">Die Rechnung ergibt kein schemagültiges ebInterface 6.1, z. B. weil Pflichtangaben fehlen.</exception>
        /// <exception cref="NotSupportedException">Die Rechnung enthält etwas, das es in 6.1 nicht gibt (Zahlungsart DirectDebit aus 4.3).</exception>
        public static void Write(EbInvoice invoice, Stream output)
        {
            if (invoice is null) throw new ArgumentNullException(nameof(invoice));
            if (output is null) throw new ArgumentNullException(nameof(output));

            byte[] xml = Serialize(InvoiceWriter61.Write(invoice));

            using (var check = new MemoryStream(xml, writable: false))
            {
                ValidationResult validation = EbInterfaceValidator.Validate(check);
                if (!validation.IsValid)
                    throw new EbInterfaceWriteException(validation);
            }

            output.Write(xml, 0, xml.Length);
        }

        /// <summary>Schreibt die Rechnung als ebInterface 6.1 in eine Datei (wird überschrieben).</summary>
        /// <exception cref="EbInterfaceWriteException">Die Rechnung ergibt kein schemagültiges ebInterface 6.1.</exception>
        public static void WriteFile(EbInvoice invoice, string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            using var buffer = new MemoryStream();
            Write(invoice, buffer); // erst prüfen, dann die Datei anlegen – keine halben Dateien
            File.WriteAllBytes(path, buffer.ToArray());
        }

        private static byte[] Serialize(XDocument document)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = true,
                IndentChars = "  ",
            };

            using var stream = new MemoryStream();
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
                document.Save(writer);
            return stream.ToArray();
        }
    }

    /// <summary>Die Rechnung konnte nicht geschrieben werden, weil sie kein schemagültiges ebInterface ergibt.</summary>
    public sealed class EbInterfaceWriteException : Exception
    {
        internal EbInterfaceWriteException(ValidationResult validation)
            : base(BuildMessage(validation))
        {
            Validation = validation;
        }

        /// <summary>Ergebnis der Schema-Prüfung des erzeugten XML mit allen Meldungen.</summary>
        public ValidationResult Validation { get; }

        private static string BuildMessage(ValidationResult validation)
        {
            string first = validation.Errors.GetEnumerator() is var errors && errors.MoveNext() ? errors.Current.ToString() : "unbekannter Fehler";
            return $"Die Rechnung ergibt kein gültiges ebInterface 6.1 und wurde nicht geschrieben: {first}";
        }
    }
}
