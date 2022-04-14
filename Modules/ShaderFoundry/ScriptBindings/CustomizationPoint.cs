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
    [NativeHeader("Modules/ShaderFoundry/Public/CustomizationPoint.h")]
    internal struct CustomizationPointInternal
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_InputListHandle;
        internal FoundryHandle m_OutputListHandle;
        internal FoundryHandle m_PropertyListHandle;
        internal FoundryHandle m_DefaultBlockInstanceList;

        internal extern static CustomizationPointInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct CustomizationPoint : IEquatable<CustomizationPoint>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly CustomizationPointInternal customizationPoint;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public string Name => container?.GetString(customizationPoint.m_NameHandle) ?? string.Empty;
        public IEnumerable<BlockVariable> Inputs => GetVariableEnumerable(customizationPoint.m_InputListHandle);
        public IEnumerable<BlockVariable> Outputs => GetVariableEnumerable(customizationPoint.m_OutputListHandle);
        public IEnumerable<BlockVariable> Properties => GetVariableEnumerable(customizationPoint.m_PropertyListHandle);
        public IEnumerable<BlockInstance> DefaultBlockInstances
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(customizationPoint.m_DefaultBlockInstanceList);
                return list.Select<BlockInstance>(localContainer, (handle) => (new BlockInstance(localContainer, handle)));
            }
        }

        IEnumerable<BlockVariable> GetVariableEnumerable(FoundryHandle listHandle)
        {
            var localContainer = Container;
            var list = new FixedHandleListInternal(listHandle);
            return list.Select<BlockVariable>(localContainer, (handle) => (new BlockVariable(localContainer, handle)));
        }

        // private
        internal CustomizationPoint(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.customizationPoint = container?.GetCustomizationPoint(handle) ?? CustomizationPointInternal.Invalid();
        }

        public static CustomizationPoint Invalid => new CustomizationPoint(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is CustomizationPoint other && this.Equals(other);
        public bool Equals(CustomizationPoint other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(CustomizationPoint lhs, CustomizationPoint rhs) => lhs.Equals(rhs);
        public static bool operator!=(CustomizationPoint lhs, CustomizationPoint rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            internal string name;
            public List<BlockVariable> inputs { get; set; } = new List<BlockVariable>();
            public List<BlockVariable> outputs { get; set; } = new List<BlockVariable>();
            public List<BlockVariable> properties { get; set; } = new List<BlockVariable>();
            public List<BlockInstance> defaultBlockInstances { get; set; } = new List<BlockInstance>();
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
            }

            public void AddInput(BlockVariable input) { inputs.Add(input); }
            public void AddOutput(BlockVariable output) { outputs.Add(output); }
            public void AddProperty(BlockVariable prop) { properties.Add(prop); }
            public void AddDefaultBlockInstance(BlockInstance blockInstance) { defaultBlockInstances.Add(blockInstance); }

            public CustomizationPoint Build()
            {
                var customizationPointInternal = new CustomizationPointInternal
                {
                    m_NameHandle = container.AddString(name),
                };

                customizationPointInternal.m_InputListHandle = FixedHandleListInternal.Build(container, inputs, (v) => (v.handle));
                customizationPointInternal.m_OutputListHandle = FixedHandleListInternal.Build(container, outputs, (v) => (v.handle));
                customizationPointInternal.m_PropertyListHandle = FixedHandleListInternal.Build(container, properties, (v) => (v.handle));
                customizationPointInternal.m_DefaultBlockInstanceList = FixedHandleListInternal.Build(container, defaultBlockInstances, (v) => (v.handle));

                var returnTypeHandle = container.AddCustomizationPointInternal(customizationPointInternal);
                return new CustomizationPoint(container, returnTypeHandle);
            }
        }
    }
}
