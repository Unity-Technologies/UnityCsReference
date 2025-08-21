// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace UnityEditor.AdaptivePerformance.Editor
{
    /// <summary>
    /// Build attribute to identify which platforms a loader supports.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class AdaptivePerformanceSupportedBuildTargetAttribute : Attribute
    {
        /// <summary>
        /// String representation of <see href="https://docs.unity3d.com/ScriptReference/BuildTargetGroup.html">UnityEditor.Build.BuildTargetGroup</see>.
        /// </summary>
        public BuildTargetGroup buildTargetGroup { get; set; }

        /// <summary>
        /// Array of BuildTargets, each of which is the representation of <see href="https://docs.unity3d.com/ScriptReference/BuildTarget.html">UnityEditor.Build.BuildTarget</see>
        /// aligned with <see cref="buildTargetGroup"/>.
        ///
        /// Currently only advisory.
        /// </summary>
        public BuildTarget[] buildTargets { get; set; }

        private AdaptivePerformanceSupportedBuildTargetAttribute() {}

        /// <summary>Constructor for attribute. We assume that all build targets for this group will be supported.</summary>
        /// <param name="buildTargetGroup">Build Target Group that will be supported.</param>
        public AdaptivePerformanceSupportedBuildTargetAttribute(BuildTargetGroup buildTargetGroup)
        {
            this.buildTargetGroup = buildTargetGroup;
        }

        /// <summary>Constructor for attribute</summary>
        /// <param name="buildTargetGroup">Build Target Group that will be supported.</param>
        /// <param name="buildTargets">The set of build targets of Build Target Group that will be supported.</param>
        public AdaptivePerformanceSupportedBuildTargetAttribute(BuildTargetGroup buildTargetGroup, BuildTarget[] buildTargets)
        {
            this.buildTargetGroup = buildTargetGroup;
            this.buildTargets = buildTargets;
        }
    }
}
