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
    internal struct StructFieldInternal
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_TypeHandle;
        internal FoundryHandle m_AttributeListHandle;

        // TODO no need to make this extern, can duplicate it here
        internal static extern StructFieldInternal Invalid();

        internal bool IsValid => m_NameHandle.IsValid;

        internal IEnumerable<ShaderAttribute> Attributes(ShaderContainer container)
        {
            var list = new FixedHandleListInternal(m_AttributeListHandle);
            return list.Select<ShaderAttribute>(container, (handle) => (new ShaderAttribute(container, handle)));
        }

        internal extern static bool ValueEquals(ShaderContainer aContainer, FoundryHandle aHandle, ShaderContainer bContainer, FoundryHandle bHandle);
    }

    [FoundryAPI]
    internal readonly struct StructField : IEquatable<StructField>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly StructFieldInternal field;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null) && handle.IsValid && (field.IsValid);
        public string Name => container?.GetString(field.m_NameHandle) ?? string.Empty;
        public ShaderType Type => new ShaderType(container, field.m_TypeHandle);
        public IEnumerable<ShaderAttribute> Attributes => field.Attributes(container);

        // private
        internal StructField(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.field = container?.GetStructField(handle) ?? StructFieldInternal.Invalid();
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

            public Builder(ShaderContainer container, string name, ShaderType type)
            {
                this.container = container;
                this.name = name;
                this.type = type;
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                if (attributes == null)
                    attributes = new List<ShaderAttribute>();
                attributes.Add(attribute);
            }

            public StructField Build()
            {
                var structFieldInternal = new StructFieldInternal();
                structFieldInternal.m_NameHandle = container.AddString(name);
                structFieldInternal.m_TypeHandle = type.handle;
                structFieldInternal.m_AttributeListHandle = FixedHandleListInternal.Build(container, attributes, (a) => (a.handle));
                var returnHandle = container.AddStructFieldInternal(structFieldInternal);
                return new StructField(container, returnHandle);
            }
        }
    }
}
