using System.Xml.Linq;
using Xunit;

namespace XmpCore.Tests
{
    public class XNameTests
    {
        [Fact]
        public void StringComparisons()
        {
            string s = "xmlns";
            XName n = "xmlns";

            Assert.True(s == n);
            Assert.True(n == s);
            Assert.False(s.Equals(n));
            Assert.False(n.Equals(s));
        }
    }
}