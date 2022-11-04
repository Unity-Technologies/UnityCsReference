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
    [NativeHeader("Modules/ShaderFoundry/Public/StageDescription.h")]
    internal struct StageDescriptionInternal
    {
        internal PassStageType m_StageType;
        internal FoundryHandle m_SetupVariablesListHandle;
        internal FoundryHandle m_ElementListHandle;

        internal extern static StageDescriptionInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct StageDescription : IEquatable<StageDescription>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly StageDescriptionInternal stageDescription;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid && stageDescription.IsValid());

        public PassStageType StageType => stageDescription.m_StageType;

        public IEnumerable<FunctionParameter> SetupVariables => stageDescription.m_SetupVariablesListHandle.AsListEnumerable<FunctionParameter>(Container, (container, handle) => (new FunctionParameter(container, handle)));
        public IEnumerable<FunctionParameter> InputSetupVariables => SetupVariables.Where((v) => (v.IsInput));
        public IEnumerable<FunctionParameter> OutputSetupVariables => SetupVariables.Where((v) => (v.IsOutput));

        public IEnumerable<BlockSequenceElement> Elements
        {
            get
            {
                return stageDescription.m_ElementListHandle.AsListEnumerable<BlockSequenceElement>(Container,
                    (container, handle) => (new BlockSequenceElement(container, handle)));
            }
        }
        // private
        internal StageDescription(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.stageDescription = container?.GetStageDescription(handle) ?? StageDescriptionInternal.Invalid();
        }

        public static StageDescription Invalid => new StageDescription(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is StageDescription other && this.Equals(other);
        public bool Equals(StageDescription other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(StageDescription lhs, StageDescription rhs) => lhs.Equals(rhs);
        public static bool operator!=(StageDescription lhs, StageDescription rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;

            public PassStageType stageType;
            public List<FunctionParameter> setupVariables;
            public List<BlockSequenceElement> elements;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, PassStageType stageType)
            {
                this.container = container;
                this.stageType = stageType;
            }

            public void AddElement(BlockSequenceElement element)
            {
                if (elements == null)
                    elements = new List<BlockSequenceElement>();
                elements.Add(element);
            }

            public void AddSetupVariable(FunctionParameter param)
            {
                if (setupVariables == null)
                    setupVariables = new List<FunctionParameter>();

                setupVariables.Add(param);
            }

            public StageDescription Build()
            {
                var stageDescriptionInternal = new StageDescriptionInternal()
                {
                    m_StageType = stageType,
                };

                stageDescriptionInternal.m_SetupVariablesListHandle = FixedHandleListInternal.Build(container, setupVariables, (e) => (e.handle));
                stageDescriptionInternal.m_ElementListHandle = FixedHandleListInternal.Build(container, elements, (e) => (e.handle));

                var returnTypeHandle = container.AddStageDescriptionInternal(stageDescriptionInternal);
                return new StageDescription(container, returnTypeHandle);
            }
        }
    }
}
