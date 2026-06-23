// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Debug = UnityEngine.Debug;

namespace Unity.Burst.Editor
{
    /// <summary>
    /// Disassembler for Intel and ARM
    /// </summary>
    internal partial class BurstDisassembler
    {
        // The following member need to be reset/clear on each Reset()
        private readonly Dictionary<int, string> _fileName;
        private readonly Dictionary<int, string[]> _fileList;
        private readonly List<AsmToken> _tokens;
        private readonly List<AsmBlock> _blocks;
        private readonly List<string> _blockToString;
        private readonly List<int> _columnIndices;
        private readonly List<AsmLine> _lines;
        internal UsedRegisters _registersUsedAtLine;
        private readonly DictionaryGlobalLabel _globalLabels;
        private readonly List<TempLabelRef> _tempLabelRefs;
        private readonly Dictionary<int, StringSlice> _mapBlockIndexToGlobalLabel;
        private DictionaryLocalLabel _currentDictLocalLabel;
        public bool IsInitialized { get; private set; }

        // ^^^
        private string _input;
        private AsmKind _inputAsmKind;
        internal readonly StringBuilder _output;
        private bool _colored;

        // This is used to aligned instructions and there operands so they look like this
        //
        // mulps   x,x,x
        // shufbps x,x,x
        //
        // instead of
        //
        // mulps x,x,x
        // shufbps x,x,x
        //
        // Notice if instruction name is longer than this no alignment will be done.
        private const int InstructionAlignment = 10;

        private static readonly StringSlice CVLocDirective = new StringSlice(".cv_loc");

        // Colors used for the tokens
        // TODO: Make this configurable via some editor settings?
        private const string DarkColorLineDirective = "#FFFF00";
        private const string DarkColorDirective = "#CCCCCC";
        private const string DarkColorIdentifier = "#d4d4d4";
        private const string DarkColorQualifier = "#DCDCAA";
        private const string DarkColorInstruction = "#4EC9B0";
        internal const string DarkColorInstructionSIMD = "#C586C0";
        internal const string DarkColorInstructionSIMDPacked = "#A586C0";
        internal const string DarkColorInstructionSIMDScalar = "#E586C0";
        private const string DarkColorRegister = "#d7ba7d";
        private const string DarkColorNumber = "#9cdcfe";
        private const string DarkColorString = "#ce9178";
        private const string DarkColorComment = "#6A9955";

        private const string LightColorLineDirective = "#888800";
        private const string LightColorDirective = "#444444";
        private const string LightColorIdentifier = "#1c1c1c";
        private const string LightColorQualifier = "#267f99";
        private const string LightColorInstruction = "#0451a5";
        private const string LightColorInstructionSIMD = "#0000ff";
        private const string LightColorInstructionSIMDPacked = "#8000ff";
        private const string LightColorInstructionSIMDScalar = "#8050ff";
        private const string LightColorRegister = "#811f3f";
        private const string LightColorNumber = "#007ACC";
        private const string LightColorString = "#a31515";
        private const string LightColorComment = "#008000";

        private string ColorLineDirective;
        private string ColorDirective;
        private string ColorIdentifier;
        private string ColorQualifier;
        private string ColorInstruction;
        private string ColorInstructionSIMD;
        private string ColorInstructionSIMDPacked;
        private string ColorInstructionSIMDScalar;
        private string ColorRegister;
        private string ColorNumber;
        private string ColorString;
        private string ColorComment;

        private char _commentStart;

        public BurstDisassembler()
        {
            _fileName = new Dictionary<int, string>();
            _fileList = new Dictionary<int, string[]>();
            _tokens = new List<AsmToken>(65536);
            _blocks = new List<AsmBlock>(128);
            _blockToString = new List<string>(128);
            _columnIndices = new List<int>(65536);
            _lines = new List<AsmLine>(4096);
            _registersUsedAtLine = new UsedRegisters(4096);
            _tempLabelRefs = new List<TempLabelRef>(4096);
            _globalLabels = new DictionaryGlobalLabel(128);
            _mapBlockIndexToGlobalLabel = new Dictionary<int, StringSlice>(128);
            _output = new StringBuilder();
        }

        internal List<int> ColumnIndices => _columnIndices;

        /// <summary>
        /// Gets all the blocks.
        /// </summary>
        public List<AsmBlock> Blocks => _blocks;

        /// <summary>
        /// Gets whether the disassembly is colored.
        /// </summary>
        public bool IsColored => _colored;

        /// <summary>
        /// Gets all the lines for all the blocks.
        /// </summary>
        public List<AsmLine> Lines => _lines;

        /// <summary>
        /// Gets all the tokens
        /// </summary>
        public List<AsmToken> Tokens => _tokens;

        public int LineUsedReg(int lineIdx, string reg) => _registersUsedAtLine.RegisterMatch(lineIdx, reg);
        public bool LineUsesRegs(int lineIdx, out List<string> usedRegs) => _registersUsedAtLine.LineContainsRegs(lineIdx, out usedRegs);
        public List<string> CleanRegs(List<string> regs) => _registersUsedAtLine.CleanRegs(regs);

        public int GetRegisterTokenIndex(AsmLine line, string reg, int startIndex = 0)
        {
            var idx = -1;

            var i = Math.Max(line.TokenIndex, startIndex);
            var len = line.TokenIndex + line.Length;
            for (; i < len; i++)
            {
                var token = Tokens[i];
                if (_registersUsedAtLine.RegisterEquality(reg, GetTokenAsText(token)))
                {
                    idx = i;
                    break;
                }
            }

            return idx;
        }

