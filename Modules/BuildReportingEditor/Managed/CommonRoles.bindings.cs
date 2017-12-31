// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    namespace Build.Reporting
    {
        [NativeType(Header = "Modules/BuildReportingEditor/Public/CommonRoles.h")]
        public static class CommonRoles
        {
            [NativeProperty("BuildReporting::CommonRoles::scene", true, TargetType.Field)]
            public static extern string scene { get; }

            [NativeProperty("BuildReporting::CommonRoles::sharedAssets", true, TargetType.Field)]
            public static extern string sharedAssets { get; }

            [NativeProperty("BuildReporting::CommonRoles::resourcesFile", true, TargetType.Field)]
            public static extern string resourcesFile { get; }

            [NativeProperty("BuildReporting::CommonRoles::assetBundle", true, TargetType.Field)]
            public static extern string assetBundle { get; }

            [NativeProperty("BuildReporting::CommonRoles::manifestAssetBundle", true, TargetType.Field)]
            public static extern string manifestAssetBundle { get; }

            [NativeProperty("BuildReporting::CommonRoles::assetBundleTextManifest", true, TargetType.Field)]
            public static extern string assetBundleTextManifest { get; }

            [NativeProperty("BuildReporting::CommonRoles::managedLibrary", true, TargetType.Field)]
            public static extern string managedLibrary { get; }

            [NativeProperty("BuildReporting::CommonRoles::dependentManagedLibrary", true, TargetType.Field)]
            public static extern string dependentManagedLibrary { get; }

            [NativeProperty("BuildReporting::CommonRoles::executable", true, TargetType.Field)]
            public static extern string executable { get; }

            [NativeProperty("BuildReporting::CommonRoles::streamingResourceFile", true, TargetType.Field)]
            public static extern string streamingResourceFile { get; }

            [NativeProperty("BuildReporting::CommonRoles::streamingAsset", true, TargetType.Field)]
            public static extern string streamingAsset { get; }

            [NativeProperty("BuildReporting::CommonRoles::bootConfig", true, TargetType.Field)]
            public static extern string bootConfig { get; }

            [NativeProperty("BuildReporting::CommonRoles::builtInResources", true, TargetType.Field)]
            public static extern string builtInResources { get; }

            [NativeProperty("BuildReporting::CommonRoles::builtInShaders", true, TargetType.Field)]
            public static extern string builtInShaders { get; }

            [NativeProperty("BuildReporting::CommonRoles::appInfo", true, TargetType.Field)]
            public static extern string appInfo { get; }

            [NativeProperty("BuildReporting::CommonRoles::managedEngineAPI", true, TargetType.Field)]
            public static extern string managedEngineApi { get; }

            [NativeProperty("BuildReporting::CommonRoles::monoRuntime", true, TargetType.Field)]
            public static extern string monoRuntime { get; }

            [NativeProperty("BuildReporting::CommonRoles::monoConfig", true, TargetType.Field)]
            public static extern string monoConfig { get; }

            [NativeProperty("BuildReporting::CommonRoles::debugInfo", true, TargetType.Field)]
            public static extern string debugInfo { get; }

            [NativeProperty("BuildReporting::CommonRoles::globalGameManagers", true, TargetType.Field)]
            public static extern string globalGameManagers { get; }

            [NativeProperty("BuildReporting::CommonRoles::crashHandler", true, TargetType.Field)]
            public static extern string crashHandler { get; }

            [NativeProperty("BuildReporting::CommonRoles::engineLibrary", true, TargetType.Field)]
            public static extern string engineLibrary { get; }
        }
    }
}
