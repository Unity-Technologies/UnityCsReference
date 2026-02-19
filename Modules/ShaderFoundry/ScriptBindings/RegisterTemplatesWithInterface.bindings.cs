// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/RegisterTemplatesWithInterface.h")]
    internal struct RegisterTemplatesWithInterfaceInternal : IInternalType<RegisterTemplatesWithInterfaceInternal>
    {
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_BlockShaderInterfaceHandle;
        internal FoundryHandle m_RegistrationStatementListHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static RegisterTemplatesWithInterfaceInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        RegisterTemplatesWithInterfaceInternal IInternalType<RegisterTemplatesWithInterfaceInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct RegisterTemplatesWithInterface : IEquatable<RegisterTemplatesWithInterface>, IPublicType<RegisterTemplatesWithInterface>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly RegisterTemplatesWithInterfaceInternal internalData;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        RegisterTemplatesWithInterface IPublicType<RegisterTemplatesWithInterface>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle)
            => new RegisterTemplatesWithInterface(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public IEnumerable<ShaderAttribute> Attributes =>
            ListType.Enumerate<ShaderAttribute>(container, internalData.m_AttributeListHandle);
        public BlockShaderInterface Interface => new BlockShaderInterface(container, internalData.m_BlockShaderInterfaceHandle);
        public IEnumerable<InterfaceRegistrationStatement> RegistrationStatements =>
            ListType.Enumerate<InterfaceRegistrationStatement>(container, internalData.m_RegistrationStatementListHandle);
        // private
        internal RegisterTemplatesWithInterface(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out internalData);
        }

        public static RegisterTemplatesWithInterface Invalid => new RegisterTemplatesWithInterface(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is RegisterTemplatesWithInterface other && this.Equals(other);
        public bool Equals(RegisterTemplatesWithInterface other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(RegisterTemplatesWithInterface lhs, RegisterTemplatesWithInterface rhs) => lhs.Equals(rhs);
        public static bool operator!=(RegisterTemplatesWithInterface lhs, RegisterTemplatesWithInterface rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public BlockShaderInterface blockShaderInterface;
            public List<ShaderAttribute> Attributes;
            public List<InterfaceRegistrationStatement> RegistrationStatements;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, BlockShaderInterface blockShaderInterface)
            {
                this.container = container;
                this.blockShaderInterface = blockShaderInterface;
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                Utilities.AddToList(ref Attributes, attribute);
            }

            public void AddRegistrationStatement(InterfaceRegistrationStatement registrationStatement)
            {
                Utilities.AddToList(ref RegistrationStatements, registrationStatement);
            }

            public RegisterTemplatesWithInterface Build()
            {
                var internalData = new RegisterTemplatesWithInterfaceInternal();
                internalData.m_BlockShaderInterfaceHandle = blockShaderInterface.handle;
                internalData.m_AttributeListHandle = ListType.Build(container, Attributes);
                internalData.m_RegistrationStatementListHandle = ListType.Build(container, RegistrationStatements);

                if (RegistrationStatements != null)
                {
                    foreach (var registrationStatement in RegistrationStatements)
                    {
                        if (!registrationStatement.RegisterWithInterface(blockShaderInterface, container))
                        {
                            var templateName = registrationStatement.Template.Name;
                            var message = "Failed to register template " + templateName + " with the block shader interface.";
                            throw new InvalidOperationException(message);
                        }
                    }
                }

                var returnTypeHandle = container.Add(internalData);
                return new RegisterTemplatesWithInterface(container, returnTypeHandle);
            }
        }
    }
}
