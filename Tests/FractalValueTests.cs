using System;
using System.Collections.Generic;

using Insight.Shared.Calculation;

using NUnit.Framework;

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
            Assert.AreEqual(0.0, value);

            // 0.5
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 100 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.AreEqual(0.5, value);

            // 0.44
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 50 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.AreEqual(0.44, Math.Round(value,2));

            // 0.67
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 100 }, { "C", 100 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.AreEqual(0.67, Math.Round(value, 2));

            // 0.625
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 50 }, { "C", 50 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.AreEqual(0.625, Math.Round(value, 3));

            // 0.75
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 100 }, { "C", 100 }, { "D", 100 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.AreEqual(0.75, Math.Round(value, 2));

            // 0.057
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 1 }, { "C", 1 }, { "D", 1 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.AreEqual(0.057, Math.Round(value, 3));

            // 0.46
            developerToContribution = new Dictionary<string, uint> { { "A", 100 }, { "B", 50 }, { "C", 1 }, { "D", 1 } };
            value = FractalValue.Calculate(developerToContribution);
            Assert.AreEqual(0.46, Math.Round(value, 2));
        }
    }
}