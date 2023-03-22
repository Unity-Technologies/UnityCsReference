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
    internal struct TemplateInternal : IInternalType<TemplateInternal>
    {
        internal FoundryHandle m_NameHandle;                        // string
        internal FoundryHandle m_AttributeListHandle;               // List<ShaderAttribute>
        internal FoundryHandle m_ContainingNamespaceHandle;         // Namespace
        internal FoundryHandle m_PassListHandle;                    // List<TemplatePass>
        internal FoundryHandle m_TagDescriptorListHandle;           // List<FoundryHandle>
        internal FoundryHandle m_LODHandle;                         // string
        internal FoundryHandle m_PackageRequirementListHandle;      // List<PackageRequirements>
        internal FoundryHandle m_CustomizationPointListHandle;      // List<CustomizationPoint>
        internal FoundryHandle m_ExtendedTemplateListHandle;        // List<Template>
        internal FoundryHandle m_PassCopyRuleListHandle;            // List<CopyRule>
        internal FoundryHandle m_CustomizationPointCopyRuleListHandle; // List<CopyRule>
        internal FoundryHandle m_CustomizationPointImplementationListHandle; // List<CustomizationPointImplementation>
        internal FoundryHandle m_AdditionalShaderIDStringHandle;    // string
        internal FoundryHandle m_LinkerHandle;                      // ILinker (in C# container only)

        // these are per-shader settings, that get passed up to the shader level
        // and merged with the same settings from other subshaders
        internal FoundryHandle m_ShaderDependencyListHandle;        // List<ShaderDependency>
        internal FoundryHandle m_ShaderCustomEditorHandle;          // ShaderCustomEditor
        internal FoundryHandle m_ShaderFallbackHandle;              // string

        internal extern static TemplateInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        TemplateInternal IInternalType<TemplateInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct Template : IEquatable<Template>, IPublicType<Template>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly TemplateInternal template;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        Template IPublicType<Template>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new Template(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public string Name => container?.GetString(template.m_NameHandle) ?? string.Empty;
        public IEnumerable<ShaderAttribute> Attributes => FixedHandleListInternal.Enumerate<ShaderAttribute>(container, template.m_AttributeListHandle);
        public Namespace ContainingNamespace => new Namespace(container, template.m_ContainingNamespaceHandle);
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
        public string LOD => container?.GetString(template.m_LODHandle) ?? string.Empty;
        public IEnumerable<PackageRequirement> PackageRequirements => template.m_PackageRequirementListHandle.AsListEnumerable<PackageRequirement>(container, (container, handle) => (new PackageRequirement(container, handle)));
        public IEnumerable<CustomizationPoint> CustomizationPoints => template.m_CustomizationPointListHandle.AsListEnumerable<CustomizationPoint>(Container, (container, handle) => (new CustomizationPoint(container, handle)));
        public IEnumerable<Template> ExtendedTemplates => template.m_ExtendedTemplateListHandle.AsListEnumerable<Template>(container, (container, handle) => (new Template(container, handle)));
        public IEnumerable<CopyRule> PassCopyRules => template.m_PassCopyRuleListHandle.AsListEnumerable<CopyRule>(container, (container, handle) => (new CopyRule(container, handle)));
        public IEnumerable<CopyRule> CustomizationPointCopyRules => template.m_CustomizationPointCopyRuleListHandle.AsListEnumerable<CopyRule>(container, (container, handle) => (new CopyRule(container, handle)));
        public IEnumerable<CustomizationPointImplementation> CustomizationPointImplementations => template.m_CustomizationPointImplementationListHandle.AsListEnumerable<CustomizationPointImplementation>(container, (container, handle) => (new CustomizationPointImplementation(container, handle)));
        public IEnumerable<ShaderDependency> ShaderDependencies => template.m_ShaderDependencyListHandle.AsListEnumerable(container, (container, handle) => new ShaderDependency(container, handle));
        public ShaderCustomEditor CustomEditor => new ShaderCustomEditor(container, template.m_ShaderCustomEditorHandle);

        public string ShaderFallback => container?.GetString(template.m_ShaderFallbackHandle) ?? string.Empty;
        public ITemplateLinker Linker => container?.GetTemplateLinker(template.m_LinkerHandle) ?? null;

        // private
        internal Template(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out template);
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
            public List<ShaderAttribute> attributes;
            public Namespace containingNamespace = Namespace.Invalid;
            List<TemplatePass> passes { get; set; }
            public string AdditionalShaderID { get; set; }
            ITemplateLinker linker;
            List<TagDescriptor> tagDescriptors;
            public string LOD;
            List<PackageRequirement> packageRequirements;
            List<CustomizationPoint> customizationPoints;
            List<Template> extendedTemplates;
            List<CopyRule> passCopyRules;
            List<CopyRule> customizationPointCopyRules;
            List<CustomizationPointImplementation> customizationPointImplementations;
            List<ShaderDependency> shaderDependencies;
            public ShaderCustomEditor CustomEditor { get; set; } = ShaderCustomEditor.Invalid;
            public string ShaderFallback { get; set; }

            readonly ShaderContainer container;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                if (container == null)
                    throw new Exception("A valid ShaderContainer must be provided to create a Template Builder.");

                this.container = container;
                this.Name = name;
                this.containingNamespace = Utilities.BuildDefaultObjectNamespace(container, name);
                this.linker = null;
            }

            public Builder(ShaderContainer container, string name, ITemplateLinker linker)
            {
                if (container == null || linker == null)
                    throw new Exception("A valid ShaderContainer and ITemplateLinker must be provided to create a Template Builder.");

                this.container = container;
                this.Name = name;
                this.containingNamespace = Utilities.BuildDefaultObjectNamespace(container, name);
                this.linker = linker;
            }

            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
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

            public void AddPackageRequirement(PackageRequirement packageRequirement)
            {
                if (packageRequirement.IsValid)
                {
                    if (packageRequirements == null)
                        packageRequirements = new List<PackageRequirement>();
                    packageRequirements.Add(packageRequirement);
                }
            }

            public void AddCustomizationPoint(CustomizationPoint customizationPoint)
            {
                if (customizationPoint.IsValid)
                {
                    if (customizationPoints == null)
                        customizationPoints = new List<CustomizationPoint>();
                    customizationPoints.Add(customizationPoint);
                }
            }

            public void AddTemplateExtension(Template template) => Utilities.AddToList(ref extendedTemplates, template);
            public void AddPassCopyRule(CopyRule rule) => Utilities.AddToList(ref passCopyRules, rule);
            public void AddCustomizationPointCopyRule(CopyRule rule) => Utilities.AddToList(ref customizationPointCopyRules, rule);
            public void AddCustomizationPointImplementation(CustomizationPointImplementation customizationPointImplementation) => Utilities.AddToList(ref customizationPointImplementations, customizationPointImplementation);

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

                templateInternal.m_AttributeListHandle = FixedHandleListInternal.Build(container, attributes);
                templateInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                templateInternal.m_PassListHandle = FixedHandleListInternal.Build(container, passes, (p) => (p.handle));
                templateInternal.m_LODHandle = container.AddString(LOD);
                templateInternal.m_PackageRequirementListHandle = FixedHandleListInternal.Build(container, packageRequirements, (p) => (p.handle));
                templateInternal.m_CustomizationPointListHandle = FixedHandleListInternal.Build(container, customizationPoints, (c) => c.handle);
                templateInternal.m_ExtendedTemplateListHandle = FixedHandleListInternal.Build(container, extendedTemplates, (t) => (t.handle));
                templateInternal.m_PassCopyRuleListHandle = FixedHandleListInternal.Build(container, passCopyRules, (r) => (r.handle));
                templateInternal.m_CustomizationPointCopyRuleListHandle = FixedHandleListInternal.Build(container, customizationPointCopyRules, (r) => (r.handle));
                templateInternal.m_CustomizationPointImplementationListHandle = FixedHandleListInternal.Build(container, customizationPointImplementations, (c) => (c.handle));
                templateInternal.m_TagDescriptorListHandle = FixedHandleListInternal.Build(container, tagDescriptors, (t) => (t.handle));
                templateInternal.m_ShaderDependencyListHandle = FixedHandleListInternal.Build(container, shaderDependencies, (sd) => sd.handle);
                templateInternal.m_ShaderCustomEditorHandle = CustomEditor.IsValid ? CustomEditor.handle : FoundryHandle.Invalid();

                var returnTypeHandle = container.Add(templateInternal);
                return new Template(container, returnTypeHandle);
            }
        }
    }
}
