using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Insight.GitProvider
{
    public sealed class Scope : IEnumerable<KeyValuePair<string, Guid>>
    {
        private readonly Dictionary<string, Guid> _serverPathToId = new Dictionary<string, Guid>();
        
        public Scope(Dictionary<string, Guid> seed)
        {
            foreach (var pair in seed)
            {
                _serverPathToId.Add(pair.Key, pair.Value);
            }
        }

        public Scope(IEnumerable<KeyValuePair<string, Guid>> seed)
        {
            foreach (var pair in seed)
            {
                _serverPathToId.Add(pair.Key, pair.Value);
            }
        }

        public Scope()
        {
        }

        public Scope Clone()
        {
            // Note: We do not clone the hashsets. Instead we build new ones on the merge commit scope
            var clone = new Scope();
            foreach (var pair in _serverPathToId)
            {
                clone._serverPathToId.Add(pair.Key, pair.Value);
            }

            return clone;
        }

        public HashSet<string> GetAllFiles()
        {
            return _serverPathToId.Keys.ToHashSet();
        }
        

        public void Update(string fromServerPath, string toServerPath)
        {
            if (_serverPathToId.TryGetValue(fromServerPath, out var id) is false)
            {
                throw new Exception($"Tracking renaming but from path '{fromServerPath}' is not available");
            }

            _serverPathToId.Remove(fromServerPath);
            _serverPathToId.Add(toServerPath, id);
        }

        public void Remove(string serverPath)
        {
            if (_serverPathToId.ContainsKey(serverPath))
            {
                _serverPathToId.Remove(serverPath);
                
            }
        }

        // TODO remove when all references are resolved
        public bool IsKnown(string servePath)
        {
            return _serverPathToId.ContainsKey(servePath);
        }

        public bool IsKnown(KeyValuePair<string, Guid> file) // TODO intro of class
        {
            if (_serverPathToId.TryGetValue(file.Key, out var id))
            {
                return id == file.Value;
            }

            return false;
        }


        public string GetIdOrDefault(string serverPath)
        {
            if (_serverPathToId.TryGetValue(serverPath, out var guid))
            {
                return guid.ToString();
            }

            return null;
        }

        private string GetOrCreateId(string serverPath)
        {
            if (_serverPathToId.TryGetValue(serverPath, out var guid))
            {
                return guid.ToString();
            }

            return Add(serverPath);
        }

        public string GetId(string serverPath)
        {
            return _serverPathToId[serverPath].ToString();
        }

        public string Add(string serverPath)
        {
            Debug.Assert(_serverPathToId.ContainsKey(serverPath) is false);
            _serverPathToId.Remove(serverPath);

            var newId = Guid.NewGuid();
            _serverPathToId.Add(serverPath, newId);
            
            return newId.ToString();
        }

        /// <summary>
        /// Adds a file to the scope with an already known id
        /// </summary>
        public void MergeAdd(string serverPath, Guid id)
        {
            _serverPathToId.Add(serverPath, id);
            VerifyScope();
        }

        private void VerifyScope()
        {
            //Debug.Assert(_serverPathToId.Values.Distinct().Count() == _serverPathToId.Count);
        }

        public IEnumerator<KeyValuePair<string, Guid>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, Guid>>) _serverPathToId).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}