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
    internal class NormalCurveRenderer : CurveRenderer
    {
        const float kSegmentWindowResolution = 1000;
        const int kMaximumSampleCount = 50;
        const int kMaximumLoops = 100;
        const string kCurveRendererMeshName = "NormalCurveRendererMesh";

        private AnimationCurve m_Curve;
        private float m_CustomRangeStart = 0;
        private float m_CustomRangeEnd = 0;
        private float rangeStart { get { return (m_CustomRangeStart == 0 && m_CustomRangeEnd == 0 && m_Curve.length > 0 ? m_Curve.keys[0].time : m_CustomRangeStart); } }
        private float rangeEnd   { get { return (m_CustomRangeStart == 0 && m_CustomRangeEnd == 0 && m_Curve.length > 0 ? m_Curve.keys[m_Curve.length - 1].time : m_CustomRangeEnd); } }
        private WrapMode preWrapMode = WrapMode.Once;
        private WrapMode postWrapMode = WrapMode.Once;

        private Bounds? m_Bounds;
        private Mesh m_CurveMesh;

        private static Material s_CurveMaterial;
        public static Material curveMaterial
        {
            get
            {
                if (!s_CurveMaterial)
                {
                    Shader shader = (Shader)EditorGUIUtility.LoadRequired("Editors/AnimationWindow/Curve.shader");
                    s_CurveMaterial = new Material(shader);
                }

                return s_CurveMaterial;
            }
        }

        public NormalCurveRenderer(AnimationCurve curve)
        {
            m_Curve = curve;
            if (m_Curve == null)
                m_Curve = new AnimationCurve();
        }

        public AnimationCurve GetCurve()
        {
            return m_Curve;
        }

        public float RangeStart() { return rangeStart; }
        public float RangeEnd() { return rangeEnd; }
        public void SetWrap(WrapMode wrap)
        {
            this.preWrapMode = wrap;
            this.postWrapMode = wrap;
        }

        public void SetWrap(WrapMode preWrap, WrapMode postWrap)
        {
            this.preWrapMode = preWrap;
            this.postWrapMode = postWrap;
        }

        public void SetCustomRange(float start, float end)
        {
            m_CustomRangeStart = start;
            m_CustomRangeEnd = end;
        }

        public virtual float ClampedValue(float value)
        {
            return value;
        }

        public virtual float EvaluateCurveSlow(float time)
        {
            return m_Curve.Evaluate(time);
        }

        // TODO: Implement proper analytic evaluation of curve delta instead of this numeric hack
        public float EvaluateCurveDeltaSlow(float time)
        {
            float epsilon = 0.0001f;
            return (EvaluateCurveSlow(time + epsilon) - EvaluateCurveSlow(time - epsilon)) / (epsilon * 2);
        }

        private Vector3[] GetPoints()
        {
            return GetPoints(rangeStart, rangeEnd);
        }

        private Vector3[] GetPoints(float minTime, float maxTime)
        {
            List<Vector3> points = new List<Vector3>();

            if (m_Curve.length == 0)
                return points.ToArray();
            points.Capacity = 1000 + m_Curve.length;

            float[,] ranges = CalculateRanges(minTime, maxTime, rangeStart, rangeEnd, preWrapMode, postWrapMode);
            for (int i = 0; i < ranges.GetLength(0); i++)
                AddPoints(ref points, ranges[i, 0], ranges[i, 1], minTime, maxTime);

            // Remove points that don't go in ascending time order
            if (points.Count > 0)
            {
                for (int i = 1; i < points.Count; i++)
                {
                    if (points[i].x < points[i - 1].x)
                    {
                        points.RemoveAt(i);
                        i--;
                    }
                }
            }

            return points.ToArray();
        }

        public static float[,] CalculateRanges(float minTime, float maxTime, float rangeStart, float rangeEnd, WrapMode preWrapMode, WrapMode postWrapMode)
        {
            // Don't want to deal with optimizing for different pre and post wrap mode for now
            WrapMode wrap = preWrapMode;
            if (postWrapMode != wrap)
                return new float[1, 2] {{rangeStart, rangeEnd}};

            if (wrap == WrapMode.Loop)
            {
                // If we are covering a range longer than a full loop, just add all points:
                if (maxTime - minTime > rangeEnd - rangeStart)
                {
                    return new float[1, 2] {{rangeStart, rangeEnd}};
                }
                // Else, only add the needed range(s)
                else
                {
                    // Find the start and end of needed shown range repeated into the range of the curve
                    minTime = Mathf.Repeat(minTime - rangeStart, rangeEnd - rangeStart) + rangeStart;
                    maxTime = Mathf.Repeat(maxTime - rangeStart, rangeEnd - rangeStart) + rangeStart;
                    if (minTime < maxTime)
                        return new float[1, 2] {{minTime, maxTime}};
                    else
                        return new float[2, 2] {{rangeStart, maxTime}, {minTime, rangeEnd}};
                }
            }
            else if (wrap == WrapMode.PingPong)
            {
                // TODO: Maybe optimize so not whole range is calculated if not needed
                return new float[1, 2] {{rangeStart, rangeEnd}};
            }
            else
                return new float[1, 2] {{minTime, maxTime}};
        }

        protected virtual int GetSegmentResolution(float minTime, float maxTime, float keyTime, float nextKeyTime)
        {
            float fullTimeRange = maxTime - minTime;
            float keyTimeRange = nextKeyTime - keyTime;
            int count = Mathf.RoundToInt(kSegmentWindowResolution * (keyTimeRange / fullTimeRange));
            return Mathf.Clamp(count, 1, kMaximumSampleCount);
        }

        protected virtual void AddPoint(ref List<Vector3> points, ref float lastTime, float sampleTime, ref float lastValue, float sampleValue)
        {
            points.Add(new Vector3(sampleTime, sampleValue));
            lastTime = sampleTime;
            lastValue = sampleValue;
        }

        private void AddPoints(ref List<Vector3> points, float minTime, float maxTime, float visibleMinTime, float visibleMaxTime)
        {
            if (m_Curve[0].time >= minTime)
            {
                points.Add(new Vector3(rangeStart, ClampedValue(m_Curve[0].value)));
                points.Add(new Vector3(m_Curve[0].time, ClampedValue(m_Curve[0].value)));
            }

            for (int i = 0; i < m_Curve.length - 1; i++)
            {
                Keyframe key = m_Curve[i];
                Keyframe nextKey = m_Curve[i + 1];

                // Ignore segments that are outside of the range from minTime to maxTime
                if (nextKey.time < minTime || key.time > maxTime)
                    continue;

                // Get first value from actual key rather than evaluating curve (to correctly handle stepped interpolation)
                points.Add(new Vector3(key.time, key.value));

                // Place second sample very close to first one (to correctly handle stepped interpolation)
                int segmentResolution = GetSegmentResolution(visibleMinTime, visibleMaxTime, key.time, nextKey.time);
                float newTime = Mathf.Lerp(key.time, nextKey.time, 0.001f / segmentResolution);
                float lastTime = key.time;
                float lastValue = ClampedValue(key.value);
                float value = EvaluateCurveSlow(newTime);
                AddPoint(ref points, ref lastTime, newTime, ref lastValue, value);

                // Iterate through curve segment
                for (float j = 1; j < segmentResolution; j++)
                {
                    newTime = Mathf.Lerp(key.time, nextKey.time, j / segmentResolution);
                    value = EvaluateCurveSlow(newTime);
                    AddPoint(ref points, ref lastTime, newTime, ref lastValue, value);
                }

                // Place second last sample very close to last one (to correctly handle stepped interpolation)
                newTime = Mathf.Lerp(key.time, nextKey.time, 1 - 0.001f / segmentResolution);
                value = EvaluateCurveSlow(newTime);
                AddPoint(ref points, ref lastTime, newTime, ref lastValue, value);

                // Get last value from actual key rather than evaluating curve (to correctly handle stepped interpolation)
                newTime = nextKey.time;
                AddPoint(ref points, ref lastTime, newTime, ref lastValue, value);
            }

            if (m_Curve[m_Curve.length - 1].time <= maxTime)
            {
                float clampedValue = ClampedValue(m_Curve[m_Curve.length - 1].value);
                points.Add(new Vector3(m_Curve[m_Curve.length - 1].time, clampedValue));
                points.Add(new Vector3(rangeEnd, clampedValue));
            }
        }

        private void BuildCurveMesh()
        {
            if (m_CurveMesh != null)
                return;

            Vector3[] vertices = GetPoints();

            m_CurveMesh = new Mesh();
            m_CurveMesh.name = kCurveRendererMeshName;
            m_CurveMesh.hideFlags |= HideFlags.DontSave;
            m_CurveMesh.vertices = vertices;

            if (vertices.Length > 0)
            {
                int nIndices = vertices.Length - 1;
                int index = 0;

                List<int> indices = new List<int>(nIndices * 2);
                while (index < nIndices)
                {
                    indices.Add(index);
                    indices.Add(++index);
                }

                m_CurveMesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            }
        }

        public void DrawCurve(float minTime, float maxTime, Color color, Matrix4x4 transform, Color wrapColor)
        {
            BuildCurveMesh();

            Keyframe[] keys = m_Curve.keys;
            if (keys.Length > 0)
            {
                Vector3 firstPoint = new Vector3(rangeStart, keys.First().value);
                Vector3 lastPoint = new Vector3(rangeEnd, keys.Last().value);
                DrawCurveWrapped(minTime, maxTime, rangeStart, rangeEnd, preWrapMode, postWrapMode, m_CurveMesh, firstPoint, lastPoint, transform, color, wrapColor);
            }
        }

        public static void DrawPolyLine(Matrix4x4 transform, float minDistance, params Vector3[] points)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color col = Handles.color * new Color(1, 1, 1, 0.75f);

            HandleUtility.ApplyWireMaterial();
            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(GL.LINES);
            GL.Color(col);

            Vector3 previous = transform.MultiplyPoint(points[0]);
            for (int i = 1; i < points.Length; i++)
            {
                Vector3 current = transform.MultiplyPoint(points[i]);

                if ((previous - current).magnitude > minDistance)
                {
                    GL.Vertex(previous);
                    GL.Vertex(current);

                    previous = current;
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        public static void DrawCurveWrapped(float minTime, float maxTime, float rangeStart, float rangeEnd,
            WrapMode preWrap, WrapMode postWrap, Mesh mesh, Vector3 firstPoint, Vector3 lastPoint,
            Matrix4x4 transform, Color color, Color wrapColor)
        {
            if (mesh.vertexCount == 0)
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            int minRep;
            int maxRep;
            if (rangeEnd - rangeStart != 0)
            {
                minRep = Mathf.FloorToInt((minTime - rangeStart) / (rangeEnd - rangeStart));
                maxRep = Mathf.CeilToInt((maxTime - rangeEnd) / (rangeEnd - rangeStart));

                // Prevent too many loops from being rendered at one time.
                if (minRep < -kMaximumLoops)
                    preWrap = WrapMode.Clamp;
                if (maxRep > kMaximumLoops)
                    postWrap = WrapMode.Clamp;
            }
            else
            {
                preWrap = WrapMode.Clamp;
                postWrap = WrapMode.Clamp;
                minRep = (minTime < rangeStart ? -1 : 0);
                maxRep = (maxTime > rangeEnd   ?  1 : 0);
            }

            // Draw curve itself
            Material mat = curveMaterial;
            mat.SetColor("_Color", color);
            Handles.color = color;

            // Previous camera may still be active when calling DrawMeshNow.
            Camera.SetupCurrent(null);

            mat.SetPass(0);
            Graphics.DrawMeshNow(mesh, Handles.matrix * transform);

            // Draw wrapping
            mat.SetColor("_Color", new Color(wrapColor.r, wrapColor.g, wrapColor.b, wrapColor.a * color.a));
            Handles.color = new Color(wrapColor.r, wrapColor.g, wrapColor.b, wrapColor.a * color.a);

            // Draw pre wrapping
            if (preWrap == WrapMode.Loop)
            {
                Matrix4x4 firstTransform = Handles.matrix * transform * Matrix4x4.TRS(new Vector3(minRep * (rangeEnd - rangeStart), 0.0f, 0.0f), Quaternion.identity, Vector3.one);
                Matrix4x4 iterTransform = Matrix4x4.TRS(new Vector3(rangeEnd - rangeStart, 0.0f, 0.0f), Quaternion.identity, Vector3.one);

                //  Render loop curve itself.
                mat.SetPass(0);
                Matrix4x4 curTransform = firstTransform;
                for (int r = minRep; r < 0; ++r)
                {
                    Graphics.DrawMeshNow(mesh, curTransform);
                    curTransform = curTransform * iterTransform;
                }

                //  Render straight lines connecting one loop to another.
                curTransform = firstTransform;
                for (int r = minRep; r < 0; ++r)
                {
                    Matrix4x4 nextTransform = curTransform * iterTransform;

                    Handles.DrawLine(
                        curTransform.MultiplyPoint(lastPoint),
                        nextTransform.MultiplyPoint(firstPoint)
                        );

                    curTransform = nextTransform;
                }
            }
            else if (preWrap == WrapMode.PingPong)
            {
                mat.SetPass(0);
                for (int r = minRep; r < 0; ++r)
                {
                    //  Even loop
                    if (r % 2 == 0)
                    {
                        Matrix4x4 shiftTransform = Matrix4x4.TRS(new Vector3(r * (rangeEnd - rangeStart), 0.0f, 0.0f), Quaternion.identity, Vector3.one);
                        Graphics.DrawMeshNow(mesh, Handles.matrix * transform * shiftTransform);
                    }
                    //  Odd loop
                    else
                    {
                        Matrix4x4 shiftTransform = Matrix4x4.TRS(new Vector3((r + 1) * (rangeEnd - rangeStart) + rangeStart * 2, 0.0f, 0.0f), Quaternion.identity, new Vector3(-1.0f, 1.0f, 1.0f));
                        Graphics.DrawMeshNow(mesh, Handles.matrix * transform * shiftTransform);
                    }
                }
            }
            else
            {
                if (minRep < 0)
                {
                    Handles.DrawLine(
                        transform.MultiplyPoint(new Vector3(minTime, firstPoint.y, 0)),
                        transform.MultiplyPoint(new Vector3(Mathf.Min(maxTime, firstPoint.x), firstPoint.y, 0))
                        );
                }
            }

            // Draw post wrapping
            if (postWrap == WrapMode.Loop)
            {
                Matrix4x4 firstTransform = Handles.matrix * transform;
                Matrix4x4 iterTransform = Matrix4x4.TRS(new Vector3(rangeEnd - rangeStart, 0.0f, 0.0f), Quaternion.identity, Vector3.one);

                //  Render straight lines connecting one loop to another.
                Matrix4x4 prevTransform = firstTransform;
                for (int r = 1; r <= maxRep; ++r)
                {
                    Matrix4x4 curTransform = prevTransform * iterTransform;

                    Handles.DrawLine(
                        prevTransform.MultiplyPoint(lastPoint),
                        curTransform.MultiplyPoint(firstPoint)
                        );

                    prevTransform = curTransform;
                }

                //  Render loop curve itself.
                mat.SetPass(0);
                prevTransform = firstTransform;
                for (int r = 1; r <= maxRep; ++r)
                {
                    Matrix4x4 curTransform = prevTransform * iterTransform;
                    Graphics.DrawMeshNow(mesh, curTransform);
                    prevTransform = curTransform;
                }
            }
            else if (postWrap == WrapMode.PingPong)
            {
                mat.SetPass(0);
                for (int r = 1; r <= maxRep; ++r)
                {
                    //  Even loop
                    if (r % 2 == 0)
                    {
                        Matrix4x4 shiftTransform = Matrix4x4.TRS(new Vector3(r * (rangeEnd - rangeStart), 0.0f, 0.0f), Quaternion.identity, Vector3.one);
                        Graphics.DrawMeshNow(mesh, Handles.matrix * transform * shiftTransform);
                    }
                    //  Odd loop
                    else
                    {
                        Matrix4x4 shiftTransform = Matrix4x4.TRS(new Vector3((r + 1) * (rangeEnd - rangeStart) + rangeStart * 2, 0.0f, 0.0f), Quaternion.identity, new Vector3(-1.0f, 1.0f, 1.0f));
                        Graphics.DrawMeshNow(mesh, Handles.matrix * transform * shiftTransform);
                    }
                }
            }
            else
            {
                if (maxRep > 0)
                {
                    Handles.DrawLine(
                        transform.MultiplyPoint(new Vector3(Mathf.Max(minTime, lastPoint.x), lastPoint.y, 0)),
                        transform.MultiplyPoint(new Vector3(maxTime, lastPoint.y, 0))
                        );
                }
            }
        }

        public static void DrawCurveWrapped(float minTime, float maxTime, float rangeStart, float rangeEnd,
            WrapMode preWrap, WrapMode postWrap, Color color, Matrix4x4 transform, Vector3[] points, Color wrapColor)
        {
            if (points.Length == 0)
                return;

            int minRep;
            int maxRep;
            if (rangeEnd - rangeStart != 0)
            {
                minRep = Mathf.FloorToInt((minTime - rangeStart) / (rangeEnd - rangeStart));
                maxRep = Mathf.CeilToInt((maxTime - rangeEnd) / (rangeEnd - rangeStart));

                // Prevent too many loops from being rendered at one time.
                if (minRep < -kMaximumLoops)
                    preWrap = WrapMode.Clamp;
                if (maxRep > kMaximumLoops)
                    postWrap = WrapMode.Clamp;
            }
            else
            {
                preWrap = WrapMode.Clamp;
                postWrap = WrapMode.Clamp;
                minRep = (minTime < rangeStart ? -1 : 0);
                maxRep = (maxTime > rangeEnd   ?  1 : 0);
            }
            int last = points.Length - 1;

            // Draw curve itself
            Handles.color = color;
            List<Vector3> line = new List<Vector3>();

            if (minRep <= 0 && maxRep >= 0)
            {
                // Use line drawing with minimum segment length. Faster for large data sets
                DrawPolyLine(transform, 2, points);
            }
            else
                Handles.DrawPolyLine(points);

            // Draw wrapping
            Handles.color = new Color(wrapColor.r, wrapColor.g, wrapColor.b, wrapColor.a * color.a);

            // Draw pre wrapping
            if (preWrap == WrapMode.Loop)
            {
                line = new List<Vector3>();
                for (int r = minRep; r < 0; r++)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 point = points[i];
                        point.x += r * (rangeEnd - rangeStart);
                        point = transform.MultiplyPoint(point);
                        line.Add(point);
                    }
                }
                line.Add(transform.MultiplyPoint(points[0]));
                Handles.DrawPolyLine(line.ToArray());
            }
            else if (preWrap == WrapMode.PingPong)
            {
                line = new List<Vector3>();
                for (int r = minRep; r < 0; r++)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (r / 2 == r / 2f)
                        {
                            Vector3 point = points[i];
                            point.x += r * (rangeEnd - rangeStart);
                            point = transform.MultiplyPoint(point);
                            line.Add(point);
                        }
                        else
                        {
                            Vector3 point = points[last - i];
                            point.x = -point.x + (r + 1) * (rangeEnd - rangeStart) + rangeStart * 2;
                            point = transform.MultiplyPoint(point);
                            line.Add(point);
                        }
                    }
                }
                Handles.DrawPolyLine(line.ToArray());
            }
            else
            {
                if (minRep < 0)
                {
                    Handles.DrawPolyLine(new Vector3[] {
                        transform.MultiplyPoint(new Vector3(minTime, points[0].y, 0)),
                        transform.MultiplyPoint(new Vector3(Mathf.Min(maxTime, points[0].x), points[0].y, 0))
                    });
                }
            }

            // Draw post wrapping
            if (postWrap == WrapMode.Loop)
            {
                line = new List<Vector3>();
                line.Add(transform.MultiplyPoint(points[last]));
                for (int r = 1; r <= maxRep; r++)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 point = points[i];
                        point.x += r * (rangeEnd - rangeStart);
                        point = transform.MultiplyPoint(point);
                        line.Add(point);
                    }
                }
                Handles.DrawPolyLine(line.ToArray());
            }
            else if (postWrap == WrapMode.PingPong)
            {
                line = new List<Vector3>();
                for (int r = 1; r <= maxRep; r++)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        if (r / 2 == r / 2f)
                        {
                            Vector3 point = points[i];
                            point.x += r * (rangeEnd - rangeStart);
                            point = transform.MultiplyPoint(point);
                            line.Add(point);
                        }
                        else
                        {
                            Vector3 point = points[last - i];
                            point.x = -point.x + (r + 1) * (rangeEnd - rangeStart) + rangeStart * 2;
                            point = transform.MultiplyPoint(point);
                            line.Add(point);
                        }
                    }
                }
                Handles.DrawPolyLine(line.ToArray());
            }
            else
            {
                if (maxRep > 0)
                {
                    Handles.DrawPolyLine(new Vector3[] {
                        transform.MultiplyPoint(new Vector3(Mathf.Max(minTime, points[last].x), points[last].y, 0)),
                        transform.MultiplyPoint(new Vector3(maxTime, points[last].y, 0))
                    });
                }
            }
        }

        public Bounds GetBounds()
        {
            BuildCurveMesh();
            if (m_Bounds == null)
                m_Bounds = m_CurveMesh.bounds;

            return m_Bounds.Value;
        }

        public Bounds GetBounds(float minTime, float maxTime)
        {
            Vector3[] points = GetPoints(minTime, maxTime);
            float min = Mathf.Infinity;
            float max = Mathf.NegativeInfinity;
            for (int i = 0; i < points.Length; ++i)
            {
                Vector3 point = points[i];
                if (point.y > max)
                    max = point.y;
                if (point.y < min)
                    min = point.y;
            }
            if (min == Mathf.Infinity)
            {
                min = 0;
                max = 0;
            }
            return new Bounds(new Vector3((maxTime + minTime) * 0.5f, (max + min) * 0.5f, 0), new Vector3(maxTime - minTime, max - min, 0));
        }

        public void FlushCache()
        {
            Object.DestroyImmediate(m_CurveMesh);
        }
    }
} // namespace
