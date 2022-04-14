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
    [NativeHeader("Modules/ShaderFoundry/Public/Template.h")]
    internal struct TemplateInternal
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_PassListHandle;
        internal FoundryHandle m_TagDescriptorListHandle;
        internal FoundryHandle m_AdditionalShaderIDStringHandle;
        internal FoundryHandle m_LinkerHandle;

        internal extern static TemplateInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct Template : IEquatable<Template>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly TemplateInternal template;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public string Name => container?.GetString(template.m_NameHandle) ?? string.Empty;
        public string AdditionalShaderID => container?.GetString(template.m_AdditionalShaderIDStringHandle) ?? string.Empty;
        public IEnumerable<TemplatePass> Passes
        {
            get
            {
                var localContainer = Container;
                var passList = new FixedHandleListInternal(template.m_PassListHandle);
                return passList.Select<TemplatePass>(localContainer, (handle) => (new TemplatePass(localContainer, handle)));
            }
        }

        public IEnumerable<TagDescriptor> TagDescriptors
        {
            get
            {
                var localContainer = Container;
                var tagDescriptorList = new FixedHandleListInternal(template.m_TagDescriptorListHandle);
                return tagDescriptorList.Select<TagDescriptor>(localContainer, (handle) => (new TagDescriptor(localContainer, handle)));
            }
        }

        public ITemplateLinker Linker => Container?.GetTemplateLinker(template.m_LinkerHandle) ?? null;

        // private
        internal Template(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.template = container?.GetTemplate(handle) ?? TemplateInternal.Invalid();
        }

        public static Template Invalid => new Template(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is Template other && this.Equals(other);
        public bool Equals(Template other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(Template lhs, Template rhs) => lhs.Equals(rhs);
        public static bool operator!=(Template lhs, Template rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public string Name { get; set; }
            public string AdditionalShaderID { get; set; }
            List<TemplatePass> passes { get; set; } = new List<TemplatePass>();
            ITemplateLinker linker;
            List<TagDescriptor> tagDescriptors = new List<TagDescriptor>();
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, ITemplateLinker linker)
            {
                if (container == null || linker == null)
                    throw new Exception("A valid ShaderContainer and ITemplateLinker must be provided to create a Template Builder.");

                this.container = container;
                this.Name = name;
                this.linker = linker;
            }

            public void AddPass(TemplatePass pass)
            {
                passes.Add(pass);
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                tagDescriptors.Add(tagDescriptor);
            }

            public Template Build()
            {
                var templateInternal = new TemplateInternal()
                {
                    m_NameHandle = container.AddString(Name),
                    m_AdditionalShaderIDStringHandle = container.AddString(AdditionalShaderID),
                    m_LinkerHandle = container.AddTemplateLinker(linker)
                };

                templateInternal.m_PassListHandle = FixedHandleListInternal.Build(container, passes, (p) => (p.handle));
                templateInternal.m_TagDescriptorListHandle = FixedHandleListInternal.Build(container, tagDescriptors, (t) => (t.handle));

                var returnTypeHandle = container.AddTemplateInternal(templateInternal);
                return new Template(container, returnTypeHandle);
            }
        }
    }
}
