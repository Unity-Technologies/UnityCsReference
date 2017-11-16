// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    public partial class EditorUserBuildSettings
    {
        // Select a new build target to be active.
        [Obsolete("Please use SwitchActiveBuildTarget(BuildTargetGroup targetGroup, BuildTarget target)")]
        public static bool SwitchActiveBuildTarget(BuildTarget target)
        {
            return SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
        }

        // Triggered in response to SwitchActiveBuildTarget.
        [Obsolete("UnityEditor.activeBuildTargetChanged has been deprecated.Use UnityEditor.Build.IActiveBuildTargetChanged instead.")]
        public static Action activeBuildTargetChanged;

#pragma warning disable 0618
        internal static void Internal_ActiveBuildTargetChanged()
        {
            if (activeBuildTargetChanged != null)
                activeBuildTargetChanged();
        }

#pragma warning restore 0618

        // Force full optimisations for script complilation in Development builds (OBSOLETE, replaced by "IL2CPP optimization level" Player Setting)
        [Obsolete("forceOptimizeScriptCompilation is obsolete - will always return false. Control script optimization using the 'IL2CPP optimization level' configuration in Player Settings / Other.")]
        public static bool forceOptimizeScriptCompilation { get { return false; } }
    }
}
