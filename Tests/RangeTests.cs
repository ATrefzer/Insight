using NUnit.Framework;
using NUnit.Framework.Legacy;
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
            Assert.That(range.Contains(9.0), Is.True);
        }

        [Test]
        public void ValueInside()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.That(range.Contains(6.0), Is.True);
        }

        [Test]
        public void ValueLowerBorderIncluded()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.That(range.Contains(5.0), Is.True);
        }

        [Test]
        public void ValueTooHigh()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.That(range.Contains(9.1), Is.False);
        }

        [Test]
        public void ValueTooLow()
        {
            var range = new Range<double>(5.0, 9.0);
            Assert.That(range.Contains(4.9), Is.False);
        }
    }
}