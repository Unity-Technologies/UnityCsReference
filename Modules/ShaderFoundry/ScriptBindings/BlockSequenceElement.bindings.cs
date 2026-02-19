// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/BlockSequenceElement.h")]
    internal struct BlockSequenceElementInternal : IInternalType<BlockSequenceElementInternal>
    {
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_TypeHandle;
        internal FoundryHandle m_InstanceNameHandle;
        internal FoundryHandle m_LinkOverridesListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static BlockSequenceElementInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

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
        public IEnumerable<ShaderAttribute> Attributes => ListType.Enumerate<ShaderAttribute>(container, element.m_AttributeListHandle);
        public IPublicType Type => container?.ConstructTypeFromHandle(element.m_TypeHandle);
        [Obsolete("Use '(Type as Block)' instead")]
        public Block Block => (Type is Block block) ? block : Block.Invalid;
        [Obsolete("Use '(Type as CustomizationPoint)' instead")]
        public CustomizationPoint CustomizationPoint => (Type is CustomizationPoint cp) ? cp : CustomizationPoint.Invalid;
        public string InstanceName => container?.GetString(element.m_InstanceNameHandle) ?? string.Empty;

        public IEnumerable<BlockLinkOverride> LinkOverrides =>
            ListType.Enumerate<BlockLinkOverride>(container, element.m_LinkOverridesListHandle);

        public Location Location => new Location(container, element.m_LocationHandle);

        // private
        internal BlockSequenceElement(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out element);
        }

        public static BlockSequenceElement Invalid => new BlockSequenceElement(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is BlockSequenceElement other && this.Equals(other);
        public bool Equals(BlockSequenceElement other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockSequenceElement lhs, BlockSequenceElement rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockSequenceElement lhs, BlockSequenceElement rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public List<ShaderAttribute> attributes;
            public IPublicType Type { get; private set; }
            public string instanceName;
            public List<BlockLinkOverride> linkOverrides;
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, Block block)
            {
                this.container = container;
                this.Type = block;
            }

            public Builder(ShaderContainer container, BlockSequence blockSequence)
            {
                this.container = container;
                this.Type = blockSequence;
            }

            public Builder(ShaderContainer container, CustomizationPoint customizationPoint)
            {
                this.container = container;
                this.Type = customizationPoint;
            }

            public void AddAttribute(ShaderAttribute attribute) =>
                Utilities.AddToList(ref attributes, attribute);
            public void AddLinkOverride(BlockLinkOverride linkOverride) =>
                Utilities.AddToList(ref linkOverrides, linkOverride);

            public BlockSequenceElement Build()
            {
                var internalResult = new BlockSequenceElementInternal();
                internalResult.m_AttributeListHandle = ListType.Build(container, attributes);
                internalResult.m_TypeHandle = Type.Handle;
                internalResult.m_InstanceNameHandle = container.AddString(instanceName);
                internalResult.m_LinkOverridesListHandle = ListType.Build(container, linkOverrides);
                internalResult.m_LocationHandle = location.handle;
                var returnTypeHandle = container.Add(internalResult);
                return new BlockSequenceElement(container, returnTypeHandle);
            }
        }
    }
}
