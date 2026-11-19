// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    // Runs the active build profile's post-build callbacks. Discovered by BuildPipelineInterfaces.InitializeBuildCallbacks.
    class BuildProfilePostprocessBuildDispatcher : IPostprocessBuildWithContext
    {
        // Per-profile callbacks run here as one group, in the order they're listed on the profile, after the other
        // context-based post-build callbacks
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildCallbackContext ctx)
        {
            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null)
                return;

            var component = profile.GetComponent<BuildProfilePostprocessBuild>();
            if (component == null || component.callbacks == null)
                return;

            RunCallbacks(component.callbacks, ctx, profile, IsStrictMode(ctx?.Report));
        }

        internal static void RunCallbacks(IBuildProfilePostprocessBuild[] callbacks, BuildCallbackContext ctx, BuildProfile profile, bool strictMode)
        {
            foreach (var callback in callbacks)
            {
                // Check for the case of an unassigned array element, or a callback type that no longer exists after a script change
                if (callback == null)
                    continue;

                try
                {
                    callback.OnPostprocessBuild(ctx);
                }
                catch (Exception e)
                {
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, profile,
                        "Post-build callback {0} on build profile '{1}' threw an exception.",
                        callback.GetType().FullName, AssetDatabase.GetAssetPath(profile));

                    // In strict mode the exception propagates so OnBuildPostProcessWithContext logs it and stops the global callbacks too
                    if (strictMode)
                        throw;

                    Debug.LogException(e, profile);
                }
            }
        }

        static bool IsStrictMode(BuildReport report) =>
            report != null &&
            ((report.summary.options & BuildOptions.StrictMode) != 0 || (report.summary.assetBundleOptions & BuildAssetBundleOptions.StrictMode) != 0);
    }
}
