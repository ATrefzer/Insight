using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Insight.Analyzers;

using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests
{
   

    [TestFixture]
    sealed class FractalValueTests
    {
        [Test]
        public void UnderstandingTheFomrula()
        {            
            var developerToContribution = new Dictionary<string, uint>();
            double value;
          
            // The higher the number the more "fractalized"
            // The more developers the higher the number gets.
            // However: Some (very) small contributions make the number smaller.


            // 0
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            value = FractalValue.Calculate(developerToContribution);

            // 0.5
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            developerToContribution.Add("B", 100);
            value = FractalValue.Calculate(developerToContribution);

            // 0.44
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            developerToContribution.Add("B", 50);
            value = FractalValue.Calculate(developerToContribution);

            // 0.67
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            developerToContribution.Add("B", 100);
            developerToContribution.Add("C", 100);
            value = FractalValue.Calculate(developerToContribution);

            // 0.625
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            developerToContribution.Add("B", 50);
            developerToContribution.Add("C", 50);
            value = FractalValue.Calculate(developerToContribution);

            // 0.75
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            developerToContribution.Add("B", 100);
            developerToContribution.Add("C", 100);
            developerToContribution.Add("D", 100);
            value = FractalValue.Calculate(developerToContribution);

            // 0.0057
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            developerToContribution.Add("B", 1);
            developerToContribution.Add("C", 1);
            developerToContribution.Add("D", 1);
            value = FractalValue.Calculate(developerToContribution);

            // 0.46
            developerToContribution = new Dictionary<string, uint>();
            developerToContribution.Add("A", 100);
            developerToContribution.Add("B", 50);
            developerToContribution.Add("C", 1);
            developerToContribution.Add("D", 1);
            value = FractalValue.Calculate(developerToContribution);
        }


    }
}
