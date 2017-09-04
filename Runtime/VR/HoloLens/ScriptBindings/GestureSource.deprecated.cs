// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;


namespace UnityEngine.XR.WSA.Input
{
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    [Obsolete("InteractionSourceLocation is deprecated, and will be removed in a future release. Use InteractionSourcePose instead. (UnityUpgradable) -> InteractionSourcePose", true)]
    public struct InteractionSourceLocation
    {
        public bool TryGetVelocity(out Vector3 velocity)
        {
            velocity = Vector3.zero;
            return false;
        }

        public bool TryGetPosition(out Vector3 position)
        {
            position = Vector3.zero;
            return false;
        }
    }

    public partial struct InteractionSourceProperties
    {
#pragma warning disable 0618
        [Obsolete("InteractionSourceProperties.location is deprecated, and will be removed in a future release. Use InteractionSourceState.sourcePose instead.", true)]
        public InteractionSourceLocation location { get { return new InteractionSourceLocation(); } }
#pragma warning disable 0618

        [Obsolete("InteractionSourceProperties.sourcePose is deprecated, and will be removed in a future release. Use InteractionSourceState.sourcePose instead.", false)]
        public InteractionSourcePose sourcePose { get { return m_SourcePose; } }
    }

    public partial struct InteractionSourceState
    {
        [Obsolete("InteractionSourceState.pressed is deprecated, and will be removed in a future release. Use InteractionSourceState.selectPressed instead. (UnityUpgradable) -> selectPressed", false)]
        public bool pressed
        { get { return selectPressed; } }

        [Obsolete("InteractionSourceState.headRay is obsolete - update your scripts to use InteractionSourceLocation.headPose instead.", false)]
        public Ray headRay
        {
            get { return new Ray(m_HeadPose.position, m_HeadPose.rotation * Vector3.forward); }
        }
    }

    public partial class InteractionManager
    {
        public delegate void SourceEventHandler(InteractionSourceState state);

#pragma warning disable 0067
        [Obsolete("SourceDetected is deprecated, and will be removed in a future release. Use InteractionSourceDetected instead. (UnityUpgradable) -> InteractionSourceDetectedLegacy", true)]
        public static event SourceEventHandler SourceDetected;

        [Obsolete("SourceLost is deprecated, and will be removed in a future release. Use InteractionSourceLost instead. (UnityUpgradable) -> InteractionSourceLostLegacy", true)]
        public static event SourceEventHandler SourceLost;

        [Obsolete("SourcePressed is deprecated, and will be removed in a future release. Use InteractionSourcePressed instead. (UnityUpgradable) -> InteractionSourcePressedLegacy", true)]
        public static event SourceEventHandler SourcePressed;

        [Obsolete("SourceReleased is deprecated, and will be removed in a future release. Use InteractionSourceReleased instead. (UnityUpgradable) -> InteractionSourceReleasedLegacy", true)]
        public static event SourceEventHandler SourceReleased;

        [Obsolete("SourceUpdated is deprecated, and will be removed in a future release. Use InteractionSourceUpdated instead. (UnityUpgradable) -> InteractionSourceUpdatedLegacy", true)]
        public static event SourceEventHandler SourceUpdated;
#pragma warning disable 0067

        [Obsolete("InteractionSourceDetectedLegacy is deprecated, and will be removed in a future release. Use InteractionSourceDetected instead.", false)]
        public static event SourceEventHandler InteractionSourceDetectedLegacy;

        [Obsolete("InteractionSourceLostLegacy is deprecated, and will be removed in a future release. Use InteractionSourceLost instead.", false)]
        public static event SourceEventHandler InteractionSourceLostLegacy;

        [Obsolete("InteractionSourcePressedLegacy has been deprecated, and will be removed in a future release. Use InteractionSourcePressed instead.", false)]
        public static event SourceEventHandler InteractionSourcePressedLegacy;

        [Obsolete("InteractionSourceReleasedLegacy has been deprecated, and will be removed in a future release. Use InteractionSourceReleased instead.", false)]
        public static event SourceEventHandler InteractionSourceReleasedLegacy;

        [Obsolete("InteractionSourceUpdatedLegacy has been deprecated, and will be removed in a future release. Use InteractionSourceUpdated instead.", false)]
        public static event SourceEventHandler InteractionSourceUpdatedLegacy;
    }
}

