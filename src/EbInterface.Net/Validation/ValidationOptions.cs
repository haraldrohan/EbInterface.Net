using System;

namespace EbInterface.Validation
{
    /// <summary>Legt fest, gegen welche Regeln geprüft wird.</summary>
    public enum ValidationProfile
    {
        /// <summary>
        /// Nur der ebInterface-Standard: wohlgeformtes XML, bekannte Version und XML-Schema.
        /// Passend für Rechnungen an Unternehmen (B2B).
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Standard plus die Sonderregeln von e-Rechnung.gv.at für Rechnungen an Bund, Länder,
        /// Gemeinden und andere öffentliche Auftraggeber.
        /// </summary>
        ERechnungGvAt = 1,
    }

    /// <summary>Einstellungen für eine Prüfung.</summary>
    public sealed class ValidationOptions
    {
        /// <summary>Prüfprofil. Vorgabe: <see cref="ValidationProfile.Standard"/>.</summary>
        public ValidationProfile Profile { get; set; } = ValidationProfile.Standard;

        /// <summary>
        /// Stichtag für Regeln, die vom aktuellen Datum abhängen (z. B. „Zahlungsziel höchstens
        /// 999 Tage in der Zukunft“). Vorgabe: <c>null</c> = heutiges Datum.
        /// </summary>
        public DateTime? ReferenceDate { get; set; }
    }
}
