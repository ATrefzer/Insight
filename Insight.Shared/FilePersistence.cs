namespace Insight.Shared;

public sealed class FilePersistence<T>
{
    public T Read(string filePath)
    {
        return new JsonFile<T>().Read(filePath);
    }

    public void Write(string filePath, T obj)
    {
        var writer = new JsonFile<T>();
        writer.Write(filePath, obj);
    }
}