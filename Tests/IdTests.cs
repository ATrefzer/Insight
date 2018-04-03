using System.Collections.Generic;

using Insight.Shared.Model;

using NUnit.Framework;

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
            Assert.IsFalse(n1.Equals(s1));
        }

        [Test]
        public void Equality_Number()
        {
            Id n1 = new NumberId(1);
            Id n11 = new NumberId(1);
            Id n2 = new NumberId(2);

            // Number
            Assert.IsTrue(n1.Equals(n11));
            Assert.IsTrue(n11.Equals(n1));
            Assert.IsFalse(n1.Equals(n2));
            Assert.IsFalse(n2.Equals(n1));
            Assert.IsFalse(n2.Equals(null));

            Assert.IsTrue(n1 == n11);
            Assert.IsTrue(n11 == n1);
            Assert.IsFalse(n1 == n2);
            Assert.IsFalse(n2 == n1);
            Assert.IsFalse(n2 == null);
        }

        [Test]
        public void Equality_String()
        {
            Id s1 = new StringId("1");
            Id s11 = new StringId("1");
            Id s2 = new StringId("2");

            // String
            Assert.IsTrue(s1.Equals(s11));
            Assert.IsTrue(s11.Equals(s1));
            Assert.IsFalse(s1.Equals(s2));
            Assert.IsFalse(s2.Equals(s1));
            Assert.IsFalse(s2.Equals(null));

            Assert.IsTrue(s1 == s11);
            Assert.IsTrue(s11 == s1);
            Assert.IsFalse(s1 == s2);
            Assert.IsFalse(s2 == s1);
        }

        [Test]
        public void NumberId_CanBeUsedAsKey()
        {
            Id id1 = new NumberId(1);
            Id id2 = new NumberId(1);

            var hash = new HashSet<Id>();
            hash.Add(id1);
            hash.Add(id2);

            Assert.AreEqual(1, hash.Count);

            Id id3 = new NumberId(2);
            hash.Add(id3);

            Assert.AreEqual(2, hash.Count);
        }

        [Test]
        public void StringId_CanBeUsedAsKey()
        {
            Id id1 = new StringId("a");
            Id id2 = new StringId("a");

            var hash = new HashSet<Id>();
            hash.Add(id1);
            hash.Add(id2);

            Assert.AreEqual(1, hash.Count);

            Id id3 = new StringId("b");
            hash.Add(id3);

            Assert.AreEqual(2, hash.Count);
        }

      
    }
}