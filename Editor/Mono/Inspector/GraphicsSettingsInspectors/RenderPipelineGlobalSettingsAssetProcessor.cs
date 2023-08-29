// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.Inspector.GraphicsSettingsInspectors;

namespace UnityEditor.Mono.Inspector.GraphicsSettingsInspectors
{
    internal class RenderPipelineGlobalSettingsAssetProcessor : AssetModificationProcessor
    {
        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (GraphicsSettingsInspector.s_Instance == null)
                return AssetDeleteResult.DidNotDelete;

            var globalSettings = GraphicsSettingsInspector.s_Instance.globalSettings;
            foreach (var globalSetting in globalSettings)
            {
                if (string.Compare(globalSetting.path, assetPath, StringComparison.InvariantCultureIgnoreCase) != 0)
                    continue;

                GraphicsSettingsUtils.ReloadGraphicsSettingsEditor();
                return AssetDeleteResult.DidNotDelete;
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
