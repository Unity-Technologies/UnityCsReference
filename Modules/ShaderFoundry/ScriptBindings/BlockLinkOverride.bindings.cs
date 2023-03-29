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
    using LinkAccessorKind = BlockLinkOverrideInternal.LinkAccessorInternal.Kind;

    [NativeHeader("Modules/ShaderFoundry/Public/BlockLinkOverride.h")]
    internal struct BlockLinkOverrideInternal : IInternalType<BlockLinkOverrideInternal>
    {
        internal struct LinkAccessorInternal : IInternalType<LinkAccessorInternal>
        {
            internal enum Kind : ushort
            {
                // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN BlockLinkOverride.h
                Invalid = 0,
                MemberAccess = 1,
                ArrayAccess = 2,
            }

            internal Kind m_Kind;
            internal FoundryHandle m_AccessorHandle;

            internal extern static LinkAccessorInternal Invalid();
            internal extern bool IsValid();

            // IInternalType
            LinkAccessorInternal IInternalType<LinkAccessorInternal>.ConstructInvalid() => Invalid();
        }

        internal struct LinkElementInternal : IInternalType<LinkElementInternal>
        {
            internal FoundryHandle m_TypeHandle; // ShaderType
            internal FoundryHandle m_CastTypeHandle; // ShaderType
            internal FoundryHandle m_NamespaceHandle; // string
            internal FoundryHandle m_NameHandle; // string
            internal FoundryHandle m_ConstantExpressionHandle; // string
            internal FoundryHandle m_AccessorListHandle; // List<LinkAccessor>

            internal extern static LinkElementInternal Invalid();
            internal extern bool IsValid();

            // IInternalType
            LinkElementInternal IInternalType<LinkElementInternal>.ConstructInvalid() => Invalid();
        }

        internal FoundryHandle m_InterfaceFieldHandle;
        internal FoundryHandle m_ArgumentHandle;

        internal extern static BlockLinkOverrideInternal Invalid();
        internal extern bool IsValid();

        // IInternalType
        BlockLinkOverrideInternal IInternalType<BlockLinkOverrideInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct BlockLinkOverride : IEquatable<BlockLinkOverride>, IPublicType<BlockLinkOverride>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly BlockLinkOverrideInternal linkOverride;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        BlockLinkOverride IPublicType<BlockLinkOverride>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new BlockLinkOverride(container, handle);

        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid && linkOverride.IsValid());

        internal readonly struct LinkAccessor : IEquatable<LinkAccessor>, IPublicType<LinkAccessor>
        {
            // data members
            readonly ShaderContainer container;
            readonly internal FoundryHandle handle;
            readonly BlockLinkOverrideInternal.LinkAccessorInternal accessor;

            // IPublicType
            ShaderContainer IPublicType.Container => Container;
            bool IPublicType.IsValid => IsValid;
            FoundryHandle IPublicType.Handle => handle;
            LinkAccessor IPublicType<LinkAccessor>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new LinkAccessor(container, handle);

            public ShaderContainer Container => container;
            public bool IsValid => (container != null && handle.IsValid && accessor.IsValid());

            public bool IsMemberAccessor => IsKind(LinkAccessorKind.MemberAccess);
            public bool IsArrayAccessor => IsKind(LinkAccessorKind.ArrayAccess);
            public string MemberAccessor => GetAccessor(LinkAccessorKind.MemberAccess);
            public string ArrayAccessor => GetAccessor(LinkAccessorKind.ArrayAccess);

            bool IsKind(BlockLinkOverrideInternal.LinkAccessorInternal.Kind expectedKind)
            {
                return accessor.m_Kind == expectedKind;
            }

            string GetAccessor(BlockLinkOverrideInternal.LinkAccessorInternal.Kind expectedKind)
            {
                if (IsKind(expectedKind))
                    return container?.GetString(accessor.m_AccessorHandle);
                return string.Empty;
            }

            // private
            internal LinkAccessor(ShaderContainer container, FoundryHandle handle)
            {
                this.container = container;
                this.handle = handle;
                ShaderContainer.Get(container, handle, out accessor);
            }

            public static LinkAccessor Invalid => new LinkAccessor(null, FoundryHandle.Invalid());

            // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
            public override bool Equals(object obj) => obj is LinkAccessor other && this.Equals(other);
            public bool Equals(LinkAccessor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
            public override int GetHashCode() => (container, handle).GetHashCode();
            public static bool operator==(LinkAccessor lhs, LinkAccessor rhs) => lhs.Equals(rhs);
            public static bool operator!=(LinkAccessor lhs, LinkAccessor rhs) => !lhs.Equals(rhs);

            static LinkAccessor BuildAccessor(ShaderContainer container, LinkAccessorKind kind, FoundryHandle accessorHandle)
            {
                var linkAccessorInternal = new BlockLinkOverrideInternal.LinkAccessorInternal();
                linkAccessorInternal.m_Kind = kind;
                linkAccessorInternal.m_AccessorHandle = accessorHandle;
                var returnTypeHandle = container.Add(linkAccessorInternal);
                return new LinkAccessor(container, returnTypeHandle);
            }

            public class MemberAccessBuilder
            {
                ShaderContainer container;
                public string memberAccessor;

                public ShaderContainer Container => container;

                public MemberAccessBuilder(ShaderContainer container, string memberAccessor)
                {
                    this.container = container;
                    this.memberAccessor = memberAccessor;
                }

                public LinkAccessor Build() => BuildAccessor(container, LinkAccessorKind.MemberAccess, container.AddString(memberAccessor));
            }

            public class ArrayAccessBuilder
            {
                ShaderContainer container;
                public string arrayAccessor;

                public ShaderContainer Container => container;

                public ArrayAccessBuilder(ShaderContainer container, string arrayAccessor)
                {
                    this.container = container;
                    this.arrayAccessor = arrayAccessor;
                }

                public LinkAccessor Build() => BuildAccessor(container, LinkAccessorKind.ArrayAccess, container.AddString(arrayAccessor));
            }
        }

        internal readonly struct LinkElement : IEquatable<LinkElement>, IPublicType<LinkElement>
        {
            // data members
            readonly ShaderContainer container;
            readonly internal FoundryHandle handle;
            readonly BlockLinkOverrideInternal.LinkElementInternal element;

            // IPublicType
            ShaderContainer IPublicType.Container => Container;
            bool IPublicType.IsValid => IsValid;
            FoundryHandle IPublicType.Handle => handle;
            LinkElement IPublicType<LinkElement>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new LinkElement(container, handle);

            public ShaderContainer Container => container;
            public bool IsValid => (container != null && handle.IsValid && element.IsValid());

            public ShaderType Type => new ShaderType(container, element.m_TypeHandle);
            public ShaderType CastType => new ShaderType(container, element.m_CastTypeHandle);
            public string Namespace => container?.GetString(element.m_NamespaceHandle) ?? string.Empty;
            public string Name => container?.GetString(element.m_NameHandle) ?? string.Empty;
            public string ConstantExpression => container?.GetString(element.m_ConstantExpressionHandle) ?? string.Empty;
            public IEnumerable<LinkAccessor> Accessors => element.m_AccessorListHandle.AsListEnumerable<LinkAccessor>(Container, (container, handle) => new LinkAccessor(container, handle));

            // private
            internal LinkElement(ShaderContainer container, FoundryHandle handle)
            {
                this.container = container;
                this.handle = handle;
                ShaderContainer.Get(container, handle, out element);
            }

            public static LinkElement Invalid => new LinkElement(null, FoundryHandle.Invalid());

            // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
            public override bool Equals(object obj) => obj is LinkElement other && this.Equals(other);
            public bool Equals(LinkElement other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
            public override int GetHashCode() => (container, handle).GetHashCode();
            public static bool operator==(LinkElement lhs, LinkElement rhs) => lhs.Equals(rhs);
            public static bool operator!=(LinkElement lhs, LinkElement rhs) => !lhs.Equals(rhs);

            public class Builder
            {
                ShaderContainer container;
                public ShaderType type { get; set; } = ShaderType.Invalid;
                public ShaderType castType { get; set; } = ShaderType.Invalid;
                public string namespaceName { get; set; }
                public string name { get; set; }
                List<LinkAccessor> m_Accessors;

                public ShaderContainer Container => container;

                public Builder(ShaderContainer container, string name)
                {
                    this.container = container;
                    this.name = name;
                }

                public void AddAccessor(LinkAccessor linkAccessor)
                {
                    if (m_Accessors == null)
                        m_Accessors = new List<LinkAccessor>();
                    m_Accessors.Add(linkAccessor);
                }

                public LinkElement Build()
                {
                    var linkElementInternal = new BlockLinkOverrideInternal.LinkElementInternal();
                    linkElementInternal.m_TypeHandle = type.handle;
                    linkElementInternal.m_CastTypeHandle = castType.handle;
                    linkElementInternal.m_NamespaceHandle = container.AddString(namespaceName);
                    linkElementInternal.m_NameHandle = container.AddString(name);
                    linkElementInternal.m_AccessorListHandle = HandleListInternal.Build(container, m_Accessors, (a) => (a.handle));
                    var returnTypeHandle = container.Add(linkElementInternal);
                    return new LinkElement(container, returnTypeHandle);
                }
            }

            public class ConstantExpressionBuilder
            {
                ShaderContainer container;
                public string constantExpression { get; set; }

                public ShaderContainer Container => container;

                public ConstantExpressionBuilder(ShaderContainer container, string constantExpression)
                {
                    this.container = container;
                    this.constantExpression = constantExpression;
                }

                public LinkElement Build()
                {
                    var linkElementInternal = new BlockLinkOverrideInternal.LinkElementInternal();
                    linkElementInternal.m_ConstantExpressionHandle = container.AddString(constantExpression);
                    var returnTypeHandle = container.Add(linkElementInternal);
                    return new LinkElement(container, returnTypeHandle);
                }
            }
        }

        // The interface field is applied to the block sequence element (e.g. block input or output).
        public LinkElement InterfaceField => new LinkElement(container, linkOverride.m_InterfaceFieldHandle);
        // The argument is applied to the block sequence (e.g. the input or output being matched against).
        public LinkElement Argument => new LinkElement(container, linkOverride.m_ArgumentHandle);

        // private
        internal BlockLinkOverride(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out linkOverride);
        }

        public static BlockLinkOverride Invalid => new BlockLinkOverride(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is BlockLinkOverride other && this.Equals(other);
        public bool Equals(BlockLinkOverride other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(BlockLinkOverride lhs, BlockLinkOverride rhs) => lhs.Equals(rhs);
        public static bool operator!=(BlockLinkOverride lhs, BlockLinkOverride rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            internal LinkElement interfaceField;
            internal LinkElement argument;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, LinkElement interfaceField, LinkElement argument)
            {
                this.container = container;
                this.interfaceField = interfaceField;
                this.argument = argument;
            }

            public BlockLinkOverride Build()
            {
                var linkOverrideInternal = new BlockLinkOverrideInternal();
                linkOverrideInternal.m_InterfaceFieldHandle = interfaceField.handle;
                linkOverrideInternal.m_ArgumentHandle = argument.handle;
                var returnTypeHandle = container.Add(linkOverrideInternal);
                return new BlockLinkOverride(container, returnTypeHandle);
            }
        }
    }
}
