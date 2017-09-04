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
using UnityEngineInternal;


namespace UnityEngine
{


    internal interface IPlayerEditorConnectionNative
    {
        void Initialize();
        void DisconnectAll();
        void SendMessage(Guid messageId, byte[] data, int playerId);
        void Poll();
        void RegisterInternal(Guid messageId);
        void UnregisterInternal(Guid messageId);
        bool IsConnected();
    }


internal sealed partial class PlayerConnectionInternal : IPlayerEditorConnectionNative
{
    
    void IPlayerEditorConnectionNative.SendMessage(Guid messageId, byte[] data, int playerId)
        {
            if (messageId == Guid.Empty)
            {
                throw new ArgumentException("messageId must not be empty");
            }

            SendMessage(messageId.ToString("N"), data, playerId);
        }
    
    void IPlayerEditorConnectionNative.Poll()
        {
            PollInternal();
        }
    
    void IPlayerEditorConnectionNative.RegisterInternal(Guid messageId)
        {
            RegisterInternal(messageId.ToString("N"));
        }
    
    void IPlayerEditorConnectionNative.UnregisterInternal(Guid messageId)
        {
            UnregisterInternal(messageId.ToString("N"));
        }
    
    void IPlayerEditorConnectionNative.Initialize()
        {
            Initialize();
        }
    
    bool IPlayerEditorConnectionNative.IsConnected()
        {
            return IsConnected();
        }
    
    void IPlayerEditorConnectionNative.DisconnectAll()
        {
            DisconnectAll();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsConnected () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Initialize () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RegisterInternal (string messageId) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnregisterInternal (string messageId) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SendMessage (string messageId, byte[] data, int playerId) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void PollInternal () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DisconnectAll () ;

}

}
