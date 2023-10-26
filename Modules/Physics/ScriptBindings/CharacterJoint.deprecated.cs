// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class CharacterJoint
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("TargetRotation not in use for Unity 5 and assumed disabled.", true)]
        public Quaternion targetRotation;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("TargetAngularVelocity not in use for Unity 5 and assumed disabled.", true)]
        public Vector3 targetAngularVelocity;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("RotationDrive not in use for Unity 5 and assumed disabled.", true)]
        public JointDrive rotationDrive;
    }
}
