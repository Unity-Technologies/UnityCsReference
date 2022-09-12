// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using System;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    [RequiredByNativeCode]
    public abstract partial class CallbackOrderAttribute : Attribute
    {
        protected int m_CallbackOrder;
        internal int callbackOrder { get {return m_CallbackOrder; } }
    }

    [RequiredByNativeCode]
    [System.Obsolete("PostProcessAttribute has been renamed to CallbackOrderAttribute.")]
    public abstract partial class PostProcessAttribute : CallbackOrderAttribute
    {
        [System.Obsolete("PostProcessAttribute has been renamed. Use m_CallbackOrder of CallbackOrderAttribute.")]
        protected int m_PostprocessOrder;

        [System.Obsolete("PostProcessAttribute has been renamed. Use callbackOrder of CallbackOrderAttribute.")]
        internal int GetPostprocessOrder { get {return m_PostprocessOrder; } }
    }

    namespace Callbacks
    {
        internal sealed partial class RegisterPluginsAttribute : CallbackOrderAttribute
        {
            public RegisterPluginsAttribute() { m_CallbackOrder = 1; }
            public RegisterPluginsAttribute(int callbackOrder) { m_CallbackOrder = callbackOrder; }

            [RequiredSignature]
            static IEnumerable<PluginDesc> Signature(BuildTarget target) { throw new InvalidOperationException(); }
        }

        [RequiredByNativeCode]
        public sealed partial class PostProcessBuildAttribute : CallbackOrderAttribute
        {
            public PostProcessBuildAttribute() { m_CallbackOrder = 1; }
            public PostProcessBuildAttribute(int callbackOrder) { m_CallbackOrder = callbackOrder; }

            [RequiredSignature]
            static void Signature(BuildTarget target, string pathToBuiltProject) { throw new InvalidOperationException(); }
        }

        [RequiredByNativeCode]
        public sealed partial class PostProcessSceneAttribute : CallbackOrderAttribute
        {
            private int m_version;

            public PostProcessSceneAttribute() { m_CallbackOrder = 1; m_version = 0; }

            public PostProcessSceneAttribute(int callbackOrder) { m_CallbackOrder = callbackOrder; m_version = 0; }

            public PostProcessSceneAttribute(int callbackOrder, int version) { m_CallbackOrder = callbackOrder; m_version = version; }

            [RequiredByNativeCode]
            internal int GetVersion() => m_version;

            [RequiredSignature]
            static void Signature() { throw new InvalidOperationException(); }
        }

        [RequiredByNativeCode]
        public sealed partial class DidReloadScripts : CallbackOrderAttribute
        {
            public DidReloadScripts() { m_CallbackOrder = 1; }

            public DidReloadScripts(int callbackOrder) { m_CallbackOrder = callbackOrder; }

            [RequiredSignature]
            static void Signature() { throw new InvalidOperationException(); }
        }

        // Native version in Editor/Src/EditorHelper.h
        public enum OnOpenAssetAttributeMode
        {
            Execute,
            Validate
        }

        // Add this attribute to a static method to get a callback for opening an asset inside Unity before trying to open it with an external tool
        [RequiredByNativeCode]
        public sealed partial class OnOpenAssetAttribute : CallbackOrderAttribute
        {
            OnOpenAssetAttributeMode m_AttributeMode;

            public OnOpenAssetAttribute() : this(OnOpenAssetAttributeMode.Execute) {}
            public OnOpenAssetAttribute(OnOpenAssetAttributeMode attributeMode) : this(1, attributeMode) {}
            public OnOpenAssetAttribute(int callbackOrder) : this(callbackOrder, OnOpenAssetAttributeMode.Execute) {}
            public OnOpenAssetAttribute(int callbackOrder, OnOpenAssetAttributeMode attributeMode) { m_CallbackOrder = callbackOrder; m_AttributeMode = attributeMode; }

            [RequiredSignature]
            static bool Signature(int instanceID) { throw new InvalidOperationException(); }

            [RequiredSignature]
            static bool SignatureLine(int instanceID, int line) { throw new InvalidOperationException(); }

            [RequiredSignature]
            static bool SignatureLineColumn(int instanceID, int line, int column) { throw new InvalidOperationException(); }
        }
    }
}
