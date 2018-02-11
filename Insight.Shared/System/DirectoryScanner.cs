using System;
using System.Collections.Generic;
using System.IO;

namespace Insight.Shared.System
{
    public class DirectoryScanner
    {
        public List<string> GetFilesRecursive(string rootDir)
        {
            var foundFiles = new List<string>();
            Scan(rootDir, foundFiles, true);
            return foundFiles;
        }

        private void Scan(string rootDir, List<string> foundFiles, bool recursive)
        {
            try
            {
                // Files (leaf nodes)
                var files = Directory.EnumerateFiles(rootDir);
                foreach (var file in files)
                {
                    foundFiles.Add(file);
                }
            }
            catch (Exception)
            {
                // 
            }

            if (recursive == false)
            {
                return;
            }

            var subDirs = Directory.EnumerateDirectories(rootDir);
            try
            {
                foreach (var subDir in subDirs)
                {
                    Scan(subDir, foundFiles, true);
                }
            }
            catch (Exception)
            {
                //
            }
        }
    }
}