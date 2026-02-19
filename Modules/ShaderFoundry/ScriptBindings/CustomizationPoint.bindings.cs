// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/CustomizationPoint.h")]
    internal struct CustomizationPointInternal : IInternalType<CustomizationPointInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_InterfaceFieldListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static CustomizationPointInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        CustomizationPointInternal IInternalType<CustomizationPointInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct CustomizationPoint : IEquatable<CustomizationPoint>, IPublicType<CustomizationPoint>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly CustomizationPointInternal customizationPoint;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        CustomizationPoint IPublicType<CustomizationPoint>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new CustomizationPoint(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public string Name => container?.GetString(customizationPoint.m_NameHandle) ?? string.Empty;
        public IEnumerable<ShaderAttribute> Attributes => ListType.Enumerate<ShaderAttribute>(container, customizationPoint.m_AttributeListHandle);
        public Namespace ContainingNamespace => new Namespace(container, customizationPoint.m_ContainingNamespaceHandle);
        public IEnumerable<StructField> InterfaceFields =>
            ListType.Enumerate<StructField>(container, customizationPoint.m_InterfaceFieldListHandle);
        public IEnumerable<StructField> Inputs => InterfaceFields.Select(StructFieldInternal.Flags.kInput);
        public IEnumerable<StructField> Outputs => InterfaceFields.Select(StructFieldInternal.Flags.kOutput);
        public Location Location => new Location(container, customizationPoint.m_LocationHandle);

        // private
        internal CustomizationPoint(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out customizationPoint);
        }

        public static CustomizationPoint Invalid => new CustomizationPoint(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is CustomizationPoint other && this.Equals(other);
        public bool Equals(CustomizationPoint other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(CustomizationPoint lhs, CustomizationPoint rhs) => lhs.Equals(rhs);
        public static bool operator!=(CustomizationPoint lhs, CustomizationPoint rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            internal string name;
            public List<ShaderAttribute> attributes;
            public Namespace containingNamespace = Namespace.Invalid;
            public List<StructField> interfaceFields;
            public Location location;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
            }

            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
            public void AddInterfaceField(StructField field) => Utilities.AddToList(ref interfaceFields, field);

            public CustomizationPoint Build()
            {
                var customizationPointInternal = new CustomizationPointInternal
                {
                    m_NameHandle = container.AddString(name),
                };

                customizationPointInternal.m_AttributeListHandle = ListType.Build(container, attributes);
                customizationPointInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                customizationPointInternal.m_InterfaceFieldListHandle = ListType.Build(container, interfaceFields);
                customizationPointInternal.m_LocationHandle = location.handle;

                var returnTypeHandle = container.Add(customizationPointInternal);
                return new CustomizationPoint(container, returnTypeHandle);
            }
        }
    }
}
