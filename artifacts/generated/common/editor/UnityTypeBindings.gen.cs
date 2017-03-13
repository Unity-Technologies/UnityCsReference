// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEditor;

namespace UnityEditor
{



internal sealed partial class UnityType
{
    [UsedByNativeCode]
    [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
    private partial struct UnityTypeTransport
    {
        public uint runtimeTypeIndex;
        public uint descendantCount;
        public uint baseClassIndex;
        public string className;
        public string classNamespace;
        public int persistentTypeID;
        public uint flags;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  UnityTypeTransport[] Internal_GetAllTypes () ;

}

}
