// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Modules with broad use of Event's internals (marshalling, event queue, Event.current seams)
[assembly: InternalsVisibleTo("UnityEngine.IMGUIModule")]
[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]
[assembly: InternalsVisibleTo("UnityEngine.InputForUIModule")]
[assembly: InternalsVisibleTo("UnityEditor.CoreModule")] // EventCommandNames, Event.CopyFromPtr, Event.isDirectManipulationDevice
[assembly: InternalsVisibleTo("UnityEditor.Graphs")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.AnimationWindowModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.ClothModule")] // EventCommandNames.ModifierKeysChanged
[assembly: InternalsVisibleTo("UnityEditor.GraphToolkitModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.GraphViewModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.HierarchyModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.PackageManagerUIModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.ParticleSystemModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.ProfilerModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.QuickSearchModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.ShortcutManagerModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.UIBuilderModule")] // EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.UIToolkitAuthoringModule")] // EventCommandNames

// Packages and bridge assemblies
[assembly: InternalsVisibleTo("Unity.2D.Sprite.Editor")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.2D.Tilemap.Editor")] // EventCommandNames.FrameSelected
[assembly: InternalsVisibleTo("Unity.Timeline.Editor")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.Editor")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.002")] // EventCommandNames (com.unity.entities)
[assembly: InternalsVisibleTo("Unity.InputSystem")] // Event.scrollWheelDeltaPerTick
[assembly: InternalsVisibleTo("Unity.InputSystem.ForUI")] // Event.scrollWheelDeltaPerTick
[assembly: InternalsVisibleTo("Unity.InputSystem.TestFramework")] // Event.scrollWheelDeltaPerTick
[assembly: InternalsVisibleTo("Unity.XR.Interaction.Toolkit")] // Event.scrollWheelDeltaPerTick
[assembly: InternalsVisibleTo("Unity.UI.TestFramework.Editor")] // EventCommandNames

// Test assemblies that reach Event's internals
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.UIElements.Tests")] // Event.QueueEvent, Event.ClearEvents
[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")] // EventCommandNames, Event.GetDoubleClickTime
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")] // Event.ClearEvents
[assembly: InternalsVisibleTo("Unity.Modules.InputForUI.Tests.Editor")] // Event.GetEventAtIndex
[assembly: InternalsVisibleTo("Unity.Modules.InputForUI.Tests.Playmode")] // Event.QueueEvent, Event.ClearEvents
[assembly: InternalsVisibleTo("Unity.Modules.GraphToolkit.Internal.Tests.Editor")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.Modules.CoreEditor.SceneHierarchy.Tests.Editor")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.Hierarchy.Editor.Tests")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.Timeline.EditorTests")] // EventCommandNames
[assembly: InternalsVisibleTo("Unity.InputSystem.Tests")] // Event.scrollWheelDeltaPerTick
