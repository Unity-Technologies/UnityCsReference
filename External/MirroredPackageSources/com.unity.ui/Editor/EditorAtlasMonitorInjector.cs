using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.UIElements
{
    // This class was created in an attempt to move the atlas monitor to the com.unity.ui package. However, at the
    // moment of writing these lines, the package cannot contain editor classes. So the atlas monitor has been moved
    // and this class injects the required editor callbacks. Once assembly override is complete:
    // a) Remove this class
    // b) Complete the TODOs in EditorAtlasMonitor.cs
    // c) Move EditorAtlasMonitor.cs under and Editor directory.
    [InitializeOnLoad]
    class EditorAtlasMonitorInjector : UnityEditor.AssetPostprocessor
    {
        static EditorAtlasMonitorInjector()
        {
            EditorAtlasMonitorBridge.StaticInit();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            EditorAtlasMonitorBridge.OnPostprocessAllAssets?.Invoke(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        public void OnPostprocessTexture(Texture2D texture)
        {
            EditorAtlasMonitorBridge.OnPostprocessTexture?.Invoke(texture);
        }
    }
}
