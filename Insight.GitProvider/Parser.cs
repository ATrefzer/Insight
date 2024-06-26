﻿using System;
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
    /// Parser for a git log output.
    /// Note: The parser does not provide item ids! This is no concept known by git.
    /// </summary>
    public sealed class Parser
    {
        private readonly PathMapper _mapper;
        private const string EndHeaderMarker = "END_HEADER";
        private const string RecordMarker = "START_HEADER";
        private string _lastLine;

        public Parser(PathMapper mapper)
        {
            _mapper = mapper;
        }

        public string WorkItemRegex { get; set; }


        public ChangeSetHistory ParseLogFile(string logFile)
        {
            using (var stream = new FileStream(logFile, FileMode.Open))
            {
                var history = ParseLog(stream, null);
                return history;
            }
        }

        public (ChangeSetHistory, Graph) ParseLogString(string gitLogString)
        {
            var graph = new Graph();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(gitLogString));
            return (ParseLog(stream, graph), graph);
        }

        public ChangeSetHistory ParseLogStringNoGraph(string gitLogString)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(gitLogString));
            return ParseLog(stream, null);
        }


        private void CreateChangeItem(ChangeSet cs, string changeItem)
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

        private bool GoToNextRecord(StreamReader reader)
        {
            if (_lastLine == RecordMarker)
            {
                // We are already positioned on the next changeset.
                return true;
            }

            string line;
            while ((line = ReadLine(reader)) != null)
            {
                if (line.Equals(RecordMarker))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Log file has format specified in GitCommandLine class
        /// </summary>
        private ChangeSetHistory ParseLog(Stream log, Graph graph)
        {
            var changeSets = new List<ChangeSet>();

            using (var reader = new StreamReader(log))
            {
                var proceed = GoToNextRecord(reader);
                if (!proceed)
                {
                    return new ChangeSetHistory(new List<ChangeSet>());
					
					// I found one case where git log --follow caused an empty output.
                    //throw new FormatException("The file does not contain any change sets.");
                }

                while (proceed)
                {
                    var changeSet = ParseRecord(reader, graph);
                    changeSets.Add(changeSet);
                    proceed = GoToNextRecord(reader);
                }
            }

            var history = new ChangeSetHistory(changeSets.OrderByDescending(x => x.Date).ToList());
            return history;
        }

        private ChangeSet ParseRecord(StreamReader reader, Graph graph)
        {
            // We are located on the first data item of the record
            var hash = ReadLine(reader);

            var committer = ReadLine(reader);
            var date = ReadLine(reader);
            var parents = ReadLine(reader);

            var comment = ReadComment(reader);

            // Last node has no parents.
            graph?.UpdateGraph(hash, parents);

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

        private void ReadChangeItems(ChangeSet cs, StreamReader reader)
        {
            // Now parse the files!
            var changeItem = ReadLine(reader);
            while (changeItem != null && changeItem != RecordMarker)
            {
                if (!string.IsNullOrEmpty(changeItem))
                {
                    CreateChangeItem(cs, changeItem);
                }

                changeItem = ReadLine(reader);
            }
        }

        private string ReadComment(StreamReader reader)
        {
            string commentLine;

            var commentBuilder = new StringBuilder();
            while ((commentLine = ReadLine(reader)) != EndHeaderMarker)
            {
                if (!string.IsNullOrEmpty(commentLine))
                {
                    commentBuilder.AppendLine(commentLine);
                }
            }

            Debug.Assert(commentLine == EndHeaderMarker);
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
                // Note overlapping with "RM"
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
            else if (kind == "D" || kind == "DD")
            {
                return KindOfChange.Delete;
            }
            else if (kind == "M")
            {
                return KindOfChange.Edit;
            }
            else if (kind == "T")
            {
                return KindOfChange.TypeChanged;
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