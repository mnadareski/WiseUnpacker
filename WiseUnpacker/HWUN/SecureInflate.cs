/*
   Unit name ...... SINFLATE (Secure Inflate)
   Version ........ 1.01
   Date ........... 2002-01-14
   Creator ........ J�germeister Markus (jaegermeister.markus@gmx.de)

   Unit purpose ... This unit is intended to unpack data compressed with
                    the DEFLATE algorithm (used in PKZIP, GZIP and some
                    more) by easy to use procedure/function calls and
                    easy to understand sourcecode structures. Anyway, I'd
                    recommend to read the DEFLATE specifications which
                    can be found at the official GZIP website.
                        Without having read it first you probably won't
                    understand how this unit is working (except you know
                    a lot about huffman trees and LZ77 compression).
                        Although DEFLATE uses a combination of LZ77 with
                    huffman trees there are some special rules and of
                    course the structure how tree data are saved in DE-
                    FLATED data.
                        The difference between SINFLATE and INFLATE (by
                    Oliver Fromme) is that SINFLATE will take the main
                    program and the user care of errors, statistics and
                    of course how the unit works. This will result in
                    a little speed loss, but SINFLATE will NEVER cause
                    some runtime errors or will access memory outside
                    all allocated areas (useful for Virtual Pascal and
                    other than Turbo Pascal compilers). Apart from that
                    I tried to use understandable name definitions for
                    procedures/functions/variables and not two-character
                    names where nobody else like the programmer knows
                    what's their purpose.
                        Another thing is that the SI_Inflate procedure
                    will only read as much bits as needed, this is
                    a little bit slower (no lookup!) but the main
                    program won't need to care about if the decompressor
                    has read one or more bytes which don't belong to
                    the compressed data, so no more searching for
                    signatures or whatever for the main program.


   Conventions .... Variables which point to a certain type of data
                    get a lowercase "p" in front of the name,
                    types get a lowercase "t" in front.
                    Vars/Procs/Funcs used non-internally usable have
                    an "SI_" in front and are written uppercase.

   Initialization . Before decompressing some data you will need to do
                    two things:
                    1. Submit an SI_READ function which reads a byte
                           from the compressed data stream
                           (SI_READ:=MyRead;)
                    2. Submit an SI_WRITE procedure which takes a
                           datum X of type word as the first parameter
                           and writes or does something else with
                           the first X bytes of the array SI_WIND^,
                           which contains the uncompressed data.
                           Usually the word X will be $8000 until the
                           whole data has been decompressed.
                           (SI_WRITE:=MyWrite;)
                    An example can be seen some lines below! As you can
                    see in comparison with Oliver Fromme's inflate.pas
                    with this unit the user does not need to allocate
                    memory for the sliding window. As that amount of
                    memory always is needed for the inflate process
                    each time the SI_INFLATE is called it's first
                    reserved and after inflate is complete, it's
                    given free again.

   Copyrights ..... This unit and it's sourcecode aren't copyrighted.
                    You can do what you want with both, but you're welcome
                    if you mail me any bugs you've found, made improvements
                    or something else to the unit. Credits won't be missing!
*/

