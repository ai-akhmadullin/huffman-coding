# huffman-coding
Programs implementing Huffman coding/decoding algorithms.

In particular, `huffman-coder` constructs a Huffman tree for a specific file and uses it for producing a compressed version of the file. The program will receive the name of the file in the form of a single command-line argument, read all data from the input file and then proceed with the Huffman tree building and compression. The compressed file will be written to a binary file whose name is gained by adding the .huff extension to the input file name. 

If the program does not receive exactly one argument, `Argument Error` will be printed to standard output. In case of problems encountered when opening (e.g. the file does not exist, insufficient access rights) or reading the file, the program will print `File Error`. The input file can have any format (i.e. it can be a binary file).

Finally, `huffman-decoder` decodes the file that was compressed by `huffman-coder`.
