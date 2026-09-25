using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using EbInterface.Validation;

namespace EbInterface.Internal
{
    /// <summary>
    /// Schema-Prüfung mit verständlichen deutschen Meldungen.
    /// Statt die Texte von .NET zu übersetzen (sie sind je nach Laufzeit und Sprachpaket englisch oder deutsch und
    /// tragen keinen öffentlichen Fehlerschlüssel), wird das Dokument selbst mit <see cref="XmlSchemaValidator"/>
    /// durchlaufen. So ist bei jedem Fehler bekannt, welcher Schritt ihn ausgelöst hat und was erwartet wurde.
    /// </summary>
    internal sealed class SchemaCheck
    {
        private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

        private readonly XmlSchemaSet _schemas;
        private readonly EbInterfaceVersion _version;
        private readonly IList<ValidationMessage> _messages;
        private readonly XmlSchemaValidator _validator;
        private readonly ScopeResolver _resolver = new ScopeResolver();

        // Was gerade geprüft wird – der Ereignis-Handler ordnet Fehler damit ein.
        private Step _step;
        private XElement? _element;
        private XAttribute? _attribute;
        private XmlSchemaType? _currentType;
        private XmlSchemaParticle[] _expectedParticles = Array.Empty<XmlSchemaParticle>();
        private XmlSchemaAttribute[] _expectedAttributes = Array.Empty<XmlSchemaAttribute>();
        private bool _stepReported;
        private string _code = "XSD-01";

        private SchemaCheck(XmlSchemaSet schemas, EbInterfaceVersion version, IList<ValidationMessage> messages)
        {
            _schemas = schemas;
            _version = version;
            _messages = messages;
            _validator = new XmlSchemaValidator(new NameTable(), schemas, _resolver, XmlSchemaValidationFlags.AllowXmlAttributes);
            _validator.ValidationEventHandler += OnValidationEvent;
        }

        internal static void Run(XDocument document, XmlSchemaSet schemas, EbInterfaceVersion version, IList<ValidationMessage> messages)
        {
            var check = new SchemaCheck(schemas, version, messages);
            check._validator.Initialize();
            check.ValidateElement(document.Root!);
            check._validator.EndValidation();
        }

        private void ValidateElement(XElement element)
        {
            XElement? parent = _element;
            _resolver.Current = element;
            _validator.LineInfoProvider = element;

            var info = new XmlSchemaInfo();
            Begin(Step.Element, parent);
            _element = element;
            _expectedParticles = parent == null ? Array.Empty<XmlSchemaParticle>() : _validator.GetExpectedParticles();
            _validator.ValidateElement(element.Name.LocalName, element.Name.NamespaceName, info,
                (string?)element.Attribute(XName.Get("type", XsiNamespace)),
                (string?)element.Attribute(XName.Get("nil", XsiNamespace)),
                (string?)element.Attribute(XName.Get("schemaLocation", XsiNamespace)),
                (string?)element.Attribute(XName.Get("noNamespaceSchemaLocation", XsiNamespace)));

            if (_stepReported)
            {
                // Das Element ist hier nicht erlaubt; sein Inhalt wird nicht weiter bewertet.
                _validator.SkipToEndElement(info);
                _element = parent;
                _resolver.Current = parent;
                return;
            }

            XmlSchemaType? type = info.SchemaType;

            foreach (XAttribute attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration || attribute.Name.NamespaceName == XsiNamespace) continue;

                _validator.LineInfoProvider = attribute;
                _expectedAttributes = _validator.GetExpectedAttributes();
                Begin(Step.Attribute, element);
                _attribute = attribute;
                _validator.ValidateAttribute(attribute.Name.LocalName, attribute.Name.NamespaceName, attribute.Value, null);
            }

            _validator.LineInfoProvider = element;
            _expectedAttributes = _validator.GetExpectedAttributes();
            Begin(Step.EndOfAttributes, element);
            _validator.ValidateEndOfAttributes(null);

