// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/PrimitiveLiteral.h")]
    internal struct FloatLiteralInternal : IInternalType<FloatLiteralInternal>
    {
        internal float m_Value;
        private bool m_IsValid;

        internal static extern FloatLiteralInternal Invalid();
        internal extern bool IsValid();
        internal extern float GetValue();
        internal extern void SetValue(float value);

        // IInternalType
        FloatLiteralInternal IInternalType<FloatLiteralInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct FloatLiteral : IEquatable<FloatLiteral>, IPublicType<FloatLiteral>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly FloatLiteralInternal value;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        FloatLiteral IPublicType<FloatLiteral>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new FloatLiteral(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && value.IsValid());
        public float Value => value.GetValue();

        // private
        internal FloatLiteral(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out value);
        }

        public static FloatLiteral Invalid => new FloatLiteral(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is FloatLiteral other && this.Equals(other);
        public bool Equals(FloatLiteral other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(FloatLiteral lhs, FloatLiteral rhs) => lhs.Equals(rhs);
        public static bool operator!=(FloatLiteral lhs, FloatLiteral rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            float? setValue = null;

            public float Value
            {
                get { return setValue ?? 0.0f; }
                set { setValue = value; }
            }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container)
            {
                this.container = container;
            }

            public FloatLiteral Build()
            {
                var floatInternal = new FloatLiteralInternal();
                if (setValue != null)
                    floatInternal.SetValue(setValue.Value);

                var returnTypeHandle = container.Add(floatInternal);
                return new FloatLiteral(container, returnTypeHandle);
            }
        }
    }
}
