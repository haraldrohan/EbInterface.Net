using System.Text;
using System.Xml.Linq;
using EbInterface.Validation;

namespace EbInterface.Tests
{
    internal static class TestSupport
    {
        internal static readonly string StandardsRoot = Path.Combine(AppContext.BaseDirectory, "standards");
        internal static readonly string TestDataRoot = Path.Combine(AppContext.BaseDirectory, "TestData");

        /// <summary>Fester Stichtag, damit datumsabhängige Regeln reproduzierbar sind.</summary>
        internal static readonly DateTime ReferenceDate = new DateTime(2026, 9, 25);

        internal static ValidationOptions ERechnung => new ValidationOptions
        {
            Profile = ValidationProfile.ERechnungGvAt,
            ReferenceDate = ReferenceDate,
        };

        internal static string TestFile(string relativePath) => Path.Combine(TestDataRoot, relativePath);

        internal static XDocument LoadTestData(string relativePath) => XDocument.Load(TestFile(relativePath));

        internal static ValidationResult Validate(XDocument document, ValidationOptions? options = null)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToString(SaveOptions.DisableFormatting)));
            return EbInterfaceValidator.Validate(stream, options);
        }

        internal static ValidationResult Validate(string xml, ValidationOptions? options = null)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return EbInterfaceValidator.Validate(stream, options);
        }

        internal static string Describe(ValidationResult result) =>
            result.Messages.Count == 0 ? "(keine Meldungen)" : string.Join(Environment.NewLine, result.Messages);

        /// <summary>Element im ebInterface-Namespace des Dokuments (erster Treffer in Dokumentreihenfolge).</summary>
        internal static XElement El(this XDocument document, string localName) =>
            document.Descendants(document.Root!.Name.Namespace + localName).First();

        internal static XName N(this XDocument document, string localName) => document.Root!.Name.Namespace + localName;

        /// <summary>Ersetzt den Namespace aller ebInterface-Elemente (für Tests mit anderen Versionen).</summary>
        internal static XDocument WithNamespace(this XDocument document, string newNamespace)
        {
            XNamespace oldNs = document.Root!.Name.Namespace;
            XNamespace ns = newNamespace;
            var copy = new XDocument(document);
            foreach (XElement element in copy.Descendants().Where(e => e.Name.Namespace == oldNs))
                element.Name = ns + element.Name.LocalName;
            copy.Root!.Attributes().Where(a => a.IsNamespaceDeclaration && a.Value == oldNs.NamespaceName).Remove();
            return copy;
        }
    }
}