/*
EXAMPLE MAIN PROGRAM USING THIS UNIT
  ------------------------------------

  uses SI_INFLATE;

  var input,output:file;
      inputbuffer:array[$0000..$1fff] of byte;  { 8kb read buffer for speed }
      inputbufferposition:word;
      inputbuffersize:word;

  procedure MyReadInitialize;
    begin
      inputbufferposition:=sizeof(inputbuffer);
    end;

  function MyRead:byte; far;        { far statement needed for Turbo Pascal }
    begin
      if inputbufferposition=sizeof(inputbuffer)    { if position is beyond }
        then begin                                      { the actual buffer }
          if filesize(input)-filepos(input)<sizeof(inputbuffer)  { read new }
            then inputbuffersize:=filesize(input)-filepos(input)    { block }
            else inputbuffersize:=sizeof(inputbuffer);        { and set the }
            inputbufferposition:=$0000;              { pointer to the start }
          blockread(input,inputbuffer,inputbuffersize);
        end;
      if inputbufferposition=inputbuffersize      { last byte has been read }
        then begin                           { but inflate still wants more }
          SI_BREAK:=TRUE;                  { then stop inflate by this line }
        end else begin
          MyRead:=inputbuffer[inputbufferposition];
          inc(inputbufferposition);
        end;
    end;

  procedure MyWrite(amount:word); far;
    begin                                { The ^ is needed for the case you }
      blockwrite(output,SI_WINDOW^,amount); { aren't familiar with pointers }
    end;

  begin
    MyReadInitialize;{ forces MyRead to read a new block for the first time }
    SI_READ:=MyRead;                  { assign your read function (NEEDED!) }
    SI_WRITE:=MyWrite;              { assign your write procedure (NEEDED!) }
    assign(input,'INPUT.DAT');               { standard file opening... :)) }
    assign(output,'OUTPUT.DAT');
    reset(input,1);
    rewrite(output,1);
    write('Inflating INPUT.DAT to OUTPUT.DAT... ');
    SI_INFLATE;                                               { Run inflate }
    writeln(SI_GETERROR);                   { SI_GETERROR returns a string! }
    close(input);
    close(output);
  end.
*/

namespace WiseUnpacker.HWUN
{
    internal abstract class SecureInflate
    {
        /// <summary>
        /// The break variable, set it to TRUE if you want to break the
        /// inflate process at any time (use it in SI_READ or SI_WRITE)
        /// </summary>
        protected bool SI_BREAK = false;

        /// <summary>
        /// The type for the uncompressed data array ("sliding" window), must
        /// be at least $8000 bytes long!
        /// 
        /// This is the type'd pointer variable for the sliding window, it must be
        /// allocated and deallocated by the main program
        /// </summary>
        protected byte[] SI_WINDOW = new byte[0x8000];

        /// <summary>
        /// The actual window position when increased from $7fff to $8000 then
        /// SI_WRITE($8000) is called and it's set back to $0000.
        /// Don't change it during the inflate process, just read it for
        /// further information on the process of inflate!!!
        /// </summary>
        private ushort SI_POSITION;

        /// <summary>
        /// This is the error indicator which is set when breaking an inflate
        /// process or when an error occured. The following value table let's you
        /// conclude what have been going wrong:
        /// SI_ERROR (word bitform hi-lo)  ee--|-----|--at|hhbb
        /// e = error occured          0   = no error
        ///                            1   = error during inflate
        ///                            2   = user break during inflate
        /// a = block header error     0   = no error
        ///                            1   = header corrupt (only types 0/2)
        ///                                   on block type 0 this means
        ///                                   that the checksum is wrong,
        ///                                   on block type 2 this means
        ///                                   one of the huffman tables
        ///                                   it wrong, check t and h
        ///  t = huffman error type     0   = incomplete
        ///                             1   = full
        ///  h = huffman error tree     0   = no error
        ///                             1   = codelength tree (error)
        ///                             2   = literal/lengthcode tree (error)
        ///                             3   = distance tree (error)
        ///  b = actual block type      0-2 = block type b
        ///                             3   = illegal block type (error)
        /// </summary>
        protected ushort SI_ERROR;

        /// <summary>
        /// Returns a text describing the occured error
        /// </summary>
        public string SI_GETERROR()
        {
            string s = string.Empty;
            if ((SI_ERROR & 0xc000) == 0x0000)
            {
                s = "okay";
            }
            else if ((SI_ERROR & 0x4000) == 0x4000)
            {
                s = "user break";
            }
            else if ((SI_ERROR & 0x0020) == 0x0020)
            {
                s = "block header corrupt";
                if ((SI_ERROR & 0x000c) != 0x0000)
                {
                    s += ", ";
                    if ((SI_ERROR & 0x000c) == 0x0004) s += "lengthcode tree";
                    else if ((SI_ERROR & 0x000c) == 0x0008) s += "literal tree";
                    else if ((SI_ERROR & 0x000c) == 0x000c) s += "distance tree";
                    s += " is ";
                    if ((SI_ERROR & 0x0010) == 0x0000) s += "incomplete"; else s += "full";
                }
            }

            return s;
        }

