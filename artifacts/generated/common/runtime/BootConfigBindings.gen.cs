// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.InteropServices;

namespace UnityEngine
{


internal sealed partial class BootConfigData
{
    private IntPtr m_Ptr;
    
    
    public void AddKey(string key)
        {
            Append(m_Ptr, key, null);
        }
    
    
    public void Append(string key, string value)
        {
            Append(m_Ptr, key, value);
        }
    
    
    public void Set(string key, string value)
        {
            Set(m_Ptr, key, value);
        }
    
    
    public string Get(string key)
        {
            return Get(m_Ptr, key);
        }
    
    
    static BootConfigData Wrap(IntPtr nativeHandle)
        {
            return new BootConfigData(nativeHandle);
        }
    
    
    private BootConfigData(IntPtr nativeHandle)
        {
            if (nativeHandle == IntPtr.Zero)
                throw new ArgumentException("native handle can not be null");
            m_Ptr = nativeHandle;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Append (IntPtr nativeHandle, string key, string val) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Set (IntPtr nativeHandle, string key, string val) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string Get (IntPtr nativeHandle, string key) ;

}


}
