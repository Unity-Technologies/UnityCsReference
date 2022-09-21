// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderCustomEditor.h")]
    internal struct ShaderCustomEditorInternal
    {
        internal FoundryHandle m_CustomEditorClassName;         // string
        internal FoundryHandle m_RenderPipelineAssetClassName;  // string

        internal extern static ShaderCustomEditorInternal Invalid();
        internal extern bool IsValid();
    }

    [FoundryAPI]
    internal readonly struct ShaderCustomEditor : IEquatable<ShaderCustomEditor>, IComparable<ShaderCustomEditor>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly ShaderCustomEditorInternal shaderCustomEditor;

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public ShaderCustomEditor(ShaderContainer container, string customEditorClassName, string renderPipelineAssetClassName)
            : this(container, container.AddString(customEditorClassName), container.AddString(renderPipelineAssetClassName))
        {
        }

        internal ShaderCustomEditor(ShaderContainer container, FoundryHandle customEditorClassName, FoundryHandle renderPipelineAssetClassName)
        {
            if ((container == null) || !customEditorClassName.IsValid) // rpAsset is allowed to be invalid
            {
                this = Invalid;
            }
            else
            {
                shaderCustomEditor.m_CustomEditorClassName = customEditorClassName;
                shaderCustomEditor.m_RenderPipelineAssetClassName = renderPipelineAssetClassName;
                handle = container.AddShaderCustomEditor(shaderCustomEditor);
                this.container = handle.IsValid ? container : null;
            }
        }

        public string CustomEditorClassName => container?.GetString(shaderCustomEditor.m_CustomEditorClassName);
        public string RenderPipelineAssetClassName => container?.GetString(shaderCustomEditor.m_RenderPipelineAssetClassName);

        internal ShaderCustomEditor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            this.shaderCustomEditor = container?.GetShaderCustomEditor(handle) ?? ShaderCustomEditorInternal.Invalid();
        }

        public static ShaderCustomEditor Invalid => new ShaderCustomEditor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderCustomEditor other && this.Equals(other);
        public bool Equals(ShaderCustomEditor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator ==(ShaderCustomEditor lhs, ShaderCustomEditor rhs) => lhs.Equals(rhs);
        public static bool operator !=(ShaderCustomEditor lhs, ShaderCustomEditor rhs) => !lhs.Equals(rhs);

        public int CompareTo(ShaderCustomEditor other)
        {
            int result = string.CompareOrdinal(RenderPipelineAssetClassName, other.RenderPipelineAssetClassName);
            if (result == 0)
                result = string.CompareOrdinal(CustomEditorClassName, other.CustomEditorClassName);
            return result;
        }

        public class Builder
        {
            ShaderContainer container;
            string customEditorClassName;
            string renderPipelineAssetClassName;

            public ShaderContainer Container => container;

            public Builder(ShaderContainer container, string customEditorClassName, string renderPipelineAssetClassName)
            {
                this.container = container;
                this.customEditorClassName = customEditorClassName;
                this.renderPipelineAssetClassName = renderPipelineAssetClassName;
            }

            public ShaderCustomEditor Build()
            {
                return new ShaderCustomEditor(container, customEditorClassName, renderPipelineAssetClassName);
            }
        }
    }
}