        /// <summary>
        /// Get a token index for a particular block, line number and column number.
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="line"></param>
        /// <param name="column"></param>
        /// <param name="lineIndex">Returns the line index to query <see cref="Lines"/></param>
        /// <returns>The token index to use with <see cref="GetToken"/> or -1 if the line, column was not found.</returns>
        public int GetTokenIndexFromColumn(int blockIndex, int line, int column, out int lineIndex)
        {
            lineIndex = -1;
            var block = _blocks[blockIndex];
            var lineStartIndex = block.LineIndex + line;
            var asmLine = _lines[lineStartIndex];
            if (asmLine.Kind != AsmLineKind.SourceFileLocation)
            {
                var columnIndex = asmLine.ColumnIndex;
                for (int j = 1; j < asmLine.Length; j++)
                {
                    // _columnIndices doesn't have an index for the first token (because the column is always 0)
                    var tokenColumn = _columnIndices[columnIndex + j - 1];
                    var token = GetToken(asmLine.TokenIndex + j);

                    if (tokenColumn <= column && column < tokenColumn + token.Length)
                    {
                        lineIndex = lineStartIndex;
                        return asmLine.TokenIndex + j;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets or renders a particular block to text without caching the result.
        /// </summary>
        /// <param name="blockIndex">The block to render.</param>
        /// <param name="colored">Whether output should be colored.</param>
        /// <returns>A string representation of the block.</returns>
        public string GetOrRenderBlockToTextUncached(int blockIndex, bool colored)
        {
            return RenderBlock(blockIndex, colored);
        }

        /// <summary>
        /// Gets or renders a particular block to text (colored if specified at <see cref="Initialize"/> time)
        /// </summary>
        /// <param name="blockIndex">The block to render.</param>
        /// <returns>A string representation of the block.</returns>
        public string GetOrRenderBlockToText(int blockIndex)
        {
            var str = _blockToString[blockIndex];
            if (str == null)
            {
                str = RenderBlock(blockIndex, _colored);
                _blockToString[blockIndex] = str;
            }
            return str;
        }

        /// <summary>
        /// Gets a token at the specified token index.
        /// </summary>
        /// <param name="tokenIndex">The token index</param>
        /// <returns>The token available at the specified index</returns>
        public AsmToken GetToken(int tokenIndex)
        {
            return _tokens[tokenIndex];
        }

        /// <summary>
        /// Returns the text representation of the token at the specified index
        /// </summary>
        /// <param name="tokenIndex"></param>
        /// <returns></returns>
        public StringSlice GetTokenAsTextSlice(int tokenIndex)
        {
            return _tokens[tokenIndex].Slice(_input);
        }

        /// <summary>
        /// Returns the text representation of the specified token.
        /// </summary>
        public StringSlice GetTokenAsTextSlice(AsmToken token)
        {
            return token.Slice(_input);
        }

        /// <summary>
        /// Returns the text representation of the specified token.
        /// </summary>
        public string GetTokenAsText(AsmToken token)
        {
            return token.ToString(_input);
        }

        /// <summary>
        /// Try and get description of <see cref="instruction"/>.
        /// </summary>
        /// <param name="instruction">Instruction to query information about.</param>
        /// <param name="info">If instruction present the queried information, else default string.</param>
        /// <returns>Whether instruction was present in burst disassembler core.</returns>
        internal bool GetInstructionInformation(string instruction, out string info)
        {
            switch (_inputAsmKind)
            {
                case AsmKind.Intel:
                    return X86AsmInstructionInfo.GetX86InstructionInfo(instruction, out info);
                case AsmKind.ARM:
                    return ARM64InstructionInfo.GetARM64Info(instruction, out info);
                case AsmKind.LLVMIR:
                    return LLVMIRInstructionInfo.GetLLVMIRInfo(instruction, out info);
                case AsmKind.Wasm:
                    return WasmInstructionInfo.GetWasmInfo(instruction, out info);
                default:
                    throw new InvalidOperationException($"No instruction information for {_inputAsmKind}");
            }
        }

        /// <summary>
        /// Initialize the disassembler with the input and parametesr.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="asmKind"></param>
        /// <param name="useDarkSkin"></param>
        /// <param name="useSyntaxColoring"></param>
        /// <param name="smellTest"></param>
        /// <returns></returns>
        public bool Initialize(string input, AsmKind asmKind, bool useDarkSkin = true, bool useSyntaxColoring = true, bool smellTest = false)
        {
            try
            {
                InitializeImpl(input, asmKind, useDarkSkin, useSyntaxColoring, smellTest);
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Reset();
                UnityEngine.Debug.Log($"Error while trying to disassemble the input: {ex}");
            }

            return IsInitialized;
        }

        /// <summary>
        /// Helper method to output the full (colored) text as we did before.
        ///
        /// This method will be deprecated. Just here for testing during the transition.
        /// </summary>
        public string RenderFullText()
        {
            // If not initialized correctly (disassembly failed), return the input string as-is
            if (!IsInitialized) return _input ?? string.Empty;

            var builder = new StringBuilder();
            for (int i = 0; i < _blocks.Count; i++)
            {
                var text = GetOrRenderBlockToText(i);
                builder.Append(text);
            }
            return builder.ToString();
        }

        private void Reset()
        {
            _registersUsedAtLine.Clear();
            _fileList.Clear();
            _fileName.Clear();
            _tokens.Clear();
            _blocks.Clear();
            _blockTextIdxs.Clear();
            _blockToString.Clear();
            _columnIndices.Clear();
            _lines.Clear();
            _tempLabelRefs.Clear();
            _globalLabels.Clear();
            _mapBlockIndexToGlobalLabel.Clear();
            _currentDictLocalLabel = null;
            IsInitialized = false;
        }

        private AsmTokenKindProvider _tokenProvider = null;

        private void InitializeImpl(string input, AsmKind asmKind, bool useDarkSkin = true, bool useSyntaxColoring = true, bool smellTest=false)
        {
            _commentStart = (asmKind == AsmKind.Intel || asmKind == AsmKind.Wasm) ? '#' : ';';
            UseSkin(useDarkSkin, smellTest);
            _colored = useSyntaxColoring;
            _tokenProvider = InitializeInput(input, asmKind);
            _registersUsedAtLine.AddTokenProvider(_tokenProvider);
            ParseAndProcessTokens(_tokenProvider);
        }

        /// <summary>
        /// Finds the block index encapsulating <see cref="textIdx"/>.
        /// </summary>
        /// <param name="textIdx">Text index relative to <see cref="_input"/>.</param>
        /// <param name="start">Left-most block index to search within.</param>
        /// <returns>(block index, blocks start index in <see cref="_input"/>)</returns>
        public (int idx, int l) GetBlockIdxFromTextIdx(int textIdx)
        {
            return GetBlockIdxFromTextIdx(textIdx, 0);
        }


        /// <summary>
        /// Finds the block index encapsulating <see cref="textIdx"/>.
        /// </summary>
        /// <param name="textIdx">Text index relative to <see cref="_input"/>.</param>
        /// <param name="start">Left-most block index to search within.</param>
        /// <returns>(block index, blocks start index in <see cref="_input"/>)</returns>
        public (int idx, int l) GetBlockIdxFromTextIdx(int textIdx, int start)
        {
            int end = _blockTextIdxs.Count-1;
            while (start <= end)
            {
                int mid = (end + start) / 2;
                var (startIdx, endIdx) = _blockTextIdxs[mid];

                if (startIdx <= textIdx && textIdx <= endIdx)
                {
                    return (mid, startIdx);
                }

                if (endIdx < textIdx)
                {
                    start = mid + 1;
                }
                else
                {
                    end = mid - 1;
                }
            }
            return (-1, -1);
        }

        private bool _smellTest;
        private void UseSkin(bool useDarkSkin, bool smellTest)
        {
            _smellTest = smellTest;
            if (useDarkSkin)
            {
                ColorLineDirective = DarkColorLineDirective;
                ColorDirective = DarkColorDirective;
                ColorIdentifier = DarkColorIdentifier;
                ColorQualifier = DarkColorQualifier;
                ColorInstruction = DarkColorInstruction;
                ColorInstructionSIMD = DarkColorInstructionSIMD;
                ColorInstructionSIMDPacked = DarkColorInstructionSIMDPacked;
                ColorInstructionSIMDScalar = DarkColorInstructionSIMDScalar;
                ColorRegister = DarkColorRegister;
                ColorNumber = DarkColorNumber;
                ColorString = DarkColorString;
                ColorComment = DarkColorComment;
            }
            else
            {
                ColorLineDirective = LightColorLineDirective;
                ColorDirective = LightColorDirective;
                ColorIdentifier = LightColorIdentifier;
                ColorQualifier = LightColorQualifier;
                ColorInstruction = LightColorInstruction;
                ColorInstructionSIMD = LightColorInstructionSIMD;
                ColorInstructionSIMDPacked = LightColorInstructionSIMDPacked;
                ColorInstructionSIMDScalar = LightColorInstructionSIMDScalar;
                ColorRegister = LightColorRegister;
                ColorNumber = LightColorNumber;
                ColorString = LightColorString;
                ColorComment = LightColorComment;
            }
        }

        private int AlignInstruction(StringBuilder output, int instructionLength, AsmKind asmKind)
        {
            // Only support Intel for now
            if (instructionLength >= InstructionAlignment || asmKind != AsmKind.Intel)
                return 0;

            int align = InstructionAlignment - instructionLength;
            output.Append(' ', align);
            return align;
        }

        private AsmTokenKindProvider InitializeInput(string input, AsmKind asmKind)
        {
            AsmTokenKindProvider asmTokenProvider = null;

            _input = input;
            _inputAsmKind = asmKind;

            switch (asmKind)
            {
                case AsmKind.Intel:
                    asmTokenProvider = (AsmTokenKindProvider)X86AsmTokenKindProvider.Instance;
                    break;
                case AsmKind.ARM:
                    asmTokenProvider = (AsmTokenKindProvider)ARM64AsmTokenKindProvider.Instance;
                    break;
                case AsmKind.Wasm:
                    asmTokenProvider = (AsmTokenKindProvider)WasmAsmTokenKindProvider.Instance;
                    break;
                case AsmKind.LLVMIR:
                    asmTokenProvider = (AsmTokenKindProvider)LLVMIRAsmTokenKindProvider.Instance;
                    break;
                default:
                    throw new InvalidOperationException($"No {nameof(AsmTokenKindProvider)} for {asmKind}");
            }

            return asmTokenProvider;
        }

        private int GetLineLen(in AsmLine line)
        {
            int len = 0;
            int offset = line.TokenIndex;
            int numLineTokens = line.Length;
            for (int i = 0; i < numLineTokens; i++)
            {
                AsmToken token = _tokens[offset + i];
                len += token.Kind != AsmTokenKind.NewLine
                    ? token.Length
                    : 1; // We don't use windows line endings, but internal token might,
            }

            return len;
        }

        private void ParseAndProcessTokens(AsmTokenKindProvider asmTokenProvider)
        {
            Reset();

            var tokenizer = new AsmTokenizer(_input, _inputAsmKind, asmTokenProvider, _commentStart);

            // Adjust token size
            var pseudoTokenSizeMax = _input.Length / 7;
            if (pseudoTokenSizeMax > _tokens.Capacity)
            {
                _tokens.Capacity = pseudoTokenSizeMax;
            }

            // Start the top-block as a directive block
            var block = new AsmBlock { Kind = AsmBlockKind.Block };
            AsmLine line = default;
            var blockKindDetectFlags = BlockKindDetectFlags.None;

            // Skip first line
            // Don't tokenize the first line that contains e.g:
            // While compiling job: System.Single BurstJobTester/MyJob::CheckFmaSlow(System.Single,System.Single,System.Single)
            while (tokenizer.TryGetNextToken(out var token))
            {
                if (token.Kind == AsmTokenKind.NewLine)
                {
                    break;
                }
            }

            // Read all tokens
            // Create blocks and lines on the fly, record functions
            int totalIdx = 0;
            int blockStartIdx = 0;
            bool newLine = false;
            var (possiblyRemoveAlignment, addedAlignment) = (false, 0);
            while (tokenizer.TryGetNextToken(out var token))
            {
                var tokenIndex = _tokens.Count;
                _tokens.Add(token);

                if (newLine)
                {
                    if (possiblyRemoveAlignment)
                    {
                        // Alignment was added just before a newline
                        totalIdx -= addedAlignment;
                    }

                    // Push new line
                    if (line.Kind == AsmLineKind.SourceFile)
                    {
                        // Have to remove the line from totalIdx, for proper block idx saving.
                        totalIdx -= GetLineLen(line);
                        ProcessSourceFile(ref line);
                        // We drop this line, we don't store SourceFile line as-is but just below as SourceFileLocation
                    }
                    else
                    {
                        var lineRef = new AsmLineRef(_blocks.Count, block.Length);
                        if (line.Kind == AsmLineKind.SourceLocation)
                        {
                            // Have to remove the line from totalIdx, for proper block idx saving.
                            totalIdx -= GetLineLen(line);
                            ProcessSourceLocation(ref line, ref totalIdx);
                            // after this, the line is now a SourceFileLocation
                        }
                        else if (line.Kind == AsmLineKind.LabelDeclaration)
                        {
                            // Record labels (global and locals)
                            ProcessLabelDeclaration(lineRef, line);
                        }
                        else if (line.Kind == AsmLineKind.CodeBranch || line.Kind == AsmLineKind.CodeJump)
                        {
                            // Record temp branch/jumps
                            ProcessJumpOrBranch(lineRef, ref line);
                        }

                        _lines.Add(line);
                        _registersUsedAtLine.PushLine();
                        block.Length++;
                    }

                    bool previousLineWasBranch = line.Kind == AsmLineKind.CodeBranch;

                    // Reset the line
                    line = default;
                    line.Kind = AsmLineKind.Empty;
                    line.TokenIndex = tokenIndex;
                    // We create a new block when hitting a label declaration
                    // If the previous line was a conditional branch, it is like having an implicit label
                    if (previousLineWasBranch || token.Kind == AsmTokenKind.Label)
                    {
                        // Refine the kind of block before pushing it
                        if ((blockKindDetectFlags & BlockKindDetectFlags.Code) != 0)
                        {
                            block.Kind = AsmBlockKind.Code;
                        }
                        else if ((blockKindDetectFlags & BlockKindDetectFlags.Data) != 0)
                        {
                            block.Kind = AsmBlockKind.Data;
                        }
                        else if ((blockKindDetectFlags & BlockKindDetectFlags.Directive) != 0)
                        {
                            block.Kind = AsmBlockKind.Directive;
                        }

                        // Push the current block
                        _blocks.Add(block);
                        _blockTextIdxs.Add((blockStartIdx, totalIdx-1));
                        _blockToString.Add(null);

                        // Create a new block
                        blockStartIdx = totalIdx;
                        block = new AsmBlock
                        {
                            Kind = AsmBlockKind.None,
                            LineIndex = _lines.Count,
                            Length = 0
                        };
                        blockKindDetectFlags = BlockKindDetectFlags.None;
                    }
                }

                // If the current line is still undefined try to detect what kind of line we have
                var lineKind = line.Kind;
                if (lineKind == AsmLineKind.Empty)
                {
                    switch (token.Kind)
                    {
                        case AsmTokenKind.Directive:
                            lineKind = AsmLineKind.Directive;
                            blockKindDetectFlags |= BlockKindDetectFlags.Directive;
                            break;
                        case AsmTokenKind.SourceFile:
                            lineKind = AsmLineKind.SourceFile;
                            break;
                        case AsmTokenKind.SourceLocation:
                            lineKind = AsmLineKind.SourceLocation;
                            blockKindDetectFlags |= BlockKindDetectFlags.Code;
                            break;
                        case AsmTokenKind.DataDirective:
                            lineKind = AsmLineKind.Data;
                            blockKindDetectFlags |= BlockKindDetectFlags.Data;
                            break;
                        case AsmTokenKind.Instruction:
                        case AsmTokenKind.InstructionSIMD:
                            lineKind = AsmLineKind.Code;
                            blockKindDetectFlags |= BlockKindDetectFlags.Code;
                            break;
                        case AsmTokenKind.BranchInstruction:
                            lineKind = AsmLineKind.CodeBranch;
                            blockKindDetectFlags |= BlockKindDetectFlags.Code;
                            break;
                        case AsmTokenKind.JumpInstruction:
                            lineKind = AsmLineKind.CodeJump;
                            blockKindDetectFlags |= BlockKindDetectFlags.Code;
                            break;
                        case AsmTokenKind.CallInstruction:
                            lineKind = AsmLineKind.CodeCall;
                            blockKindDetectFlags |= BlockKindDetectFlags.Code;
                            break;
                        case AsmTokenKind.ReturnInstruction:
                            lineKind = AsmLineKind.CodeReturn;
                            blockKindDetectFlags |= BlockKindDetectFlags.Code;
                            break;
                        case AsmTokenKind.Label:
                            lineKind = newLine ? AsmLineKind.LabelDeclaration : AsmLineKind.Empty;
                            break;
                        case AsmTokenKind.Comment:
                            lineKind = AsmLineKind.Comment;
                            break;
                        case AsmTokenKind.FunctionBegin:
                            lineKind = AsmLineKind.FunctionBegin;
                            break;
                        case AsmTokenKind.FunctionEnd:
                            lineKind = AsmLineKind.FunctionEnd;
                            break;
                    }
                    line.Kind = lineKind;
                }

                // Add alignment for it to match the output BurstDisassembler gives to the outside world
                switch (token.Kind)
                {
                    case AsmTokenKind.Instruction:
                    case AsmTokenKind.CallInstruction:
                    case AsmTokenKind.BranchInstruction:
                    case AsmTokenKind.JumpInstruction:
                    case AsmTokenKind.ReturnInstruction:
                    case AsmTokenKind.InstructionSIMD:
                        if (!(token.Length >= InstructionAlignment || _inputAsmKind != AsmKind.Intel))
                        {
                            totalIdx += (InstructionAlignment - token.Length);
                            possiblyRemoveAlignment = true;
                            addedAlignment = InstructionAlignment - token.Length;
                        }
                        break;
                    // If new line is hit do not set to false, as to carry the information
                    // into the next iteration.
                    case AsmTokenKind.NewLine:
                        break;
                    default:
                        possiblyRemoveAlignment = false;
                        break;
                }

                // Add used registers to the index appropriate for specific line.
                if (token.Kind == AsmTokenKind.Register)
                {
                    _registersUsedAtLine.Add(_lines.Count, GetTokenAsText(token));
                }

                line.Length++;
                newLine = token.Kind == AsmTokenKind.NewLine;
                totalIdx += newLine ? 1 : token.Length;
            }

            // Process the remaining line
            if (line.Length > 0)
            {
                _lines.Add(line);
                block.Length++;

                _registersUsedAtLine.PushLine();
            }

            if (block.Length > 0)
            {
                _blocks.Add(block);
                _blockTextIdxs.Add((blockStartIdx, totalIdx - 1));
                _blockToString.Add(null);
            }

            ProcessLabelsAndCreateEdges();
        }

        private void ProcessLabelDeclaration(in AsmLineRef lineRef, in AsmLine line)
        {
            var iterator = GetIterator(line);

            iterator.TryGetNext(out var token); // label
            var text = token.Slice(_input);
            if (IsLabelLocal(text))
            {
                // if ´_currentDictLocalLabel==null´ we just hit a local label prior to any global labels.
                // So we simply create a empty global label, to hold this local:
                if (_currentDictLocalLabel is null)
                {
                    _currentDictLocalLabel = _globalLabels.GetOrCreate(new StringSlice(""), lineRef);
                    _mapBlockIndexToGlobalLabel[lineRef.BlockIndex] = text;
                }

                // Record local labels to the current global label dictionary
                _currentDictLocalLabel.Add(text, lineRef);
            }
            else
            {
                // Create a local label dictionary per global label
                _currentDictLocalLabel = _globalLabels.GetOrCreate(text, lineRef);
                // Associate the current block index to this global index
                _mapBlockIndexToGlobalLabel[lineRef.BlockIndex] = text;
            }
        }

        private void ProcessJumpOrBranch(in AsmLineRef lineRef, ref AsmLine line)
        {
            var iterator = GetIterator(line);
            iterator.TryGetNext(out _); // branch/jump instruction

            if (iterator.TryGetNext(out var label, out var labelTokenIndex))
            {
                if (label.Kind == AsmTokenKind.String || label.Kind == AsmTokenKind.Identifier || label.Kind == AsmTokenKind.Label)
                {
                    // In case the token is not a label, convert it to a label after this
                    if (label.Kind != AsmTokenKind.Label)
                    {
                        var token = _tokens[labelTokenIndex];
                        token = new AsmToken(AsmTokenKind.Label, token.Position, token.AlignedPosition, token.Length);
                        _tokens[labelTokenIndex] = token;
                    }

                    var currentGlobalBlockIndex = _currentDictLocalLabel.GlobalLabelLineRef.BlockIndex;
                    _tempLabelRefs.Add(new TempLabelRef(currentGlobalBlockIndex, lineRef, label.Position, label.Length));
                }
            }
        }

        private void ProcessSourceFile(ref AsmLine line)
        {
            var it = GetIterator(line);

            it.TryGetNext(out _); // skip .file or .cv_file

            int index = 0;
            if (it.TryGetNext(out var token) && token.Kind == AsmTokenKind.Number)
            {
                var numberAsStr = GetTokenAsText(token);
                index = int.Parse(numberAsStr);
            }

            if (it.TryGetNext(out token) && token.Kind == AsmTokenKind.String)
            {
                var filename = GetTokenAsText(token).Trim('"').Replace('\\', '/');
                string[] fileLines = null;

                //blockIdx += 4 + System.IO.Path.GetFileName(filename).Length;// ("=== " + filename).Length
                try
                {
                    if (System.IO.File.Exists(filename))
                    {
                        fileLines = System.IO.File.ReadAllLines(filename);
                    }
                }
                catch
                {
                    fileLines = null;
                }


                _fileName.Add(index, filename);
                _fileList.Add(index, fileLines);
            }
        }

        private void ProcessSourceLocation(ref AsmLine line, ref int blockIdx)
        {
            var it = GetIterator(line);

            // .loc {fileno} {lineno} [column] [options] -
            // .cv_loc funcid fileno lineno [column]
            int fileno = 0;
            int colno = 0;
            int lineno = 0; // NB 0 indicates no information given

            if (it.TryGetNext(out var token))
            {
                var tokenSlice = GetTokenAsTextSlice(token);
                if (tokenSlice == CVLocDirective)
                {
                    // skip funcId
                    it.TryGetNext(out token);
                }
            }

            if (it.TryGetNext(out token) && token.Kind == AsmTokenKind.Number)
            {
                var numberAsStr = GetTokenAsText(token);
                fileno = int.Parse(numberAsStr);
            }

            if (it.TryGetNext(out token) && token.Kind == AsmTokenKind.Number)
            {
                var numberAsStr = GetTokenAsText(token);
                lineno = int.Parse(numberAsStr);
            }

            if (it.TryGetNext(out token) && token.Kind == AsmTokenKind.Number)
            {
                var numberAsStr = GetTokenAsText(token);
                colno = int.Parse(numberAsStr);
            }

            // Transform the SourceLocation into a SourceFileLocation
            line.Kind = AsmLineKind.SourceFileLocation;
            line.SourceFileNumber = fileno;
            line.SourceLineNumber = lineno;
            line.SourceColumnNumber = colno;

            // Make sure blockTextIdxs are correct
            if (fileno == 0) return;
            blockIdx += 2 + System.IO.Path.GetFileName(_fileName[fileno]).Length; // ("; " + filename).length

            if (lineno != 0)
            {
                blockIdx += 4 + lineno.ToString().Length + (colno + 1).ToString().Length;// "(x, y)"

                if (_fileList.ContainsKey(fileno) && _fileList[fileno] != null && lineno - 1 < _fileList[fileno].Length)
                {
                    blockIdx += _fileList[fileno][lineno - 1].Length;
                }
            }
            blockIdx++; // \n
        }

        private static bool IsLabelLocal(in StringSlice slice)
        {
            return slice.StartsWith(".L");
        }

        private void ProcessLabelsAndCreateEdges()
        {
            foreach (var tempLabelRef in _tempLabelRefs)
            {
                var globalBlockIndex = tempLabelRef.GlobalBlockIndex;

                // Source Block + Line
                var srcRef = tempLabelRef.LineRef;
                var srcBlockIndex = srcRef.BlockIndex;
                var srcLineIndex = srcRef.LineIndex;
                var srcBlock = _blocks[srcBlockIndex];
                // Line where the edge occurs
                var srcLine = _lines[srcBlock.LineIndex + srcLineIndex];

                var label = new StringSlice(_input, tempLabelRef.StringIndex, tempLabelRef.StringLength);
                var isLocal = IsLabelLocal(label);
                AsmLineRef destRef;
                if (isLocal)
                {
                    var globalLabel = _mapBlockIndexToGlobalLabel[globalBlockIndex];
                    var localLabel = _globalLabels[globalLabel];
                    destRef = localLabel[label];
                }
                else
                {
                    if (_globalLabels.TryGetValue(label, out var entry))
                    {
                        destRef = entry.GlobalLabelLineRef;
                    }
                    else
                    {
                        continue;   // Some global labels (at least on arm) e.g. __divsi3 are runtime library defined and not present at all in the source
                    }
                }

                // Destination Block + Line
                var dstBlock = _blocks[destRef.BlockIndex];

                // Create edges
                srcBlock.AddEdge(new AsmEdge(AsmEdgeKind.OutBound, srcRef, destRef));
                dstBlock.AddEdge(new AsmEdge(AsmEdgeKind.InBound, destRef, srcRef));

                // For conditional branches, add the false branch as well
                // TODO: should we comment that in the meantime or?
                if (srcLine.Kind == AsmLineKind.CodeBranch)
                {
                    // The implicit destination block for the false branch is the next block of the source
                    // TODO: we pickup the line 0, while we might want to select the first code of line or first Label declaration
                    var blockFalseRef = new AsmLineRef(srcRef.BlockIndex + 1, 0);
                    dstBlock = _blocks[blockFalseRef.BlockIndex];

                    srcBlock.AddEdge(new AsmEdge(AsmEdgeKind.OutBound, srcRef, blockFalseRef));
                    dstBlock.AddEdge(new AsmEdge(AsmEdgeKind.InBound, blockFalseRef, srcRef));
                }
            }

            // Sort all edges
            foreach (var block in Blocks)
            {
                block.SortEdges();
            }
        }

        private List<(int startIdx, int endIdx)> _blockTextIdxs = new List<(int startIdx, int endIdx)>(128);

        public List<(int startIdx, int endIdx)> BlockIdxs => _blockTextIdxs;


        private string RenderBlock(int blockIndex, bool colored)
        {
            var block = _blocks[blockIndex];
            _output.Clear();
            var lineStart = block.LineIndex;
            var length = block.Length;
            for (int i = 0; i < length; i++)
            {
                var line = _lines[lineStart + i];
                RenderLine(ref line, colored);
                // write back the line that has been modified. But only if we run with the same color mode,
                // that the disassembler was initialized with.
                if (colored == _colored) _lines[lineStart + i] = line;
            }

            var str = _output.ToString();
            _output.Length = 0;
            return str;
        }

        internal void RenderLine(ref AsmLine line, bool colored)
        {
            // Render this line with a specific renderer
            if (line.Kind == AsmLineKind.SourceFileLocation)
            {
                RenderSourceFileLocation(ref line, colored);
                return;
            }

            // Process all tokens
            var length = line.Length;
            int column = 0;
            for (int i = 0; i < length; i++)
            {
                var token = _tokens[line.TokenIndex + i];
                var slice = token.Slice(_input);

                // We don't record the first column because it is always 0
                if (column > 0)
                {
                    if (line.ColumnIndex == 0)
                    {
                        line.ColumnIndex = _columnIndices.Count;
                    }
                    _columnIndices.Add(column);
                }

                if (colored)
                {
                    switch (token.Kind)
                    {
                        case AsmTokenKind.DataDirective:
                        case AsmTokenKind.Directive:
                        case AsmTokenKind.FunctionBegin:
                        case AsmTokenKind.FunctionEnd:
                            _output.Append("<color=").Append(ColorDirective).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            break;
                        case AsmTokenKind.Label:
                        case AsmTokenKind.Identifier:
                            _output.Append("<color=").Append(ColorIdentifier).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            break;
                        case AsmTokenKind.Qualifier:
                            _output.Append("<color=").Append(ColorQualifier).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            break;
                        case AsmTokenKind.Instruction:
                        case AsmTokenKind.CallInstruction:
                        case AsmTokenKind.BranchInstruction:
                        case AsmTokenKind.JumpInstruction:
                        case AsmTokenKind.ReturnInstruction:
                            _output.Append("<color=").Append(ColorInstruction).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            if (i == length - 2) // last slice always a newline
                                break;
                            column += AlignInstruction(_output, slice.Length, _inputAsmKind);
                            break;
                        case AsmTokenKind.InstructionSIMD:
                            // Perform smell test for simd instructions:
                            var col = ColorInstructionSIMD;
                            if (_smellTest)
                            {
                                switch (_tokenProvider.SimdKind(slice))
                                {
                                    case SIMDkind.Packed:
                                        col = ColorInstructionSIMDPacked;
                                        break;
                                    case SIMDkind.Scalar:
                                        col = ColorInstructionSIMDScalar;
                                        break;
                                    case SIMDkind.Infrastructure:
                                        break;
                                }
                            }

                            _output.Append("<color=").Append(col).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            if (i == length - 2) // last slice always newline
                                break;
                            column += AlignInstruction(_output, slice.Length, _inputAsmKind);
                            break;
                        case AsmTokenKind.Register:
                            _output.Append("<color=").Append(ColorRegister).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            break;
                        case AsmTokenKind.Number:
                            _output.Append("<color=").Append(ColorNumber).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            break;
                        case AsmTokenKind.String:
                            _output.Append("<color=").Append(ColorString).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            break;
                        case AsmTokenKind.Comment:
                            _output.Append("<color=").Append(ColorComment).Append('>');
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            _output.Append("</color>");
                            break;
                        case AsmTokenKind.NewLine:
                            _output.Append('\n');
                            break;
                        default:
                            _output.Append(_input, slice.Position, slice.Length);
                            column += slice.Length;
                            break;
                    }
                }
                else
                {
                    if (token.Kind == AsmTokenKind.NewLine)
                    {
                        _output.Append('\n');
                    }
                    else
                    {
                        _output.Append(_input, slice.Position, slice.Length);
                        column += slice.Length;
                    }

                    // Also wants to align instructions in uncolored mode the same way as colored.
                    switch (token.Kind)
                    {
                        case AsmTokenKind.Instruction:
                        case AsmTokenKind.CallInstruction:
                        case AsmTokenKind.BranchInstruction:
                        case AsmTokenKind.JumpInstruction:
                        case AsmTokenKind.ReturnInstruction:
                        case AsmTokenKind.InstructionSIMD:
                            // Do not add alignment to instruction with no arguments
                            // last slice always a newline
                            if (i == length - 2) break;
                            column += AlignInstruction(_output, slice.Length, _inputAsmKind);
                            break;
                    }
                }
            }
        }

        private void RenderSourceFileLocation(ref AsmLine line, bool colored)
        {
            char[] comment = {_commentStart, ' '};
            var fileno = line.SourceFileNumber;
            var lineno = line.SourceLineNumber;
            var colno = line.SourceColumnNumber;

            // If the file number is 0, skip the line
            if (fileno == 0)
            {
            }
            // If the line number is 0, then we can update the file tracking, but still not output a line
            else if (lineno == 0)
            {
                if (colored) _output.Append("<color=").Append(ColorLineDirective).Append('>');
                _output.Append(comment).Append(System.IO.Path.GetFileName(_fileName[fileno]));
                if (colored) _output.Append("</color>");
            }
            // We have a source line and number -- can we load file and extract this line?
            else
            {
                if (_fileList.ContainsKey(fileno) && _fileList[fileno] != null && lineno - 1 < _fileList[fileno].Length)
                {
                    if (colored) _output.Append("<color=").Append(ColorLineDirective).Append('>');
                    _output.Append(comment).Append(System.IO.Path.GetFileName(_fileName[fileno])).Append('(').Append(lineno).Append(", ").Append(colno + 1).Append(')').Append(_fileList[fileno][lineno - 1]);
                    if (colored) _output.Append("</color>");
                }
                else
                {
                    if (colored) _output.Append("<color=").Append(ColorLineDirective).Append('>');
                    _output.Append(comment).Append(System.IO.Path.GetFileName(_fileName[fileno])).Append('(').Append(lineno).Append(", ").Append(colno + 1).Append(')');
                    if (colored) _output.Append("</color>");
                }
            }
            _output.Append('\n');
        }
        private AsmTokenIterator GetIterator(in AsmLine line)
        {
            return new AsmTokenIterator(_tokens, line.TokenIndex, line.Length);
        }

        public enum AsmKind
        {
            Intel,
            ARM,
            Wasm,
            LLVMIR
        }

        [Flags]
        enum BlockKindDetectFlags
        {
            None = 0,
            Code = 1 << 0,
            Data = 1 << 1,
            Directive = 1 << 2,
        }

        public enum AsmBlockKind
        {
            None,
            Block,
            Directive,
            Code,
            Data
        }

        [DebuggerDisplay("Block {Kind} LineIndex = {LineIndex} Length = {Length}")]
        public class AsmBlock
        {
            public AsmBlockKind Kind;

            public int LineIndex;

            public int Length;

            // Edges attached to this block, might be null if no edges
            public List<AsmEdge> Edges;

            public void AddEdge(in AsmEdge edge)
            {
                var edges = Edges;
                if (edges == null)
                {
                    edges = new List<AsmEdge>();
                    Edges = edges;
                }
                edges.Add(edge);
            }

            /// <summary>
            /// Sort edges by in-bound first, block index, line index
            /// </summary>
            public void SortEdges()
            {
                var edges = Edges;
                if (edges == null) return;
                edges.Sort(EdgeComparer.Instance);
            }

            private class EdgeComparer : IComparer<AsmEdge>
            {
                public static readonly EdgeComparer Instance = new EdgeComparer();

                public int Compare(AsmEdge x, AsmEdge y)
                {
                    // Order by kind first (InBound first, outbound first)
                    if (x.Kind != y.Kind)
                    {
                        return x.Kind == AsmEdgeKind.InBound ? -1 : 1;
                    }

                    // Order by Block Index
                    if (x.LineRef.BlockIndex != y.LineRef.BlockIndex) return x.LineRef.BlockIndex.CompareTo(y.LineRef.BlockIndex);

                    // Then order by Line Index
                    return x.LineRef.LineIndex.CompareTo(y.LineRef.LineIndex);
                }
            }
        }

        public enum AsmLineKind
        {
            Empty = 0,
            Comment,
            Directive,
            SourceFile,
            SourceLocation,
            SourceFileLocation, // computed line
            FunctionBegin,
            FunctionEnd,
            LabelDeclaration,
            Code,
            CodeCall,
            CodeBranch,
            CodeJump,
            CodeReturn,
            Data,
        }

        /// <summary>
        /// An <see cref="AsmToken"/> iterator skipping spaces.
        /// </summary>
        struct AsmTokenIterator
        {
            private readonly List<AsmToken> _tokens;
            private readonly int _startIndex;
            private readonly int _endIndex;
            private int _index;

            public AsmTokenIterator(List<AsmToken> tokens, int index, int length)
            {
                if (tokens == null) throw new ArgumentNullException(nameof(tokens));
                _tokens = tokens;
                if (index < 0 || index >= tokens.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}. Must be >= 0 and < {tokens.Count}");
                if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), $"Invalid length {length}. Must be >=0");
                _startIndex = index;
                _endIndex = index + length - 1;
                if (_endIndex >= tokens.Count) throw new ArgumentOutOfRangeException(nameof(length), $"Invalid length {length}. The final index {_endIndex} cannot be >= {tokens.Count}");
                _index = index;
            }

            public void Reset()
            {
                _index = _startIndex;
            }

            public bool TryGetNext(out AsmToken token)
            {
                while (_index <= _endIndex)
                {
                    var nextToken = _tokens[_index++];
                    if (nextToken.Kind == AsmTokenKind.Misc) continue;
                    token = nextToken;
                    return true;
                }

                token = default;
                return false;
            }

            public bool TryGetNext(out AsmToken token, out int tokenIndex)
            {
                while (_index <= _endIndex)
                {
                    tokenIndex = _index;
                    var nextToken = _tokens[_index++];
                    if (nextToken.Kind == AsmTokenKind.Misc) continue;
                    token = nextToken;
                    return true;
                }

                tokenIndex = -1;
                token = default;
                return false;
            }
        }

        [DebuggerDisplay("{ToDebuggerDisplay(),nq}")]
        [StructLayout(LayoutKind.Explicit)]
        public struct AsmLine
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // CAUTION: It is important to not put *any managed objects*
            // into this struct for GC efficiency
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            [FieldOffset(0)] public AsmLineKind Kind;

            [FieldOffset(4)] public int TokenIndex;

            // only valid when Kind == SourceFileLocation
            [FieldOffset(4)] public int SourceFileNumber;

            [FieldOffset(8)] public int Length;

            // only valid when Kind == SourceFileLocation
            [FieldOffset(8)] public int SourceLineNumber;

            // only valid when Kind == SourceFileLocation
            [FieldOffset(12)] public int SourceColumnNumber;

            /// <summary>
            /// Index into <see cref="_columnIndices"/>, the column indices will then contain <see cref="Length"/> minus 1 of column ints,
            /// each column corresponding the horizontal offset to a token.
            /// The first column is always 0 for the first token, hence the minus 1.
            /// Only get filled when asking for the text for a block.
            /// </summary>
            [FieldOffset(16)] public int ColumnIndex;

            private string ToDebuggerDisplay()
            {
                if (Kind == AsmLineKind.SourceFileLocation)
                {
                    return $"Line {Kind} File={SourceFileNumber} Line={SourceLineNumber} Column={SourceColumnNumber}";
                }
                else
                {
                    return $"Line {Kind} TokenIndex={TokenIndex} Length={Length} ColumnIndex={ColumnIndex}";
                }
            }
        }


        public enum AsmEdgeKind
        {
            InBound,
            OutBound,
        }

        /// <summary>
        /// An inbound or outbound connection for a block to another block+line
        /// </summary>
        [DebuggerDisplay("Edge {Kind} Origin: {OriginRef} LineRef: {LineRef}")]
        public struct AsmEdge : IEquatable<AsmEdge>
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // CAUTION: It is important to not put *any managed objects*
            // into this struct for GC efficiency
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            public AsmEdge(AsmEdgeKind kind, AsmLineRef originRef, AsmLineRef lineRef)
            {
                Kind = kind;
                OriginRef = originRef;
                LineRef = lineRef;
            }


            public AsmEdgeKind Kind;

            public AsmLineRef OriginRef;

            public AsmLineRef LineRef;

            public override string ToString()
            {
                return Kind == AsmEdgeKind.InBound ?
                    $"Edge {Kind} {LineRef} => {OriginRef}"
                    : $"Edge {Kind} {OriginRef} => {LineRef}";
            }

            public bool Equals(AsmEdge obj) => Kind == obj.Kind && OriginRef.Equals(obj.OriginRef) && LineRef.Equals(obj.LineRef);

            public override bool Equals(object obj) => obj is AsmEdge other && Equals(other);

            public override int GetHashCode() => base.GetHashCode();
        }

