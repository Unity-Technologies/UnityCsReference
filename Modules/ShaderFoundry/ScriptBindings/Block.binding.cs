// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using BlockInput = UnityEditor.ShaderFoundry.BlockVariable;
using BlockOutput = UnityEditor.ShaderFoundry.BlockVariable;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/Block.h")]
    internal struct BlockInternal : IInternalType<BlockInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_DeclaredTypeListHandle;
        internal FoundryHandle m_ReferencedTypeListHandle;
        internal FoundryHandle m_DeclaredFunctionListHandle;
        internal FoundryHandle m_ReferencedFunctionListHandle;
        internal FoundryHandle m_EntryPointFunctionHandle;
        internal FoundryHandle m_InputVariableListHandle;
        internal FoundryHandle m_OutputVariableListHandle;
        internal FoundryHandle m_PropertyVariableListHandle;
        internal FoundryHandle m_CommandListHandle;
        internal FoundryHandle m_DefineListHandle;
        internal FoundryHandle m_IncludeListHandle;
        internal FoundryHandle m_KeywordListHandle;
        internal FoundryHandle m_PragmaListHandle;
        internal FoundryHandle m_PassParentHandle;
        internal FoundryHandle m_TemplateParentHandle;

        internal extern static BlockInternal Invalid();
        internal extern bool IsValid { [NativeMethod("IsValid")] get; }

        internal extern FoundryHandle GetPassParentHandle();
        internal extern FoundryHandle GetTemplateParentHandle();
        internal extern bool HasParent();

        // IInternalType
        BlockInternal IInternalType<BlockInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct Block : IEquatable<Block>, IPublicType<Block>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockInternal block;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        Block IPublicType<Block>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new Block(container, handle);

        // public API
        public ShaderContainer Container => container;

        // exists if it has been allocated
        public bool Exists => (container != null && handle.IsValid);

        // must have a name assigned to be considered valid
        public bool IsValid => Exists && block.IsValid;

        public string Name => container?.GetString(block.m_NameHandle) ?? string.Empty;
        public IEnumerable<ShaderAttribute> Attributes => FixedHandleListInternal.Enumerate<ShaderAttribute>(container, block.m_AttributeListHandle);
        public IEnumerable<ShaderType> Types
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_DeclaredTypeListHandle);
                return list.Select<ShaderType>(localContainer, (handle) => (new ShaderType(localContainer, handle)));
            }
        }
        public ShaderType GetType(string typeName) => Container.GetType(typeName, this);
        public IEnumerable<ShaderType> ReferencedTypes
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_ReferencedTypeListHandle);
                return list.Select<ShaderType>(localContainer, (handle) => (new ShaderType(localContainer, handle)));
            }
        }

        public IEnumerable<ShaderFunction> Functions
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_DeclaredFunctionListHandle);
                return list.Select<ShaderFunction>(localContainer, (handle) => (new ShaderFunction(localContainer, handle)));
            }
        }
        public IEnumerable<ShaderFunction> ReferencedFunctions
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_ReferencedFunctionListHandle);
                return list.Select<ShaderFunction>(localContainer, (handle) => (new ShaderFunction(localContainer, handle)));
            }
        }

        public ShaderFunction EntryPointFunction => new ShaderFunction(container, block.m_EntryPointFunctionHandle);

        public IEnumerable<BlockInput> Inputs => GetVariableEnumerable(block.m_InputVariableListHandle);

        public IEnumerable<BlockOutput> Outputs => GetVariableEnumerable(block.m_OutputVariableListHandle);

        public IEnumerable<CommandDescriptor> Commands
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_CommandListHandle);
                return list.Select<CommandDescriptor>(localContainer, (handle) => (new CommandDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<DefineDescriptor> Defines
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_DefineListHandle);
                return list.Select<DefineDescriptor>(localContainer, (handle) => (new DefineDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<IncludeDescriptor> Includes
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_IncludeListHandle);
                return list.Select<IncludeDescriptor>(localContainer, (handle) => (new IncludeDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<KeywordDescriptor> Keywords
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_KeywordListHandle);
                return list.Select<KeywordDescriptor>(localContainer, (handle) => (new KeywordDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<PragmaDescriptor> Pragmas
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(block.m_PragmaListHandle);
                return list.Select<PragmaDescriptor>(localContainer, (handle) => (new PragmaDescriptor(localContainer, handle)));
            }
        }

        IEnumerable<BlockVariable> GetVariableEnumerable(FoundryHandle listHandle)
        {
            var localContainer = Container;
            var list = new FixedHandleListInternal(listHandle);
            return list.Select<BlockVariable>(localContainer, (handle) => (new BlockVariable(localContainer, handle)));
        }

        public TemplatePass ParentPass => new TemplatePass(container, block.GetPassParentHandle());
        public Template ParentTemplate => new Template(container, block.GetTemplateParentHandle());

        // private
        internal Block(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out block);
        }

        public static Block Invalid => new Block(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is Block other && this.Equals(other);
        public bool Equals(Block other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(Block lhs, Block rhs) => lhs.Equals(rhs);
        public static bool operator!=(Block lhs, Block rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            internal readonly FoundryHandle blockHandle;
            internal FoundryHandle passParentHandle;
            internal FoundryHandle templateParentHandle;
            internal string name;
            List<ShaderAttribute> attributes;
            List<ShaderType> types = new List<ShaderType>();
            List<ShaderType> referencedTypes = new List<ShaderType>();
            List<ShaderFunction> functions = new List<ShaderFunction>();
            List<ShaderFunction> referencedFunctions = new List<ShaderFunction>();
            ShaderFunction entryPointFunction = ShaderFunction.Invalid;

            List<CommandDescriptor> m_Commands = new List<CommandDescriptor>();
            List<DefineDescriptor> m_Defines = new List<DefineDescriptor>();
            List<IncludeDescriptor> m_Includes = new List<IncludeDescriptor>();
            List<KeywordDescriptor> m_Keywords = new List<KeywordDescriptor>();
            List<PragmaDescriptor> m_Pragmas = new List<PragmaDescriptor>();
            bool finalized = false;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
                : this(container, name, FoundryHandle.Invalid(), FoundryHandle.Invalid())
            {
            }

            internal Builder(ShaderContainer container, string name, FoundryHandle passParentHandle, FoundryHandle templateParentHandle)
            {
                this.container = container;
                this.name = name;
                this.passParentHandle = passParentHandle;
                this.templateParentHandle = templateParentHandle;
                blockHandle = container.Create<BlockInternal>();
            }

            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
            internal void AddType(ShaderType type) { types.Add(type); }

            public void AddReferencedType(ShaderType type) { referencedTypes.Add(type); }
            public void AddFunction(ShaderFunction function) { functions.Add(function); }
            public void AddReferencedFunction(ShaderFunction function) { referencedFunctions.Add(function); }
            public void SetEntryPointFunction(ShaderFunction function) { entryPointFunction = function; AddFunction(entryPointFunction); }

            public void AddCommand(CommandDescriptor descriptor) { m_Commands.Add(descriptor); }
            public void AddDefine(DefineDescriptor descriptor) { m_Defines.Add(descriptor); }
            public void AddInclude(IncludeDescriptor descriptor) { m_Includes.Add(descriptor); }
            public void AddKeyword(KeywordDescriptor descriptor) { m_Keywords.Add(descriptor); }
            public void AddPragma(PragmaDescriptor descriptor) { m_Pragmas.Add(descriptor); }

            // Get the input and output types for this function (assumed to be an entry point)
            static void GetInOutTypes(ShaderFunction function, out ShaderType inputType, out ShaderType outputType)
            {
                inputType = outputType = ShaderType.Invalid;
                if (function.IsValid)
                {
                    outputType = function.ReturnType;
                    var parameters = function.Parameters.GetEnumerator();
                    if (parameters.MoveNext())
                        inputType = parameters.Current.Type;
                }
            }

            static List<BlockVariable> BuildVariablesFromTypeFields(ShaderContainer container, ShaderType type)
            {
                var results = new List<BlockVariable>();
                foreach (var field in type.StructFields)
                {
                    var builder = new BlockVariable.Builder(container);
                    builder.Name = field.Name;
                    builder.Type = field.Type;
                    foreach (var attribute in field.Attributes)
                        builder.AddAttribute(attribute);
                    results.Add(builder.Build());
                }
                return results;
            }

            public Block Build()
            {
                if (finalized)
                    return new Block(Container, blockHandle);
                finalized = true;

                var blockInternal = new BlockInternal();
                blockInternal.m_NameHandle = container.AddString(name);

                blockInternal.m_AttributeListHandle = FixedHandleListInternal.Build(container, attributes);
                blockInternal.m_DeclaredTypeListHandle = FixedHandleListInternal.Build(container, types, (t) => (t.handle));
                blockInternal.m_ReferencedTypeListHandle = FixedHandleListInternal.Build(container, referencedTypes, (t) => (t.handle));
                blockInternal.m_DeclaredFunctionListHandle = FixedHandleListInternal.Build(container, functions, (f) => (f.handle));
                blockInternal.m_ReferencedFunctionListHandle = FixedHandleListInternal.Build(container, referencedFunctions, (f) => (f.handle));
                blockInternal.m_EntryPointFunctionHandle = entryPointFunction.handle;

                // Build up the input/output variable list from the entry point function
                GetInOutTypes(entryPointFunction, out var inputType, out var outputType);
                var inputs = BuildVariablesFromTypeFields(Container, inputType);
                var outputs = BuildVariablesFromTypeFields(Container, outputType);

                blockInternal.m_InputVariableListHandle = FixedHandleListInternal.Build(container, inputs, (v) => (v.handle));
                blockInternal.m_OutputVariableListHandle = FixedHandleListInternal.Build(container, outputs, (v) => (v.handle));

                blockInternal.m_CommandListHandle = FixedHandleListInternal.Build(container, m_Commands, (c) => (c.handle));
                blockInternal.m_DefineListHandle = FixedHandleListInternal.Build(container, m_Defines, (d) => (d.handle));
                blockInternal.m_IncludeListHandle = FixedHandleListInternal.Build(container, m_Includes, (i) => (i.handle));
                blockInternal.m_KeywordListHandle = FixedHandleListInternal.Build(container, m_Keywords, (k) => (k.handle));
                blockInternal.m_PragmaListHandle = FixedHandleListInternal.Build(container, m_Pragmas, (p) => (p.handle));
                blockInternal.m_PassParentHandle = passParentHandle;
                blockInternal.m_TemplateParentHandle = templateParentHandle;

                container.Set(blockHandle, blockInternal);
                return new Block(container, blockHandle);
            }
        }
    }
}
