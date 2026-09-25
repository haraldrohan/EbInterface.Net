using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace EbInterface.Internal
{
    /// <summary>
    /// Ermittelt, warum ein Wert nicht zu seinem Schema-Datentyp passt, und formuliert den Grund auf Deutsch.
    /// Geprüft werden der Grunddatentyp (z. B. Datum, Dezimalzahl) und die Facetten der Ableitungskette.
    /// </summary>
    internal static class ValueDiagnosis
    {
        private const int MaxListedValues = 15;

        /// <summary>Grund als Satz, oder <c>null</c>, wenn er sich nicht bestimmen lässt.</summary>
        internal static string? Explain(string value, XmlSchemaType? type)
        {
            if (type == null) return null;

            XmlSchemaDatatype? datatype = type.Datatype;
            if (datatype == null) return null;

            bool isString = datatype.TypeCode == XmlTypeCode.String;
            string normalized = isString ? value : value.Trim();

            string? lexical = CheckLexical(normalized, datatype.TypeCode);
            if (lexical != null) return lexical;

            foreach (List<XmlSchemaFacet> level in FacetLevels(type))
            {
                string? reason = CheckLevel(normalized, level);
                if (reason != null) return reason;
            }

            return null;
        }

        private static string? CheckLexical(string value, XmlTypeCode code)
        {
            XmlSchemaSimpleType? primitive = XmlSchemaType.GetBuiltInSimpleType(code);
            if (primitive == null) return null;

            try
            {
                primitive.Datatype!.ParseValue(value, new NameTable(), null);
                return null;
            }
            catch (XmlSchemaException)
            {
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
                return "Die Zahl ist zu groß.";
            }

            switch (code)
            {
                case XmlTypeCode.Date:
                    return "Erwartet wird ein Datum im Format JJJJ-MM-TT, z. B. 2026-09-25.";
                case XmlTypeCode.DateTime:
                    return "Erwartet wird Datum und Uhrzeit im Format JJJJ-MM-TTThh:mm:ss, z. B. 2026-09-25T14:30:00.";
                case XmlTypeCode.Decimal:
                case XmlTypeCode.Double:
                case XmlTypeCode.Float:
                    return "Erwartet wird eine Zahl mit Punkt als Dezimaltrennzeichen und ohne Tausendertrennzeichen, z. B. 1234.50.";
                case XmlTypeCode.Integer:
                case XmlTypeCode.Int:
                case XmlTypeCode.Long:
                case XmlTypeCode.Short:
                case XmlTypeCode.Byte:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.NonPositiveInteger:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.UnsignedInt:
                case XmlTypeCode.UnsignedLong:
                case XmlTypeCode.UnsignedShort:
                case XmlTypeCode.UnsignedByte:
                    return "Erwartet wird eine ganze Zahl ohne Nachkommastellen.";
                case XmlTypeCode.Boolean:
                    return "Erwartet wird true oder false.";
                default:
                    return $"Der Wert passt nicht zum Datentyp {code}.";
            }
        }

        /// <summary>Facetten je Ableitungsstufe, von der speziellsten zur allgemeinsten.</summary>
        private static IEnumerable<List<XmlSchemaFacet>> FacetLevels(XmlSchemaType type)
        {
            for (XmlSchemaType? current = type; current != null && !IsBuiltIn(current); current = current.BaseXmlSchemaType)
            {
                XmlSchemaObjectCollection? facets = current switch
                {
                    XmlSchemaSimpleType { Content: XmlSchemaSimpleTypeRestriction r } => r.Facets,
                    XmlSchemaComplexType { ContentModel: XmlSchemaSimpleContent { Content: XmlSchemaSimpleContentRestriction r } } => r.Facets,
                    _ => null,
                };

                if (facets != null && facets.Count > 0)
                    yield return facets.OfType<XmlSchemaFacet>().ToList();
            }
        }

        private static bool IsBuiltIn(XmlSchemaType type) =>
            type.QualifiedName.Namespace == XmlSchema.Namespace;

        private static string? CheckLevel(string value, List<XmlSchemaFacet> facets)
        {
            var enumeration = facets.OfType<XmlSchemaEnumerationFacet>().Select(f => f.Value ?? string.Empty).ToList();
            if (enumeration.Count > 0 && !enumeration.Contains(value))
            {
                string list = string.Join(", ", enumeration.Take(MaxListedValues));
                if (enumeration.Count > MaxListedValues) list += ", …";
                return $"Zulässig sind nur: {list}.";
            }

            var patterns = facets.OfType<XmlSchemaPatternFacet>().Select(f => f.Value ?? string.Empty).ToList();
            if (patterns.Count > 0 && !patterns.Any(p => MatchesPattern(value, p) != false))
                return $"Der Wert hat nicht das vorgeschriebene Format (Muster: {string.Join(" oder ", patterns)}).";

            int length = value.Length;
            foreach (XmlSchemaFacet facet in facets)
            {
                string limit = facet.Value ?? string.Empty;
                switch (facet)
                {
                    case XmlSchemaLengthFacet _ when TryInt(limit, out int n) && length != n:
                        return $"Er muss genau {n} Zeichen lang sein (hier {length}).";
                    case XmlSchemaMinLengthFacet _ when TryInt(limit, out int n) && length < n:
                        return length == 0 ? "Das Feld darf nicht leer sein." : $"Er muss mindestens {n} Zeichen lang sein (hier {length}).";
                    case XmlSchemaMaxLengthFacet _ when TryInt(limit, out int n) && length > n:
                        return $"Er darf höchstens {n} Zeichen lang sein (hier {length}).";
                    case XmlSchemaTotalDigitsFacet _ when TryInt(limit, out int n) && TryDecimal(value, out decimal d) && TotalDigits(d) > n:
                        return $"Er hat zu viele Stellen; erlaubt sind höchstens {n}.";
                    case XmlSchemaFractionDigitsFacet _ when TryInt(limit, out int n) && TryDecimal(value, out decimal d) && DecimalMath.DecimalPlaces(d) > n:
                        return n == 0 ? "Nachkommastellen sind nicht erlaubt." : $"Er hat zu viele Nachkommastellen; erlaubt sind höchstens {n}.";
                    case XmlSchemaMinInclusiveFacet _ when Compare(value, limit) < 0:
                        return $"Er muss mindestens {limit} sein.";
                    case XmlSchemaMaxInclusiveFacet _ when Compare(value, limit) > 0:
                        return $"Er darf höchstens {limit} sein.";
                    case XmlSchemaMinExclusiveFacet _ when Compare(value, limit) <= 0:
                        return $"Er muss größer als {limit} sein.";
                    case XmlSchemaMaxExclusiveFacet _ when Compare(value, limit) >= 0:
                        return $"Er muss kleiner als {limit} sein.";
                }
            }

            return null;
        }

        /// <summary>XSD-Muster sind immer vollständig verankert. <c>null</c>, wenn .NET das Muster nicht versteht.</summary>
        private static bool? MatchesPattern(string value, string pattern)
        {
            try
            {
                return Regex.IsMatch(value, "^(?:" + pattern + ")$", RegexOptions.CultureInvariant);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Vergleich als Dezimalzahl oder Datum; 0, wenn nicht vergleichbar.</summary>
        private static int Compare(string value, string limit)
        {
            if (TryDecimal(value, out decimal a) && TryDecimal(limit, out decimal b)) return a.CompareTo(b);
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime x) &&
                DateTime.TryParse(limit, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime y))
                return x.CompareTo(y);
            return 0;
        }

        private static bool TryInt(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

        private static bool TryDecimal(string text, out decimal value) =>
            decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);

        private static int TotalDigits(decimal value)
        {
            decimal normalized = value / 1.0000000000000000000000000000m;
            string digits = Math.Abs(normalized).ToString(CultureInfo.InvariantCulture).Replace(".", string.Empty).TrimStart('0');
            return Math.Max(digits.Length, 1);
        }
    }
}