        public readonly struct AsmLineRef: IEquatable<AsmLineRef>
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // CAUTION: It is important to not put *any managed objects*
            // into this struct for GC efficiency
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


            public AsmLineRef(int blockIndex, int lineIndex)
            {
                BlockIndex = blockIndex;
                LineIndex = lineIndex;
            }

            public readonly int BlockIndex;

            public readonly int LineIndex;

            public override string ToString()
            {
                return $"Block: {BlockIndex}, Line: {LineIndex}";
            }

            public bool Equals(AsmLineRef obj) => BlockIndex == obj.BlockIndex && LineIndex == obj.LineIndex;

            public override bool Equals(object obj) => obj is AsmLineRef other && Equals(other);

            public override int GetHashCode() => base.GetHashCode();
        }

        /// <summary>
        /// Structure used to store all label references before they are getting fully resolved
        /// </summary>
        [DebuggerDisplay("TempLabelRef {LineRef} - String {StringIndex}, {StringLength}")]
        private readonly struct TempLabelRef
        {
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // CAUTION: It is important to not put *any managed objects*
            // into this struct for GC efficiency
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            public TempLabelRef(int globalBlockIndex, AsmLineRef lineRef, int stringIndex, int stringLength)
            {
                GlobalBlockIndex = globalBlockIndex;
                LineRef = lineRef;
                StringIndex = stringIndex;
                StringLength = stringLength;
            }

            public readonly int GlobalBlockIndex;

            public readonly AsmLineRef LineRef;

            public readonly int StringIndex;

            public readonly int StringLength;
        }

