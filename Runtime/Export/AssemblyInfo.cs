// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// NOTE: the build system includes this source file in ALL modules (Runtime and Editor)

using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: UnityEngine.UnityEngineModuleAssembly]

// required for the type forwarders
[assembly: InternalsVisibleTo("UnityEngine")]
[assembly: InternalsVisibleTo("UnityEditor")]

// TODO: over time, remove the InternalsVisibleTo attributes from this section
// To remove a line in there, the target assembly must not depend on _any_ internal API from built-in modules

[assembly: InternalsVisibleTo("UnityEngine.Advertisements")]
[assembly: InternalsVisibleTo("UnityEditor.Advertisements")]
[assembly: InternalsVisibleTo("Unity.Analytics.Editor")]
[assembly: InternalsVisibleTo("UnityEditor.Analytics")]
[assembly: InternalsVisibleTo("UnityEngine.UnityAnalyticsCommon")]
[assembly: InternalsVisibleTo("UnityEditor.Purchasing")]
[assembly: InternalsVisibleTo("Unity.PureCSharpTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // for Moq
[assembly: InternalsVisibleTo("UnityEditor.Graphs")]
[assembly: InternalsVisibleTo("UnityEditor.XboxOne.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.PS4.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.PS5.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.Switch.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.UWP.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.GameCoreScarlett.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.GameCoreXboxOne.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.GameCoreCommon.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.WindowsStandalone.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.WebGL.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.EmbeddedLinux.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.QNX.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.Kepler.Extensions")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-firstpass-testable")]
[assembly: InternalsVisibleTo("UnityEditor.iOS.Extensions.Common")]
[assembly: InternalsVisibleTo("UnityEditor.Apple.Extensions.Common")]
[assembly: InternalsVisibleTo("UnityEditor.VisionOS.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.Android.Extensions")]
[assembly: InternalsVisibleTo("Unity.AndroidBuildPipeline")]
[assembly: InternalsVisibleTo("UnityEditor.OSXStandalone.Extensions")]
[assembly: InternalsVisibleTo("Unity.Timeline.Editor")] // for Driven Properties
[assembly: InternalsVisibleTo("Unity.Timeline.EditorTests")]
[assembly: InternalsVisibleTo("UnityEditor.InteractiveTutorialsFramework")]
[assembly: InternalsVisibleTo("Unity.2D.Tilemap.Editor")]
[assembly: InternalsVisibleTo("Unity.2D.Tilemap.EditorTests")]
[assembly: InternalsVisibleTo("Unity.DeviceSimulator.Editor")]
[assembly: InternalsVisibleTo("Unity.UIElements")]
[assembly: InternalsVisibleTo("Unity.UIElements.Editor")]
[assembly: InternalsVisibleTo("UnityEditor.UIElementsModule")]
[assembly: InternalsVisibleTo("UnityEditor.UIElementsGameObjectsModule")]
[assembly: InternalsVisibleTo("Unity.UI.TestFramework.Editor")] // for UI Test Framework
[assembly: InternalsVisibleTo("Unity.Tiny.Rendering.Authoring")]
[assembly: InternalsVisibleTo("Unity.Tiny.Authoring")]
[assembly: InternalsVisibleTo("Unity.ImageConversionTests")]

// This should move with the AnimationWindow to a module at some point
[assembly: InternalsVisibleTo("UnityEditor.Modules.Animation.tests.AnimationWindow")]

[assembly: InternalsVisibleTo("Unity.Motion.Editor.AnimationWindow")]

[assembly: InternalsVisibleTo("UnityEngine.Networking")]
[assembly: InternalsVisibleTo("UnityEngine.Cloud")]
[assembly: InternalsVisibleTo("UnityEngine.Cloud.Service")]
[assembly: InternalsVisibleTo("Unity.Analytics")]
[assembly: InternalsVisibleTo("UnityEngine.Analytics")]
[assembly: InternalsVisibleTo("UnityEngine.UnityAnalyticsCommon")]
[assembly: InternalsVisibleTo("UnityEngine.Advertisements")]
[assembly: InternalsVisibleTo("UnityEngine.Purchasing")]
[assembly: InternalsVisibleTo("UnityEngine.TestRunner")]
[assembly: InternalsVisibleTo("Unity.Automation")]
[assembly: InternalsVisibleTo("Unity.Burst")]
[assembly: InternalsVisibleTo("Unity.Burst.Editor")]
[assembly: InternalsVisibleTo("Unity.DeploymentTests.Services")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests.ExternalVersionControl")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests.UnityAnalytics")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests.Timeline")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests.Framework")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests.Framework.Tests")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests.Framework")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests.Framework.Tests")]
[assembly: InternalsVisibleTo("Unity.PerformanceTests.RuntimeTestRunner.Tests")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests.AllIn1Runner")]
[assembly: InternalsVisibleTo("Unity.Timeline")]
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]
[assembly: InternalsVisibleTo("Assembly-CSharp-firstpass-testable")]
[assembly: InternalsVisibleTo("UnityEngine.SpatialTracking")]
[assembly: InternalsVisibleTo("GoogleAR.UnityNative")]
[assembly: InternalsVisibleTo("Unity.WindowsMRAutomation")]
[assembly: InternalsVisibleTo("UnityEngine.SpriteShapeModule")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Universal.2D.Runtime")]
[assembly: InternalsVisibleTo("Unity.2D.Sprite.Editor")]
[assembly: InternalsVisibleTo("Unity.2D.Sprite.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UI.Builder.Editor")]
[assembly: InternalsVisibleTo("UnityEditor.UIBuilderModule")]
[assembly: InternalsVisibleTo("Unity.UI.Builder.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UIElements")]
[assembly: InternalsVisibleTo("UnityEngine.UIElementsGameObjectsModule")]
[assembly: InternalsVisibleTo("Unity.UIElements.Editor")]
[assembly: InternalsVisibleTo("Unity.UIElements.PlayModeTests")]
[assembly: InternalsVisibleTo("Unity.UI.TestFramework.Runtime")]

[assembly: InternalsVisibleTo("Unity.UIElements.EditorTests")]
[assembly: InternalsVisibleTo("Unity.UIElements.RuntimeTests")]
[assembly: InternalsVisibleTo("UnityEngine.UI")]

// Needed for Baselib CSharp binding access.
[assembly: InternalsVisibleTo("Unity.Networking.Transport")]
[assembly: InternalsVisibleTo("Unity.ucg.QoS")] // TODO(andrews): Remove this when we fix transport
[assembly: InternalsVisibleTo("Unity.Services.QoS")] // TODO: Remove this when we fix transport
[assembly: InternalsVisibleTo("Unity.Logging")]
[assembly: InternalsVisibleTo("Unity.Entities")]
[assembly: InternalsVisibleTo("Unity.Entities.Tests")]
[assembly: InternalsVisibleTo("Unity.Collections")]
[assembly: InternalsVisibleTo("Unity.Runtime")]
[assembly: InternalsVisibleTo("Unity.Core")]
[assembly: InternalsVisibleTo("UnityEngine.Core.Runtime.Tests")]

[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.001")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.002")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.003")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.004")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.005")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.006")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.007")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.008")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.009")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.010")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.011")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.012")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.013")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.014")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.015")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.016")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.017")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.018")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.019")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.020")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.021")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.022")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.023")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridge.024")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridgeDev.001")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridgeDev.002")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridgeDev.003")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridgeDev.004")]
[assembly: InternalsVisibleTo("Unity.InternalAPIEngineBridgeDev.005")]

[assembly: InternalsVisibleTo("Unity.Subsystem.Registration")]
// Note: Don't add InternalsVisibleTo for UnityEngine.UI, because it's editable by users and it shouldn't access internal UnityEngine methods


