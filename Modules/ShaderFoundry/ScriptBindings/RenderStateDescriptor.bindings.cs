// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/RenderStateDescriptor.h")]
    internal struct RenderStateDescriptorInternal : IInternalType<RenderStateDescriptorInternal>
    {
        internal RenderStateDescriptor.StateKind m_Kind;
        // core::string, IntegerLiteral, FloatLitereal, RenderStateProperty, RenderStateTargetSpecifier, RenderStateNamedValue
        internal FoundryHandle m_ListHandle;
        internal FoundryHandle m_LocationHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static RenderStateDescriptorInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        RenderStateDescriptorInternal IInternalType<RenderStateDescriptorInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct RenderStateDescriptor : IEquatable<RenderStateDescriptor>, IPublicType<RenderStateDescriptor>
    {
        public enum StateKind : ushort
        {
            Invalid = 0,
            AlphaToMask,
            Blend,
            BlendOp,
            ColorMask,
            Conservative,
            Cull,
            ZClip,
            ZTest,
            ZWrite,
            Offset,
            Stencil,

            Count,
        };

        // data members
        readonly ShaderContainer container;
        readonly RenderStateDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        RenderStateDescriptor IPublicType<RenderStateDescriptor>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle)
            => new RenderStateDescriptor(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.IsValid());
        public StateKind Kind => descriptor.m_Kind;

        internal IEnumerable<IPublicType> Ops =>
            ListType.EnumeratePublicType(container, descriptor.m_ListHandle);

        internal Location Location => new Location(container, descriptor.m_LocationHandle);

        // private
        internal RenderStateDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out descriptor);
        }

        public static RenderStateDescriptor Invalid => new RenderStateDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is RenderStateDescriptor other && this.Equals(other);
        public bool Equals(RenderStateDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(RenderStateDescriptor lhs, RenderStateDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(RenderStateDescriptor lhs, RenderStateDescriptor rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            public StateKind kind = StateKind.Invalid;
            internal List<IPublicType> publicTypes;
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, StateKind kind)
            {
                this.container = container;
                this.kind = kind;
            }
            void AddOp(IPublicType item) => Utilities.AddToList(ref publicTypes, item);
            public void Add(RenderStateNamedValue symbol) => AddOp(symbol);
            public void Add(RenderStateTargetSpecifier symbol) => AddOp(symbol);
            public void Add(RenderStateProperty symbol) => AddOp(symbol);
            public void Add(string value)
            {
                AddOp(new StringLiteral(container, container.AddString(value)));
            }
            public void Add(int value)
            {
                var intBuilder = new IntegerLiteral.Builder(container);
                intBuilder.Value = value;
                AddOp(intBuilder.Build());
            }
            public void Add(float value)
            {
                var floatBuilder = new FloatLiteral.Builder(container);
                floatBuilder.Value = value;
                AddOp(floatBuilder.Build());
            }

            public RenderStateDescriptor Build()
            {
                var descriptor = new RenderStateDescriptorInternal();
                descriptor.m_Kind = kind;
                descriptor.m_ListHandle = ListType.Build(container, publicTypes);
                descriptor.m_LocationHandle = location.handle;
                var resultHandle = container.Add(descriptor);
                return new RenderStateDescriptor(container, resultHandle);
            }
        }
    }
}
