// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.VR.WSA.Input
{


public enum InteractionSourceKind
    {
        Other,
        Hand,
        Voice,
        Controller
    }


[RequiredByNativeCode]
    public struct InteractionSource
    {
        public uint id { get { return m_id; } }
        public InteractionSourceKind kind { get { return m_kind; } }

        internal uint m_id;
        internal InteractionSourceKind m_kind;
    }


[RequiredByNativeCode]
    public struct InteractionSourceLocation
    {
        public bool TryGetPosition(out Vector3 position)
        {
            position = m_position;
            return m_hasPosition != 0;
        }

        public bool TryGetVelocity(out Vector3 velocity)
        {
            velocity = m_velocity;
            return m_hasVelocity != 0;
        }

        internal byte m_hasPosition;
        internal Vector3 m_position;
        internal byte m_hasVelocity;
        internal Vector3 m_velocity;
    }


[RequiredByNativeCode]
    public struct InteractionSourceProperties
    {
        public double sourceLossRisk { get { return m_sourceLossRisk; } }
        public Vector3 sourceLossMitigationDirection { get { return m_sourceLossMitigationDirection; } }
        public InteractionSourceLocation location { get { return m_location; } }

        internal double m_sourceLossRisk;
        internal Vector3 m_sourceLossMitigationDirection;
        internal InteractionSourceLocation m_location;
    }


[RequiredByNativeCode]
    public struct InteractionSourceState
    {
        public bool pressed { get { return m_pressed != 0; } }
        public InteractionSourceProperties properties { get { return m_properties; } }
        public InteractionSource source { get { return m_source; } }
        public Ray headRay { get { return m_headRay; } }

        internal byte m_pressed;
        internal InteractionSourceProperties m_properties;
        internal InteractionSource m_source;
        internal Ray m_headRay;
    }


public sealed partial class InteractionManager
{
    public delegate void SourceEventHandler(InteractionSourceState state);
    
    
    public static event SourceEventHandler SourceDetected;
    public static event SourceEventHandler SourceLost;
    public static event SourceEventHandler SourcePressed;
    public static event SourceEventHandler SourceReleased;
    public static event SourceEventHandler SourceUpdated;
    
    
    public extern static int numSourceStates
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int GetCurrentReading_Internal (InteractionSourceState[] sourceStates) ;

    public static int GetCurrentReading(InteractionSourceState[] sourceStates)
        {
            if (sourceStates == null)
                throw new ArgumentNullException("sourceStates");

            if (sourceStates.Length > 0)
                return GetCurrentReading_Internal(sourceStates);
            else
                return 0;
        }
    
    
    public static InteractionSourceState[] GetCurrentReading()
        {
            InteractionSourceState[] sourceStates = new InteractionSourceState[numSourceStates];
            if (sourceStates.Length > 0)
                GetCurrentReading_Internal(sourceStates);
            return sourceStates;
        }
    
    
    private enum EventType
        {
            SourceDetected,
            SourceLost,
            SourceUpdated,
            SourcePressed,
            SourceReleased
        }
    
    
    private delegate void InternalSourceEventHandler(EventType eventType, InteractionSourceState state);
    private static InternalSourceEventHandler m_OnSourceEventHandler;
    
    
    static InteractionManager()
        {
            m_OnSourceEventHandler = OnSourceEvent;
            Initialize(Marshal.GetFunctionPointerForDelegate(m_OnSourceEventHandler));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Initialize (IntPtr internalSourceEventHandler) ;

    [AOT.MonoPInvokeCallback(typeof(InternalSourceEventHandler))]
    private static void OnSourceEvent(EventType eventType, InteractionSourceState state)
        {
            switch (eventType)
            {
                case EventType.SourceDetected:
                {
                    var ev = SourceDetected;
                    if (ev != null)
                        ev(state);
                }
                break;

                case EventType.SourceLost:
                {
                    var ev = SourceLost;
                    if (ev != null)
                        ev(state);
                }
                break;

                case EventType.SourceUpdated:
                {
                    var ev = SourceUpdated;
                    if (ev != null)
                        ev(state);
                }
                break;

                case EventType.SourcePressed:
                {
                    var ev = SourcePressed;
                    if (ev != null)
                        ev(state);
                }
                break;

                case EventType.SourceReleased:
                {
                    var ev = SourceReleased;
                    if (ev != null)
                        ev(state);
                }
                break;

                default:
                    throw new ArgumentException("OnSourceEvent: Invalid EventType");
            }
        }
    
    
}


}
