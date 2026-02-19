// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/Template.h")]
    internal struct TemplateInternal : IInternalType<TemplateInternal>
    {
        internal FoundryHandle m_NameHandle;                        // string
        internal FoundryHandle m_AttributeListHandle;               // List<ShaderAttribute>
        internal FoundryHandle m_ContainingNamespaceHandle;         // Namespace
        internal FoundryHandle m_PassListHandle;                    // List<TemplatePass>
        internal FoundryHandle m_TagDescriptorListHandle;           // List<TagDescriptor>
        internal FoundryHandle m_RenderStateDescriptorListHandle;   // List<RenderStateDecriptor>
        internal FoundryHandle m_LODHandle;                         // IntegerLiteral
        internal FoundryHandle m_PackageRequirementListHandle;      // List<PackageRequirements>
        internal FoundryHandle m_CustomizationPointListHandle;      // List<CustomizationPoint>
        internal FoundryHandle m_ExtendedTemplateListHandle;        // List<Template>
        internal FoundryHandle m_PassCopyRuleListHandle;            // List<CopyRule>
        internal FoundryHandle m_CustomizationPointCopyRuleListHandle; // List<CopyRule>
        internal FoundryHandle m_CustomizationPointImplementationListHandle; // List<CustomizationPointImplementation>
        internal FoundryHandle m_AdditionalShaderIDStringHandle;    // string
        internal FoundryHandle m_BlockListHandle;                   // List<Block>
        internal FoundryHandle m_BlockSequenceList;                 // List<BlockSequence>
        internal FoundryHandle m_CustomAttributeDefinitionListHandle; // List<CustomAttributeDefinition>

        // these are per-shader settings, that get passed up to the shader level
        // and merged with the same settings from other subshaders
        internal FoundryHandle m_ShaderDependencyListHandle;        // List<ShaderDependency>
        internal FoundryHandle m_ShaderFallbackHandle;              // string

        internal FoundryHandle m_LocationHandle;
        internal FoundryHandle m_LodLocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static TemplateInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

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
        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, template.m_AttributeListHandle);
        public Namespace ContainingNamespace => new Namespace(container, template.m_ContainingNamespaceHandle);
        public string AdditionalShaderID => container?.GetString(template.m_AdditionalShaderIDStringHandle) ?? string.Empty;
        public IEnumerable<TemplatePass> Passes =>
            ListType.Enumerate<TemplatePass>(container, template.m_PassListHandle);
        public IEnumerable<TagDescriptor> TagDescriptors =>
            ListType.Enumerate<TagDescriptor>(container, template.m_TagDescriptorListHandle);
        public IEnumerable<RenderStateDescriptor> RenderStateDescriptors =>
            ListType.Enumerate<RenderStateDescriptor>(container, template.m_RenderStateDescriptorListHandle);
        public int? LOD => template.m_LODHandle.IsValid ? new IntegerLiteral(container, template.m_LODHandle).Value : null;
        public IEnumerable<PackageRequirement> PackageRequirements =>
            ListType.Enumerate<PackageRequirement>(container, template.m_PackageRequirementListHandle);
        public IEnumerable<CustomizationPoint> CustomizationPoints =>
            ListType.Enumerate<CustomizationPoint>(container, template.m_CustomizationPointListHandle);
        public IEnumerable<Block> Blocks =>
            ListType.Enumerate<Block>(container, template.m_BlockListHandle);
        public IEnumerable<CustomAttributeDefinition> CustomAttributeDefinitions =>
            ListType.Enumerate<CustomAttributeDefinition>(container, template.m_CustomAttributeDefinitionListHandle);
        public IEnumerable<Template> ExtendedTemplates =>
            ListType.Enumerate<Template>(container, template.m_ExtendedTemplateListHandle);
        public IEnumerable<CopyRule> PassCopyRules =>
            ListType.Enumerate<CopyRule>(container, template.m_PassCopyRuleListHandle);
        public IEnumerable<CopyRule> CustomizationPointCopyRules =>
            ListType.Enumerate<CopyRule>(container, template.m_CustomizationPointCopyRuleListHandle);
        public IEnumerable<CustomizationPointImplementation> CustomizationPointImplementations =>
            ListType.Enumerate<CustomizationPointImplementation>(container, template.m_CustomizationPointImplementationListHandle);
        public IEnumerable<ShaderDependency> ShaderDependencies =>
            ListType.Enumerate<ShaderDependency>(container, template.m_ShaderDependencyListHandle);

        public string ShaderFallback => container?.GetString(template.m_ShaderFallbackHandle) ?? string.Empty;
        public Location Location => new Location(container, template.m_LocationHandle);
        public Location LODLocation => new Location(container, template.m_LodLocationHandle);

        // private
        internal Template(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out template);
        }

        public static Template Invalid => new Template(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
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
            List<TemplatePass> passes;
            public string AdditionalShaderID { get; set; }
            List<TagDescriptor> tagDescriptors;
            List<RenderStateDescriptor> renderStateDescriptors;
            public int? LOD;
            List<PackageRequirement> packageRequirements;
            List<CustomizationPoint> customizationPoints;
            List<Block> blocks;
            List<CustomAttributeDefinition> customAttributeDefinitions;
            List<Template> extendedTemplates;
            List<CopyRule> passCopyRules;
            List<CopyRule> customizationPointCopyRules;
            List<CustomizationPointImplementation> customizationPointImplementations;
            List<ShaderDependency> shaderDependencies;
            public string ShaderFallback { get; set; }
            public Location location;
            public Location lodLocation;

            readonly ShaderContainer container;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                if (container == null)
                    throw new Exception("A valid ShaderContainer must be provided to create a Template Builder.");

                this.container = container;
                this.Name = name;
                this.containingNamespace = Utilities.BuildSymbolNamespace(container, name, DataType.Template);
            }

            public void AddAttribute(ShaderAttribute attribute) => Utilities.AddToList(ref attributes, attribute);
            public void AddPass(TemplatePass pass)
            {
                if (pass.IsValid)
                {
                    Utilities.AddToList(ref passes, pass);
                }
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                if (tagDescriptor.IsValid)
                {
                    Utilities.AddToList(ref tagDescriptors, tagDescriptor);
                }
            }

            public void AddRenderStateDescriptor(RenderStateDescriptor renderStateDescriptor)
            {
                if (renderStateDescriptor.IsValid)
                {
                    Utilities.AddToList(ref renderStateDescriptors, renderStateDescriptor);
                }
            }

            public void AddPackageRequirement(PackageRequirement packageRequirement)
            {
                if (packageRequirement.IsValid)
                {
                    Utilities.AddToList(ref packageRequirements, packageRequirement);
                }
            }

            public void AddCustomizationPoint(CustomizationPoint customizationPoint)
            {
                if (customizationPoint.IsValid)
                {
                    Utilities.AddToList(ref customizationPoints, customizationPoint);
                }
            }

            public void AddBlock(Block block)
            {
                if (block.IsValid)
                {
                    Utilities.AddToList(ref blocks, block);
                }
            }

            public void AddCustomAttributeDefinition(CustomAttributeDefinition customAttributeDefinition)
            {
                if (customAttributeDefinition.IsValid)
                {
                    Utilities.AddToList(ref customAttributeDefinitions, customAttributeDefinition);
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
                    Utilities.AddToList(ref shaderDependencies, shaderDependency);
                }
            }

            public void AddShaderDependency(string dependencyName, string shaderName)
            {
                AddShaderDependency(new ShaderDependency(container, dependencyName, shaderName));
            }

            public Template Build()
            {
                var templateInternal = new TemplateInternal()
                {
                    m_NameHandle = container.AddString(Name),
                    m_AdditionalShaderIDStringHandle = container.AddString(AdditionalShaderID),
                    m_ShaderFallbackHandle = container.AddString(ShaderFallback),
                };

                templateInternal.m_AttributeListHandle = ListType.Build(container, attributes);
                templateInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                templateInternal.m_PassListHandle = ListType.Build(container, passes);

                if (LOD != null)
                {
                    var intBuilder = new IntegerLiteral.Builder(container);
                    intBuilder.Value = LOD.Value;
                    var integer = intBuilder.Build();
                    templateInternal.m_LODHandle = integer.handle;
                }
                templateInternal.m_PackageRequirementListHandle = ListType.Build(container, packageRequirements);
                templateInternal.m_CustomizationPointListHandle = ListType.Build(container, customizationPoints);
                templateInternal.m_BlockListHandle = ListType.Build(container, blocks);
                templateInternal.m_CustomAttributeDefinitionListHandle = ListType.Build(container, customAttributeDefinitions);
                templateInternal.m_ExtendedTemplateListHandle = ListType.Build(container, extendedTemplates);
                templateInternal.m_PassCopyRuleListHandle = ListType.Build(container, passCopyRules);
                templateInternal.m_CustomizationPointCopyRuleListHandle = ListType.Build(container, customizationPointCopyRules);
                templateInternal.m_CustomizationPointImplementationListHandle = ListType.Build(container, customizationPointImplementations);
                templateInternal.m_TagDescriptorListHandle = ListType.Build(container, tagDescriptors);
                templateInternal.m_RenderStateDescriptorListHandle = ListType.Build(container, renderStateDescriptors);
                templateInternal.m_ShaderDependencyListHandle = ListType.Build(container, shaderDependencies);
                templateInternal.m_LocationHandle = location.handle;
                templateInternal.m_LodLocationHandle = lodLocation.handle;

                var returnTypeHandle = container.Add(templateInternal);
                return new Template(container, returnTypeHandle);
            }
        }
    }
}
