// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Runtime/Graphics/Mesh/StaticBatching.h")]
    internal struct StaticBatchingEditorHelper
    {
        [FreeFunction("StaticBatching::CombineAllStaticMeshesInSceneForStaticBatching")]
        extern internal static void CombineAllStaticMeshesForScenePostProcessing(ulong sceneHash, UnityEngine.SceneManagement.Scene scene);
    }
}
