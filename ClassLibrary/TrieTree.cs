using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                root.isEnd = true;
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
            } else {
                root.isEnd = true;
            }
        }
        
        public void ClearTree() {
            overallRoot = new TrieNode("");
            numItems = 0;
        }

        
        public void SaveTreeToBlob() {
            var localPath = Path.GetTempPath() + "visited.txt";
            using (StreamWriter stream = new StreamWriter(localPath, false)) {
                WriteLinks(stream, overallRoot, "");
            }

            using(var fs = File.OpenRead(localPath)) {
                AzureStorage.VisitedLinksContainer.GetBlockBlobReference("lastsave").UploadFromStream(fs);
            }
        }
        
        private void WriteLinks(StreamWriter writer, TrieNode root, string link) {
            if (root.isEnd) {
                writer.WriteLine("http://" + link + " => " + root.isAllowed);
            }
            foreach (TrieNode child in root.children.Values) {
                WriteLinks(writer, child, link + child.path);
            }
        }

        public void DownloadVisitedLinks() {
            var localPath = Path.GetTempPath() + "visited.txt";
            using (var fs = File.OpenWrite(localPath)) {
                AzureStorage.VisitedLinksContainer.GetBlockBlobReference("lastsave").DownloadToStream(fs);
            }

            using (StreamReader stream = new StreamReader(localPath)) {
                string line = stream.ReadLine();
                while(line != null) {
                    string[] split = Regex.Split(line, " => ");
                    if (split[1].Equals("true")) {
                        HasPath(new Uri(split[0]));
                    } else {
                        AddDisallowedUrl(new Uri(split[0]));
                    }
                    line = stream.ReadLine();
                }
            }
        }
    }
}
