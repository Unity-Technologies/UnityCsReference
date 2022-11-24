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
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderAttributeParam.h")]
    internal struct ShaderAttributeParamInternal : IInternalType<ShaderAttributeParamInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ValueHandle;

        internal extern static ShaderAttributeParamInternal Invalid();

        internal extern void Setup(ShaderContainer container, string name, string value);

        internal extern bool IsValid();
        internal extern string GetName(ShaderContainer container);
        internal extern string GetValue(ShaderContainer container);

        internal extern bool ValueIsString(ShaderContainer container);
        internal extern bool ValueIsArray(ShaderContainer container);

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);

        // IInternalType
        ShaderAttributeParamInternal IInternalType<ShaderAttributeParamInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderAttributeParam : IEquatable<ShaderAttributeParam>, IPublicType<ShaderAttributeParam>
    {
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly ShaderAttributeParamInternal param;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderAttributeParam IPublicType<ShaderAttributeParam>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderAttributeParam(container, handle);

        internal ShaderAttributeParam(ShaderContainer container, FoundryHandle handle)
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
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParam.Value'. Value is not a string. Check ValueIsString before calling.");
                return container.GetString(param.m_ValueHandle);
            }
        }
        public IEnumerable<ShaderAttributeParam> Values
        {
            get
            {
                if (container == null || !ValueIsArray)
                    throw new InvalidOperationException("Invalid call to 'ShaderAttributeParam.Values'. Values is not a List. Check ValueIsArray before calling.");

                var handleType = container.GetDataTypeFromHandle(param.m_ValueHandle);
                var localContainer = Container;
                var list = new FixedHandleListInternal(param.m_ValueHandle);
                return list.Select<ShaderAttributeParam>(localContainer, (handle) => (new ShaderAttributeParam(localContainer, handle)));
            }
        }
        public bool ValueIsString => param.ValueIsString(container);
        // The value is an array of sub attributes. This is equivalent to "param = [value, value]".
        public bool ValueIsArray => param.ValueIsArray(container);
        public static ShaderAttributeParam Invalid => new ShaderAttributeParam(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderAttributeParam other && this.Equals(other);
        public bool Equals(ShaderAttributeParam other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderAttributeParam lhs, ShaderAttributeParam rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderAttributeParam lhs, ShaderAttributeParam rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in ShaderAttributeParam other)
        {
            return ShaderAttributeParamInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        public class Builder
        {
            ShaderContainer container;
            internal string m_Name;
            internal string m_Value;
            internal List<ShaderAttributeParam> m_Values;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name, string value)
            {
                this.container = container;
                m_Name = name;
                m_Value = value;
                m_Values = null;
            }

            public Builder(ShaderContainer container, string name, List<ShaderAttributeParam> values)
            {
                this.container = container;
                m_Name = name;
                m_Value = null;
                m_Values = values;
            }

            public ShaderAttributeParam Build()
            {
                var paramInternal = new ShaderAttributeParamInternal();
                if (m_Values == null)
                    paramInternal.Setup(container, m_Name, m_Value);
                else
                {
                    paramInternal.m_NameHandle = container.AddString(m_Name);
                    paramInternal.m_ValueHandle = FixedHandleListInternal.Build(container, m_Values, (v) => v.handle);
                }

                var returnHandle = container.Add(paramInternal);
                return new ShaderAttributeParam(container, returnHandle);
            }
        }
    }

    [NativeHeader("Modules/ShaderFoundry/Public/ShaderAttribute.h")]
    internal struct ShaderAttributeInternal : IInternalType<ShaderAttributeInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_ParameterListHandle;

        internal extern static ShaderAttributeInternal Invalid();

        internal extern void Setup(ShaderContainer container, string name, FoundryHandle parameterListHandle);

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

        public IEnumerable<ShaderAttributeParam> Parameters
        {
            get
            {
                var localContainer = Container;
                var list = new FixedHandleListInternal(attr.m_ParameterListHandle);
                return list.Select<ShaderAttributeParam>(localContainer, (handle) => (new ShaderAttributeParam(localContainer, handle)));
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
            internal List<ShaderAttributeParam> parameters;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.name = name;
                this.parameters = null;
            }

            public Builder Param(ShaderAttributeParam attribute)
            {
                if (parameters == null)
                    parameters = new List<ShaderAttributeParam>();
                parameters.Add(attribute);
                return this;
            }

            public Builder Param(string value)
            {
                return Param(null, value);
            }

            public Builder Param(string name, string value)
            {
                var paramBuilder = new ShaderAttributeParam.Builder(container, name, value);
                return Param(paramBuilder.Build());
            }

            public ShaderAttribute Build()
            {
                var paramListHandle = FixedHandleListInternal.Build(container, parameters, (p) => (p.handle));
                var attributeInternal = new ShaderAttributeInternal();
                attributeInternal.Setup(container, name, paramListHandle);

                var returnHandle = container.Add(attributeInternal);
                return new ShaderAttribute(container, returnHandle);
            }
        }
    }
}
