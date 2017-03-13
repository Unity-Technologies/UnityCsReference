// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;

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
        }

        [RequiredByNativeCode]
        public sealed partial class PostProcessBuildAttribute : CallbackOrderAttribute
        {
            public PostProcessBuildAttribute() { m_CallbackOrder = 1; }
            public PostProcessBuildAttribute(int callbackOrder) { m_CallbackOrder = callbackOrder; }
        }

        [RequiredByNativeCode]
        public sealed partial class PostProcessSceneAttribute : CallbackOrderAttribute
        {
            private int m_version;
            internal int version { get {return m_version; } }

            public PostProcessSceneAttribute() { m_CallbackOrder = 1; m_version = 0; }

            public PostProcessSceneAttribute(int callbackOrder) { m_CallbackOrder = callbackOrder; m_version = 0; }

            public PostProcessSceneAttribute(int callbackOrder, int version) { m_CallbackOrder = callbackOrder; m_version = version; }
        }

        [RequiredByNativeCode]
        public sealed partial class DidReloadScripts : CallbackOrderAttribute
        {
            public DidReloadScripts() { m_CallbackOrder = 1; }

            public DidReloadScripts(int callbackOrder) { m_CallbackOrder = callbackOrder; }
        }

        // Add this attribute to a static method to get a callback for opening an asset inside Unity before trying to open it with an external tool
        [RequiredByNativeCode]
        public sealed partial class OnOpenAssetAttribute : CallbackOrderAttribute
        {
            public OnOpenAssetAttribute() { m_CallbackOrder = 1; }
            public OnOpenAssetAttribute(int callbackOrder) { m_CallbackOrder = callbackOrder; }
        }
    }

    internal partial class Empty
    {
        internal static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject) {}
        internal static bool OnOpenAsset(int instanceID, int line) {return false; }
    }
}
