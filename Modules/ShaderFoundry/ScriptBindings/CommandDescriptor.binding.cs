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
    [NativeHeader("Modules/ShaderFoundry/Public/CommandDescriptor.h")]
    internal struct CommandDescriptorInternal
    {
        internal FoundryHandle m_ListHandle;

        internal extern static CommandDescriptorInternal Invalid();
        internal extern void Setup(ShaderContainer container, string name, string[] ops);
        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);
        internal extern int GetOpCount(ShaderContainer container);
        internal extern string GetOp(ShaderContainer container, int index);
    }

    [FoundryAPI]
    internal readonly struct CommandDescriptor : IEquatable<CommandDescriptor>
    {
        // data members
        readonly ShaderContainer container;
        readonly CommandDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

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
        internal CommandDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.descriptor = container?.GetCommandDescriptor(handle) ?? CommandDescriptorInternal.Invalid();
        }

        public static CommandDescriptor Invalid => new CommandDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is CommandDescriptor other && this.Equals(other);
        public bool Equals(CommandDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(CommandDescriptor lhs, CommandDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(CommandDescriptor lhs, CommandDescriptor rhs) => !lhs.Equals(rhs);

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
                this.ops = ops.ToList();
            }

            public CommandDescriptor Build()
            {
                var descriptor = new CommandDescriptorInternal();
                descriptor.Setup(container, name, ops.ToArray());
                var resultHandle = container.AddCommandDescriptorInternal(descriptor);
                return new CommandDescriptor(container, resultHandle);
            }
        }
    }
}
