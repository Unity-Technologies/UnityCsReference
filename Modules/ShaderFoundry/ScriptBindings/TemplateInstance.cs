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
    [NativeHeader("Modules/ShaderFoundry/Public/TemplateInstance.h")]
    internal struct TemplateInstanceInternal : IInternalType<TemplateInstanceInternal>
    {
        internal FoundryHandle m_TemplateHandle;
        internal FoundryHandle m_CustomizationPointImplementationListHandle;
        internal FoundryHandle m_TagDescriptorListHandle;

        internal extern static TemplateInstanceInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        TemplateInstanceInternal IInternalType<TemplateInstanceInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct TemplateInstance : IEquatable<TemplateInstance>, IPublicType<TemplateInstance>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly TemplateInstanceInternal templateInstance;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        TemplateInstance IPublicType<TemplateInstance>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new TemplateInstance(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public Template Template
        {
            get
            {
                var localContainer = container;
                return new Template(localContainer, templateInstance.m_TemplateHandle);
            }
        }

        public IEnumerable<CustomizationPointImplementation> CustomizationPointImplementations => templateInstance.m_CustomizationPointImplementationListHandle.AsListEnumerable<CustomizationPointImplementation>(container, (container, handle) => (new CustomizationPointImplementation(container, handle)));

        public IEnumerable<TagDescriptor> TagDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(templateInstance.m_TagDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new TagDescriptor(localContainer, handle)));
            }
        }

        // private
        internal TemplateInstance(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out templateInstance);
        }

        public static TemplateInstance Invalid => new TemplateInstance(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is TemplateInstance other && this.Equals(other);
        public bool Equals(TemplateInstance other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(TemplateInstance lhs, TemplateInstance rhs) => lhs.Equals(rhs);
        public static bool operator!=(TemplateInstance lhs, TemplateInstance rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            Template template = Template.Invalid;
            public List<CustomizationPointImplementation> customizationPointImplementations;
            List<TagDescriptor> tagDescriptors = new List<TagDescriptor>();
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, Template template)
            {
                this.container = container;
                this.template = template;
            }

            public void AddCustomizationPointImplementation(CustomizationPointImplementation customizationPointImplementation)
            {
                Utilities.AddToList(ref customizationPointImplementations, customizationPointImplementation);
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                tagDescriptors.Add(tagDescriptor);
            }

            public TemplateInstance Build()
            {
                var templateInstanceInternal = new TemplateInstanceInternal();
                templateInstanceInternal.m_TemplateHandle = template.handle;
                templateInstanceInternal.m_CustomizationPointImplementationListHandle = FixedHandleListInternal.Build(container, customizationPointImplementations, (o) => (o.handle));
                templateInstanceInternal.m_TagDescriptorListHandle = FixedHandleListInternal.Build(container, tagDescriptors, (o) => (o.handle));

                var returnTypeHandle = container.Add(templateInstanceInternal);
                return new TemplateInstance(container, returnTypeHandle);
            }
        }
    }
}
