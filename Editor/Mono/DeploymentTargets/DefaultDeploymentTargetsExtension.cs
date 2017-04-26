// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Modules;

namespace UnityEditor.DeploymentTargets
{
    internal class DefaultDeploymentTargetInfo : IDeploymentTargetInfo
    {
        public virtual FlagSet<DeploymentTargetSupportFlags> GetSupportFlags()
        {
            return DeploymentTargetSupportFlags.None;
        }

        public virtual BuildCheckResult CheckBuild(BuildReporting.BuildReport buildReport)
        {
            return new BuildCheckResult();
        }
    }

    internal abstract class DefaultDeploymentTargetsExtension
        : IDeploymentTargetsExtension
    {
        public virtual List<DeploymentTargetIdAndStatus> GetKnownTargets(ProgressHandler progressHandler = null)
        {
            return new List<DeploymentTargetIdAndStatus>();
        }

        public virtual IDeploymentTargetInfo GetTargetInfo(DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            return new DefaultDeploymentTargetInfo();
        }

        public virtual void LaunchBuildOnTarget(BuildReporting.BuildReport buildReport, DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            throw new NotSupportedException();
        }
    }
}
