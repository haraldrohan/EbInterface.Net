using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EbInterface.Validation;
using Xunit;

namespace EbInterface.Tests
{
    public class OfficialSampleTests
    {
        private static readonly string SamplesRoot = Path.Combine(AppContext.BaseDirectory, "standards");

        public static IEnumerable<object[]> Samples() =>
            new[] { "5p0", "6p0", "6p1" }
                .SelectMany(v => Directory.GetFiles(Path.Combine(SamplesRoot, $"ebInterface{v}", "samples"), "*.xml"))
                .Select(f => new object[] { Path.GetRelativePath(SamplesRoot, f) });

        [Theory]
        [MemberData(nameof(Samples))]
        public void OfficialSample_IsValid_UnlessMarkedInvalid(string relativePath)
        {
            var result = EbInterfaceValidator.ValidateFile(Path.Combine(SamplesRoot, relativePath));
            bool expectedValid = !relativePath.Contains("_invalid");

            Assert.True(expectedValid == result.IsValid,
                $"{relativePath}: erwartet {(expectedValid ? "gültig" : "ungültig")}, Meldungen:\n" +
                string.Join("\n", result.Messages));
        }

        [Theory]
        [MemberData(nameof(Samples))]
        public void OfficialSample_VersionIsDetectedFromFolder(string relativePath)
        {
            string folder = relativePath.Split(Path.DirectorySeparatorChar)[0]; // z. B. "ebInterface6p1"
            var expected = (EbInterfaceVersion)Enum.Parse(typeof(EbInterfaceVersion), "V" + folder.Substring("ebInterface".Length));

            using var stream = File.OpenRead(Path.Combine(SamplesRoot, relativePath));
            Assert.Equal(expected, EbInterfaceDetector.DetectVersion(stream));
        }

        [Fact]
        public void InvalidSample_ReportsLineAndSchemaCode()
        {
            var result = EbInterfaceValidator.ValidateFile(
                Path.Combine(SamplesRoot, "ebInterface6p1", "samples", "ebinterface_6p1_sample_ecosio_invalid.xml"));

            var error = Assert.Single(result.Errors);
            Assert.Equal("XSD-01", error.Code);
            Assert.True(error.Line > 0);
        }
    }

    public class RobustnessTests
    {
        private static ValidationResult Validate(string xml) =>
            EbInterfaceValidator.Validate(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

        [Fact]
        public void ForeignXml_IsRejected()
        {
            var result = Validate("<Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\"/>");
            Assert.False(result.IsValid);
            Assert.Equal("VER-01", result.Errors.First().Code);
        }

        [Fact]
        public void MalformedXml_IsRejected()
        {
            Assert.False(Validate("<Invoice xmlns=\"http://www.ebinterface.at/schema/6p1/\">").IsValid);
            Assert.False(Validate("das ist kein xml").IsValid);
        }

        [Fact]
        public void Dtd_IsNotProcessed_XxeProtection()
        {
            const string xxe = "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///etc/passwd\">]>" +
                               "<Invoice xmlns=\"http://www.ebinterface.at/schema/6p1/\">&e;</Invoice>";
            Assert.False(Validate(xxe).IsValid);
        }
    }
}
