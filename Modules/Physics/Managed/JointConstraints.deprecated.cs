// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    // The [[ConfigurableJoint]] attempts to attain position / velocity targets based on this flag
    [Flags]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("JointDriveMode is no longer supported")]
    public enum JointDriveMode
    {
        [Obsolete("JointDriveMode.None is no longer supported")]
        // Don't apply any forces to reach the target
        None = 0,

        [Obsolete("JointDriveMode.Position is no longer supported")]
        // Try to reach the specified target position
        Position = 1,

        [Obsolete("JointDriveMode.Velocity is no longer supported")]
        // Try to reach the specified target velocity
        Velocity = 2,

        [Obsolete("JointDriveMode.PositionAndvelocity is no longer supported")]
        // Try to reach the specified target position and velocity
        PositionAndVelocity = 3
    }

    public partial struct JointDrive
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("JointDriveMode is obsolete")]
        public JointDriveMode mode { get { return JointDriveMode.None; } set { } }
    }
}
