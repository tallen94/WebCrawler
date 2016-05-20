using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public Int64 GetNumItems() {
            return numItems;
        }

        public void AddDisallowedUrl(Uri uri) {
            TrieNode authority = overallRoot.children.GetOrAdd(uri.Authority, new TrieNode(uri.Authority));
            AddDisallowedPath(authority, uri.Segments, 0);
        }

        private void AddDisallowedPath(TrieNode root, string[] segments, int level) {
            if (level == segments.Length) {
                root.isAllowed = false;
            } else {
                TrieNode next = root.children.GetOrAdd(segments[level], new TrieNode(segments[level]));
                level = level + 1;
                AddDisallowedPath(next, segments, level);
            }
        }

        /*
         * Searches the tree and if the path is not found it adds it and returns false
         * If the path was found or it hits a path that isnt allowed it returns true.
         */
        public bool HasPath(Uri uri) {
            bool hasPath = true;
            TrieNode authority = overallRoot.children.GetOrAdd(uri.Authority, new TrieNode(uri.Authority));
            AddPath(authority, uri.Segments, 0, ref hasPath);
            return hasPath;
        }

        private void AddPath(TrieNode root, string[] segments, int level, ref bool hasPath) {
            if (root.isAllowed && level < segments.Length) {
                hasPath = root.children.ContainsKey(segments[level]);
                TrieNode next = root.children.GetOrAdd(segments[level], new TrieNode(segments[level]));
                level = level + 1;
                AddPath(next, segments, level, ref hasPath);
            } 
        }
        
        public void ClearTree() {
            overallRoot = new TrieNode("");
            numItems = 0;
        }

        /*
        public void SaveTree() {
            if (File.Exists(Path.GetTempPath() + "visited.txt")) {
                File.Delete(Path.GetTempPath() + "visited.txt");
            }
            using (StreamWriter sw = new StreamWriter(Path.GetTempPath() + "visited.txt")) {
                ICollection<TrieNode> authorities = overallRoot.GetChildren();

                foreach (TrieNode authority in authorities) {
                    DFSWrite(authority, sw);
                }
            }
        }

        
        private void DFSWrite(TrieNode root, StreamWriter sw) {
            if (!root.HasChildren()) {
                sw.WriteLine(root.GetValue() + " => " + root.IsAllowed() + " => leaf");
            } else {
                sw.WriteLine(root.GetValue() + " => " + root.IsAllowed());
                ICollection<TrieNode> children = root.GetChildren();
                foreach (TrieNode child in children) {
                    DFSWrite(child, sw);
                }
            }
        }

        public bool ReBuildIfCan() {
            if (File.Exists(Path.GetTempPath() + "visited.txt")) {
                using (StreamReader sr = new StreamReader(Path.GetTempPath() + "visited.txt")) {
                    if (sr != null) {
                        AddChildren(overallRoot, sr);
                        return true;
                    }
                }
            }
            return false;
        }

        private void AddChildren(TrieNode root, StreamReader sr) {
            string[] line = sr.ReadLine().Split(new string[] { " => " }, StringSplitOptions.None);
            if (line.Length == 2) {
                TrieNode child = root.AddEntry(line[0], Convert.ToBoolean(line[1]));
                Debug.Write(line[0]);
                AddChildren(child, sr);
            } else {
                root.AddEntry(line[0], Convert.ToBoolean(line[1]));
                Debug.WriteLine(line[0]);
            }
        }
        */
    }
}
