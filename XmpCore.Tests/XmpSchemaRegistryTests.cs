using XmpCore.Impl;
using Xunit;

namespace XmpCore.Tests
{
    public sealed class XmpSchemaRegistryTests
    {
        private readonly XmpSchemaRegistry _registry;

        public XmpSchemaRegistryTests()
        {
            _registry = new XmpSchemaRegistry();
        }

        [Fact]
        public void DefaultNamespaces()
        {
            Assert.Equal(56, _registry.Namespaces.Count);
        }

        [Fact]
        public void DefaultPrefixes()
        {
            Assert.Equal(56, _registry.Prefixes.Count);
        }

        [Fact]
        public void DefaultAliases()
        {
            Assert.Equal(34, _registry.Aliases.Count);
        }

        [Fact]
        public void Namespaces()
        {
            Assert.Equal("ns1:", _registry.RegisterNamespace("ns:test1/", "ns1"));
            Assert.Equal("ns1:", _registry.RegisterNamespace("ns:test1/", "ns1"));

            Assert.Equal("ns2:", _registry.RegisterNamespace("ns:test2/", "ns2"));

            Assert.Equal("ns1_1_:", _registry.RegisterNamespace("ns:test3/", "ns1"));

            Assert.Equal("ns1:", _registry.GetNamespacePrefix("ns:test1/"));
            Assert.Equal("ns2:", _registry.GetNamespacePrefix("ns:test2/"));
            Assert.Equal("ns1_1_:", _registry.GetNamespacePrefix("ns:test3/"));

            Assert.Null(_registry.GetNamespacePrefix("foo"));

            Assert.Equal("ns:test1/", _registry.GetNamespaceUri("ns1"));
            Assert.Equal("ns:test1/", _registry.GetNamespaceUri("ns1:"));
            Assert.Equal("ns:test2/", _registry.GetNamespaceUri("ns2"));
            Assert.Equal("ns:test3/", _registry.GetNamespaceUri("ns1_1_"));

            Assert.Null(_registry.GetNamespaceUri("foo"));

            _registry.DeleteNamespace("ns:test1/");

            Assert.Null(_registry.GetNamespaceUri("ns1"));
            Assert.Null(_registry.GetNamespacePrefix("ns:test1/"));

            // deleting again has no effect
            _registry.DeleteNamespace("ns:test1/");
        }

        [Fact]
        public void Aliases()
        {
            _registry.RegisterNamespace("ns:test1/", "ns1");

            var alias = _registry.ResolveAlias("ns:test1/", "alias1");

            Assert.Equal(null, alias);
        }
    }
}