// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderAttributeParameter.h")]
    internal struct ShaderAttributeParameterInternal : IInternalType<ShaderAttributeParameterInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ValueHandle;

        internal extern static ShaderAttributeParameterInternal Invalid();

        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);
        internal extern string GetValue(ShaderContainer container);

        internal extern bool ValueIsString(ShaderContainer container);
        internal extern bool ValueIsArray(ShaderContainer container);

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);

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
        public string Value
        {
            get
            {
                if (container == null || !ValueIsString)
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParameter.Value'. Value is not a string. Check ValueIsString before calling.");
                return param.GetValue(container);
            }
        }
        public IEnumerable<ShaderAttributeParameter> Values
        {
            get
            {
                if (container == null || !ValueIsArray)
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParameter.Values'. Values is not a List. Check ValueIsArray before calling.");

                var handleType = container.GetDataTypeFromHandle(param.m_ValueHandle);
                var localContainer = Container;
                var list = new FixedHandleListInternal(param.m_ValueHandle);
                return list.Select<ShaderAttributeParameter>(localContainer, (handle) => (new ShaderAttributeParameter(localContainer, handle)));
            }
        }
        public bool ValueIsString => param.ValueIsString(container);
        // The value is an array of sub attributes. This is equivalent to "param = [value, value]".
        public bool ValueIsArray => param.ValueIsArray(container);
        public static ShaderAttributeParameter Invalid => new ShaderAttributeParameter(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderAttributeParameter other && this.Equals(other);
        public bool Equals(ShaderAttributeParameter other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderAttributeParameter lhs, ShaderAttributeParameter rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderAttributeParameter lhs, ShaderAttributeParameter rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in ShaderAttributeParameter other)
        {
            return ShaderAttributeParameterInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        public class Builder
        {
            ShaderContainer container;
            internal string m_Name;
            internal string m_Value;
            internal List<ShaderAttributeParameter> m_Values;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, string value)
            {
                this.container = container;
                m_Name = name;
                m_Value = value;
                m_Values = null;
            }

            public Builder(ShaderContainer container, string name, List<ShaderAttributeParameter> values)
            {
                this.container = container;
                m_Name = name;
                m_Value = null;
                m_Values = values;
            }

            public ShaderAttributeParameter Build()
            {
                var paramInternal = new ShaderAttributeParameterInternal();
                paramInternal.m_NameHandle = container.AddString(m_Name);
                if (m_Values == null)
                    paramInternal.m_ValueHandle = container.AddString(m_Value);
                else
                    paramInternal.m_ValueHandle = FixedHandleListInternal.Build(container, m_Values, (v) => v.handle);

                var returnHandle = container.Add(paramInternal);
                return new ShaderAttributeParameter(container, returnHandle);
            }
        }
    }

    [NativeHeader("Modules/ShaderFoundry/Public/ShaderAttribute.h")]
    internal struct ShaderAttributeInternal : IInternalType<ShaderAttributeInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ParameterListHandle;

        internal extern static ShaderAttributeInternal Invalid();

        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);

        // IInternalType
        ShaderAttributeInternal IInternalType<ShaderAttributeInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderAttribute : IEquatable<ShaderAttribute>, IPublicType<ShaderAttribute>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ShaderAttributeInternal attr;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderAttribute IPublicType<ShaderAttribute>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderAttribute(container, handle);

        // public API
        public ShaderContainer Container => container;
        public static ShaderAttribute Invalid => new ShaderAttribute(null, FoundryHandle.Invalid());
        public bool IsValid => (container != null) && handle.IsValid && (attr.IsValid());
        public string Name => attr.GetName(container);

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderAttribute other && this.Equals(other);
        public bool Equals(ShaderAttribute other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderAttribute lhs, ShaderAttribute rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderAttribute lhs, ShaderAttribute rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in ShaderAttribute other)
        {
            return ShaderAttributeInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        public IEnumerable<ShaderAttributeParameter> Parameters
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(attr.m_ParameterListHandle);
                return list.Select<ShaderAttributeParameter>(localContainer, (handle) => (new ShaderAttributeParameter(localContainer, handle)));
            }
        }

        internal ShaderAttribute(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = (container ? handle : FoundryHandle.Invalid());
            ShaderContainer.Get(container, handle, out attr);
        }

        public class Builder
        {
            ShaderContainer container;
            internal string name;
            internal List<ShaderAttributeParameter> parameters;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.parameters = null;
            }

            public Builder Parameter(ShaderAttributeParameter attribute)
            {
                if (parameters == null)
                    parameters = new List<ShaderAttributeParameter>();
                parameters.Add(attribute);
                return this;
            }

            public Builder Parameter(string value)
            {
                return Parameter(null, value);
            }

            public Builder Parameter(string name, string value)
            {
                var paramBuilder = new ShaderAttributeParameter.Builder(container, name, value);
                return Parameter(paramBuilder.Build());
            }

            public ShaderAttribute Build()
            {
                var paramListHandle = FixedHandleListInternal.Build(container, parameters, (p) => (p.handle));
                var attributeInternal = new ShaderAttributeInternal();
                attributeInternal.m_NameHandle = container.AddString(name); 
                attributeInternal.m_ParameterListHandle = paramListHandle; 

                var returnHandle = container.Add(attributeInternal);
                return new ShaderAttribute(container, returnHandle);
            }
        }
    }
}
