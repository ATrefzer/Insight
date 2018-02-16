using Insight.Shared.Exceptions;
using Insight.Shared.System;

namespace Insight.GitProvider
{
    internal sealed class GitCommandLine
    {
        private readonly string _workingDirectory;

        public GitCommandLine(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        public string Annotate(string localPath)
        {
            // git pull origin master
            var program = "git";
            var args = $"annotate \"{localPath}\"";
            return ExecuteCommandLine(program, args).StdOut;
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

        public ProcessResult PullMasterFromOrigin()
        {
            // git pull origin master
            var program = "git";
            var args = $"pull origin master";
            return ExecuteCommandLine(program, args);
        }

        internal string Log()
        {
            // %H   Hash (abbrebiated is %h)
            // %n   Newline
            // %aN  Author name
            // %cN  Committer name
            // %ad  Author date (format respects --date= option)
            // %cd  Committer date (format respects --date= option)
            // %s   Subject (commit message)

            // --num_stat Shows added and removed lines
            var program = "git";

            //var args = $"log --pretty=format:'%H%n%aN%n%ad%n%s' --date=iso --numstat";
            var args = $"log --pretty=format:START_HEADER%n%h%n%cN%n%cd%n%s%nEND_HEADER --date=iso-strict --name-status";

            // Alternativ: iso-strict
            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
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