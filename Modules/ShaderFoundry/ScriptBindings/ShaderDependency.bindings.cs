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
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static ShaderDependencyInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();
        [NativeMethod(IsThreadSafe = true)] internal extern string GetDependencyName(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern string GetShaderName(ShaderContainer container);

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

        internal ShaderDependency(ShaderContainer container, string dependencyName, string shaderName)
            : this(container, dependencyName, shaderName, Location.Invalid)
        {
        }

        internal ShaderDependency(ShaderContainer container, string dependencyName, string shaderName, Location location)
            : this(container, container.AddString(dependencyName), container.AddString(shaderName), location)
        {
        }

        internal ShaderDependency(ShaderContainer container, FoundryHandle dependencyName, FoundryHandle shaderName, Location location)
        {
            if ((container == null) || (!dependencyName.IsValid) || (!shaderName.IsValid))
            {
                this = Invalid;
            }
            else
            {
                shaderDependency.m_DependencyNameStringHandle = dependencyName;
                shaderDependency.m_ShaderNameStringHandle = shaderName;
                shaderDependency.m_LocationHandle = location.handle;
                handle = container.Add(shaderDependency);
                this.container = handle.IsValid ? container : null;
            }
        }

        public string DependencyName => shaderDependency.GetDependencyName(container);
        public string ShaderName => shaderDependency.GetShaderName(container);
        public Location Location => new Location(container, shaderDependency.m_LocationHandle);

        internal ShaderDependency(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out shaderDependency);
        }

        public static ShaderDependency Invalid => new ShaderDependency(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
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
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string dependencyName, string shaderName)
            {
                this.container = container;
                this.dependencyName = dependencyName;
                this.shaderName = shaderName;
            }

            public ShaderDependency Build()
            {
                return new ShaderDependency(container, dependencyName, shaderName, location);
            }
        }
    }
}
