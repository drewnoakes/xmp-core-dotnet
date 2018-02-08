using XmpCore.Impl;
using XmpCore.Options;
using Xunit;

namespace XmpCore.Tests
{
    public sealed class XmpNodeTests
    {
        [Fact]
        public void Basics()
        {
            var node = new XmpNode("Foo", new PropertyOptions());
            var child = new XmpNode("Child", new PropertyOptions());

            Assert.Equal("Foo", node.Name);
            Assert.Null(node.Value);

            Assert.False(node.HasChildren);
            Assert.Equal(0, node.GetChildrenLength());

            node.AddChild(child);

            Assert.True(node.HasChildren);
            Assert.Equal(1, node.GetChildrenLength());

            Assert.Same(child, node.GetChild(1));
            Assert.Same(child, node.FindChildByName("Child"));
            Assert.Null(node.FindChildByName("NonExistantChild"));
        }

        [Fact]
        public void ManyChildren()
        {
            var node = new XmpNode("Foo", new PropertyOptions());

            for (var i = 1; i < 10000; i++)
            {
                node.AddChild(new XmpNode($"Child{i}", new PropertyOptions()));
            }

            for (var i = 1; i < 10000; i++)
            {
                var child = node.FindChildByName($"Child{i}");
                Assert.Same(node.GetChild(i), child);
            }
        }
    }
}
