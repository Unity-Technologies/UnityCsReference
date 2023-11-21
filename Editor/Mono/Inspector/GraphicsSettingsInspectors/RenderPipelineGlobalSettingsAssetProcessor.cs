// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering;

namespace UnityEditor.Mono.Inspector.GraphicsSettingsInspectors
{
    internal class RenderPipelineGlobalSettingsAssetProcessor : AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (!EditorWindow.HasOpenInstances<ProjectSettingsWindow>())
                return AssetDeleteResult.DidNotDelete;

            var settingsWindow = EditorWindow.GetWindow<ProjectSettingsWindow>(null, false);
            if (settingsWindow.GetCurrentProvider() is not GraphicsSettingsProvider currentProvider)
                return AssetDeleteResult.DidNotDelete;

            foreach (var globalSetting in currentProvider.inspector.globalSettings)
            {
                if (string.Compare(globalSetting.path, assetPath, StringComparison.InvariantCultureIgnoreCase) != 0)
                    continue;

                settingsWindow.m_Parent.Reload(settingsWindow);;
                return AssetDeleteResult.DidNotDelete;
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
