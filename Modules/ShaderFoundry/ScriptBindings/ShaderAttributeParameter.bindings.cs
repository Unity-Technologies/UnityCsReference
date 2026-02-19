// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderAttributeParameter.h")]
    internal struct ShaderAttributeParameterInternal : IInternalType<ShaderAttributeParameterInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ValueHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static ShaderAttributeParameterInternal Invalid();

        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();
        [NativeMethod(IsThreadSafe = true)] internal extern string GetName(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern ShaderTypeInternal GetShaderTypeValue(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern string GetStringValue(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern int GetIntegerValue(ShaderContainer container);

        [NativeMethod(IsThreadSafe = true)] internal extern bool ValueIsInteger(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern bool ValueIsShaderType(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern bool ValueIsString(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern bool ValueIsArray(ShaderContainer container);

        // IInternalType
        ShaderAttributeParameterInternal IInternalType<ShaderAttributeParameterInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderAttributeParameter : IEquatable<ShaderAttributeParameter>, IPublicType<ShaderAttributeParameter>
    {
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ShaderAttributeParameterInternal param;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderAttributeParameter IPublicType<ShaderAttributeParameter>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderAttributeParameter(container, handle);

        internal ShaderAttributeParameter(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out param);
        }

        public ShaderContainer Container => container;
        public bool IsValid => (container != null) && (param.IsValid());
        public string Name => param.GetName(container);

        public IPublicType Value => container?.ConstructTypeFromHandle(param.m_ValueHandle);
        public Location Location => new Location(container, param.m_LocationHandle);

        [Obsolete("Use '(Value as IntegerLiteral).Value' instead")]
        public int IntegerValue
        {
            get
            {
                if (container == null || !ValueIsInteger)
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParameter.IntegerValue'. Value is not an integer. Check ValueIsInteger before calling.");
                return param.GetIntegerValue(container);
            }
        }
        [Obsolete("Use '(Value as ShaderType)' instead")]
        public ShaderType ShaderTypeValue
        {
            get
            {
                if (container == null || !ValueIsShaderType)
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParameter.ShaderTypeValue'.  Value is not a ShaderType.  Check ValueIsShaderType before calling.");
                return new ShaderType(container, param.m_ValueHandle);
            }
        }
        [Obsolete("Use '(Value as StringLiteral).Value' instead")]
        public string StringValue
        {
            get
            {
                if (container == null || !ValueIsString)
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParameter.StringValue'. Value is not a string. Check ValueIsString before calling.");
                return param.GetStringValue(container);
            }
        }
        [Obsolete("Use '(Value as ListType).Values' instead")]
        public IEnumerable<ShaderAttributeParameter> Values
        {
            get
            {
                var localContainer = Container;
                if (localContainer == null || !ValueIsArray)
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParameter.Values'. Values is not a List. Check ValueIsArray before calling.");
                return ListType.Enumerate<ShaderAttributeParameter>(localContainer, param.m_ValueHandle);
            }
        }
        [Obsolete("Use '(Value is IntegerLiteral)' instead")]
        public bool ValueIsInteger => param.ValueIsInteger(container);
        [Obsolete("Use '(Value is ShaderType)' instead")]
        public bool ValueIsShaderType => param.ValueIsShaderType(container);
        [Obsolete("Use '(Value is StringLiteral)' instead")]
        public bool ValueIsString => param.ValueIsString(container);
        // The value is an array of sub attributes. This is equivalent to "param = [value, value]".
        [Obsolete("Use '(Value is ListType)' instead")]
        public bool ValueIsArray => param.ValueIsArray(container);
        public static ShaderAttributeParameter Invalid => new ShaderAttributeParameter(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is ShaderAttributeParameter other && this.Equals(other);
        public bool Equals(ShaderAttributeParameter other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderAttributeParameter lhs, ShaderAttributeParameter rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderAttributeParameter lhs, ShaderAttributeParameter rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            internal string m_Name;
            internal IPublicType Value { get; private set; }
            public Location location;

            public ShaderContainer Container => container;

            private static IntegerLiteral BuildInteger(ShaderContainer container, int value)
            {
                var builder = new IntegerLiteral.Builder(container);
                builder.Value = value;
                return builder.Build();
            }

            Builder(ShaderContainer container, string name, IPublicType value)
            {
                this.container = container;
                m_Name = name;
                Value = value;
            }

            public Builder(ShaderContainer container, string name, int value)
                : this(container, name, BuildInteger(container, value))
            {}

            public Builder(ShaderContainer container, string name, BooleanLiteral value)
                : this(container, name, value as IPublicType)
            {
            }

            public Builder(ShaderContainer container, string name, IntegerLiteral value)
                : this(container, name, value as IPublicType)
            {
            }

            public Builder(ShaderContainer container, string name, FloatLiteral value)
                : this(container, name, value as IPublicType)
            {
            }

            public Builder(ShaderContainer container, string name, ShaderType value)
                : this(container, name, value as IPublicType)
            {
            }

            public Builder(ShaderContainer container, string name, StringLiteral value)
                : this(container, name, value as IPublicType)
            {
            }

            public Builder(ShaderContainer container, string name, EnumLiteral value)
                : this(container, name, value as IPublicType)
            {
            }

            public Builder(ShaderContainer container, string name, List<ShaderAttributeParameter> values)
                : this(container, name, ListType.Construct(container, values))
            {
            }

            public ShaderAttributeParameter Build()
            {
                var paramInternal = new ShaderAttributeParameterInternal();
                paramInternal.m_NameHandle = container.AddString(m_Name);
                paramInternal.m_ValueHandle = Value.Handle;
                paramInternal.m_LocationHandle = location.handle;

                var returnHandle = container.Add(paramInternal);
                return new ShaderAttributeParameter(container, returnHandle);
            }
        }
    }
}
