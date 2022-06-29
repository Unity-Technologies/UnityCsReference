// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.ShaderFoundry
{
    [FoundryAPI]
    internal class ShaderBuilder
    {
        List<String> strs;
        int charLength;

        // builder-only state
        int currentIndent;
        int tabSize;

        public ShaderBuilder(int initialSize = 100, int indent = 0, int tabSize = 4)
        {
            strs = new List<string>(initialSize);
            charLength = 0;
            currentIndent = indent;
            this.tabSize = tabSize;
        }

        public void Space()
        {
            Add(_space);
        }

        public void Indent()
        {
            currentIndent += tabSize;
        }

        public void Deindent()
        {
            currentIndent -= tabSize;
        }

        public void Indentation()
        {
            // TODO: cached space strings would be faster... or just do a formatting pass later on
            for (int x = 0; x < currentIndent; x++)
                Space();
        }

        public void NewLine()
        {
            Add(_newline);
        }

        public void Add(string l0)
        {
            strs.Add(l0);
            charLength += l0.Length;
        }

        public void Add(string l0, string l1) { Add(l0); Add(l1); }
        public void Add(string l0, string l1, string l2) { Add(l0); Add(l1); Add(l2); }
        public void Add(string l0, string l1, string l2, string l3) { Add(l0); Add(l1); Add(l2); Add(l3); }
        public void Add(string l0, string l1, string l2, string l3, string l4) { Add(l0); Add(l1); Add(l2); Add(l3); Add(l4); }
        public void Add(string l0, string l1, string l2, string l3, string l4, string l5) { Add(l0); Add(l1); Add(l2); Add(l3); Add(l4); Add(l5); }
        public void Add(params string[] lArray)
        {
            foreach (var l in lArray)
                Add(l);
        }

        public void AddLine(string l0) { Indentation(); Add(l0); NewLine(); }
        public void AddLine(string l0, string l1) { Indentation(); Add(l0); Add(l1); NewLine(); }
        public void AddLine(string l0, string l1, string l2) { Indentation(); Add(l0); Add(l1); Add(l2); NewLine(); }
        public void AddLine(string l0, string l1, string l2, string l3) { Indentation(); Add(l0); Add(l1); Add(l2); Add(l3); NewLine(); }
        public void AddLine(string l0, string l1, string l2, string l3, string l4) { Indentation(); Add(l0); Add(l1); Add(l2); Add(l3); Add(l4); NewLine(); }
        public void AddLine(string l0, string l1, string l2, string l3, string l4, string l5) { Indentation(); Add(l0); Add(l1); Add(l2); Add(l3); Add(l4); Add(l5); NewLine(); }
        public void AddLine(params string[] lArray)
        {
            Indentation();
            foreach (var l in lArray)
                Add(l);
            NewLine();
        }

        static readonly string _newline = "\n";
        static readonly string _space = " ";
        static readonly string _blockNamespaceSuffix = "Block::";
        static readonly string _comma = ", "; // Space always follows a comma in current usage
        static readonly string _semicolon = ";";
        static readonly string _equal = " = ";
        static readonly string _openParen = "(";
        static readonly string _closeParen = ")";
        static readonly string _openSquare = "[";
        static readonly string _closeSquare = "]";
        static readonly string _openCurly = "{";
        static readonly string _closeCurly = "}";
        static readonly string _inout = "inout ";
        static readonly string _out = "out ";
        const char _tokenMarker = '$';
        static readonly string _tokenFunctionName = "$F:";
        static readonly string _tokenTypeName = "$T:";
        static readonly string _tokenDeclareVariable = "$V:";
        static readonly string _tokenEnd = "$";
        static readonly string _struct = "struct ";

        internal enum DeclarationMode { NoSemicolon, Semicolon }

        internal void AddFunctionNameInternal(ShaderFunction function)
        {
            if (!function.Exists)
            {
                Add("/* ERROR: non-existent function specified for function name */");
                return;
            }

            bool useDeferredDeclaration =
                (function.ParentBlock.Exists && !function.ParentBlock.IsValid) ||
                (!function.IsValid);

            if (useDeferredDeclaration)
            {
                Add(_tokenFunctionName, function.handle.ToString(), _tokenEnd);
            }
            else
            {
                // if there's a parent block, the type is in the block namespace
                if (function.ParentBlock.IsValid)
                    Add(function.ParentBlock.Name, _blockNamespaceSuffix);
                Add(function.Name);
            }
        }

        internal void AddNonArrayTypeNameInternal(ShaderType type)
        {
            if (!type.Exists)
            {
                Add("/* ERROR: cannot get name of non-existent type */");
                return;
            }

            bool useDeferredDeclaration =
                (type.ParentBlock.Exists && !type.ParentBlock.IsValid) ||
                (!type.IsValid);

            if (useDeferredDeclaration)
            {
                Add(_tokenTypeName, type.handle.ToString(), _tokenEnd);
            }
            else
            {
                if (type.IsArray)
                {
                    Add("/* ERROR: cannot use an array type here */");
                    return;
                }

                // if there's a parent block, the type is in the block namespace
                if (type.ParentBlock.IsValid)
                    Add(type.ParentBlock.Name, _blockNamespaceSuffix);
                Add(type.Name);
            }
        }

        internal void VariableDeclarationInternal(ShaderType type, string name, DeclarationMode mode, string defaultValue = null)
        {
            if (!type.Exists)
            {
                Add("/* ERROR: non-existent type specified for variable ", name, " */");
                return;
            }

            bool useDeferredDeclaration =
                (type.ParentBlock.Exists && !type.ParentBlock.IsValid) ||
                (!type.IsValid) ||
                (type.IsArray && !type.ArrayElementType.IsValid);
            // we must catch unfinished array element types in the var decl to correctly handle arrays of arrays of unfinished types
            // this is because the array count must be appended in reverse order, so we must use the $V: token

            if (useDeferredDeclaration)
            {
                Add(_tokenDeclareVariable, type.handle.ToString(), ",", name, _tokenEnd);
            }
            else
            {
                if (type.IsVoid)
                {
                    Add("/* ERROR: cannot declare a variable of type void ", name, " */");
                    return;
                }
                else if (type.IsArray)
                {
                    // have to declare arrays in reverse order (standard HLSL/C syntax)
                    // this is not ideal in terms of string allocations, but I think any approach requires some form of allocation..
                    var nameSuffix = _openSquare + (type.ArrayElements > 0 ? type.ArrayElements.ToString() : "") + _closeSquare;
                    var modifiedName = name + nameSuffix;
                    VariableDeclarationInternal(type.ArrayElementType, modifiedName, DeclarationMode.NoSemicolon);
                }
                else
                {
                    AddNonArrayTypeNameInternal(type);
                    Add(_space, name);
                }
            }

            if (defaultValue != null)
                Add(_equal, defaultValue);

            if (mode == DeclarationMode.Semicolon)
                Add(_semicolon);
        }

        internal void FunctionCallInternal(ShaderFunction function, DeclarationMode mode, params string[] arguments)
        {
            if (!function.Exists)
            {
                Add("/* ERROR: non-existent function specified for function call */");
                return;
            }

            AddFunctionNameInternal(function);

            Add(_openParen);

            // Iterate across the list of parameters (if any) and emit each one
            for (int a = 0; a < arguments.Length; a++)
            {
                if (a > 0)
                    Add(_comma);
                Add(arguments[a]);
            }

            Add(_closeParen);

            if (mode == DeclarationMode.Semicolon)
                Add(_semicolon);
        }

        // This will emit a simple variable declaration with optional default value
        public void DeclareVariable(ShaderType type, string name, string defaultValue = null)
        {
            Indentation();
            VariableDeclarationInternal(type, name, DeclarationMode.Semicolon, defaultValue);
            NewLine();
        }

        // These will emit a call to 'function' using the supplied arguments
        public void CallFunction(ShaderFunction function, params string[] arguments)
        {
            Indentation();
            FunctionCallInternal(function, DeclarationMode.Semicolon, arguments);
            NewLine();
        }

        public void CallFunctionWithReturn(ShaderFunction function, string returnVariable, params string[] arguments)
        {
            Indentation();
            Add(returnVariable);
            Add(_equal);
            FunctionCallInternal(function, DeclarationMode.Semicolon, arguments);
            NewLine();
        }

        public void CallFunctionWithDeclaredReturn(ShaderFunction function, ShaderType returnType, string returnVariable, params string[] arguments)
        {
            Indentation();
            VariableDeclarationInternal(returnType, returnVariable, DeclarationMode.NoSemicolon);
            Add(_equal);
            FunctionCallInternal(function, DeclarationMode.Semicolon, arguments);
            NewLine();
        }

        bool DeclareFunctionSignatureInternal(ShaderFunction function)
        {
            if (!function.IsValid)
            {
                AddLine("/* ERROR: cannot declare invalid function */");
                return false;
            }

            var returnType = function.ReturnType;
            if (!returnType.IsValid)
            {
                AddLine("/* ERROR: cannot declare function with invalid return type */");
                return false;
            }

            if (returnType.IsArray)
            {
                AddLine("/* ERROR: cannot declare a function with an array return type */");
                return false;
            }

            Indentation();

            AddNonArrayTypeNameInternal(returnType);
            Add(_space, function.Name, _openParen);

            var paramIndex = 0;
            foreach (var param in function.Parameters)
            {
                if (paramIndex != 0)
                    Add(_comma);

                if (param.IsOutput)
                {
                    if (param.IsInput)
                        Add(_inout);
                    else
                        Add(_out);
                }

                VariableDeclarationInternal(param.Type, param.Name, DeclarationMode.NoSemicolon);
                ++paramIndex;
            }
            Add(_closeParen);
            return true;
        }

        public void DeclareFunctionSignature(ShaderFunction function)
        {
            if (!DeclareFunctionSignatureInternal(function))
                return;

            Add(_semicolon);
            NewLine();
        }

        public void DeclareFunction(ShaderFunction function)
        {
            if (!DeclareFunctionSignatureInternal(function))
                return;

            NewLine();

            using (BlockScope())
            {
                AddWithTokenTranslation(function.Body, function.Container);
            }
        }

        public void DeclareStruct(ShaderType type)
        {
            if (type.IsStruct)
            {
                AddLine(_struct, type.Name);
                using (BlockScope(true))
                {
                    foreach (var field in type.StructFields)
                        DeclareVariable(field.Type, field.Name);
                }
            }
        }

        void AddSubString(string str, int startOffsetInclusive, int endOffsetExclusive)
        {
            int length = endOffsetExclusive - startOffsetInclusive;
            if (length > 0)
            {
                // don't GC alloc the substring if we can help it
                // possible this is not necessary if str.SubString(0, str.Length) just returns str already... needs test
                if ((startOffsetInclusive == 0) && (length == str.Length))
                    Add(str);
                else
                    Add(str.Substring(startOffsetInclusive, length));
            }
        }

        static bool IsDigit(char c)
        {
            return (c >= '0') && (c <= '9');
        }

        static bool TryParseInt(string str, int startOffsetInclusive, int endOffsetExclusive, out int value, out int end)
        {
            // search for the end of the int
            int offset;
            for (offset = startOffsetInclusive; offset < endOffsetExclusive; offset++)
            {
                if (!IsDigit(str[offset]))
                {
                    break;
                }
            }
            end = offset;
            var intString = str.Substring(startOffsetInclusive, end - startOffsetInclusive);
            return Int32.TryParse(intString, out value);
        }

        int ParseSimpleToken(string str, int startOffsetInclusive, int endOffsetExclusive, char token)
        {
            // Expected Format: "{Token}:{id}"
            if (str[startOffsetInclusive + 1] != ':')
            {
                var tokenSubStr = str.Substring(startOffsetInclusive, endOffsetExclusive - startOffsetInclusive);
                throw new Exception($"Malformed token found when parsing '{tokenSubStr}'. Expected ':' after '${token}'");
            }

            if (!TryParseInt(str, startOffsetInclusive + 2, endOffsetExclusive, out int tokenId, out int end))
            {
                var tokenSubStr = str.Substring(startOffsetInclusive, endOffsetExclusive - startOffsetInclusive);
                throw new Exception($"Token '{tokenSubStr}' is malformed. The identifier number is not a valid integer.");
            }
            return tokenId;
        }

        void ParseTokenWithName(string str, int startOffsetInclusive, int endOffsetExclusive, char token, out int tokenId, out string outName)
        {
            // Expected Format: "{Token}:{id},{name}"
            tokenId = -1;
            outName = null;
            int offset = startOffsetInclusive + 1;
            if (str[offset] != ':')
            {
                var tokenSubStr = str.Substring(startOffsetInclusive, endOffsetExclusive - startOffsetInclusive);
                throw new Exception($"Malformed token found when parsing '{tokenSubStr}'. Expected ':' after '${token}'");
            }

            ++offset;
            if (!TryParseInt(str, offset, endOffsetExclusive, out tokenId, out int end))
            {
                var tokenSubStr = str.Substring(startOffsetInclusive, endOffsetExclusive - startOffsetInclusive);
                throw new Exception($"Token '{tokenSubStr}' is malformed. The identifier number is not a valid integer.");
            }

            offset = end;
            if (str[offset] != ',')
            {
                var tokenSubStr = str.Substring(startOffsetInclusive, endOffsetExclusive - startOffsetInclusive);
                throw new Exception($"Malformed token found when parsing $V: '{tokenSubStr}'. Expected ',' after the identifier.");
            }

            offset++;
            outName = str.Substring(offset, endOffsetExclusive - offset);
        }

        void ProcessToken(string str, int startOffsetInclusive, int endOffsetExclusive, ShaderContainer container)
        {
            int length = endOffsetExclusive - startOffsetInclusive;
            if (length <= 2)
                return; // can't possibly be a valid token

            // Special tokens are used to done ids that are to be replaced with actual identifiers later:
            // - "$T:{id}$": Replaces the string with the type name with the given id.
            // - "$F:{id}$": Replaces the string with the function name with the given id.
            // - "$V:{id},{varName}$": Replaces the string with the variable declaration with the given id.
            // Variable declarations also need the variable name due to how arrays have to be declared: "type name[size]".
            // This may go away if we ever add a variable instance object.
            int offset = startOffsetInclusive;
            switch (str[offset])
            {
                case 'V': // variable declaration
                {
                    ParseTokenWithName(str, startOffsetInclusive, endOffsetExclusive, 'V', out int shaderTypeIndex, out string varName);
                    // declare it!
                    FoundryHandle shaderTypeHandle = new FoundryHandle();
                    shaderTypeHandle.Handle = (uint)shaderTypeIndex;
                    var shaderType = new ShaderType(container, shaderTypeHandle);
                    VariableDeclarationInternal(shaderType, varName, DeclarationMode.NoSemicolon);
                    break;
                }
                case 'F': // function call - function name
                {
                    int tokenId = ParseSimpleToken(str, startOffsetInclusive, endOffsetExclusive, 'F');
                    FoundryHandle shaderFunctionHandle = new FoundryHandle();
                    shaderFunctionHandle.Handle = (uint)tokenId;
                    var shaderFunction = new ShaderFunction(container, shaderFunctionHandle);
                    AddFunctionNameInternal(shaderFunction);
                    break;
                }
                case 'T': // type name
                {
                    int typeId = ParseSimpleToken(str, startOffsetInclusive, endOffsetExclusive, 'T');
                    FoundryHandle shaderTypeHandle = new FoundryHandle();
                    shaderTypeHandle.Handle = (uint)typeId;
                    var shaderType = new ShaderType(container, shaderTypeHandle);
                    AddNonArrayTypeNameInternal(shaderType);
                    break;
                }
                default:
                {
                    var tokenSubStr = str.Substring(startOffsetInclusive, endOffsetExclusive - startOffsetInclusive);
                    throw new Exception($"Malformed token found when parsing '{tokenSubStr}'. Identifier {str[offset]} was unexpected.");
                }
            }
        }

        internal void AddWithTokenTranslation(string str, ShaderContainer container)
        {
            int startRun = 0;
            int length = str.Length;
            int offset = 0;

            while (offset < length)
            {
                if (str[offset] != _tokenMarker)
                    offset++;
                else
                {
                    // found a token
                    // first complete the plain text run up to this point
                    AddSubString(str, startRun, offset);

                    // find the end of the token
                    var tokenStart = offset + 1;
                    var tokenEnd = tokenStart;
                    while (tokenEnd < length && str[tokenEnd] != _tokenMarker)
                        tokenEnd++;

                    // process the token
                    ProcessToken(str, tokenStart, tokenEnd, container);

                    // start a new plain text run after the token marker
                    startRun = offset = tokenEnd + 1;
                }
            }
            // we hit the end of the string, complete the current plain text run
            // (if offset is greater than length, then string was terminated inside a token)
            if (offset == length)
                AddSubString(str, startRun, offset);
        }

        // MUST BE DISPOSED, example:   using(var block = BlockScope()) { block.foo(); }
        public Block BlockScope(bool semicolon = false)
        {
            AddLine(_openCurly);
            Indent();
            return new Block(this, semicolon);
        }

        public readonly ref struct Block
        {
            readonly ShaderBuilder parent;
            readonly bool semicolon;

            public Block(ShaderBuilder parent, bool semicolon = false)
            {
                this.parent = parent;
                this.semicolon = semicolon;
            }

            public void Dispose()
            {
                parent.Deindent();
                if (semicolon)
                    parent.AddLine(_closeCurly, _semicolon);
                else
                    parent.AddLine(_closeCurly);
            }
        }

        public string ConvertToString()
        {
            //         var result = string.Create(charLength, this, (span, builder) =>
            //         {
            //             // TODO: once we have spans (.NEt 5) this is a better way to do it, fewer allocations
            //             foreach (var s in builder.strs)
            //             {
            //                 s.AsSpan().CopyTo(chars.Slice(position));
            //             }
            //         });

            char[] buf = new char[charLength];
            int len = 0;
            foreach (var s in strs)
            {
                int slen = s.Length;
                s.CopyTo(0, buf, len, slen);
                len += slen;
            }
            var result = new string(buf, 0, len);
            return result;
        }

        public override string ToString()
        {
            return ConvertToString();
        }
    }
}
