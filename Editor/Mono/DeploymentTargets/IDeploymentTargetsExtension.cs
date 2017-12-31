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
        // Meta-targets, to be able to select targets that does not depend on the current connection state.
        //  For example: selecting "all" should launch on all connected targets, even if the number of connected targets
        //  changes between launches.
        internal static readonly DeploymentTargetId kDefault = new DeploymentTargetId("__builtin__target_default");
        internal static readonly DeploymentTargetId kAll = new DeploymentTargetId("__builtin__target_all");

        public string id;

        public DeploymentTargetId(string id)
        {
            if (string.IsNullOrEmpty(id))
                id = kDefault.id;
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

        // Weather this is a meta target (kDefault, kAll) or a specific one.
        public bool IsSpecificTarget()
        {
            return id != kDefault.id && id != kAll.id;
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

    internal struct TargetCheckResult
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

        // Checks a target requirements against target information
        // Check passes if the target is compatible
        TargetCheckResult CheckTarget(DeploymentTargetRequirements targetRequirements);

        string GetDisplayName();
    }

    internal class DeploymentOperationAbortedException : Exception {}

    internal class DeploymentOperationFailedException : Exception
    {
        public readonly string title;
        public DeploymentOperationFailedException(string title, string message, Exception inner = null) : base(message, inner)
        {
            this.title = title;
        }
    }

    internal class CorruptBuildException : DeploymentOperationFailedException
    {
        public CorruptBuildException(string message = "Corrupt build.", Exception inner = null) : base("Corrupt build", message, inner) {}
    }

    internal interface IDeploymentTargetsExtension
    {
        // Returns a list of all known targets and their status
        //  Can be called from a background thread
        List<DeploymentTargetIdAndStatus> GetKnownTargets(ProgressHandler progressHandler = null);

        // Returns info for a target
        // Throws DeploymentOperationFailedException (or one of its subclasses) is something goes wrong.
        // Throws DeploymentOperationAbortedException if process is cancelled by the user.
        //  Can be called from a background thread
        IDeploymentTargetInfo GetTargetInfo(DeploymentTargetId targetId, ProgressHandler progressHandler = null);

        // Launches a build on a target
        // Throws DeploymentOperationFailedException (or one of its subclasses) is something goes wrong.
        // Throws DeploymentOperationAbortedException if process is cancelled by the user.
        //  Can be called from a background thread
        void LaunchBuildOnTarget(BuildProperties buildProperties, DeploymentTargetId targetId, ProgressHandler progressHandler = null);
    }
}
