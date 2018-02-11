namespace Insight.Shared
{
    public interface IFilter
    {
        bool IsAccepted(string path);
    }
}