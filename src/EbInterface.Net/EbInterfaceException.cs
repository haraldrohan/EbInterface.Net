using System;
using EbInterface.Validation;

namespace EbInterface
{
    /// <summary>
    /// Basis der Ausnahmen dieser Bibliothek: Ein Dokument konnte nicht gelesen oder geschrieben werden.
    /// <see cref="Validation"/> enthält alle Meldungen mit stabilen Codes.
    /// </summary>
    public abstract class EbInterfaceException : Exception
    {
        private protected EbInterfaceException(string message, ValidationResult validation)
            : base(message)
        {
            Validation = validation;
        }

        /// <summary>Ergebnis der Prüfung mit allen Meldungen.</summary>
        public ValidationResult Validation { get; }

        private protected static string FirstError(ValidationResult validation)
        {
            foreach (ValidationMessage error in validation.Errors)
                return error.ToString();
            return "unbekannter Fehler";
        }
    }
}
