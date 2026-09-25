using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Schema;

namespace EbInterface.Internal
{
    /// <summary>Lädt die eingebetteten Schemas einmalig und hält sie threadsicher vor.</summary>
    internal static class SchemaCache
    {
        private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

        private static readonly ConcurrentDictionary<EbInterfaceVersion, XmlSchemaSet> Cache =
            new ConcurrentDictionary<EbInterfaceVersion, XmlSchemaSet>();

        internal static bool IsSupported(EbInterfaceVersion version) =>
            version == EbInterfaceVersion.V4p3 ||
            version == EbInterfaceVersion.V5p0 ||
            version == EbInterfaceVersion.V6p0 ||
            version == EbInterfaceVersion.V6p1;

        internal static XmlSchemaSet Get(EbInterfaceVersion version) => Cache.GetOrAdd(version, Load);

        private static XmlSchemaSet Load(EbInterfaceVersion version)
        {
            if (!IsSupported(version))
                throw new NotSupportedException($"Schema-Prüfung für {version.ToDisplayString()} ist noch nicht verfügbar.");

            string folder = version.ToString().Substring(1); // "V6p1" -> "6p1"

            // Ohne XmlResolver werden xs:import-Angaben nicht nachgeladen. Importierte Schemas kommen deshalb
            // vorab aus den eigenen Ressourcen in dieselbe Menge; beim Kompilieren werden die Verweise aufgelöst.
            var set = new XmlSchemaSet { XmlResolver = null };
            if (version == EbInterfaceVersion.V4p3)
            {
                Add(set, XmlDsigNamespace, "EbInterface.Schemas.w3c.xmldsig-core-schema.xsd", trustedDtd: true);
                Add(set, "http://www.ebinterface.at/schema/4p3/extensions/sv", "EbInterface.Schemas.4p3.ebInterfaceExtension_SV.xsd");
                Add(set, "http://www.ebinterface.at/schema/4p3/extensions/ext", "EbInterface.Schemas.4p3.ebInterfaceExtension.xsd");
            }

            Add(set, version.GetNamespace(), $"EbInterface.Schemas.{folder}.Invoice.xsd");
            set.Compile();
            return set;
        }

        private static void Add(XmlSchemaSet set, string targetNamespace, string resourceName, bool trustedDtd = false)
        {
            using Stream? stream = typeof(SchemaCache).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                throw new InvalidOperationException($"Eingebettetes Schema '{resourceName}' nicht gefunden.");

            XmlReaderSettings settings = trustedDtd
                ? XmlSettings.CreateTrustedSchemaReaderSettings()
                : XmlSettings.CreateSecureReaderSettings(closeInput: false);
            using var reader = XmlReader.Create(stream, settings);
            set.Add(targetNamespace, reader);
        }
    }
}
