using Insight.Shared;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework.Legacy;
using Visualization.Controls.CirclePacking;
using Visualization.Controls.Data;

namespace Tests
{
    [TestFixture]
    public class CirclePackingLayoutTests
    {
        private string _resourceDirectory;

        /// <summary>
        /// Setting this flag to true allows to generate new test reference data.
        /// I don't want to commit the large binary files. So store the somewhere else.
        /// </summary>
        private const bool GenerateReferenceData = false;

        [Test]
        public void SingleRootNodeWithoutArea()
        {
            var data = new HierarchicalData("", 0.0);

            var layout = new CirclePackingLayout();

            // Disable assertions
            var backup = new TraceListener[Trace.Listeners.Count];
            Trace.Listeners.CopyTo(backup, 0);
            Trace.Listeners.Clear();

            layout.Layout(data, 100, 100);

            // Restore assertions
            Trace.Listeners.AddRange(backup);

            Assert.That(data.Layout.ToString(), Is.EqualTo("(x-0)^2+(y-0)^2=0^2"));
        }


        private void GenerateTestHierarchicalReferenceFile(string path, double width, double height)
        {
            // Generate reference hierarchical data with layout info
            var generator = new HierarchicalDataBuilder();
            var root = generator.GenerateRandomHierarchy();
            var layout = new CirclePackingLayout();
            layout.Layout(root, width, height);
            var file = new FilePersistence<HierarchicalData>();
            file.Write(path, root);
        }

        [Ignore("Don't want to store the large binary test data in the repo. Generate new one via GenerateTestHierarchicalReferenceFile"), Test]
        public void ReferenceLayout_LargeHierarchicalExample1()
        {
            // Loads a HierarchicalData bin file that includes the layout information and checks
            // if the layouting still produces the same result.

            const double width = 100;
            const double height = 100;


            var path = Path.Combine(_resourceDirectory, "large_hierarchical_example_circle1.bin");

            if (GenerateReferenceData)
            {
#pragma warning disable CS0162 // Unreachable code detected
                GenerateTestHierarchicalReferenceFile(path, width, height);
#pragma warning restore CS0162 // Unreachable code detected
            }

            AssertLayouting(path, width, height);
        }

        private static void AssertLayouting(string path, double width, double height)
        {
            var layout = new CirclePackingLayout();
            var binFile = new FilePersistence<HierarchicalData>();
            var data = binFile.Read(path);

            // Includes layout info
            var reference = data.Dump();

            ClassicAssert.IsTrue(File.Exists(path));

            var clone = (HierarchicalData)data.Clone();
            // Clone does not copy the layout.
            layout.Layout(clone, width, height);
            var result = clone.Dump();

            Assert.That(result, Is.EqualTo(reference));
        }

        [Ignore("Don't want to store the large binary test data in the repo. Generate new one via GenerateTestHierarchicalReferenceFile"), Test]
        public void ReferenceLayout_LargeHierarchicalExample2()
        {
            // Loads a HierarchicalData bin file that includes the layout information and checks
            // if the layouting still produces the same result.

            double width = 300;
            double height = 100;
            var path = Path.Combine(_resourceDirectory, "large_hierarchical_example_circle2.bin");

            if (GenerateReferenceData)
            {
#pragma warning disable CS0162 // Unreachable code detected
                GenerateTestHierarchicalReferenceFile(path, width, height);
#pragma warning restore CS0162 // Unreachable code detected
            }

            AssertLayouting(path, width, height);
        }

        [Ignore("Don't want to store the large binary test data in the repo. Generate new one via GenerateTestHierarchicalReferenceFile"), Test]
        public void ReferenceLayout_LargeHierarchicalExample3()
        {
            // Loads a HierarchicalData bin file that includes the layout information and checks
            // if the layouting still produces the same result.

            const double width = 100;
            const double height = 300;

            var path = Path.Combine(_resourceDirectory, "large_hierarchical_example_circle3.bin");

            if (GenerateReferenceData)
            {
#pragma warning disable CS0162 // Unreachable code detected
                GenerateTestHierarchicalReferenceFile(path, width, height);
#pragma warning restore CS0162 // Unreachable code detected
            }

            AssertLayouting(path, width, height);
        }


        [SetUp]
        public void Setup()
        {
            var assembly = Assembly.GetAssembly(GetType());
            var directory = new FileInfo(assembly.Location).Directory?.FullName ?? ".";
            _resourceDirectory = Path.Combine(directory, @"..\Tests\Resources");
        }
    }
}
