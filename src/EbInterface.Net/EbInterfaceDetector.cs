using System;
using System.IO;
using System.Xml;

namespace EbInterface
{
    /// <summary>Erkennt, ob und in welcher Version ein Dokument ebInterface ist.</summary>
    public static class EbInterfaceDetector
    {
        /// <summary>
        /// Liest nur bis zum Wurzelelement und ermittelt die Version über dessen Namespace.
        /// Der Stream wird nicht geschlossen; seine Position wird verändert.
        /// </summary>
        public static EbInterfaceVersion DetectVersion(Stream xml)
        {
            if (xml is null) throw new ArgumentNullException(nameof(xml));

            using var reader = XmlReader.Create(xml, Internal.XmlSettings.CreateSecureReaderSettings(closeInput: false));
            try
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        return reader.LocalName == "Invoice"
                            ? EbInterfaceVersionExtensions.FromNamespace(reader.NamespaceURI)
                            : EbInterfaceVersion.Unknown;
                    }
                }
            }
            catch (XmlException)
            {
                // Kein wohlgeformtes XML – damit auch kein ebInterface.
            }

            return EbInterfaceVersion.Unknown;
        }
    }
}
