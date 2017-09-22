﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ToolGood.Words
{
    public class StringSearchEx
    {
        #region Class
        class TrieNode
        {
            internal bool End;
            internal List<int> Results { get; private set; }
            internal Dictionary<char, TrieNode> m_values;
            internal Dictionary<char, TrieNode> merge_values;
            private uint minflag = uint.MaxValue;
            private uint maxflag = uint.MinValue;
            internal int Position;
            internal int Next;
            private int Count;

            public TrieNode()
            {
                m_values = new Dictionary<char, TrieNode>();
                merge_values = new Dictionary<char, TrieNode>();
                Results = new List<int>();
            }

            public bool TryGetValue(char c, out TrieNode node)
            {
                if (minflag <= (uint)c && maxflag >= (uint)c) {
                    return m_values.TryGetValue(c, out node);
                }
                node = null;
                return false;
            }

            public TrieNode Add(char c)
            {
                TrieNode node;

                if (m_values.TryGetValue(c, out node)) {
                    return node;
                }

                if (minflag > c) { minflag = c; }
                if (maxflag < c) { maxflag = c; }

                node = new TrieNode();
                m_values[c] = node;
                Count++;
                return node;
            }

            public void SetResults(int text)
            {
                if (End == false) {
                    End = true;
                }
                Results.Add(text);
            }

            public void Merge(TrieNode node, Dictionary<TrieNode, TrieNode> links)
            {
                if (node.End) {
                    if (End == false) {
                        End = true;
                    }
                    foreach (var item in node.Results) {
                        Results.Add(item);
                    }
                }

                foreach (var item in node.m_values) {
                    if (m_values.ContainsKey(item.Key) == false) {
                        if (minflag > item.Key) { minflag = item.Key; }
                        if (maxflag < item.Key) { maxflag = item.Key; }
                        if (merge_values.ContainsKey(item.Key) == false) {
                            merge_values[item.Key] = item.Value;
                            Count++;
                        }
                    }
                }
                TrieNode node2;
                if (links.TryGetValue(node, out node2)) {
                    Merge(node2, links);
                }
            }

            public int Rank()
            {
                List<int> seats = new List<int>();//占位
                int maxCount = 1;
                int start = 1;
                bool[] has = new bool[GetMaxLength()];
                has[0] = true;

                Rank(ref maxCount, ref start, seats, has);
                return maxCount;
            }

            private int GetMaxLength()
            {
                var count = m_values.Count + merge_values.Count;
                count = count * 5;
                foreach (var item in m_values) {
                    count += item.Value.GetMaxLength();
                }
                return count;
            }

            private void Rank(ref int maxCount, ref int start, List<int> seats, bool[] has)
            {
                if (maxflag == 0) return;
                var keys = m_values.Select(q => q.Key).ToList();
                keys.AddRange(merge_values.Select(q => q.Key).ToList());

                while (has[start]) { start++; }
                for (int i = start; i < has.Length; i++) {
                    if (has[i] == false) {
                        var isok = true;
                        foreach (var item in keys) {
                            if (has[i - minflag + item]) { isok = false; break; }
                        }
                        if (isok) {
                            var next = i - (int)minflag;
                            if (next < 0) continue;
                            if (seats.Contains(next)) continue;
                            SetSeats(next, ref maxCount, seats, has);
                            break;
                        }
                    }
                }

                var keys2 = m_values.OrderByDescending(q => q.Value.Count);
                foreach (var key in keys2) {
                    key.Value.Rank(ref maxCount, ref start, seats, has);
                }
            }

            private void SetSeats(int next, ref int maxCount, List<int> seats, bool[] has)
            {
                Next = next;
                seats.Add(next);

                foreach (var item in merge_values) {
                    var position = next + item.Key;
                    has[position] = true;
                    if (maxCount <= position) {
                        maxCount = position;
                    }
                }

                foreach (var item in m_values) {
                    item.Value.Position = next + item.Key;
                    has[item.Value.Position] = true;
                    if (maxCount <= item.Value.Position) {
                        maxCount = item.Value.Position;
                    }
                }

            }

        }
        #endregion
        private string[] _keywords;
        private int[][] _guides;
        private int[] _key;
        private int[] _next;
        private int[] _check;
        private int[] _dict;

        public List<string> FindAll(string text)
        {
            List<string> root = new List<string>();
            var p = 0;

            foreach (char t1 in text) {
                var t = (char)_dict[t1];
                if (t == 0) {
                    p = 0;
                    continue;
                }
                var next = _next[p] + t;
                bool find = _key[next] == t;
                if (find == false && p != 0) {
                    p = 0;
                    next = _next[0] + t;
                    find = _key[next] == t;
                }
                if (find) {
                    var index = _check[next];
                    if (index > 0) {
                        foreach (var item in _guides[index]) {
                            root.Add(_keywords[item]);
                        }
                    }
                    p = next;
                }
            }
            return root;
        }

        public string FindFirst(string text)
        {
            var p = 0;
            foreach (char t1 in text) {
                var t = (char)_dict[t1];
                if (t == 0) {
                    p = 0;
                    continue;
                }
                var next = _next[p] + t;
                if (_key[next] == t) {
                    var index = _check[next];
                    if (index > 0) {
                        return _keywords[_guides[index][0]];
                    }
                    p = next;
                } else {
                    p = 0;
                    next = _next[p] + t;
                    if (_key[next] == t) {
                        var index = _check[next];
                        if (index > 0) {
                            return _keywords[_guides[index][0]];
                        }
                        p = next;
                    }
                }
            }
            return null;
        }

        public bool ContainsAny(string text)
        {
            var p = 0;
            foreach (char t1 in text) {
                var t = (char)_dict[t1];
                if (t == 0) {
                    p = 0;
                    continue;
                }
                var next = _next[p] + t;
                if (_key[next] == t) {
                    if (_check[next] > 0) { return true; }
                    p = next;
                } else {
                    p = 0;
                    next = _next[p] + t;
                    if (_key[next] == t) {
                        if (_check[next] > 0) { return true; }
                        p = next;
                    }
                }
            }
            return false;
        }

        public string Replace(string text, char replaceChar = '*')
        {
            StringBuilder result = new StringBuilder(text);

            var p = 0;

            for (int i = 0; i < text.Length; i++) {
                var t = (char)_dict[text[i]];
                if (t == 0) {
                    p = 0;
                    continue;
                }
                var next = _next[p] + t;
                bool find = _key[next] == t;
                if (find == false && p != 0) {
                    p = 0;
                    next = _next[p] + t;
                    find = _key[next] == t;
                }
                if (find) {
                    var index = _check[next];
                    if (index > 0) {
                        var maxLength = _keywords[_guides[index][0]].Length;
                        var start = i + 1 - maxLength;
                        for (int j = start; j <= i; j++) {
                            result[j] = replaceChar;
                        }
                    }
                    p = next;
                }
            }
            return result.ToString();
        }

        #region Save
        public void Save(string fileName)
        {
            var fs = File.Open(fileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(_keywords.Length);
            foreach (var item in _keywords) {
                bw.Write(item);
            }

            List<int> guideslist = new List<int>();
            guideslist.Add(_guides.Length);
            foreach (var guide in _guides) {
                guideslist.Add(guide.Length);
                foreach (var item in guide) {
                    guideslist.Add(item);
                }
            }
            var bs = IntArrToByteArr(guideslist.ToArray());
            bw.Write(bs.Length);
            bw.Write(bs);

            bs = IntArrToByteArr(_key);
            bw.Write(bs.Length);
            bw.Write(bs);

            bs = IntArrToByteArr(_next);
            bw.Write(bs.Length);
            bw.Write(bs);

            bs = IntArrToByteArr(_check);
            bw.Write(bs.Length);
            bw.Write(bs);

            bs = IntArrToByteArr(_dict);
            bw.Write(bs.Length);
            bw.Write(bs);

            bw.Close();
            fs.Close();
        }

        private byte[] IntArrToByteArr(int[] intArr)
        {
            int intSize = sizeof(int) * intArr.Length;
            byte[] bytArr = new byte[intSize];
            //申请一块非托管内存  
            IntPtr ptr = Marshal.AllocHGlobal(intSize);
            //复制int数组到该内存块  
            Marshal.Copy(intArr, 0, ptr, intArr.Length);
            //复制回byte数组  
            Marshal.Copy(ptr, bytArr, 0, bytArr.Length);
            //释放申请的非托管内存  
            Marshal.FreeHGlobal(ptr);
            return bytArr;
        }

        #endregion

        #region Load
        public void Load(string filePath)
        {
            var fs = File.OpenRead(filePath);
            BinaryReader br = new BinaryReader(fs);

            var length = br.ReadInt32();
            _keywords = new string[length];
            for (int i = 0; i < length; i++) {
                _keywords[i] = br.ReadString();
            }

            length = br.ReadInt32();
            var bs = br.ReadBytes(length);
            using (MemoryStream ms = new MemoryStream(bs)) {
                BinaryReader b = new BinaryReader(ms);
                var length2 = b.ReadInt32();
                _guides = new int[length2][];
                for (int i = 0; i < length2; i++) {
                    var length3 = b.ReadInt32();
                    _guides[i] = new int[length3];
                    for (int j = 0; j < length3; j++) {
                        _guides[i][j] = b.ReadInt32();
                    }
                }
            }

            length = br.ReadInt32();
            _key = ByteArrToIntArr(br.ReadBytes(length));

            length = br.ReadInt32();
            _next = ByteArrToIntArr(br.ReadBytes(length));

            length = br.ReadInt32();
            _check = ByteArrToIntArr(br.ReadBytes(length));

            length = br.ReadInt32();
            _dict = ByteArrToIntArr(br.ReadBytes(length));

            br.Close();
            fs.Close();
        }

        private int[] ByteArrToIntArr(byte[] btArr)
        {
            int intSize = btArr.Length / sizeof(int);
            int[] intArr = new int[intSize];
            IntPtr ptr = Marshal.AllocHGlobal(btArr.Length);
            Marshal.Copy(btArr, 0, ptr, btArr.Length);
            Marshal.Copy(ptr, intArr, 0, intArr.Length);
            Marshal.FreeHGlobal(ptr);
            return intArr;
        }
        #endregion

        #region SetKeywords
        public void SetKeywords(List<string> keywords)
        {
            _keywords = keywords.ToArray();
            var length = CreateDict(keywords);
            var root = new TrieNode();

            for (int i = 0; i < keywords.Count; i++) {
                var p = keywords[i];
                var nd = root;
                for (int j = 0; j < p.Length; j++) {
                    nd = nd.Add((char)_dict[p[j]]);
                }
                nd.SetResults(i);
            }

            Dictionary<TrieNode, TrieNode> links = new Dictionary<TrieNode, TrieNode>();
            foreach (var item in root.m_values) {
                TryLinks(item.Value, null, links, root);
            }
            foreach (var item in links) {
                item.Key.Merge(item.Value, links);
            }

            build(root, length);
            //_root = root;
        }

        private void build(TrieNode root, int length)
        {
            length = root.Rank() + length + 1;
            _key = new int[length];
            _next = new int[length];
            _check = new int[length];
            List<int[]> guides = new List<int[]>();
            guides.Add(new int[] { 0 });

            _next[0] = root.Next;
            buildNode(root, guides);
            _guides = guides.ToArray();
        }

        private void buildNode(TrieNode node, List<int[]> guides)
        {
            foreach (var item in node.merge_values) {
                _key[item.Value.Position] = item.Key;
                _next[item.Value.Position] = item.Value.Next;
                if (item.Value.End) {
                    _check[item.Value.Position] = guides.Count;
                    int[] result = item.Value.Results.ToArray();
                    guides.Add(result);
                }
            }

            foreach (var item in node.m_values) {
                _key[item.Value.Position] = item.Key;
                _next[item.Value.Position] = item.Value.Next;
                if (item.Value.End) {
                    _check[item.Value.Position] = guides.Count;
                    int[] result = item.Value.Results.ToArray();
                    guides.Add(result);
                }
                buildNode(item.Value, guides);
            }

        }

        private void TryLinks(TrieNode node, TrieNode node2, Dictionary<TrieNode, TrieNode> links, TrieNode root)
        {
            foreach (var item in node.m_values) {
                TrieNode tn = null;
                if (node2 == null) {
                    if (root.TryGetValue(item.Key, out tn)) {
                        links[item.Value] = tn;
                    }
                } else if (node2.TryGetValue(item.Key, out tn)) {
                    links[item.Value] = tn;
                }
                TryLinks(item.Value, tn, links, root);
            }
        }
        #endregion


        #region 生成映射字典

        private int CreateDict(List<string> keywords)
        {
            Dictionary<char, int> dictionary = new Dictionary<char, int>();

            foreach (var keyword in keywords) {
                for (int i = 0; i < keyword.Length; i++) {
                    var item = keyword[i];
                    if (dictionary.ContainsKey(item)) {
                        if (i > 0)
                            dictionary[item] += 2;
                    } else {
                        dictionary[item] = i > 0 ? 2 : 1;
                    }
                }
            }
            var list = dictionary.OrderByDescending(q => q.Value).Select(q => q.Key).ToList();
            var list2 = new List<char>();
            var sh = false;
            foreach (var item in list) {
                if (sh) {
                    list2.Add(item);
                } else {
                    list2.Insert(0, item);
                }
                sh = !sh;
            }
            _dict = new int[char.MaxValue + 1];
            for (int i = 0; i < list2.Count; i++) {
                _dict[list2[i]] = i + 1;
            }
            return dictionary.Count;
        }
        #endregion

    }
}
