// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor
{
    internal class JointAngularLimitHandle2D
    {
        public enum JointMotion
        {
            Locked = 0,
            Limited = 1,
            Free = 2
        }

        private enum ArcType
        {
            Solid = 0,
            Wire = 1
        }

        private static readonly Matrix4x4 s_HandleOffset = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.forward), Vector3.one);
        private static readonly float s_LockedColorAmount = 0.5f;
        private static readonly Color s_LockedColor = new Color(0.5f, 0.5f, 0.5f, 0f);

        private List<KeyValuePair<Action, float>> m_HandleFunctionDistances = new List<KeyValuePair<Action, float>>(6);
        private ArcHandle m_MinHandle;
        private ArcHandle m_MaxHandle;
        private bool m_HandleColorInitialized = false;

        // When primary axis is limited, secondary and tertiary manipulators are reoriented about its average
        private Matrix4x4 m_SecondaryAxesMatrix;

        public float min
        {
            get
            {
                switch (jointMotion)
                {
                    case JointMotion.Free:
                        return range.x;

                    case JointMotion.Locked:
                        return 0f;

                    default:
                        return Mathf.Clamp(m_MinHandle.angle, range.x, m_MaxHandle.angle);
                }
            }
            set { m_MinHandle.angle = value; }
        }

        public float max
        {
            get
            {
                switch (jointMotion)
                {
                    case JointMotion.Free:
                        return range.y;

                    case JointMotion.Locked:
                        return 0f;

                    default:
                        return Mathf.Clamp(m_MaxHandle.angle, m_MinHandle.angle, range.y);
                }
            }
            set { m_MaxHandle.angle = value; }
        }

        public Vector2 range { get; set; }

        public JointMotion jointMotion { get; set; }

        public Color handleColor
        {
            get
            {
                if (!m_HandleColorInitialized)
                    handleColor = Handles.xAxisColor;

                return m_MinHandle.angleHandleColor;
            }
            set
            {
                m_MinHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_MaxHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_HandleColorInitialized = true;
            }
        }

        public float radius
        {
            get { return m_MinHandle.radius; }
            set { m_MinHandle.radius = m_MaxHandle.radius = value; }
        }

        public float fillAlpha { get; set; }

        public float wireframeAlpha { get; set; }

        public Handles.CapFunction angleHandleDrawFunction
        {
            get { return m_MinHandle.angleHandleDrawFunction; }
            set { m_MinHandle.angleHandleDrawFunction = m_MaxHandle.angleHandleDrawFunction = value; }
        }

        public Handles.SizeFunction angleHandleSizeFunction
        {
            get { return m_MinHandle.angleHandleSizeFunction; }
            set { m_MinHandle.angleHandleSizeFunction = m_MaxHandle.angleHandleSizeFunction = value; }
        }

        // Handle functions need to be manually sorted
        private static float GetSortingDistance(ArcHandle handle)
        {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint3x4(Quaternion.AngleAxis(handle.angle, Vector3.up) * Vector3.forward * handle.radius);

            Vector3 toHandle = Camera.current == null
                ? worldPosition
                : worldPosition - Camera.current.transform.position;

            if (Camera.current == null || Camera.current.orthographic)
            {
                Vector3 lookVector = Camera.current == null ? Vector3.forward : Camera.current.transform.forward;
                toHandle = lookVector * Vector3.Dot(lookVector, toHandle);
            }

            return toHandle.sqrMagnitude;
        }

        private static int CompareHandleFunctionsByDistance(KeyValuePair<Action, float> func1, KeyValuePair<Action, float> func2)
        {
            return func2.Value.CompareTo(func1.Value);
        }

        public JointAngularLimitHandle2D()
        {
            m_MinHandle = new ArcHandle();
            m_MaxHandle = new ArcHandle();
            jointMotion = JointMotion.Limited;
            radius = 1f;
            fillAlpha = 0.1f;
            wireframeAlpha = 1f;
            range = new Vector2(-180f, 180f);
        }

        public void DrawHandle()
        {
            m_SecondaryAxesMatrix = Handles.matrix;

            // Ensure handle colors are up to date
            handleColor = handleColor;
            m_MinHandle.fillColor = m_MinHandle.wireframeColor = Color.clear;
            m_MaxHandle.fillColor = m_MaxHandle.wireframeColor = Color.clear;

            // Draw fill shapes as needed
            Color fillScalar = new Color(1f, 1f, 1f, fillAlpha);
            bool draw = false;

            switch (jointMotion)
            {
                case JointMotion.Free:
                    using (new Handles.DrawingScope(Handles.color * handleColor))
                    {
                        Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                        Handles.color *= fillScalar;
                        Handles.DrawSolidDisc(Vector3.zero, Vector3.right, radius);
                    }
                    break;

                case JointMotion.Limited:
                    draw = true;
                    m_SecondaryAxesMatrix *= Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis((min + max) * 0.5f, Vector3.left), Vector3.one);

                    using (new Handles.DrawingScope(Handles.matrix * s_HandleOffset))
                    {
                        DrawArc(m_MinHandle, m_MaxHandle, handleColor * fillScalar, ArcType.Solid);
                    }
                    break;

                case JointMotion.Locked:
                    using (new Handles.DrawingScope(Handles.color * Color.Lerp(handleColor, s_LockedColor, s_LockedColorAmount)))
                    {
                        Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                    }
                    break;
            }

            // Collect and sort handle drawing functions for all enabled axes
            m_HandleFunctionDistances.Clear();
            m_MinHandle.GetControlIDs();
            m_MaxHandle.GetControlIDs();

            if (draw)
            {
                using (new Handles.DrawingScope(Handles.matrix * s_HandleOffset))
                {
                    DrawArc(m_MinHandle, m_MaxHandle, handleColor, ArcType.Wire);

                    m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawMinHandle, GetSortingDistance(m_MinHandle)));
                    m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawMaxHandle, GetSortingDistance(m_MaxHandle)));
                }
            }
            m_HandleFunctionDistances.Sort(CompareHandleFunctionsByDistance);

            // Draw handles
            foreach (var handleFunction in m_HandleFunctionDistances)
                handleFunction.Key();
        }

        private void DrawArc(ArcHandle minHandle, ArcHandle maxHandle, Color color, ArcType arcType)
        {
            float angle = maxHandle.angle - minHandle.angle;
            Vector3 forward = Quaternion.AngleAxis(minHandle.angle, Vector3.up) * Vector3.forward;

            using (new Handles.DrawingScope(Handles.color * color))
            {
                if (arcType == ArcType.Solid)
                {
                    for (int i = 0, revolutions = (int)Mathf.Abs(angle) / 360; i < revolutions; ++i)
                    {
                        Handles.DrawSolidArc(Vector3.zero, Vector3.up, Vector3.forward, 360f, radius);
                    }
                    Handles.DrawSolidArc(Vector3.zero, Vector3.up, forward, angle % 360f, radius);
                }
                else
                {
                    for (int i = 0, revolutions = (int)Mathf.Abs(angle) / 360; i < revolutions; ++i)
                    {
                        Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, 360f, radius);
                    }
                    Handles.DrawWireArc(Vector3.zero, Vector3.up, forward, angle % 360f, radius);
                }
            }
        }

        private void DrawMinHandle()
        {
            using (new Handles.DrawingScope(Handles.matrix * s_HandleOffset))
            {
                m_MinHandle.DrawHandle();
                m_MinHandle.angle = Mathf.Clamp(m_MinHandle.angle, range.x, m_MaxHandle.angle);
            }
        }

        private void DrawMaxHandle()
        {
            using (new Handles.DrawingScope(Handles.matrix * s_HandleOffset))
            {
                m_MaxHandle.DrawHandle();
                m_MaxHandle.angle = Mathf.Clamp(m_MaxHandle.angle, m_MinHandle.angle, range.y);
            }
        }
    }
}
