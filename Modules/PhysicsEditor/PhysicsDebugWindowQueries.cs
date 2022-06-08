// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;

using static UnityEditor.PhysicsVisualizationSettings;
using static UnityEditor.PhysicsDebugDraw;
using Unity.Collections;

namespace UnityEditor
{
    public partial class PhysicsDebugWindow : EditorWindow
    {
        private const float c_LineThickness = 2f;
        private const float c_ConeSize = 0.5f;
        private const float c_MaxDistanceFallback = 500f;

        [SerializeField] Dictionary<Query, ShapeDraw> m_ShapesToDraw = new Dictionary<Query, ShapeDraw>();

        #region Query shape definitions
        private abstract class ShapeDraw
        {
            private readonly Func<bool> m_GetShowShape;

            protected ShapeDraw(QueryFilter type)
            {
                m_GetShowShape = () => GetQueryFilterState(type);
            }

            protected void InitDraw()
            {
                Handles.color = queryColor;
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
            }

            public abstract void Draw();
        }

        private abstract class ShapeCastDraw : ShapeDraw
        {
            protected bool m_IsFiniteDistance = true;
            protected float m_Distance;
            protected Vector3 m_Direction;
            protected Quaternion m_LookRotation;
            protected Vector3[] m_Points;
            protected Vector3[] m_SecondaryPoints;
            protected Vector3[] m_ConePoints;

            protected ShapeDraw m_PrimaryShape;
            protected ShapeDraw m_SecondaryShape;

            protected ShapeCastDraw(QueryFilter type
                , Vector3 direction, float distance)
                : base(type)
            {
                m_IsFiniteDistance = IsFinite(distance);
                m_Distance = distance;
                m_Direction = direction;
                m_LookRotation = direction == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(direction);
            }

            public override void Draw()
            {
                InitDraw();

                if(m_PrimaryShape != null)
                    m_PrimaryShape.Draw();

                for (int i = 0; i < m_Points.Length; i++)
                {
                    Handles.DrawLine(m_Points[i], m_SecondaryPoints[i], c_LineThickness);
                    // Makes the arrow heads a lot nicer for close casts
                    var cone = Mathf.Lerp(0f, c_ConeSize, m_Distance / 20f);
                    cone = Mathf.Clamp(cone, 0.1f, c_ConeSize);
                    Handles.ConeHandleCap(0, m_ConePoints[i], m_LookRotation, cone, EventType.Repaint);
                }

                if (m_IsFiniteDistance && m_SecondaryShape != null)
                    m_SecondaryShape.Draw();
            }

            protected static bool IsFinite(float f)
            {
                if (f == Mathf.Infinity || f == Mathf.NegativeInfinity || f == float.NaN)
                    return false;
                return true;
            }

            protected void InitPoints()
            {
                GetSecondaryPoints();
                GetConePoints();
            }

            private void GetSecondaryPoints()
            {
                m_SecondaryPoints = new Vector3[m_Points.Length];

                for (int i = 0; i < m_Points.Length; i++)
                {
                    m_SecondaryPoints[i] = m_Points[i] + m_Direction * (m_IsFiniteDistance ? Mathf.Min(m_Distance, c_MaxDistanceFallback) : c_MaxDistanceFallback);
                }
            }

            private void GetConePoints()
            {
                m_ConePoints = new Vector3[m_SecondaryPoints.Length];

                if (m_IsFiniteDistance)
                {
                    for (int i = 0; i < m_SecondaryPoints.Length; i++)
                        m_ConePoints[i] = (m_Points[i] + m_SecondaryPoints[i]) / 2f;
                }
                else
                {
                    for (int i = 0; i < m_SecondaryPoints.Length; i++)
                        m_ConePoints[i] = m_Points[i] + m_Direction * 2f;
                }
            }
        }

        private class RayCast : ShapeCastDraw
        {
            public RayCast(Vector3 origin, Vector3 direction, float distance, QueryFilter type)
                : base(type, direction, distance)
            {
                m_PrimaryShape = null;
                m_SecondaryPoints = null;

                m_Points = new Vector3[1];
                m_Points[0] = origin;

                InitPoints();
            }
        }

        private class SphereOvelap : ShapeDraw
        {
            private Vector3 m_Position;
            private float m_Radius;

            private readonly float h;
            private readonly float r2;

            public SphereOvelap(Vector3 position, float radius, QueryFilter type)
                : base(type)
            {
                m_Position = position;
                m_Radius = radius;

                h = m_Radius * 0.5f;
                r2 = Mathf.Sqrt(2f * h * m_Radius - h * h);
            }

