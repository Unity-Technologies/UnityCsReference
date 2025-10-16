// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/PrimitiveLiteral.h")]
    internal struct BooleanLiteralInternal : IInternalType<BooleanLiteralInternal>
    {
        internal bool m_Value;
        private bool m_IsValid;

        internal static extern BooleanLiteralInternal Invalid();
        internal extern bool IsValid();
        internal extern bool GetValue();
        internal extern void SetValue(bool value);

        // IInternalType
        BooleanLiteralInternal IInternalType<BooleanLiteralInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct BooleanLiteral : IEquatable<BooleanLiteral>, IPublicType<BooleanLiteral>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BooleanLiteralInternal value;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        BooleanLiteral IPublicType<BooleanLiteral>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new BooleanLiteral(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && value.IsValid());
        public bool Value => value.GetValue();

        // private
        internal BooleanLiteral(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out value);
        }

        public static BooleanLiteral Invalid => new BooleanLiteral(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is BooleanLiteral other && this.Equals(other);
        public bool Equals(BooleanLiteral other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BooleanLiteral lhs, BooleanLiteral rhs) => lhs.Equals(rhs);
        public static bool operator!=(BooleanLiteral lhs, BooleanLiteral rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            bool? setValue = null;

            public bool Value
            {
                get { return setValue ?? false; }
                set { setValue = value; }
            }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
            }

            public BooleanLiteral Build()
            {
                var symbol = new BooleanLiteralInternal();
                if (setValue != null)
                    symbol.SetValue(setValue.Value);

                var returnTypeHandle = container.Add(symbol);
                return new BooleanLiteral(container, returnTypeHandle);
            }
        }
    }
}
