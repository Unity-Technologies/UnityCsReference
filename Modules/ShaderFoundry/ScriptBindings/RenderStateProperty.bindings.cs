// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/RenderStateProperty.h")]
    internal struct RenderStatePropertyInternal : IInternalType<RenderStatePropertyInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static RenderStatePropertyInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        RenderStatePropertyInternal IInternalType<RenderStatePropertyInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct RenderStateProperty : IEquatable<RenderStateProperty>, IPublicType<RenderStateProperty>
    {
        // data members
        readonly ShaderContainer container;
        readonly RenderStatePropertyInternal symbol;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        RenderStateProperty IPublicType<RenderStateProperty>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle)
            => new RenderStateProperty(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && symbol.IsValid());

        public string Name => container?.GetString(symbol.m_NameHandle) ?? string.Empty;
        public Location Location => new Location(container, symbol.m_LocationHandle);

        // private
        internal RenderStateProperty(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out symbol);
        }

        public static RenderStateProperty Invalid => new RenderStateProperty(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is RenderStateProperty other && this.Equals(other);
        public bool Equals(RenderStateProperty other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(RenderStateProperty lhs, RenderStateProperty rhs) => lhs.Equals(rhs);
        public static bool operator!=(RenderStateProperty lhs, RenderStateProperty rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public string name;
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
            }

            public RenderStateProperty Build()
            {
                var symbol = new RenderStatePropertyInternal();
                symbol.m_NameHandle = container.AddString(name);
                symbol.m_LocationHandle = location.handle;
                var resultHandle = container.Add(symbol);
                return new RenderStateProperty(container, resultHandle);
            }
        }
    }
}
