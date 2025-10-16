// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/PrimitiveLiteral.h")]
    internal struct IntegerLiteralInternal : IInternalType<IntegerLiteralInternal>
    {
        internal int m_Value;
        private bool m_IsValid;

        internal static extern IntegerLiteralInternal Invalid();
        internal extern bool IsValid();
        internal extern int GetValue();
        internal extern void SetValue(int value);

        // IInternalType
        IntegerLiteralInternal IInternalType<IntegerLiteralInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct IntegerLiteral : IEquatable<IntegerLiteral>, IPublicType<IntegerLiteral>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly IntegerLiteralInternal integer;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        IntegerLiteral IPublicType<IntegerLiteral>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new IntegerLiteral(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && integer.IsValid());
        public int Value => integer.GetValue();

        // private
        internal IntegerLiteral(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out integer);
        }

        public static IntegerLiteral Invalid => new IntegerLiteral(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is IntegerLiteral other && this.Equals(other);
        public bool Equals(IntegerLiteral other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(IntegerLiteral lhs, IntegerLiteral rhs) => lhs.Equals(rhs);
        public static bool operator!=(IntegerLiteral lhs, IntegerLiteral rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            int? setValue = null;

            public int Value
            {
                get { return setValue ?? 0; }
                set { setValue = value; }
            }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
            }

            public IntegerLiteral Build()
            {
                var integerInternal = new IntegerLiteralInternal();
                if (setValue != null)
                    integerInternal.SetValue(setValue.Value);

                var returnTypeHandle = container.Add(integerInternal);
                return new IntegerLiteral(container, returnTypeHandle);
            }
        }
    }
}
