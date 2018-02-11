using System;
using System.Diagnostics;

namespace Insight.Shared.System
{
    public static class ProcessRunner
    {
        public static Tuple<int, string, string> RunProcess(string pathToExecutable, string arguments)
        {
            return RunProcess(pathToExecutable, arguments, null);
        }

        /// <summary>
        /// ExitCode, StdOut, StdErr
        /// </summary>
        public static Tuple<int, string, string> RunProcess(string pathToExecutable, string arguments, string workingDirectory)
        {
            using (var process = CreateProcess(pathToExecutable, workingDirectory))
            {
                if (!string.IsNullOrEmpty(arguments))
                {
                    process.StartInfo.Arguments = arguments;
                }

                process.Start();

                var stdOut = process.StandardOutput.ReadToEnd();
                var stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new Tuple<int, string, string>(process.ExitCode, stdOut, stdErr);
            }
        }

        private static Process CreateProcess(string pathToExecutable, string workingDirectory = null)
        {
            var startInfo = new ProcessStartInfo
                            {
                                    UseShellExecute = false,
                                    FileName = pathToExecutable,
                                    CreateNoWindow = true,
                                    RedirectStandardOutput = true,
                                    RedirectStandardInput = true,
                                    RedirectStandardError = true
                            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            var process = new Process
                          {
                                  StartInfo = startInfo
                          };

            return process;
        }
    }
}