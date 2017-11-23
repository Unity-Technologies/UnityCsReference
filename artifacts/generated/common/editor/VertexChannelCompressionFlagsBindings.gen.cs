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
public enum VertexChannelCompressionFlags
{
    None        = 0,
    Position    = 1 << 0,
    Normal      = 1 << 1,
    Tangent     = 1 << 2,
    Color       = 1 << 3,
    TexCoord0   = 1 << 4,
    TexCoord1   = 1 << 5,
    TexCoord2   = 1 << 6,
    TexCoord3   = 1 << 7,
    [System.Obsolete ("Use Position instead (UnityUpgradable) -> Position")]
    kPosition       = 1 << 0,
    [System.Obsolete ("Use Normal instead (UnityUpgradable) -> Normal")]
    kNormal         = 1 << 1,
    [System.Obsolete ("Use Color instead (UnityUpgradable) -> Color")]
    kColor          = 1 << 2,
    [System.Obsolete ("Use TexCoord0 instead (UnityUpgradable) -> TexCoord0")]
    kUV0            = 1 << 3,
    [System.Obsolete ("Use TexCoord1 instead (UnityUpgradable) -> TexCoord1")]
    kUV1            = 1 << 4,
    [System.Obsolete ("Use TexCoord2 instead (UnityUpgradable) -> TexCoord2")]
    kUV2            = 1 << 5,
    [System.Obsolete ("Use TexCoord3 instead (UnityUpgradable) -> TexCoord3")]
    kUV3            = 1 << 6,
    [System.Obsolete ("Use Tangent instead (UnityUpgradable) -> Tangent")]
    kTangent        = 1 << 7
}

}
