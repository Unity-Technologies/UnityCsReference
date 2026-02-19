// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using ShaderStage = UnityEngine.Shaders.ShaderStage;
using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/StageDescription.h")]
    internal struct StageDescriptionInternal : IInternalType<StageDescriptionInternal>
    {
        internal ShaderStage m_StageType;
        internal FoundryHandle m_SetupVariablesListHandle;
        internal FoundryHandle m_ElementListHandle;
        internal FoundryHandle m_OutputLinkOverridesListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static StageDescriptionInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        StageDescriptionInternal IInternalType<StageDescriptionInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct StageDescription : IEquatable<StageDescription>, IPublicType<StageDescription>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly StageDescriptionInternal stageDescription;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        StageDescription IPublicType<StageDescription>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new StageDescription(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid && stageDescription.IsValid());

        public ShaderStage StageType => stageDescription.m_StageType;

        public IEnumerable<StructField> SetupVariables =>
            ListType.Enumerate<StructField>(container, stageDescription.m_SetupVariablesListHandle);
        public IEnumerable<StructField> InputSetupVariables =>
            SetupVariables.Select(StructFieldInternal.Flags.kInput);
        public IEnumerable<StructField> OutputSetupVariables =>
            SetupVariables.Select(StructFieldInternal.Flags.kOutput);

        public IEnumerable<BlockSequenceElement> Elements =>
            ListType.Enumerate<BlockSequenceElement>(container, stageDescription.m_ElementListHandle);
        public IEnumerable<BlockLinkOverride> OutputLinkOverrides =>
            ListType.Enumerate<BlockLinkOverride>(container, stageDescription.m_OutputLinkOverridesListHandle);

        public Location Location => new Location(container, stageDescription.m_LocationHandle);

        // private
        internal StageDescription(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out stageDescription);
        }

        public static StageDescription Invalid => new StageDescription(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is StageDescription other && this.Equals(other);
        public bool Equals(StageDescription other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(StageDescription lhs, StageDescription rhs) => lhs.Equals(rhs);
        public static bool operator!=(StageDescription lhs, StageDescription rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;

            public ShaderStage stageType;
            public List<StructField> setupVariables;
            public List<BlockSequenceElement> elements;
            public List<BlockLinkOverride> outputOverrides;
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, ShaderStage stageType)
            {
                this.container = container;
                this.stageType = stageType;
            }

            public void AddElement(BlockSequenceElement element)
            {
                Utilities.AddToList(ref elements, element);
            }

            public void AddOutputOverride(BlockLinkOverride linkOverride)
            {
                Utilities.AddToList(ref outputOverrides, linkOverride);
            }

            public void AddSetupVariable(StructField variable)
            {
                Utilities.AddToList(ref setupVariables, variable);
            }

            public StageDescription Build()
            {
                var stageDescriptionInternal = new StageDescriptionInternal()
                {
                    m_StageType = stageType,
                };

                stageDescriptionInternal.m_SetupVariablesListHandle = ListType.Build(container, setupVariables);
                stageDescriptionInternal.m_ElementListHandle = ListType.Build(container, elements);
                stageDescriptionInternal.m_OutputLinkOverridesListHandle = ListType.Build(container, outputOverrides);
                stageDescriptionInternal.m_LocationHandle = location.handle;

                var returnTypeHandle = container.Add(stageDescriptionInternal);
                return new StageDescription(container, returnTypeHandle);
            }
        }
    }
}
