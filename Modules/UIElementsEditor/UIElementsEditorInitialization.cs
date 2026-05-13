// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.Profiling;
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
                UnityEngine.UIElements.UIElementsInitialization.InitializeUIElementsManaged();
                VisualTreeAssetHierarchyDropHandler.Register();

                UIToolkitProjectSettings.CaptureBootValues();
                if (UIToolkitProjectSettings.enablePanelRendererAnimation)
                    UnityEngine.UIElements.PanelRenderer.RegisterPanelRendererAnimationBinding();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}


