namespace EbInterface
{
    /// <summary>Versionen des ebInterface-Standards.</summary>
    public enum EbInterfaceVersion
    {
        /// <summary>Unbekannt oder kein ebInterface-Dokument.</summary>
        Unknown = 0,

        /// <summary>ebInterface 4.3.</summary>
        V4p3,

        /// <summary>ebInterface 5.0.</summary>
        V5p0,

        /// <summary>ebInterface 6.0.</summary>
        V6p0,

        /// <summary>ebInterface 6.1.</summary>
        V6p1,
    }

    /// <summary>Hilfsmethoden rund um <see cref="EbInterfaceVersion"/>.</summary>
    public static class EbInterfaceVersionExtensions
    {
        /// <summary>Liefert den XML-Namespace der Version.</summary>
        public static string GetNamespace(this EbInterfaceVersion version)
        {
            switch (version)
            {
                case EbInterfaceVersion.V4p3: return "http://www.ebinterface.at/schema/4p3/";
                case EbInterfaceVersion.V5p0: return "http://www.ebinterface.at/schema/5p0/";
                case EbInterfaceVersion.V6p0: return "http://www.ebinterface.at/schema/6p0/";
                case EbInterfaceVersion.V6p1: return "http://www.ebinterface.at/schema/6p1/";
                default: return string.Empty;
            }
        }

        /// <summary>Ermittelt die Version anhand des XML-Namespace.</summary>
        public static EbInterfaceVersion FromNamespace(string? xmlNamespace)
        {
            switch (xmlNamespace)
            {
                case "http://www.ebinterface.at/schema/4p3/": return EbInterfaceVersion.V4p3;
                case "http://www.ebinterface.at/schema/5p0/": return EbInterfaceVersion.V5p0;
                case "http://www.ebinterface.at/schema/6p0/": return EbInterfaceVersion.V6p0;
                case "http://www.ebinterface.at/schema/6p1/": return EbInterfaceVersion.V6p1;
                default: return EbInterfaceVersion.Unknown;
            }
        }

        /// <summary>Anzeigename, z. B. "ebInterface 6.1".</summary>
        public static string ToDisplayString(this EbInterfaceVersion version)
        {
            switch (version)
            {
                case EbInterfaceVersion.V4p3: return "ebInterface 4.3";
                case EbInterfaceVersion.V5p0: return "ebInterface 5.0";
                case EbInterfaceVersion.V6p0: return "ebInterface 6.0";
                case EbInterfaceVersion.V6p1: return "ebInterface 6.1";
                default: return "unbekannt";
            }
        }
    }
}
