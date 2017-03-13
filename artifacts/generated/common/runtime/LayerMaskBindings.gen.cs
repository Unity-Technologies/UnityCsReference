// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;

namespace UnityEngine
{


[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct LayerMask
{
    
            private int m_Mask;
    
    
    public static implicit operator int(LayerMask mask) { return mask.m_Mask; }
    
            public static implicit operator LayerMask(int intVal) { LayerMask mask; mask.m_Mask = intVal; return mask; }
    
    
    public int value { get { return m_Mask; } set { m_Mask = value; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string LayerToName (int layer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int NameToLayer (string layerName) ;

    public static int GetMask(params string[] layerNames)
        {
            if (layerNames == null) throw new ArgumentNullException("layerNames");

            int mask = 0;
            foreach (string name in layerNames)
            {
                int layer = NameToLayer(name);

                if (layer != -1)
                    mask |= 1 << layer;
            }
            return mask;
        }
    
    
}

}
