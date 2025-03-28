// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// OK: "friend" and test assemblies from the same product area, evolving in lockstep with this module

[assembly: InternalsVisibleTo("Unity.UIElements.Tests")]
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.TestComponents")]
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]

[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests")] // for UI Test Framework
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.InternalAccessTests")] // for UI Test Framework tests that need internal access

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // for Moq

[assembly: InternalsVisibleTo("UnityEditor.StyleSheetsModule")]
[assembly: InternalsVisibleTo("UnityEditor.UIBuilderModule")]
[assembly: InternalsVisibleTo("UnityEditor.UIElementsModule")]

[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.TestComponents.Editor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
[assembly: InternalsVisibleTo("Unity.UI.Builder.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UXMLReferenceGenerator.Bridge")]
[assembly: InternalsVisibleTo("UnityEditor.UIElements.Tests")] // for UI Test Framework
[assembly: InternalsVisibleTo("UnityEditor.UIElements.Tests.InternalAccessTests")] // for UI Test Framework tests that need internal access


// TOLERATED: modules or core packages evolving in lockstep with this module
// Reducing this list means to improve the API design of this module.

[assembly: InternalsVisibleTo("UnityEngine.UI")] // com.unity.ugui

[assembly: InternalsVisibleTo("UnityEditor.CoreModule")]
[assembly: InternalsVisibleTo("UnityEditor.EditorToolbarModule")]
[assembly: InternalsVisibleTo("UnityEditor.GraphViewModule")]
[assembly: InternalsVisibleTo("UnityEditor.Graphs")]
[assembly: InternalsVisibleTo("UnityEditor.GridAndSnapModule")] // ButtonStripField
[assembly: InternalsVisibleTo("UnityEditor.PresetsModule")]
[assembly: InternalsVisibleTo("UnityEditor.PresetsUIModule")]
[assembly: InternalsVisibleTo("UnityEditor.QuickSearchModule")]
[assembly: InternalsVisibleTo("UnityEditor.SceneTemplateModule")]
[assembly: InternalsVisibleTo("UnityEditor.SceneViewModule")]
[assembly: InternalsVisibleTo("UnityEditor.UnityConnectModule")]
[assembly: InternalsVisibleTo("UnityEditor.UIElementsSamplesModule")]
[assembly: InternalsVisibleTo("UnityEditor.UI.EditorTests")]

[assembly: InternalsVisibleTo("Unity.2D.Sprite.Editor")] // com.unity.2d.sprite: VisualElement.styleSheetList, FocusController.IsFocused
[assembly: InternalsVisibleTo("Unity.2D.Tilemap.Editor")] // com.unity.2d.tilemap: IGenericMenu
[assembly: InternalsVisibleTo("Unity.2D.Tilemap.EditorTests")] // com.unity.2d.tilemap.tests: UIElementsUtility

// NOT TOLERATED: assemblies distributed in packages not evolving in lockstep with this module
// Until this list is empty, your internal API is included in your public API, and changing internal APIs is considered a breaking change.

[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.001")] // com.unity.2d.common: VisualElement.pseudoStates, PseudoStates
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.002")] // com.unity.entities: VisualElementBridge.cs, ListViewBridge.cs
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.003")] // com.unity.vectorgraphics: VectorImage, GradientSettings
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.017")] // com.unity.motion: UIElementsUtility

[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.015")] // Eventually remove this line. Kept for earlier, unreleased versions of com.unity.graphtoolsfoundation, which is now com.unity.graphtoolsauthoringframework (line below).
[assembly: InternalsVisibleTo("Unity.GraphToolsAuthoringFramework.InternalBridge")] // com.unity.graphtoolsauthoringframework


[assembly: InternalsVisibleTo("UnityEditor.Purchasing")] // com.unity.purchasing, VisualElement.AddStyleSheetPath
[assembly: InternalsVisibleTo("Unity.GraphToolsAuthoringFramework.InternalEditorBridge")]