            foreach (XNode node in element.Nodes())
            {
                switch (node)
                {
                    case XElement child:
                        ValidateElement(child);
                        _resolver.Current = element;
                        break;
                    case XText text: // auch XCData
                        _validator.LineInfoProvider = text;
                        _expectedParticles = _validator.GetExpectedParticles();
                        Begin(Step.Text, element);
                        if (string.IsNullOrWhiteSpace(text.Value))
                            _validator.ValidateWhitespace(text.Value);
                        else
                            _validator.ValidateText(text.Value);
                        break;
                }
            }

            _validator.LineInfoProvider = element;
            _expectedParticles = _validator.GetExpectedParticles();
            Begin(Step.EndElement, element);
            _currentType = type;
            _validator.ValidateEndElement(null);

            _element = parent;
        }

        private void Begin(Step step, XElement? element)
        {
            _step = step;
            _element = element;
            _attribute = null;
            _stepReported = false;
        }

        private void OnValidationEvent(object? sender, ValidationEventArgs e)
        {
            if (e.Severity != XmlSeverityType.Error) return;
            if (_stepReported) return; // je Schritt eine Meldung – Folgefehler desselben Schritts unterdrücken
            _stepReported = true;

            IXmlLineInfo? at = (IXmlLineInfo?)_attribute ?? _element;
            string? text = Describe(e);
            if (text == null)
            {
                Add("XSD-01", $"Verstoß gegen das Schema von {_version.ToDisplayString()}: {e.Message}", at);
                return;
            }

            Add(_code, text, at);
        }

        private string? Describe(ValidationEventArgs e)
        {
            switch (_step)
            {
                case Step.Element:
                    return DescribeUnexpectedElement();

                case Step.Attribute:
                    return DescribeAttribute(e);

                case Step.EndOfAttributes:
                    return DescribeMissingAttributes();

                case Step.Text:
                    _code = "XSD-05";
                    return $"{Tag(_element!)} darf keinen Text enthalten, nur Unterelemente" +
                        ExpectedSuffix(" (erwartet: {0})") + ".";

                case Step.EndElement:
                    if (IsElementOnly(_currentType) && e.Exception?.InnerException == null)
                    {
                        _code = "XSD-02";
                        string expected = ExpectedNames();
                        return expected.Length > 0
                            ? $"In {Tag(_element!)} fehlt ein Pflichtelement: {expected}."
                            : $"Der Inhalt von {Tag(_element!)} ist unvollständig.";
                    }

                    _code = "XSD-06";
                    return DescribeValue(_element!.Value, _currentType, $"in {Tag(_element!)}", e);

                default:
                    return null;
            }
        }

        private string DescribeUnexpectedElement()
        {
            XElement element = _element!;
            XElement? parent = element.Parent;
            string where = parent != null ? $" in {Tag(parent)}" : string.Empty;
            string expected = ExpectedNames();

            bool known = element.Name.NamespaceName == _version.GetNamespace() &&
                _schemas.GlobalElements.Contains(new XmlQualifiedName(element.Name.LocalName, element.Name.NamespaceName));

            if (!known)
            {
                _code = "XSD-04";
                string what = element.Name.NamespaceName == _version.GetNamespace()
                    ? $"Das Element {Tag(element)} gibt es in {_version.ToDisplayString()} nicht (Tippfehler?)."
                    : $"Das Element {Tag(element)} aus dem Namespace '{element.Name.NamespaceName}' ist{where} nicht erlaubt.";
                return what + (expected.Length > 0 ? $" Erwartet wird{where} an dieser Stelle: {expected}." : string.Empty);
            }

            _code = "XSD-03";
            if (expected.Length == 0)
                return $"Das Element {Tag(element)} ist{where} an dieser Stelle nicht erlaubt; es darf hier kein weiteres Element folgen.";

            XmlSchemaParticle[] required = _expectedParticles.Where(p => p.MinOccurs >= 1).ToArray();
            string hint = required.Length == 1 && _expectedParticles.Length == 1
                ? $" Vermutlich fehlt davor das Pflichtelement {Tag(Name(required[0]))}, oder die Reihenfolge stimmt nicht."
                : " Prüfen Sie die Reihenfolge und ob davor ein Pflichtelement fehlt.";
            return $"Das Element {Tag(element)} ist{where} an dieser Stelle nicht erlaubt. Erwartet wird hier: {expected}.{hint}";
        }

