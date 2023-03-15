// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/ShaderCustomEditor.h")]
    internal struct ShaderCustomEditorInternal : IInternalType<ShaderCustomEditorInternal>
    {
        internal FoundryHandle m_CustomEditorClassNameHandle;         // string
        internal FoundryHandle m_RenderPipelineAssetClassNameHandle;  // string

        internal extern static ShaderCustomEditorInternal Invalid();
        internal extern bool IsValid();
        internal extern string GetCustomEditorClassName(ShaderContainer container);
        internal extern string GetRenderPipelineAssetClassName(ShaderContainer container);

        // IInternalType
        ShaderCustomEditorInternal IInternalType<ShaderCustomEditorInternal>.ConstructInvalid() => Invalid();
    }

    [FoundryAPI]
    internal readonly struct ShaderCustomEditor : IEquatable<ShaderCustomEditor>, IComparable<ShaderCustomEditor>, IPublicType<ShaderCustomEditor>
    {
        // data members
        readonly ShaderContainer container;
        internal readonly FoundryHandle handle;
        readonly ShaderCustomEditorInternal shaderCustomEditor;

        // IPublicType
        ShaderContainer IPublicType.Container => Container;
        bool IPublicType.IsValid => IsValid;
        FoundryHandle IPublicType.Handle => handle;
        ShaderCustomEditor IPublicType<ShaderCustomEditor>.ConstructFromHandle(ShaderContainer container, FoundryHandle handle) => new ShaderCustomEditor(container, handle);

        // public API
        public ShaderContainer Container => container;
        public bool IsValid => (container != null && handle.IsValid);

        public ShaderCustomEditor(ShaderContainer container, string customEditorClassName, string renderPipelineAssetClassName)
            : this(container, container.AddString(customEditorClassName), container.AddString(renderPipelineAssetClassName))
        {
        }

        internal ShaderCustomEditor(ShaderContainer container, FoundryHandle customEditorClassNameHandle, FoundryHandle renderPipelineAssetClassNameHandle)
        {
            if ((container == null) || !customEditorClassNameHandle.IsValid) // rpAsset is allowed to be invalid
            {
                this = Invalid;
            }
            else
            {
                shaderCustomEditor.m_CustomEditorClassNameHandle = customEditorClassNameHandle;
                shaderCustomEditor.m_RenderPipelineAssetClassNameHandle = renderPipelineAssetClassNameHandle;
                handle = container.Add(shaderCustomEditor);
                this.container = handle.IsValid ? container : null;
            }
        }

        public string CustomEditorClassName => shaderCustomEditor.GetCustomEditorClassName(container);
        public string RenderPipelineAssetClassName => shaderCustomEditor.GetRenderPipelineAssetClassName(container);

        internal ShaderCustomEditor(ShaderContainer container, FoundryHandle handle)
        {
            this.container = container;
            this.handle = handle;
            ShaderContainer.Get(container, handle, out shaderCustomEditor);
        }

        public static ShaderCustomEditor Invalid => new ShaderCustomEditor(null, FoundryHandle.Invalid());

        // Equals and operator == implement Reference Equality.  ValueEquals does a deep compare if you need that instead.
        public override bool Equals(object obj) => obj is ShaderCustomEditor other && this.Equals(other);
        public bool Equals(ShaderCustomEditor other) => EqualityChecks.ReferenceEquals(this.handle, this.container, other.handle, other.container);
        public override int GetHashCode() => (container, handle).GetHashCode();
        public static bool operator==(ShaderCustomEditor lhs, ShaderCustomEditor rhs) => lhs.Equals(rhs);
        public static bool operator!=(ShaderCustomEditor lhs, ShaderCustomEditor rhs) => !lhs.Equals(rhs);

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
