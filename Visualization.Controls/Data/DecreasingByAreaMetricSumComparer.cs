using System;
using System.Collections.Generic;

namespace Visualization.Controls.Data
{
    public sealed partial class HierarchicalData
    {
        public sealed class DecreasingByAreaMetricSumComparer : IComparer<HierarchicalData>
        {
            /// <summary>
            /// Sorts collection of hierarchical data in decreasing order of the area metric
            /// </summary>
            public int Compare(HierarchicalData x, HierarchicalData y)
            {
                if (x == null)
                {
                    throw new ArgumentNullException(nameof(x));
                }

                if (y == null)
                {
                    throw new ArgumentNullException(nameof(y));
                }

                if (x.AreaMetricSum < y.AreaMetricSum)
                {
                    return 1;
                }

                if (x.AreaMetricSum > y.AreaMetricSum)
                {
                    return -1;
                }

                return 0;
            }
        }
    }
}