        private const ushort SI_USERBREAK = 0x4000;

        #region DEFLATE STATIC TABLES block

        private ushort[] LengthcodeValueOffset = new ushort[0x11e]; // [$101..$11d]
        private byte[] LengthcodeValueExtrabits = new byte[0x11e]; // [$101..$11d]
        private ushort[] DistancecodeValueOffset = new ushort[0x01e]; // [$000..$01d]
        private byte[] DistancecodeValueExtrabits = new byte[0x01e]; // [$000..$01d]

        /// <summary>
        /// Allocates and generates the tables:
        ///   - LengthcodeValueOffset
        ///   - LengthcodeValueExtrabits
        ///   - DistancecodeValueOffset
        ///   - DistancecodeValueExtrabits
        /// </summary>
        private void AllocateStaticTables()
        {
            ushort LengthcodeExtrabits = 0;
            ushort DistancecodeExtrabits = 0;
            ushort LengthcodeOffset = 0x0003;
            ushort DistancecodeOffset = 0x0001;

            LengthcodeValueOffset[0x11d] = 0x0102;
            LengthcodeValueExtrabits[0x11d] = 0x0000;
            for (int pos = 0x00; pos <= 0x1d; pos++)
            {
                // increase number of extra bits for length code table every 4th value
                if (pos > 0x08 && (pos & 0x03) == 0x00)
                    LengthcodeExtrabits++;

                // increase number of extra bits for distance code table every 2nd value
                if (pos > 0x04 && (pos & 0x01) == 0x00)
                    DistancecodeExtrabits++;

                // for pos<=$1c put value entry into length code table
                if (pos <= 0x1b) LengthcodeValueOffset[pos + 0x101] = LengthcodeOffset;
                if (pos <= 0x1b) LengthcodeValueExtrabits[pos + 0x101] = (byte)LengthcodeExtrabits;

                // put value entry into distance code table
                DistancecodeValueOffset[pos] = DistancecodeOffset;
                // write(hexa(DistancecodeOffset,4),'/',hexa(DistancecodeExtrabits,1),'  ');
                DistancecodeValueExtrabits[pos] = (byte)DistancecodeExtrabits;

                // increase length and distance code values
                LengthcodeOffset += (ushort)(0x0001 << LengthcodeExtrabits);
                DistancecodeOffset += (ushort)(0x0001 << DistancecodeExtrabits);
            }
        }

        private void DeAllocateStaticTables()
        {
            LengthcodeValueOffset = new ushort[0x11e];
            LengthcodeValueExtrabits = new byte[0x11e];
            DistancecodeValueOffset = new ushort[0x01e];
            DistancecodeValueExtrabits = new byte[0x01e];
        }

        #endregion

        #region DEFLATE HUFFMAN TREE block

        /// <summary>
        /// The huffman-tree node structure, differs from common specifications
        /// </summary>
        public class HuffmanNode
        {
            /// <summary>
            /// The value of the acutal alphabet entry
            /// </summary>
            public ushort value;

            /// <summary>
            /// The pointers to the children for both directions,
            /// if NIL then there's no child for the actual direction
            /// </summary>
            public HuffmanNode?[] next = new HuffmanNode[2];

            /// <summary>
            /// Tells us if the nodes in both directions either are
            /// end nodes or not
            /// </summary>
            public bool[] endnode = new bool[2];
        }

        /// <summary>
        /// Creates a virgin huffman tree root
        /// </summary>
        private void CreateNewHuffmanTree(ref HuffmanNode? HuffmanTree)
        {
            byte direction;
            HuffmanTree = new HuffmanNode { value = 0xffff };
            for (direction = 0; direction <= 1; direction++)
            {
                HuffmanTree.next[direction] = null;
                HuffmanTree.endnode[direction] = false;
            }
        }

