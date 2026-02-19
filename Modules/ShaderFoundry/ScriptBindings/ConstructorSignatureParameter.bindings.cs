// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ConstructorSignatureParameter.h")]
    internal struct ConstructorSignatureParameterInternal : IInternalType<ConstructorSignatureParameterInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal DataType m_Type;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static ConstructorSignatureParameterInternal Invalid();
        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }
        
        // IInternalType
        ConstructorSignatureParameterInternal IInternalType<ConstructorSignatureParameterInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ConstructorSignatureParameter : IEquatable<ConstructorSignatureParameter>, IPublicType<ConstructorSignatureParameter>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly ConstructorSignatureParameterInternal param;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ConstructorSignatureParameter IPublicType<ConstructorSignatureParameter>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ConstructorSignatureParameter(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => param.IsValid;
        public string Name => container?.GetString(param.m_NameHandle) ?? string.Empty;
        public Location Location => new Location(container, param.m_LocationHandle);

        public bool IsBool => param.m_Type == DataType.Boolean;
        public bool IsInt  => param.m_Type == DataType.Integer;
        public bool IsFloat => param.m_Type == DataType.Float;
        public bool IsString => param.m_Type == DataType.String;
        
        // private
        internal ConstructorSignatureParameter(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out param);
        }

        public static ConstructorSignatureParameter Invalid => new ConstructorSignatureParameter(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is ConstructorSignatureParameter other && this.Equals(other);
        public bool Equals(ConstructorSignatureParameter other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ConstructorSignatureParameter lhs, ConstructorSignatureParameter rhs) => lhs.Equals(rhs);
        public static bool operator!=(ConstructorSignatureParameter lhs, ConstructorSignatureParameter rhs) => !lhs.Equals(rhs);
    }
}
