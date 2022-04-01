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
    [NativeHeader("Modules/ShaderFoundry/Public/TagDescriptor.h")]
    internal struct TagDescriptorInternal
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ValueHandle;

        internal extern static TagDescriptorInternal Invalid();

        internal extern bool IsValid();

        internal extern FoundryHandle GetNameHandle();
        internal extern FoundryHandle GetValueHandle();
    }

    [FoundryAPI]
    internal readonly struct TagDescriptor : IEquatable<TagDescriptor>
    {
        // data members
        readonly ShaderContainer container;
        readonly TagDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.IsValid());

        public string Name => Container?.GetString(descriptor.GetNameHandle()) ?? string.Empty;
        public string Value => Container?.GetString(descriptor.GetValueHandle()) ?? string.Empty;

        // private
        internal TagDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.descriptor = container?.GetTagDescriptor(handle) ?? TagDescriptorInternal.Invalid();
        }

        public static TagDescriptor Invalid => new TagDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is TagDescriptor other && this.Equals(other);
        public bool Equals(TagDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(TagDescriptor lhs, TagDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(TagDescriptor lhs, TagDescriptor rhs) => !lhs.Equals(rhs);

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

            public TagDescriptor Build()
            {
                var descriptor = new TagDescriptorInternal();
                descriptor.m_NameHandle = container.AddString(name);
                descriptor.m_ValueHandle = container.AddString(value);
                var resultHandle = container.AddTagDescriptorInternal(descriptor);
                return new TagDescriptor(container, resultHandle);
            }
        }
    }
}
