// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// OK: "friend" and test assemblies from the same product area, evolving in lockstep with this module

[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]
[assembly: InternalsVisibleTo("Unity.UIElements.Tests")]
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")]
[assembly: InternalsVisibleTo("EditorGUI.Tests.Playmode")]
[assembly: InternalsVisibleTo("Unity.PerformanceTesting.IMGUI")]
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")] // TextGenerator runtime tests use RuntimeTextSettings
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Base")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Bindings")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Controls")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Utils")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.UXML")]

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // for Moq

[assembly: InternalsVisibleTo("UnityEditor.CoreModule")]
[assembly: InternalsVisibleTo("UnityEditor.StyleSheetsModule")]
[assembly: InternalsVisibleTo("UnityEditor.UIElementsModule")]
[assembly: InternalsVisibleTo("UnityEditor.UIBuilderModule")]

[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.EditorResources.Authoring")]
[assembly: InternalsVisibleTo("Unity.UIElements.EditorResources.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UI.Builder.EditorTests")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
[assembly: InternalsVisibleTo("Unity.UI.TestFramework.Editor")]
[assembly: InternalsVisibleTo("Unity.Modules.InputForUI.Tests.Editor")] // GUIUtility.processEvent
[assembly: InternalsVisibleTo("Unity.Modules.Core.InspectorFramework.Tests.Editor")] // GUIContent.Temp

// TOLERATED: modules or core packages evolving in lockstep with this module
// Reducing this list means to improve the API design of this module.

[assembly: InternalsVisibleTo("UnityEditor.AnimationWindowModule")] // GUIClip, GUIUtility.GetPermanentControlID, SDFStyleScope
[assembly: InternalsVisibleTo("UnityEditor.ClothModule")] // GUILayoutUtility.topLevel
[assembly: InternalsVisibleTo("UnityEditor.DeviceSimulatorModule")] // GUI.blitMaterial
[assembly: InternalsVisibleTo("UnityEditor.DiagnosticsModule")] // GUIContent.Temp
[assembly: InternalsVisibleTo("UnityEditor.GraphViewModule")] // GUIUtility.RoundToPixelGrid
[assembly: InternalsVisibleTo("UnityEditor.Graphs")] // GUIClip.Clip/Unclip, GUIContent.Temp, SDFStyleScope
[assembly: InternalsVisibleTo("UnityEditor.Physics2DModule")] // GUIClip.topmostRect
[assembly: InternalsVisibleTo("UnityEditor.PresetsUIModule")] // GUIContent.Temp
[assembly: InternalsVisibleTo("UnityEditor.QuickSearchModule")] // GUIClip.Unclip/enabled, GUIContent.Temp
[assembly: InternalsVisibleTo("UnityEditor.SketchUpModule")] // GUIContent.Temp
[assembly: InternalsVisibleTo("UnityEditor.TerrainModule")] // GUIUtility.mouseUsed
[assembly: InternalsVisibleTo("UnityEditor.VideoModule")] // GUIContent.Temp

[assembly: InternalsVisibleTo("UnityEditor.Android.Extensions")] // GUIContent.Temp

[assembly: InternalsVisibleTo("Unity.Modules.Core.TextureMipLimit.Tests.Editor")] // GUIUtility.pixelsPerPoint

// NOT TOLERATED: assemblies distributed in packages not evolving in lockstep with this module
// Until this list is empty, your internal API is included in your public API, and changing internal APIs is considered a breaking change.

[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.001")] // com.unity.2d.common: GUIClip.visibleRect/topmostRect/GetTopRect
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.002")] // com.unity.entities: GUIUtility.pixelsPerPoint

[assembly: InternalsVisibleTo("Unity.2D.Sprite.Editor")] // com.unity.2d.sprite: GUIClip.Push/Pop/Unclip, GUIUtility.GetPermanentControlID
[assembly: InternalsVisibleTo("Unity.2D.Tilemap.Editor")] // com.unity.2d.tilemap: GUIClip.Unclip, GUIContent.Temp, GUIUtility.GetPermanentControlID
[assembly: InternalsVisibleTo("Unity.Timeline.Editor")] // com.unity.timeline: GUIClip.Clip/Unclip, GUIContent.Temp, GUISkin.current, GUIUtility.guiDepth/GetPermanentControlID
[assembly: InternalsVisibleTo("Unity.Timeline.EditorTests")] // com.unity.timeline tests: GUIClip.Unclip, GUISkin.error
[assembly: InternalsVisibleTo("Unity.Motion.Editor.AnimationWindow")] // com.unity.motion (external repository)
