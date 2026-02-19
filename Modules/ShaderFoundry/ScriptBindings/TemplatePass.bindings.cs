// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using ShaderStage = UnityEngine.Shaders.ShaderStage;
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
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static TemplatePassInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsUsePass();

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

        public StageDescription GetStageDescription(ShaderStage stageType)
        {
            var stages = ListType.Enumerate<StageDescription>(container, templatePass.m_StageDescriptionListHandle);
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
                var items = ListType.Enumerate<StageDescription>(container, templatePass.m_StageDescriptionListHandle);
                foreach (var item in items)
                {
                    if (item.IsValid)
                        yield return item;
                }
            }
        }

        public IEnumerable<RenderStateDescriptor> RenderStateDescriptors =>
            ListType.Enumerate<RenderStateDescriptor>(container, templatePass.m_RenderStateDescriptorListHandle);

        public IEnumerable<PragmaDescriptor> PragmaDescriptors =>
            ListType.Enumerate<PragmaDescriptor>(container, templatePass.m_PragmaDescriptorListHandle);

        public IEnumerable<TagDescriptor> TagDescriptors =>
            ListType.Enumerate<TagDescriptor>(container, templatePass.m_TagDescriptorListHandle);

        public IEnumerable<PackageRequirement> PackageRequirements =>
            ListType.Enumerate<PackageRequirement>(container, templatePass.m_PackageRequirementListHandle);

        public bool EnableDebugging => templatePass.m_EnableDebugging;
        public Location Location => new Location(container, templatePass.m_LocationHandle);

        // private
        internal TemplatePass(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out templatePass);
        }

        public static TemplatePass Invalid => new TemplatePass(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is TemplatePass other && this.Equals(other);
        public bool Equals(TemplatePass other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(TemplatePass lhs, TemplatePass rhs) => lhs.Equals(rhs);
        public static bool operator!=(TemplatePass lhs, TemplatePass rhs) => !lhs.Equals(rhs);

        internal class UsePassBuilder
        {
            ShaderContainer container;
            string usePassName;
            public Location location;

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
                    m_EnableDebugging = false,
                    m_LocationHandle = location.handle,
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
            List<TagDescriptor> tagDescriptors;
            List<PackageRequirement> packageRequirements;
            public Location location;

            public string PassName { get; set; }
            public bool EnableDebugging { get; set; }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
                stageDescriptions = new List<StageDescription>();
                for (var i = 0; i < (int)ShaderStage.Count; ++i)
                    stageDescriptions.Add(StageDescription.Invalid);
            }

            public void AddRenderStateDescriptor(RenderStateDescriptor renderStateDescriptor)
            {
                Utilities.AddToList(ref renderStateDescriptors, renderStateDescriptor);
            }

            public void AddPragmaDescriptor(PragmaDescriptor pragmaDescriptor)
            {
                Utilities.AddToList(ref pragmaDescriptors, pragmaDescriptor);
            }

            public void AddTagDescriptor(TagDescriptor tagDescriptor)
            {
                Utilities.AddToList(ref tagDescriptors, tagDescriptor);
            }

            public void AddPackageRequirement(PackageRequirement packageRequirement)
            {
                if (packageRequirement.IsValid)
                {
                    Utilities.AddToList(ref packageRequirements, packageRequirement);
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


                templatePassInternal.m_StageDescriptionListHandle = ListType.Build(container, BuildValidStages());
                templatePassInternal.m_RenderStateDescriptorListHandle = ListType.Build(container, renderStateDescriptors);
                templatePassInternal.m_PragmaDescriptorListHandle = ListType.Build(container, pragmaDescriptors);
                templatePassInternal.m_TagDescriptorListHandle = ListType.Build(container, tagDescriptors);
                templatePassInternal.m_PackageRequirementListHandle = ListType.Build(container, packageRequirements);
                templatePassInternal.m_LocationHandle = location.handle;

                var returnTypeHandle = container.Add(templatePassInternal);
                return new TemplatePass(container, returnTypeHandle);
            }
        }
    }
}
