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
    [NativeHeader("Modules/ShaderFoundry/Public/DefineDescriptor.h")]
    internal struct DefineDescriptorInternal
    {
        internal FoundryHandle m_ListHandle;

        internal extern static DefineDescriptorInternal Invalid();

        internal extern void Setup(ShaderContainer container, string name, string value);

        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);
        internal extern string GetValue(ShaderContainer container);
    }

    [FoundryAPI]
    internal readonly struct DefineDescriptor : IEquatable<DefineDescriptor>
    {
        // data members
        readonly ShaderContainer container;
        readonly DefineDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.IsValid());

        public string Name => descriptor.GetName(Container);
        public string Value => descriptor.GetValue(Container);

        // private
        internal DefineDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.descriptor = container?.GetDefineDescriptor(handle) ?? DefineDescriptorInternal.Invalid();
        }

        public static DefineDescriptor Invalid => new DefineDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is DefineDescriptor other && this.Equals(other);
        public bool Equals(DefineDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(DefineDescriptor lhs, DefineDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(DefineDescriptor lhs, DefineDescriptor rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            string name;
            string value;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, string value)
            {
                this.container = container;
                this.name = name;
                this.value = value;
            }

            public DefineDescriptor Build()
            {
                var descriptor = new DefineDescriptorInternal();
                descriptor.Setup(container, name, value);
                var resultHandle = container.AddDefineDescriptorInternal(descriptor);
                return new DefineDescriptor(container, resultHandle);
            }
        }
    }
}
