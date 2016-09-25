using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary {
    class TrieNode {
        public string path;
        public ConcurrentDictionary<string, TrieNode> children;
        public bool isAllowed;
        public bool isEnd;

        public TrieNode(string path) {
            children = new ConcurrentDictionary<string, TrieNode>();
            this.path = path;
            this.isAllowed = true;
            this.isEnd = false;
        }

        public ICollection<TrieNode> GetChildren() {
            return children.Values;
        }
    }
}
