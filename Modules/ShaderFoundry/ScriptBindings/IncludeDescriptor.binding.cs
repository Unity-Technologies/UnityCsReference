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
    internal struct IncludeDescriptorInternal
    {
        internal FoundryHandle m_StringHandle;

        internal extern void Setup(ShaderContainer container, String value);
        internal extern bool IsValid();
        internal extern string GetValue(ShaderContainer container);
        internal extern static IncludeDescriptorInternal Invalid();
    }

    [FoundryAPI]
    internal readonly struct IncludeDescriptor
    {
        // data members
        readonly ShaderContainer container;
        readonly IncludeDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.IsValid());

        public string Value => descriptor.GetValue(Container);

        // private
        internal IncludeDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.descriptor = container?.GetIncludeDescriptor(handle) ?? IncludeDescriptorInternal.Invalid();
        }

        public static IncludeDescriptor Invalid => new IncludeDescriptor(null, FoundryHandle.Invalid());

        public struct Builder
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
                var resultHandle = container.AddIncludeDescriptorInternal(descriptor);
                return new IncludeDescriptor(container, resultHandle);
            }
        }
    }
}
