using System;

using Visualization.Controls;

namespace Insight.ViewModels
{
    internal interface ISearchableViewModel
    {
        Predicate<object> CreateFilter(string text);
    }
}