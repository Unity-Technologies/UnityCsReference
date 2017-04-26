// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Modules;
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

        public static IDeploymentTargetInfo GetTargetInfo(BuildTargetGroup targetGroup, BuildTarget buildTarget, DeploymentTargetId targetId)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildTarget);
            return extension.GetTargetInfo(targetId);
        }

        public static bool SupportsLaunchBuild(IDeploymentTargetInfo info, BuildReporting.BuildReport buildReport)
        {
            return info.GetSupportFlags().HasFlags(DeploymentTargetSupportFlags.Launch) &&
                info.CheckBuild(buildReport).Passed();
        }

        public static void LaunchBuildOnTarget(BuildTargetGroup targetGroup, BuildReporting.BuildReport buildReport, DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildReport.buildTarget);
            extension.LaunchBuildOnTarget(buildReport, targetId, progressHandler);
        }

        public static List<DeploymentTargetIdAndStatus> GetKnownTargets(BuildTargetGroup targetGroup, BuildTarget buildTarget)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildTarget);
            return extension.GetKnownTargets();
        }

        // Launch a build on any target on a platform
        public static List<DeploymentTargetId> FindValidTargetsForLaunchBuild(BuildTargetGroup targetGroup, BuildReporting.BuildReport buildReport)
        {
            IDeploymentTargetsExtension extension = GetExtension(targetGroup, buildReport.buildTarget);
            List<DeploymentTargetId> validTargetIds = new List<DeploymentTargetId>();
            List<DeploymentTargetIdAndStatus> knownTargets = extension.GetKnownTargets();
            foreach (var target in knownTargets)
            {
                if (target.status == DeploymentTargetStatus.Ready)
                {
                    if (SupportsLaunchBuild(extension.GetTargetInfo(target.id), buildReport))
                        validTargetIds.Add(target.id);
                }
            }
            return validTargetIds;
        }
    }
}
