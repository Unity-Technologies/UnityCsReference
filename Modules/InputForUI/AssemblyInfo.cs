// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

// Input System provider
[assembly: InternalsVisibleTo("Unity.InputSystem.ForUI")]
[assembly: InternalsVisibleTo("Unity.InputSystem.Tests")]
[assembly: InternalsVisibleTo("Unity.InputSystem.IntegrationTests")]

// UITK user
[assembly: InternalsVisibleTo("UnityEngine.UIElementsModule")]

// XR integration sending events manually
[assembly: InternalsVisibleTo("Unity.XR.Interaction.Toolkit")]
[assembly: InternalsVisibleTo("Unity.XR.Interaction.Toolkit.Editor")]
[assembly: InternalsVisibleTo("Unity.XR.Interaction.Toolkit.Samples.StarterAssets")]
[assembly: InternalsVisibleTo("Unity.XR.Interaction.Toolkit.Samples.StarterAssets.Editor")]
[assembly: InternalsVisibleTo("Unity.XR.Interaction.Toolkit.Samples.UIToolkit")]

// Input testing
[assembly: InternalsVisibleTo("UnityEngine.InputForUITests")]
[assembly: InternalsVisibleTo("UnityEngine.InputForUIVisualizer")]

// UITK testing
[assembly: InternalsVisibleTo("Unity.UIElements.Tests")]
[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")]

// other testing
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
