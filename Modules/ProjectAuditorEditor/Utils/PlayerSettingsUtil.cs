// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class PlayerSettingsUtil
    {
        internal static SerializedObject GetPlayerSettingsSerializedObject()
        {
            var internalGetPlayerSettingsMethod = typeof(PlayerSettings).GetMethod("InternalGetPlayerSettingsObject",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (internalGetPlayerSettingsMethod == null)
                return null;

            var playerSettings = internalGetPlayerSettingsMethod.Invoke(null, null);
            if (playerSettings == null)
                return null;

            return new SerializedObject(playerSettings as UnityEngine.Object);
        }

        public static int GetArchitecture(BuildTargetGroup buildTargetGroup)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            return PlayerSettings.GetArchitecture(namedBuildTarget);
        }

        public static Il2CppCompilerConfiguration GetIl2CppCompilerConfiguration(BuildTargetGroup buildTargetGroup)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            return PlayerSettings.GetIl2CppCompilerConfiguration(namedBuildTarget);
        }

        public static void SetIl2CppCompilerConfiguration(BuildTargetGroup buildTargetGroup, Il2CppCompilerConfiguration configuration)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.SetIl2CppCompilerConfiguration(namedBuildTarget, configuration);
        }

        public static ScriptingImplementation GetScriptingBackend(BuildTargetGroup buildTargetGroup)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            return PlayerSettings.GetScriptingBackend(namedBuildTarget);
        }

        public static void SetScriptingBackend(
            BuildTargetGroup buildTargetGroup,
            ScriptingImplementation backend)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            PlayerSettings.SetScriptingBackend(namedBuildTarget, backend);
        }

        public static ManagedStrippingLevel GetManagedStrippingLevel(BuildTargetGroup buildTargetGroup)
        {
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            return PlayerSettings.GetManagedStrippingLevel(namedBuildTarget);
        }

        public static int GetVertexChannelCompressionMask()
        {
            var serializedSettings = GetPlayerSettingsSerializedObject();

            var compressionFlagsProperty = serializedSettings.FindProperty("VertexChannelCompressionMask");
            if (compressionFlagsProperty == null)
                return 0;

            return compressionFlagsProperty.intValue;
        }

        public static void SetVertexChannelCompressionMask(int newValue)
        {
            var serializedSettings = GetPlayerSettingsSerializedObject();

            var compressionFlagsProperty = serializedSettings.FindProperty("VertexChannelCompressionMask");
            if (compressionFlagsProperty == null)
                return;

            serializedSettings.Update();

            compressionFlagsProperty.intValue = newValue;

            serializedSettings.ApplyModifiedProperties();
        }

        public static bool IsStaticBatchingEnabled(BuildTarget platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("Getting batching per platform is not supported");

            const int staticBatching = 0;
            const int dynamicBatching = 0;
            var args = new object[]
            {
                platform,
                staticBatching,
                dynamicBatching
            };

            method.Invoke(null, args);
            return (int)args[1] > 0;
        }

        public static void SetStaticBatchingEnabled(BuildTarget platform, bool enabled)
        {
            var setterMethod = typeof(PlayerSettings).GetMethod("SetBatchingForPlatform",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);

            if (setterMethod != null)
            {
                var setterArgs = new object[]
                {
                    platform,
                    enabled ? 1 : 0,
                    0
                };

                setterMethod.Invoke(null, setterArgs);
            }
        }

        public static bool IsLightmapStreamingEnabled(BuildTargetGroup platform)
        {
            var method = typeof(PlayerSettings).GetMethod("GetLightmapStreamingEnabledForPlatformGroup",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("Getting Lightmap Streaming per platform is not supported");

            var returnValue = method.Invoke(null, new object[] {platform});

            if (returnValue == null)
                throw new NotSupportedException("Getting Lightmap Streaming per platform is not supported");

            return (bool)returnValue;
        }

        public static void SetLightmapStreaming(BuildTargetGroup platform, bool value)
        {
            var method = typeof(PlayerSettings).GetMethod("SetLightmapStreamingEnabledForPlatformGroup",
                BindingFlags.Static | BindingFlags.Default | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("Setting Lightmap Streaming per platform is not supported");

            method.Invoke(null, new object[] {platform, value});
        }

        public static void GetDefaultTextureCompressionFormat(BuildTargetGroup buildTargetGroup,
            out int formatEnumIndex, out Array formatEnumValues)
        {
            formatEnumValues = default;
            formatEnumIndex = -1;

            var method = typeof(PlayerSettings).GetMethod("GetDefaultTextureCompressionFormat",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
                throw new NotSupportedException("PlayerSettings.GetDefaultTextureCompressionFormat method is not supported");

            var format = method.Invoke(null, new object[] { buildTargetGroup });

            var enumType = format.GetType();
            formatEnumValues = Enum.GetValues(enumType);
            formatEnumIndex = (int)format;
        }
    }
}
