using System.Collections.Generic;
using System.Text.RegularExpressions;

using Insight.Shared.Model;

namespace Insight.Shared
{
    public sealed class WorkItemExtractor
    {
        /// For example [a-zA-Z]+[a-zA-Z0-9]+\-[0-9]+
        private readonly string _regEx;

        public WorkItemExtractor(string regEx)
        {
            _regEx = regEx;
        }

        public List<WorkItem> Extract(string text)
        {
            var workItems = new List<WorkItem>();
            if (string.IsNullOrEmpty(_regEx))
            {
                return workItems;
            }      

            var regex = new Regex(_regEx);
            var matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                var name = match.Value.ToUpper();
                var workItem = new WorkItem(new StringId(name));
                workItem.Title = name;
                workItems.Add(workItem);
            }

            return workItems;
        }
    }
}