            public override void Draw()
            {
                InitDraw();

                Handles.DrawWireDisc(m_Position, Vector3.up, m_Radius, c_LineThickness);

                Handles.DrawWireDisc(m_Position + new Vector3(0f, h, 0f), Vector3.up, r2, c_LineThickness);
                Handles.DrawWireDisc(m_Position + new Vector3(0f, -h, 0f), Vector3.up, r2, c_LineThickness);

                Handles.DrawWireDisc(m_Position, Vector3.right, m_Radius, c_LineThickness);
                Handles.DrawWireDisc(m_Position, new Vector3(1f, 0f, 1f), m_Radius, c_LineThickness);
                Handles.DrawWireDisc(m_Position, new Vector3(1f, 0f, -1f), m_Radius, c_LineThickness);
                Handles.DrawWireDisc(m_Position, new Vector3(0, 0f, 1f), m_Radius, c_LineThickness);
            }
        }

        private class SphereCast : ShapeCastDraw
        {
            public SphereCast(Vector3 origin, float radius, Vector3 direction, float distance, QueryFilter type)
                :base (type, direction, distance)
            {
                m_PrimaryShape = new SphereOvelap(origin, radius, type);

                var inPlane = new Vector3(1f, 0f, 0f);
                var perpendicular1 = m_LookRotation * inPlane;
                var perpendicular2 = Vector3.Cross(m_Direction, perpendicular1);

                m_Points = new Vector3[4];
                m_Points[0] = origin + perpendicular1 * radius;
                m_Points[1] = origin + (perpendicular1 * -1f) * radius;
                m_Points[2] = origin + perpendicular2 * radius;
                m_Points[3] = origin + (perpendicular2 * -1f) * radius;

                InitPoints();

                m_SecondaryShape = m_IsFiniteDistance ? new SphereOvelap(origin + m_Direction * distance, radius, type) : null;
            }
        }

        private class BoxOverlap : ShapeDraw
        {
            public Vector3[] Corners { get; private set; } = new Vector3[8];

            public BoxOverlap(Vector3 center, Vector3 halfExtents, Quaternion orientation, QueryFilter type)
                : base(type)
            {
                Corners[0] = center + halfExtents;
                Corners[1] = center - halfExtents;
                Corners[2] = center + new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);
                Corners[3] = center + new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
                Corners[4] = center + new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
                Corners[5] = center + new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
                Corners[6] = center + new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);
                Corners[7] = center + new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);