        /// <summary>
        /// Adds a child in direction >newdirection< to the tree and gives the child
        /// the value >newvalue<. If >newvalue< is set between $0000 and $fffe the
        /// child is seen as an endnode, if $ffff the child is just another node with
        /// two children.
        /// </summary>
        private void AddNewChildToHuffmanNode(HuffmanNode HuffmanNode, byte newdirection, ushort newvalue)
        {
            HuffmanNode newnode;
            byte direction;
            newnode = new HuffmanNode { value = newvalue };
            for (direction = 0; direction <= 1; direction++)
            {
                newnode.next[direction] = null;
                newnode.endnode[direction] = false;
            }

            if (newvalue < 0xffff)
                HuffmanNode.endnode[newdirection] = true;
            HuffmanNode.next[newdirection] = newnode;
        }

        /// <summary>
        /// Tries to add a new codelength to the tree, but beware:
        /// It only returns FALSE if no space is found for a value! This means e.g.
        /// that if you have a complete huffman tree with all codelengths=8 and
        /// you try to add a value with codelength<8 it will return TRUE and writes
        /// the code as the bitstream 0000000 (always left direction).
        /// So you have to fulfill the following rules:
        ///   - always add the values with short codelengths before you add
        ///         the values with codelengths which are longer
        ///   - to test if the tree is complete try to add another value
        ///         with the highest codelength you've used yet for the tree,
        ///         if TRUE is the result, the tree is incomplete,
        ///         if FALSE is the result, the tree is complete
        /// </summary>
        private bool AddNewCodeToHuffmanTree(HuffmanNode HuffmanNode, byte Codelength, ushort Codevalue)
        {
            bool result;

            // impossible codelength
            if (Codelength == 0)
                return false;

            if (!HuffmanNode.endnode[0])
            {
                if (HuffmanNode.next[0] == null)
                    AddNewChildToHuffmanNode(HuffmanNode, 0, 0xffff);
                if (Codelength == 1)
                {
                    HuffmanNode.endnode[0] = true;
                    HuffmanNode.next[0]!.value = Codevalue;
                    result = true;
                }
                else
                {
                    result = AddNewCodeToHuffmanTree(HuffmanNode.next[0]!, (byte)(Codelength - 1), Codevalue);
                }
                // if result then write(0);
            }
            else
                result = false;
            if (result == false)
            {
                if (!HuffmanNode.endnode[1])
                {
                    HuffmanNode.endnode[0] = true;
                    if (HuffmanNode.next[1] == null)
                        AddNewChildToHuffmanNode(HuffmanNode, 1, 0xffff);
                    if (Codelength == 1)
                    {
                        HuffmanNode.endnode[1] = true;
                        HuffmanNode.next[1]!.value = Codevalue;
                        result = true;
                    }
                    else
                    {
                        result = AddNewCodeToHuffmanTree(HuffmanNode.next[1]!, (byte)(Codelength - 1), Codevalue);
                    }
                    // if result then write(1);
                }
                else
                {
                    result = false;
                    HuffmanNode.endnode[1] = true;
                }
            }
            return result;
        }

        private void FreeHuffmanTree(ref HuffmanNode? HuffmanNode)
        {
            if (HuffmanNode?.next[0] != null) FreeHuffmanTree(ref HuffmanNode!.next[0]!);
            if (HuffmanNode?.next[1] != null) FreeHuffmanTree(ref HuffmanNode!.next[1]!);
            HuffmanNode = null;
        }

        #endregion

        #region BITWISE READ block

        private byte BitBuffer;
        private byte BitNumber;

        /// <summary>
        /// Reads >NumberOfBits< bits from the input stream and returns it/them
        /// as a longint. This causes a little speed decrease but assures that
        /// not too much bytes are read and the caller doesn't have to calculate
        /// the byte/word/integer/whatever value by hand.
        /// </summary>
        private ushort ReadBits(byte NumberOfBits)
        {
            ushort ResultMask;
            ushort Result;

            ResultMask = 1;
            Result = 0;
            while (NumberOfBits > 0)
            {
                if (BitNumber == 8)
                {
                    BitNumber = 0;
                    BitBuffer = SI_READ();
                }
                Result += (ushort)((BitBuffer & 0x01) * ResultMask);
                ResultMask = (ushort)(ResultMask << 0x01);
                BitBuffer = (byte)(BitBuffer >> 0x01);
                BitNumber++;
                NumberOfBits--;
            }
            return Result;
        }

