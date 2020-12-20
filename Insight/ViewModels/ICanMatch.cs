using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insight.ViewModels
{
    interface ICanMatch
    {
        bool IsMatch(string lowerCaseSearchText);
    }
}
