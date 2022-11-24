// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering;

[Obsolete($"{nameof(ScriptableRenderPipelineExtensionAttribute)} is deprecated. Use {nameof(SupportedOnRenderPipelineAttribute)} instead. #from(23.1) (UnityUpgradable) -> UnityEngine.Rendering.SupportedOnRenderPipelineAttribute", false)]
[AttributeUsage(AttributeTargets.Class)]
public class ScriptableRenderPipelineExtensionAttribute : Attribute
{
    internal Type renderPipelineType;

    public ScriptableRenderPipelineExtensionAttribute(Type rpAssetType)
    {
        if (!(rpAssetType?.IsSubclassOf(typeof(RenderPipelineAsset)) ?? false))
            throw new ArgumentException($"Given {nameof(rpAssetType)} must derive from {nameof(RenderPipelineAsset)}");
        renderPipelineType = rpAssetType;
    }

    [Obsolete($"ScriptableRenderPipelineExtensionAttribute.inUse is deprecated. Use SupportedOnRenderPipelineAttribute.isSupportedOnCurrentPipeline instead. #from(23.1) (UnityUpgradable) -> UnityEngine.Rendering.SupportedOnRenderPipelineAttribute.isSupportedOnCurrentPipeline", false)]
    public bool inUse
        => GraphicsSettings.currentRenderPipelineAssetType == renderPipelineType;
}
