using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary {
    class TrieNode {
        private string path;
        private ConcurrentDictionary<string, TrieNode> children;
        private int numChildren;
        private bool isAllowed;

        public TrieNode(string path, bool isAllowed) {
            children = new ConcurrentDictionary<string, TrieNode>();
            this.path = path;
            numChildren = 0;
            this.isAllowed = isAllowed;
        }

        public ICollection<TrieNode> GetChildren() {
            return children.Values;
        }

        public TrieNode AddEntry(string entry, bool isAllowed) {
            if (!ContainsChild(entry)) {
                TrieNode child = new TrieNode(entry, isAllowed);
                children.TryAdd(entry, child);
                numChildren++;
                return child;
            } else {
                return null;
            }
        }
        
        public string GetValue() {
            return path;
        }

        public bool HasChildren() {
            return numChildren > 0;
        }

        public bool ContainsChild(string key) {
            return GetChild(key) != null;
        }

        public TrieNode GetChild(string key) {
            TrieNode child;
            children.TryGetValue(key, out child);
            return child;
        }

        public int GetNumChildren() {
            return numChildren;
        }

        public void setIsAllowed(bool isAllowed) {
            this.isAllowed = isAllowed;
        }

        public bool IsAllowed() {
            return isAllowed;
        }
    }
}
