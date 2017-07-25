// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;

namespace UnityEngine.Apple.ReplayKit
{
public static partial class ReplayKit
{
    public delegate void BroadcastStatusCallback(bool hasStarted, string errorMessage);
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StartBroadcasting (BroadcastStatusCallback callback, [uei.DefaultValue("false")]  bool enableMicrophone , [uei.DefaultValue("false")]  bool enableCamera ) ;

    [uei.ExcludeFromDocs]
    public static void StartBroadcasting (BroadcastStatusCallback callback, bool enableMicrophone ) {
        bool enableCamera = false;
        StartBroadcasting ( callback, enableMicrophone, enableCamera );
    }

    [uei.ExcludeFromDocs]
    public static void StartBroadcasting (BroadcastStatusCallback callback) {
        bool enableCamera = false;
        bool enableMicrophone = false;
        StartBroadcasting ( callback, enableMicrophone, enableCamera );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void StopBroadcasting () ;

}


}  


