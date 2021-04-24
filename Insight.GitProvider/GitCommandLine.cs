using System;
using System.IO;
using System.Linq;
using System.Text;
using Insight.Shared.Exceptions;
using Insight.Shared.System;

namespace Insight.GitProvider
{
    public sealed class GitCommandLine
    {
        // Note: Use Committer Date. Otherwise children of a commit may appear before the parent.

        /// <summary>
        /// %H   Hash (abbreviated is %h)
        /// %n   Newline
        /// %aN  Author name
        /// %cN  Committer name
        /// %ad  Author date (format respects --date= option)
        /// %cd  Committer date (format respects --date= option)
        /// %s   Subject (commit message)
        /// %P   Parents (all sha1s in one line) First commit does not have a parent!
        /// Log of the whole branch or a single file shall have the same output for easier parsing.
        /// </summary>
        private const string LogFormat = "START_HEADER%n%H%n%aN%n%ad%n%P%n%s%nEND_HEADER";

        const string MainBranch = "master";
        private readonly string _workingDirectory;
        private readonly string _branch;
        private readonly ProcessRunner _runner;

        public GitCommandLine(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
            _runner = new ProcessRunner { DefaultEncoding = Encoding.UTF8 };
            _branch = GetCheckedOutBranch();
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

        public string GetAllTrackedFiles(string hash = null)
        {
            var program = "git";

            if (hash == null)
            {
                hash = "HEAD";
            }

            // Optional HEAD
            var args = $"ls-tree -r --name-only {hash}";

            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
        }

        /// <summary>
        /// Returns true if there are any changes in the working or
        /// staging area. TODO really
        /// Detects un-tracked changes
        /// </summary>
        public bool HasLocalChanges()
        {
            const string program = "git";
            const string args = "status --short";
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
            var args = "status";
            var result = ExecuteCommandLine(program, args);
            return !result.StdOut.ToLowerInvariant().Contains(hint);
        }


        public bool HasIndexOrWorkspaceChanges()
        {
            // https://stackoverflow.com/questions/3882838/whats-an-easy-way-to-detect-modified-files-in-a-git-workspace

            // ... check both the staged contents (what is in the index) and the files in the working tree.
            // Alternatives like git ls-files -m will only check the working tree against the index
            // (i.e. they will ignore any staged (but uncommitted) content that is also in the working tree)

            var program = "git";
            var args = "diff-index -M -C --quiet HEAD";
            var result = ExecuteCommandLine(program, args);

            // Exit code 0 = no changes
            return result.ExitCode != 0;
        }


        public string LogWithoutRenames()
        {
            // git log --all --numstat --date=short --pretty=format:'--%h--%ad--%aN' --no-renames 
            var program = "git";

            var args = $"log --pretty=format:{LogFormat} --date=iso-strict-local --name-status --no-renames";

            //-c diff.renameLimit=99999 log --pretty=format:{START_HEADER%n%H%n%aN%n%cd%n%P%n%s%nEND_HEADER --date=iso-strict --name-status --simplify-merges --full-history
            
            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
        }

        public string Log()
        {
            // --num_stat Would shows added and removed lines

            var program = "git";

            // Full history, simplify merges. Renames are tracked by default.
            // We could use --find-renames -M9 to change the similarity to 90% but to results are not so good
            //
            // --cc implies the -c option and further compresses merge commits.
            //
            var args = $"-c diff.renameLimit=99999 log --pretty=format:{LogFormat} --date=iso-strict-local --name-status --simplify-merges --full-history";

            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
        }

        /// <summary>
        /// Git by default simplifies the history for a single file. This means the parent relationships may be incomplete.
        /// Renames are tracked. Merge commits that do not contribute are not part of the history.
        /// </summary>
        public string Log(string localPath)
        {
            localPath = localPath.Replace("\\", "/");
            const string program = "git";

            // --follow to track renaming, works only for a single file!
            // When --follow is used --full-history has no effect. We don't see merge commits that do contribute to the file.

            // I had a single case where --follow caused the output to be empty!

            var args = $"log --follow --pretty=format:{LogFormat} --date=iso-strict-local --name-status -- \"{localPath}\"";

            var result = ExecuteCommandLine(program, args);
            return result.StdOut;
        }


        public string GetCheckedOutBranch()
        {
            var program = "git";
            var args = "symbolic-ref --short -q HEAD";
            var result = ExecuteCommandLine(program, args);
            return result.StdOut.Trim();
        }

        public bool IsMasterGetCheckedOut()
        {
            const string program = "git";
            const string args = "symbolic-ref --short -q HEAD";
            var result = ExecuteCommandLine(program, args);
            return string.Compare(result.StdOut.Trim('\n'), _branch, System.StringComparison.OrdinalIgnoreCase) == 0;
        }

        private ProcessResult ExecuteCommandLine(string program, string args)
        {
            var result = _runner.RunProcess(program, args, _workingDirectory);

            if (!string.IsNullOrEmpty(result.StdErr))
            {
                throw new ProviderException(result.StdErr);
            }

            return result;
        }

        public string GetMasterHead(string repoDirectory)
        {
            var branch = GetCheckedOutBranch();
            var masterRefPath = Path.Combine(repoDirectory, $".git\\refs\\heads\\{branch}");
            if (!File.Exists(masterRefPath))
            {
                throw new Exception("Can't locate master's head.");
            }
            var lines = File.ReadAllLines(masterRefPath);
            return lines.Single().Substring(0, 40);
        }
    }
}