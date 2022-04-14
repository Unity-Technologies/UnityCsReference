// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/TemplatePassStageElement.h")]
    internal struct TemplatePassStageElementInternal
    {
        internal FoundryHandle m_BlockInstanceHandle;
        internal FoundryHandle m_CustomizationPointHandle;

        internal extern static TemplatePassStageElementInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct TemplatePassStageElement : IEquatable<TemplatePassStageElement>
    {
        // data members
        readonly ShaderContainer container;
        readonly internal FoundryHandle handle;
        readonly TemplatePassStageElementInternal templatePassStageElement;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);
        public BlockInstance BlockInstance => new BlockInstance(container, templatePassStageElement.m_BlockInstanceHandle);
        public CustomizationPoint CustomizationPoint => new CustomizationPoint(container, templatePassStageElement.m_CustomizationPointHandle);

        // private
        internal TemplatePassStageElement(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.templatePassStageElement = container?.GetTemplatePassStageElement(handle) ?? TemplatePassStageElementInternal.Invalid();
        }

        public static TemplatePassStageElement Invalid => new TemplatePassStageElement(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is TemplatePassStageElement other && this.Equals(other);
        public bool Equals(TemplatePassStageElement other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(TemplatePassStageElement lhs, TemplatePassStageElement rhs) => lhs.Equals(rhs);
        public static bool operator!=(TemplatePassStageElement lhs, TemplatePassStageElement rhs) => !lhs.Equals(rhs);
    }
}
