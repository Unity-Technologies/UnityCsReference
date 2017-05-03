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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityEngine
{

#pragma warning disable 414
[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct Hash128
{
    public Hash128(uint u32_0, uint u32_1, uint u32_2, uint u32_3)
        {
            m_u32_0 = u32_0;
            m_u32_1 = u32_1;
            m_u32_2 = u32_2;
            m_u32_3 = u32_3;
        }
    
    
    uint m_u32_0;
    uint m_u32_1;
    uint m_u32_2;
    uint m_u32_3;
    
    
    public bool isValid
        {
            get
            {
                return m_u32_0 != 0
                    || m_u32_1 != 0
                    || m_u32_2 != 0
                    || m_u32_3 != 0;
            }
        }
    
    
    public override string ToString()
        {
            return Internal_Hash128ToString(m_u32_0, m_u32_1, m_u32_2, m_u32_3);
        }
    
    
    public static Hash128 Parse (string hashString) {
        Hash128 result;
        INTERNAL_CALL_Parse ( hashString, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Parse (string hashString, out Hash128 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string Internal_Hash128ToString (uint d0, uint d1, uint d2, uint d3) ;

    public override bool Equals(object obj)
        {
            return obj is Hash128 && this == (Hash128)obj;
        }
    
    
    public override int GetHashCode()
        {
            return m_u32_0.GetHashCode() ^ m_u32_1.GetHashCode() ^ m_u32_2.GetHashCode() ^ m_u32_3.GetHashCode();
        }
    
    
    public static bool operator==(Hash128 hash1, Hash128 hash2)
        {
            return (hash1.m_u32_0 == hash2.m_u32_0 && hash1.m_u32_1 == hash2.m_u32_1 && hash1.m_u32_2 == hash2.m_u32_2 && hash1.m_u32_3 == hash2.m_u32_3);
        }
    
    
    public static bool operator!=(Hash128 hash1, Hash128 hash2)
        {
            return !(hash1 == hash2);
        }
    
    
}

public enum AudioType
{
    
    UNKNOWN = 0,
    
    ACC = 1,
    
    AIFF = 2,
    
    
    
    
    
    
    
    
    
    IT = 10,
    
    
    MOD = 12,
    
    MPEG = 13,
    
    OGGVORBIS = 14,
    
    
    
    S3M = 17,
    
    
    
    WAV = 20,
    
    XM = 21,
    
    XMA = 22,
    VAG = 23,
    
    AUDIOQUEUE = 24,
    
    
    
    
    
    
}


#pragma warning restore 414



internal sealed partial class UnityLogWriter : System.IO.TextWriter
{
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void WriteStringToUnityLog (string s) ;

    
    public static void Init()
        {
            System.Console.SetOut(new UnityLogWriter());
        }
    
            public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    public override void Write(char value)
        {
            WriteStringToUnityLog(value.ToString());
        }
    
    public override void Write(string s)
        {
            WriteStringToUnityLog(s);
        }
    
    
}

public sealed partial class UnityEventQueueSystem
{
    
    public static string GenerateEventIdForPayload(string eventPayloadName)
        {
            byte[] bs = System.Guid.NewGuid().ToByteArray();
            return string.Format("REGISTER_EVENT_ID(0x{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2}ULL,0x{8:X2}{9:X2}{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}ULL,{16})"
                , bs[0], bs[1], bs[2], bs[3], bs[4], bs[5], bs[6], bs[7]
                , bs[8], bs[9], bs[10], bs[11], bs[12], bs[13], bs[14], bs[15]
                , eventPayloadName);
        }
    
    
    public static IntPtr GetGlobalEventQueue () {
        IntPtr result;
        INTERNAL_CALL_GetGlobalEventQueue ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetGlobalEventQueue (out IntPtr value);
}


}
