using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary {
    public class TrieTree {
        private TrieNode overallRoot;
        private Int64 numItems;

        public TrieTree() {
            overallRoot = new TrieNode("");
            numItems = 0;
        }

        private void AddAuthority(string authority) {
            overallRoot.AddEntry(authority);
        }

        public Int64 GetNumItems() {
            return numItems;
        }

        private void AddNewPath(TrieNode root, string[] segments, int level, bool isAllowed) {
            if (level < segments.Length) {
                TrieNode next = root.AddEntry(segments[level]);
                if (next != null) {
                    level = level + 1;
                    AddNewPath(next, segments, level, isAllowed);
                }
            } else {
                root.setIsAllowed(isAllowed);
            }
        }

        public bool FindOrAdd(Uri uri) {
            int level = 0;
            if (!overallRoot.ContainsChild(uri.Authority)) {
                AddAuthority(uri.Authority);
            }
            TrieNode end = FindPath(overallRoot.GetChild(uri.Authority), uri.Segments, ref level);
            if (end.IsAllowed() && level < uri.Segments.Length) {
                AddNewPath(end, uri.Segments, level, true);
                return true;
            }
            return false;
        }

        public bool Find(Uri uri) {
            int level = 0;
            if (!overallRoot.ContainsChild(uri.Authority)) {
                return false;
            }
            TrieNode end = FindPath(overallRoot.GetChild(uri.Authority), uri.Segments, ref level);
            return end.IsAllowed() && level == uri.Segments.Length - 1;
        }
        
        public void AddDisallow(Uri uri) {
            if (!overallRoot.ContainsChild(uri.Authority)) {
                AddAuthority(uri.Authority);
            }
            AddNewPath(overallRoot.GetChild(uri.Authority), uri.Segments, 0, false);
        }

        private TrieNode FindPath(TrieNode root, string[] segments, ref int level) {
            if (level == segments.Length || !root.ContainsChild(segments[level]) || !root.IsAllowed()) {
                return root;
            } else {
                TrieNode next = root.GetChild(segments[level]);
                level = level + 1;
                return FindPath(next, segments, ref level);
            }
        }

        public void ClearTree() {
            overallRoot = new TrieNode("");
            numItems = 0;
        }
    }
}
