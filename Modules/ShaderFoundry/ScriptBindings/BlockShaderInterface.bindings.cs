// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/BlockShaderInterface.h")]
    internal struct BlockShaderInterfaceInternal : IInternalType<BlockShaderInterfaceInternal>
    {
        internal FoundryHandle m_NameHandle;
        internal FoundryHandle m_AttributeListHandle;
        internal FoundryHandle m_ContainingNamespaceHandle;
        internal FoundryHandle m_ExtensionsListHandle;
        internal FoundryHandle m_CustomizationPointListHandle;

        internal extern static BlockShaderInterfaceInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        BlockShaderInterfaceInternal IInternalType<BlockShaderInterfaceInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct BlockShaderInterface : IEquatable<BlockShaderInterface>, IPublicType<BlockShaderInterface>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockShaderInterfaceInternal blockShaderInterfaceInternal;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        BlockShaderInterface IPublicType<BlockShaderInterface>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new BlockShaderInterface(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public string Name => container?.GetString(blockShaderInterfaceInternal.m_NameHandle) ?? string.Empty;
        public IEnumerable<ShaderAttribute> Attributes
        {
            get
            {
                var listHandle = blockShaderInterfaceInternal.m_AttributeListHandle;
                return FixedHandleListInternal.Enumerate<ShaderAttribute>(container, listHandle);
            }
        }
        public Namespace ContainingNamespace => new Namespace(container, blockShaderInterfaceInternal.m_ContainingNamespaceHandle);
        public IEnumerable<BlockShaderInterface> Extensions
        {
            get
            {
                var listHandle = blockShaderInterfaceInternal.m_ExtensionsListHandle;
                return FixedHandleListInternal.Enumerate<BlockShaderInterface>(container, listHandle);
            }
        }
        public IEnumerable<CustomizationPoint> CustomizationPoints
        {
            get
            {
                var listHandle = blockShaderInterfaceInternal.m_CustomizationPointListHandle;
                return FixedHandleListInternal.Enumerate<CustomizationPoint>(container, listHandle);
            }
        }

        // private
        internal BlockShaderInterface(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out blockShaderInterfaceInternal);
        }

        public static BlockShaderInterface Invalid => new BlockShaderInterface(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is BlockShaderInterface other && this.Equals(other);
        public bool Equals(BlockShaderInterface other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockShaderInterface lhs, BlockShaderInterface rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockShaderInterface lhs, BlockShaderInterface rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public string Name;
            public List<ShaderAttribute> Attributes;
            public Namespace containingNamespace;
            public List<BlockShaderInterface> Extensions;
            public List<CustomizationPoint> CustomizationPoints;
            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                this.Name = name;
            }

            public void AddAttribute(ShaderAttribute attribute)
            {
                Utilities.AddToList(ref Attributes, attribute);
            }

            public void AddExtension(BlockShaderInterface blockShaderInterface)
            {
                Utilities.AddToList(ref Extensions, blockShaderInterface);
            }

            public void AddCustomizationPoint(CustomizationPoint customizationPoint)
            {
                Utilities.AddToList(ref CustomizationPoints, customizationPoint);
            }

            public BlockShaderInterface Build()
            {
                var blockShaderInterfaceInternal = new BlockShaderInterfaceInternal()
                {
                    m_NameHandle = container.AddString(Name),
                };

                blockShaderInterfaceInternal.m_AttributeListHandle = FixedHandleListInternal.Build(container, Attributes);
                blockShaderInterfaceInternal.m_ContainingNamespaceHandle = containingNamespace.handle;
                blockShaderInterfaceInternal.m_ExtensionsListHandle = FixedHandleListInternal.Build(container, Extensions);
                blockShaderInterfaceInternal.m_CustomizationPointListHandle = FixedHandleListInternal.Build(container, CustomizationPoints);

                var returnTypeHandle = container.Add(blockShaderInterfaceInternal);
                return new BlockShaderInterface(container, returnTypeHandle);
            }
        }
    }
}
