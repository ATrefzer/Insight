using System.IO;

using Insight.Shared.Exceptions;
using Insight.Shared.System;

namespace Insight.GitProvider
{
    sealed class GitCommandLine
    {
        /// <summary>
        /// %H   Hash (abbrebiated is %h)
        /// %n   Newline
        /// %aN  Author name
        /// %cN  Committer name
        /// %ad  Author date (format respects --date= option)
        /// %cd  Committer date (format respects --date= option)
        /// %s   Subject (commit message)
        /// %P   Parents (all sha1s in one line) First commit does not have a parent!
        /// Log of the whole branch or a single file shall have the same output for easier parsing.
        /// </summary>
        const string LogFormat = "START_HEADER%n%H%n%cN%n%cd%n%P%n%s%nEND_HEADER";

        readonly string _workingDirectory;

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

        public void ExportFileRevision(string serverPath, string revision, string exportFile)
        {
            var program = "git";

            var args = $"show {revision}:\"{serverPath}\"";

            var result = ExecuteCommandLine(program, args);
            File.WriteAllText(exportFile, result.StdOut);
        }

        public string GetAllTrackedFiles()
        {
            var program = "git";

            // Optional HEAD
            var args = $"ls-tree -r master --name-only";

            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
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
            // --num_stat Shows added and removed lines
            var program = "git";

            //var args = $"log --pretty=format:'%H%n%aN%n%ad%n%s' --date=iso --numstat";
            var args = $"-c diff.renameLimit=99999 log --pretty=format:{LogFormat} --date=iso-strict --name-status";

            // Alternatives: iso-strict, iso
            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
        }

        internal string Log(string localPath)
        {
            localPath = localPath.Replace("\\", "/");
            var program = "git";

            // --follow to track
            var args = $"log --follow --pretty=format:{LogFormat} --date=iso-strict --name-status -- \"{localPath}\"";

            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
        }

        ProcessResult ExecuteCommandLine(string program, string args)
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