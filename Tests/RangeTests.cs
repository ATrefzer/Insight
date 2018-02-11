using NUnit.Framework;

using Visualization.Controls.Utility;

namespace Tests
{
    [TestFixture]
    internal sealed class RangeTests
    {
        [Test]
        public void ValueHigherBorderIncluded()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.IsTrue(range.Contains(9.0));
        }

        [Test]
        public void ValueInside()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.IsTrue(range.Contains(6.0));
        }

        [Test]
        public void ValueLowerBorderIncluded()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.IsTrue(range.Contains(5.0));
        }

        [Test]
        public void ValueTooHigh()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.IsFalse(range.Contains(9.1));
        }

        [Test]
        public void ValueTooLow()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.IsFalse(range.Contains(4.9));
        }
    }
}