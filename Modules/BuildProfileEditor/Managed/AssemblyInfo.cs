// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.BuildProfileModule.Tests")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]

[assembly: InternalsVisibleTo("Unity.Modules.BuildProfile.Tests.Common")]
[assembly: InternalsVisibleTo("Unity.Modules.BuildProfile.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Modules.BuildProfileEditor.Tests.Editor")]
[assembly: InternalsVisibleTo("Unity.Modules.AdaptivePerformanceEditor.Tests.Editor")]

[assembly: InternalsVisibleTo("Unity.Insights.Editor.Tests")]

[assembly: InternalsVisibleTo("UnityEditor.Rendering.ShaderBuildSettings.Tests")]

// Platform editor extensions that reach into BuildProfileModule internals: TrText for the build
// button labels, BuildProfileWindow for repaints. Mono did not enforce these accesses, CoreCLR does.
[assembly: InternalsVisibleTo("UnityEditor.Android.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.PS4.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.PS5.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.QNX.Extensions")]
[assembly: InternalsVisibleTo("UnityEditor.WebGL.Extensions")]
