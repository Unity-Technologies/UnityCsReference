// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ConstructorSignature.h")]
    internal struct ConstructorSignatureInternal : IInternalType<ConstructorSignatureInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ParameterListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static ConstructorSignatureInternal Invalid();
        internal extern bool IsValid { [NativeMethod(Name = "IsValid", IsThreadSafe = true)] get; }

        // IInternalType
        ConstructorSignatureInternal IInternalType<ConstructorSignatureInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ConstructorSignature : IEquatable<ConstructorSignature>, IPublicType<ConstructorSignature>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ConstructorSignatureInternal signature;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ConstructorSignature IPublicType<ConstructorSignature>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ConstructorSignature(container, handle);

        // public API
        public ShaderContainer Container => container;
        public static ConstructorSignature Invalid => new ConstructorSignature(null, FoundryHandle.Invalid());

        public bool Exists => (container != null) && handle.IsValid;
        public bool IsValid => Exists && signature.IsValid;
        public string Name => container?.GetString(signature.m_NameHandle) ?? string.Empty;
        public IEnumerable<ConstructorSignatureParameter> Parameters =>
            ListType.Enumerate<ConstructorSignatureParameter>(container, signature.m_ParameterListHandle);

        public Location Location => new Location(container, signature.m_LocationHandle);

        internal ConstructorSignature(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out signature);
        }

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is ConstructorSignature other && this.Equals(other);
        public bool Equals(ConstructorSignature other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ConstructorSignature lhs, ConstructorSignature rhs) => lhs.Equals(rhs);
        public static bool operator!=(ConstructorSignature lhs, ConstructorSignature rhs) => !lhs.Equals(rhs);
    }
}
