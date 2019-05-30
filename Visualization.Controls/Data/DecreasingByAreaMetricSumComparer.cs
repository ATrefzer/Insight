﻿using System;
using System.Collections.Generic;
using Visualization.Controls.Interfaces;

namespace Visualization.Controls.Data
{
    public sealed class DecreasingByAreaMetricSumComparer : IComparer<IHierarchicalData>
    {
        /// <summary>
        /// Sorts collection of hierarchical data in decreasing order of the area metric
        /// </summary>
        public int Compare(IHierarchicalData x, IHierarchicalData y)
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