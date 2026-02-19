// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/BlockSequence.h")]
    internal struct BlockSequenceInternal : IInternalType<BlockSequenceInternal>
    {
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_InterfaceFieldListHandle;
        internal FoundryHandle m_ElementListHandle;
        internal FoundryHandle m_OutputLinkOverridesListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static BlockSequenceInternal Invalid();
        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }

        //IInternalType
        BlockSequenceInternal IInternalType<BlockSequenceInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct BlockSequence : IEquatable<BlockSequence>, IPublicType<BlockSequence>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockSequenceInternal blockSequence;

        // Required for IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        BlockSequence IPublicType<BlockSequence>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) =>
            new BlockSequence(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public string Name => container?.GetString(blockSequence.m_NameHandle) ?? string.Empty;
        public Namespace ContainingNamespace => new Namespace(container, blockSequence.m_ContainingNamespaceHandle);
        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, blockSequence.m_AttributeListHandle);
        public IEnumerable<StructField> InterfaceFields =>
            ListType.Enumerate<StructField>(container, blockSequence.m_InterfaceFieldListHandle);
        public IEnumerable<BlockSequenceElement> Elements =>
            ListType.Enumerate<BlockSequenceElement>(container, blockSequence.m_ElementListHandle);
        public IEnumerable<BlockLinkOverride> OutputLinkOverrides =>
            ListType.Enumerate<BlockLinkOverride>(container, blockSequence.m_OutputLinkOverridesListHandle);
        public Location Location => new Location(container, blockSequence.m_LocationHandle);

        // private
        internal BlockSequence(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out blockSequence);
        }

        public static BlockSequence Invalid => new BlockSequence(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is BlockSequence other && this.Equals(other);
        public bool Equals(BlockSequence other) => EqualityChecks.ReferenceEquals(this.handle, this.container,
            other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockSequence lhs, BlockSequence rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockSequence lhs, BlockSequence rhs) => !lhs.Equals(rhs);
    }
}
