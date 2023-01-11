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
using UnityEngine;

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
            public List<BlockVariable> interfaceFields;
            public ShaderType InterfaceType { get; private set; }
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
            public void SetEntryPointFunction(ShaderFunction function)
            {
                entryPointFunction = HandleLegacyEntryPointFunction(function);
                AddFunction(entryPointFunction);
            }

            bool ValidateEntryPointFunction(ShaderFunction function)
            {
                if (!function.IsValid)
                    return false;

                // Function must have a return type of void
                if (!function.ReturnType.IsVoid)
                    return false;

                // Function must have only one parameter that is "inout" with the
                // type name of either "Interface" or the block's name.
                // TODO @ Shaders: Update this to check for [EntryPoint] somehow
                var paramCount = 0;
                foreach (var param in function.Parameters)
                {
                    bool isInOut = param.IsInput && param.IsOutput;
                    if (!isInOut || paramCount != 0)
                        return false;

                    bool isParamTypeValid = param.Type.Name == "Interface" || param.Type.Name == this.name;
                    if (!isParamTypeValid)
                        return false;
                    ++paramCount;
                }
                return true;
            }

            public void AddCommand(CommandDescriptor descriptor) { m_Commands.Add(descriptor); }
            public void AddDefine(DefineDescriptor descriptor) { m_Defines.Add(descriptor); }
            public void AddInclude(IncludeDescriptor descriptor) { m_Includes.Add(descriptor); }
            public void AddKeyword(KeywordDescriptor descriptor) { m_Keywords.Add(descriptor); }
            public void AddPragma(PragmaDescriptor descriptor) { m_Pragmas.Add(descriptor); }

            // Adds a new interface field to the block.
            public void AddInterfaceField(BlockVariable variable)
            {
                if (InterfaceType.IsValid)
                    throw new InvalidOperationException("Cannot add an interface field after the interface type has been built");

                Utilities.AddToList(ref interfaceFields, variable);
            }

            // Builds the interface type from current interface fields.
            // If the interface type has already been set, then the old value is returned.
            public ShaderType BuildInterfaceType(string typeName = "Interface")
            {
                if (InterfaceType.IsValid)
                    return InterfaceType;

                var builder = CreateBuilderForInterfaceType(typeName);
                SetInterfaceType(builder.Build());
                return InterfaceType;
            }

            // Creates the builder from the current interface fields. Attributes can be added to the resultant type builder.
            public ShaderType.StructBuilder CreateBuilderForInterfaceType(string typeName = "Interface")
            {
                var typeBuilder = new ShaderType.StructBuilder(this, typeName);
                if (interfaceFields != null)
                {
                    foreach (var field in interfaceFields)
                    {
                        var fieldBuilder = new StructField.Builder(container, field.Name, field.Type);
                        foreach (var attribute in field.Attributes)
                            fieldBuilder.AddAttribute(attribute);

                        typeBuilder.AddField(fieldBuilder.Build());
                    }
                }
                return typeBuilder;
            }

            // Sets the current interface type for the block.
            public void SetInterfaceType(ShaderType type)
            {
                if (InterfaceType.IsValid)
                    throw new InvalidOperationException($"Interface type for block '{name}' has already been set. Cannot set interface type twice.");

                InterfaceType = type;
                AddType(type);
            }

            // Generates a base entry point function builder for this block. The interface type must be set before calling this.
            public ShaderFunction.Builder CreateBuilderForEntryPointFunction(string fnName, string selfName)
            {
                if (!InterfaceType.IsValid)
                    throw new InvalidOperationException("You must call 'SetInterfaceType' before building an entry point function.");

                var builder = new ShaderFunction.Builder(this, fnName);
                var paramBuilder = new FunctionParameter.Builder(container, selfName, InterfaceType, true, true);
                builder.AddParameter(paramBuilder.Build());
                builder.AddAttribute(new ShaderAttribute.Builder(container, "EntryPoint").Build());
                return builder;
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

                List<BlockVariable> inputs = new List<BlockInput>();
                List<BlockVariable> outputs = new List<BlockInput>();
                if (interfaceFields != null)
                {
                    foreach (var field in interfaceFields)
                    {
                        if (field.IsInput)
                            inputs.Add(field);
                        if (field.IsOutput)
                            outputs.Add(field);
                    }
                }

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

            // TODO @ SHADERS: Below here is utilities for building from a legacy entry point.
            // This can be deleted once we clean up all examples, and shader graph.
            ShaderFunction HandleLegacyEntryPointFunction(ShaderFunction function)
            {
                // Old Interface, build a wrapper
                if (!ValidateEntryPointFunction(function))
                {
                    Debug.LogWarning($"Setting entry point for block {name} which uses a legacy syntax. " +
                        "A wrapper entry point has been generated from the provide entry point. " +
                        "Please update to use AddInterfaceField, BuildInterfaceType, and CreateEntryPointFunction.");
                    AddFunction(function);
                    return BuildLegacyFromEntryPoint(function);
                }
                else
                    return function;
            }

            ShaderFunction BuildLegacyFromEntryPoint(ShaderFunction legacyEntryPointFn)
            {
                const string selfName = "self";
                const string inputsName = "inputs";
                const string outputsName = "outputs";
                Dictionary<string, BlockVariable.Builder> variables = new Dictionary<string, BlockVariable.Builder>();
                void AddVariable(StructField field, bool input, bool output)
                {
                    // The field name already exists, make sure there's not a type conflict
                    if (variables.TryGetValue(field.Name, out var builder))
                    {
                        if (builder.Type != field.Type)
                        {
                            var message = $"Cannot upgrade old interface field {field.Name}. " +
                                $"Variable is declared with conflicting types {builder.Type.Name} and {field.Type.Name}";
                            throw new InvalidOperationException(message);
                        }
                    }
                    // The field name doesn't exist, create it
                    else
                    {
                        builder = new BlockVariable.Builder(Container);
                        builder.Name = field.Name;
                        builder.Type = field.Type;
                        // Initialize the flags to false so that the ors work correctly.
                        builder.IsInput = false;
                        builder.IsOutput = false;
                        variables[field.Name] = builder;
                    }

                    builder.IsInput |= input;
                    builder.IsOutput |= output;
                    foreach (var attribute in field.Attributes)
                        builder.AddAttribute(attribute);
                }

                GetInOutTypes(legacyEntryPointFn, out var inputType, out var outputType);
                // First have to merge inputs and outputs together so we know if something was an inout.
                // This can cause errors if the old interface had an input and output with the same name but different types.
                foreach (var field in inputType.StructFields)
                    AddVariable(field, true, false);
                foreach (var field in outputType.StructFields)
                    AddVariable(field, false, true);

                // Now build the interface from the merged types
                foreach (var variableBuilder in variables.Values)
                    AddInterfaceField(variableBuilder.Build());
                BuildInterfaceType();

                // Now build the entry point function
                var fnBuilder = CreateBuilderForEntryPointFunction("apply", selfName);

                // Copy the inputs to the old inputs struct
                fnBuilder.DeclareVariable(inputType, inputsName);
                foreach (var field in inputType.StructFields)
                    fnBuilder.AddLine($"{inputsName}.{field.Name} = {selfName}.{field.Name};");

                // Call the old entry point
                fnBuilder.CallFunctionWithDeclaredReturn(legacyEntryPointFn, outputType, outputsName, inputsName);

                // Copy the outputs from the old outputs struct
                foreach (var field in outputType.StructFields)
                    fnBuilder.AddLine($"{selfName}.{field.Name} = {outputsName}.{field.Name};");

                return fnBuilder.Build();
            }

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
        }
    }
}
