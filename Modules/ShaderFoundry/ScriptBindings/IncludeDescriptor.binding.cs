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
    [NativeHeader("Modules/ShaderFoundry/Public/IncludeDescriptor.h")]
    internal struct IncludeDescriptorInternal : IInternalType<IncludeDescriptorInternal>
    {
        internal FoundryHandle m_StringHandle;

        internal extern void Setup(ShaderContainer container, String value);
        internal extern bool IsValid();
        internal extern string GetValue(ShaderContainer container);
        internal extern static IncludeDescriptorInternal Invalid();

        // IInternalType
        IncludeDescriptorInternal IInternalType<IncludeDescriptorInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct IncludeDescriptor : IEquatable<IncludeDescriptor>, IPublicType<IncludeDescriptor>
    {
        // data members
        readonly ShaderContainer container;
        readonly IncludeDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        IncludeDescriptor IPublicType<IncludeDescriptor>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new IncludeDescriptor(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.IsValid());

        public string Value => descriptor.GetValue(Container);

        // private
        internal IncludeDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out descriptor);
        }

        public static IncludeDescriptor Invalid => new IncludeDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is IncludeDescriptor other && this.Equals(other);
        public bool Equals(IncludeDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(IncludeDescriptor lhs, IncludeDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(IncludeDescriptor lhs, IncludeDescriptor rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            string value;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string value)
            {
                this.container = container;
                this.value = value;
            }

            public IncludeDescriptor Build()
            {
                var descriptor = new IncludeDescriptorInternal();
                descriptor.Setup(container, value);
                var resultHandle = container.Add(descriptor);
                return new IncludeDescriptor(container, resultHandle);
            }
        }
    }
}
