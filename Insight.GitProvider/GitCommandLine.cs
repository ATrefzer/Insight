using Insight.Shared.Exceptions;
using Insight.Shared.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insight.GitProvider
{
    class GitCommandLine
    {
        // TODO Configure path to git instead of path
        private readonly string _workingDirectory;

        public GitCommandLine(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        //git log --pretty=format:'[%h] %aN %ad %s' --date=short --numstat

        public string PullMasterFromOrigin()
        {
            // git pull origin master
            var program = "git";
            var args = $"pull origin master";
            return ExecuteCommandLine(program, args);
             
            // TODO  
        }

        /// <summary>
        /// Returns true if there are any changes in the working or
        /// staging area.
        /// </summary>
        public bool HasLocalChanges()
        {
            var program = "git";
            var args = $"status --short";
            var result = ProcessRunner.RunProcess(program, args, _workingDirectory);

            return !string.IsNullOrEmpty(result.StdOut.Trim());
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
