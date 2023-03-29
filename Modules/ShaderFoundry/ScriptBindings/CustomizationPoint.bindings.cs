// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/CustomizationPoint.h")]
    internal struct CustomizationPointInternal : IInternalType<CustomizationPointInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_InterfaceFieldListHandle;
        internal FoundryHandle m_DefaultBlockSequenceElementListHandle;

        internal extern static CustomizationPointInternal Invalid();
        internal extern bool IsValid();

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
        public IEnumerable<ShaderAttribute> Attributes => HandleListInternal.Enumerate<ShaderAttribute>(container, customizationPoint.m_AttributeListHandle);
        public Namespace ContainingNamespace => new Namespace(container, customizationPoint.m_ContainingNamespaceHandle);
        public IEnumerable<BlockVariable> InterfaceFields => GetVariableEnumerable(customizationPoint.m_InterfaceFieldListHandle);
        public IEnumerable<BlockVariable> Inputs => InterfaceFields.Where((v) => (v.IsInput));
        public IEnumerable<BlockVariable> Outputs => InterfaceFields.Where((v) => (v.IsOutput));
        // TODO @ SHADERS: Delete this once we don't rely on it in the prototype
        internal IEnumerable<BlockSequenceElement> DefaultBlockSequenceElements
        {
            get
            {
                return customizationPoint.m_DefaultBlockSequenceElementListHandle.AsListEnumerable(container,
                    (container, handle) => new BlockSequenceElement(container, handle));
            }
        }

        IEnumerable<BlockVariable> GetVariableEnumerable(FoundryHandle listHandle)
        {
            var localContainer = Container;
            var list = new HandleListInternal(listHandle);
            return list.Select<BlockVariable>(localContainer, (handle) => (new BlockVariable(localContainer, handle)));
        }

        // private
        internal CustomizationPoint(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out customizationPoint);
        }

        public static CustomizationPoint Invalid => new CustomizationPoint(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
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
            public List<BlockVariable> interfaceFields { get; set; } = new List<BlockVariable>();
            public List<BlockVariable> properties { get; set; } = new List<BlockVariable>();
            public List<BlockSequenceElement> defaultBlockSequenceElements;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
            }

            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
            public void AddInterfaceField(BlockVariable field) { interfaceFields.Add(field); }
            // TODO @ SHADERS: Delete this once we don't rely on it in the prototype
            internal void AddDefaultBlockSequenceElement(BlockSequenceElement blockSequenceElement) => Utilities.AddToList(ref defaultBlockSequenceElements, blockSequenceElement);

            public CustomizationPoint Build()
            {
                var customizationPointInternal = new CustomizationPointInternal
                {
                    m_NameHandle = container.AddString(name),
                };

                customizationPointInternal.m_AttributeListHandle = HandleListInternal.Build(container, attributes);
                customizationPointInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                customizationPointInternal.m_InterfaceFieldListHandle = HandleListInternal.Build(container, interfaceFields, (v) => (v.handle));
                customizationPointInternal.m_DefaultBlockSequenceElementListHandle = HandleListInternal.Build(container, defaultBlockSequenceElements, (v) => (v.handle));

                var returnTypeHandle = container.Add(customizationPointInternal);
                return new CustomizationPoint(container, returnTypeHandle);
            }
        }
    }
}
