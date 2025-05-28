// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// OK: "friend" and test assemblies from the same product area, evolving in lockstep with this module

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")] // Performance Tests

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // for Moq

[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")] // For editor-only playmode tests
[assembly: InternalsVisibleTo("Unity.UI.TestFramework.Editor")] // for UI Test Framework
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Base")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Bindings")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Controls")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.StyleSheets")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.Utils")]
[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests.UXML")]

[assembly: InternalsVisibleTo("Unity.UIElements.TestComponents.Editor")]
[assembly: InternalsVisibleTo("UnityEditor.UIElementsSamplesModule")]

[assembly: InternalsVisibleTo("UnityEditor.UIBuilderModule")]
[assembly: InternalsVisibleTo("Unity.UI.Builder.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UXMLReferenceGenerator.Bridge")]

[assembly: InternalsVisibleTo("UnityEngine.UIElements.Tests")] // for UI Test Framework

// TOLERATED: modules or core packages evolving in lockstep with this module
// Reducing this list means to improve the API design of this module.

[assembly: InternalsVisibleTo("UnityEditor.PresetsUIModule")]