                for (int i = 0; i < Corners.Length; i++)
                {
                    Corners[i] = orientation * (Corners[i] - center) + center;
                }
            }

            public override void Draw()
            {
                InitDraw();

                //Front face
                Handles.DrawLine(Corners[1], Corners[5], c_LineThickness);
                Handles.DrawLine(Corners[5], Corners[4], c_LineThickness);
                Handles.DrawLine(Corners[4], Corners[7], c_LineThickness);
                Handles.DrawLine(Corners[7], Corners[1], c_LineThickness);
                //Back face
                Handles.DrawLine(Corners[0], Corners[3], c_LineThickness);
                Handles.DrawLine(Corners[3], Corners[6], c_LineThickness);
                Handles.DrawLine(Corners[6], Corners[2], c_LineThickness);
                Handles.DrawLine(Corners[2], Corners[0], c_LineThickness);
                //Connections
                Handles.DrawLine(Corners[0], Corners[4], c_LineThickness);
                Handles.DrawLine(Corners[3], Corners[5], c_LineThickness);
                Handles.DrawLine(Corners[6], Corners[1], c_LineThickness);
                Handles.DrawLine(Corners[2], Corners[7], c_LineThickness);
            }
        }

        private class BoxCast : ShapeCastDraw
        {
            public BoxCast(Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, QueryFilter type)
                : base(type, direction, distance)
            {
                var box = new BoxOverlap(center, halfExtents, orientation, type);
                m_PrimaryShape = box;

                m_Points = box.Corners;

                InitPoints();

                m_SecondaryShape = m_IsFiniteDistance ? new BoxOverlap(center + m_Direction * distance, halfExtents, orientation, type) : null;
            }
        }

        private class CapsuleOverlap : ShapeDraw
        {
            private Vector3 m_Point0; // The center of the sphere at the start of the capsule.
            private Vector3 m_Point1; // The center of the sphere at the end of the capsule.
            private float m_Radius;

            public CapsuleOverlap(Vector3 point0, Vector3 point1, float radius, QueryFilter type)
                : base(type)
            {
                m_Point0 = point0;
                m_Point1 = point1;
                m_Radius = radius;
            }

            public override void Draw()
            {
                InitDraw();

                if(m_Point0 == m_Point1)
                {
                    Handles.DrawWireDisc(m_Point0, Vector3.up, m_Radius, c_LineThickness);

                    Handles.DrawWireDisc(m_Point0, Vector3.right, m_Radius, c_LineThickness);
                    Handles.DrawWireDisc(m_Point0, new Vector3(1f, 0f, 1f), m_Radius, c_LineThickness);
                    Handles.DrawWireDisc(m_Point0, new Vector3(1f, 0f, -1f), m_Radius, c_LineThickness);
                    Handles.DrawWireDisc(m_Point0, new Vector3(0, 0f, 1f), m_Radius, c_LineThickness);

                    return;
                }

                Quaternion p1Rotation = Quaternion.LookRotation(m_Point0 - m_Point1);
                Quaternion p2Rotation = Quaternion.LookRotation(m_Point1 - m_Point0);

                float c = Vector3.Dot((m_Point0 - m_Point1).normalized, Vector3.up);
                if (c == 1f || c == -1f)
                {
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
                }
                // First side
                Handles.DrawWireArc(m_Point0, p1Rotation * Vector3.left, p1Rotation * Vector3.down, 180f, m_Radius, c_LineThickness);
                Handles.DrawWireArc(m_Point0, p1Rotation * Vector3.up, p1Rotation * Vector3.left, 180f, m_Radius, c_LineThickness);
                Handles.DrawWireDisc(m_Point0, (m_Point1 - m_Point0).normalized, m_Radius, c_LineThickness);
                // Second side
                Handles.DrawWireArc(m_Point1, p2Rotation * Vector3.left, p2Rotation * Vector3.down, 180f, m_Radius, c_LineThickness);
                Handles.DrawWireArc(m_Point1, p2Rotation * Vector3.up, p2Rotation * Vector3.left, 180f, m_Radius, c_LineThickness);
                Handles.DrawWireDisc(m_Point1, (m_Point0 - m_Point1).normalized, m_Radius, c_LineThickness);
                // Lines
                Handles.DrawLine(m_Point0 + p1Rotation * Vector3.down * m_Radius, m_Point1 + p2Rotation * Vector3.down * m_Radius, c_LineThickness);
                Handles.DrawLine(m_Point0 + p1Rotation * Vector3.left * m_Radius, m_Point1 + p2Rotation * Vector3.right * m_Radius, c_LineThickness);
                Handles.DrawLine(m_Point0 + p1Rotation * Vector3.up * m_Radius, m_Point1 + p2Rotation * Vector3.up * m_Radius, c_LineThickness);
                Handles.DrawLine(m_Point0 + p1Rotation * Vector3.right * m_Radius, m_Point1 + p2Rotation * Vector3.left * m_Radius, c_LineThickness);
            }
        }

        private class CapsuleCast : ShapeCastDraw
        {
            private Vector3 m_Center;

            public CapsuleCast( Vector3 point1, Vector3 point2, float radius, Vector3 direction, float distance, QueryFilter type)
                : base(type, direction, distance)
            {
                m_PrimaryShape = new CapsuleOverlap(point1, point2, radius, type);

                var inPlane = new Vector3(1f, 0f, 0f);
                var perpendicular1 = m_LookRotation * inPlane;

                if (point1 == point2)
                {
                    m_Center = point1;
                    var perpendicular2 = Vector3.Cross(m_Direction, perpendicular1);

                    m_Points = new Vector3[4];
                    m_Points[0] = point1 + perpendicular1 * radius;
                    m_Points[1] = point1 + (perpendicular1 * -1f) * radius;
                    m_Points[2] = point1 + perpendicular2 * radius;
                    m_Points[3] = point1 + (perpendicular2 * -1f) * radius;
                }
                else
                {
                    m_Center = (point1 + point2) / 2f;
                    var capsuleDirection = (point1 - m_Center).normalized;
                    var perpendicular2 = Vector3.Cross(capsuleDirection, perpendicular1);

                    m_Points = new Vector3[10];
                    m_Points[0] = point1 + capsuleDirection * radius;
                    m_Points[1] = point2 + -1f * capsuleDirection * radius;
                    m_Points[2] = point1 + perpendicular1 * radius;
                    m_Points[3] = point1 + -1f * perpendicular1 * radius;
                    m_Points[4] = point2 + perpendicular1 * radius;
                    m_Points[5] = point2 + -1f * perpendicular1 * radius;
                    m_Points[6] = point1 + perpendicular2 * radius;
                    m_Points[7] = point1 + -1f * perpendicular2 * radius;
                    m_Points[8] = point2 + perpendicular2 * radius;
                    m_Points[9] = point2 + -1f * perpendicular2 * radius;
                }

                InitPoints();

                m_SecondaryShape = m_IsFiniteDistance ? new CapsuleOverlap(point1 + m_Direction * distance, point2 + m_Direction * distance, radius, type) : null;
            }
        }
        #endregion

        private void DrawQueriesTab()
        {
            PropertyDrawingWrapper(Style.showQueries , QueryFilter.ShowQueries);

            queryColor = EditorGUILayout.ColorField(Style.queryColor, queryColor);

            EditorGUILayout.LabelField(Style.showShapes);
            EditorGUI.indentLevel++;

            PropertyDrawingWrapper(Style.sphereQueries, QueryFilter.Sphere);

            PropertyDrawingWrapper(Style.boxQueries , QueryFilter.Box);

            PropertyDrawingWrapper(Style.capsuleQueries , QueryFilter.Capsule);

            PropertyDrawingWrapper(Style.rayQueries , QueryFilter.Ray);

            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(Style.showTypes);
            EditorGUI.indentLevel++;

            PropertyDrawingWrapper(Style.overlapQueries , QueryFilter.Overlap);

            PropertyDrawingWrapper(Style.checkQueries , QueryFilter.Check);

            PropertyDrawingWrapper(Style.castQueries , QueryFilter.Cast);

            EditorGUI.indentLevel--;

            maxNumberOfQueries = EditorGUILayout.IntField(
                Style.maxNumberOfQueries, maxNumberOfQueries);

            GUILayout.Space(4);

            GUILayout.BeginHorizontal();

            bool selectNone = GUILayout.Button(Style.showNone);
            bool selectAll = GUILayout.Button(Style.showAll);
            if (selectNone || selectAll)
            {
                SetQueryFilterState(QueryFilter.All, selectAll);
            }

            GUILayout.EndHorizontal();
        }

        private void PropertyDrawingWrapper(GUIContent label, QueryFilter filterMask)
        {
            var value = EditorGUILayout.Toggle(label, GetQueryFilterState(filterMask));
            SetQueryFilterState(filterMask, value);
        }

        private void DrawCastsAndOverlaps()
        {
            foreach(var (query, shape) in m_ShapesToDraw)
                shape.Draw();
        }

        private void ClearQueryShapes()
        {
            m_ShapesToDraw.Clear();
        }

        private void InsertShape(Query query, ShapeDraw shape)
        {
            if (m_ShapesToDraw.Count > maxNumberOfQueries)
                return;

            m_ShapesToDraw.Add(query, shape);
        }

        private void OnQueriesRetrieved(NativeArray<Query> array)
        {
            for (int i = 0; i < array.Length; i++)
                ConstructQueryVisualizationShape(array[i]);
        }

        private static void ConstructQueryVisualizationShape(Query query)
        {
            if (!HasOpenInstances<PhysicsDebugWindow>() || s_Window == null)
                return;

            if (s_Window.m_ShapesToDraw.ContainsKey(query))
                return;

            if (!GetQueryFilterState(query.filter | QueryFilter.ShowQueries))
                return;

            if ((query.filter & QueryFilter.Box) != 0)
            {
                if ((query.filter & (QueryFilter.Check | QueryFilter.Overlap)) != 0)
                    s_Window.InsertShape(query, new BoxOverlap(query.v1, query.v2, query.q, query.filter));
                else if ((query.filter & QueryFilter.Cast) != 0)
                    s_Window.InsertShape(query, new BoxCast(query.v1, query.v2, query.q, query.direction, query.distance, query.filter));
            }
            else if ((query.filter & QueryFilter.Sphere) != 0)
            {
                if ((query.filter & (QueryFilter.Check | QueryFilter.Overlap)) != 0)
                    s_Window.InsertShape(query, new SphereOvelap(query.v1, query.r, query.filter));
                else if ((query.filter & QueryFilter.Cast) != 0)
                    s_Window.InsertShape(query, new SphereCast(query.v1, query.r, query.direction, query.distance, query.filter));
            }
            else if ((query.filter & QueryFilter.Capsule) != 0)
            {
                if ((query.filter & (QueryFilter.Check | QueryFilter.Overlap)) != 0)
                    s_Window.InsertShape(query, new CapsuleOverlap(query.v1, query.v2, query.r, query.filter));
                else if ((query.filter & QueryFilter.Cast) != 0)
                    s_Window.InsertShape(query, new CapsuleCast(query.v1, query.v2, query.r, query.direction, query.distance, query.filter));
            }
            else if ((query.filter & QueryFilter.Ray) != 0)
            {
                if ((query.filter & QueryFilter.Cast) != 0)
                    s_Window.InsertShape(query, new RayCast(query.v1, query.direction, query.distance, query.filter));
            }
        }
    }
}
