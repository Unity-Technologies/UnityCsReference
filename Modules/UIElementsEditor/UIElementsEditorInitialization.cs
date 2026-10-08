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
                // Push before RegisterCustomDependencies so the published hash reflects the value.
                // This init also runs in import worker processes, so workers pick up the on-disk
                // value here before any .tss import.
                ThemeRegistry.legacyThemePriority = UIToolkitProjectSettings.enableLegacyThemePriority;
                ThemeRegistry.RegisterCustomDependencies();
                RegisterSerializationLayoutDependency();
                UnityEngine.UIElements.UIElementsInitialization.InitializeUIElementsManaged();
                VisualTreeAssetHierarchyDropHandler.Register();

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


