using Insight.Shared.Exceptions;
using Insight.Shared.Model;
using Insight.Shared.System;

namespace Insight.SvnProvider
{
    /// <summary>
    /// Svn command line interface
    /// </summary>
    internal sealed class SvnCommandLine
    {
        private readonly string _workingDirectory;
        private readonly ProcessRunner _runner;

        public SvnCommandLine(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
            _runner = new ProcessRunner();
        }

        public string BlameFile(string localFile)
        {
            var program = "svn";
            var args = $"blame \"{localFile}\"";
            return ExecuteCommandLine(program, args);
        }

        public void ExportFileRevision(string localFile, string revision, string exportFile)
        {
            var program = "svn";
            var args = $"export -r {revision} \"{localFile}\" \"{exportFile}\"";
            ExecuteCommandLine(program, args);
        }

        public string GetAllTrackedFiles()
        {
            var program = "svn";
            var args = $"list --recursive -r HEAD";
            return ExecuteCommandLine(program, args);
        }

        public string GetRevisionsForLocalFile(string localFile)
        {
            var program = "svn";
            var args = $"log \"{localFile}\" --xml";
            return ExecuteCommandLine(program, args);
        }

        public bool HasModifications()
        {
            // Quiet hides files not under version control
            var program = "svn";
            var args = $"status -q";
            var stdOut = ExecuteCommandLine(program, args);
            return !string.IsNullOrEmpty(stdOut.Trim());
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


        public string Info(string obj)
        {
            var program = "svn";
            var args = $"info {obj} --xml";
            return ExecuteCommandLine(program, args);
        }

        public string Log(Id revision)
        {
            var program = "svn";
            var args = $"log -v --xml -r {revision}:HEAD";
            return ExecuteCommandLine(program, args);
        }


        public void UpdateWorkingCopy()
        {
            var program = "svn";
            var args = $"update";
            ExecuteCommandLine(program, args);
        }

        internal string Log()
        {
            var program = "svn";
            var args = $"log -v --xml";
            return ExecuteCommandLine(program, args);
        }

        private string ExecuteCommandLine(string program, string args)
        {
            var result = _runner.RunProcess(program, args, _workingDirectory);

            if (!string.IsNullOrEmpty(result.StdErr))
            {
                throw new ProviderException(result.StdErr);
            }

            return result.StdOut;
        }
    }
}