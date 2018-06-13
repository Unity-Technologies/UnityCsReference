// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.Profiling;
using UnityEngine.Profiling;

namespace UnityEditorInternal
{
[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public sealed partial class ObjectMemoryInfo
{
    public int    instanceId;
    public long   memorySize;
    public int    count;
    public int    reason;
    public string name;
    public string className;
}

[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
[RequiredByNativeCode]
public sealed partial class ObjectMemoryStackInfo
{
    public bool   expanded;
    public bool   sorted;
    public int    allocated;
    public int    ownedAllocated;
    public ObjectMemoryStackInfo[] callerSites;
    public string name;
}

}
