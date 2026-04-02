// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal static class DescriptorExtensions
    {
        public static string GetAreasSummary(this Descriptor descriptor)
        {
            return DescriptorLibrary.GetAreasString(descriptor.Areas);
        }

        public static bool MatchesAnyAreas(this Descriptor descriptor, Areas areasToMatch)
        {
            return (descriptor.Areas & areasToMatch) != 0;
        }

        public static string GetPlatformsSummary(this Descriptor descriptor)
        {
            return (descriptor.Platforms == null || descriptor.Platforms.Length == 0) ? "Any" : Formatting.CombineStrings(descriptor.Platforms);
        }

        public static string GetFullTypeName(this Descriptor descriptor)
        {
            return descriptor.Type + "." + descriptor.Method;
        }

        /// <summary>
        /// Check if the descriptor applies to the given platform
        /// </summary>
        public static bool IsPlatformCompatible(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.Platforms == null || descriptor.Platforms.Length == 0)
                return true;
            return Array.IndexOf(descriptor.Platforms, buildTarget) != -1;
        }

        public static bool IsApplicable(this Descriptor desc, AnalysisParams analysisParams)
        {
            return desc.IsVersionCompatible() && desc.IsPlatformCompatible(analysisParams.Platform);
        }

        public static bool IsSupported(this Descriptor desc)
        {
            return desc.IsPlatformSupported() && desc.IsVersionCompatible();
        }

        /// <summary>
        /// Check if any descriptor's platforms are supported by the current editor
        /// </summary>
        public static bool IsPlatformSupported(this Descriptor desc)
        {
            var platforms = desc.Platforms;
            if (platforms == null)
                return true;
            foreach (var buildTarget in platforms)
            {
                var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);


                if (BuildPipeline.IsBuildTargetSupported(buildTargetGroup, buildTarget))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the descriptor applies only to the given platform
        /// </summary>
        public static bool IsPlatformSpecific(this Descriptor descriptor, BuildTarget buildTarget)
        {
            if (descriptor.Platforms == null || descriptor.Platforms.Length != 1)
                return false;
            return descriptor.Platforms[0].Equals(buildTarget.ToString());
        }

        static Version s_UnityVersion = (Version)null;

        /// <summary>
        /// Check if the descriptor's version is compatible with the current editor
        /// </summary>
        public static bool IsVersionCompatible(this Descriptor desc)
        {
            if (s_UnityVersion == null)
            {
                var unityVersionString = Application.unityVersion;
                unityVersionString = unityVersionString.Remove(
                    Regex.Match(unityVersionString, "[A-Za-z]").Index);
                s_UnityVersion = new Version(unityVersionString);
            }

            var minimumVersion = (Version)null;
            var maximumVersion = (Version)null;

            if (!string.IsNullOrEmpty(desc.MinimumVersion))
            {
                try
                {
                    minimumVersion = new Version(desc.MinimumVersion);
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("Descriptor ({0}) minimumVersion ({1}) is invalid. Exception: {2}", desc.Id, desc.MinimumVersion, exception.Message);
                }
            }

            if (!string.IsNullOrEmpty(desc.MaximumVersion))
            {
                try
                {
                    maximumVersion = new Version(desc.MaximumVersion);
                }
                catch (Exception exception)
                {
                    Debug.LogErrorFormat("Descriptor ({0}) maximumVersion ({1}) is invalid. Exception: {2}", desc.Id, desc.MaximumVersion, exception.Message);
                }
            }

            if (minimumVersion != null && maximumVersion != null && minimumVersion > maximumVersion)
            {
                Debug.LogErrorFormat("Descriptor ({0}) minimumVersion ({1}) is greater than maximumVersion ({2}).", desc.Id, minimumVersion, maximumVersion);
                return false;
            }

            if (minimumVersion != null && s_UnityVersion < minimumVersion)
                return false;
            if (maximumVersion != null && s_UnityVersion > maximumVersion)
                return false;

            return true;
        }
    }
}
