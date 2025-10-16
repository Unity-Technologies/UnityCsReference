// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShaderFoundry
{
    // The internal types are not actually used but is needed for how DataTypeStatic works.
    internal struct StringLiteralInternal : IInternalType<StringLiteralInternal>
    {
        internal FoundryHandle handle;

        StringLiteralInternal IInternalType<StringLiteralInternal>.ConstructInvalid() => new StringLiteralInternal();
    }
    internal struct EmptyStringInternal : IInternalType<EmptyStringInternal>
    {
        internal FoundryHandle handle;

        EmptyStringInternal IInternalType<EmptyStringInternal>.ConstructInvalid() => new EmptyStringInternal();
    }

    [FoundryAPI]
    // StringLiteral is a special object to deal with polymorphic scenarios of string.
    // This type is only used for retrieving data and is not valid for building strings.
    internal readonly struct StringLiteral : IEquatable<StringLiteral>,  IPublicType<StringLiteral>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle; // StringHandle or EmptyStringHandle

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        StringLiteral IPublicType<StringLiteral>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new StringLiteral(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public string Value => GetValue();

        private string GetValue()
        {
            if (container == null)
                return string.Empty;
            else if (container.GetDataTypeFromHandle(handle) == DataType.EmptyString)
                return string.Empty;
            else
                return container.GetString(handle);
        }

        // private
        internal StringLiteral(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
        }

        public static StringLiteral Invalid => new StringLiteral(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is StringLiteral other && this.Equals(other);
        public bool Equals(StringLiteral other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(StringLiteral lhs, StringLiteral rhs) => lhs.Equals(rhs);
        public static bool operator!=(StringLiteral lhs, StringLiteral rhs) => !lhs.Equals(rhs);
    }
}
