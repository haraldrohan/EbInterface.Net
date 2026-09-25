using EbInterface.Validation;
using static EbInterface.Tests.TestSupport;

namespace EbInterface.Tests
{
    /// <summary>Offizielle Beispiele von AUSTRIAPRO aus dem Submodule (Standardprofil, ohne Bundesregeln).</summary>
    public class OfficialSampleTests
    {
        public static IEnumerable<object[]> Samples(string versions) =>
            versions.Split(',')
                .SelectMany(v => Directory.GetFiles(Path.Combine(StandardsRoot, $"ebInterface{v}", "samples"), "*.xml"))
                .Select(f => new object[] { Path.GetRelativePath(StandardsRoot, f) });

        public static IEnumerable<object[]> SchemaSamples() => Samples("5p0,6p0,6p1");

        public static IEnumerable<object[]> AllSupportedSamples() => Samples("4p3,5p0,6p0,6p1");

        [Theory]
        [MemberData(nameof(SchemaSamples))]
        public void Sample_IsValid_UnlessMarkedInvalid(string relativePath)
        {
            var result = EbInterfaceValidator.ValidateFile(Path.Combine(StandardsRoot, relativePath));

            bool expectedValid = !relativePath.Contains("_invalid");
            Assert.True(expectedValid == result.IsValid, $"{relativePath}:{Environment.NewLine}{Describe(result)}");
        }

        [Theory]
        [MemberData(nameof(AllSupportedSamples))]
        public void Sample_VersionIsDetectedFromFolder(string relativePath)
        {
            string folder = relativePath.Split(Path.DirectorySeparatorChar)[0]; // z. B. "ebInterface6p1"
            var expected = (EbInterfaceVersion)Enum.Parse(typeof(EbInterfaceVersion), "V" + folder.Substring("ebInterface".Length));

            using var stream = File.OpenRead(Path.Combine(StandardsRoot, relativePath));
            Assert.Equal(expected, EbInterfaceDetector.DetectVersion(stream));
        }

        [Fact]
        public void InvalidSample_ReportsSchemaErrorWithLine()
        {
            var result = EbInterfaceValidator.ValidateFile(
                Path.Combine(StandardsRoot, "ebInterface6p1", "samples", "ebinterface_6p1_sample_ecosio_invalid.xml"));

            var error = Assert.Single(result.Errors);
            Assert.StartsWith("XSD-", error.Code);
            Assert.True(error.Line > 0);
        }
    }
}
