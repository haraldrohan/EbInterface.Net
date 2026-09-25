using System.Text;
using EbInterface.Validation;
using static EbInterface.Tests.TestSupport;

namespace EbInterface.Tests
{
    /// <summary>Prüfstufen XML, Version und Schema sowie das Verhalten der Profile.</summary>
    public class ValidatorStageTests
    {
        [Fact]
        public void MalformedXml_ReportsXml01WithLine()
        {
            var result = Validate("<Invoice xmlns=\"http://www.ebinterface.at/schema/6p1/\">\n  <InvoiceNumber>1</Invoice>");

            var error = Assert.Single(result.Errors);
            Assert.Equal("XML-01", error.Code);
            Assert.Equal(2, error.Line);
        }

        [Fact]
        public void Dtd_IsRejected()
        {
            var result = Validate("<!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]><x>&e;</x>");

            Assert.Equal("XML-01", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public void OtherRootElement_ReportsVer01()
        {
            var result = Validate("<Order xmlns=\"http://www.ebinterface.at/schema/6p1/\"/>");

            Assert.Equal("VER-01", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public void OutdatedVersion_ReportsVer03()
        {
            var result = Validate("<Invoice xmlns=\"http://www.ebinterface.at/schema/4p2/\"/>");

            Assert.Equal("VER-03", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public void Version4p3_IsDetectedButNotYetChecked()
        {
            var result = EbInterfaceValidator.ValidateFile(
                Path.Combine(StandardsRoot, "ebInterface4p3", "samples", "ebInterface_4p3_sample.xml"));

            Assert.Equal(EbInterfaceVersion.V4p3, result.Version);
            Assert.Equal("VER-02", Assert.Single(result.Errors).Code);
        }

        [Fact]
        public void NonSeekableStream_IsAccepted()
        {
            byte[] bytes = File.ReadAllBytes(TestFile("6p1/gueltig-bestellnummer.xml"));
            using var stream = new NonSeekableStream(new MemoryStream(bytes));

            var result = EbInterfaceValidator.Validate(stream, ERechnung);

            Assert.True(result.IsValid, Describe(result));
        }

        [Fact]
        public void StandardProfile_DoesNotApplyFederalRules()
        {
            var document = LoadTestData("6p1/gueltig-bestellnummer.xml");
            document.Root!.SetAttributeValue("DocumentType", "SelfBilling");

            Assert.True(Validate(document).IsValid);
            Assert.Contains(Validate(document, ERechnung).Errors, m => m.Code == "ERB-01");
        }

        [Fact]
        public void SchemaError_SkipsFederalRules()
        {
            var document = LoadTestData("6p1/gueltig-bestellnummer.xml");
            document.El("InvoiceNumber").Remove();
            document.El("OrderReference").Remove();

            var result = Validate(document, ERechnung);

            Assert.All(result.Errors, m => Assert.Equal("XSD-01", m.Code));
        }

        private sealed class NonSeekableStream : Stream
        {
            private readonly Stream _inner;

            public NonSeekableStream(Stream inner) => _inner = inner;

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
