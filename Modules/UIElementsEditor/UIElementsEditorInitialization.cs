// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.UIElements
{
    static class UIElementsEditorInitialization
    {


        [UsedImplicitly]
        [RequiredByNativeCode(optional:false)]
        public static void InitializeUIElementsEditorManaged()
        {
            try
            {
                UxmlSerializedDataRegistry.RegisterUxmlSerializedDataTypes();
                UxmlSerializedDataRegistry.RegisterCustomDependencies();
                ThemeRegistry.RegisterCustomDependencies();
                RegisterSerializationLayoutDependency();
                UnityEngine.UIElements.UIElementsInitialization.InitializeUIElementsManaged();
                VisualTreeAssetHierarchyDropHandler.Register();

                // The setter pushes on change; also apply the persisted value at editor load so a saved setting takes effect.
                UnityEngine.UIElements.Layout.LayoutNative.SetGridLayoutEnabled(UIToolkitProjectSettings.enableGridLayout);
                UnityEngine.UIElements.PanelRenderer.RegisterPanelRendererAnimationBinding();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        static void RegisterSerializationLayoutDependency()
        {
            var hash = new Hash128();
            hash.Append(UnityEngine.UIElements.StyleSheet.currentSerializationLayoutHash);
            AssetDatabase.RegisterCustomDependency(UnityEngine.UIElements.StyleSheet.k_SerializationLayoutDependencyKey, hash);
        }
    }
}


