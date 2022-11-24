// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor;

[Obsolete($"{nameof(LightingExplorerExtensionAttribute)} is deprecated. Use {nameof(SupportedOnRenderPipelineAttribute)} instead. #from(23.1) (UnityUpgradable) -> UnityEngine.Rendering.SupportedOnRenderPipelineAttribute", false)]
[AttributeUsage(AttributeTargets.Class)]
public class LightingExplorerExtensionAttribute : ScriptableRenderPipelineExtensionAttribute
{
    public LightingExplorerExtensionAttribute(Type renderPipeline)
        : base(renderPipeline) {}
}
