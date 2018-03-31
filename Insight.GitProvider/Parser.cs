using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Insight.Shared;
using Insight.Shared.Model;

namespace Insight.GitProvider
{
    /// <summary>
    /// Parser for a git log output. No processing but decoding the 1252 codes etc.
    /// Note: The parser does not provide item ids! This is no concept known by git.
    /// </summary>
    public sealed class Parser
    {
        readonly PathMapper _mapper;
        readonly Action<string, string> _updateGraph;
        readonly string endHeaderMarker = "END_HEADER";
        readonly string recordMarker = "START_HEADER";
        string _lastLine;

        public Parser(PathMapper mapper, Action<string, string> updateGraph)
        {
            _mapper = mapper;

            // cs id -> parents (tab separated)
            _updateGraph = updateGraph;
        }

        public string WorkItemRegex { get; set; }


        public ChangeSetHistory ParseLogFile(string logFile)
        {
            using (var stream = new FileStream(logFile, FileMode.Open))
            {
                var history = ParseLog(stream);
                return history;
            }
        }

        public ChangeSetHistory ParseLogString(string gitLogString)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(gitLogString));
            return ParseLog(stream);
        }

        void CreateChangeItem(ChangeSet cs, string changeItem)
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
                ci.ServerPath = Decoder.DecodeEscapedBytes(newName);
                ci.FromServerPath = Decoder.DecodeEscapedBytes(oldName);
                cs.Items.Add(ci);
            }
            else
            {
                Debug.Assert(parts.Length == 2 || parts.Length == 3);
                ci.ServerPath = Decoder.DecodeEscapedBytes(parts[1]);
                cs.Items.Add(ci);
            }

            ci.LocalPath = _mapper.MapToLocalFile(ci.ServerPath);
        }

        bool GoToNextRecord(StreamReader reader)
        {
            if (_lastLine == recordMarker)
            {
                // We are already positioned on the next changeset.
                return true;
            }

            string line;
            while ((line = ReadLine(reader)) != null)
            {
                if (line.Equals(recordMarker))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Log file has format specified in GitCommandLine class
        /// </summary>
        ChangeSetHistory ParseLog(Stream log)
        {
            var changeSets = new List<ChangeSet>();

            using (var reader = new StreamReader(log))
            {
                var proceed = GoToNextRecord(reader);
                if (!proceed)
                {
                    throw new FormatException("The file does not contain any change sets.");
                }

                while (proceed)
                {
                    var changeSet = ParseRecord(reader);
                    changeSets.Add(changeSet);
                    proceed = GoToNextRecord(reader);
                }
            }

            var history = new ChangeSetHistory(changeSets.OrderByDescending(x => x.Date).ToList());
            return history;
        }

        ChangeSet ParseRecord(StreamReader reader)
        {
            // We are located on the first data item of the record
            var hash = ReadLine(reader);

            var committer = ReadLine(reader);
            var date = ReadLine(reader);
            var parents = ReadLine(reader);

            var comment = ReadComment(reader);

            _updateGraph?.Invoke(hash, parents);

            var cs = new ChangeSet();
            cs.Id = hash;
            cs.Committer = committer;
            cs.Comment = comment;

            ParseWorkItemsFromComment(cs.WorkItems, cs.Comment);

            cs.Date = DateTime.Parse(date);

            ReadChangeItems(cs, reader);
            return cs;
        }

        public void ParseWorkItemsFromComment(List<WorkItem> workItems, string comment)
        {
            if (!string.IsNullOrEmpty(WorkItemRegex))
            {
                var extractor = new WorkItemExtractor(WorkItemRegex);
                workItems.AddRange(extractor.Extract(comment));
            }
        }

        void ReadChangeItems(ChangeSet cs, StreamReader reader)
        {
            // Now parse the files!
            var changeItem = ReadLine(reader);
            while (changeItem != null && changeItem != recordMarker)
            {
                if (!string.IsNullOrEmpty(changeItem))
                {
                    CreateChangeItem(cs, changeItem);
                }

                changeItem = ReadLine(reader);
            }
        }

        string ReadComment(StreamReader reader)
        {
            string commentLine;

            var commentBuilder = new StringBuilder();
            while ((commentLine = ReadLine(reader)) != endHeaderMarker)
            {
                if (!string.IsNullOrEmpty(commentLine))
                {
                    commentBuilder.AppendLine(commentLine);
                }
            }

            Debug.Assert(commentLine == endHeaderMarker);
            return commentBuilder.ToString().Trim('\r', '\n');
        }

        public string ReadLine(StreamReader reader)
        {
            // The only place where we read
            var raw = reader.ReadLine()?.Trim();
           
            // Rely on reading from the process output is correct.
            _lastLine = raw;
            return _lastLine;
        }

        public static KindOfChange ToKindOfChange(string kind)
        {
            if (kind.StartsWith("R"))
            {
                // Followed by the similarity
                return KindOfChange.Rename;
            }

            if (kind.StartsWith("C"))
            {
                // Followed by the similarity.              
                return KindOfChange.Copy;
            }
            else if (kind == "A")
            {
                return KindOfChange.Add;
            }
            else if (kind == "D")
            {
                return KindOfChange.Delete;
            }
            else if (kind == "M")
            {
                return KindOfChange.Edit;
            }
            else
            {
                Debug.Assert(false);
                // ReSharper disable once HeuristicUnreachableCode
                return KindOfChange.None;
            }
        }
    }
}