        /// <summary>
        /// Reads one bits from the input stream and returns it/them
        /// as a byte.
        /// </summary>
        private byte ReadBit()
        {
            if (BitNumber == 8)
            {
                BitNumber = 0;
                BitBuffer = SI_READ();
            }
            byte ReadBit = (byte)(BitBuffer & 0x01);
            BitBuffer = (byte)(BitBuffer >> 0x01);
            BitNumber++;
            return ReadBit;
        }

        #endregion

        #region HUFFMAN-TREE-USING block

        private static byte[] CodelengthOrder = new byte[0x13]
        {
            0x10,0x11,0x12,0x00,0x08,0x07,0x09,0x06,0x0a,0x05,0x0b,0x04,0x0c,0x03,0x0d,0x02,
            0x0e,0x01,0x0f
        };

        /// <summary>
        /// where $000-$0ff is for literals, $100 for the end of block sign,
        /// $101-$11d for the length codes and $11e-$13b for the distance codes
        /// </summary>
        private byte[] AlphabetCodelength = new byte[0x13e];

        private HuffmanNode? LiteralHuffmanTree = null;

        private HuffmanNode? DistanceHuffmanTree = null;

        private byte[] CodelengthCodelength = new byte[0x013];

        private HuffmanNode? CodelengthHuffmanTree = null;

        private void CreateStaticCodeTrees()
        {
            ushort Value;
            CreateNewHuffmanTree(ref LiteralHuffmanTree);
            for (Value = 0x100; Value <= 0x117; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree!, 7, Value);
            for (Value = 0x000; Value <= 0x08f; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree!, 8, Value);
            for (Value = 0x118; Value <= 0x11f; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree!, 8, Value);
            for (Value = 0x090; Value <= 0x0ff; Value++) AddNewCodeToHuffmanTree(LiteralHuffmanTree!, 9, Value);
            CreateNewHuffmanTree(ref DistanceHuffmanTree);
            for (Value = 0x00; Value <= 0x1f; Value++) AddNewCodeToHuffmanTree(DistanceHuffmanTree!, 5, Value);
        }

        private void FreeStaticCodeTrees()
        {
            FreeHuffmanTree(ref LiteralHuffmanTree);
            FreeHuffmanTree(ref DistanceHuffmanTree);
        }

        private ushort DecodeValue(HuffmanNode ActualNode)
        {
            do
            {
                ActualNode = ActualNode.next[ReadBit()]!;
            } while (ActualNode.value != 0xffff || SI_BREAK);
            if (SI_BREAK)
                SI_ERROR = 0x4000;
            return ActualNode.value;
        }

