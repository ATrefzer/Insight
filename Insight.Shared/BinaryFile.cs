using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Insight.Shared
{
    public sealed class BinaryFile<T>
    {
        public T Read(string filePath)
        {
            T deserialized;
            var formatter = new BinaryFormatter();
            using (var stream = File.Open(filePath, FileMode.Open))
            {
                deserialized = (T) formatter.Deserialize(stream);
            }

            return deserialized;
        }

        public void Write(string filePath, T obj)
        {
            var formatter = new BinaryFormatter();
            using (var stream = File.Create(filePath))
            {
                formatter.Serialize(stream, obj);
            }
        }
    }
}