        private string DescribeAttribute(ValidationEventArgs e)
        {
            XAttribute attribute = _attribute!;
            XmlSchemaAttribute? declaration = _expectedAttributes.FirstOrDefault(a =>
                a.QualifiedName.Name == attribute.Name.LocalName && a.QualifiedName.Namespace == attribute.Name.NamespaceName);

            if (declaration == null)
            {
                _code = "XSD-08";
                return $"Das Attribut {attribute.Name.LocalName} ist bei {Tag(_element!)} nicht vorgesehen.";
            }

            _code = "XSD-06";
            return DescribeValue(attribute.Value, declaration.AttributeSchemaType, $"im Attribut {attribute.Name.LocalName} von {Tag(_element!)}", e);
        }

        private string? DescribeMissingAttributes()
        {
            var missing = _expectedAttributes.Where(a => a.Use == XmlSchemaUse.Required).Select(a => a.QualifiedName.Name).ToList();
            if (missing.Count == 0) return null;

            _code = "XSD-07";
            return missing.Count == 1
                ? $"Bei {Tag(_element!)} fehlt das Pflichtattribut {missing[0]}."
                : $"Bei {Tag(_element!)} fehlen die Pflichtattribute {string.Join(", ", missing)}.";
        }

        private static string DescribeValue(string value, XmlSchemaType? type, string where, ValidationEventArgs e)
        {
            string reason = ValueDiagnosis.Explain(value, type) ?? $"Er entspricht nicht dem Schema ({e.Message}).";
            return $"Der Wert '{Shorten(value)}' {where} ist ungültig: {reason}";
        }

        private static bool IsElementOnly(XmlSchemaType? type) =>
            type is XmlSchemaComplexType complex &&
            (complex.ContentType == XmlSchemaContentType.ElementOnly || complex.ContentType == XmlSchemaContentType.Empty);

        private string ExpectedNames() =>
            string.Join(", ", _expectedParticles.Select(Name).Where(n => n.Length > 0).Distinct().Select(Tag));

        private string ExpectedSuffix(string format)
        {
            string expected = ExpectedNames();
            return expected.Length > 0 ? string.Format(format, expected) : string.Empty;
        }

        private static string Name(XmlSchemaParticle particle) => particle switch
        {
            XmlSchemaElement element => element.QualifiedName.Name,
            XmlSchemaAny _ => "Erweiterungselement",
            _ => string.Empty,
        };

        private static string Tag(XElement element) => Tag(element.Name.LocalName);

        private static string Tag(string name) => name == "Erweiterungselement" ? name : $"<{name}>";

        private static string Shorten(string value)
        {
            value = value.Replace("\r", " ").Replace("\n", " ");
            return value.Length <= 60 ? value : value.Substring(0, 57) + "…";
        }

        private void Add(string code, string message, IXmlLineInfo? at) =>
            _messages.Add(new ValidationMessage(ValidationSeverity.Error, code, message, LineInfo.Line(at), LineInfo.Position(at)));

        private enum Step
        {
            Element,
            Attribute,
            EndOfAttributes,
            Text,
            EndElement,
        }

        /// <summary>Löst Namespace-Präfixe im Gültigkeitsbereich des aktuellen Elements auf (für QName-Werte).</summary>
        private sealed class ScopeResolver : IXmlNamespaceResolver
        {
            internal XElement? Current { get; set; }

            public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
            {
                var result = new Dictionary<string, string>();
                for (XElement? e = Current; e != null; e = e.Parent)
                {
                    foreach (XAttribute a in e.Attributes().Where(a => a.IsNamespaceDeclaration))
                    {
                        string prefix = a.Name.NamespaceName.Length == 0 ? string.Empty : a.Name.LocalName;
                        if (!result.ContainsKey(prefix)) result[prefix] = a.Value;
                    }
                }

                return result;
            }

            public string? LookupNamespace(string prefix) =>
                prefix.Length == 0 ? Current?.GetDefaultNamespace().NamespaceName : Current?.GetNamespaceOfPrefix(prefix)?.NamespaceName;

            public string? LookupPrefix(string namespaceName) => Current?.GetPrefixOfNamespace(namespaceName);
        }
    }
}
