using System.Text;

using Insight.GitProvider;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests
{
    [TestFixture]
    internal class GraphTests
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
            Assert.That(builder.ToString(), Is.EqualTo("n0n1n2n3n4n5n10n6n9n7n8"));
        }

        [Test]
        public void FindCommonAncestor_Regression()
        {
            var graph = CreateTestData1();


            Assert.That(graph.FindCommonAncestor("n10", "n8"), Is.EqualTo("n4"));
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
            |      |    n7
            n8     |    |  
            |      n6 --- 
            |      |
       n3-> n4 --> n5       
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

            Assert.That(graph.FindCommonAncestor("n3", "n1"), Is.EqualTo("n1"));
            //graph._preprocessData.EulerPath.ForEach(node => Debug.Write(node.CommitHash + ","));


            Assert.That(graph.FindCommonAncestor("n1", "n3"), Is.EqualTo("n1"));
            Assert.That(graph.FindCommonAncestor("n9", "n7"), Is.EqualTo("n4"));
            Assert.That(graph.FindCommonAncestor("n1", "n1"), Is.EqualTo("n1"));
            //ClassicAssert.AreEqual("n8", graph.FindCommonAncestor("n9", "n8"));
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

            Assert.That(graph.GetNode("n1").Parents.Count, Is.EqualTo(0));
            Assert.That(graph.GetNode("n1").Children.Count, Is.EqualTo(2));

            Assert.That(graph.GetNode("n2").Parents.Count, Is.EqualTo(1));
            Assert.That(graph.GetNode("n2").Children.Count, Is.EqualTo(1));

            Assert.That(graph.GetNode("n3").Parents.Count, Is.EqualTo(1));
            Assert.That(graph.GetNode("n3").Children.Count, Is.EqualTo(1));

            Assert.That(graph.GetNode("n4").Parents.Count, Is.EqualTo(2));
            Assert.That(graph.GetNode("n4").Children.Count, Is.EqualTo(0));

        }
    }
}
