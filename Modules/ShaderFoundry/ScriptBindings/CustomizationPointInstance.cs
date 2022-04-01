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
    [NativeHeader("Modules/ShaderFoundry/Public/CustomizationPointInstance.h")]
    internal struct CustomizationPointInstanceInternal
    {
        internal FoundryHandle m_CustomizationPointHandle;
        internal FoundryHandle m_BlockInstanceListHandle;
        internal FoundryHandle m_PassIdentifierListHandle;

        internal extern static CustomizationPointInstanceInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct CustomizationPointInstance : IEquatable<CustomizationPointInstance>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly CustomizationPointInstanceInternal customizationPointInstance;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null);
        public CustomizationPoint CustomizationPoint => new CustomizationPoint(container, customizationPointInstance.m_CustomizationPointHandle);

        public IEnumerable<BlockInstance> BlockInstances
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(customizationPointInstance.m_BlockInstanceListHandle);
                return list.Select<BlockInstance>(localContainer, (handle) => (new BlockInstance(localContainer, handle)));
            }
        }

        public IEnumerable<PassIdentifier> PassIdentifiers
        {
            get
            {
                if (Container == null)
                    return Enumerable.Empty<PassIdentifier>();

                var localContainer = Container;
                var list = new FixedHandleListInternal(customizationPointInstance.m_PassIdentifierListHandle);
                return list.Select<PassIdentifier>(localContainer, (handle) => (localContainer.GetPassIdentifier(handle)));
            }
        }

        // private
        internal CustomizationPointInstance(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.customizationPointInstance = container?.GetCustomizationPointInstance(handle) ?? CustomizationPointInstanceInternal.Invalid();
        }

        public static CustomizationPointInstance Invalid => new CustomizationPointInstance(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is CustomizationPointInstance other && this.Equals(other);
        public bool Equals(CustomizationPointInstance other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(CustomizationPointInstance lhs, CustomizationPointInstance rhs) => lhs.Equals(rhs);
        public static bool operator!=(CustomizationPointInstance lhs, CustomizationPointInstance rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            CustomizationPoint customizationPoint = CustomizationPoint.Invalid;
            public List<BlockInstance> BlockInstances { get; set; } = new List<BlockInstance>();
            List<PassIdentifierInternal> PassIdentifiers { get; set; } = new List<PassIdentifierInternal>();
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, CustomizationPoint customizationPoint)
            {
                this.container = container;
                this.customizationPoint = customizationPoint;
            }

            public void AddPassIdentifier(uint subShaderIndex, uint passIndex)
            {
                PassIdentifiers.Add(new PassIdentifierInternal(subShaderIndex, passIndex));
            }

            public CustomizationPointInstance Build()
            {
                var customizationPointInstanceInternal = new CustomizationPointInstanceInternal()
                {
                    m_CustomizationPointHandle = customizationPoint.handle,
                };

                customizationPointInstanceInternal.m_BlockInstanceListHandle = FixedHandleListInternal.Build(container, BlockInstances, (v) => (v.handle));
                customizationPointInstanceInternal.m_PassIdentifierListHandle = FixedHandleListInternal.Build(container, PassIdentifiers, (p) => (container.AddPassIdentifier(p.SubShaderIndex, p.PassIndex)));

                var returnTypeHandle = container.AddCustomizationPointInstanceInternal(customizationPointInstanceInternal);
                return new CustomizationPointInstance(container, returnTypeHandle);
            }
        }
    }
}