        private class DictionaryLocalLabel : Dictionary<StringSlice, AsmLineRef>
        {
            public DictionaryLocalLabel()
            {
            }

            public DictionaryLocalLabel(int capacity) : base(capacity)
            {
            }

            public AsmLineRef GlobalLabelLineRef;
        }

        private class DictionaryGlobalLabel : Dictionary<StringSlice, DictionaryLocalLabel>
        {
            public DictionaryGlobalLabel()
            {
            }

            public DictionaryGlobalLabel(int capacity) : base(capacity)
            {
            }

            public DictionaryLocalLabel GetOrCreate(StringSlice label, AsmLineRef globalLineRef)
            {
                if (!TryGetValue(label, out var dictLabel))
                {
                    dictLabel = new DictionaryLocalLabel();
                    Add(label, dictLabel);
                }
                dictLabel.GlobalLabelLineRef = globalLineRef;
                return dictLabel;
            }
        }

        internal struct UsedRegisters
        {
            private AsmTokenKindProvider _tokenProvider;

            /// <summary>
            /// Dictionary<lineNr, List<reg>>
            /// </summary>
            internal readonly Dictionary<int, List<string>> _linesRegisters;

            private readonly List<string> _tmp;
            private int _currentLineIdx;

            public UsedRegisters(int count)
            {
                _linesRegisters = new Dictionary<int, List<string>>(count);
                _tmp = new List<string>(2);
                _currentLineIdx = -1;
                _tokenProvider = null;
            }

