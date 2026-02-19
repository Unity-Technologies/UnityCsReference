// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Shaders;
using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/KeywordDescriptor.h")]
    internal struct KeywordDescriptorInternal : IInternalType<KeywordDescriptorInternal>
    {
        internal FoundryHandle m_LocationHandle;
        internal KeywordDescriptor.DefinitionType m_Definition;
        internal KeywordDescriptor.ScopeType m_Scope;
        internal ShaderStageFlags m_Stage;
        internal FoundryHandle m_ListHandle;

        [NativeMethod(IsThreadSafe = true)] internal extern static KeywordDescriptorInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern void Setup(ShaderContainer container, KeywordDescriptor.DefinitionType definition,
            KeywordDescriptor.ScopeType scope, ShaderStageFlags stage, string[] ops);

        [NativeMethod(IsThreadSafe = true)] internal extern KeywordDescriptor.DefinitionType GetDefinition();
        [NativeMethod(IsThreadSafe = true)] internal extern KeywordDescriptor.ScopeType GetScope();
        [NativeMethod(IsThreadSafe = true)] internal extern ShaderStageFlags GetStage();
        [NativeMethod(IsThreadSafe = true)] internal extern ulong GetOpCount(ShaderContainer container);
        [NativeMethod(IsThreadSafe = true)] internal extern string GetOp(ShaderContainer container, ulong index);

        // IInternalType
        KeywordDescriptorInternal IInternalType<KeywordDescriptorInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct KeywordDescriptor : IEquatable<KeywordDescriptor>, IPublicType<KeywordDescriptor>
    {
        // These enums must match the declarations in Modules/ShaderFoundry/Public/Enums/KeywordEnums.h
        public enum DefinitionType
        {
            Invalid,
            DynamicBranch,
            MaterialVariant,
            RuntimeVariant
        };

        public enum ScopeType
        {
            Invalid,
            Global,
            Local
        };

        // data members
        readonly ShaderContainer container;
        readonly KeywordDescriptorInternal descriptor;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        KeywordDescriptor IPublicType<KeywordDescriptor>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new KeywordDescriptor(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && descriptor.m_ListHandle.IsValid);

        public KeywordDescriptor.DefinitionType Definition => descriptor.GetDefinition();
        public KeywordDescriptor.ScopeType Scope => descriptor.GetScope();
        public ShaderStageFlags Stage => descriptor.GetStage();

        public IEnumerable<string> Ops
        {
            get
            {
                ulong opCount = descriptor.GetOpCount(container);
                for (ulong i = 0; i < opCount; ++i)
                    yield return descriptor.GetOp(container, i);
            }
        }

        public Location Location => new Location(container, descriptor.m_LocationHandle);

        // private
        internal KeywordDescriptor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out descriptor);
        }

        public static KeywordDescriptor Invalid => new KeywordDescriptor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is KeywordDescriptor other && this.Equals(other);
        public bool Equals(KeywordDescriptor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(KeywordDescriptor lhs, KeywordDescriptor rhs) => lhs.Equals(rhs);
        public static bool operator!=(KeywordDescriptor lhs, KeywordDescriptor rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            KeywordDescriptor.DefinitionType definition;
            KeywordDescriptor.ScopeType scope;
            ShaderStageFlags stage;
            List<string> ops = new List<string>();
            public Location location;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, KeywordDescriptor.DefinitionType definition,
                KeywordDescriptor.ScopeType scope, ShaderStageFlags stage, IEnumerable<string> ops)
            {
                this.container = container;
                this.definition = definition;
                this.scope = scope;
                this.stage = stage;
                this.ops.AddRange(ops);
            }

            public KeywordDescriptor Build()
            {
                var descriptor = new KeywordDescriptorInternal();
                descriptor.Setup(container, definition, scope, stage, ops.ToArray());
                descriptor.m_LocationHandle = location.handle;
                var resultHandle = container.Add(descriptor);
                return new KeywordDescriptor(container, resultHandle);
            }
        }
    }
}
