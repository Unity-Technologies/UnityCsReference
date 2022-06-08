// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/Template.h")]
    internal struct TemplateInternal
    {
        internal FoundryHandle m_NameHandle;                        // string
        internal FoundryHandle m_PassListHandle;                    // List<TemplatePass>
        internal FoundryHandle m_TagDescriptorListHandle;           // List<FoundryHandle>
        internal FoundryHandle m_AdditionalShaderIDStringHandle;    // string
        internal FoundryHandle m_LinkerHandle;                      // ILinker (in C# container only)

        // these are per-shader settings, that get passed up to the shader level
        // and merged with the same settings from other subshaders
        internal FoundryHandle m_ShaderDependencyListHandle;        // List<ShaderDependency>
        internal FoundryHandle m_ShaderCustomEditorHandle;          // ShaderCustomEditor
        internal FoundryHandle m_ShaderFallbackHandle;              // string

        internal extern static TemplateInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct Template : IEquatable<Template>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
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

        public IEnumerable<TagDescriptor> TagDescriptors => template.m_TagDescriptorListHandle.AsListEnumerable<TagDescriptor>(container, (container, handle) => (new TagDescriptor(container, handle)));
        public IEnumerable<ShaderDependency> ShaderDependencies => template.m_ShaderDependencyListHandle.AsListEnumerable(container, (container, handle) => new ShaderDependency(container, handle));
        public ShaderCustomEditor CustomEditor => new ShaderCustomEditor(container, template.m_ShaderCustomEditorHandle);

        public string ShaderFallback => container?.GetString(template.m_ShaderFallbackHandle) ?? string.Empty;
        public ITemplateLinker Linker => container?.GetTemplateLinker(template.m_LinkerHandle) ?? null;

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
            public string Name { get; set; }
            List<TemplatePass> passes { get; set; }
            public string AdditionalShaderID { get; set; }
            ITemplateLinker linker;
            List<TagDescriptor> tagDescriptors;
            List<ShaderDependency> shaderDependencies;
            public ShaderCustomEditor CustomEditor { get; set; } = ShaderCustomEditor.Invalid;
            public string ShaderFallback { get; set; }

            readonly ShaderContainer container;
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
                if (pass.IsValid)
                {
                    if (passes == null)
                        passes = new List<TemplatePass>();
                    passes.Add(pass);
                }
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                if (tagDescriptor.IsValid)
                {
                    if (tagDescriptors == null)
                        tagDescriptors = new List<TagDescriptor>();
                    tagDescriptors.Add(tagDescriptor);
                }
            }

            public void AddUsePass(string usePassName)
            {
                if (!string.IsNullOrEmpty(usePassName))
                {
                    var builder = new TemplatePass.UsePassBuilder(container, usePassName);
                    AddPass(builder.Build());
                }
            }

            public void AddShaderDependency(ShaderDependency shaderDependency)
            {
                if (shaderDependency.IsValid)
                {
                    if (shaderDependencies == null)
                        shaderDependencies = new List<ShaderDependency>();
                    shaderDependencies.Add(shaderDependency);
                }
            }

            public void AddShaderDependency(string dependencyName, string shaderName)
            {
                AddShaderDependency(new ShaderDependency(container, dependencyName, shaderName));
            }

            public void SetCustomEditor(string customEditorClassName, string renderPipelineAssetClassName)
            {
                CustomEditor = new ShaderCustomEditor(container, customEditorClassName, renderPipelineAssetClassName);
            }

            public Template Build()
            {
                var templateInternal = new TemplateInternal()
                {
                    m_NameHandle = container.AddString(Name),
                    m_AdditionalShaderIDStringHandle = container.AddString(AdditionalShaderID),
                    m_LinkerHandle = container.AddTemplateLinker(linker),
                    m_ShaderFallbackHandle = container.AddString(ShaderFallback),
                };

                templateInternal.m_PassListHandle = FixedHandleListInternal.Build(container, passes, (p) => (p.handle));
                templateInternal.m_TagDescriptorListHandle = FixedHandleListInternal.Build(container, tagDescriptors, (t) => (t.handle));
                templateInternal.m_ShaderDependencyListHandle = FixedHandleListInternal.Build(container, shaderDependencies, (sd) => sd.handle);
                templateInternal.m_ShaderCustomEditorHandle = CustomEditor.IsValid ? CustomEditor.handle : FoundryHandle.Invalid();

                var returnTypeHandle = container.AddTemplateInternal(templateInternal);
                return new Template(container, returnTypeHandle);
            }
        }
    }
}
