﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Insight.Shared;

namespace Insight
{
    class NullAliasMapping : IAliasMapping
    {
        public string GetAlias(string name)
        {
            return name;
        }

        public IEnumerable<string> GetReverse(string alias)
        {
            return new List<string>{alias};
        }
    }

    public class AliasMapping : IAliasMapping
    {
        readonly Dictionary<string, string> _aliasMapping = new Dictionary<string, string>();

        private readonly string _fileName;

        const string Separator = ">>";

        public AliasMapping(string fileName)
        {
            _fileName = fileName;
        }

        /// <summary>
        /// Adds new developers to the default team.
        /// </summary>
        public void CreateDefaultAliases(IEnumerable<string> developers)
        {
            Load();

            var toAdd = developers.Except(_aliasMapping.Keys);

            // Add default alias for new developers
            foreach (var name in toAdd)
            {
                _aliasMapping.Add(name, "Default Team");
            }

            Save();
        }

        public void Load()
        {
            _aliasMapping.Clear();

            if (!File.Exists(_fileName))
            {
                return;
            }

            var lines = File.ReadAllLines(_fileName);

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("#"))
                {
                    continue;
                }

                var parts = line.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Count() != 2)
                {
                    continue;
                }

                var name = parts[0].Trim();
                var alias = parts[1].Trim();
                _aliasMapping.Add(name, alias);
            }
        }

        public void Save()
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Every developer not mentioned in this file is mapped to his own name");
            foreach (var mapping in _aliasMapping)
            {
                builder.AppendLine($"{mapping.Key} {Separator} {mapping.Value}");
            }

            File.WriteAllText(_fileName, builder.ToString());
        }

        public string GetAlias(string name)
        {
            if (!_aliasMapping.TryGetValue(name, out var value))
            {
                return name;
            }

            return value;
        }

        public IEnumerable<string> GetReverse(string alias)
        {
            var names = _aliasMapping.Where(m => m.Value == alias).Select(m => m.Key);
            return names.ToList();
        }
    }
}