        private void ReadDynamicCodeTrees(ushort CodelengthNumber, ushort LiteralNumber, ushort DistanceNumber)
        {
            ushort CodeValue;
            byte Lengthcode, CodeLength;
            ushort RepeatAmount;
            byte RepeatValue = 0x00;
            byte ActualCodeLength, HighestCodeLength, Highest2;
            bool BuildSuccess = false;

            // Read codelength codelengths (first tree)
            CodelengthCodelength = new byte[0x013];
            HighestCodeLength = 0x00;
            for (CodeValue = 0x00; CodeValue <= CodelengthNumber - 0x01; CodeValue++)
            {
                if (!SI_BREAK)
                {
                    ActualCodeLength = (byte)ReadBits(3);
                    if (ActualCodeLength > HighestCodeLength)
                        HighestCodeLength = ActualCodeLength;
                    CodelengthCodelength[CodelengthOrder[CodeValue]] = ActualCodeLength;
                }
            }
            if (SI_BREAK)
            {
                SI_ERROR = SI_USERBREAK;
                CodelengthCodelength = [];
                return;
            }

            // Build up tree
            CreateNewHuffmanTree(ref CodelengthHuffmanTree);
            for (CodeLength = 0x01; CodeLength <= 0x0f; CodeLength++)
            {
                for (CodeValue = 0x00; CodeValue <= 0x12; CodeValue++)
                {
                    if (CodeLength == CodelengthCodelength[CodeValue])
                        BuildSuccess = AddNewCodeToHuffmanTree(CodelengthHuffmanTree!, CodeLength, CodeValue);
                }
            }
            CodelengthCodelength = [];
            if (BuildSuccess)
            {
                if (HighestCodeLength == 0)
                    HighestCodeLength++;
                BuildSuccess = !AddNewCodeToHuffmanTree(CodelengthHuffmanTree!, HighestCodeLength, 0);
                if (!BuildSuccess)
                {
                    SI_ERROR = (ushort)((SI_ERROR & 0x7fc3) + 0x8024);
                    return;
                }
            }
            else
            {
                SI_ERROR = (ushort)((SI_ERROR & 0x7fc3) + 0x8034);
                return;
            }

            // Real literal + distance (alphabet) codelengths
            AlphabetCodelength = new byte[0x13e];
            RepeatAmount = 0;
            HighestCodeLength = 0;
            Highest2 = 0;
            for (CodeValue = 0x000; CodeValue <= LiteralNumber + DistanceNumber - 0x01; CodeValue++)
            {
                if (!SI_BREAK)
                {
                    if (RepeatAmount == 0)
                    {
                        Lengthcode = (byte)DecodeValue(CodelengthHuffmanTree!);
                        if (Lengthcode < 0x10)
                        {
                            AlphabetCodelength[CodeValue] = Lengthcode;
                            if (CodeValue < LiteralNumber)
                            {
                                if (Lengthcode > HighestCodeLength)
                                    HighestCodeLength = Lengthcode;
                            }
                            else
                            {
                                if (Lengthcode > Highest2)
                                    Highest2 = Lengthcode;
                            }
                        }
                        else if (Lengthcode == 0x10)
                        {
                            RepeatAmount = (ushort)(0x02 + ReadBits(2));
                            RepeatValue = AlphabetCodelength[CodeValue - 0x01];
                            AlphabetCodelength[CodeValue] = RepeatValue;
                        }
                        else if (Lengthcode == 0x11)
                        {
                            RepeatAmount = (ushort)(0x02 + ReadBits(3));
                            RepeatValue = 0x00;
                            AlphabetCodelength[CodeValue] = RepeatValue;
                        }
                        else if (Lengthcode == 0x12)
                        {
                            RepeatAmount = (ushort)(0x0a + ReadBits(7));
                            RepeatValue = 0x00;
                            AlphabetCodelength[CodeValue] = RepeatValue;
                        }
                        if (SI_BREAK)
                        {
                            SI_ERROR = SI_USERBREAK;
                            AlphabetCodelength = [];
                            FreeHuffmanTree(ref CodelengthHuffmanTree);
                            return;
                        }
                    }
                    else
                    {
                        AlphabetCodelength[CodeValue] = RepeatValue;
                        RepeatAmount--;
                    }
                }
            }

            // Free huffman tree for codelength resolving (not needed anymore)
            FreeHuffmanTree(ref CodelengthHuffmanTree);

            // Build up literal tree
            CreateNewHuffmanTree(ref LiteralHuffmanTree);
            for (CodeLength = 0x01; CodeLength <= 0x0f; CodeLength++)
            {
                for (CodeValue = 0x000; CodeValue <= LiteralNumber - 1; CodeValue++)
                {
                    if (CodeLength == AlphabetCodelength[CodeValue])
                        BuildSuccess = AddNewCodeToHuffmanTree(LiteralHuffmanTree!, CodeLength, CodeValue);
                }
            }
            if (BuildSuccess)
            {
                if (HighestCodeLength == 0)
                    HighestCodeLength++;
                BuildSuccess = !AddNewCodeToHuffmanTree(LiteralHuffmanTree!, HighestCodeLength, 0);
                if (!BuildSuccess)
                {
                    SI_ERROR = (ushort)((SI_ERROR & 0x7fc3) + 0x8028);
                    FreeHuffmanTree(ref LiteralHuffmanTree);
                    AlphabetCodelength = [];
                    return;
                }
            }
            else
            {
                SI_ERROR = (ushort)((SI_ERROR & 0x7fc3) + 0x8038);
                FreeHuffmanTree(ref LiteralHuffmanTree);
                AlphabetCodelength = [];
                return;
            }

            // Build up distance tree
            CreateNewHuffmanTree(ref DistanceHuffmanTree);
            for (CodeLength = 0x01; CodeLength <= 0x0f; CodeLength++)
            {
                for (CodeValue = 0x00; CodeValue <= DistanceNumber - 1; CodeValue++)
                {
                    if (CodeLength == AlphabetCodelength[CodeValue + LiteralNumber])
                        BuildSuccess = AddNewCodeToHuffmanTree(DistanceHuffmanTree!, CodeLength, CodeValue);
                }
            }
            if (BuildSuccess)
            {
                if (Highest2 == 0)
                    Highest2++;
                BuildSuccess = !AddNewCodeToHuffmanTree(DistanceHuffmanTree!, Highest2, 0);
                if (!BuildSuccess)
                {
                    SI_ERROR = (ushort)((SI_ERROR & 0x7fc3) + 0x802c);
                    FreeHuffmanTree(ref DistanceHuffmanTree);
                    AlphabetCodelength = [];
                    return;
                }
            }
            else
            {
                SI_ERROR = (ushort)((SI_ERROR & 0x7fc3) + 0x803c);
                FreeHuffmanTree(ref DistanceHuffmanTree);
                AlphabetCodelength = [];
                return;
            }
            AlphabetCodelength = [];
        }

