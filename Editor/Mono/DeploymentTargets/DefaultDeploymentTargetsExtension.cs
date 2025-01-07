// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Modules;
using UnityEditorInternal;

namespace UnityEditor.DeploymentTargets
{
    internal class DefaultDeploymentTargetsMainThreadContext : IDeploymentTargetsMainThreadContext
    {
    }

    internal class DefaultDeploymentTargetInfo : IDeploymentTargetInfo
    {
        public virtual FlagSet<DeploymentTargetSupportFlags> GetSupportFlags()
        {
            return DeploymentTargetSupportFlags.None;
        }

        public virtual TargetCheckResult CheckTarget(DeploymentTargetRequirements targetRequirements)
        {
            return new TargetCheckResult();
        }

        public virtual string GetDisplayName()
        {
            return "";
        }

        public virtual bool SupportsLaunchBuild(BuildProperties buildProperties)
        {
            return GetSupportFlags().HasFlags(DeploymentTargetSupportFlags.Launch) && CheckTarget(buildProperties.GetTargetRequirements()).Passed();
        }
    }

    internal class DefaultDeploymentTargetLogger : DeploymentTargetLogger
    {
        internal override void Start() {}

        internal override void Stop() {}

        internal override void Clear() {}
    }

    internal abstract class DefaultDeploymentTargetsExtension
        : IDeploymentTargetsExtension
    {
        public virtual IDeploymentTargetsMainThreadContext GetMainThreadContext(bool setup)
        {
            CheckGetMainThreadContextCalledOnMainThread();
            return new DefaultDeploymentTargetsMainThreadContext();
        }

        protected void CheckGetMainThreadContextCalledOnMainThread()
        {
            if (!InternalEditorUtility.CurrentThreadIsMainThread())
                throw new NotSupportedException("Deployment targets main thread context can only be retrieved from the main thread.");
        }

        public virtual List<DeploymentTargetIdAndStatus> GetKnownTargets(IDeploymentTargetsMainThreadContext context, ProgressHandler progressHandler = null)
        {
            return new List<DeploymentTargetIdAndStatus>();
        }

        public virtual IDeploymentTargetInfo GetTargetInfo(IDeploymentTargetsMainThreadContext context, DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            return new DefaultDeploymentTargetInfo();
        }

        public virtual DeploymentTargetLogger GetTargetLogger(IDeploymentTargetsMainThreadContext context, DeploymentTargetId targetId)
        {
            return new DefaultDeploymentTargetLogger();
        }

        public virtual IDeploymentLaunchResult LaunchBuildOnTarget(IDeploymentTargetsMainThreadContext context, BuildProperties buildProperties, DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            throw new NotSupportedException();
        }
    }
}
