// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/CopyRule.h")]
    internal struct CopyRuleInternal : IInternalType<CopyRuleInternal>
    {
        internal FoundryHandle m_SourceNameHandle; // string
        internal FoundryHandle m_InclusionListHandle; // List<string>
        internal FoundryHandle m_ExclusionListHandle; // List<string>

        [NativeMethod(IsThreadSafe = true)] internal extern static CopyRuleInternal Invalid();
        [NativeMethod(IsThreadSafe = true)] internal extern bool IsValid();

        // IInternalType
        CopyRuleInternal IInternalType<CopyRuleInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct CopyRule : IEquatable<CopyRule>, IPublicType<CopyRule>
    {
        // data members
        readonly ShaderContainer container;
        readonly CopyRuleInternal rule;
        internal readonly FoundryHandle handle;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        CopyRule IPublicType<CopyRule>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new CopyRule(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && rule.IsValid());
        public string SourceName => container?.GetString(rule.m_SourceNameHandle) ?? string.Empty;
        public IEnumerable<string> Inclusions =>
            ListType.EnumerateStrings(container, rule.m_InclusionListHandle);
        public IEnumerable<string> Exclusions =>
            ListType.EnumerateStrings(container, rule.m_ExclusionListHandle);

        // private
        internal CopyRule(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out rule);
        }

        public static CopyRule Invalid => new CopyRule(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.
        public override bool Equals(object obj) => obj is CopyRule other && this.Equals(other);
        public bool Equals(CopyRule other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(CopyRule lhs, CopyRule rhs) => lhs.Equals(rhs);
        public static bool operator!=(CopyRule lhs, CopyRule rhs) => !lhs.Equals(rhs);

        public class Builder
        {
            ShaderContainer container;
            string sourceName;
            List<string> inclusions;
            List<string> exclusions;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string sourceName)
            {
                this.container = container;
                this.sourceName = sourceName;
            }

            public void AddInclusion(string name) =>
                Utilities.AddToList(ref inclusions, name);

            public void AddExclusion(string name) =>
                Utilities.AddToList(ref exclusions, name);

            public CopyRule Build()
            {
                var rule = new CopyRuleInternal();
                rule.m_SourceNameHandle = container.AddString(sourceName);
                rule.m_InclusionListHandle = ListType.Build(container, inclusions);
                rule.m_ExclusionListHandle = ListType.Build(container, exclusions);
                var resultHandle = container.Add(rule);
                return new CopyRule(container, resultHandle);
            }
        }
    }
}
