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
    internal struct TemplateInstanceInternal
    {
        internal FoundryHandle m_TemplateHandle;
        internal FoundryHandle m_CustomizationPointListHandle;
        internal FoundryHandle m_TagDescriptorListHandle;

        internal extern static TemplateInstanceInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct TemplateInstance : IEquatable<TemplateInstance>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly TemplateInstanceInternal templateInstance;

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

        public IEnumerable<CustomizationPointInstance> CustomizationPointInstances
        {
            get
            {
                var localContainer = Container;
                var blockHandles = new FixedHandleListInternal(templateInstance.m_CustomizationPointListHandle);
                return blockHandles.Select(localContainer, (handle) => (new CustomizationPointInstance(localContainer, handle)));
            }
        }

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
            this.templateInstance = container?.GetTemplateInstance(handle) ?? TemplateInstanceInternal.Invalid();
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
            public List<CustomizationPointInstance> customizationPointInstances = new List<CustomizationPointInstance>();
            List<TagDescriptor> tagDescriptors = new List<TagDescriptor>();
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, Template template)
            {
                this.container = container;
                this.template = template;
            }

            public void AddCustomizationPointInstance(CustomizationPointInstance customizationPointInstance)
            {
                customizationPointInstances.Add(customizationPointInstance);
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                tagDescriptors.Add(tagDescriptor);
            }

            public TemplateInstance Build()
            {
                var templateInstanceInternal = new TemplateInstanceInternal();
                templateInstanceInternal.m_TemplateHandle = template.handle;
                templateInstanceInternal.m_CustomizationPointListHandle = FixedHandleListInternal.Build(container, customizationPointInstances, (o) => (o.handle));
                templateInstanceInternal.m_TagDescriptorListHandle = FixedHandleListInternal.Build(container, tagDescriptors, (o) => (o.handle));

                var returnTypeHandle = container.AddTemplateInstanceInternal(templateInstanceInternal);
                return new TemplateInstance(container, returnTypeHandle);
            }
        }
    }
}
