using System;
using System.Xml;
using Xunit;

namespace XmpCore.Tests
{
    public class ParseTests
    {
        [Fact]
        public void Sample1()
        {
            // https://github.com/drewnoakes/metadata-extractor-dotnet/issues/66

            const string xmpPacket = @"<?xpacket begin='ï»¿' id='W5M0MpCehiHzreSzNTczkc9d'?>
<x:xmpmeta xmlns:x=""adobe:ns:meta/"">
  <rdf:RDF xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
    <rdf:Description rdf:about=""uuid:faf5bdd5-ba3d-11da-ad31-d33d75182f1b"" xmlns:dc=""http://purl.org/dc/elements/1.1/""/>
    <rdf:Description rdf:about=""uuid:faf5bdd5-ba3d-11da-ad31-d33d75182f1b"" xmlns:xmp=""http://ns.adobe.com/xap/1.0/"">
      <xmp:CreateDate>2016-06-15T08:38:12.946</xmp:CreateDate>
    </rdf:Description>
    <rdf:Description rdf:about=""uuid:faf5bdd5-ba3d-11da-ad31-d33d75182f1b"" xmlns:dc=""http://purl.org/dc/elements/1.1/"">
      <dc:creator>
        <rdf:Seq xmlns:rdf=""http://www.w3.org/1999/02/22-rdf-syntax-ns#"">
          <rdf:li>xxx</rdf:li>
        </rdf:Seq>
      </dc:creator>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>";

            XmpMetaFactory.ParseFromString(xmpPacket);
        }

        [Fact]
        public void XXE_DoctypeDisabled()
        {
            string testdata = @"<!DOCTYPE doc [<!ENTITY win SYSTEM ""c:\windows\win.ini"">]><doc></doc>";

            // doctype not allowed by default
            Assert.Throws<XmpException>(() => XmpMetaFactory.ParseFromString(testdata));
        }

        [Fact]
        public void XXE_DoctypeEnabled()
        {
            string testdata = @"<!DOCTYPE doc [<!ENTITY win SYSTEM ""c:\windows\win.ini"">]><doc></doc>";

            var options = new Options.ParseOptions();

            // enable doctype
            options.DisallowDoctype = false;
            Exception e = null;
            try
            {
                XmpMetaFactory.ParseFromString(testdata, options);
            }
            catch (Exception ex)
            {
                e = ex;
            }
            Assert.Null(e);
        }

        [Fact]
        public void BillionLaughs_DoctypeDisabled()
        {
            var testdata = @"<?xml version=""1.0""?>
                <!DOCTYPE lolz [
                <!ENTITY lol ""lol"">
                <!ENTITY lol2 ""&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;"">
                <!ENTITY lol3 ""&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;"">
                <!ENTITY lol4 ""&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;"">
                <!ENTITY lol5 ""&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;"">
                <!ENTITY lol6 ""&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;"">
                <!ENTITY lol7 ""&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;"">
                <!ENTITY lol8 ""&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;"">
                <!ENTITY lol9 ""&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;"">
                ]>
                <lolz>&lol9;</lolz>";

            XmpException e = null;
            try
            {
                // doctype not allowed by default
                XmpMetaFactory.ParseFromString(testdata);
            }
            catch (XmpException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
            Assert.True(e.InnerException.Message.StartsWith("For security reasons DTD is prohibited"));
        }

        [Fact]
        public void BillionLaughs_DoctypeEnabled()
        {
            var testdata = @"<?xml version=""1.0""?>
                <!DOCTYPE lolz [
                <!ENTITY lol ""lol"">
                <!ENTITY lol2 ""&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;"">
                <!ENTITY lol3 ""&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;"">
                <!ENTITY lol4 ""&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;"">
                <!ENTITY lol5 ""&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;"">
                <!ENTITY lol6 ""&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;"">
                <!ENTITY lol7 ""&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;"">
                <!ENTITY lol8 ""&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;"">
                <!ENTITY lol9 ""&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;"">
                ]>
                <lolz>&lol9;</lolz>";

            var options = new Options.ParseOptions();
            // enable doctype
            options.DisallowDoctype = false;

            XmpException e = null;
            try
            {
                XmpMetaFactory.ParseFromString(testdata, options);
            }
            catch (XmpException ex)
            {
                e = ex;
            }

            Assert.NotNull(e);
            Assert.True(e.InnerException.Message.StartsWith("The input document has exceeded a limit set by MaxCharactersFromEntities"));

        }
    }
}