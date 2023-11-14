// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace UnityEditor.AI
{
    [NativeHeader("Modules/AIEditor/Utilities/NavMeshEditorHelpers.bindings.h")]
    public static partial class NavMeshEditorHelpers
    {
        [FreeFunction("NavMeshEditorHelpersBindings::CollectSourcesInStageInternal")]
        static extern NavMeshBuildSource[] CollectSourcesInStageInternal(
            int includedLayerMask, Bounds includedWorldBounds, Transform root, bool useBounds, NavMeshCollectGeometry geometry,
            int defaultArea, bool generateLinksByDefault, NavMeshBuildMarkup[] markups, bool includeOnlyMarkedObjects, Scene stageProxy);
    }
}
