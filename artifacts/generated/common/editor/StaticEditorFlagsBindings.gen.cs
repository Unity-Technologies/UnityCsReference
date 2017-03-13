// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
namespace UnityEditor
{


[Flags]
public enum StaticEditorFlags
{
    
    LightmapStatic        = 1,
    
    OccluderStatic       = 2,
    
    OccludeeStatic       = 16,
    
    BatchingStatic        = 4,
    
    NavigationStatic      = 8,
    
    OffMeshLinkGeneration = 32,
    ReflectionProbeStatic = 64
}

}
