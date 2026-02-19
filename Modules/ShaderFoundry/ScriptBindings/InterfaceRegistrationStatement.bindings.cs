// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/InterfaceRegistrationStatement.h")]
    internal struct InterfaceRegistrationStatementInternal : IInternalType<InterfaceRegistrationStatementInternal>
    {
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_EntryHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static InterfaceRegistrationStatementInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        InterfaceRegistrationStatementInternal IInternalType<InterfaceRegistrationStatementInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct InterfaceRegistrationStatement : IEquatable<InterfaceRegistrationStatement>, IPublicType<InterfaceRegistrationStatement>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly InterfaceRegistrationStatementInternal statementInternal;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        InterfaceRegistrationStatement IPublicType<InterfaceRegistrationStatement>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new InterfaceRegistrationStatement(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, statementInternal.m_AttributeListHandle);

        public Template Template
        {
            get
            {
                if (container == null)
                {
                    var message = "Invalid call to 'InterfaceRegistrationStatement.Template'. " +
                        "Container is null, check the container";
                    throw new InvalidOperationException(message);
                }
                return new Template(container, statementInternal.m_EntryHandle);
            }
        }

        // private
        internal InterfaceRegistrationStatement(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out statementInternal);
        }

        internal bool RegisterWithInterface(BlockShaderInterface blockShaderInterface, ShaderContainer container)
        {
            return blockShaderInterface.RegisterTemplate(container, handle);
        }

        public static InterfaceRegistrationStatement Invalid => new InterfaceRegistrationStatement(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is InterfaceRegistrationStatement other && this.Equals(other);
        public bool Equals(InterfaceRegistrationStatement other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(InterfaceRegistrationStatement lhs, InterfaceRegistrationStatement rhs) => lhs.Equals(rhs);
        public static bool operator!=(InterfaceRegistrationStatement lhs, InterfaceRegistrationStatement rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;

            public List<ShaderAttribute> Attributes;
            public Template Template { get; private set; }
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, Template template)
            {
                this.container = container;
                this.Template = template;
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                Utilities.AddToList(ref Attributes, attribute);
            }

            public InterfaceRegistrationStatement Build()
            {
                var statementInternal = new InterfaceRegistrationStatementInternal();

                statementInternal.m_AttributeListHandle = ListType.Build(container, Attributes);
                if (Template.IsValid)
                    statementInternal.m_EntryHandle = Template.handle;

                var returnTypeHandle = container.Add(statementInternal);
                return new InterfaceRegistrationStatement(container, returnTypeHandle);
            }
        }
    }
}
