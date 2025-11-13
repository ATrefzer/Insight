using NUnit.Framework;
using NUnit.Framework.Legacy;
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

            Assert.That(layout, Is.EqualTo(fc.Head.Next.Value));
            Assert.That(layout, Is.EqualTo(fc.Head.Previous.Value));
        }


        [Test]
        public void AddingSecondNode()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            var fc = new FrontChain();
            var node1 = fc.Add(layout1);
            var node2 = fc.Add(layout2);

            Assert.That(node1.Value.Radius, Is.EqualTo(1.0));
            Assert.That(node2.Value.Radius, Is.EqualTo(2.0));

            Assert.That(layout2, Is.EqualTo(fc.Head.Next.Value));
            Assert.That(layout2, Is.EqualTo(fc.Head.Previous.Value));

            var layout2Node = fc.Head.Next;

            Assert.That(layout1, Is.EqualTo(layout2Node.Next.Value));
            Assert.That(layout1, Is.EqualTo(layout2Node.Previous.Value));
        }


        [Test]
        public void Count()
        {
            var fc = new FrontChain();
            Assert.That(fc.Count(), Is.EqualTo(0));

            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            fc.Add(layout1);
            Assert.That(fc.Count(), Is.EqualTo(1));

            var layout2 = new CircularLayoutInfo { Radius = 2.0 };
            fc.Add(layout2);
            Assert.That(fc.Count(), Is.EqualTo(2));
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
            Assert.That(fc.Head, Is.EqualTo(node2));
            Assert.That(fc.Head.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Next.Next.Value.Radius, Is.EqualTo(2.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Previous.Previous.Value.Radius, Is.EqualTo(2.0));
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

            Assert.That(fc.Head, Is.EqualTo(node1));
            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Next.Next.Value.Radius, Is.EqualTo(1.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Previous.Previous.Value.Radius, Is.EqualTo(1.0));
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
            Assert.That(fc.Head, Is.EqualTo(node2));
            Assert.That(fc.Head.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Next.Next.Value.Radius, Is.EqualTo(2.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Previous.Previous.Value.Radius, Is.EqualTo(2.0));

            fc.Delete(fc.Head); // Single node
            Assert.That(fc.Head, Is.EqualTo(node3));
            Assert.That(fc.Head.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(3.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(3.0));

            fc.Delete(fc.Head);
            Assert.That(fc.Head, Is.Null);
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
            Assert.That(fc.Head, Is.EqualTo(node1));
            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Next.Next.Value.Radius, Is.EqualTo(1.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Previous.Previous.Value.Radius, Is.EqualTo(1.0));
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
            Assert.That(fc.Head, Is.EqualTo(node1));
            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(2));
            Assert.That(fc.Head.Next.Next.Value.Radius, Is.EqualTo(1.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Previous.Previous.Value.Radius, Is.EqualTo(1.0));

            fc.Delete(fc.Head.Previous); // Single node
            Assert.That(fc.Head, Is.EqualTo(node1));
            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(1.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(1.0));

            fc.Delete(fc.Head.Previous);
            Assert.That(fc.Head, Is.EqualTo(null));
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

            Assert.That(fc.Find(node => node.Value.Radius.Equals(1.0)), Is.EqualTo(node1));
            Assert.That(fc.Find(node => node.Value.Radius.Equals(2.0)), Is.EqualTo(node2));
            Assert.That(fc.Find(node => node.Value.Radius.Equals(3.0)), Is.EqualTo(node3));
            Assert.That(fc.Find(node => node.Value.Radius.Equals(4.0)), Is.EqualTo(node4));
            Assert.That(fc.Find(node => node.Value.Radius.Equals(5.0)), Is.Null);
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

            Assert.That(fc.FindMinValue(node => node.Value.Radius), Is.EqualTo(node1));
        }


        [Test]
        public void HeadOnly_InsertAfter()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };
            var layout2 = new CircularLayoutInfo { Radius = 2.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);

            var node2 = fc.InsertAfter(node1, layout2);
            Assert.That(node2.Value.Radius, Is.EqualTo(2.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Next.Next.Value.Radius, Is.EqualTo(1.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Previous.Previous.Value.Radius, Is.EqualTo(1.0));
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

            Assert.That(fc.IndexOf(node1), Is.EqualTo(0));
            Assert.That(fc.IndexOf(node2), Is.EqualTo(1));
            Assert.That(fc.IndexOf(node3), Is.EqualTo(2));
            Assert.That(fc.IndexOf(node4), Is.EqualTo(3));
        }

        [Test]
        public void IndexOf_OnlyHead()
        {
            var layout1 = new CircularLayoutInfo { Radius = 1.0 };

            var fc = new FrontChain();
            var node1 = fc.Add(layout1);

            Assert.That(fc.IndexOf(node1), Is.EqualTo(0));
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
            Assert.That(node2.Value.Radius, Is.EqualTo(2.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Next.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Next.Next.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Next.Next.Next.Value.Radius, Is.EqualTo(1.0));

            Assert.That(fc.Head.Value.Radius, Is.EqualTo(1.0));
            Assert.That(fc.Head.Previous.Value.Radius, Is.EqualTo(3.0));
            Assert.That(fc.Head.Previous.Previous.Value.Radius, Is.EqualTo(2.0));
            Assert.That(fc.Head.Previous.Previous.Previous.Value.Radius, Is.EqualTo(1.0));
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

            ClassicAssert.IsFalse(fc.IsAfter(node1, node1));
            ClassicAssert.IsTrue(fc.IsAfter(node1, node2));
            ClassicAssert.IsTrue(fc.IsAfter(node1, node3));
            ClassicAssert.IsFalse(fc.IsAfter(node1, node4));

            ClassicAssert.IsFalse(fc.IsAfter(node4, node4));
            ClassicAssert.IsTrue(fc.IsAfter(node4, node1));
            ClassicAssert.IsTrue(fc.IsAfter(node4, node2));
            ClassicAssert.IsFalse(fc.IsAfter(node4, node3));

            ClassicAssert.IsFalse(fc.IsAfter(node4, node3));
            ClassicAssert.IsTrue(fc.IsAfter(node3, node4));
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

            Assert.That(fc.ToList().Count, Is.EqualTo(4));
        }
    }
}