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
    internal struct DeploymentTargetId
    {
        public string id;

        public DeploymentTargetId(string id)
        {
            this.id = id;
        }

        public static implicit operator DeploymentTargetId(string id)
        {
            return new DeploymentTargetId(id);
        }

        public static implicit operator string(DeploymentTargetId id)
        {
            return id.id;
        }
    }

    internal enum DeploymentTargetStatus
    {
        // Target is ready
        Ready,
        // Target is available but there are issues
        NotReady,
        // Target exists, but we cannot connect to it
        Unavailable,
        // Target does not exist or it has existed but no longer responds
        Unknown,
    }

    internal struct DeploymentTargetIdAndStatus
    {
        public DeploymentTargetId id;
        public DeploymentTargetStatus status;
    }

    [Flags]
    internal enum DeploymentTargetSupportFlags
    {
        None            = 0,
        Launch          = 0x01 << 0
    }

    internal enum CheckStatus
    {
        Ok = 0,
        Failed
    }

    internal struct CategoryCheckResult
    {
        public CheckStatus status;
        public string failureMessage;
    }

    internal struct BuildCheckResult
    {
        public CategoryCheckResult hardware;
        public CategoryCheckResult sdk;

        public bool Passed()
        {
            return hardware.status == CheckStatus.Ok &&
                sdk.status == CheckStatus.Ok;
        }
    }

    internal interface IDeploymentTargetInfo
    {
        FlagSet<DeploymentTargetSupportFlags> GetSupportFlags();

        // Checks a build against target information
        // Check passes if the build is compatible with the target
        BuildCheckResult CheckBuild(BuildReporting.BuildReport buildReport);
    }

    internal class OperationAbortedException : Exception {}

    internal class OperationFailedException : Exception
    {
        public readonly string title;
        public OperationFailedException(string title, string message) : base(message)
        {
            this.title = title;
        }
    }

    internal class UnknownDeploymentTargetException : OperationFailedException
    {
        public UnknownDeploymentTargetException(string message = "Unknown deployment target.") : base("Cannot find deployment target", message) {}
    }

    internal class NoResponseFromDeploymentTargetException : OperationFailedException
    {
        public NoResponseFromDeploymentTargetException(string message = "No response from deployment target.") : base("No response from deployment target", message) {}
    }

    internal class CorruptBuildException : OperationFailedException
    {
        public CorruptBuildException(string message = "Corrupt build.") : base("Corrupt build", message) {}
    }

    internal interface IDeploymentTargetsExtension
    {
        // Returns a list of all known targets and their status
        List<DeploymentTargetIdAndStatus> GetKnownTargets(ProgressHandler progressHandler = null);

        // Returns info for a target
        // Throws UnknownDeploymentTargetException for unknown targets
        IDeploymentTargetInfo GetTargetInfo(DeploymentTargetId targetId, ProgressHandler progressHandler = null);

        // Launches a build on a target
        // Throws UnknownDeploymentTargetException for unknown targets
        void LaunchBuildOnTarget(BuildReporting.BuildReport buildReport, DeploymentTargetId targetId, ProgressHandler progressHandler = null);
    }
}
