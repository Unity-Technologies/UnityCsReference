// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Listener for when the active build target changes.
    /// This is used to update the build profile window and other components when the active build target changes.
    /// </summary>
    /// <remarks>
    /// Calls to `EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, target)` will trigger this event.
    /// The event is called after the build target is changed, and an instance of this class is created when the event occurs.
    /// </remarks>
    internal class ActiveBuildTargetListener : IActiveBuildTargetChanged
    {
        /// <summary>
        /// The order in which the callback will be called. Lower numbers are called first.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Called when the active build target changes.
        /// </summary>
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            activeBuildTargetChanged?.Invoke(previousTarget, newTarget);
        }

        /// <summary>
        /// Event that is called when the active build platform changes.
        /// </summary>
        static public event Action<BuildTarget, BuildTarget> activeBuildTargetChanged;
    }
}
