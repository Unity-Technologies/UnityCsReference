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
    kPosition   = 1 << 0,
    kNormal     = 1 << 1,
    kColor      = 1 << 2,
    kUV0        = 1 << 3,
    kUV1        = 1 << 4,
    kUV2        = 1 << 5,
    kUV3        = 1 << 6,
    kTangent    = 1 << 7
}

}
