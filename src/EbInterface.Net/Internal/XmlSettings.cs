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
    }
}
