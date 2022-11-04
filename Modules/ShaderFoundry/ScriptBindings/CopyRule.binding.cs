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
    [NativeHeader("Modules/ShaderFoundry/Public/CopyRule.h")]
    internal struct CopyRuleInternal
    {
        internal FoundryHandle m_SourceName; // string
        internal FoundryHandle m_InclusionListHandle; // List<string>
        internal FoundryHandle m_ExclusionListHandle; // List<string>

        internal extern static CopyRuleInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct CopyRule : IEquatable<CopyRule>
    {
        // data members
        readonly ShaderContainer container;
        readonly CopyRuleInternal rule;
        internal readonly FoundryHandle handle;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && rule.IsValid());
        public string SourceName => container?.GetString(rule.m_SourceName) ?? string.Empty;
        public IEnumerable<string> Inclusions => rule.m_InclusionListHandle.AsListEnumerable<string>(Container, (container, handle) => (container?.GetString(handle) ?? string.Empty));
        public IEnumerable<string> Exclusions => rule.m_ExclusionListHandle.AsListEnumerable<string>(Container, (container, handle) => (container?.GetString(handle) ?? string.Empty));

        // private
        internal CopyRule(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.rule = container?.GetCopyRule(handle) ?? CopyRuleInternal.Invalid();
        }

        public static CopyRule Invalid => new CopyRule(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
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

            public void AddInclusion(string name)
            {
                if (inclusions == null)
                    inclusions = new List<string>();
                inclusions.Add(name);
            }

            public void AddExclusion(string name)
            {
                if (exclusions == null)
                    exclusions = new List<string>();
                exclusions.Add(name);
            }

            public CopyRule Build()
            {
                var rule = new CopyRuleInternal();
                rule.m_SourceName = container.AddString(sourceName);
                rule.m_InclusionListHandle = FixedHandleListInternal.Build(container, inclusions, (n) => (container.AddString(n)));
                rule.m_ExclusionListHandle = FixedHandleListInternal.Build(container, exclusions, (n) => (container.AddString(n)));
                var resultHandle = container.AddCopyRuleInternal(rule);
                return new CopyRule(container, resultHandle);
            }
        }
    }
}
