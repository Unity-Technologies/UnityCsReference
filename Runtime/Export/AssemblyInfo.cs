// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

//TODO: Think about if this is secure or not, and if we should be using UnityEditor's public key instead.
//I think we're cool, because we ifdef it out in the version of UnityEngine.dll that we ship with the webplayer.
[assembly: InternalsVisibleTo("UnityEngine.Advertisements")]
[assembly: InternalsVisibleTo("UnityEditor.Advertisements")]
[assembly: InternalsVisibleTo("UnityEditor.Analytics")]
[assembly: InternalsVisibleTo("UnityEditor.Purchasing")]
[assembly: InternalsVisibleTo("UnityEditor")]
[assembly: InternalsVisibleTo("Unity.PureCSharpTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // for Moq
[assembly: InternalsVisibleTo("UnityEditor.Graphs")]
[assembly: InternalsVisibleTo("UnityEditor.WiiU.Extensions")]
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
[assembly: InternalsVisibleTo("UnityEditor.iOS.Extensions.Common")]

[assembly: InternalsVisibleTo("UnityEngine.Physics")]
[assembly: InternalsVisibleTo("UnityEngine.Terrain")]
[assembly: InternalsVisibleTo("UnityEngine.TerrainPhysics")]
[assembly: InternalsVisibleTo("UnityEngine.Networking")]
[assembly: InternalsVisibleTo("UnityEngine.Cloud")]
[assembly: InternalsVisibleTo("UnityEngine.Cloud.Service")]
[assembly: InternalsVisibleTo("UnityEngine.Analytics")]
[assembly: InternalsVisibleTo("UnityEngine.Advertisements")]
[assembly: InternalsVisibleTo("UnityEngine.Purchasing")]
[assembly: InternalsVisibleTo("Unity.Automation")]
[assembly: InternalsVisibleTo("Unity.DeploymentTests.Services")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests.UnityAnalytics")]
[assembly: InternalsVisibleTo("Unity.IntegrationTests.Framework")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests.Framework")]
[assembly: InternalsVisibleTo("Unity.RuntimeTests.Framework.Tests")]

// Note: Don't add InternalsVisibleTo for UnityEngine.UI, because it's editable by users and it shouldn't access internal UnityEngine methods


