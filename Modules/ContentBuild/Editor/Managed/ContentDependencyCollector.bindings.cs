// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Loading;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine;

namespace UnityEditor.Build.Content
{
    [StructLayout(LayoutKind.Sequential)]
    struct ContentDependencyCollectResult
    {
        public EntityId[] references;
        public LoadableObjectId[] loadableObjectIds;
        public LoadableSceneId[] loadableSceneIds;
    }

    [NativeHeader("Modules/ContentBuild/Editor/Public/ContentDependencyCollector.h")]
    static class ContentDependencyCollectorUtility
    {
        [FreeFunction("ContentDependencyCollectUtility_Collect")]
        extern public static ContentDependencyCollectResult Collect(EntityId objects, DependencyType mode);
    }
}
