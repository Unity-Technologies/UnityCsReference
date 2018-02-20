// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;

namespace UnityEngine.XR.WSA
{
public enum HolographicStreamerConnectionState
{
    Disconnected,
    Connecting,
    Connected
}

public enum HolographicStreamerConnectionFailureReason
{
    None,
    Unknown,
    Unreachable,
    HandshakeFailed,
    ProtocolVersionMismatch,
    ConnectionLost
}


internal enum EmulationMode
{
    None,
    RemoteDevice,
    Simulated
    };
    internal enum GestureHand    
    {
        Left,
        Right
        };
        internal enum SimulatedGesture        
        {
            FingerPressed,
            FingerReleased
        }

        
        internal sealed partial class HolographicEmulation        
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void Initialize () ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void Shutdown () ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void LoadRoom (string id) ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void SetEmulationMode (EmulationMode mode) ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  EmulationMode GetEmulationMode () ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void SetGestureHand (GestureHand hand) ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void Reset () ;

        }

        internal sealed partial class PerceptionRemotingPlugin        
        {
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void Connect (string clientName) ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void Disconnect () ;

            internal static HolographicStreamerConnectionFailureReason CheckForDisconnect()
        {
            return HolographicStreamerConnectionFailureReason.None;
        }
            
            
            internal static HolographicStreamerConnectionState GetConnectionState()
        {
            return HolographicStreamerConnectionState.Disconnected;
        }
            
            
            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void SetEnableAudio (bool enable) ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void SetEnableVideo (bool enable) ;

            [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
            [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
            extern internal static  void SetVideoEncodingParameters (Int32 maxBitRate) ;

        }

        
    }

