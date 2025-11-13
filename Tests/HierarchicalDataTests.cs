using NUnit.Framework;
using System;
using System.Linq;
using NUnit.Framework.Legacy;
using Visualization.Controls.Data;

namespace Tests
{
    [TestFixture]
    internal sealed class HierarchicalDataTests
    {
        [Test]
        public void RemoveLeafNodesWithoutArea_ThrowsIfSingularRootNodeHasNoArea()
        {
            // Arrange
            var root = new HierarchicalData("root");
            var a_level = new HierarchicalData("a_level");
            var a_leaf = new HierarchicalData("a_leaf", double.NaN);
            root.AddChild(a_level);
            a_leaf.AddChild(a_leaf);

            // Nothing left but the root node!
            ClassicAssert.Throws(typeof(Exception), () => root.RemoveLeafNodesWithoutArea());

            // Assert
            Assert.That(root.Name, Is.EqualTo("root"));
            Assert.That(root.Children.Count, Is.EqualTo(0));
            ClassicAssert.IsTrue(double.IsNaN(root.AreaMetric));
        }

        [Test]
        public void RemoveLeafNodesWithoutArea_WorksRecursively()
        {
            // Arrange
            var root = new HierarchicalData("root");
            var a_leaf = new HierarchicalData("a_leaf", 1);
            var b_empty = new HierarchicalData("b");
            var c_level = new HierarchicalData("c");
            var c_leaf = new HierarchicalData("c_leaf", double.NaN);
            root.AddChild(a_leaf);
            root.AddChild(b_empty);
            root.AddChild(c_level);
            c_level.AddChild(c_leaf);

            root.RemoveLeafNodesWithoutArea();

            // Assert
            // After c_leaf was deleted, c_level is deleted too.
            Assert.That(root.CountLeafNodes(), Is.EqualTo(1));
            Assert.That(root.Name, Is.EqualTo("root"));
            Assert.That(root.Children.Count, Is.EqualTo(1));
            Assert.That(root.Children.First().Name, Is.EqualTo("a_leaf"));
        }

        [Test]
        public void Shrink_StopsOnMultipleChildren()
        {
            var a = new HierarchicalData("a");
            var b = new HierarchicalData("b");
            var c = new HierarchicalData("c");
            var d = new HierarchicalData("d");

            a.AddChild(b);
            b.AddChild(c);
            b.AddChild(d);

            // b has two children

            var data = a.Shrink();
            Assert.That(data.Name, Is.EqualTo("b"));
        }

        [Test]
        public void Shrink_StopsAtLeafNode()
        {
            var a = new HierarchicalData("a");
            var b = new HierarchicalData("b");
            var c = new HierarchicalData("c");

            a.AddChild(b);
            b.AddChild(c);

            // c is leaf node

            var data = a.Shrink();
            Assert.That(data.Name, Is.EqualTo("c"));
        }
    }
}