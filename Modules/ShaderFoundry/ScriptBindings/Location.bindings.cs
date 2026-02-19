// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/Location.h")]
    internal struct LocationInternal : IInternalType<LocationInternal>
    {
        internal FoundryHandle m_FilenameHandle;
        internal TextRange m_TextRange;

        [NativeMethod(IsThreadSafe = true)] internal static extern LocationInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        LocationInternal IInternalType<LocationInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    readonly struct Location : IEquatable<Location>, IPublicType<Location>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly LocationInternal location;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        Location IPublicType<Location>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new Location(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && location.IsValid());
        public string Filename => container?.GetString(location.m_FilenameHandle) ?? string.Empty;
        public TextRange Range => location.m_TextRange;

        // private
        internal Location(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out location);
        }

        public static Location Invalid => new Location(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is Location other && this.Equals(other);
        public bool Equals(Location other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(Location lhs, Location rhs) => lhs.Equals(rhs);
        public static bool operator!=(Location lhs, Location rhs) => !lhs.Equals(rhs);
    }
}
