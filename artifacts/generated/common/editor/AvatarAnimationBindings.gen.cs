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

namespace UnityEditor
{


[RequiredByNativeCode]
internal sealed partial class MuscleClipQualityInfo
{
    public float loop = 0;
    public float loopOrientation = 0;
    public float loopPositionY = 0;
    public float loopPositionXZ = 0;
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct QualityCurvesTime
{
    public float fixedTime;
    public float variableEndStart;
    public float variableEndEnd;
    public int q;
}

internal sealed partial class MuscleClipEditorUtilities
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  MuscleClipQualityInfo GetMuscleClipQualityInfo (AnimationClip clip, float startTime, float stopTime) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CalculateQualityCurves (AnimationClip clip, QualityCurvesTime time, Vector2[] poseCurve, Vector2[] rotationCurve, Vector2[] heightCurve, Vector2[] positionCurve) ;

}

}
