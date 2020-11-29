using System;
using System.IO;

using LibGit2Sharp;

namespace Tests
{
    sealed class RepoBuilder : IDisposable
    {
        private readonly Repository _repo;

        private readonly string _repoRoot;

        private const string EnvironmentDirectory = "d:\\TestRepositories";
        private const string UserName = "user";
        private const string UserMail = "user@mail.com";

        public void Dispose()
        {
            _repo?.Dispose();
        }

        public static string GetRepoPath(string repoName)
        {
            return Path.Combine(EnvironmentDirectory, repoName);
        }

        public RepoBuilder(Repository repo, string repoRoot)
        {
            _repo = repo;
            _repoRoot = repoRoot;
        }

        public static RepoBuilder InitNewRepository(string repoName)
        {
            SetupEnvironment();

            var repoDir = GetRepoRoot(repoName);
            DeleteDirectory(repoDir);

            Directory.CreateDirectory(repoDir);

            var repo = new Repository(Repository.Init(repoDir));
            return new RepoBuilder(repo, repoDir);
        }


        public void Checkout(string name)
        {
            var branch = _repo.Branches[name];
            Commands.Checkout(_repo, branch);
        }


        public void CreateBranch(string name)
        {
            _repo.CreateBranch(name);
        }


        public void AddFile(string fileName)
        {
            using (File.CreateText(Path.Combine(_repoRoot, fileName)))
            {
            }

            Commands.Stage(_repo, fileName);
        }

        public void DeleteFile(string fileName)
        {
            File.Delete(Path.Combine(_repoRoot, fileName));
            Commands.Stage(_repo, fileName);
        }

        public Commit Commit(string shortMessage = "")
        {
            return _repo.Commit(shortMessage, GetSignature(), GetSignature());
        }

        public void Merge(string branchName)
        {
            var branchFrom = _repo.Branches[branchName];
            _repo.Merge(branchFrom, GetSignature(), new MergeOptions{FastForwardStrategy = FastForwardStrategy.NoFastForward});
        }


        public void ModifyFileAppend(string fileName, string content)
        {
            using (var file = File.AppendText(Path.Combine(_repoRoot, fileName)))
            {
                file.WriteLine(content);
            }
            Commands.Stage(_repo, fileName);
        }


        private Signature GetSignature()
        {
            return new Signature(UserName, UserMail, DateTimeOffset.Now);
        }


        private static void SetupEnvironment()
        {
            if (Directory.Exists(EnvironmentDirectory) is false)
            {
                Directory.CreateDirectory(EnvironmentDirectory);
            }
        }

        static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

                foreach (var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }

                directory.Delete(true);
            }
        }


        static string GetRepoRoot(string repoName)
        {
            return Path.Combine(EnvironmentDirectory, repoName);
        }

        public void Rename(string source, string destination)
        {
            File.Move(Path.Combine(_repoRoot, source), Path.Combine(_repoRoot, destination));
            Commands.Stage(_repo, source);
            Commands.Stage(_repo, destination);
        }
    }
}