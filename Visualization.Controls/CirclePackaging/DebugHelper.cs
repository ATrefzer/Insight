using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

using Visualization.Controls.Data;

namespace Visualization.Controls.CirclePackaging
{
    public static class DebugHelper
    {
        /// <summary>
        /// Allows to set breakpoints in a certain processing step. Counted across all levels.
        /// </summary>
        private static int _dbg;

        public static void BreakOn(int dbg)
        {
            if (_dbg == dbg)
            {
                Debugger.Break();
            }
        }

        public static void DeleteDebugOutput()
        {
            var files = Directory.GetFiles("d:\\", "*circle*.html");
            foreach (var file in files)
            {
                File.Delete(file);
            }
        }

        public static void DumpChildrenWeights(List<HierarchicalData> children)
        {
            var builder = new StringBuilder();
            foreach (var item in children)
            {
                var layout = item.Layout as CircularLayoutInfo;
                var radius = RadiusToWeight(layout.Radius).ToString(CultureInfo.InvariantCulture);
                var line = $"root.AddChild(new HierarchicalData(\"ra\", {radius}));";
                builder.AppendLine(line);
            }

            builder.AppendLine();
            File.WriteAllText("d:\\weights.txt", builder.ToString());
        }

        public static void IncDbgCounter()
        {
            _dbg++;
        }

        public static void ResetDbgCounter()
        {
            _dbg = 0;
        }

        public static void WriteDebugOutput(FrontChain frontChain, string extra, params CircularLayoutInfo[] proposedSolutions)
        {
            var id = 1;
            var d = frontChain.ToDesmos();
            foreach (var circle in proposedSolutions)
            {
                d.Add("solution" + id++, circle.ToString());
            }

            if (extra == null)
            {
                d.Write($"d:\\circles_{_dbg:D3}.html");
            }
            else
            {
                d.Write($"d:\\circles_{_dbg:D3}_{extra}.html");
            }
        }


        private static double RadiusToWeight(double radius)
        {
            return radius * radius * 2 * Math.PI;
        }
    }
}