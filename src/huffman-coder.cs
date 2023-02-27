using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Huffman {
    class ByteCounter {
        ulong[] bytes = new ulong[256];
        public ulong[] CountBytes(string path) {
            using (FileStream fs = new FileStream(path, FileMode.Open)) {
                byte[] buffer = new byte[4096];
                int count;
                while ((count = fs.Read(buffer, 0, 4096)) != 0) {
                    for (int i = 0; i < count; i++) {
                        bytes[buffer[i]] += 1;
                    } 
                }
            }
            return bytes;
        }
    }
    class Node {
        public byte symbol { get; set; }
        public ulong freq { get; set; }
        public bool processed { get; set; }
        public Node? left { get; set; }
        public Node? right { get; set; }
    }
    class HuffmanTree {
        List<Node> nodes = new List<Node>();
        public void BuildTree(ulong[] byteCount) {
            if (byteCount.Length > 0) {    
                nodes = new List<Node>(byteCount.Length * 2);
                for (int i = 0; i < byteCount.Length; i++) {
                    if (byteCount[i] > 0) nodes.Add(new Node { symbol = (byte) i, freq = byteCount[i] });
                }
                bool done = false;
                while (!done) {
                    (Node? leftNode, Node? rightNode) = GetTwoSmallestNodes();
                    if (leftNode is null || rightNode is null) done = true;
                    else {
                        Node parent = new Node { 
                            freq = leftNode.freq + rightNode.freq, 
                            left = leftNode, 
                            right = rightNode,
                        };
                        nodes.Add(parent);              
                    }
                }
            }
        }
        (Node?, Node?) GetTwoSmallestNodes() {
            Node? left = FindProperNode(nodes[0]);
            Node? right = FindProperNode(nodes[0]);
            if (left is not null && right is not null) return (left, right);
            else return (null, null);
        }
        Node? FindProperNode(Node nextNode) {
            foreach (Node node in nodes) {
                if (!node.processed) {
                    nextNode = node;
                    break;
                }
            }
            if (!nextNode.processed) {
                foreach (Node node in nodes) {
                    if (!node.processed && OtherNodeIsSmaller(nextNode, node)) nextNode = node;
                }
                nextNode.processed = true;
                return nextNode;
            }
            else return null;
        }
        bool OtherNodeIsSmaller(Node first, Node second) {
            if (second.freq == first.freq) {
                if (first.left is null && second.left is null) {
                    return second.symbol < first.symbol;
                }
                else if (first.left is not null && second.left is null) {
                    return true;
                }
                else if (first.left is null && second.left is not null) {
                    return false;
                }
                return false;
            }
            return second.freq < first.freq;
        }

        List<bool> path = new List<bool>();
        List<bool>[] paths = new List<bool>[256];
        public List<bool>[] EncodeNodesToFile(FileStream fs) {
            if (nodes.Count > 0) {
                WriteOut(nodes[nodes.Count - 1], fs);
                byte[] nulls = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                fs.Write(nulls, 0, nulls.Length);
            }
            return paths;
        }
        void WriteOut(Node node, FileStream fs) {
            byte[] byteArray = BitConverter.GetBytes(node.freq << 1);
            if (node.left is not null) {
                byteArray[7] = 0x00;
                fs.Write(byteArray, 0, byteArray.Length);
                path.Add(false);
                WriteOut(node.left, fs);
                path.RemoveAt(path.Count - 1);
                if (node.right is not null) {
                    path.Add(true);
                    WriteOut(node.right, fs);
                    path.RemoveAt(path.Count - 1);
                }
            }
            else {
                byteArray[0] |= 1;
                byteArray[7] = BitConverter.GetBytes(node.symbol)[0];
                fs.Write(byteArray, 0, byteArray.Length);
                paths[node.symbol] = new List<bool>(path);
            }
        }
    }
    class Huffman {
        HuffmanTree tree = new HuffmanTree();
        public Huffman(ulong[] byteCount) {
            tree.BuildTree(byteCount);
        }

        public void Encode(string fileName) {
            using (FileStream fsReader = new FileStream(fileName, FileMode.Open)) {
                using (FileStream fsWriter = new FileStream(fileName + ".huff", FileMode.Create)) {
                    byte[] header = new byte[] { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 };
                    fsWriter.Write(header, 0, 8);
                    List<bool>[] paths = tree.EncodeNodesToFile(fsWriter);
                    
                    var bytesBuffer = new byte[4096];
                    var bitsBuffer = new bool[8];
                    List<bool> path;
                    int count;
                    byte outputByte;
                    int p = 0;
                    while ((count = fsReader.Read(bytesBuffer, 0, 4096)) != 0) {
                        for (int i = 0; i < count; i++) {
                            path = paths[bytesBuffer[i]];
                            for (int j = 0; j < path.Count; j++) {
                                bitsBuffer[p] = path[j];
                                p++;
                                if (p == 8) {
                                    outputByte = (byte) EncodeBools(ReverseBits((bitsBuffer)));
                                    fsWriter.WriteByte(outputByte);
                                    p = 0;
                                }
                            }
                        }
                    }
                    if (p > 0) {
                        int n = p;
                        for (int i = 0; i < 8 - n; i++) {
                            bitsBuffer[p] = false;
                            p++;
                        }
                        outputByte = (byte) EncodeBools(ReverseBits((bitsBuffer)));
                        fsWriter.WriteByte(outputByte);
                    }
                }
            }
        }

        bool[] ReverseBits(bool[] bits) {
            bool[] reversed = new bool[8];
            for (int i = 0; i < 8; i++) {
                reversed[i] = bits[7 - i];
            }
            return reversed;
        }

        int EncodeBools(bool[] bits) {
            int result = 0; 
            for (int i = 0; i < 8; i++) { 
                result <<= 1; 
                if (bits[i]) result += 1;
            } 
            return result; 
        }
    }
    class Program {
        static void Main(string[] args) {
            if (args.Length == 1) {
                var byteCounter = new ByteCounter();
                try {
                    var byteCount = byteCounter.CountBytes(args[0]);
                    var huff = new Huffman(byteCount);
                    huff.Encode(args[0]);
                }
                catch (Exception e) when (e is FileNotFoundException || e is UnauthorizedAccessException || e is ArgumentException) {
                    Console.WriteLine("File Error");
                    return;
                }
            }
            else Console.WriteLine("Argument Error");
        }
    }
}