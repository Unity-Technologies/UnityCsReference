// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderAttribute.h")]
    internal struct ShaderAttributeInternal : IInternalType<ShaderAttributeInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ParameterListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_TypedHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static ShaderAttributeInternal Invalid();

        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();
        [NativeMethod(IsThreadSafe = true)] internal extern string GetName(ShaderContainer container);

        // IInternalType
        ShaderAttributeInternal IInternalType<ShaderAttributeInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderAttribute : IEquatable<ShaderAttribute>, IPublicType<ShaderAttribute>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ShaderAttributeInternal attr;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderAttribute IPublicType<ShaderAttribute>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderAttribute(container, handle);

        // public API
        public ShaderContainer Container => container;
        public static ShaderAttribute Invalid => new ShaderAttribute(null, FoundryHandle.Invalid());
        public bool IsValid => (container != null) && handle.IsValid && (attr.IsValid());
        public string Name => attr.GetName(container);
        public Namespace ContainingNamespace => new Namespace(container, attr.m_ContainingNamespaceHandle);
        public Location Location => new Location(container, attr.m_LocationHandle);

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is ShaderAttribute other && this.Equals(other);
        public bool Equals(ShaderAttribute other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderAttribute lhs, ShaderAttribute rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderAttribute lhs, ShaderAttribute rhs) => !lhs.Equals(rhs);

        public IEnumerable<ShaderAttributeParameter> Parameters =>
            ListType.Enumerate<ShaderAttributeParameter>(container, attr.m_ParameterListHandle);

        internal ShaderAttribute(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = (container != null ? handle : FoundryHandle.Invalid());
            ShaderContainer.Get(container, handle, out attr);
        }

        public class Builder
        {
            ShaderContainer container;
            internal string name;
            public Namespace containingNamespace = Namespace.Invalid;
            internal List<ShaderAttributeParameter> parameters;
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.parameters = null;
            }

            public ShaderAttribute Build()
            {
                var paramListHandle = ListType.Build(container, parameters);
                var attributeInternal = new ShaderAttributeInternal();
                attributeInternal.m_NameHandle = container.AddString(name);
                attributeInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                attributeInternal.m_ParameterListHandle = paramListHandle;
                attributeInternal.m_LocationHandle = location.handle;

                var returnHandle = container.Add(attributeInternal);
                container.ConstructTypedAttributeManaged(returnHandle);
                return new ShaderAttribute(container, returnHandle);
            }
        }
    }
}
