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
    [NativeHeader("Modules/ShaderFoundry/Public/BlockInstance.h")]
    internal struct BlockInstanceInternal
    {
        internal FoundryHandle m_BlockHandle;

        internal extern static BlockInstanceInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct BlockInstance : IEquatable<BlockInstance>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockInstanceInternal blockInstance;

        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public Block Block { get { return new Block(container, blockInstance.m_BlockHandle); } }

        // private
        internal BlockInstance(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.blockInstance = container?.GetBlockInstance(handle) ?? BlockInstanceInternal.Invalid();
        }

        public static BlockInstance Invalid => new BlockInstance(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is BlockInstance other && this.Equals(other);
        public bool Equals(BlockInstance other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockInstance lhs, BlockInstance rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockInstance lhs, BlockInstance rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            Block block;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, Block block)
            {
                this.container = container;
                this.block = block;
            }

            public BlockInstance Build()
            {
                var blockInstanceInternal = new BlockInstanceInternal();
                blockInstanceInternal.m_BlockHandle = block.handle;
                var returnTypeHandle = container.AddBlockInstanceInternal(blockInstanceInternal);
                return new BlockInstance(container, returnTypeHandle);
            }
        }
    }
}
