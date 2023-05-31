// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/Namespace.h")]
    internal struct NamespaceInternal : IInternalType<NamespaceInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;

        internal extern static NamespaceInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        NamespaceInternal IInternalType<NamespaceInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct Namespace : IEquatable<Namespace>, IPublicType<Namespace>
    {
        // data members
        readonly ShaderContainer container;
        readonly NamespaceInternal data;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        Namespace IPublicType<Namespace>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new Namespace(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && data.IsValid());

        public string Name => Container?.GetString(data.m_NameHandle) ?? string.Empty;
        public Namespace ContainingNamespace => new Namespace(Container, data.m_ContainingNamespaceHandle);

        // private
        internal Namespace(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out data);
        }

        public static Namespace Invalid => new Namespace(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is Namespace other && this.Equals(other);
        public bool Equals(Namespace other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator ==(Namespace lhs, Namespace rhs) => lhs.Equals(rhs);
        public static bool operator !=(Namespace lhs, Namespace rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public string name;
            public Namespace containingNamespace = Namespace.Invalid;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
                : this(container, name, Namespace.Invalid)
            {
            }

            public Builder(ShaderContainer container, string name, Namespace containingNamespace)
            {
                this.container = container;
                this.name = name;
                this.containingNamespace = containingNamespace;
            }

            public Namespace Build()
            {
                var data = new NamespaceInternal
                {
                    m_NameHandle = container.AddString(name),
                    m_ContainingNamespaceHandle = containingNamespace.handle,
                };

                var resultHandle = container.Add(data);
                return new Namespace(container, resultHandle);
            }
        }
    }
}
