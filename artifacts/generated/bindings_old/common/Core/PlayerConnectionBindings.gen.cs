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


namespace UnityEngine.Diagnostics
{


public static partial class PlayerConnection
{
    [System.Obsolete ("Use UnityEngine.Networking.PlayerConnection.PlayerConnection.instance.isConnected instead.")]
    public static bool connected { get { return UnityEngine.Networking.PlayerConnection.PlayerConnection.instance.isConnected; } }
    
    
    [System.Obsolete ("PlayerConnection.SendFile is no longer supported.", true)]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SendFile (string remoteFilePath, byte[] data) ;

}

}
