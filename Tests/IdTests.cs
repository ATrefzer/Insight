using System.Collections.Generic;

using Insight.Shared.Model;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests
{
    [TestFixture]
    internal sealed class IdTests
    {
        [Test]
        public void Equality_Mixed()
        {
            Id n1 = new NumberId(1);
            Id s1 = new StringId("1");

            // Mixed
            Assert.That(n1.Equals(s1), Is.False);
        }

        [Test]
        public void Equality_Number()
        {
            Id n1 = new NumberId(1);
            Id n11 = new NumberId(1);
            Id n2 = new NumberId(2);

            // Number
            ClassicAssert.IsTrue(n1.Equals(n11));
            ClassicAssert.IsTrue(n11.Equals(n1));
            Assert.That(n1.Equals(n2), Is.False);
            Assert.That(n2.Equals(n1), Is.False);
            Assert.That(n2.Equals(null), Is.False);

            ClassicAssert.IsTrue(n1 == n11);
            ClassicAssert.IsTrue(n11 == n1);
            Assert.That(n1 == n2, Is.False);
            Assert.That(n2 == n1, Is.False);
            Assert.That(n2 == null, Is.False);
        }

        [Test]
        public void Equality_String()
        {
            Id s1 = new StringId("1");
            Id s11 = new StringId("1");
            Id s2 = new StringId("2");

            // String
            ClassicAssert.IsTrue(s1.Equals(s11));
            ClassicAssert.IsTrue(s11.Equals(s1));
            Assert.That(s1.Equals(s2), Is.False);
            Assert.That(s2.Equals(s1), Is.False);
            Assert.That(s2.Equals(null), Is.False);

            ClassicAssert.IsTrue(s1 == s11);
            ClassicAssert.IsTrue(s11 == s1);
            Assert.That(s1 == s2, Is.False);
            Assert.That(s2 == s1, Is.False);
        }

        [Test]
        public void NumberId_CanBeUsedAsKey()
        {
            Id id1 = new NumberId(1);
            Id id2 = new NumberId(1);

            var hash = new HashSet<Id> { id1, id2 };

            Assert.That(hash.Count, Is.EqualTo(1));

            Id id3 = new NumberId(2);
            hash.Add(id3);

            Assert.That(hash.Count, Is.EqualTo(2));
        }

        [Test]
        public void StringId_CanBeUsedAsKey()
        {
            Id id1 = new StringId("a");
            Id id2 = new StringId("a");

            var hash = new HashSet<Id> { id1, id2 };

            Assert.That(hash.Count, Is.EqualTo(1));

            Id id3 = new StringId("b");
            hash.Add(id3);

            Assert.That(hash.Count, Is.EqualTo(2));
        }

      
    }
}