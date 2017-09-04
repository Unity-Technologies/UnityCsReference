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

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct HoldCompletedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct HoldCanceledEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct HoldStartedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct TappedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        public int tapCount
        { get { return m_TapCount; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
        internal int m_TapCount;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationCanceledEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationCompletedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        public Vector3 cumulativeDelta
        { get { return m_CumulativeDelta; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
        internal Vector3 m_CumulativeDelta;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationStartedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct ManipulationUpdatedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        public Vector3 cumulativeDelta
        { get { return m_CumulativeDelta; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
        internal Vector3 m_CumulativeDelta;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationCanceledEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationCompletedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        public Vector3 normalizedOffset
        { get { return m_NormalizedOffset; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
        internal Vector3 m_NormalizedOffset;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationStartedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct NavigationUpdatedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        public Vector3 normalizedOffset
        { get { return m_NormalizedOffset; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
        internal Vector3 m_NormalizedOffset;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct RecognitionEndedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
    }

    [RequiredByNativeCode]
    [MovedFrom("UnityEngine.VR.WSA.Input")]
    public struct RecognitionStartedEventArgs
    {
        public InteractionSource source
        { get { return m_Source; } }

        public InteractionSourcePose sourcePose
        { get { return m_SourcePose; } }

        public Pose headPose
        { get { return m_HeadPose; } }

        internal InteractionSource m_Source;
        internal InteractionSourcePose m_SourcePose;
        internal Pose m_HeadPose;
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
        public event Action<HoldCanceledEventArgs> HoldCanceled;
        public event Action<HoldCompletedEventArgs> HoldCompleted;
        public event Action<HoldStartedEventArgs> HoldStarted;
        public event Action<TappedEventArgs> Tapped;
        public event Action<ManipulationCanceledEventArgs> ManipulationCanceled;
        public event Action<ManipulationCompletedEventArgs> ManipulationCompleted;
        public event Action<ManipulationStartedEventArgs> ManipulationStarted;
        public event Action<ManipulationUpdatedEventArgs> ManipulationUpdated;
        public event Action<NavigationCanceledEventArgs> NavigationCanceled;
        public event Action<NavigationCompletedEventArgs> NavigationCompleted;
        public event Action<NavigationStartedEventArgs> NavigationStarted;
        public event Action<NavigationUpdatedEventArgs> NavigationUpdated;
        public event Action<RecognitionEndedEventArgs> RecognitionEnded;
        public event Action<RecognitionStartedEventArgs> RecognitionStarted;
        public event Action<GestureErrorEventArgs> GestureError;

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

#pragma warning disable 0618
        [RequiredByNativeCode]
        private void InvokeHoldCanceled(HoldCanceledEventArgs eventArgs)
        {
            var holdCanceledEvent = HoldCanceledEvent;
            if (holdCanceledEvent != null)
                holdCanceledEvent(eventArgs.m_Source.m_SourceKind, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var holdCanceled = HoldCanceled;
            if (holdCanceled != null)
                holdCanceled(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeHoldCompleted(HoldCompletedEventArgs eventArgs)
        {
            var holdCompletedEvent = HoldCompletedEvent;
            if (holdCompletedEvent != null)
                holdCompletedEvent(eventArgs.m_Source.m_SourceKind, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var holdCompleted = HoldCompleted;
            if (holdCompleted != null)
                holdCompleted(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeHoldStarted(HoldStartedEventArgs eventArgs)
        {
            var holdStartedEvent = HoldStartedEvent;
            if (holdStartedEvent != null)
                holdStartedEvent(eventArgs.m_Source.m_SourceKind, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var holdStarted = HoldStarted;
            if (holdStarted != null)
                holdStarted(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeTapped(TappedEventArgs eventArgs)
        {
            var tappedEvent = TappedEvent;
            if (tappedEvent != null)
                tappedEvent(eventArgs.m_Source.m_SourceKind, eventArgs.m_TapCount, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var tapped = Tapped;
            if (tapped != null)
                tapped(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeManipulationCanceled(ManipulationCanceledEventArgs eventArgs)
        {
            var manipulationCanceledEvent = ManipulationCanceledEvent;
            if (manipulationCanceledEvent != null)
                manipulationCanceledEvent(eventArgs.m_Source.m_SourceKind, Vector3.zero, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var manipulationCanceled = ManipulationCanceled;
            if (manipulationCanceled != null)
                manipulationCanceled(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeManipulationCompleted(ManipulationCompletedEventArgs eventArgs)
        {
            var manipulationCompletedEvent = ManipulationCompletedEvent;
            if (manipulationCompletedEvent != null)
                manipulationCompletedEvent(eventArgs.m_Source.m_SourceKind, eventArgs.m_CumulativeDelta, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var manipulationCompleted = ManipulationCompleted;
            if (manipulationCompleted != null)
                manipulationCompleted(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeManipulationStarted(ManipulationStartedEventArgs eventArgs)
        {
            var manipulationStartedEvent = ManipulationStartedEvent;
            if (manipulationStartedEvent != null)
                manipulationStartedEvent(eventArgs.m_Source.m_SourceKind, Vector3.zero, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var manipulationStarted = ManipulationStarted;
            if (manipulationStarted != null)
                manipulationStarted(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeManipulationUpdated(ManipulationUpdatedEventArgs eventArgs)
        {
            var manipulationUpdatedEvent = ManipulationUpdatedEvent;
            if (manipulationUpdatedEvent != null)
                manipulationUpdatedEvent(eventArgs.m_Source.m_SourceKind, eventArgs.m_CumulativeDelta, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var manipulationUpdated = ManipulationUpdated;
            if (manipulationUpdated != null)
                manipulationUpdated(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeNavigationCanceled(NavigationCanceledEventArgs eventArgs)
        {
            var navigationCanceledEvent = NavigationCanceledEvent;
            if (navigationCanceledEvent != null)
                navigationCanceledEvent(eventArgs.m_Source.m_SourceKind, Vector3.zero, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var navigationCanceled = NavigationCanceled;
            if (navigationCanceled != null)
                navigationCanceled(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeNavigationCompleted(NavigationCompletedEventArgs eventArgs)
        {
            var navigationCompletedEvent = NavigationCompletedEvent;
            if (navigationCompletedEvent != null)
                navigationCompletedEvent(eventArgs.m_Source.m_SourceKind, eventArgs.m_NormalizedOffset, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var navigationCompleted = NavigationCompleted;
            if (navigationCompleted != null)
                navigationCompleted(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeNavigationStarted(NavigationStartedEventArgs eventArgs)
        {
            var navigationStartedEvent = NavigationStartedEvent;
            if (navigationStartedEvent != null)
                navigationStartedEvent(eventArgs.m_Source.m_SourceKind, Vector3.zero, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var navigationStarted = NavigationStarted;
            if (navigationStarted != null)
                navigationStarted(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeNavigationUpdated(NavigationUpdatedEventArgs eventArgs)
        {
            var navigationUpdatedEvent = NavigationUpdatedEvent;
            if (navigationUpdatedEvent != null)
                navigationUpdatedEvent(eventArgs.m_Source.m_SourceKind, eventArgs.m_NormalizedOffset, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var navigationUpdated = NavigationUpdated;
            if (navigationUpdated != null)
                navigationUpdated(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeRecognitionEnded(RecognitionEndedEventArgs eventArgs)
        {
            var recognitionEndedEvent = RecognitionEndedEvent;
            if (recognitionEndedEvent != null)
                recognitionEndedEvent(eventArgs.m_Source.m_SourceKind, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var recognitionEnded = RecognitionEnded;
            if (recognitionEnded != null)
                recognitionEnded(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeRecognitionStarted(RecognitionStartedEventArgs eventArgs)
        {
            var recognitionStartedEvent = RecognitionStartedEvent;
            if (recognitionStartedEvent != null)
                recognitionStartedEvent(eventArgs.m_Source.m_SourceKind, new Ray(eventArgs.m_HeadPose.position, eventArgs.m_HeadPose.rotation * Vector3.forward));

            var recognitionStarted = RecognitionStarted;
            if (recognitionStarted != null)
                recognitionStarted(eventArgs);
        }

        [RequiredByNativeCode]
        private void InvokeErrorEvent(string error, int hresult)
        {
            var gestureErrorEvent = GestureErrorEvent;
            if (gestureErrorEvent != null)
                gestureErrorEvent(error, hresult);

            var onGestureErrorEvent = GestureError;
            if (onGestureErrorEvent != null)
                onGestureErrorEvent(new GestureErrorEventArgs(error, hresult));
        }

#pragma warning restore 0618
    }
}

