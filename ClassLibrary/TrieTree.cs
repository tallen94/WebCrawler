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
            overallRoot = new TrieNode("", true);
            numItems = 0;
        }

        private void AddAuthority(string authority) {
            overallRoot.AddEntry(authority, true);
        }

        public Int64 GetNumItems() {
            return numItems;
        }

        private void AddNewPath(TrieNode root, string[] segments, int level, bool isAllowed) {
            if (level < segments.Length) {
                TrieNode next = root.AddEntry(segments[level], isAllowed);
                if (next != null) {
                    level = level + 1;
                    AddNewPath(next, segments, level, isAllowed);
                }
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
            overallRoot = new TrieNode("", true);
            numItems = 0;
        }

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
    }
}
