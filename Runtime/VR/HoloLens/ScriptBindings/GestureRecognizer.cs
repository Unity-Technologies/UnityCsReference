// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.VR.WSA.Input;


namespace UnityEngine.VR.WSA.Input
{
    // These are mirrors of those in windows.perception.h, specifically the
    // ABI::Windows::UI::Input::Spatial::SpatialGestureSettings enum.
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
        public event HoldCanceledEventDelegate HoldCanceledEvent;
        public event HoldCompletedEventDelegate HoldCompletedEvent;
        public event HoldStartedEventDelegate HoldStartedEvent;
        public event TappedEventDelegate TappedEvent;
        public event ManipulationCanceledEventDelegate ManipulationCanceledEvent;
        public event ManipulationCompletedEventDelegate ManipulationCompletedEvent;
        public event ManipulationStartedEventDelegate ManipulationStartedEvent;
        public event ManipulationUpdatedEventDelegate ManipulationUpdatedEvent;
        public event NavigationCanceledEventDelegate NavigationCanceledEvent;
        public event NavigationCompletedEventDelegate NavigationCompletedEvent;
        public event NavigationStartedEventDelegate NavigationStartedEvent;
        public event NavigationUpdatedEventDelegate NavigationUpdatedEvent;
        public event RecognitionEndedEventDelegate RecognitionEndedEvent;
        public event RecognitionStartedEventDelegate RecognitionStartedEvent;
        public event GestureErrorDelegate GestureErrorEvent;

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
        };

        [RequiredByNativeCode]
        private void InvokeHoldEvent(GestureEventType eventType, InteractionSourceKind source, Ray headRay)
        {
            switch (eventType)
            {
                case GestureEventType.HoldCanceled:
                {
                    var holdCanceledEvent = HoldCanceledEvent;
                    if (holdCanceledEvent != null)
                    {
                        holdCanceledEvent(source, headRay);
                    }
                }
                break;

                case GestureEventType.HoldCompleted:
                {
                    var holdCompletedEvent = HoldCompletedEvent;
                    if (holdCompletedEvent != null)
                    {
                        holdCompletedEvent(source, headRay);
                    }
                }
                break;

                case GestureEventType.HoldStarted:
                {
                    var holdStartedEvent = HoldStartedEvent;
                    if (holdStartedEvent != null)
                    {
                        holdStartedEvent(source, headRay);
                    }
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
        private void InvokeTapEvent(InteractionSourceKind source, Ray headRay, int tapCount)
        {
            var tappedEvent = TappedEvent;
            if (tappedEvent != null)
            {
                tappedEvent(source, tapCount, headRay);
            }
        }

        [RequiredByNativeCode]
        private void InvokeManipulationEvent(GestureEventType eventType, InteractionSourceKind source, Vector3 position, Ray headRay)
        {
            switch (eventType)
            {
                case GestureEventType.ManipulationCanceled:
                {
                    var manipulationCanceledEvent = ManipulationCanceledEvent;
                    if (manipulationCanceledEvent != null)
                    {
                        manipulationCanceledEvent(source, position, headRay);
                    }
                }
                break;

                case GestureEventType.ManipulationCompleted:
                {
                    var manipulationCompletedEvent = ManipulationCompletedEvent;
                    if (manipulationCompletedEvent != null)
                    {
                        manipulationCompletedEvent(source, position, headRay);
                    }
                }
                break;

                case GestureEventType.ManipulationStarted:
                {
                    var manipulationStartedEvent = ManipulationStartedEvent;
                    if (manipulationStartedEvent != null)
                    {
                        manipulationStartedEvent(source, position, headRay);
                    }
                }
                break;

                case GestureEventType.ManipulationUpdated:
                {
                    var manipulationUpdatedEvent = ManipulationUpdatedEvent;
                    if (manipulationUpdatedEvent != null)
                    {
                        manipulationUpdatedEvent(source, position, headRay);
                    }
                }
                break;

                default:
                    throw new ArgumentException("InvokeManipulationEvent: Invalid GestureEventType");
            }
        }

        [RequiredByNativeCode]
        private void InvokeNavigationEvent(GestureEventType eventType, InteractionSourceKind source, Vector3 relativePosition, Ray headRay)
        {
            switch (eventType)
            {
                case GestureEventType.NavigationCanceled:
                {
                    var navigationCanceledEvent = NavigationCanceledEvent;
                    if (navigationCanceledEvent != null)
                    {
                        navigationCanceledEvent(source, relativePosition, headRay);
                    }
                }
                break;

                case GestureEventType.NavigationCompleted:
                {
                    var navigationCompletedEvent = NavigationCompletedEvent;
                    if (navigationCompletedEvent != null)
                    {
                        navigationCompletedEvent(source, relativePosition, headRay);
                    }
                }
                break;

                case GestureEventType.NavigationStarted:
                {
                    var navigationStartedEvent = NavigationStartedEvent;
                    if (navigationStartedEvent != null)
                    {
                        navigationStartedEvent(source, relativePosition, headRay);
                    }
                }
                break;

                case GestureEventType.NavigationUpdated:
                {
                    var navigationUpdatedEvent = NavigationUpdatedEvent;
                    if (navigationUpdatedEvent != null)
                    {
                        navigationUpdatedEvent(source, relativePosition, headRay);
                    }
                }
                break;

                default:
                    throw new ArgumentException("InvokeNavigationEvent: Invalid GestureEventType");
            }
        }

        [RequiredByNativeCode]
        private void InvokeRecognitionEvent(GestureEventType eventType, InteractionSourceKind source, Ray headRay)
        {
            switch (eventType)
            {
                case GestureEventType.RecognitionEnded:
                {
                    var recognitionEndedEvent = RecognitionEndedEvent;
                    if (recognitionEndedEvent != null)
                    {
                        recognitionEndedEvent(source, headRay);
                    }
                }
                break;

                case GestureEventType.RecognitionStarted:
                {
                    var recognitionStartedEvent = RecognitionStartedEvent;
                    if (recognitionStartedEvent != null)
                    {
                        recognitionStartedEvent(source, headRay);
                    }
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
            {
                gestureErrorEvent(error, hresult);
            }
        }
    };
}

