// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor
{


[RequiredByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct GUID
{
    
            private uint m_Value0, m_Value1, m_Value2, m_Value3;
    
    
    public GUID(string hexRepresentation)
        {
            TryParse(hexRepresentation, out this);
        }
    
    
    public static bool operator==(GUID x, GUID y)
        {
            return x.m_Value0 == y.m_Value0 && x.m_Value1 == y.m_Value1 && x.m_Value2 == y.m_Value2 && x.m_Value3 == y.m_Value3;
        }
    
    
    public static bool operator!=(GUID x, GUID y)
        {
            return !(x == y);
        }
    
    
    public override bool Equals(object obj)
        {
            GUID rhs = (GUID)obj;
            return rhs == this;
        }
    
    
    public override int GetHashCode()
        {
            return m_Value0.GetHashCode();
        }
    
    
    public bool Empty()
        {
            return m_Value0 == 0 && m_Value1 == 0 && m_Value2 == 0 && m_Value3 == 0;
        }
    
    
    [Obsolete("Use TryParse instead")]
    public bool ParseExact(string hex)
        {
            return TryParse(hex, out this);
        }
    
    
    public static bool TryParse(string hex, out GUID result)
        {
            HexToGUIDInternal(hex, out result);
            return !result.Empty();
        }
    
    
    public static GUID Generate()
        {
            GUID guid;
            GenerateInternal(out guid);
            return guid;
        }
    
    
    public override string ToString()
        {
            return GUIDToHexInternal(ref this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GUIDToHexInternal (ref GUID value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void HexToGUIDInternal (string hex, out GUID result) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GenerateInternal (out GUID result) ;

}

}
