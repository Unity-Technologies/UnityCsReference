// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/RenderStateDescriptor.h")]
    internal struct RenderStateDescriptorInternal : IInternalType<RenderStateDescriptorInternal>
    {
        internal FoundryHandle m_ListHandle;

        internal extern static RenderStateDescriptorInternal Invalid();
        internal extern void Setup(ShaderContainer container, string name, string[] ops);
        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);
        internal extern int GetOpCount(ShaderContainer container);
        internal extern string GetOp(ShaderContainer container, int index);

        // IInternalType
        RenderStateDescriptorInternal IInternalType<RenderStateDescriptorInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct RenderStateDescriptor : IEquatable<RenderStateDescriptor>, IPublicType<RenderStateDescriptor>
    {
        // data members
        readonly ShaderContainer container;
        readonly RenderStateDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        RenderStateDescriptor IPublicType<RenderStateDescriptor>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new RenderStateDescriptor(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.IsValid());

        public string Name => descriptor.GetName(container);

        public IEnumerable<string> Ops
        {
            get
            {
                var opCount = descriptor.GetOpCount(container);
                for (var i = 0; i < opCount; ++i)
                    yield return descriptor.GetOp(container, i);
            }
        }

        // private
        internal RenderStateDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out descriptor);
        }

        public static RenderStateDescriptor Invalid => new RenderStateDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is RenderStateDescriptor other && this.Equals(other);
        public bool Equals(RenderStateDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(RenderStateDescriptor lhs, RenderStateDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(RenderStateDescriptor lhs, RenderStateDescriptor rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            string name;
            List<string> ops = new List<string>();

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, IEnumerable<string> ops)
            {
                this.container = container;
                this.name = name;
                this.ops.AddRange(ops);
            }

            public RenderStateDescriptor Build()
            {
                var descriptor = new RenderStateDescriptorInternal();
                descriptor.Setup(container, name, ops.ToArray());
                var resultHandle = container.Add(descriptor);
                return new RenderStateDescriptor(container, resultHandle);
            }
        }
    }
}
