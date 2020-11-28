using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public void Enumerate_Breadth_First()
        {
            /*
           n9 <---
           |      |
           n8     |
           |      n10
           n7     |
           |      |
           n6     |
           |      |
           n5     |
           |      |
      n3-> n4 ----
      |    |
      n1   n2
      --  --
         |
        n0
            */

            var graph = CreateTestData1();

            var builder = new StringBuilder();
            foreach (var node in graph)
            {
                builder.Append(node.CommitHash);
            }

            // All nodes in breadth first
            Assert.AreEqual("n0n1n2n3n4n5n10n6n9n7n8n", builder.ToString());
        }

        [Test]
        public void FindCommonAncestor_Regression()
        {
            var graph = CreateTestData1();


            Assert.AreEqual("n4", graph.FindCommonAncestor("n10", "n8"));
        }

        private static Graph CreateTestData1()
        {
            /*
            n9 <---
            |      |
            n8     |
            |      n10
            n7     |
            |      |
            n6     |
            |      |
            n5     |
            |      |
       n3-> n4 ----
       |    |
       n1   n2
       --  --
          |
         n0
             */

            var graph = new Graph();
            graph.UpdateGraph("n1", "n0");
            graph.UpdateGraph("n2", "n0");
            graph.UpdateGraph("n3", "n1");

            graph.UpdateGraph("n4", "n2 n3");
            graph.UpdateGraph("n5", "n4");
            graph.UpdateGraph("n6", "n5");
            graph.UpdateGraph("n7", "n6");
            graph.UpdateGraph("n8", "n7");

            graph.UpdateGraph("n9", "n8 n10");

            graph.UpdateGraph("n10", "n4");

          
      
          
          
            return graph;
        }


        [Test]
        public void FindCommonAncestor()
        {
            /*
            n9 <---
            |      |
            |      |
            n8     |      
            |      n6 --> n7
            |      |
       n3-> n4-->  n5       
       |    |
       n1   n2
          |   
          n0    
             */
            
            var graph = new Graph();
            graph.UpdateGraph("n9", "n8 n6");
            graph.UpdateGraph("n8", "n4");
            graph.UpdateGraph("n7", "n6");
            graph.UpdateGraph("n6", "n5");
            graph.UpdateGraph("n5", "n4");
            graph.UpdateGraph("n4", "n2 n3");
            graph.UpdateGraph("n3", "n1");
            graph.UpdateGraph("n2", "n0");
            graph.UpdateGraph("n1", "n0");

            Assert.AreEqual("n1", graph.FindCommonAncestor("n3", "n1"));
            //graph._preprocessData.EulerPath.ForEach(node => Debug.Write(node.CommitHash + ","));

            
            Assert.AreEqual("n1", graph.FindCommonAncestor("n1", "n3"));
            Assert.AreEqual("n6", graph.FindCommonAncestor("n9", "n7"));
            Assert.AreEqual("n1", graph.FindCommonAncestor("n1", "n1"));
            //Assert.AreEqual("n8", graph.FindCommonAncestor("n9", "n8"));
        }
        
        
        
        
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
            graph.UpdateGraph("n1", "");

            Assert.AreEqual(0, graph.GetNode("n1").Parents.Count);
            Assert.AreEqual(2, graph.GetNode("n1").Children.Count);

            Assert.AreEqual(1, graph.GetNode("n2").Parents.Count);
            Assert.AreEqual(1, graph.GetNode("n2").Children.Count);

            Assert.AreEqual(1, graph.GetNode("n3").Parents.Count);
            Assert.AreEqual(1, graph.GetNode("n3").Children.Count);

            Assert.AreEqual(2, graph.GetNode("n4").Parents.Count);
            Assert.AreEqual(0, graph.GetNode("n4").Children.Count);

        }
    }
}
