using System.Xml;

namespace EbInterface.Internal
{
    internal static class XmlSettings
    {
        /// <summary>
        /// Sichere Voreinstellungen: keine DTDs, keine externen Ressourcen.
        /// Rechnungen kommen oft von Dritten – XXE und ähnliche Angriffe sind auszuschließen.
        /// </summary>
        internal static XmlReaderSettings CreateSecureReaderSettings(bool closeInput)
        {
            return new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                CloseInput = closeInput,
                IgnoreComments = true,
            };
        }

        /// <summary>
        /// Nur für mitgelieferte, unveränderte Fremd-Schemas mit DOCTYPE (W3C xmldsig): Die DTD wird übersprungen,
        /// nicht ausgewertet – keine Entitäten, kein Nachladen. Niemals für Eingabedokumente verwenden.
        /// </summary>
        internal static XmlReaderSettings CreateTrustedSchemaReaderSettings()
        {
            XmlReaderSettings settings = CreateSecureReaderSettings(closeInput: false);
            settings.DtdProcessing = DtdProcessing.Ignore;
            return settings;
        }
    }
}
