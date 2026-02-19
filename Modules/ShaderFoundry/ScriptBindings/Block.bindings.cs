// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/Block.h")]
    internal struct BlockInternal : IInternalType<BlockInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_DeclaredTypeListHandle;
        internal FoundryHandle m_ReferencedTypeListHandle;
        internal FoundryHandle m_ReferencedFunctionListHandle;
        internal FoundryHandle m_EntryPointFunctionHandle;
        internal FoundryHandle m_InterfaceTypeHandle;
        internal FoundryHandle m_RenderStateDescriptorListHandle;
        internal FoundryHandle m_DefineListHandle;
        internal FoundryHandle m_IncludeListHandle;
        internal FoundryHandle m_KeywordListHandle;
        internal FoundryHandle m_PragmaDescriptorListHandle;
        internal FoundryHandle m_GeneratedIncludePathHandle;
        internal FoundryHandle m_ConstructorFunctionHandle;
        internal FoundryHandle m_PackageRequirementListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static BlockInternal Invalid();
        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }

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
        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, block.m_AttributeListHandle);
        public Namespace ContainingNamespace => new Namespace(container, block.m_ContainingNamespaceHandle);
        public IEnumerable<ShaderType> Types =>
            ListType.Enumerate<ShaderType>(container, block.m_DeclaredTypeListHandle);
        public IEnumerable<ShaderType> ReferencedTypes =>
            ListType.Enumerate<ShaderType>(container, block.m_ReferencedTypeListHandle);

        public IEnumerable<ShaderFunction> Functions => InterfaceType.StructFunctions;
        public IEnumerable<ShaderFunction> ReferencedFunctions =>
            ListType.Enumerate<ShaderFunction>(container, block.m_ReferencedFunctionListHandle);

        public ShaderFunction EntryPointFunction => new ShaderFunction(container, block.m_EntryPointFunctionHandle);
        public ShaderType InterfaceType => new ShaderType(container, block.m_InterfaceTypeHandle);
        public ShaderFunction ConstructorFunction => new ShaderFunction(container, block.m_ConstructorFunctionHandle);

        public IEnumerable<StructField> InterfaceFields => GetInterfaceFields();

        public IEnumerable<StructField> Inputs => GetFields(StructFieldInternal.Flags.kInput);

        public IEnumerable<StructField> Outputs => GetFields(StructFieldInternal.Flags.kOutput);

        IEnumerable<StructField> GetFields(StructFieldInternal.Flags flags)
        {
            foreach (var field in InterfaceFields)
            {
                if (field.HasFlag(flags))
                    yield return field;
            }
        }

        public IEnumerable<RenderStateDescriptor> RenderStates =>
            ListType.Enumerate<RenderStateDescriptor>(container, block.m_RenderStateDescriptorListHandle);

        public IEnumerable<DefineDescriptor> Defines =>
            ListType.Enumerate<DefineDescriptor>(container, block.m_DefineListHandle);

        public IEnumerable<IncludeDescriptor> Includes =>
            ListType.Enumerate<IncludeDescriptor>(container, block.m_IncludeListHandle);

        public IEnumerable<KeywordDescriptor> Keywords =>
            ListType.Enumerate<KeywordDescriptor>(container, block.m_KeywordListHandle);

        public IEnumerable<PragmaDescriptor> Pragmas =>
            ListType.Enumerate<PragmaDescriptor>(container, block.m_PragmaDescriptorListHandle);

        public IEnumerable<PackageRequirement> PackageRequirements =>
            ListType.Enumerate<PackageRequirement>(container, block.m_PackageRequirementListHandle);

        public Location Location => new Location(container, block.m_LocationHandle);

        private IEnumerable<StructField> GetInterfaceFields()
        {
            var blockInterfaceType = new ShaderType(Container, block.m_InterfaceTypeHandle);
            if (blockInterfaceType.IsValid)
                return blockInterfaceType.StructFields;

            return Array.Empty<StructField>();
        }

        // private
        internal Block(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out block);
        }

        public static Block Invalid => new Block(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is Block other && this.Equals(other);
        public bool Equals(Block other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(Block lhs, Block rhs) => lhs.Equals(rhs);
        public static bool operator!=(Block lhs, Block rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            internal readonly FoundryHandle blockHandle;
            internal string name;
            List<ShaderAttribute> attributes;
            public Namespace containingNamespace;
            List<ShaderType> types = new List<ShaderType>();
            List<ShaderType> referencedTypes;
            List<ShaderFunction> referencedFunctions;
            ShaderFunction entryPointFunction = ShaderFunction.Invalid;
            List<RenderStateDescriptor> m_RenderStates;
            List<DefineDescriptor> m_Defines;
            List<IncludeDescriptor> m_Includes;
            List<KeywordDescriptor> m_Keywords;
            List<PragmaDescriptor> m_Pragmas;
            List<PackageRequirement> m_PackageRequirements;
            public Location location;
            ShaderFunction Constructor;
            ShaderType.StructBuilder InterfaceBuilder;
            public ShaderType InterfaceType { get; private set; }
            bool finalized = false;

            public ShaderContainer Container => container;

            internal Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.containingNamespace = Utilities.BuildSymbolNamespace(container, name, DataType.Block);
                blockHandle = container.Create<BlockInternal>();
                InterfaceBuilder = new ShaderType.StructBuilder(Container, "Interface");
                InterfaceBuilder.containingNamespace = containingNamespace;
            }

            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
            internal void AddType(ShaderType type) => Utilities.AddToList(ref types, type);

            public void AddReferencedType(ShaderType type) => Utilities.AddToList(ref referencedTypes, type);
            public void AddFunction(ShaderFunction function)
            {
                if (InterfaceType.IsValid)
                    throw new InvalidOperationException("Cannot add a function after the interface type has been built.");
                if (function.ContainingNamespace.IsValid)
                    throw new InvalidOperationException("Block functions cannot have a namespace set.");

                if (InterfaceBuilder.functions != null)
                {
                    foreach (var f in InterfaceBuilder.functions)
                    {
                        if (f.Equals(function))
                            return;
                    }
                }
                InterfaceBuilder.AddFunction(function);
            }
            public void AddReferencedFunction(ShaderFunction function) => Utilities.AddToList(ref referencedFunctions, function);


            // Generates a base entry point function builder for this block.
            // This allows adding of custom attributes or other function data.
            public ShaderFunction.Builder CreateBuilderForEntryPointFunction(string fnName)
            {
                var builder = new ShaderFunction.Builder(container, fnName);
                builder.AddAttribute(new ShaderAttribute.Builder(container, "EntryPoint").Build());
                return builder;
            }

            public void SetEntryPointFunction(ShaderFunction function)
            {
                if (!function.IsValid)
                    throw new InvalidOperationException("Trying to set the entry point function to an invalid function.");
                if (function.ContainingNamespace.IsValid)
                    throw new InvalidOperationException("Entry point functions cannot have a namespace set.");
                if (InterfaceType.IsValid)
                    throw new InvalidOperationException("Cannot set the entry point function after the interface type has been built.");
                // For now, error if an entry point is set more than once.
                // We could remove the pre-existing one and find it in the list of functions to remove,
                // but given that we don't have any public api to remove functions
                // currently it seems like it's better to error for now.
                if (entryPointFunction.IsValid)
                    throw new InvalidOperationException("Entry point function has already been set.");

                if (!ValidateEntryPointFunction(function))
                {
                    throw new InvalidOperationException($"Setting entry point for block {name} with an invalid signature. " +
                        "Entry points must be of the signature 'void apply()'.");
                }

                entryPointFunction = function;
                // Make sure this is added to the interface type. This makes sure the function isn't added twice.
                AddFunction(function);
            }

            bool ValidateEntryPointFunction(ShaderFunction function)
            {
                if (!function.IsValid)
                    return false;

                // Function must have a return type of void
                if (!function.ReturnType.IsVoid)
                    return false;

                // An entry point function must be of the signature 'void fnName()'.
                // TODO @ Shaders: Update this to check for [EntryPoint] somehow
                foreach (var param in function.Parameters)
                {
                    return false;
                }
                return true;
            }

            public void AddRenderState(RenderStateDescriptor descriptor) => Utilities.AddToList(ref m_RenderStates, descriptor);
            public void AddDefine(DefineDescriptor descriptor) => Utilities.AddToList(ref m_Defines, descriptor);
            public void AddInclude(IncludeDescriptor descriptor) => Utilities.AddToList(ref m_Includes, descriptor);
            public void AddKeyword(KeywordDescriptor descriptor) => Utilities.AddToList(ref m_Keywords, descriptor);
            public void AddPragma(PragmaDescriptor descriptor) => Utilities.AddToList(ref m_Pragmas, descriptor);
            public void AddPackageRequirement(PackageRequirement packageRequirement)
            {
                Utilities.AddToList(ref m_PackageRequirements, packageRequirement);
            }

            public void AddInterfaceField(StructField variable)
            {
                if (InterfaceType.IsValid)
                    throw new InvalidOperationException("Cannot add an interface field after the interface type has been built");

                InterfaceBuilder.AddField(variable);
            }

            // Finalizes the interface type given the current builder which contains any fields/functions on the interface.
            // If the interface type has already been set, then the old value is returned.
            public ShaderType BuildInterfaceType()
            {
                if (InterfaceType.IsValid)
                    return InterfaceType;

                // Now that we know the full set of interface fields, create a constructor for the interface type if needed.
                CreateConstructorFunction(InterfaceBuilder);

                InterfaceType = InterfaceBuilder.Build();
                AddType(InterfaceType);
                return InterfaceType;
            }

            void CreateConstructorFunction(ShaderType.StructBuilder interfaceBuilder)
            {
                if (interfaceBuilder.fields == null || interfaceBuilder.fields.Count == 0)
                    return;

                var generator = new BlockConstructorGenerator(container);
                var fieldHandles = new List<FoundryHandle>();
                foreach (var field in interfaceBuilder.fields)
                    fieldHandles.Add(field.handle);
                var fnHandle = generator.BuildFunction(fieldHandles.ToArray());
                // TODO @ SHADERS: This should be improved to have the actual error message.
                // In the short term this at least lets us know if something failed vs. being completely silent.
                if (generator.hasErrors)
                    throw new Exception("Block constructor failed to build. Some interface field has errors");

                if (fnHandle.IsValid)
                {
                    Constructor = new ShaderFunction(container, fnHandle);
                    interfaceBuilder.AddFunction(Constructor);
                }
            }

            public Block Build()
            {
                if (finalized)
                    return new Block(Container, blockHandle);
                finalized = true;

                // Make sure to build the interface type if it wasn't already built
                BuildInterfaceType();

                var blockInternal = new BlockInternal();
                blockInternal.m_NameHandle = container.AddString(name);

                blockInternal.m_AttributeListHandle = ListType.Build(container, attributes);
                blockInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                blockInternal.m_DeclaredTypeListHandle = ListType.Build(container, types);
                blockInternal.m_ReferencedTypeListHandle = ListType.Build(container, referencedTypes);
                blockInternal.m_ReferencedFunctionListHandle = ListType.Build(container, referencedFunctions);
                blockInternal.m_EntryPointFunctionHandle = entryPointFunction.handle;
                blockInternal.m_InterfaceTypeHandle = InterfaceType.handle;
                blockInternal.m_ConstructorFunctionHandle = Constructor.handle;
                blockInternal.m_RenderStateDescriptorListHandle = ListType.Build(container, m_RenderStates);
                blockInternal.m_DefineListHandle = ListType.Build(container, m_Defines);
                blockInternal.m_IncludeListHandle = ListType.Build(container, m_Includes);
                blockInternal.m_KeywordListHandle = ListType.Build(container, m_Keywords);
                blockInternal.m_PragmaDescriptorListHandle = ListType.Build(container, m_Pragmas);
                blockInternal.m_PackageRequirementListHandle = ListType.Build(container, m_PackageRequirements);
                blockInternal.m_LocationHandle = location.handle;

                container.Set(blockHandle, blockInternal);
                return new Block(container, blockHandle);
            }
        }
    }
}
