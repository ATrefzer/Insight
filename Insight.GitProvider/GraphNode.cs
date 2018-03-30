using System.Collections.Generic;

using Insight.Shared.Model;

namespace Insight.GitProvider
{
    sealed class GraphNode
    {
        public string Commit { get; set; }
        public List<string> Parents { get; set; }
    }
}