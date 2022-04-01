// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/KeywordDescriptor.h")]
    internal struct KeywordDescriptorInternal
    {
        internal FoundryHandle m_ListHandle;

        internal extern static KeywordDescriptorInternal Invalid();
        internal extern void Setup(ShaderContainer container, string definition, string scope, string stage, string name, string[] ops);

        internal extern string GetDefinition(ShaderContainer container);
        internal extern string GetScope(ShaderContainer container);
        internal extern string GetStage(ShaderContainer container);
        internal extern string GetName(ShaderContainer container);
        internal extern int GetOpCount(ShaderContainer container);
        internal extern string GetOp(ShaderContainer container, int index);
    }

    [FoundryAPI]
    internal readonly struct KeywordDescriptor : IEquatable<KeywordDescriptor>
    {
        // data members
        readonly ShaderContainer container;
        readonly KeywordDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.m_ListHandle.IsValid);

        public string Definition => descriptor.GetDefinition(container);
        public string Scope => descriptor.GetScope(container);
        public string Stage => descriptor.GetStage(container);
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
        internal KeywordDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.descriptor = container?.GetKeywordDescriptor(handle) ?? KeywordDescriptorInternal.Invalid();
        }

        public static KeywordDescriptor Invalid => new KeywordDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is KeywordDescriptor other && this.Equals(other);
        public bool Equals(KeywordDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(KeywordDescriptor lhs, KeywordDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(KeywordDescriptor lhs, KeywordDescriptor rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            string definition;
            string scope;
            string stage;
            string name;
            List<string> ops = new List<string>();

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, string definition, string scope, string stage, IEnumerable<string> ops)
            {
                this.container = container;
                this.name = name;
                this.definition = definition;
                this.scope = scope;
                this.stage = stage;
                this.ops = ops.ToList();
            }

            public KeywordDescriptor Build()
            {
                var descriptor = new KeywordDescriptorInternal();
                descriptor.Setup(container, definition, scope, stage, name, ops.ToArray());
                var resultHandle = container.AddKeywordDescriptorInternal(descriptor);
                return new KeywordDescriptor(container, resultHandle);
            }
        }
    }
}
