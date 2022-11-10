// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Bindings;
using PassIdentifier = UnityEngine.Rendering.PassIdentifier;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/TemplatePass.h")]
    internal struct TemplatePassInternal : IInternalType<TemplatePassInternal>
    {
        internal FoundryHandle m_UsePassNameHandle;
        internal FoundryHandle m_DisplayNameHandle;
        internal FoundryHandle m_ReferenceNameHandle;
        internal FoundryHandle m_PassIdentifierHandle;
        internal FoundryHandle m_StageDescriptionListHandle;
        internal FoundryHandle m_CommandDescriptorListHandle;
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
        public string DisplayName => container?.GetString(templatePass.m_DisplayNameHandle) ?? string.Empty;
        public string ReferenceName => container?.GetString(templatePass.m_ReferenceNameHandle) ?? string.Empty;
        // TODO SHADER: The else case should return invalid pass identifier once it's possible to construct this in managed.
        public PassIdentifier PassIdentifier => container?.GetPassIdentifier(templatePass.m_PassIdentifierHandle) ?? new PassIdentifier();

        public StageDescription GetStageDescription(PassStageType stageType)
        {
            var list = new FixedHandleListInternal(templatePass.m_StageDescriptionListHandle);
            var handle = list.GetElement(container, (uint)stageType);
            return new StageDescription(container, handle);
        }

        public IEnumerable<StageDescription> StageDescriptions => templatePass.m_StageDescriptionListHandle.AsListEnumerable<StageDescription>(container, (container, handle) => (new StageDescription(container, handle))).Where((s) => (s.IsValid));

        public IEnumerable<CommandDescriptor> CommandDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(templatePass.m_CommandDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new CommandDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<PragmaDescriptor> PragmaDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(templatePass.m_PragmaDescriptorListHandle);
                return list.Select(localContainer, (handle) => (new PragmaDescriptor(localContainer, handle)));
            }
        }

        public IEnumerable<TagDescriptor> TagDescriptors
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(templatePass.m_TagDescriptorListHandle);
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
                    m_DisplayNameHandle = invalid,
                    m_ReferenceNameHandle = invalid,
                    m_PassIdentifierHandle = invalid,
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
            PassIdentifierInternal passIdentifier = new PassIdentifierInternal(uint.MaxValue, uint.MaxValue);
            List<StageDescription> stageDescriptions;
            List<CommandDescriptor> commandDescriptors;
            List<PragmaDescriptor> pragmaDescriptors;
            List<TagDescriptor> tagDescriptors = new List<TagDescriptor>();
            List<PackageRequirement> packageRequirements;

            public string DisplayName { get; set; }
            public string ReferenceName { get; set; }
            public bool EnableDebugging { get; set; }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
                stageDescriptions = new List<StageDescription>();
                for (var i = 0; i < (int)PassStageType.Count; ++i)
                    stageDescriptions.Add(StageDescription.Invalid);
            }

            public void SetPassIdentifier(uint subShaderIndex, uint passIndex)
            {
                passIdentifier = new PassIdentifierInternal(subShaderIndex, passIndex);
            }

            public void AddCommandDescriptor(CommandDescriptor commandDescriptor)
            {
                if (commandDescriptors == null)
                    commandDescriptors = new List<CommandDescriptor>();
                commandDescriptors.Add(commandDescriptor);
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

            public TemplatePass Build()
            {
                var templatePassInternal = new TemplatePassInternal()
                {
                    m_UsePassNameHandle = FoundryHandle.Invalid(),
                    m_DisplayNameHandle = container.AddString(DisplayName),
                    m_ReferenceNameHandle = container.AddString(ReferenceName),
                    m_EnableDebugging = EnableDebugging,
                };

                templatePassInternal.m_PassIdentifierHandle = container.AddPassIdentifier(passIdentifier.SubShaderIndex, passIdentifier.PassIndex);
                templatePassInternal.m_StageDescriptionListHandle = FixedHandleListInternal.Build(container, stageDescriptions, (s) => (s.handle));
                templatePassInternal.m_CommandDescriptorListHandle = FixedHandleListInternal.Build(container, commandDescriptors, (o) => (o.handle));
                templatePassInternal.m_PragmaDescriptorListHandle = FixedHandleListInternal.Build(container, pragmaDescriptors, (o) => (o.handle));
                templatePassInternal.m_TagDescriptorListHandle = FixedHandleListInternal.Build(container, tagDescriptors, (o) => (o.handle));
                templatePassInternal.m_PackageRequirementListHandle = FixedHandleListInternal.Build(container, packageRequirements, (p) => (p.handle));

                var returnTypeHandle = container.Add(templatePassInternal);
                return new TemplatePass(container, returnTypeHandle);
            }
        }
    }
}
