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
    }
}