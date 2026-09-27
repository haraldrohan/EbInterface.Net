namespace EbInterface.Internal
{
    /// <summary>Prüfung von IBANs nach ISO 13616 (Aufbau und Prüfziffer, Modulo 97).</summary>
    internal static class Iban
    {
        /// <summary>Wie das Portal: Leerzeichen entfernen, Kleinbuchstaben zulassen („AT61 1904 …“ und „at61…“ sind gültig).</summary>
        internal static string Normalize(string iban) => iban.Replace(" ", string.Empty).Trim().ToUpperInvariant();

        /// <summary>
        /// True, wenn der (normalisierte) Wert eine IBAN mit gültiger Prüfziffer ist: Ländercode (2 Großbuchstaben),
        /// 2 Prüfziffern, dann 11 bis 30 Ziffern oder Großbuchstaben.
        /// </summary>
        internal static bool HasValidCheckDigits(string iban)
        {
            if (iban.Length < 15 || iban.Length > 34) return false;
            if (!IsUpper(iban[0]) || !IsUpper(iban[1]) || !IsDigit(iban[2]) || !IsDigit(iban[3])) return false;

            // Die ersten vier Zeichen ans Ende, Buchstaben als Zahlen (A = 10 … Z = 35), Rest modulo 97 muss 1 sein.
            int remainder = 0;
            for (int i = 0; i < iban.Length; i++)
            {
                char c = iban[(i + 4) % iban.Length];
                if (IsDigit(c))
                {
                    remainder = (remainder * 10 + (c - '0')) % 97;
                }
                else if (IsUpper(c))
                {
                    remainder = (remainder * 100 + (c - 'A' + 10)) % 97;
                }
                else
                {
                    return false;
                }
            }

            return remainder == 1;
        }

        private static bool IsDigit(char c) => c >= '0' && c <= '9';

        private static bool IsUpper(char c) => c >= 'A' && c <= 'Z';
    }
}
