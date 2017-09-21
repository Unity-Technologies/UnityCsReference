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
        private void InvokeHoldCanceled(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var holdCanceledEvent = HoldCanceledEvent;
            if (holdCanceledEvent != null)
                holdCanceledEvent(source.m_SourceKind, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var holdCanceled = HoldCanceled;
            if (holdCanceled != null)
            {
                HoldCanceledEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                holdCanceled(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeHoldCompleted(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var holdCompletedEvent = HoldCompletedEvent;
            if (holdCompletedEvent != null)
                holdCompletedEvent(source.m_SourceKind, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var holdCompleted = HoldCompleted;
            if (holdCompleted != null)
            {
                HoldCompletedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                holdCompleted(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeHoldStarted(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var holdStartedEvent = HoldStartedEvent;
            if (holdStartedEvent != null)
                holdStartedEvent(source.m_SourceKind, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var holdStarted = HoldStarted;
            if (holdStarted != null)
            {
                HoldStartedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                holdStarted(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeTapped(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, int tapCount)
        {
            var tappedEvent = TappedEvent;
            if (tappedEvent != null)
                tappedEvent(source.m_SourceKind, tapCount, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var tapped = Tapped;
            if (tapped != null)
            {
                TappedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                eventArgs.m_TapCount = tapCount;
                tapped(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeManipulationCanceled(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var manipulationCanceledEvent = ManipulationCanceledEvent;
            if (manipulationCanceledEvent != null)
                manipulationCanceledEvent(source.m_SourceKind, Vector3.zero, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var manipulationCanceled = ManipulationCanceled;
            if (manipulationCanceled != null)
            {
                ManipulationCanceledEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                manipulationCanceled(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeManipulationCompleted(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, Vector3 cumulativeDelta)
        {
            var manipulationCompletedEvent = ManipulationCompletedEvent;
            if (manipulationCompletedEvent != null)
                manipulationCompletedEvent(source.m_SourceKind, cumulativeDelta, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var manipulationCompleted = ManipulationCompleted;
            if (manipulationCompleted != null)
            {
                ManipulationCompletedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                eventArgs.m_CumulativeDelta = cumulativeDelta;
                manipulationCompleted(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeManipulationStarted(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var manipulationStartedEvent = ManipulationStartedEvent;
            if (manipulationStartedEvent != null)
                manipulationStartedEvent(source.m_SourceKind, Vector3.zero, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var manipulationStarted = ManipulationStarted;
            if (manipulationStarted != null)
            {
                ManipulationStartedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                manipulationStarted(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeManipulationUpdated(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, Vector3 cumulativeDelta)
        {
            var manipulationUpdatedEvent = ManipulationUpdatedEvent;
            if (manipulationUpdatedEvent != null)
                manipulationUpdatedEvent(source.m_SourceKind, cumulativeDelta, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var manipulationUpdated = ManipulationUpdated;
            if (manipulationUpdated != null)
            {
                ManipulationUpdatedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                eventArgs.m_CumulativeDelta = cumulativeDelta;
                manipulationUpdated(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeNavigationCanceled(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var navigationCanceledEvent = NavigationCanceledEvent;
            if (navigationCanceledEvent != null)
                navigationCanceledEvent(source.m_SourceKind, Vector3.zero, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var navigationCanceled = NavigationCanceled;
            if (navigationCanceled != null)
            {
                NavigationCanceledEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                navigationCanceled(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeNavigationCompleted(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, Vector3 normalizedOffset)
        {
            var navigationCompletedEvent = NavigationCompletedEvent;
            if (navigationCompletedEvent != null)
                navigationCompletedEvent(source.m_SourceKind, normalizedOffset, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var navigationCompleted = NavigationCompleted;
            if (navigationCompleted != null)
            {
                NavigationCompletedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                eventArgs.m_NormalizedOffset = normalizedOffset;
                navigationCompleted(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeNavigationStarted(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var navigationStartedEvent = NavigationStartedEvent;
            if (navigationStartedEvent != null)
                navigationStartedEvent(source.m_SourceKind, Vector3.zero, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var navigationStarted = NavigationStarted;
            if (navigationStarted != null)
            {
                NavigationStartedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                navigationStarted(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeNavigationUpdated(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, Vector3 normalizedOffset)
        {
            var navigationUpdatedEvent = NavigationUpdatedEvent;
            if (navigationUpdatedEvent != null)
                navigationUpdatedEvent(source.m_SourceKind, normalizedOffset, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var navigationUpdated = NavigationUpdated;
            if (navigationUpdated != null)
            {
                NavigationUpdatedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                eventArgs.m_NormalizedOffset = normalizedOffset;
                navigationUpdated(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeRecognitionEnded(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var recognitionEndedEvent = RecognitionEndedEvent;
            if (recognitionEndedEvent != null)
                recognitionEndedEvent(source.m_SourceKind, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var recognitionEnded = RecognitionEnded;
            if (recognitionEnded != null)
            {
                RecognitionEndedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                recognitionEnded(eventArgs);
            }
        }

        [RequiredByNativeCode]
        private void InvokeRecognitionStarted(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose)
        {
            var recognitionStartedEvent = RecognitionStartedEvent;
            if (recognitionStartedEvent != null)
                recognitionStartedEvent(source.m_SourceKind, new Ray(headPose.position, headPose.rotation * Vector3.forward));

            var recognitionStarted = RecognitionStarted;
            if (recognitionStarted != null)
            {
                RecognitionStartedEventArgs eventArgs;
                eventArgs.m_Source = source;
                eventArgs.m_SourcePose = sourcePose;
                eventArgs.m_HeadPose = headPose;
                recognitionStarted(eventArgs);
            }
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

