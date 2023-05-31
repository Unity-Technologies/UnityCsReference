// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/PackageRequirement.h")]
    internal struct PackageRequirementInternal : IInternalType<PackageRequirementInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_VersionHandle;

        internal static extern PackageRequirementInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        PackageRequirementInternal IInternalType<PackageRequirementInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct PackageRequirement : IEquatable<PackageRequirement>, IPublicType<PackageRequirement>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly PackageRequirementInternal packageRequirement;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        PackageRequirement IPublicType<PackageRequirement>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new PackageRequirement(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null) && handle.IsValid && (packageRequirement.IsValid());
        public string Name => container?.GetString(packageRequirement.m_NameHandle) ?? string.Empty;
        public string Version => container?.GetString(packageRequirement.m_VersionHandle) ?? string.Empty;

        // private
        internal PackageRequirement(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out packageRequirement);
        }

        public static PackageRequirement Invalid => new PackageRequirement(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is PackageRequirement other && this.Equals(other);
        public bool Equals(PackageRequirement other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(PackageRequirement lhs, PackageRequirement rhs) => lhs.Equals(rhs);
        public static bool operator!=(PackageRequirement lhs, PackageRequirement rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            internal ShaderContainer container;
            internal string name;
            internal string version;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.version = null;
            }

            public Builder(ShaderContainer container, string name, string version)
            {
                this.container = container;
                this.name = name;
                this.version = version;
            }

            public PackageRequirement Build()
            {
                var packageRequirementInternal = new PackageRequirementInternal();
                packageRequirementInternal.m_NameHandle = container.AddString(name);
                packageRequirementInternal.m_VersionHandle = container.AddString(version);
                var returnHandle = container.Add(packageRequirementInternal);
                return new PackageRequirement(container, returnHandle);
            }
        }
    }
}
