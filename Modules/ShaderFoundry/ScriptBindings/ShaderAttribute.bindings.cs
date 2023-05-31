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

        internal extern static ShaderAttributeInternal Invalid();

        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);

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

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderAttribute other && this.Equals(other);
        public bool Equals(ShaderAttribute other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderAttribute lhs, ShaderAttribute rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderAttribute lhs, ShaderAttribute rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in ShaderAttribute other)
        {
            return ShaderAttributeInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        public IEnumerable<ShaderAttributeParameter> Parameters
        {
            get
            {
                var localContainer = Container;
                var list = new HandleListInternal(attr.m_ParameterListHandle);
                return list.Select<ShaderAttributeParameter>(localContainer, (handle) => (new ShaderAttributeParameter(localContainer, handle)));
            }
        }

        internal ShaderAttribute(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = (container ? handle : FoundryHandle.Invalid());
            ShaderContainer.Get(container, handle, out attr);
        }

        public class Builder
        {
            ShaderContainer container;
            internal string name;
            public Namespace containingNamespace = Namespace.Invalid;
            internal List<ShaderAttributeParameter> parameters;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.parameters = null;
            }

            public Builder Parameter(ShaderAttributeParameter attribute)
            {
                if (parameters == null)
                    parameters = new List<ShaderAttributeParameter>();
                parameters.Add(attribute);
                return this;
            }

            public Builder Parameter(string value)
            {
                return Parameter(null, value);
            }

            public Builder Parameter(string name, string value)
            {
                var paramBuilder = new ShaderAttributeParameter.Builder(container, name, value);
                return Parameter(paramBuilder.Build());
            }

            public ShaderAttribute Build()
            {
                var paramListHandle = HandleListInternal.Build(container, parameters, (p) => (p.handle));
                var attributeInternal = new ShaderAttributeInternal();
                attributeInternal.m_NameHandle = container.AddString(name);
                attributeInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                attributeInternal.m_ParameterListHandle = paramListHandle;

                var returnHandle = container.Add(attributeInternal);
                return new ShaderAttribute(container, returnHandle);
            }
        }
    }
}
