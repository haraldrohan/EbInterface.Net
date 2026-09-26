using System.Collections.Generic;
using System.Linq;

namespace EbInterface.Validation
{
    /// <summary>Ergebnis einer Prüfung.</summary>
    public sealed class ValidationResult
    {
        internal ValidationResult(EbInterfaceVersion version, IReadOnlyList<ValidationMessage> messages)
        {
            Version = version;
            Messages = messages;
        }

        /// <summary>Erkannte ebInterface-Version.</summary>
        public EbInterfaceVersion Version { get; }

        /// <summary>Alle Meldungen in der Reihenfolge ihres Auftretens.</summary>
        public IReadOnlyList<ValidationMessage> Messages { get; }

        /// <summary>Nur die Fehler.</summary>
        public IEnumerable<ValidationMessage> Errors => Messages.Where(m => m.Severity == ValidationSeverity.Error);

        /// <summary>Nur die Warnungen (z. B. Felder, die e-Rechnung.gv.at nicht auswertet).</summary>
        public IEnumerable<ValidationMessage> Warnings => Messages.Where(m => m.Severity == ValidationSeverity.Warning);

        /// <summary>True, wenn kein Fehler gefunden wurde.</summary>
        public bool IsValid => !Errors.Any();
    }
}