        private void FreeDynamicCodeTrees()
        {
            FreeHuffmanTree(ref LiteralHuffmanTree);
            FreeHuffmanTree(ref DistanceHuffmanTree);
        }

        #endregion

        #region DEFLATE-BLOCK-HANDLING block

        private const ushort LiteralAlphabetLengthOffset = 0x0101;
        private const byte DistanceAlphabetLengthOffset = 0x01;
        private const byte LengthcodeAlphabetLengthOffset = 0x04;

        private void OutputByte(byte b)
        {
            SI_WINDOW[SI_POSITION] = b;
            SI_POSITION++;
            if (SI_POSITION == 0x8000)
            {
                SI_WRITE(0x8000);
                SI_POSITION = 0x000;
            }
        }

        private void CopyBytes(ushort distance, ushort length)
        {
            while (length > 0)
            {
                OutputByte(SI_WINDOW[(SI_POSITION + 0x8000 - distance) & 0x7fff]);
                // OutputByte($ff);
                length--;
            }
        }

        public abstract byte SI_READ();

        public abstract void SI_WRITE(ushort amount);

        /// <summary>
        /// Starts the inflate process
        /// </summary>
        public unsafe void SI_INFLATE()
        {
            byte LastBlock = 0, BlockType;
            ushort CodelengthNumber, LiteralNumber, DistanceNumber;
            ushort Literal = 0;
            ushort Length;
            ushort Distance;

            // Force ReadBits to read a byte first
            BitNumber = 8;

            // Set sliding window position to the start
            SI_POSITION = 0;
            SI_ERROR = 0x0000;
            AllocateStaticTables();
            SI_WINDOW = new byte[0x8000];
            do
            {
                LastBlock = ReadBit();
                BlockType = (byte)ReadBits(2);

                // writeln('LastBlock              = $',hexa(LastBlock,2));
                // write('BlockType              = $',hexa(BlockType,2),' (');
                // if (BlockType == 0)
                //     Console.WriteLine("stored");
                // else if (BlockType == 1)
                //     Console.WriteLine("static huffman");
                // else if (BlockType == 2)
                //     Console.WriteLine("dynamic huffman");
                // else
                // {
                //     Console.WriteLine("illegal");
                //     SI_ERROR = 0x8003;
                // }
                // writeln(')');

                // Decompress the specific block
                if (BlockType == 0)
                {
                    // ignore all bits till next byte
                    BitNumber = 8;
                    Length = (ushort)(SI_READ() + SI_READ() * 0x100);
                    Distance = (ushort)(SI_READ() + SI_READ() * 0x100);
                    // writeln('Length                 = $',hexa(Length,4));
                    // writeln('Length complement      = $',hexa(Distance,4));
                    if ((Length ^ Distance) != 0xffff)
                        SI_ERROR = 0x8020;
                    else
                    {
                        while (Length > 0 && (SI_ERROR & 0xc000) == 0x0000)
                        {
                            Literal = SI_READ();
                            OutputByte((byte)Literal);
                            Length--;
                        }
                    }
                }
                else if (BlockType == 1)
                {
                    CreateStaticCodeTrees();
                    if ((SI_ERROR & 0x8000) == 0x0000)
                    {
                        do
                        {
                            Literal = DecodeValue(LiteralHuffmanTree!);
                            if ((SI_ERROR & 0xc000) != 0)
                                Literal = 0x100;
                            else
                            {
                                if (Literal < 0x100)
                                    OutputByte((byte)Literal);
                                else if (Literal == 0x100)
                                {
                                    // No-op?
                                }
                                else if (Literal <= 0x11d)
                                {
                                    Length = (ushort)(LengthcodeValueOffset[Literal] + ReadBits(LengthcodeValueExtrabits[Literal]));
                                    Distance = DecodeValue(DistanceHuffmanTree!);
                                    if (Distance > 0x1d)
                                    {
                                        SI_ERROR = 0x8000;
                                        Literal = 0x100;
                                    }
                                    else
                                    {
                                        Distance = (ushort)(DistancecodeValueOffset[Distance] + ReadBits(DistancecodeValueExtrabits[Distance]));
                                        CopyBytes(Distance, Length);
                                    }
                                }
                                else
                                {
                                    SI_ERROR = 0x8000;
                                    Literal = 0x100;
                                }
                            }
                        } while (Literal != 0x100);
                        FreeStaticCodeTrees();
                    }
                    else if (BlockType == 2)
                    {
                        LiteralNumber = (ushort)(LiteralAlphabetLengthOffset + ReadBits(5));
                        DistanceNumber = (ushort)(DistanceAlphabetLengthOffset + ReadBits(5));
                        CodelengthNumber = (ushort)(LengthcodeAlphabetLengthOffset + ReadBits(4));
                        ReadDynamicCodeTrees(CodelengthNumber, LiteralNumber, DistanceNumber);
                        // writeln('LiteralNumber          = $',hexa(LiteralNumber,4));
                        // writeln('DistanceNumber         = $',hexa(DistanceNumber,4));
                        // writeln('CodelengthNumber       = $',hexa(CodelengthNumber,4));
                        // writeln('DynamicCodeTreesCheck  = $',hexa(SI_ERROR,4)+' ('+SI_GetError+')');
                        if ((SI_ERROR & 0x8000) == 0x000)
                        {
                            do
                            {
                                Literal = DecodeValue(LiteralHuffmanTree!);
                                if ((SI_ERROR & 0xc000) != 0x0000)
                                    Literal = 0x100;
                                else
                                {
                                    if (Literal < 0x100)
                                        OutputByte((byte)Literal);
                                    else if (Literal == 0x100)
                                    {
                                        // No-op?
                                    }
                                    else if (Literal <= 0x11d)
                                    {
                                        Length = (ushort)(LengthcodeValueOffset[Literal] + ReadBits(LengthcodeValueExtrabits[Literal]));
                                        Distance = DecodeValue(DistanceHuffmanTree!);
                                        Distance = (ushort)(DistancecodeValueOffset[Distance] + ReadBits(DistancecodeValueExtrabits[Distance]));
                                        CopyBytes(Distance, Length);
                                    }
                                    else
                                    {
                                        SI_ERROR = 0x8000;
                                        Literal = 0x100;
                                    }
                                }
                            } while (Literal != 0x100);
                            if ((SI_ERROR & 0x8000) == 0x0000 && SI_ERROR != 0x8000)
                                FreeDynamicCodeTrees();
                        }
                    }
                }
            } while (LastBlock != 1);

            if ((SI_ERROR & 0xc000) == 0x0000 && SI_POSITION > 0x0000)
                SI_WRITE(SI_POSITION);
            DeAllocateStaticTables();
            SI_WINDOW = [];
        }

        #endregion
    }
}