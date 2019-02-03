using System.Text;
using XmpCore.Impl;
using Xunit;

namespace XmpCore.Tests
{
    public class XmpUtilsTests
    {
        [Fact]
        public void ConvertToInteger()
        {
            Assert.Equal(1, XmpUtils.ConvertToInteger("1"));
            Assert.Equal(1, XmpUtils.ConvertToInteger("001"));
            Assert.Equal(1, XmpUtils.ConvertToInteger("0x1"));
            Assert.Equal(15, XmpUtils.ConvertToInteger("0xF"));
            Assert.Equal(-1, XmpUtils.ConvertToInteger("-1"));
            Assert.Equal(123, XmpUtils.ConvertToInteger(" 123 "));

            Assert.Throws<XmpException>(() => XmpUtils.ConvertToInteger(null));
            Assert.Throws<XmpException>(() => XmpUtils.ConvertToInteger("Foo"));
        }

        [Fact]
        public void ConvertToLong()
        {
            Assert.Equal(1L, XmpUtils.ConvertToLong("1"));
            Assert.Equal(1L, XmpUtils.ConvertToLong("001"));
            Assert.Equal(1L, XmpUtils.ConvertToLong("0x1"));
            Assert.Equal(15L, XmpUtils.ConvertToLong("0xF"));
            Assert.Equal(-1L, XmpUtils.ConvertToLong("-1"));
            Assert.Equal(123L, XmpUtils.ConvertToLong(" 123 "));

            Assert.Throws<XmpException>(() => XmpUtils.ConvertToLong(null));
            Assert.Throws<XmpException>(() => XmpUtils.ConvertToLong("Foo"));
        }

        [Fact]
        public void PackageEmptyXmpDataToJPEG()
        {
            IXmpMeta data = new XmpMeta();
            StringBuilder standard = new StringBuilder();
            StringBuilder extended = new StringBuilder();
            StringBuilder digest = new StringBuilder();

            XmpUtils.PackageForJPEG(data, standard, extended, digest);
            
            Assert.Equal(0, extended.Length);
        }
    }
}