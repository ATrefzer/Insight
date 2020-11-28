using System;
using System.IO;

namespace Tests
{
    internal sealed class Cache : IDisposable
    {
        private readonly string _cacheDir;

        public Cache()
        {
            _cacheDir = Path.GetTempPath() + Guid.NewGuid();
            Directory.CreateDirectory(_cacheDir);
        }

        public override string ToString()
        {
            return _cacheDir;
        }

        public void Dispose()
        {
            if (Directory.Exists(_cacheDir))
            {
                Directory.Delete(_cacheDir, true);
            }
        }
    }
}