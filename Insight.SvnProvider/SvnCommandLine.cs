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
            // Added --force because a text file was recognized as binary.
            const string program = "svn";
            var args = $"blame \"{localFile}\" --force";
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
            const string program = "svn";
            const string args = "list --recursive -r HEAD";
            return ExecuteCommandLine(program, args);
        }

        public string GetRevisionsForLocalFile(string localFile)
        {
            const string program = "svn";
            var args = $"log \"{localFile}\" --xml";
            return ExecuteCommandLine(program, args);
        }

        public bool HasModifications()
        {
            // Quiet hides files not under version control
            const string program = "svn";
            const string args = "status -q";
            var stdOut = ExecuteCommandLine(program, args);
            return !string.IsNullOrEmpty(stdOut.Trim());
        }

        /// <summary>
        /// Called in base directory to obtain the server path.
        /// </summary>
        public string Info()
        {
            const string program = "svn";
            const string args = "info --xml";
            return ExecuteCommandLine(program, args);
        }


        public string Info(string obj)
        {
            const string program = "svn";
            var args = $"info {obj} --xml";
            return ExecuteCommandLine(program, args);
        }

        public string Log(Id revision)
        {
            const string program = "svn";
            var args = $"log -v --xml -r {revision}:HEAD";
            return ExecuteCommandLine(program, args);
        }


        public void UpdateWorkingCopy()
        {
            var program = "svn";
            var args = "update";
            ExecuteCommandLine(program, args);
        }

        internal string Log()
        {
            const string program = "svn";
            const string args = "log -v --xml";
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