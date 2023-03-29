// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/StructField.h")]
    internal struct StructFieldInternal : IInternalType<StructFieldInternal>
    {
        // This enum must be kept in sync with the enum in StructField.h
        internal enum Flags : ushort
        {
            kNone = 0,
            kInput = 1 << 0,
            kOutput = 1 << 1
        }

        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_TypeHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal Flags m_Flags;

        internal static extern StructFieldInternal Invalid();
        internal extern bool IsValid { [NativeMethod("IsValid")] get; }
        internal extern string GetName(ShaderContainer container);

        internal IEnumerable<ShaderAttribute> Attributes(ShaderContainer container)
        {
            var list = new HandleListInternal(m_AttributeListHandle);
            return list.Select<ShaderAttribute>(container, (handle) => (new ShaderAttribute(container, handle)));
        }

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);

        // IInternalType
        StructFieldInternal IInternalType<StructFieldInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct StructField : IEquatable<StructField>, IPublicType<StructField>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly StructFieldInternal field;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        StructField IPublicType<StructField>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new StructField(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null) && handle.IsValid && (field.IsValid);
        public string Name => field.GetName(container);
        public ShaderType Type => new ShaderType(container, field.m_TypeHandle);
        public IEnumerable<ShaderAttribute> Attributes => field.Attributes(container);
        public bool IsInput => field.m_Flags.HasFlag(StructFieldInternal.Flags.kInput);
        public bool IsOutput => field.m_Flags.HasFlag(StructFieldInternal.Flags.kOutput);

        // private
        internal StructField(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out field);
        }

        public static StructField Invalid => new StructField(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is StructField other && this.Equals(other);
        public bool Equals(StructField other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(StructField lhs, StructField rhs) => lhs.Equals(rhs);
        public static bool operator!=(StructField lhs, StructField rhs) => !lhs.Equals(rhs);

        public bool ValueEquals(in StructField other)
        {
            return StructFieldInternal.ValueEquals(container, handle, other.container, other.handle);
        }

        public class Builder
        {
            internal ShaderContainer container;
            internal string name;
            internal ShaderType type = ShaderType.Invalid;
            internal List<ShaderAttribute> attributes;
            internal StructFieldInternal.Flags m_Flags = StructFieldInternal.Flags.kNone;

            public Builder(ShaderContainer container, string name, ShaderType type)
            {
                this.container = container;
                this.name = name;
                this.type = type;
            }
            public Builder(ShaderContainer container, string name, ShaderType type, bool isInput, bool isOutput)
            {
                this.container = container;
                this.name = name;
                this.type = type;
                IsInput = isInput;
                IsOutput = isOutput;
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                if (attributes == null)
                    attributes = new List<ShaderAttribute>();
                attributes.Add(attribute);
            }
            public bool IsInput
            {
                get { return m_Flags.HasFlag(StructFieldInternal.Flags.kInput); }
                set { SetFlag(StructFieldInternal.Flags.kInput, value); }
            }
            public bool IsOutput
            {
                get { return m_Flags.HasFlag(StructFieldInternal.Flags.kOutput); }
                set { SetFlag(StructFieldInternal.Flags.kOutput, value); }
            }
            void SetFlag(StructFieldInternal.Flags flag, bool state)
            {
                if (state)
                    m_Flags |= flag;
                else
                    m_Flags &= ~flag;
            }

            public StructField Build()
            {
                var structFieldInternal = new StructFieldInternal();
                structFieldInternal.m_NameHandle = container.AddString(name);
                structFieldInternal.m_TypeHandle = type.handle;
                structFieldInternal.m_AttributeListHandle = HandleListInternal.Build(container, attributes, (a) => (a.handle));
                structFieldInternal.m_Flags = m_Flags;
                var returnHandle = container.Add(structFieldInternal);
                return new StructField(container, returnHandle);
            }
        }
    }
}
