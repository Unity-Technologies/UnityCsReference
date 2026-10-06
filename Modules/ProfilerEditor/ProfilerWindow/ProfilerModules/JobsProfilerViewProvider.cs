// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Bindings;

namespace Unity.Profiling.Editor
{
    /// <summary>
    /// Supplies the Jobs Profiler view, which the CPU module presents as its 'New Timeline' view type.
    /// </summary>
    /// <remarks>
    /// The Jobs Profiler is not a Profiler module of its own; it is an alternative view of the CPU module, so all the
    /// Profiler Window needs from it is a view controller.
    ///
    /// Its implementation lives in the UnityEditor.JobsProfilerModule assembly, which depends on the Editor, so it
    /// cannot be referenced by type from here - the dependency only runs the other way. The single implementation is
    /// therefore discovered through <see cref="TypeCache"/>, which keeps the contract a compile-time one rather than
    /// a name comparison, and avoids relying on load order the way a static registration hook would.
    /// </remarks>
    [VisibleToOtherModules("UnityEditor.JobsProfilerModule")]
    internal abstract class JobsProfilerViewProvider
    {
        internal abstract ProfilerModuleViewController CreateViewController(ProfilerWindow profilerWindow);

        /// <summary>
        /// Creates the Jobs Profiler view controller, or returns null if no implementation is available.
        /// </summary>
        internal static ProfilerModuleViewController CreateJobsProfilerViewController(ProfilerWindow profilerWindow)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<JobsProfilerViewProvider>())
            {
                if (type.IsAbstract)
                    continue;

                try
                {
                    var provider = (JobsProfilerViewProvider)Activator.CreateInstance(type);
                    return provider.CreateViewController(profilerWindow);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Unable to create the Jobs Profiler view from '{type}'. {e.Message}");
                    return null;
                }
            }

            UnityEngine.Debug.LogError($"No {nameof(JobsProfilerViewProvider)} implementation was found, so the Jobs Profiler view cannot be shown.");
            return null;
        }
    }
}
