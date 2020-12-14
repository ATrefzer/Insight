using System.Collections.Generic;

using Insight.Shared;

namespace Insight.Alias
{
    internal sealed class NullAliasMapping : IAliasMapping
    {
        public string GetAlias(string name)
        {
            return name;
        }

        public IEnumerable<string> GetReverse(string alias)
        {
            return new List<string>{alias};
        }
    }
}