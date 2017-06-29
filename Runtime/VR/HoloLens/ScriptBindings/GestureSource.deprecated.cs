// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;


namespace UnityEngine.XR.WSA.Input
{
    public partial struct InteractionSourceState
    {
        [Obsolete("InteractionSourceState.pressed is deprecated, and will be removed in a future release. Use InteractionSourceState.anyPressed instead. (UnityUpgradable) -> anyPressed", false)]
        public bool pressed
        { get { return anyPressed; } }

        [Obsolete("InteractionSourceState.headRay is obsolete - update your scripts to use InteractionSourceLocation.headSourceRay instead.", false)]
        public Ray headRay
        {
            get
            {
                Ray ray;
                m_HeadRay.TryGetRay(out ray);
                return ray;
            }
        }
    }

    public partial class InteractionManager
    {
        public delegate void SourceEventHandler(InteractionSourceState state);

        [Obsolete("SourceDetected is deprecated, and will be removed in a future release. Use OnSourceDetected instead.", false)]
        public static event SourceEventHandler SourceDetected;

        [Obsolete("SourceLost is deprecated, and will be removed in a future release. Use OnSourceLost instead.", false)]
        public static event SourceEventHandler SourceLost;

        [Obsolete("SourcePressed is deprecated, and will be removed in a future release. Use OnSourcePressed instead.", false)]
        public static event SourceEventHandler SourcePressed;

        [Obsolete("SourceReleased is deprecated, and will be removed in a future release. Use OnSourceReleased instead.", false)]
        public static event SourceEventHandler SourceReleased;

        [Obsolete("SourceUpdated is deprecated, and will be removed in a future release. Use OnSourceUpdated instead.", false)]
        public static event SourceEventHandler SourceUpdated;
    }
}

