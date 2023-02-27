using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HuffmanDecoding {
    class Node { }

    class InnerNode : Node {
        public Node? left { get; set; }
        public Node? right { get; set; }

        public void AddChildren(FileStream reader, byte[] buffer) {           
            reader.Read(buffer, 0, 8);
            if (buffer[0] % 2 == 0) {
                this.left = new InnerNode();
                if (this.left is InnerNode innerLeft) innerLeft.AddChildren(reader, buffer);       
            }
            else this.left = new LeafNode { symbol = buffer[7], freq = Program.ComputeFreq(buffer) };       
            
            reader.Read(buffer, 0, 8);
            if (buffer[0] % 2 == 0) {
                this.right = new InnerNode();
                if (this.right is InnerNode rightLeft) rightLeft.AddChildren(reader, buffer);
            }
            else this.right = new LeafNode { symbol = buffer[7], freq = Program.ComputeFreq(buffer) };
        }
    }

    class LeafNode : Node {
        public byte symbol { get; set; }
        public ulong freq { get; set; }
    }

    class Program {
        public static ulong ComputeFreq(byte[] buffer) {
            buffer[7] = 0;
            if (!BitConverter.IsLittleEndian) Array.Reverse(buffer);
            ulong freq = BitConverter.ToUInt64(buffer, 0);
            freq >>= 1;
            return freq;
        }
        
        public static void DecodeFile(string fileName) {
            using (var reader = new FileStream(fileName, FileMode.Open)) {
                var buffer = new byte[8];

                // Check if the beggining is valid
                var beginning = new byte[8] { 0x7B, 0x68, 0x75, 0x7C, 0x6D, 0x7D, 0x66, 0x66 };
                int count = reader.Read(buffer, 0, 8);
                bool validBeginning = true;
                if (count == 8) {
                    for (int i = 0; i < 8; i++) {
                        if (buffer[i] != beginning[i]) validBeginning = false;
                    }
                }

                // Create the root
                if (validBeginning) {
                    reader.Read(buffer, 0, 8);
                    var root = new InnerNode();
                    root.AddChildren(reader, buffer);

                    // Check if the format was correct
                    bool zeroEnding = true;
                    reader.Read(buffer, 0, 8);
                    for (int i = 0; i < 8; i++) {
                        if (buffer[i] != 0) zeroEnding = false;
                    }
                    
                    // Process the codes if the tree was correct
                    if (zeroEnding) {
                        using (var writer = new FileStream(fileName.Substring(0, fileName.Length - 5), FileMode.Create, FileAccess.Write)) {
                            int p = 0;
                            InnerNode processingNode = root;
                            List<byte> outputBuffer = new List<byte>(1024);
                            while ((count = reader.Read(buffer, 0, 8)) != 0) {
                                for (int i = 0; i < count; i++) {
                                    char[] codes = Convert.ToString(buffer[i], 2).PadLeft(8, '0').ToCharArray();
                                    Array.Reverse(codes);
                                    while (codes.Length > 0) WriteOut(writer, ref processingNode, ref p, ref codes, root, outputBuffer);
                                }
                            }
                            if (outputBuffer.Count > 0) writer.Write(outputBuffer.ToArray(), 0, outputBuffer.Count);
                        }
                    }
                    else throw new ArgumentException();
                }
                else throw new ArgumentException();
            }
        }

        public static void WriteOut(FileStream writer, ref InnerNode processingNode, ref int p, ref char[] codes, InnerNode root, List<byte> outputBuffer) {
            if (p == 8) {
                codes = new char[0];
                p = 0;
                return;
            }

            if (codes[p++] == '0') {
                if (processingNode.left is LeafNode leafNode) {
                    if (leafNode.freq > 0) {
                        outputBuffer.Add(leafNode.symbol);
                        leafNode.freq -= 1;
                    }
                    processingNode = root;
                }
                else if (processingNode.left is InnerNode innerNode) {
                    processingNode = innerNode;
                    WriteOut(writer, ref processingNode, ref p, ref codes, root, outputBuffer);
                }
            }
            else {
                if (processingNode.right is LeafNode leafNode) {
                    if (leafNode.freq > 0) {
                        outputBuffer.Add(leafNode.symbol);
                        leafNode.freq -= 1;
                    }
                    processingNode = root;
                }
                else if (processingNode.right is InnerNode innerNode) {
                    processingNode = innerNode;
                    WriteOut(writer, ref processingNode, ref p, ref codes, root, outputBuffer);
                }
            }

            if (outputBuffer.Count == 1024) {
                writer.Write(outputBuffer.ToArray(), 0, 1024);
                outputBuffer.Clear();
            }
        }
        
        public static void Main(string[] args) {
            if (args.Length == 1 && args[0].Length > 5 && args[0].Substring(args[0].Length - 5, 5) == ".huff") {
                try {
                    DecodeFile(args[0]);
                }
                catch (Exception e) when (e is FileNotFoundException || e is UnauthorizedAccessException || e is ArgumentException || e is ArgumentOutOfRangeException) {
                    Console.WriteLine("File Error");
                }
            }
            else Console.WriteLine("Argument Error");
        }
    }
}