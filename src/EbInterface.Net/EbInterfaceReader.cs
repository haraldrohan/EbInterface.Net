using System;
using System.IO;
using System.Xml.Linq;
using EbInterface.Internal;
using EbInterface.Model;
using EbInterface.Validation;

namespace EbInterface
{
    /// <summary>Liest ebInterface-Rechnungen (4.3 bis 6.1) in das gemeinsame Modell <see cref="EbInvoice"/>.</summary>
    public static class EbInterfaceReader
    {
        /// <summary>Liest eine Datei.</summary>
        /// <exception cref="EbInterfaceReadException">Das Dokument ist kein gültiges ebInterface (XML, Version oder Schema).</exception>
        public static EbInvoice ReadFile(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            using FileStream stream = File.OpenRead(path);
            return Read(stream);
        }

        /// <summary>
        /// Liest ein Dokument aus einem Stream. Es wird vorher gegen den ebInterface-Standard geprüft
        /// (<see cref="ValidationProfile.Standard"/>); nur schemagültige Dokumente werden gelesen.
        /// Der Stream wird nicht geschlossen.
        /// </summary>
        /// <exception cref="EbInterfaceReadException">Das Dokument ist kein gültiges ebInterface (XML, Version oder Schema).</exception>
        public static EbInvoice Read(Stream xml)
        {
            if (xml is null) throw new ArgumentNullException(nameof(xml));

            ValidationResult validation = EbInterfaceValidator.Validate(xml, new ValidationOptions(), out XDocument? document);
            if (!validation.IsValid || document?.Root == null)
                throw new EbInterfaceReadException(validation);

            return InvoiceMapper.Map(document.Root, validation.Version);
        }
    }

    /// <summary>Das Dokument konnte nicht gelesen werden, weil es kein gültiges ebInterface ist.</summary>
    public sealed class EbInterfaceReadException : Exception
    {
        internal EbInterfaceReadException(ValidationResult validation)
            : base(BuildMessage(validation))
        {
            Validation = validation;
        }

        /// <summary>Ergebnis der Prüfung mit allen Meldungen.</summary>
        public ValidationResult Validation { get; }

        private static string BuildMessage(ValidationResult validation)
        {
            string first = validation.Errors.GetEnumerator() is var errors && errors.MoveNext() ? errors.Current.ToString() : "unbekannter Fehler";
            return $"Das Dokument ist kein gültiges ebInterface und kann nicht gelesen werden: {first}";
        }
    }
}
