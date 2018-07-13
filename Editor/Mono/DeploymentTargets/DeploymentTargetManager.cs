// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Modules;
using UnityEditor.Build.Reporting;

namespace UnityEditor.DeploymentTargets
{
    class DeploymentTargetManager
    {
        const string k_ExtensionErrorMessage = "Platform does not implement DeploymentTargetsExtension";

        readonly IDeploymentTargetsExtension m_Extension;
        readonly IDeploymentTargetsMainThreadContext m_Context;

        public IDeploymentTargetsMainThreadContext Context { get { return m_Context; }}

        public static DeploymentTargetManager CreateInstance(BuildTargetGroup targetGroup, BuildTarget buildTarget, bool setup = true)
        {
            var extension = GetExtension(targetGroup, buildTarget);
            var context = extension.GetMainThreadContext(setup);
            return context != null ? new DeploymentTargetManager(extension, context) : null;
        }

        DeploymentTargetManager(IDeploymentTargetsExtension extension, IDeploymentTargetsMainThreadContext context)
        {
            m_Extension = extension;
            m_Context = context;
        }

        static IDeploymentTargetsExtension GetExtension(BuildTargetGroup targetGroup, BuildTarget buildTarget)
        {
            var extension = ModuleManager.GetDeploymentTargetsExtension(targetGroup, buildTarget);
            if (extension == null)
                throw new NotSupportedException(k_ExtensionErrorMessage);
            return extension;
        }

        public IDeploymentTargetInfo GetTargetInfo(DeploymentTargetId targetId)
        {
            return m_Extension.GetTargetInfo(m_Context, targetId);
        }

        public void LaunchBuildOnTarget(BuildProperties buildProperties, DeploymentTargetId targetId, ProgressHandler progressHandler = null)
        {
            m_Extension.LaunchBuildOnTarget(m_Context, buildProperties, targetId, progressHandler);
        }

        public List<DeploymentTargetIdAndStatus> GetKnownTargets()
        {
            return m_Extension.GetKnownTargets(m_Context);
        }

        // Launch a build on any target on a platform
        public List<DeploymentTargetId> FindValidTargetsForLaunchBuild(BuildProperties buildProperties)
        {
            var validTargetIds = new List<DeploymentTargetId>();
            var knownTargets = m_Extension.GetKnownTargets(m_Context);
            foreach (var target in knownTargets)
            {
                if (target.status == DeploymentTargetStatus.Ready)
                {
                    var targetInfo = m_Extension.GetTargetInfo(m_Context, target.id);
                    if (targetInfo.SupportsLaunchBuild(buildProperties))
                        validTargetIds.Add(target.id);
                }
            }
            return validTargetIds;
        }
    }
}
