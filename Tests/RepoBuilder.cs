using System;
using System.Collections.Generic;
using System.IO;

using LibGit2Sharp;

namespace Tests
{
    internal sealed class RepoBuilder : IDisposable
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

            // The name of the initial branch depends on the local git configuration (init.defaultBranch).
            // The tests expect "main", so point the unborn HEAD there explicitly.
            // The very first commit now is done in branch main independent of init.defaultBranch.
            repo.Refs.UpdateTarget("HEAD", "refs/heads/main");

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

        /// <summary>
        /// Points HEAD to an unborn branch. The next commit becomes a new root commit
        /// with no parents (unrelated history).
        /// </summary>
        public void CheckoutOrphan(string name)
        {
            _repo.Refs.UpdateTarget("HEAD", $"refs/heads/{name}");
            _repo.Index.Clear();
            _repo.Index.Write();
        }

        /// <summary>
        /// Removes the file from the working directory only (no staging).
        /// </summary>
        public void DeleteFileFromDisk(string fileName)
        {
            File.Delete(Path.Combine(_repoRoot, fileName));
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

        /// <summary>
        /// Creates a merge commit with the current HEAD and the given branches as parents
        /// (octopus merge). The staged index is used as the merge result.
        /// </summary>
        public Commit CommitMerge(string shortMessage, params string[] branchNames)
        {
            var parents = new List<Commit> { _repo.Head.Tip };
            foreach (var branchName in branchNames)
            {
                parents.Add(_repo.Branches[branchName].Tip);
            }

            var tree = _repo.ObjectDatabase.CreateTree(_repo.Index);
            var commit = _repo.ObjectDatabase.CreateCommit(GetSignature(), GetSignature(), shortMessage, tree, parents, false);

            _repo.Refs.UpdateTarget(_repo.Refs.Head.ResolveToDirectReference(), commit.Id);
            return commit;
        }

        public void Merge(string branchName)
        {
            var branchFrom = _repo.Branches[branchName];
            _repo.Merge(branchFrom, GetSignature(),
                new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward });
            

        }

        /// <summary>
        /// Overwrites the file content. Useful to resolve a merge conflict.
        /// </summary>
        public void WriteFile(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(_repoRoot, fileName), content);
            Commands.Stage(_repo, fileName);
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

        private static void DeleteDirectory(string path)
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


        private static string GetRepoRoot(string repoName)
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