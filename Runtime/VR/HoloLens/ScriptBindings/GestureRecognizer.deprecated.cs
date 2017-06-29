// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.XR.WSA.Input;


namespace UnityEngine.XR.WSA.Input
{
    sealed public partial class GestureRecognizer : IDisposable
    {
        //
        // public facing delegates and events
        //
        public delegate void HoldCanceledEventDelegate(InteractionSourceKind source, Ray headRay);
        public delegate void HoldCompletedEventDelegate(InteractionSourceKind source, Ray headRay);
        public delegate void HoldStartedEventDelegate(InteractionSourceKind source, Ray headRay);
        public delegate void TappedEventDelegate(InteractionSourceKind source, int tapCount, Ray headRay);
        public delegate void ManipulationCanceledEventDelegate(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay);
        public delegate void ManipulationCompletedEventDelegate(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay);
        public delegate void ManipulationStartedEventDelegate(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay);
        public delegate void ManipulationUpdatedEventDelegate(InteractionSourceKind source, Vector3 cumulativeDelta, Ray headRay);
        public delegate void NavigationCanceledEventDelegate(InteractionSourceKind source, Vector3 normalizedOffset, Ray headRay);
        public delegate void NavigationCompletedEventDelegate(InteractionSourceKind source, Vector3 normalizedOffset, Ray headRay);
        public delegate void NavigationStartedEventDelegate(InteractionSourceKind source, Vector3 normalizedOffset, Ray headRay);
        public delegate void NavigationUpdatedEventDelegate(InteractionSourceKind source, Vector3 normalizedOffset, Ray headRay);
        public delegate void RecognitionEndedEventDelegate(InteractionSourceKind source, Ray headRay);
        public delegate void RecognitionStartedEventDelegate(InteractionSourceKind source, Ray headRay);
        public delegate void GestureErrorDelegate([MarshalAs(UnmanagedType.LPStr)] string error, int hresult);

        [Obsolete("HoldCanceledEvent is deprecated, and will be removed in a future release. Use OnHoldCanceledEvent instead.", false)]
        public event HoldCanceledEventDelegate HoldCanceledEvent;

        [Obsolete("HoldCompletedEvent is deprecated, and will be removed in a future release. Use OnHoldCompletedEvent instead.", false)]
        public event HoldCompletedEventDelegate HoldCompletedEvent;

        [Obsolete("HoldStartedEvent is deprecated, and will be removed in a future release. Use OnHoldStartedEvent instead.", false)]
        public event HoldStartedEventDelegate HoldStartedEvent;

        [Obsolete("TappedEvent is deprecated, and will be removed in a future release. Use OnTappedEvent instead.", false)]
        public event TappedEventDelegate TappedEvent;

        [Obsolete("ManipulationCanceledEvent is deprecated, and will be removed in a future release. Use OnManipulationCanceledEvent instead.", false)]
        public event ManipulationCanceledEventDelegate ManipulationCanceledEvent;

        [Obsolete("ManipulationCompletedEvent is deprecated, and will be removed in a future release. Use OnManipulationCompletedEvent instead.", false)]
        public event ManipulationCompletedEventDelegate ManipulationCompletedEvent;

        [Obsolete("ManipulationStartedEvent is deprecated, and will be removed in a future release. Use OnManipulationStartedEvent instead.", false)]
        public event ManipulationStartedEventDelegate ManipulationStartedEvent;

        [Obsolete("ManipulationUpdatedEvent is deprecated, and will be removed in a future release. Use OnManipulationUpdatedEvent instead.", false)]
        public event ManipulationUpdatedEventDelegate ManipulationUpdatedEvent;

        [Obsolete("NavigationCanceledEvent is deprecated, and will be removed in a future release. Use OnNavigationCanceledEvent instead.", false)]
        public event NavigationCanceledEventDelegate NavigationCanceledEvent;

        [Obsolete("NavigationCompletedEvent is deprecated, and will be removed in a future release. Use OnNavigationCompletedEvent instead.", false)]
        public event NavigationCompletedEventDelegate NavigationCompletedEvent;

        [Obsolete("NavigationStartedEvent is deprecated, and will be removed in a future release. Use OnNavigationStartedEvent instead.", false)]
        public event NavigationStartedEventDelegate NavigationStartedEvent;

        [Obsolete("NavigationUpdatedEvent is deprecated, and will be removed in a future release. Use OnNavigationUpdatedEvent instead.", false)]
        public event NavigationUpdatedEventDelegate NavigationUpdatedEvent;

        [Obsolete("RecognitionEndedEvent is deprecated, and will be removed in a future release. Use OnRecognitionEndedEvent instead.", false)]
        public event RecognitionEndedEventDelegate RecognitionEndedEvent;

        [Obsolete("RecognitionStartedEvent is deprecated, and will be removed in a future release. Use OnRecognitionStartedEvent instead.", false)]
        public event RecognitionStartedEventDelegate RecognitionStartedEvent;

        [Obsolete("GestureErrorEvent is deprecated, and will be removed in a future release. Use OnGestureErrorEvent instead.", false)]
        public event GestureErrorDelegate GestureErrorEvent;
    }
}

