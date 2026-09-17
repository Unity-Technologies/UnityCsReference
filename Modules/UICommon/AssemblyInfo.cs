// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Modules with broad use of Event's internals (marshalling, event queue, Event.current seams)
[assembly: InternalsVisibleTo("UnityEngine.IMGUIModule")]
[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]
[assembly: InternalsVisibleTo("UnityEngine.InputForUIModule")]
[assembly: InternalsVisibleTo("UnityEditor.CoreModule")] // Event.CopyFromPtr, Event.isDirectManipulationDevice, internal EventCommandNames
[assembly: InternalsVisibleTo("UnityEditor.ClothModule")] // EventCommandNames.ModifierKeysChanged
[assembly: InternalsVisibleTo("UnityEditor.ShortcutManagerModule")] // EventCommandNames.ModifierKeysChanged

// Packages and bridge assemblies
[assembly: InternalsVisibleTo("Unity.InputSystem")] // Event.scrollWheelDeltaPerTick
[assembly: InternalsVisibleTo("Unity.InputSystem.ForUI")] // Event.scrollWheelDeltaPerTick
[assembly: InternalsVisibleTo("Unity.InputSystem.TestFramework")] // Event.scrollWheelDeltaPerTick
[assembly: InternalsVisibleTo("Unity.XR.Interaction.Toolkit")] // Event.scrollWheelDeltaPerTick

// Test assemblies that reach Event's internals
[assembly: InternalsVisibleTo("Unity.UIElements.Tests")] // Event.QueueEvent, Event.ClearEvents
[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")] // Event.GetDoubleClickTime
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")] // Event.ClearEvents
[assembly: InternalsVisibleTo("Unity.Modules.InputForUI.Tests.Editor")] // Event.GetEventAtIndex
[assembly: InternalsVisibleTo("Unity.Modules.InputForUI.Tests.Playmode")] // Event.QueueEvent, Event.ClearEvents
[assembly: InternalsVisibleTo("Unity.InputSystem.Tests")] // Event.scrollWheelDeltaPerTick
