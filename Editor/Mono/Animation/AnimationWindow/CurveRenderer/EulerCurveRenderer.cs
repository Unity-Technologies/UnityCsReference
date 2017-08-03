// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class EulerCurveRenderer : CurveRenderer
    {
        private int component;
        private EulerCurveCombinedRenderer renderer;

        public EulerCurveRenderer(int component, EulerCurveCombinedRenderer renderer)
        {
            this.component = component;
            this.renderer = renderer;
        }

        public AnimationCurve GetCurve()
        {
            return renderer.GetCurveOfComponent(component);
        }

        public float ClampedValue(float value)
        {
            return value;
        }

        public float RangeStart() { return renderer.RangeStart(); }
        public float RangeEnd() { return renderer.RangeEnd(); }
        public void SetWrap(WrapMode wrap)
        {
            renderer.SetWrap(wrap);
        }

        public void SetWrap(WrapMode preWrapMode, WrapMode postWrapMode)
        {
            renderer.SetWrap(preWrapMode, postWrapMode);
        }

        public void SetCustomRange(float start, float end)
        {
            renderer.SetCustomRange(start, end);
        }

        public float EvaluateCurveSlow(float time)
        {
            return renderer.EvaluateCurveSlow(time, component);
        }

        public float EvaluateCurveDeltaSlow(float time)
        {
            return renderer.EvaluateCurveDeltaSlow(time, component);
        }

        public void DrawCurve(float minTime, float maxTime, Color color, Matrix4x4 transform, Color wrapColor)
        {
            renderer.DrawCurve(minTime, maxTime, color, transform, component, wrapColor);
        }

        public Bounds GetBounds()
        {
            return GetBounds(renderer.RangeStart(), renderer.RangeEnd());
        }

        public Bounds GetBounds(float minTime, float maxTime)
        {
            return renderer.GetBounds(minTime, maxTime, component);
        }

        public void FlushCache()
        {
        }
    }
} // namespace
