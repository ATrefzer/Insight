using Insight.Shared.Exceptions;
using Insight.Shared.System;

namespace Insight.GitProvider
{
    class GitCommandLine
    {      
        private readonly string _workingDirectory;

        public GitCommandLine(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        //git log --pretty=format:'[%h] %aN %ad %s' --date=short --numstat

        public ProcessResult PullMasterFromOrigin()
        {
            // git pull origin master
            var program = "git";
            var args = $"pull origin master";
            return ExecuteCommandLine(program, args);
        }

        /// <summary>
        /// Returns true if there are any changes in the working or
        /// staging area.
        /// </summary>
        public bool HasLocalChanges()
        {
            var program = "git";
            var args = $"status --short";
            var result = ExecuteCommandLine(program, args);
            return !string.IsNullOrEmpty(result.StdOut.Trim());
        }

        /// <summary>
        /// Returns true if there are local commits not pushed to the remote.
        /// </summary>
        public bool HasLocalCommits()
        {
            var hint = "your branch is ahead of";
            var program = "git";
            var args = $"status";
            var result = ExecuteCommandLine(program, args);
            return !result.StdOut.ToLowerInvariant().Contains(hint);
        }

        private ProcessResult ExecuteCommandLine(string program, string args)
        {
            var result = ProcessRunner.RunProcess(program, args, _workingDirectory);

            if (!string.IsNullOrEmpty(result.StdErr))
            {
                throw new ProviderException(result.StdErr);
            }

            return result;
        }


    }
}
