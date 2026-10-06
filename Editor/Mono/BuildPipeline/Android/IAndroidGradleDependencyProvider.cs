// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build;
using UnityEngine.Bindings;

namespace UnityEditor.Android
{
    // Both are called on the main thread before the Gradle project is generated.
    [VisibleToOtherModules]
    internal interface IAndroidGradleDependencyProvider : IOrderedCallback
    {
        // group:artifact:version
        string[] OnCollectGradleDependencies(AndroidGradleDependencyProviderContext context);

        // Maven repository URLs the dependencies resolve from.
        string[] OnCollectMavenRepositories(AndroidGradleDependencyProviderContext context);
    }

    [VisibleToOtherModules]
    internal struct AndroidGradleDependencyProviderContext
    {
        public BuildTarget platform { get; }

        public AndroidGradleDependencyProviderContext(BuildTarget platform)
        {
            this.platform = platform;
        }
    }
}
