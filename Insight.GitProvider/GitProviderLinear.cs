using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Insight.Shared;
using Insight.Shared.Model;
using Insight.Shared.VersionControl;

namespace Insight.GitProvider
{
    /// <summary>
    /// Provides higher level funtions and queries on a git repository.
    /// </summary>
    public sealed class GitProviderLinear : GitProviderBase, ISourceControlProvider
    {
        public static string GetClass()
        {
            var type = typeof(GitProviderLinear);
            return type.FullName + "," + type.Assembly.GetName().Name;
        }


        public void Initialize(string projectBase, string cachePath, IFilter fileFilter, string workItemRegex)
        {
            _startDirectory = projectBase;
            _cachePath = cachePath;
            _workItemRegex = workItemRegex;
            _fileFilter = fileFilter;

            _gitHistoryExportFile = Path.Combine(cachePath, @"git_history.log");
            _gitCli = new GitCommandLine(_startDirectory);
        }

        /// <summary>
        /// You need to call UpdateCache before.
        /// </summary>
        public ChangeSetHistory QueryChangeSetHistory()
        {
            VerifyHistoryIsCached();
            return ParseLogFile(_gitHistoryExportFile);
        }

        public void UpdateCache(IProgress progress)
        {
            VerifyGitDirectory();

            var log = _gitCli.Log();
            File.WriteAllText(_gitHistoryExportFile, log);
        }

        /// <summary>
        /// Log file has format specified in GitCommandLine class
        /// </summary>
        protected override ChangeSetHistory ParseLog(Stream log)
        {
            var changeSets = new List<ChangeSet>();
            var tracker = new MovementTracker();

            using (var reader = new StreamReader(log))
            {
                var proceed = GoToNextRecord(reader);
                if (!proceed)
                {
                    throw new FormatException("The file does not contain any change sets.");
                }

                while (proceed)
                {
                    var changeSet = ParseRecord(reader, tracker);
                    changeSets.Add(changeSet);
                    proceed = GoToNextRecord(reader);
                }
            }

            Warnings = tracker.Warnings;
            var history = new ChangeSetHistory(changeSets);
            return history;
        }


        void CreateChangeItem(string changeItem, MovementTracker tracker)
        {
            var ci = new ChangeItem();

            // Example
            // M Visualization.Controls/Strings.resx
            // A Visualization.Controls/Tools/IHighlighting.cs
            // R083 Visualization.Controls/Filter/FilterView.xaml   Visualization.Controls/Tools/ToolView.xaml

            var parts = changeItem.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var changeKind = ToKindOfChange(parts[0]);
            ci.Kind = changeKind;
            if (changeKind == KindOfChange.Rename || changeKind == KindOfChange.Copy)
            {
                Debug.Assert(parts.Length == 3);
                var oldName = parts[1];
                var newName = parts[2];
                ci.ServerPath = Decoder(newName);
                ci.FromServerPath = Decoder(oldName);
                tracker.TrackId(ci);
            }
            else
            {
                Debug.Assert(parts.Length == 2 || parts.Length == 3);
                ci.ServerPath = Decoder(parts[1]);
                tracker.TrackId(ci);
            }

            ci.LocalPath = MapToLocalFile(ci.ServerPath);
        }


        ChangeSet ParseRecord(StreamReader reader, MovementTracker tracker)
        {
            // We are located on the first data item of the record
            var hash = ReadLine(reader);
            var committer = ReadLine(reader);
            var date = ReadLine(reader);
            var parents = ReadLine(reader);
            var comment = ReadComment(reader);

            var cs = new ChangeSet();
            cs.Id = hash;
            cs.Committer = committer;
            cs.Comment = comment;

            ParseWorkItemsFromComment(cs.WorkItems, cs.Comment);

            cs.Date = DateTime.Parse(date);

            tracker.BeginChangeSet(cs);
            ReadChangeItems(reader, tracker);
            tracker.ApplyChangeSet(cs.Items);
            return cs;
        }


        void ReadChangeItems(StreamReader reader, MovementTracker tracker)
        {
            // Now parse the files!
            var changeItem = ReadLine(reader);
            while (changeItem != null && changeItem != recordMarker)
            {
                if (!string.IsNullOrEmpty(changeItem))
                {
                    CreateChangeItem(changeItem, tracker);
                }

                changeItem = ReadLine(reader);
            }
        }
    }
}