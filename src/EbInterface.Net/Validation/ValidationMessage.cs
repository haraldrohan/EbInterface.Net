namespace EbInterface.Validation
{
    /// <summary>Schweregrad einer Prüfmeldung.</summary>
    public enum ValidationSeverity
    {
        /// <summary>Hinweis, kein Fehler.</summary>
        Warning,

        /// <summary>Fehler – das Dokument ist ungültig.</summary>
        Error,
    }

    /// <summary>Eine einzelne Prüfmeldung.</summary>
    public sealed class ValidationMessage
    {
        /// <summary>Erstellt eine Prüfmeldung.</summary>
        internal ValidationMessage(ValidationSeverity severity, string code, string message, int line = 0, int position = 0)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Line = line;
            Position = position;
        }

        /// <summary>Schweregrad.</summary>
        public ValidationSeverity Severity { get; }

        /// <summary>Stabiler Fehlercode, z. B. "XML-01" oder "XSD-01".</summary>
        public string Code { get; }

        /// <summary>Beschreibung in verständlicher Sprache.</summary>
        public string Message { get; }

        /// <summary>Zeile im Dokument (1-basiert, 0 = unbekannt).</summary>
        public int Line { get; }

        /// <summary>Spalte im Dokument (1-basiert, 0 = unbekannt).</summary>
        public int Position { get; }

        /// <inheritdoc />
        public override string ToString() =>
            Line > 0 ? $"[{Code}] Zeile {Line}, Spalte {Position}: {Message}" : $"[{Code}] {Message}";
    }
}
