using System;
using System.Diagnostics;
using System.IO;

namespace Insight.Metrics
{
    internal sealed class ProcessRunner
    {
        public Tuple<int, string> RunProcess(string pathToExecutable, string arguments)
        {
            var proc = new Process
                       {
                               StartInfo =
                               {
                                       UseShellExecute = false,
                                       FileName = pathToExecutable,
                                       CreateNoWindow = true,
                                       RedirectStandardOutput = true,
                                       RedirectStandardInput = true
                               }
                       };

            if (!string.IsNullOrEmpty(arguments))
            {
                proc.StartInfo.Arguments = arguments;
            }


            if (!File.Exists(pathToExecutable))
            {
                throw new Exception("Executable not found: " + pathToExecutable);
            }

            proc.Start();

            var stdOut = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            return new Tuple<int, string>(proc.ExitCode, stdOut);
        }
    }
}