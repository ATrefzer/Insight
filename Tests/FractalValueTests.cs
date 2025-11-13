using System;
using System.Collections.Generic;

using Insight.Shared.Calculation;

using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Tests
{
    [TestFixture]
    internal sealed class FractalValueTests
    {
        [Test]
        public void UnderstandingTheFormula()
        {
            Dictionary<string, uint> developerToContribution;
            double value;

            // The higher the number the more "fractalized"
            // The more developers the higher the number gets.
            // However: Some (very) small contributions make the number smaller.

            // 0
            developerToContribution = new Dictionary<string, uint> { { "A", 100 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(value, Is.EqualTo(0.0));

            // 0.5
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 100 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(value, Is.EqualTo(0.5));

            // 0.44
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 50 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(Math.Round(value, 2), Is.EqualTo(0.44));

            // 0.67
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 100 }, { "C", 100 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(Math.Round(value, 2), Is.EqualTo(0.67));

            // 0.625
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 50 }, { "C", 50 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(Math.Round(value, 3), Is.EqualTo(0.625));

            // 0.75
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 100 }, { "C", 100 }, { "D", 100 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(Math.Round(value, 2), Is.EqualTo(0.75));

            // 0.057
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 1 }, { "C", 1 }, { "D", 1 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(Math.Round(value, 3), Is.EqualTo(0.057));

            // 0.46
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 50 }, { "C", 1 }, { "D", 1 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.That(Math.Round(value, 2), Is.EqualTo(0.46));
        }
    }
}