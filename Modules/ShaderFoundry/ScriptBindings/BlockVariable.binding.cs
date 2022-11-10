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
    [NativeHeader("Modules/ShaderFoundry/Public/BlockVariable.h")]
    internal struct BlockVariableInternal : IInternalType<BlockVariableInternal>
    {
        internal FoundryHandle m_TypeHandle;
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;

        internal static extern BlockVariableInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        BlockVariableInternal IInternalType<BlockVariableInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct BlockVariable : IEquatable<BlockVariable>, IPublicType<BlockVariable>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockVariableInternal variable;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        BlockVariable IPublicType<BlockVariable>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new BlockVariable(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null);
        public ShaderType Type => new ShaderType(container, variable.m_TypeHandle);
        public string Name => container?.GetString(variable.m_NameHandle) ?? string.Empty;
        public IEnumerable<ShaderAttribute> Attributes
        {
            get
            {
                var count = container?.GetHandleBlobSize(variable.m_AttributeListHandle) ?? 0;
                for (uint i = 0; i < count; ++i)
                {
                    var attributeHandle = container.GetHandleBlobElement(variable.m_AttributeListHandle, i);
                    yield return new ShaderAttribute(Container, attributeHandle);
                }
            }
        }

        // private
        internal BlockVariable(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out variable);
        }

        public static BlockVariable Invalid => new BlockVariable(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is BlockVariable other && this.Equals(other);
        public bool Equals(BlockVariable other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockVariable lhs, BlockVariable rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockVariable lhs, BlockVariable rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public ShaderType Type { get; set; } = ShaderType.Invalid;
            public string Name { get; set; }
            public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                Attributes.Add(attribute);
            }

            public BlockVariable Build()
            {
                var blockVariableInternal = new BlockVariableInternal()
                {
                    m_TypeHandle = Type.handle,
                    m_NameHandle = container.AddString(Name),
                };
                blockVariableInternal.m_AttributeListHandle = container.AddHandleBlob((uint)Attributes.Count);
                for (int i = 0; i < Attributes.Count; ++i)
                    container.SetHandleBlobElement(blockVariableInternal.m_AttributeListHandle, (uint)i, Attributes[i].handle);

                var returnTypeHandle = container.Add(blockVariableInternal);
                return new BlockVariable(container, returnTypeHandle);
            }
        }
    }
}
