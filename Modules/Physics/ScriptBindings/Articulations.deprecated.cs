// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class ArticulationBody : Behaviour
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use ArticulationBody.linearVelocity instead. (UnityUpgradable) -> linearVelocity")]
        public Vector3 velocity { get => linearVelocity; set => linearVelocity = value; }

        [Obsolete("computeParentAnchor has been renamed to matchAnchors (UnityUpgradable) -> matchAnchors")]
        public bool computeParentAnchor { get => matchAnchors; set => matchAnchors = value; }

        [Obsolete("Setting joint accelerations is not supported in forward kinematics. To have inverse dynamics take acceleration into account, use GetJointForcesForAcceleration instead",true)]
        extern public void SetJointAccelerations(List<float> accelerations);
    }
}
