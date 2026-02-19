// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/PragmaDescriptor.h")]
    internal struct PragmaDescriptorInternal : IInternalType<PragmaDescriptorInternal>
    {
        internal FoundryHandle m_LocationHandle;
        internal FoundryHandle m_ListHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static PragmaDescriptorInternal Invalid();
        [NativeMethod(IsThreadSafe = true)][NativeName("SetupFromScripting")] internal extern void Setup(ShaderContainer container, string name, string[] ops);

        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();
        [NativeMethod(IsThreadSafe = true)] internal extern string GetName(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern ulong GetOpCount(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern string GetOp(ShaderContainer container, ulong index);

        // IInternalType
        PragmaDescriptorInternal IInternalType<PragmaDescriptorInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct PragmaDescriptor : IEquatable<PragmaDescriptor>, IPublicType<PragmaDescriptor>
    {
        // data members
        readonly ShaderContainer container;
        readonly PragmaDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        PragmaDescriptor IPublicType<PragmaDescriptor>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new PragmaDescriptor(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.IsValid());

        public string Name => descriptor.GetName(Container);

        public IEnumerable<string> Ops
        {
            get
            {
                ulong opCount = descriptor.GetOpCount(Container);
                for (ulong i = 0; i < opCount; ++i)
                    yield return descriptor.GetOp(Container, i);
            }
        }

        public Location Location => new Location(container, descriptor.m_LocationHandle);

        // private
        internal PragmaDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out descriptor);
        }

        public static PragmaDescriptor Invalid => new PragmaDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is PragmaDescriptor other && this.Equals(other);
        public bool Equals(PragmaDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(PragmaDescriptor lhs, PragmaDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(PragmaDescriptor lhs, PragmaDescriptor rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            string name;
            List<string> ops = new List<string>();
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, IEnumerable<string> ops)
            {
                this.container = container;
                this.name = name;
                this.ops.AddRange(ops);
            }

            public PragmaDescriptor Build()
            {
                var descriptor = new PragmaDescriptorInternal();
                descriptor.Setup(container, name, ops.ToArray());
                descriptor.m_LocationHandle = location.handle;
                var resultHandle = container.Add(descriptor);
                return new PragmaDescriptor(container, resultHandle);
            }
        }
    }
}
