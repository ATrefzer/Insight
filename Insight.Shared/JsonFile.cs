using System.IO;
using System.Runtime.Serialization.Json;

namespace Insight.Shared
{
    /// <summary>
    /// Uses DataContracts
    /// </summary>
    public sealed class JsonFile<T>
    {
        public T Read(string filePath)
        {
            T deserialized;

            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = File.Open(filePath, FileMode.Open))
            {
                deserialized = (T)serializer.ReadObject(stream);
            }

            return deserialized;
        }

        public void Write(string filePath, T obj)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var stream = File.Create(filePath))
            {
                serializer.WriteObject(stream, obj);
            }
        }
    }
}