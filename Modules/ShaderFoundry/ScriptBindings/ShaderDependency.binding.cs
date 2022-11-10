// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderDependency.h")]
    internal struct ShaderDependencyInternal : IInternalType<ShaderDependencyInternal>
    {
        internal FoundryHandle m_DependencyNameStringHandle;   // string
        internal FoundryHandle m_ShaderNameStringHandle;       // string

        internal extern static ShaderDependencyInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        ShaderDependencyInternal IInternalType<ShaderDependencyInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderDependency : IEquatable<ShaderDependency>, IComparable<ShaderDependency>, IPublicType<ShaderDependency>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ShaderDependencyInternal shaderDependency;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderDependency IPublicType<ShaderDependency>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderDependency(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public ShaderDependency(ShaderContainer container, string dependencyName, string shaderName)
            : this(container, container.AddString(dependencyName), container.AddString(shaderName))
        {
        }

        internal ShaderDependency(ShaderContainer container, FoundryHandle dependencyName, FoundryHandle shaderName)
        {
            if ((container == null) || (!dependencyName.IsValid) || (!shaderName.IsValid))
            {
                this = Invalid;
            }
            else
            {
                shaderDependency.m_DependencyNameStringHandle = dependencyName;
                shaderDependency.m_ShaderNameStringHandle = shaderName;
                handle = container.Add(shaderDependency);
                this.container = handle.IsValid ? container : null;
            }
        }

        public string DependencyName => container?.GetString(shaderDependency.m_DependencyNameStringHandle);
        public string ShaderName => container?.GetString(shaderDependency.m_ShaderNameStringHandle);

        internal ShaderDependency(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out shaderDependency);
        }

        public static ShaderDependency Invalid => new ShaderDependency(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderDependency other && this.Equals(other);
        public bool Equals(ShaderDependency other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderDependency lhs, ShaderDependency rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderDependency lhs, ShaderDependency rhs) => !lhs.Equals(rhs);

        public int CompareTo(ShaderDependency other)
        {
            int result = string.CompareOrdinal(DependencyName, other.DependencyName);
            if (result == 0)
                result = string.CompareOrdinal(ShaderName, other.ShaderName);
            return result;
        }

        public class Builder
        {
            ShaderContainer container;
            string dependencyName;
            string shaderName;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string dependencyName, string shaderName)
            {
                this.container = container;
                this.dependencyName = dependencyName;
                this.shaderName = shaderName;
            }

            public ShaderDependency Build()
            {
                return new ShaderDependency(container, dependencyName, shaderName);
            }
        }
    }
}
