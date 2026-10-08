// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>Opens the <b>Build Analysis</b> window from script.</summary>
    /// <seealso cref="BuildHistory"/>
    public static class BuildAnalysisUtility
    {
        /// <summary>Opens the <b>Build Analysis</b> window and selects a build from the build history.</summary>
        /// <param name="buildSessionGuid">The build session GUID of the build to select, for example
        /// <see cref="Reporting.BuildSummary.buildSessionGuid"/> or a GUID returned by <see cref="BuildHistory.GetAllBuilds"/>. Passing an empty GUID throws an <see cref="ArgumentException"/>.</param>
        /// <remarks>Use this method to show a build right after it completes, or after you add a build to the build
        /// history with <see cref="BuildHistory.RegisterExternalBuild"/>.
        ///
        /// If the build history contains no build with this GUID, the window still opens, keeps its current
        /// selection, and logs a warning to the Console.</remarks>
        /// <example>
        /// <code source="../Tests/UTFTests/Editor/ReferenceExamples/OpenWindowExample.cs"/>
        /// </example>
        public static void OpenWindow(GUID buildSessionGuid)
        {
            if (buildSessionGuid.Empty())
                throw new ArgumentException("The build session GUID is empty.", nameof(buildSessionGuid));

            BuildAnalysisWindow.ShowWindow(buildSessionGuid);
        }
    }
}
