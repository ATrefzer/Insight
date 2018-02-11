using Insight.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insight.Shared.Model
{
    public class TrendData
    {
        public DateTime Date { get; set; }

        public InvertedSpace InvertedSpace { get; set; }

        public LinesOfCode Loc { get; set; }
    }
}
