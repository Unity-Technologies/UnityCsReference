// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // StaticOcclusionCulling lets you perform static occlusion culling operations
    [NativeHeader("Runtime/Camera/OcclusionCullingSettings.h")]
    [NativeHeader("Runtime/Camera/RendererScene.h")]
    [NativeHeader("Editor/Src/OcclusionCulling.h")]
    public static class StaticOcclusionCulling
    {
        // Used to generate static occlusion culling data. This function will not return until occlusion data is generated.
        [NativeName("GenerateTome")]
        public static extern bool Compute();

        // Used to compute static occlusion culling data asynchronously.
        [NativeName("GenerateTomeInBackground")]
        public static extern bool GenerateInBackground();

        // Used to invalidate preVisualistion debug data.
        internal static extern void InvalidatePrevisualisationData();

        // Used to cancel asynchronous generation of static occlusion culling data.
        public static extern void Cancel();

        // Used to check if asynchronous generation of static occlusion culling data is still running.
        public static extern bool isRunning
        {
            [NativeName("IsRunning")]
            get;
        }

        // Clears the Tome of the opened scene
        [NativeName("ClearUmbraTome")]
        public static extern void Clear();

        // Get the OcclusionCullingSettings
        internal static extern Object occlusionCullingSettings
        {
            [FreeFunction]
            get;
        }

        [StaticAccessor("GetOcclusionCullingSettings()", StaticAccessorType.Dot)]
        [NativeProperty(TargetType = TargetType.Field)]
        public static extern float smallestOccluder
        {
            [NativeName("GetOcclusionBakeSettings().smallestOccluder")]
            get;
            [NativeName("GetOcclusionBakeSettingsSetDirty().smallestOccluder")]
            set;
        }

        [StaticAccessor("GetOcclusionCullingSettings()", StaticAccessorType.Dot)]
        [NativeProperty(TargetType = TargetType.Field)]
        public static extern float smallestHole
        {
            [NativeName("GetOcclusionBakeSettings().smallestHole")]
            get;
            [NativeName("GetOcclusionBakeSettingsSetDirty().smallestHole")]
            set;
        }

        [StaticAccessor("GetOcclusionCullingSettings()", StaticAccessorType.Dot)]
        [NativeProperty(TargetType = TargetType.Field)]
        public static extern float backfaceThreshold
        {
            [NativeName("GetOcclusionBakeSettings().backfaceThreshold")]
            get;
            [NativeName("GetOcclusionBakeSettingsSetDirty().backfaceThreshold")]
            set;
        }

        public static extern bool doesSceneHaveManualPortals
        {
            [NativeName("DoesSceneHaveManualPortals")]
            get;
        }

        // Returns the size in bytes that the Tome data is currently taking up in this scene on disk
        [StaticAccessor("GetRendererScene()", StaticAccessorType.Dot)]
        public static extern int umbraDataSize { get; }

        [StaticAccessor("GetOcclusionCullingSettings()", StaticAccessorType.Dot)]
        public static extern void SetDefaultOcclusionBakeSettings();
    }

    // Used to visualize static occlusion culling at development time in scene view.
    [StaticAccessor("GetOcclusionCullingVisualization()", StaticAccessorType.Arrow)]
    [NativeHeader("Editor/Src/OcclusionCullingVisualizationState.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    public static class StaticOcclusionCullingVisualization
    {
        // If set to true, visualization of target volumes is enabled.
        public static extern bool showOcclusionCulling { get; set; }

        // If set to true, the visualization lines of the PVS volumes will show all cells rather than cells after culling.
        [NativeName("ShowPreVis")]
        public static extern bool showPreVisualization { get; set; }

        // If set to true, visualization of view volumes is enabled.
        public static extern bool showViewVolumes { get; set; }

        public static extern bool showDynamicObjectBounds { get; set; }

        // If set to true, visualization of portals is enabled.
        public static extern bool showPortals { get; set; }

        // If set to true, visualization of portals is enabled.
        public static extern bool showVisibilityLines { get; set; }

        // If set to true, culling of geometry is enabled.
        public static extern bool showGeometryCulling { get; set; }

        public static extern bool isPreviewOcclusionCullingCameraInPVS
        {
            [FreeFunction("IsPreviewOcclusionCullingCameraInPVS")]
            get;
        }

        public static extern Camera previewOcclusionCamera
        {
            [FreeFunction("FindPreviewOcclusionCamera")]
            get;
        }

        //*undoc*
        // This is here because it was released on 3.0 (this is a typo)
        public static extern Camera previewOcclucionCamera
        {
            [FreeFunction("FindPreviewOcclusionCamera")]
            get;
        }
    }
}
