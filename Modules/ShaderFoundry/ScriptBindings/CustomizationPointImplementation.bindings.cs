// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/CustomizationPointImplementation.h")]
    internal struct CustomizationPointImplementationInternal : IInternalType<CustomizationPointImplementationInternal>
    {
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_CustomizationPointNameHandle;
        internal FoundryHandle m_BlockSequenceElementListHandle;
        internal FoundryHandle m_OutputLinkOverridesListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static CustomizationPointImplementationInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        CustomizationPointImplementationInternal IInternalType<CustomizationPointImplementationInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct CustomizationPointImplementation : IEquatable<CustomizationPointImplementation>, IPublicType<CustomizationPointImplementation>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly CustomizationPointImplementationInternal customizationPointImplementation;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        CustomizationPointImplementation IPublicType<CustomizationPointImplementation>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new CustomizationPointImplementation(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, customizationPointImplementation.m_AttributeListHandle);
        public string CustomizationPointName => container?.GetString(customizationPointImplementation.m_CustomizationPointNameHandle) ?? string.Empty;

        public IEnumerable<BlockSequenceElement> BlockSequenceElements
        {
            get
            {
                var listHandle = customizationPointImplementation.m_BlockSequenceElementListHandle;
                return ListType.Enumerate<BlockSequenceElement>(container, listHandle);
            }
        }
        
        public IEnumerable<BlockLinkOverride> OutputLinkOverrides =>
        ListType.Enumerate<BlockLinkOverride>(container, customizationPointImplementation.m_OutputLinkOverridesListHandle);

        public Location Location => new Location(container, customizationPointImplementation.m_LocationHandle);

        // private
        internal CustomizationPointImplementation(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out customizationPointImplementation);
        }

        public static CustomizationPointImplementation Invalid => new CustomizationPointImplementation(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is CustomizationPointImplementation other && this.Equals(other);
        public bool Equals(CustomizationPointImplementation other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(CustomizationPointImplementation lhs, CustomizationPointImplementation rhs) => lhs.Equals(rhs);
        public static bool operator!=(CustomizationPointImplementation lhs, CustomizationPointImplementation rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public List<ShaderAttribute> attributes;
            string customizationPointName;
            List<BlockSequenceElement> blockSequenceElements;
            List<BlockLinkOverride> outputLinkOverrides;
            public Location location;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string customizationPointName)
            {
                this.container = container;
                this.customizationPointName = customizationPointName;
            }

            // TODO @ SHADERS SHADERS-353: Remove this constructor
            [Obsolete("Use the constructor taking only the customization point's name instead")]
            public Builder(ShaderContainer container, CustomizationPoint customizationPoint)
                : this(container, customizationPoint.Name)
            {
            }

            public void AddAttribute(ShaderAttribute attribute) =>
                Utilities.AddToList(ref attributes, attribute);
            public void AddBlockSequenceElement(BlockSequenceElement element) =>
                Utilities.AddToList(ref blockSequenceElements, element);
            public void AddOutputOverride(BlockLinkOverride link) =>
                Utilities.AddToList(ref outputLinkOverrides, link);

            public CustomizationPointImplementation Build()
            {
                var customizationPointImplementationInternal = new CustomizationPointImplementationInternal()
                {
                    m_CustomizationPointNameHandle = container.AddString(customizationPointName),
                };

                customizationPointImplementationInternal.m_AttributeListHandle = ListType.Build(container, attributes);
                customizationPointImplementationInternal.m_BlockSequenceElementListHandle = ListType.Build(container, blockSequenceElements);
                customizationPointImplementationInternal.m_OutputLinkOverridesListHandle = ListType.Build(container, outputLinkOverrides);
                customizationPointImplementationInternal.m_LocationHandle = location.handle;

                var returnTypeHandle = container.Add(customizationPointImplementationInternal);
                return new CustomizationPointImplementation(container, returnTypeHandle);
            }
        }
    }
}
