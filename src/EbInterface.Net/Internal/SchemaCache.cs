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
        private static readonly ConcurrentDictionary<EbInterfaceVersion, XmlSchemaSet> Cache =
            new ConcurrentDictionary<EbInterfaceVersion, XmlSchemaSet>();

        internal static bool IsSupported(EbInterfaceVersion version) =>
            version == EbInterfaceVersion.V5p0 ||
            version == EbInterfaceVersion.V6p0 ||
            version == EbInterfaceVersion.V6p1;

        internal static XmlSchemaSet Get(EbInterfaceVersion version) => Cache.GetOrAdd(version, Load);

        private static XmlSchemaSet Load(EbInterfaceVersion version)
        {
            if (!IsSupported(version))
                throw new NotSupportedException($"Schema-Prüfung für {version.ToDisplayString()} ist noch nicht verfügbar.");

            string folder = version.ToString().Substring(1); // "V6p1" -> "6p1"
            string resourceName = $"EbInterface.Schemas.{folder}.Invoice.xsd";

            using Stream? stream = typeof(SchemaCache).Assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
                throw new InvalidOperationException($"Eingebettetes Schema '{resourceName}' nicht gefunden.");

            using var reader = XmlReader.Create(stream, XmlSettings.CreateSecureReaderSettings(closeInput: false));
            var set = new XmlSchemaSet { XmlResolver = null };
            set.Add(version.GetNamespace(), reader);
            set.Compile();
            return set;
        }
    }
}
