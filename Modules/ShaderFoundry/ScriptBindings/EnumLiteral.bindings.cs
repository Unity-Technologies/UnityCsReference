// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/EnumLiteral.h")]
    internal struct EnumLiteralInternal : IInternalType<EnumLiteralInternal>
    {
        internal FoundryHandle m_EnumTypeHandle;
        internal FoundryHandle m_NameHandle;
        internal int m_Value;

        [NativeMethod(IsThreadSafe = true)] internal static extern EnumLiteralInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        EnumLiteralInternal IInternalType<EnumLiteralInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct EnumLiteral : IEquatable<EnumLiteral>, IPublicType<EnumLiteral>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly EnumLiteralInternal symbol;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        EnumLiteral IPublicType<EnumLiteral>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new EnumLiteral(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && symbol.IsValid());
        public ShaderType EnumType => new ShaderType(container, symbol.m_EnumTypeHandle);
        public string Name => container?.GetString(symbol.m_NameHandle) ?? string.Empty;
        public int Value => symbol.m_Value;

        // private
        internal EnumLiteral(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out symbol);
        }

        public static EnumLiteral Invalid => new EnumLiteral(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is EnumLiteral other && this.Equals(other);
        public bool Equals(EnumLiteral other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(EnumLiteral lhs, EnumLiteral rhs) => lhs.Equals(rhs);
        public static bool operator!=(EnumLiteral lhs, EnumLiteral rhs) => !lhs.Equals(rhs);

        // There is no builder for an enum literal as they cannot be built independently of the enum type.
    }
}
