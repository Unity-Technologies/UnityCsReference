// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/TemplatePass.h")]
    internal struct TemplatePassInternal : IInternalType<TemplatePassInternal>
    {
        internal FoundryHandle m_UsePassNameHandle;
        internal FoundryHandle m_PassNameHandle;
        internal FoundryHandle m_StageDescriptionListHandle;
        internal FoundryHandle m_RenderStateDescriptorListHandle;
        internal FoundryHandle m_PragmaDescriptorListHandle;
        internal FoundryHandle m_TagDescriptorListHandle;
        internal FoundryHandle m_PackageRequirementListHandle;
        internal bool m_EnableDebugging;

        internal extern static TemplatePassInternal Invalid();
        internal extern bool IsValid();
        internal extern bool IsUsePass();

        // IInternalType
        TemplatePassInternal IInternalType<TemplatePassInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct TemplatePass : IEquatable<TemplatePass>, IPublicType<TemplatePass>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly TemplatePassInternal templatePass;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        TemplatePass IPublicType<TemplatePass>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new TemplatePass(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public bool IsUsePass => (IsValid && templatePass.IsUsePass());
        public bool IsNormalPass => (IsValid && !templatePass.IsUsePass());
        public string UsePassName => container?.GetString(templatePass.m_UsePassNameHandle) ?? string.Empty;
        public string PassName => container?.GetString(templatePass.m_PassNameHandle) ?? string.Empty;

        public StageDescription GetStageDescription(PassStageType stageType)
        {
            var stages = HandleListInternal.Enumerate<StageDescription>(container, templatePass.m_StageDescriptionListHandle);
            foreach(var stage in stages)
            {
                if (stage.StageType == stageType)
                    return stage;
            }
            return StageDescription.Invalid;
        }

        public IEnumerable<StageDescription> StageDescriptions
        {
            get
            {
                var items = HandleListInternal.Enumerate<StageDescription>(container, templatePass.m_StageDescriptionListHandle);
                foreach (var item in items)
                {
                    if (item.IsValid)
                        yield return item;
                }
            }
        }

        public IEnumerable<RenderStateDescriptor> RenderStateDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new HandleListInternal(templatePass.m_RenderStateDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new RenderStateDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<PragmaDescriptor> PragmaDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new HandleListInternal(templatePass.m_PragmaDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new PragmaDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<TagDescriptor> TagDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new HandleListInternal(templatePass.m_TagDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new TagDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<PackageRequirement> PackageRequirements => templatePass.m_PackageRequirementListHandle.AsListEnumerable<PackageRequirement>(container, (container, handle) => (new PackageRequirement(container, handle)));

        public bool EnableDebugging => templatePass.m_EnableDebugging;

        // private
        internal TemplatePass(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out templatePass);
        }

        public static TemplatePass Invalid => new TemplatePass(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is TemplatePass other && this.Equals(other);
        public bool Equals(TemplatePass other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(TemplatePass lhs, TemplatePass rhs) => lhs.Equals(rhs);
        public static bool operator!=(TemplatePass lhs, TemplatePass rhs) => !lhs.Equals(rhs);

        internal class UsePassBuilder
        {
            ShaderContainer container;
            string usePassName;

            public UsePassBuilder(ShaderContainer container, string usePassName)
            {
                this.container = container;
                this.usePassName = usePassName;
            }

            public TemplatePass Build()
            {
                FoundryHandle invalid = FoundryHandle.Invalid();

                var templatePassInternal = new TemplatePassInternal()
                {
                    m_UsePassNameHandle = container.AddString(usePassName),
                    m_PassNameHandle = invalid,
                    m_TagDescriptorListHandle = invalid,
                    m_PackageRequirementListHandle = invalid,
                    m_EnableDebugging = false
                };

                var passHandle = container.Add(templatePassInternal);
                return new TemplatePass(container, passHandle);
            }
        }

        public class Builder
        {
            ShaderContainer container;
            List<StageDescription> stageDescriptions;
            List<RenderStateDescriptor> renderStateDescriptors;
            List<PragmaDescriptor> pragmaDescriptors;
            List<TagDescriptor> tagDescriptors = new List<TagDescriptor>();
            List<PackageRequirement> packageRequirements;

            public string PassName { get; set; }
            public bool EnableDebugging { get; set; }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
                stageDescriptions = new List<StageDescription>();
                for (var i = 0; i < (int)PassStageType.Count; ++i)
                    stageDescriptions.Add(StageDescription.Invalid);
            }

            public void AddRenderStateDescriptor(RenderStateDescriptor renderStateDescriptor)
            {
                if (renderStateDescriptors == null)
                    renderStateDescriptors = new List<RenderStateDescriptor>();
                renderStateDescriptors.Add(renderStateDescriptor);
            }

            public void AddPragmaDescriptor(PragmaDescriptor pragmaDescriptor)
            {
                if (pragmaDescriptors == null)
                    pragmaDescriptors = new List<PragmaDescriptor>();
                pragmaDescriptors.Add(pragmaDescriptor);
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                tagDescriptors.Add(tagDescriptor);
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

            public void SetStageDescription(StageDescription stageDescription)
            {
                var stageTypeIndex = (int)stageDescription.StageType;
                if (stageTypeIndex < 0 || stageDescriptions.Count <= stageTypeIndex)
                    throw new Exception($"Invalid StageDescription. StageType {stageDescription.StageType} is not valid");
                stageDescriptions[stageTypeIndex] = stageDescription;
            }

            List<StageDescription> BuildValidStages()
            {
                // For a cleaner API, the stages are pre-allocated. Filter the list to only valid stages
                var validStages = new List<StageDescription>();
                foreach (var stage in stageDescriptions)
                {
                    if (stage.IsValid)
                        validStages.Add(stage);
                }
                return validStages;
            }

            public TemplatePass Build()
            {
                var templatePassInternal = new TemplatePassInternal()
                {
                    m_UsePassNameHandle = FoundryHandle.Invalid(),
                    m_PassNameHandle = container.AddString(PassName),
                    m_EnableDebugging = EnableDebugging,
                };


                templatePassInternal.m_StageDescriptionListHandle = HandleListInternal.Build(container, BuildValidStages());
                templatePassInternal.m_RenderStateDescriptorListHandle = HandleListInternal.Build(container, renderStateDescriptors, (o) => (o.handle));
                templatePassInternal.m_PragmaDescriptorListHandle = HandleListInternal.Build(container, pragmaDescriptors, (o) => (o.handle));
                templatePassInternal.m_TagDescriptorListHandle = HandleListInternal.Build(container, tagDescriptors, (o) => (o.handle));
                templatePassInternal.m_PackageRequirementListHandle = HandleListInternal.Build(container, packageRequirements, (p) => (p.handle));

                var returnTypeHandle = container.Add(templatePassInternal);
                return new TemplatePass(container, returnTypeHandle);
            }
        }
    }
}
