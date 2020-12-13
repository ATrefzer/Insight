using System.Collections.Generic;

namespace Insight.Shared
{
    public interface IAliasMapping
    {
        string GetAlias(string name);
        IEnumerable<string> GetReverse(string alias);
    }
}