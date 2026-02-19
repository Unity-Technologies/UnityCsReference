// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/BlockShader.h")]
    internal struct BlockShaderInternal : IInternalType<BlockShaderInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_BlockShaderInterfaceListHandle;
        internal FoundryHandle m_BlockListHandle;
        internal FoundryHandle m_BlockSequenceListHandle;
        internal FoundryHandle m_CustomAttributeDefinitionListHandle;
        internal FoundryHandle m_CustomizationPointImplementationListHandle;
        internal FoundryHandle m_ShaderCustomEditorListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static BlockShaderInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        BlockShaderInternal IInternalType<BlockShaderInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct BlockShader : IEquatable<BlockShader>, IPublicType<BlockShader>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockShaderInternal blockShaderInternal;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        BlockShader IPublicType<BlockShader>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new BlockShader(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public string Name => container?.GetString(blockShaderInternal.m_NameHandle) ?? string.Empty;
        public IEnumerable<ShaderAttribute> Attributes
        {
            get
            {
                var listHandle = blockShaderInternal.m_AttributeListHandle;
                return ListType.Enumerate<ShaderAttribute>(container, listHandle);
            }
        }
        public Namespace ContainingNamespace => new Namespace(container, blockShaderInternal.m_ContainingNamespaceHandle);
        public IEnumerable<BlockShaderInterface> BlockShaderInterfaces
        {
            get
            {
                var listHandle = blockShaderInternal.m_BlockShaderInterfaceListHandle;
                return ListType.Enumerate<BlockShaderInterface>(container, listHandle);
            }
        }
        public IEnumerable<Block> Blocks
        {
            get
            {
                var listHandle = blockShaderInternal.m_BlockListHandle;
                return ListType.Enumerate<Block>(container, listHandle);
            }
        }
        public IEnumerable<CustomAttributeDefinition> CustomAttributeDefinitions
        {
            get
            {
                var listHandle = blockShaderInternal.m_CustomAttributeDefinitionListHandle;
                return ListType.Enumerate<CustomAttributeDefinition>(container, listHandle);
            }
        }
        public IEnumerable<CustomizationPointImplementation> CustomizationPointImplementations
        {
            get
            {
                var listHandle = blockShaderInternal.m_CustomizationPointImplementationListHandle;
                return ListType.Enumerate<CustomizationPointImplementation>(container, listHandle);
            }
        }
        public IEnumerable<ShaderCustomEditor> CustomEditors =>
            ListType.Enumerate<ShaderCustomEditor>(container, blockShaderInternal.m_ShaderCustomEditorListHandle);

        public Location Location => new Location(container, blockShaderInternal.m_LocationHandle);

        // private
        internal BlockShader(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out blockShaderInternal);
        }

        public static BlockShader Invalid => new BlockShader(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is BlockShader other && this.Equals(other);
        public bool Equals(BlockShader other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockShader lhs, BlockShader rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockShader lhs, BlockShader rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public string Name;
            public List<ShaderAttribute> Attributes;
            public Namespace containingNamespace;
            public List<BlockShaderInterface> BlockShaderInterfaces;
            public List<Block> Blocks;
            public List<CustomAttributeDefinition> CustomAttributeDefinitions;
            public List<CustomizationPointImplementation> CustomizationPointImplementations;
            public List<ShaderCustomEditor> CustomEditors;
            public Location location;
            public ShaderContainer Container => container;

            // TODO: Should this require an interface? A block shader without this isn't really usable...
            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.Name = name;
                this.containingNamespace = Utilities.BuildSymbolNamespace(container, name, DataType.BlockShader);
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                Utilities.AddToList(ref Attributes, attribute);
            }

            public void AddBlockShaderInterface(BlockShaderInterface blockShaderInterface)
            {
                Utilities.AddToList(ref BlockShaderInterfaces, blockShaderInterface);
            }

            public void AddBlock(Block block)
            {
                Utilities.AddToList(ref Blocks, block);
            }

            public void AddCustomAttributeDefinition(CustomAttributeDefinition customAttributeDefinition)
            {
                Utilities.AddToList(ref CustomAttributeDefinitions, customAttributeDefinition);
            }

            public void AddCustomizationPointImplementation(CustomizationPointImplementation customizationPointImplementation)
            {
                Utilities.AddToList(ref CustomizationPointImplementations, customizationPointImplementation);
            }

            public void AddCustomEditor(ShaderCustomEditor customEditor)
            {
                Utilities.AddToList(ref CustomEditors, customEditor);
            }

            public BlockShader Build()
            {
                var blockShaderInternal = new BlockShaderInternal()
                {
                    m_NameHandle = container.AddString(Name),
                };

                blockShaderInternal.m_AttributeListHandle = ListType.Build(container, Attributes);
                blockShaderInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                blockShaderInternal.m_BlockShaderInterfaceListHandle = ListType.Build(container, BlockShaderInterfaces);
                blockShaderInternal.m_BlockListHandle = ListType.Build(container, Blocks);
                blockShaderInternal.m_CustomAttributeDefinitionListHandle = ListType.Build(container, CustomAttributeDefinitions);
                blockShaderInternal.m_CustomizationPointImplementationListHandle = ListType.Build(container, CustomizationPointImplementations);
                blockShaderInternal.m_ShaderCustomEditorListHandle = ListType.Build(container, CustomEditors);
                blockShaderInternal.m_LocationHandle = location.handle;

                var returnTypeHandle = container.Add(blockShaderInternal);
                return new BlockShader(container, returnTypeHandle);
            }
        }
    }
}
