// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

            foreach (var callback in component.callbacks)
            {
                // Check for the case of an unassigned array element, or a callback type that no longer exists after a script change
                if (callback == null)
                    continue;
                callback.OnPostprocessBuild(ctx);
            }
        }
    }
}
