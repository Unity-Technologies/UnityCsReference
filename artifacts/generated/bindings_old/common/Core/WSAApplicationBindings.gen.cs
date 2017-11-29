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
using UnityEngineInternal;

namespace UnityEngine.WSA
{
    public delegate void AppCallbackItem();


    public delegate void WindowSizeChanged(int width, int height);


public enum WindowActivationState
{
    CodeActivated = 0,
    Deactivated = 1,
    PointerActivated = 2
}

    public delegate void WindowActivated(WindowActivationState state);


public sealed partial class Application
{
    
            public static event WindowSizeChanged windowSizeChanged;
            public static event WindowActivated windowActivated;
    
    
    public static string arguments
        {
            get
            {
                return GetAppArguments();
            }
        }
    
    
    public static string advertisingIdentifier
        {
            get
            {
                string advertisingId = GetAdvertisingIdentifier();
                UnityEngine.Application.InvokeOnAdvertisingIdentifierCallback(advertisingId, true);
                return advertisingId;
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetAdvertisingIdentifier () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetAppArguments () ;

    internal static void InvokeWindowSizeChangedEvent(int width, int height)
        {
            if (windowSizeChanged != null)
                windowSizeChanged.Invoke(width, height);
        }
    
    
    internal static void InvokeWindowActivatedEvent(WindowActivationState state)
        {
            if (windowActivated != null) windowActivated.Invoke(state);
        }
    
    
    public static void InvokeOnAppThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
        }
    
    
    public static void InvokeOnUIThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
        }
    
    
    [System.Obsolete ("TryInvokeOnAppThread is deprecated, use InvokeOnAppThread")]
public static bool TryInvokeOnAppThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
            return true;
        }
    
    
    [System.Obsolete ("TryInvokeOnUIThread is deprecated, use InvokeOnUIThread")]
public static bool TryInvokeOnUIThread(AppCallbackItem item, bool waitUntilDone)
        {
            item();
            return true;
        }
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool InternalTryInvokeOnAppThread (AppCallbackItem item, bool waitUntilDone) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool InternalTryInvokeOnUIThread (AppCallbackItem item, bool waitUntilDone) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool RunningOnAppThread () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool RunningOnUIThread () ;

}


}
