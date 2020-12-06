using System;

namespace Insight.ViewModels
{
    internal interface ISearchableViewModel
    {
        Predicate<object> CreateFilter(string text);
    }
}