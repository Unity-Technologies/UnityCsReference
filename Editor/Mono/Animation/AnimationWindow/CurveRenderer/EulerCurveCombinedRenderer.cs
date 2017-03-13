// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class EulerCurveCombinedRenderer
    {
        const int kSegmentResolution = 40;
        const float epsilon = 0.001f;

        private AnimationCurve quaternionX;
        private AnimationCurve quaternionY;
        private AnimationCurve quaternionZ;
        private AnimationCurve quaternionW;
        private AnimationCurve eulerX;
        private AnimationCurve eulerY;
        private AnimationCurve eulerZ;

        private SortedDictionary<float, Vector3> points;

        private float cachedEvaluationTime = Mathf.Infinity;
        private Vector3 cachedEvaluationValue;

        private float cachedRangeStart = Mathf.Infinity;
        private float cachedRangeEnd = Mathf.NegativeInfinity;

        private Vector3 refEuler;

        private float m_CustomRangeStart = 0;
        private float m_CustomRangeEnd = 0;
        private float rangeStart { get { return (m_CustomRangeStart == 0 && m_CustomRangeEnd == 0 && eulerX.length > 0 ? eulerX.keys[0].time : m_CustomRangeStart); } }
        private float rangeEnd   { get { return (m_CustomRangeStart == 0 && m_CustomRangeEnd == 0 && eulerX.length > 0 ? eulerX.keys[eulerX.length - 1].time : m_CustomRangeEnd); } }
        private WrapMode preWrapMode = WrapMode.Once;
        private WrapMode postWrapMode = WrapMode.Once;

        public EulerCurveCombinedRenderer(
            AnimationCurve quaternionX,
            AnimationCurve quaternionY,
            AnimationCurve quaternionZ,
            AnimationCurve quaternionW,
            AnimationCurve eulerX,
            AnimationCurve eulerY,
            AnimationCurve eulerZ
            )
        {
            this.quaternionX = (quaternionX == null ? new AnimationCurve() : quaternionX);
            this.quaternionY = (quaternionY == null ? new AnimationCurve() : quaternionY);
            this.quaternionZ = (quaternionZ == null ? new AnimationCurve() : quaternionZ);
            this.quaternionW = (quaternionW == null ? new AnimationCurve() : quaternionW);
            this.eulerX = (eulerX == null ? new AnimationCurve() : eulerX);
            this.eulerY = (eulerY == null ? new AnimationCurve() : eulerY);
            this.eulerZ = (eulerZ == null ? new AnimationCurve() : eulerZ);
        }

        public AnimationCurve GetCurveOfComponent(int component)
        {
            switch (component)
            {
                case 0: return eulerX;
                case 1: return eulerY;
                case 2: return eulerZ;
                default: return null;
            }
        }

        public float RangeStart() { return rangeStart; }
        public float RangeEnd() { return rangeEnd; }
        public WrapMode PreWrapMode() { return preWrapMode; }
        public WrapMode PostWrapMode() { return postWrapMode; }
        public void SetWrap(WrapMode wrap)
        {
            preWrapMode = wrap;
            postWrapMode = wrap;
        }

        public void SetWrap(WrapMode preWrap, WrapMode postWrap)
        {
            preWrapMode = preWrap;
            postWrapMode = postWrap;
        }

        public void SetCustomRange(float start, float end)
        {
            m_CustomRangeStart = start;
            m_CustomRangeEnd = end;
        }

        private Vector3 GetValues(float time, bool keyReference)
        {
            if (quaternionX == null) Debug.LogError("X curve is null!");
            if (quaternionY == null) Debug.LogError("Y curve is null!");
            if (quaternionZ == null) Debug.LogError("Z curve is null!");
            if (quaternionW == null) Debug.LogError("W curve is null!");

            Quaternion q;

            if (quaternionX.length != 0 &&
                quaternionY.length != 0 &&
                quaternionZ.length != 0 &&
                quaternionW.length != 0)
            {
                q = EvaluateQuaternionCurvesDirectly(time);
                if (keyReference)
                    refEuler = EvaluateEulerCurvesDirectly(time);
                refEuler = QuaternionCurveTangentCalculation.GetEulerFromQuaternion(q, refEuler);
            }
            else //euler curves only
            {
                refEuler = EvaluateEulerCurvesDirectly(time);
            }

            return refEuler;
        }

        private Quaternion EvaluateQuaternionCurvesDirectly(float time)
        {
            return new Quaternion(
                quaternionX.Evaluate(time),
                quaternionY.Evaluate(time),
                quaternionZ.Evaluate(time),
                quaternionW.Evaluate(time));
        }

        private Vector3 EvaluateEulerCurvesDirectly(float time)
        {
            return new Vector3(
                eulerX.Evaluate(time),
                eulerY.Evaluate(time),
                eulerZ.Evaluate(time));
        }

        private void CalculateCurves(float minTime, float maxTime)
        {
            points = new SortedDictionary<float, Vector3>();

            float[,] ranges = NormalCurveRenderer.CalculateRanges(minTime, maxTime, rangeStart, rangeEnd, preWrapMode, postWrapMode);
            for (int i = 0; i < ranges.GetLength(0); i++)
                AddPoints(ranges[i, 0], ranges[i, 1]);
        }

        private void AddPoints(float minTime, float maxTime)
        {
            AnimationCurve refCurve = quaternionX;
            if (refCurve.length == 0)
            {
                refCurve = eulerX;
            }

            if (refCurve.length == 0)
                return;

            if (refCurve[0].time >= minTime)
            {
                Vector3 val = GetValues(refCurve[0].time, true);
                points[rangeStart] = val;
                points[refCurve[0].time] = val;
            }
            if (refCurve[refCurve.length - 1].time <= maxTime)
            {
                Vector3 val = GetValues(refCurve[refCurve.length - 1].time, true);
                points[refCurve[refCurve.length - 1].time] = val;
                points[rangeEnd] = val;
            }

            for (int i = 0; i < refCurve.length - 1; i++)
            {
                // Ignore segments that are outside of the range from minTime to maxTime
                if (refCurve[i + 1].time<minTime || refCurve[i].time> maxTime)
                    continue;

                // Get first value from euler curve and move forwards
                float newTime = refCurve[i].time;
                points[newTime] = GetValues(newTime, true);

                // Iterate forwards through curve segments
                for (float j = 1; j <= kSegmentResolution / 2; j++)
                {
                    newTime = Mathf.Lerp(refCurve[i].time, refCurve[i + 1].time, (j - 0.001f) / kSegmentResolution);
                    points[newTime] = GetValues(newTime, false);
                }

                // Get last value from euler curve and move backwards
                newTime = refCurve[i + 1].time;
                points[newTime] = GetValues(newTime, true);

                // Iterate backwards through curve segment
                for (float j = 1; j <= kSegmentResolution / 2; j++)
                {
                    newTime = Mathf.Lerp(refCurve[i].time, refCurve[i + 1].time, 1 - (j - 0.001f) / kSegmentResolution);
                    points[newTime] = GetValues(newTime, false);
                }
            }
        }

        public float EvaluateCurveDeltaSlow(float time, int component)
        {
            if (quaternionX == null)
                return 0;
            return (EvaluateCurveSlow(time + epsilon, component) - EvaluateCurveSlow(time - epsilon, component)) / (epsilon * 2);
        }

        public float EvaluateCurveSlow(float time, int component)
        {
            if (GetCurveOfComponent(component).length == 1)
            {
                return GetCurveOfComponent(component)[0].value;
            }

            if (time == cachedEvaluationTime)
                return cachedEvaluationValue[component];

            if (time < cachedRangeStart || time > cachedRangeEnd)
            {
                // if an evaluate call is outside of cached range we might as well calculate whole range
                CalculateCurves(rangeStart, rangeEnd);
                cachedRangeStart = Mathf.NegativeInfinity;
                cachedRangeEnd   = Mathf.Infinity;
            }

            float[] times = new float[points.Count];
            Vector3[] values = new Vector3[points.Count];
            int c = 0;
            foreach (KeyValuePair<float, Vector3> kvp in points)
            {
                times[c] = kvp.Key;
                values[c] = kvp.Value;
                c++;
            }

            for (int i = 0; i < times.Length - 1; i++)
            {
                if (time < times[i + 1])
                {
                    float lerp = Mathf.InverseLerp(times[i], times[i + 1], time);
                    cachedEvaluationValue = Vector3.Lerp(values[i], values[i + 1], lerp);
                    cachedEvaluationTime = time;
                    return cachedEvaluationValue[component];
                }
            }

            if (values.Length > 0)
                return values[values.Length - 1][component];

            Debug.LogError("List of euler curve points is empty, probably caused by lack of euler curve key synching");
            return 0;
        }

        public void DrawCurve(float minTime, float maxTime, Color color, Matrix4x4 transform, int component, Color wrapColor)
        {
            if (minTime < cachedRangeStart || maxTime > cachedRangeEnd)
            {
                CalculateCurves(minTime, maxTime);
                if (minTime <= rangeStart && maxTime >= rangeEnd)
                {
                    // if we are covering whole range
                    cachedRangeStart = Mathf.NegativeInfinity;
                    cachedRangeEnd   = Mathf.Infinity;
                }
                else
                {
                    cachedRangeStart = minTime;
                    cachedRangeEnd   = maxTime;
                }
            }

            List<Vector3> polyLine = new List<Vector3>();

            foreach (KeyValuePair<float, Vector3> kvp in points)
            {
                polyLine.Add(new Vector3(kvp.Key, kvp.Value[component]));
            }

            NormalCurveRenderer.DrawCurveWrapped(minTime, maxTime, rangeStart, rangeEnd, preWrapMode, postWrapMode, color, transform, polyLine.ToArray(), wrapColor);
        }

        public Bounds GetBounds(float minTime, float maxTime, int component)
        {
            //if (alreadyDrawn[component])
            //{
            CalculateCurves(minTime, maxTime);
            //  for (int i=0; i<alreadyDrawn.Length; i++) alreadyDrawn[i] = false;
            //}
            //alreadyDrawn[component] = true;

            float min = Mathf.Infinity;
            float max = Mathf.NegativeInfinity;
            foreach (KeyValuePair<float, Vector3> kvp in points)
            {
                if (kvp.Value[component] > max)
                    max = kvp.Value[component];
                if (kvp.Value[component] < min)
                    min = kvp.Value[component];
            }
            if (min == Mathf.Infinity)
            {
                min = 0;
                max = 0;
            }
            return new Bounds(new Vector3((maxTime + minTime) * 0.5f, (max + min) * 0.5f, 0), new Vector3(maxTime - minTime, max - min, 0));
        }
    }
} // namespace
