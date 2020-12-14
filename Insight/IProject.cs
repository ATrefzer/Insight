using Insight.Shared;

namespace Insight
{
    public interface IProject
    {
        string Cache { get; }
        IFilter DisplayFilter { get; set; }
        string SourceControlDirectory { get; set; }

        bool IsDefault { get; }
        bool IsValid();
        void Load(string path);
        ISourceControlProvider CreateProvider();
    }
}