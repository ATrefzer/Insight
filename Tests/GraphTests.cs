using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Insight.GitProvider;
using Insight.Shared.Model;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    class GraphTests
    {
        [Test]
        public void ChildAndParentRelationships()
        {

            /*
            n4 <---
            |      |
            |      |
            n3     |
            |      n2
            |      |
            n1---->
            
             */


            // Graph - Note that the order of UpdateGraph calls does not matter!
            var graph = new Graph();
            graph.UpdateGraph("n3", "n1");
            graph.UpdateGraph("n4", "n3 n2");
            graph.UpdateGraph("n2", "n1");
            graph.UpdateGraph("n1", null);

            Assert.AreEqual(0, graph.GetNode("n1").ParentHashes.Count);
            Assert.AreEqual(2, graph.GetNode("n1").ChildHashes.Count);

            Assert.AreEqual(1, graph.GetNode("n2").ParentHashes.Count);
            Assert.AreEqual(1, graph.GetNode("n2").ChildHashes.Count);

            Assert.AreEqual(1, graph.GetNode("n3").ParentHashes.Count);
            Assert.AreEqual(1, graph.GetNode("n3").ChildHashes.Count);

            Assert.AreEqual(2, graph.GetNode("n4").ParentHashes.Count);
            Assert.AreEqual(0, graph.GetNode("n4").ChildHashes.Count);

        }
    }
}
