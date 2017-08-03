// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class IntCurveRenderer : NormalCurveRenderer
    {
        const float kSegmentWindowResolution = 1000;
        const int kMaximumSampleCount = 1000;
        const float kStepHelperOffset = 0.000001f;


        public IntCurveRenderer(AnimationCurve curve)
            : base(curve)
        {
        }

        public override float ClampedValue(float value)
        {
            return Mathf.Floor(value + 0.5f);
        }

        public override float EvaluateCurveSlow(float time)
        {
            return ClampedValue(GetCurve().Evaluate(time));
        }

        protected override int GetSegmentResolution(float minTime, float maxTime, float keyTime, float nextKeyTime)
        {
            float fullTimeRange = maxTime - minTime;
            float keyTimeRange = nextKeyTime - keyTime;
            int count = Mathf.RoundToInt(kSegmentWindowResolution * (keyTimeRange / fullTimeRange));
            return Mathf.Clamp(count, 1, kMaximumSampleCount);
        }

        protected override void AddPoint(ref List<Vector3> points, ref float lastTime, float sampleTime, ref float lastValue, float sampleValue)
        {
            if (lastValue != sampleValue)
            {
                points.Add(new Vector3(lastTime + kStepHelperOffset, sampleValue));
            }

            points.Add(new Vector3(sampleTime, sampleValue));
            lastTime = sampleTime;
            lastValue = sampleValue;
        }
    }
} // namespace
