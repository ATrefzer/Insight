using System.IO;
using System.Xml.Serialization;

namespace Insight.Shared
{
    public sealed class XmlFile<T>
    {
        public T Read(string filePath)
        {
            T deserialized;
            var formatter = new XmlSerializer(typeof(T));
            using (var stream = File.Open(filePath, FileMode.Open))
            {
                deserialized = (T) formatter.Deserialize(stream);
            }

            return deserialized;
        }

        public void Write(string filePath, T obj)
        {
            var formatter = new XmlSerializer(typeof(T));
            using (var stream = File.Create(filePath))
            {
                formatter.Serialize(stream, obj);
            }
        }
    }
}