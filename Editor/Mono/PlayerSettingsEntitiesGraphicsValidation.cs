// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Build;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal static class PlayerSettingsEntitiesGraphicsValidation
    {
        private const string k_PackageName = "com.unity.entities.graphics";
        private const string k_DisableDefine = "DISABLE_ENTITIES_GRAPHICS_GLES_WARNING";

        static PlayerSettingsEntitiesGraphicsValidation()
        {
            ValidateOpenGLES3Deprecation();
        }

        private static void ValidateOpenGLES3Deprecation()
        {
            // Check if the warning is disabled via scripting define
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            var activeTargetGroup = BuildPipeline.GetBuildTargetGroup(activeTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(activeTargetGroup);
            var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            if (defines.Contains(k_DisableDefine))
                return;

            // Check if package is installed
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_PackageName);
            if (package == null)
                return;

            // Entities Graphics does not support Web platforms, so no need to check
            if (activeTarget != BuildTarget.WebGL)
            {
                var apis = PlayerSettings.GetGraphicsAPIs(activeTarget);
                if (apis != null && System.Array.IndexOf(apis, GraphicsDeviceType.OpenGLES3) >= 0)
                {
                    Debug.LogWarning("Support for OpenGL ES for Entities Graphics is deprecated, and will be removed in a future version of Entities Graphics. Update Graphics APIs in the Player Settings. " +
                        "This warning can be suppressed by adding the scripting define DISABLE_ENTITIES_GRAPHICS_GLES_WARNING. " +
                        "For more details, see: <a href=\"https://docs.unity3d.com/Documentation/Manual/custom-scripting-symbols.html#player-settings\">Custom symbols for a platform</a>.");
                }
            }
        }
    }
}
