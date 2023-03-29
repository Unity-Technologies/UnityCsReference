// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/BlockSequenceElement.h")]
    internal struct BlockSequenceElementInternal : IInternalType<BlockSequenceElementInternal>
    {
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_BlockHandle;
        internal FoundryHandle m_CustomizationPointHandle;
        internal FoundryHandle m_InstanceNameHandle;
        internal FoundryHandle m_InputLinkOverridesListHandle;
        internal FoundryHandle m_OutputLinkOverridesListHandle;

        internal extern static BlockSequenceElementInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        BlockSequenceElementInternal IInternalType<BlockSequenceElementInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct BlockSequenceElement : IEquatable<BlockSequenceElement>, IPublicType<BlockSequenceElement>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockSequenceElementInternal element;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        BlockSequenceElement IPublicType<BlockSequenceElement>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new BlockSequenceElement(container, handle);

        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public IEnumerable<ShaderAttribute> Attributes => HandleListInternal.Enumerate<ShaderAttribute>(container, element.m_AttributeListHandle);
        public Block Block => new Block(container, element.m_BlockHandle);
        public CustomizationPoint CustomizationPoint => new CustomizationPoint(container, element.m_CustomizationPointHandle);
        public string InstanceName => container?.GetString(element.m_InstanceNameHandle) ?? string.Empty;

        public IEnumerable<BlockLinkOverride> InputLinkOverrides => element.m_InputLinkOverridesListHandle.AsListEnumerable<BlockLinkOverride>(container, (container, handle) => (new BlockLinkOverride(container, handle)));
        public IEnumerable<BlockLinkOverride> OutputLinkOverrides => element.m_OutputLinkOverridesListHandle.AsListEnumerable<BlockLinkOverride>(container, (container, handle) => (new BlockLinkOverride(container, handle)));

        // private
        internal BlockSequenceElement(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out element);
        }

        public static BlockSequenceElement Invalid => new BlockSequenceElement(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is BlockSequenceElement other && this.Equals(other);
        public bool Equals(BlockSequenceElement other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockSequenceElement lhs, BlockSequenceElement rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockSequenceElement lhs, BlockSequenceElement rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public List<ShaderAttribute> attributes;
            public Block block { get; private set; }
            public CustomizationPoint customizationPoint { get; private set; }
            public string instanceName;
            public List<BlockLinkOverride> inputOverrides;
            public List<BlockLinkOverride> outputOverrides;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, Block block)
            {
                this.container = container;
                this.block = block;
                this.customizationPoint = CustomizationPoint.Invalid;
            }

            public Builder(ShaderContainer container, CustomizationPoint customizationPoint)
            {
                this.container = container;
                this.customizationPoint = customizationPoint;
                this.block = Block.Invalid;
            }

            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
            public void AddInputOverride(BlockLinkOverride linkOverride)
            {
                if (inputOverrides == null)
                    inputOverrides = new List<BlockLinkOverride>();
                inputOverrides.Add(linkOverride);
            }

            public void AddOutputOverride(BlockLinkOverride linkOverride)
            {
                if (outputOverrides == null)
                    outputOverrides = new List<BlockLinkOverride>();
                outputOverrides.Add(linkOverride);
            }

            public BlockSequenceElement Build()
            {
                var internalResult = new BlockSequenceElementInternal();
                internalResult.m_AttributeListHandle = HandleListInternal.Build(container, attributes);
                internalResult.m_BlockHandle = block.handle;
                internalResult.m_CustomizationPointHandle = customizationPoint.handle;
                internalResult.m_InstanceNameHandle = container.AddString(instanceName);
                internalResult.m_InputLinkOverridesListHandle = HandleListInternal.Build(container, inputOverrides, (o) => (o.handle));
                internalResult.m_OutputLinkOverridesListHandle = HandleListInternal.Build(container, outputOverrides, (o) => (o.handle));
                var returnTypeHandle = container.Add(internalResult);
                return new BlockSequenceElement(container, returnTypeHandle);
            }
        }
    }
}
