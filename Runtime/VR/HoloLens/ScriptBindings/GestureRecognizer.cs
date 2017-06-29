// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.WSA.Input;


namespace UnityEngine.XR.WSA.Input
{
    // These are mirrors of those in windows.perception.h, specifically the
    // ABI::Windows::UI::Input::Spatial::SpatialGestureSettings enum.
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public enum GestureSettings
    {
        None = 0,
        Tap = 1,
        DoubleTap = 2,
        Hold = 4,
        ManipulationTranslate = 8,
        NavigationX = 16,
        NavigationY = 32,
        NavigationZ = 64,
        NavigationRailsX = 128,
        NavigationRailsY = 256,
        NavigationRailsZ = 512
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct HoldCompletedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }

        public HoldCompletedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct HoldCanceledEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }

        public HoldCanceledEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct HoldStartedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }

        public HoldStartedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct TappedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public int tapCount { get; private set; }

        public TappedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, int tapCount) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.tapCount = tapCount;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationCanceledEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 cumulativeDelta { get; private set; }

        public ManipulationCanceledEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 cumulativeDelta) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.cumulativeDelta = cumulativeDelta;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationCompletedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 cumulativeDelta { get; private set; }

        public ManipulationCompletedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 cumulativeDelta) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.cumulativeDelta = cumulativeDelta;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationStartedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 cumulativeDelta { get; private set; }

        public ManipulationStartedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 cumulativeDelta) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.cumulativeDelta = cumulativeDelta;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationUpdatedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 cumulativeDelta { get; private set; }

        public ManipulationUpdatedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 cumulativeDelta) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.cumulativeDelta = cumulativeDelta;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationCanceledEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 normalizedOffset { get; private set; }

        public NavigationCanceledEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 normalizedOffset) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.normalizedOffset = normalizedOffset;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationCompletedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 normalizedOffset { get; private set; }

        public NavigationCompletedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 normalizedOffset) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.normalizedOffset = normalizedOffset;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationStartedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 normalizedOffset { get; private set; }

        public NavigationStartedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 normalizedOffset) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.normalizedOffset = normalizedOffset;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationUpdatedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }
        public Vector3 normalizedOffset { get; private set; }

        public NavigationUpdatedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 normalizedOffset) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
            this.normalizedOffset = normalizedOffset;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct RecognitionEndedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }

        public RecognitionEndedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct RecognitionStartedEventArgs
    {
        public InteractionSource source { get; private set; }
        public InteractionSourceLocation pose { get; private set; }
        public InteractionSourceRay headRay { get; private set; }

        public RecognitionStartedEventArgs(InteractionSource source, InteractionSourceLocation pose, InteractionSourceRay headRay) : this()
        {
            this.source = source;
            this.pose = pose;
            this.headRay = headRay;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct GestureErrorEventArgs
    {
        public string error { get; private set; }
        public int hresult { get; private set; }

        public GestureErrorEventArgs(string error, int hresult) : this()
        {
            this.error = error;
            this.hresult = hresult;
        }
    }

    [MovedFrom("UnityEngine.VR.WSA.Input")]
    sealed public partial class GestureRecognizer : IDisposable
    {
        public event Action<HoldCanceledEventArgs> OnHoldCanceledEvent;
        public event Action<HoldCompletedEventArgs> OnHoldCompletedEvent;
        public event Action<HoldStartedEventArgs> OnHoldStartedEvent;
        public event Action<TappedEventArgs> OnTappedEvent;
        public event Action<ManipulationCanceledEventArgs> OnManipulationCanceledEvent;
        public event Action<ManipulationCompletedEventArgs> OnManipulationCompletedEvent;
        public event Action<ManipulationStartedEventArgs> OnManipulationStartedEvent;
        public event Action<ManipulationUpdatedEventArgs> OnManipulationUpdatedEvent;
        public event Action<NavigationCanceledEventArgs> OnNavigationCanceledEvent;
        public event Action<NavigationCompletedEventArgs> OnNavigationCompletedEvent;
        public event Action<NavigationStartedEventArgs> OnNavigationStartedEvent;
        public event Action<NavigationUpdatedEventArgs> OnNavigationUpdatedEvent;
        public event Action<RecognitionEndedEventArgs> OnRecognitionEndedEvent;
        public event Action<RecognitionStartedEventArgs> OnRecognitionStartedEvent;
        public event Action<GestureErrorEventArgs> OnGestureErrorEvent;

        //
        // startup and shutdown
        //
        public GestureRecognizer()
        {
            m_Recognizer = Internal_Create();
        }

        ~GestureRecognizer()
        {
            if (m_Recognizer != IntPtr.Zero)
            {
                DestroyThreaded(m_Recognizer);
                m_Recognizer = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            if (m_Recognizer != IntPtr.Zero)
            {
                Destroy(m_Recognizer);
                m_Recognizer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets the current recognizable gestures to the specified set.  Returns
        /// the previous value.
        public GestureSettings SetRecognizableGestures(GestureSettings newMaskValue)
        {
            return GestureSettings.None;
        }

        /// <summary>
        /// Retrieves the current recognizable gestures.
        /// </summary>
        public GestureSettings GetRecognizableGestures()
        {
            return GestureSettings.None;
        }

        /// <summary>
        /// Enables this recognizer to start receiving interaction events
        /// </summary>
        public void StartCapturingGestures()
        {
        }

        /// <summary>
        /// Disabled this recognizer to stop receiving interaction events
        /// </summary>
        public void StopCapturingGestures()
        {
        }

        /// <summary>
        /// Returns the enabled state of the gesture recognizer.
        /// </summary>
        public bool IsCapturingGestures()
        {
            return false;
        }

        public void CancelGestures()
        {
        }

        // in sync with enum of the same name in Runtime/HoloLens/GestureRecognizer.h
        private enum GestureEventType
        {
            InteractionDetected,
            HoldCanceled,
            HoldCompleted,
            HoldStarted,
            TapDetected,
            ManipulationCanceled,
            ManipulationCompleted,
            ManipulationStarted,
            ManipulationUpdated,
            NavigationCanceled,
            NavigationCompleted,
            NavigationStarted,
            NavigationUpdated,
            RecognitionStarted,
            RecognitionEnded
        }

#pragma warning disable 0618
        [RequiredByNativeCode]
        private void InvokeHoldEvent(GestureEventType eventType, uint sourceId, InteractionSourceKind sourceKind, InteractionSourceLocation pose, InteractionSourceRay headRay)
        {
            switch (eventType)
            {
                case GestureEventType.HoldCanceled:
                {
                    var holdCanceledEvent = HoldCanceledEvent;
                    if (holdCanceledEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        holdCanceledEvent(sourceKind, ray);
                    }

                    var onHoldCanceledEvent = OnHoldCanceledEvent;
                    if (onHoldCanceledEvent != null)
                        onHoldCanceledEvent(new HoldCanceledEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay));
                }
                break;

                case GestureEventType.HoldCompleted:
                {
                    var holdCompletedEvent = HoldCompletedEvent;
                    if (holdCompletedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        holdCompletedEvent(sourceKind, ray);
                    }

                    var onHoldCompletedEvent = OnHoldCompletedEvent;
                    if (onHoldCompletedEvent != null)
                        onHoldCompletedEvent(new HoldCompletedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay));
                }
                break;

                case GestureEventType.HoldStarted:
                {
                    var holdStartedEvent = HoldStartedEvent;
                    if (holdStartedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        holdStartedEvent(sourceKind, ray);
                    }

                    var onHoldStartedEvent = OnHoldStartedEvent;
                    if (onHoldStartedEvent != null)
                        onHoldStartedEvent(new HoldStartedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay));
                }
                break;

                default:
                    // This means we've somehow gotten an event type that we weren't
                    // expecting.  if you're adding new events, add a case here,
                    // otherwise something upstream has gone awry.
                    throw new ArgumentException("InvokeHoldEvent: Invalid GestureEventType");
            }
        }

        [RequiredByNativeCode]
        private void InvokeTapEvent(uint sourceId, InteractionSourceKind sourceKind, InteractionSourceLocation pose, InteractionSourceRay headRay, int tapCount)
        {
            var tappedEvent = TappedEvent;
            if (tappedEvent != null)
            {
                Ray ray;
                headRay.TryGetRay(out ray);
                tappedEvent(sourceKind, tapCount, ray);
            }

            var onTappedEvent = OnTappedEvent;
            if (onTappedEvent != null)
                onTappedEvent(new TappedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, tapCount));
        }

        [RequiredByNativeCode]
        private void InvokeManipulationEvent(GestureEventType eventType, uint sourceId, InteractionSourceKind sourceKind, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 cumulativeDelta)
        {
            switch (eventType)
            {
                case GestureEventType.ManipulationCanceled:
                {
                    var manipulationCanceledEvent = ManipulationCanceledEvent;
                    if (manipulationCanceledEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        manipulationCanceledEvent(sourceKind, cumulativeDelta, ray);
                    }

                    var onManipulationCanceledEvent = OnManipulationCanceledEvent;
                    if (onManipulationCanceledEvent != null)
                        onManipulationCanceledEvent(new ManipulationCanceledEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, cumulativeDelta));
                }
                break;

                case GestureEventType.ManipulationCompleted:
                {
                    var manipulationCompletedEvent = ManipulationCompletedEvent;
                    if (manipulationCompletedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        manipulationCompletedEvent(sourceKind, cumulativeDelta, ray);
                    }

                    var onManipulationCompletedEvent = OnManipulationCompletedEvent;
                    if (onManipulationCompletedEvent != null)
                        onManipulationCompletedEvent(new ManipulationCompletedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, cumulativeDelta));
                }
                break;

                case GestureEventType.ManipulationStarted:
                {
                    var manipulationStartedEvent = ManipulationStartedEvent;
                    if (manipulationStartedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        manipulationStartedEvent(sourceKind, cumulativeDelta, ray);
                    }

                    var onManipulationStartedEvent = OnManipulationStartedEvent;
                    if (onManipulationStartedEvent != null)
                        onManipulationStartedEvent(new ManipulationStartedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, cumulativeDelta));
                }
                break;

                case GestureEventType.ManipulationUpdated:
                {
                    var manipulationUpdatedEvent = ManipulationUpdatedEvent;
                    if (manipulationUpdatedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        manipulationUpdatedEvent(sourceKind, cumulativeDelta, ray);
                    }

                    var onManipulationUpdatedEvent = OnManipulationUpdatedEvent;
                    if (onManipulationUpdatedEvent != null)
                        onManipulationUpdatedEvent(new ManipulationUpdatedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, cumulativeDelta));
                }
                break;

                default:
                    throw new ArgumentException("InvokeManipulationEvent: Invalid GestureEventType");
            }
        }

        [RequiredByNativeCode]
        private void InvokeNavigationEvent(GestureEventType eventType, uint sourceId, InteractionSourceKind sourceKind, InteractionSourceLocation pose, InteractionSourceRay headRay, Vector3 normalizedOffset)
        {
            switch (eventType)
            {
                case GestureEventType.NavigationCanceled:
                {
                    var navigationCanceledEvent = NavigationCanceledEvent;
                    if (navigationCanceledEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        navigationCanceledEvent(sourceKind, normalizedOffset, ray);
                    }

                    var onNavigationCanceledEvent = OnNavigationCanceledEvent;
                    if (onNavigationCanceledEvent != null)
                        onNavigationCanceledEvent(new NavigationCanceledEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, normalizedOffset));
                }
                break;

                case GestureEventType.NavigationCompleted:
                {
                    var navigationCompletedEvent = NavigationCompletedEvent;
                    if (navigationCompletedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        navigationCompletedEvent(sourceKind, normalizedOffset, ray);
                    }

                    var onNavigationCompletedEvent = OnNavigationCompletedEvent;
                    if (onNavigationCompletedEvent != null)
                        onNavigationCompletedEvent(new NavigationCompletedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, normalizedOffset));
                }
                break;

                case GestureEventType.NavigationStarted:
                {
                    var navigationStartedEvent = NavigationStartedEvent;
                    if (navigationStartedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        navigationStartedEvent(sourceKind, normalizedOffset, ray);
                    }

                    var onNavigationStartedEvent = OnNavigationStartedEvent;
                    if (onNavigationStartedEvent != null)
                        onNavigationStartedEvent(new NavigationStartedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, normalizedOffset));
                }
                break;

                case GestureEventType.NavigationUpdated:
                {
                    var navigationUpdatedEvent = NavigationUpdatedEvent;
                    if (navigationUpdatedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        navigationUpdatedEvent(sourceKind, normalizedOffset, ray);
                    }

                    var onNavigationUpdatedEvent = OnNavigationUpdatedEvent;
                    if (onNavigationUpdatedEvent != null)
                        onNavigationUpdatedEvent(new NavigationUpdatedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay, normalizedOffset));
                }
                break;

                default:
                    throw new ArgumentException("InvokeNavigationEvent: Invalid GestureEventType");
            }
        }

        [RequiredByNativeCode]
        private void InvokeRecognitionEvent(GestureEventType eventType, uint sourceId, InteractionSourceKind sourceKind, InteractionSourceLocation pose, InteractionSourceRay headRay)
        {
            switch (eventType)
            {
                case GestureEventType.RecognitionEnded:
                {
                    var recognitionEndedEvent = RecognitionEndedEvent;
                    if (recognitionEndedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        recognitionEndedEvent(sourceKind, ray);
                    }

                    var onRecognitionEndedEvent = OnRecognitionEndedEvent;
                    if (onRecognitionEndedEvent != null)
                        onRecognitionEndedEvent(new RecognitionEndedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay));
                }
                break;

                case GestureEventType.RecognitionStarted:
                {
                    var recognitionStartedEvent = RecognitionStartedEvent;
                    if (recognitionStartedEvent != null)
                    {
                        Ray ray;
                        headRay.TryGetRay(out ray);
                        recognitionStartedEvent(sourceKind, ray);
                    }

                    var onRecognitionStartedEvent = OnRecognitionStartedEvent;
                    if (onRecognitionStartedEvent != null)
                        onRecognitionStartedEvent(new RecognitionStartedEventArgs(new InteractionSource(sourceId, sourceKind), pose, headRay));
                }
                break;

                default:
                    // this is pretty bad, it means we've somehow gotten an event type
                    // that we weren't expecting.  if you're adding new events, add a
                    // case here, otherwise something upstream has gone awry.
                    throw new ArgumentException("InvokeRecognitionEvent: Invalid GestureEventType");
            }
        }

        [RequiredByNativeCode]
        private void InvokeErrorEvent(string error, int hresult)
        {
            var gestureErrorEvent = GestureErrorEvent;
            if (gestureErrorEvent != null)
                gestureErrorEvent(error, hresult);

            var onGestureErrorEvent = OnGestureErrorEvent;
            if (onGestureErrorEvent != null)
                onGestureErrorEvent(new GestureErrorEventArgs(error, hresult));
        }

#pragma warning restore 0618
    }
}

