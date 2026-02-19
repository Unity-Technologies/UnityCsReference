// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/RenderStateNamedValue.h")]
    internal struct RenderStateNamedValueInternal : IInternalType<RenderStateNamedValueInternal>
    {
        internal FoundryHandle m_NameHandle;
        // TypedFoundryHandle<string, IntegerLiteral, RenderStateProperty>
        internal FoundryHandle m_ValueHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static RenderStateNamedValueInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        RenderStateNamedValueInternal IInternalType<RenderStateNamedValueInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct RenderStateNamedValue : IEquatable<RenderStateNamedValue>, IPublicType<RenderStateNamedValue>
    {
        // data members
        readonly ShaderContainer container;
        readonly RenderStateNamedValueInternal symbol;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        RenderStateNamedValue IPublicType<RenderStateNamedValue>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle)
            => new RenderStateNamedValue(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && symbol.IsValid());

        public string Name => container?.GetString(symbol.m_NameHandle) ?? string.Empty;
        public IPublicType Value => container?.ConstructTypeFromHandle(symbol.m_ValueHandle);
        public Location Location => new Location(container, symbol.m_LocationHandle);

        // private
        internal RenderStateNamedValue(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out symbol);
        }

        public static RenderStateNamedValue Invalid => new RenderStateNamedValue(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is RenderStateNamedValue other && this.Equals(other);
        public bool Equals(RenderStateNamedValue other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(RenderStateNamedValue lhs, RenderStateNamedValue rhs) => lhs.Equals(rhs);
        public static bool operator!=(RenderStateNamedValue lhs, RenderStateNamedValue rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public string name;
            internal IPublicType value { get; private set; }
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, string value)
            {
                this.container = container;
                this.name = name;
                this.value = new StringLiteral(container, container.AddString(value));
            }

            public Builder(ShaderContainer container, string name, int value)
            {
                this.container = container;
                this.name = name;
                this.value = new IntegerLiteral.Builder(container) { Value = value }.Build();
            }

            public Builder(ShaderContainer container, string name, RenderStateProperty value)
            {
                this.container = container;
                this.name = name;
                this.value = value;
            }

            public RenderStateNamedValue Build()
            {
                var symbol = new RenderStateNamedValueInternal();
                symbol.m_NameHandle = container.AddString(name);
                symbol.m_ValueHandle = value.Handle;
                symbol.m_LocationHandle = location.handle;
                var resultHandle = container.Add(symbol);
                return new RenderStateNamedValue(container, resultHandle);
            }
        }
    }
}
