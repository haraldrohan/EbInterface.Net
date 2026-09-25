using System.Xml;

namespace EbInterface.Internal
{
    /// <summary>Zeilen- und Spaltenangaben aus XML-Knoten (0 = unbekannt).</summary>
    internal static class LineInfo
    {
        internal static int Line(IXmlLineInfo? node, int fallback = 0) =>
            node != null && node.HasLineInfo() ? node.LineNumber : fallback;

        internal static int Position(IXmlLineInfo? node, int fallback = 0) =>
            node != null && node.HasLineInfo() ? node.LinePosition : fallback;
    }
}
