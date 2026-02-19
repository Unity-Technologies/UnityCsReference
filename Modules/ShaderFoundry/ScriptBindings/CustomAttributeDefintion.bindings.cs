// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/CustomAttributeDefinition.h")]
    internal struct CustomAttributeDefinitionInternal : IInternalType<CustomAttributeDefinitionInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_ConstructorSignatures;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static CustomAttributeDefinitionInternal Invalid();
        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }

        // IInternalType
        CustomAttributeDefinitionInternal IInternalType<CustomAttributeDefinitionInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct CustomAttributeDefinition : IEquatable<CustomAttributeDefinition>, IPublicType<CustomAttributeDefinition>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly CustomAttributeDefinitionInternal customAttributeDefinition;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        CustomAttributeDefinition IPublicType<CustomAttributeDefinition>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new CustomAttributeDefinition(container, handle);

        // public API
        public ShaderContainer Container => container;

        // exists if it has been allocated
        public bool Exists => (container != null && handle.IsValid);

        // must have a name assigned to be considered valid
        public bool IsValid => Exists && customAttributeDefinition.IsValid;

        public string Name => container?.GetString(customAttributeDefinition.m_NameHandle) ?? string.Empty;
        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, customAttributeDefinition.m_AttributeListHandle);

        public Namespace ContainingNamespace => new Namespace(container, customAttributeDefinition.m_ContainingNamespaceHandle);
        public IEnumerable<ConstructorSignature> ConstructorSignatures =>
        ListType.Enumerate<ConstructorSignature>(container, customAttributeDefinition.m_ConstructorSignatures);
        public Location Location => new Location(container, customAttributeDefinition.m_LocationHandle);

        // private
        internal CustomAttributeDefinition(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out customAttributeDefinition);
        }

        public static CustomAttributeDefinition Invalid => new CustomAttributeDefinition(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is CustomAttributeDefinition other && this.Equals(other);
        public bool Equals(CustomAttributeDefinition other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(CustomAttributeDefinition lhs, CustomAttributeDefinition rhs) => lhs.Equals(rhs);
        public static bool operator!=(CustomAttributeDefinition lhs, CustomAttributeDefinition rhs) => !lhs.Equals(rhs);
    }
}
