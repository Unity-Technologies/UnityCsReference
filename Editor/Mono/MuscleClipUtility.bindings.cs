// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Animation/MuscleClipQualityInfo.h")]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    internal class MuscleClipQualityInfo
    {
        [NativeName("m_Loop")]
        public float loop = 0;
        [NativeName("m_LoopOrientation")]
        public float loopOrientation = 0;
        [NativeName("m_LoopPositionY")]
        public float loopPositionY = 0;
        [NativeName("m_LoopPositionXZ")]
        public float loopPositionXZ = 0;
    }

    internal struct QualityCurvesTime
    {
        public float fixedTime;
        public float variableEndStart;
        public float variableEndEnd;
        public int q;
    }

    [NativeHeader("Editor/Src/Animation/MuscleClipQualityInfo.h")]
    internal class MuscleClipUtility
    {
        [FreeFunction]
        extern internal static MuscleClipQualityInfo GetMuscleClipQualityInfo(AnimationClip clip, float startTime, float stopTime);

        internal static void CalculateQualityCurves(AnimationClip clip, QualityCurvesTime time, Vector2[] poseCurve, Vector2[] rotationCurve, Vector2[] heightCurve, Vector2[] positionCurve)
        {
            if (poseCurve.Length != rotationCurve.Length || rotationCurve.Length != heightCurve.Length || heightCurve.Length != positionCurve.Length)
            {
                throw new ArgumentException(string.Format(
                        "poseCurve '{0}', rotationCurve '{1}', heightCurve '{2}' and positionCurve '{3}' Length must be equals.",
                        poseCurve.Length, rotationCurve.Length, heightCurve.Length, positionCurve.Length));
            }

            CalculateQualityCurves(clip, time.fixedTime, time.variableEndStart, time.variableEndEnd, time.q, poseCurve, rotationCurve, heightCurve, positionCurve);
        }

        [FreeFunction]
        extern protected static void CalculateQualityCurves(AnimationClip clip, float fixedTime, float variableEndStart, float variableEndEnd, int q, [Out] Vector2[] poseCurve, [Out] Vector2[] rotationCurve, [Out] Vector2[] heightCurve, [Out] Vector2[] positionCurve);
    }
}
