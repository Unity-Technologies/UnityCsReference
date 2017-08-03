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
    internal class BoolCurveRenderer : NormalCurveRenderer
    {
        public BoolCurveRenderer(AnimationCurve curve)
            : base(curve)
        {
        }

        public override float ClampedValue(float value)
        {
            return value != 0.0f ? 1.0f : 0.0f;
        }

        public override float EvaluateCurveSlow(float time)
        {
            return ClampedValue(GetCurve().Evaluate(time));
        }
    }
} // namespace