            public void AddTokenProvider(AsmTokenKindProvider provider)
            {
                _tokenProvider = provider;
            }

            private int NumberOfOcurences(List<string> regs, string target)
            {
                var count = 0;
                foreach (var elm in regs)
                {
                    if (_tokenProvider.RegisterEqual(elm, target))
                    {
                        count++;
                    }
                }
                return count;
            }

            public int RegisterMatch(int lineIdx, string reg)
            {
                return LineContainsRegs(lineIdx, out var actualRegs)
                    ? NumberOfOcurences(actualRegs, reg)
                    : 0;
            }

            public bool RegisterEquality(string regA, string regB) => _tokenProvider.RegisterEqual(regA, regB);

            public List<string> CleanRegs(List<string> regs)
            {
                var tmpTokenProvider = _tokenProvider;
                var retVal = new List<string>(regs.Count);

                foreach (var reg in regs)
                {
                    if (!retVal.Exists(elm => tmpTokenProvider.RegisterEqual(reg, elm)))
                    {
                        retVal.Add(reg);
                    }
                }
                return retVal;
            }

            public bool LineContainsRegs(int lineIdx, out List<string> value)
            {
                return _linesRegisters.TryGetValue(lineIdx, out value);
            }

            public void Add(int lineIdx, string reg)
            {
                _currentLineIdx = lineIdx;
                _tmp.Add(reg);
            }

            public void PushLine()
            {
                if (_currentLineIdx == -1)
                {
                    // We haven't actually tried to add anything.
                    return;
                }
                _linesRegisters[_currentLineIdx] = new List<string>(_tmp);
                _tmp.Clear();
                _currentLineIdx = -1;
            }

            public int Count => _linesRegisters.Count;

            public void Clear()
            {
                _linesRegisters.Clear();
                _tmp.Clear();
            }
        }
    }
}
