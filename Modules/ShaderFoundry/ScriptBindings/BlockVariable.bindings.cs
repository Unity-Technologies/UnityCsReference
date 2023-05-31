// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/BlockVariable.h")]
    internal struct BlockVariableInternal : IInternalType<BlockVariableInternal>
    {
        // these enums must match the declarations in BlockVariable.h
        [Flags]
        internal enum Flags : UInt32
        {
            kNone = 0,
            kInput = 1 << 0,
            kOutput = 1 << 1,
        };
        internal FoundryHandle m_TypeHandle;
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal Flags m_Flags;

        internal static extern BlockVariableInternal Invalid();
        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);

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
        public string Name => variable.GetName(container);
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
        public bool IsInput => variable.m_Flags.HasFlag(BlockVariableInternal.Flags.kInput);
        public bool IsOutput => variable.m_Flags.HasFlag(BlockVariableInternal.Flags.kOutput);
        internal bool HasFlag(BlockVariableInternal.Flags flags) => variable.m_Flags.HasFlag(flags);

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
            BlockVariableInternal.Flags m_Flags = BlockVariableInternal.Flags.kInput;

            public bool IsInput
            {
                get { return m_Flags.HasFlag(BlockVariableInternal.Flags.kInput); }
                set { SetFlag(BlockVariableInternal.Flags.kInput, value); }
            }
            public bool IsOutput
            {
                get { return m_Flags.HasFlag(BlockVariableInternal.Flags.kOutput); }
                set { SetFlag(BlockVariableInternal.Flags.kOutput, value); }
            }
            void SetFlag(BlockVariableInternal.Flags flag, bool state)
            {
                if (state)
                    m_Flags |= flag;
                else
                    m_Flags &= ~flag;
            }

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
                if (IsInput == false && IsOutput == false)
                    throw new InvalidOperationException("BlockVariable is neither an input or an output. At least one must be true.");

                var blockVariableInternal = new BlockVariableInternal()
                {
                    m_TypeHandle = Type.handle,
                    m_NameHandle = container.AddString(Name),
                };
                blockVariableInternal.m_AttributeListHandle = container.AddHandleBlob((uint)Attributes.Count);
                for (int i = 0; i < Attributes.Count; ++i)
                    container.SetHandleBlobElement(blockVariableInternal.m_AttributeListHandle, (uint)i, Attributes[i].handle);
                blockVariableInternal.m_Flags = m_Flags;

                var returnTypeHandle = container.Add(blockVariableInternal);
                return new BlockVariable(container, returnTypeHandle);
            }
        }
    }
}
