using NUnit.Framework;

using Visualization.Controls.CirclePacking;

namespace Tests
{
    [TestFixture]
    internal sealed class FrontChainTests
    {
        [Test]
        public void AddingHead()
        {
            var layout = new CircularLayoutInfo { Radius = 1.0 };
            var fc = new FrontChain();
            fc.Add(layout);

            Assert.AreEqual(fc.Head.Next.Value, layout);
            Assert.AreEqual(fc.Head.Previous.Value, layout);
        }


        [Test]
        public void AddingSecondNode()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);

            Assert.AreEqual(node1.Value.Radius, 1.0);
            Assert.AreEqual(node2.Value.Radius, 2.0);

            Assert.AreEqual(fc.Head.Next.Value, layout2);
            Assert.AreEqual(fc.Head.Previous.Value, layout2);

            var layout2Node = fc.Head.Next;

            Assert.AreEqual(layout2Node.Next.Value, layout1);
            Assert.AreEqual(layout2Node.Previous.Value, layout1);
        }


        [Test]
        public void Count()
        {
            var fc = new FrontChain();
            Assert.AreEqual(0, fc.Count());

            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            fc.Add(layout1);
            Assert.AreEqual(1, fc.Count());

            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            fc.Add(layout2);
            Assert.AreEqual(2, fc.Count());
        }


        [Test]
        public void DeleteBetween_HeadIsRemoved()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };
            var layout4 = new CircularLayoutInfo { Radius = 4.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);
            var node4 = fc.Add(layout4);

            fc.Delete(node3, node2);

            // Set to end of range
            Assert.AreEqual(node2, fc.Head);
            Assert.AreEqual(2.0, fc.Head.Value.Radius);
            Assert.AreEqual(3, 0, fc.Head.Next.Value.Radius);
            Assert.AreEqual(2.0, fc.Head.Next.Next.Value.Radius);

            Assert.AreEqual(2.0, fc.Head.Value.Radius);
            Assert.AreEqual(3.0, fc.Head.Previous.Value.Radius);
            Assert.AreEqual(2.0, fc.Head.Previous.Previous.Value.Radius);
        }

        [Test]
        public void DeleteBetween_Single()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);

            fc.Delete(node1, node3);

            Assert.AreEqual(node1, fc.Head);
            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(3, 0, fc.Head.Next.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Next.Next.Value.Radius);

            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(3.0, fc.Head.Previous.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Previous.Previous.Value.Radius);
        }


        [Test]
        public void DeleteHead()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);

            fc.Delete(fc.Head);
            Assert.AreEqual(node2, fc.Head);
            Assert.AreEqual(2.0, fc.Head.Value.Radius);
            Assert.AreEqual(3, 0, fc.Head.Next.Value.Radius);
            Assert.AreEqual(2.0, fc.Head.Next.Next.Value.Radius);

            Assert.AreEqual(2.0, fc.Head.Value.Radius);
            Assert.AreEqual(3.0, fc.Head.Previous.Value.Radius);
            Assert.AreEqual(2.0, fc.Head.Previous.Previous.Value.Radius);

            fc.Delete(fc.Head); // Single node
            Assert.AreEqual(node3, fc.Head);
            Assert.AreEqual(3.0, fc.Head.Value.Radius);
            Assert.AreEqual(3, 0, fc.Head.Next.Value.Radius);

            Assert.AreEqual(3.0, fc.Head.Value.Radius);
            Assert.AreEqual(3.0, fc.Head.Previous.Value.Radius);

            fc.Delete(fc.Head);
            Assert.AreEqual(null, fc.Head);
        }


        [Test]
        public void DeleteMiddle()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);

            fc.Delete(node2);
            Assert.AreEqual(node1, fc.Head);
            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(3, 0, fc.Head.Next.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Next.Next.Value.Radius);

            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(3.0, fc.Head.Previous.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Previous.Previous.Value.Radius);
        }

        [Test]
        public void DeleteTail()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);

            fc.Delete(fc.Head.Previous);
            Assert.AreEqual(node1, fc.Head);
            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(2, 0, fc.Head.Next.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Next.Next.Value.Radius);

            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(2.0, fc.Head.Previous.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Previous.Previous.Value.Radius);

            fc.Delete(fc.Head.Previous); // Single node
            Assert.AreEqual(node1, fc.Head);
            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(1, 0, fc.Head.Next.Value.Radius);

            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Previous.Value.Radius);

            fc.Delete(fc.Head.Previous);
            Assert.AreEqual(null, fc.Head);
        }

        [Test]
        public void Find()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };
            var layout4 = new CircularLayoutInfo { Radius = 4.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);
            var node4 = fc.Add(layout4);

            Assert.AreEqual(node1, fc.Find(node => node.Value.Radius.Equals(1.0)));
            Assert.AreEqual(node2, fc.Find(node => node.Value.Radius.Equals(2.0)));
            Assert.AreEqual(node3, fc.Find(node => node.Value.Radius.Equals(3.0)));
            Assert.AreEqual(node4, fc.Find(node => node.Value.Radius.Equals(4.0)));
            Assert.IsNull(fc.Find(node => node.Value.Radius.Equals(5.0)));
        }


        [Test]
        public void FindMinValue()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };
            var layout4 = new CircularLayoutInfo { Radius = 4.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            fc.Add(layout2);
            fc.Add(layout3);
            fc.Add(layout4);

            Assert.AreEqual(node1, fc.FindMinValue(node => node.Value.Radius));
        }


        [Test]
        public void HeadOnly_InsertAfter()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);

            var node2 = fc.InsertAfter(node1, layout2);
            Assert.AreEqual(node2.Value.Radius, 2.0);

            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(2, 0, fc.Head.Next.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Next.Next.Value.Radius);

            Assert.AreEqual(1.0, fc.Head.Value.Radius);
            Assert.AreEqual(2.0, fc.Head.Previous.Value.Radius);
            Assert.AreEqual(1.0, fc.Head.Previous.Previous.Value.Radius);
        }

        [Test]
        public void IndexOf()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };
            var layout4 = new CircularLayoutInfo { Radius = 4.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);
            var node4 = fc.Add(layout4);

            Assert.AreEqual(0, fc.IndexOf(node1));
            Assert.AreEqual(1, fc.IndexOf(node2));
            Assert.AreEqual(2, fc.IndexOf(node3));
            Assert.AreEqual(3, fc.IndexOf(node4));
        }

        [Test]
        public void IndexOf_OnlyHead()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);

            Assert.AreEqual(0, fc.IndexOf(node1));
        }

        [Test]
        public void InsertAfter()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node3 = fc.Add(layout3);

            var node2 = fc.InsertAfter(node1, layout2);
            Assert.AreEqual(node2.Value.Radius, 2.0);

            Assert.AreEqual(fc.Head.Value.Radius, 1.0);
            Assert.AreEqual(fc.Head.Next.Value.Radius, 2.0);
            Assert.AreEqual(fc.Head.Next.Next.Value.Radius, 3.0);
            Assert.AreEqual(fc.Head.Next.Next.Next.Value.Radius, 1.0);

            Assert.AreEqual(fc.Head.Value.Radius, 1.0);
            Assert.AreEqual(fc.Head.Previous.Value.Radius, 3.0);
            Assert.AreEqual(fc.Head.Previous.Previous.Value.Radius, 2.0);
            Assert.AreEqual(fc.Head.Previous.Previous.Previous.Value.Radius, 1.0);
        }

        [Test]
        public void IsAfter()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };
            var layout4 = new CircularLayoutInfo { Radius = 4.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);
            var node3 = fc.Add(layout3);
            var node4 = fc.Add(layout4);

            Assert.IsFalse(fc.IsAfter(node1, node1));
            Assert.IsTrue(fc.IsAfter(node1, node2));
            Assert.IsTrue(fc.IsAfter(node1, node3));
            Assert.IsFalse(fc.IsAfter(node1, node4));

            Assert.IsFalse(fc.IsAfter(node4, node4));
            Assert.IsTrue(fc.IsAfter(node4, node1));
            Assert.IsTrue(fc.IsAfter(node4, node2));
            Assert.IsFalse(fc.IsAfter(node4, node3));

            Assert.IsFalse(fc.IsAfter(node4, node3));
            Assert.IsTrue(fc.IsAfter(node3, node4));
        }


        [Test]
        public void ToList()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var layout3 = new CircularLayoutInfo { Radius = 3.0 };
            var layout4 = new CircularLayoutInfo { Radius = 4.0 };

            var fc = new FrontChain();
            fc.Add(layout1);
            fc.Add(layout2);
            fc.Add(layout3);
            fc.Add(layout4);

            Assert.AreEqual(4, fc.ToList().Count);
        }
    }
}