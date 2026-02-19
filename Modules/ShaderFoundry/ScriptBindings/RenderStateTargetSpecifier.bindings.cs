// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/RenderStateTargetSpecifier.h")]
    internal struct RenderStateTargetSpecifierInternal : IInternalType<RenderStateTargetSpecifierInternal>
    {
        // TypedFoundryHandle<string, IntegerLiteral>
        internal FoundryHandle m_IndexHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static RenderStateTargetSpecifierInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        RenderStateTargetSpecifierInternal IInternalType<RenderStateTargetSpecifierInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct RenderStateTargetSpecifier : IEquatable<RenderStateTargetSpecifier>, IPublicType<RenderStateTargetSpecifier>
    {
        // data members
        readonly ShaderContainer container;
        readonly RenderStateTargetSpecifierInternal symbol;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        RenderStateTargetSpecifier IPublicType<RenderStateTargetSpecifier>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle)
            => new RenderStateTargetSpecifier(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && symbol.IsValid());
        internal IPublicType Index => container?.ConstructTypeFromHandle(symbol.m_IndexHandle);
        internal Location Location => new Location(container, symbol.m_LocationHandle);

        // private
        internal RenderStateTargetSpecifier(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out symbol);
        }

        public static RenderStateTargetSpecifier Invalid => new RenderStateTargetSpecifier(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is RenderStateTargetSpecifier other && this.Equals(other);
        public bool Equals(RenderStateTargetSpecifier other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(RenderStateTargetSpecifier lhs, RenderStateTargetSpecifier rhs) => lhs.Equals(rhs);
        public static bool operator!=(RenderStateTargetSpecifier lhs, RenderStateTargetSpecifier rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            internal IPublicType index { get; private set; }
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string index)
            {
                this.container = container;
                this.index = new StringLiteral(container, container.AddString(index));
            }

            public Builder(ShaderContainer container, int index)
            {
                this.container = container;
                this.index = new IntegerLiteral.Builder(container) { Value = index }.Build();
            }

            public RenderStateTargetSpecifier Build()
            {
                var symbol = new RenderStateTargetSpecifierInternal();
                symbol.m_IndexHandle = index.Handle;
                symbol.m_LocationHandle = location.handle;
                var resultHandle = container.Add(symbol);
                return new RenderStateTargetSpecifier(container, resultHandle);
            }
        }
    }
}
