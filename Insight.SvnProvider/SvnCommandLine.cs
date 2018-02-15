using Insight.Shared.Exceptions;
using Insight.Shared.System;

namespace Insight.SvnProvider
{
    /// <summary>
    /// Svn command line interface
    /// </summary>
    internal sealed class SvnCommandLine
    {
        private readonly string _workingDirectory;

        public SvnCommandLine(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        public string BlameFile(string localFile)
        {
            var program = "svn";
            var args = $"blame {localFile}";
            return ExecuteCommandLine(program, args);
        }

        public void ExportFileRevision(string localFile, int revision, string exportFile)
        {
            var program = "svn";
            var args = $"export -r {revision} {localFile} {exportFile}";
            ExecuteCommandLine(program, args);
        }

        public string GetRevisionsForLocalFile(string localFile)
        {
            var program = "svn";
            var args = $"log {localFile} --xml";
            return ExecuteCommandLine(program, args);
        }

        public string Log(int revision)
        {
            var program = "svn";
            var args = $"log -v --xml -r {revision}:HEAD";
            return ExecuteCommandLine(program, args);
        }

        /// <summary>
        /// Called in base directory to obtain the server path.
        /// </summary>        
        public string Info()
        {
            var program = "svn";
            var args = $"info --xml";
            return ExecuteCommandLine(program, args);
        }


        public void UpdateWorkingCopy()
        {
            var program = "svn";
            var args = $"update";
            ExecuteCommandLine(program, args);
        }

        public bool HasModifications()
        {
            // Quiet hides files not under version control
            var program = "svn";
            var args = $"status -q";
            var stdOut = ExecuteCommandLine(program, args);
            return !string.IsNullOrEmpty(stdOut.Trim());
        }

        internal string Log()
        {
            var program = "svn";
            var args = $"log -v --xml";
            return ExecuteCommandLine(program, args);
        }

        private string ExecuteCommandLine(string program, string args)
        {
            var result = ProcessRunner.RunProcess(program, args, _workingDirectory);

            if (!string.IsNullOrEmpty(result.StdErr))
            {
                throw new ProviderException(result.StdErr);
            }

            return result.StdOut;
        }
    }
}