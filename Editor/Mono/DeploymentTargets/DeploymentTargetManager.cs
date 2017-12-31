// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.DeploymentTargets
{
    internal class DeploymentTargetManager
    {
        const string kExtensionErrorMessage = "Platform does not implement DeploymentTargetsExtension";

        private static IDeploymentTargetsExtension GetExtension(BuildTargetGroup targetGroup, BuildTarget buildTarget)
        {
            IDeploymentTargetsExtension extension = ModuleManager.GetDeploymentTargetsExtension(targetGroup, buildTarget);
            if (extension == null)
                throw new NotSupportedException(kExtensionErrorMessage);
            return extension;
        }

        public static bool IsExtensionSupported(BuildTargetGroup targetGroup, BuildTarget buildTarget)
        {
            return ModuleManager.GetDeploymentTargetsExtension(targetGroup, buildTarget) != null;
        }

        public static IDeploymentTargetInfo GetTargetInfo(BuildTargetGroup targetGroup, BuildTarget buildTarget, DeploymentTargetId targetId)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildTarget);
            return extension.GetTargetInfo(targetId);
        }

        public static bool SupportsLaunchBuild(IDeploymentTargetInfo info, BuildProperties buildProperties)
        {
            return info.GetSupportFlags().HasFlags(DeploymentTargetSupportFlags.Launch) &&
                info.CheckTarget(buildProperties.GetTargetRequirements()).Passed();
        }

        public static bool SupportsLaunchBuild(IDeploymentTargetInfo info, BuildReport buildReport)
        {
            return SupportsLaunchBuild(info, BuildProperties.GetFromBuildReport(buildReport));
        }

        public static void LaunchBuildOnTarget(BuildTargetGroup targetGroup, BuildTarget buildTarget, BuildProperties buildProperties, DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildTarget);
            extension.LaunchBuildOnTarget(buildProperties, targetId, progressHandler);
        }

        public static void LaunchBuildOnTarget(BuildTargetGroup targetGroup, BuildReport buildReport, DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            LaunchBuildOnTarget(targetGroup, buildReport.summary.platform, BuildProperties.GetFromBuildReport(buildReport), targetId, progressHandler);
        }

        public static List<DeploymentTargetIdAndStatus> GetKnownTargets(BuildTargetGroup targetGroup, BuildTarget buildTarget)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildTarget);
            return extension.GetKnownTargets();
        }

        // Launch a build on any target on a platform
        public static List<DeploymentTargetId> FindValidTargetsForLaunchBuild(BuildTargetGroup targetGroup, BuildTarget buildTarget, BuildProperties buildProperties)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildTarget);
            List<DeploymentTargetId> validTargetIds = new List<DeploymentTargetId>();
            List<DeploymentTargetIdAndStatus> knownTargets = extension.GetKnownTargets();
            foreach (var target in knownTargets)
            {
                if (target.status == DeploymentTargetStatus.Ready)
                {
                    if (SupportsLaunchBuild(extension.GetTargetInfo(target.id), buildProperties))
                        validTargetIds.Add(target.id);
                }
            }
            return validTargetIds;
        }

        public static List<DeploymentTargetId> FindValidTargetsForLaunchBuild(BuildTargetGroup targetGroup, BuildReport buildReport)
        {
            return FindValidTargetsForLaunchBuild(targetGroup, buildReport.summary.platform, BuildProperties.GetFromBuildReport(buildReport));
        }
    }
}
