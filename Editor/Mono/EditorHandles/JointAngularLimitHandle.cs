// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class JointAngularLimitHandle
    {
        private enum ArcType { Solid, Wire }

        private static readonly Matrix4x4 s_XHandleOffset =
            Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.forward), Vector3.one);
        private static readonly Matrix4x4 s_ZHandleOffset =
            Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.left), Vector3.one);

        private static readonly float s_LockedColorAmount = 0.5f;
        private static readonly Color s_LockedColor = new Color(0.5f, 0.5f, 0.5f, 0f);

        // handle functions need to be manually sorted
        private static float GetSortingDistance(ArcHandle handle)
        {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint3x4(
                    Quaternion.AngleAxis(handle.angle, Vector3.up) * Vector3.forward * handle.radius
                    );
            Vector3 toHandle = worldPosition - Camera.current.transform.position;
            if (Camera.current.orthographic)
            {
                Vector3 lookVector = Camera.current.transform.forward;
                toHandle = lookVector * Vector3.Dot(lookVector, toHandle);
            }
            return toHandle.sqrMagnitude;
        }

        private static int CompareHandleFunctionsByDistance(
            KeyValuePair<Action, float> func1, KeyValuePair<Action, float> func2
            )
        {
            return func2.Value.CompareTo(func1.Value);
        }

        private List<KeyValuePair<Action, float>> m_HandleFunctionDistances =
            new List<KeyValuePair<Action, float>>(6);

        private ArcHandle m_XMinHandle;
        private ArcHandle m_XMaxHandle;
        private ArcHandle m_YMinHandle;
        private ArcHandle m_YMaxHandle;
        private ArcHandle m_ZMinHandle;
        private ArcHandle m_ZMaxHandle;

        // when primary axis is limited, secondary and tertiary manipulators are reoriented about its average
        private Matrix4x4 m_SecondaryAxesMatrix;

        public float xMin
        {
            get
            {
                switch (xMotion)
                {
                    case ConfigurableJointMotion.Free:
                        return xRange.x;
                    case ConfigurableJointMotion.Locked:
                        return 0f;
                    default:
                        return Mathf.Clamp(m_XMinHandle.angle, xRange.x, m_XMaxHandle.angle);
                }
            }
            set { m_XMinHandle.angle = value; }
        }

        public float xMax
        {
            get
            {
                switch (xMotion)
                {
                    case ConfigurableJointMotion.Free:
                        return xRange.y;
                    case ConfigurableJointMotion.Locked:
                        return 0f;
                    default:
                        return Mathf.Clamp(m_XMaxHandle.angle, m_XMinHandle.angle, xRange.y);
                }
            }
            set { m_XMaxHandle.angle = value; }
        }

        public float yMin
        {
            get
            {
                switch (yMotion)
                {
                    case ConfigurableJointMotion.Free:
                        return yRange.x;
                    case ConfigurableJointMotion.Locked:
                        return 0f;
                    default:
                        return Mathf.Clamp(m_YMinHandle.angle, yRange.x, m_YMaxHandle.angle);
                }
            }
            set { m_YMinHandle.angle = value; }
        }

        public float yMax
        {
            get
            {
                switch (yMotion)
                {
                    case ConfigurableJointMotion.Free:
                        return yRange.y;
                    case ConfigurableJointMotion.Locked:
                        return 0f;
                    default:
                        return Mathf.Clamp(m_YMaxHandle.angle, m_YMinHandle.angle, yRange.y);
                }
            }
            set { m_YMaxHandle.angle = value; }
        }

        public float zMin
        {
            get
            {
                switch (zMotion)
                {
                    case ConfigurableJointMotion.Free:
                        return zRange.x;
                    case ConfigurableJointMotion.Locked:
                        return 0f;
                    default:
                        return Mathf.Clamp(m_ZMinHandle.angle, zRange.x, m_ZMaxHandle.angle);
                }
            }
            set { m_ZMinHandle.angle = value; }
        }

        public float zMax
        {
            get
            {
                switch (zMotion)
                {
                    case ConfigurableJointMotion.Free:
                        return zRange.y;
                    case ConfigurableJointMotion.Locked:
                        return 0f;
                    default:
                        return Mathf.Clamp(m_ZMaxHandle.angle, m_ZMinHandle.angle, zRange.y);
                }
            }
            set { m_ZMaxHandle.angle = value; }
        }

        public Vector2 xRange { get; set; }

        public Vector2 yRange { get; set; }

        public Vector2 zRange { get; set; }

        public ConfigurableJointMotion xMotion { get; set; }

        public ConfigurableJointMotion yMotion { get; set; }

        public ConfigurableJointMotion zMotion { get; set; }

        public Color xHandleColor
        {
            get
            {
                // Handles.xAxisColor uses EditorPrefs so it cannot be called from a field initializer
                if (!m_XHandleColorInitialized)
                    xHandleColor = Handles.xAxisColor;
                return m_XMinHandle.angleHandleColor;
            }
            set
            {
                m_XMinHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_XMaxHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_XHandleColorInitialized = true;
            }
        }
        private bool m_XHandleColorInitialized = false;

        public Color yHandleColor
        {
            get
            {
                // Handles.yAxisColor uses EditorPrefs so it cannot be called from a field initializer
                if (!m_YHandleColorInitialized)
                    yHandleColor = Handles.yAxisColor;
                return m_YMinHandle.angleHandleColor;
            }
            set
            {
                m_YMinHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_YMaxHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_YHandleColorInitialized = true;
            }
        }
        private bool m_YHandleColorInitialized = false;

        public Color zHandleColor
        {
            get
            {
                // Handles.zAxisColor uses EditorPrefs so it cannot be called from a field initializer
                if (!m_ZHandleColorInitialized)
                    zHandleColor = Handles.zAxisColor;
                return m_ZMinHandle.angleHandleColor;
            }
            set
            {
                m_ZMinHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_ZMaxHandle.SetColorWithoutRadiusHandle(value, fillAlpha);
                m_ZHandleColorInitialized = true;
            }
        }
        private bool m_ZHandleColorInitialized = false;

        public float radius
        {
            get { return m_XMinHandle.radius; }
            set
            {
                m_XMinHandle.radius = value;
                m_XMaxHandle.radius = value;
                m_YMinHandle.radius = value;
                m_YMaxHandle.radius = value;
                m_ZMinHandle.radius = value;
                m_ZMaxHandle.radius = value;
            }
        }

        public float fillAlpha { get; set; }

        public float wireframeAlpha { get; set; }

        public Handles.CapFunction angleHandleDrawFunction
        {
            get { return m_XMinHandle.angleHandleDrawFunction; }
            set
            {
                m_XMinHandle.angleHandleDrawFunction = value;
                m_XMaxHandle.angleHandleDrawFunction = value;
                m_YMinHandle.angleHandleDrawFunction = value;
                m_YMaxHandle.angleHandleDrawFunction = value;
                m_ZMinHandle.angleHandleDrawFunction = value;
                m_ZMaxHandle.angleHandleDrawFunction = value;
            }
        }

        public Handles.SizeFunction angleHandleSizeFunction
        {
            get { return m_XMinHandle.angleHandleSizeFunction; }
            set
            {
                m_XMinHandle.angleHandleSizeFunction = value;
                m_XMaxHandle.angleHandleSizeFunction = value;
                m_YMinHandle.angleHandleSizeFunction = value;
                m_YMaxHandle.angleHandleSizeFunction = value;
                m_ZMinHandle.angleHandleSizeFunction = value;
                m_ZMaxHandle.angleHandleSizeFunction = value;
            }
        }

        public JointAngularLimitHandle()
        {
            m_XMinHandle = new ArcHandle();
            m_XMaxHandle = new ArcHandle();
            m_YMinHandle = new ArcHandle();
            m_YMaxHandle = new ArcHandle();
            m_ZMinHandle = new ArcHandle();
            m_ZMaxHandle = new ArcHandle();
            xMotion = yMotion = zMotion = ConfigurableJointMotion.Limited;
            radius = 1f;
            fillAlpha = 0.1f;
            wireframeAlpha = 1f;
            xRange = yRange = zRange = new Vector2(-180f, 180f);
        }

        public void DrawHandle()
        {
            m_SecondaryAxesMatrix = Handles.matrix;

            // ensure handle colors are up to date
            xHandleColor = xHandleColor;
            yHandleColor = yHandleColor;
            zHandleColor = zHandleColor;
            m_XMinHandle.fillColor = m_XMinHandle.wireframeColor = Color.clear;
            m_XMaxHandle.fillColor = m_XMaxHandle.wireframeColor = Color.clear;
            m_YMinHandle.fillColor = m_YMinHandle.wireframeColor = Color.clear;
            m_YMaxHandle.fillColor = m_YMaxHandle.wireframeColor = Color.clear;
            m_ZMinHandle.fillColor = m_ZMinHandle.wireframeColor = Color.clear;
            m_ZMaxHandle.fillColor = m_ZMaxHandle.wireframeColor = Color.clear;

            // draw fill shapes as needed
            Color fillScalar = new Color(1f, 1f, 1f, fillAlpha);
            bool drawX = false, drawY = false, drawZ = false;
            switch (xMotion)
            {
                case ConfigurableJointMotion.Free:
                    using (new Handles.DrawingScope(Handles.color * xHandleColor))
                    {
                        Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                        Handles.color *= fillScalar;
                        Handles.DrawSolidDisc(Vector3.zero, Vector3.right, radius);
                    }
                    break;
                case ConfigurableJointMotion.Limited:
                    drawX = true;
                    m_SecondaryAxesMatrix *=
                        Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis((xMin + xMax) * 0.5f, Vector3.left), Vector3.one);
                    if (yMotion == ConfigurableJointMotion.Limited)
                    {
                        DrawMultiaxialFillShape();
                    }
                    else
                    {
                        using (new Handles.DrawingScope(Handles.matrix * s_XHandleOffset))
                            DrawArc(m_XMinHandle, m_XMaxHandle, xHandleColor * fillScalar, ArcType.Solid);
                    }
                    break;
                case ConfigurableJointMotion.Locked:
                    using (new Handles.DrawingScope(Handles.color * Color.Lerp(xHandleColor, s_LockedColor, s_LockedColorAmount)))
                        Handles.DrawWireDisc(Vector3.zero, Vector3.right, radius);
                    break;
            }
            using (new Handles.DrawingScope(m_SecondaryAxesMatrix))
            {
                switch (yMotion)
                {
                    case ConfigurableJointMotion.Free:
                        using (new Handles.DrawingScope(Handles.color * yHandleColor))
                        {
                            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                            Handles.color *= fillScalar;
                            Handles.DrawSolidDisc(Vector3.zero, Vector3.up, radius);
                        }
                        break;
                    case ConfigurableJointMotion.Limited:
                        drawY = true;
                        if (xMotion != ConfigurableJointMotion.Limited)
                            DrawArc(m_YMinHandle, m_YMaxHandle, yHandleColor * fillScalar, ArcType.Solid);
                        break;
                    case ConfigurableJointMotion.Locked:
                        using (new Handles.DrawingScope(Handles.color * Color.Lerp(yHandleColor, s_LockedColor, s_LockedColorAmount)))
                            Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                        break;
                }
                switch (zMotion)
                {
                    case ConfigurableJointMotion.Free:
                        using (new Handles.DrawingScope(Handles.color * zHandleColor))
                        {
                            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
                            Handles.color *= fillScalar;
                            Handles.DrawSolidDisc(Vector3.zero, Vector3.forward, radius);
                        }
                        break;
                    case ConfigurableJointMotion.Limited:
                        using (new Handles.DrawingScope(Handles.matrix * s_ZHandleOffset))
                            DrawArc(m_ZMinHandle, m_ZMaxHandle, zHandleColor * fillScalar, ArcType.Solid);
                        drawZ = true;
                        break;
                    case ConfigurableJointMotion.Locked:
                        using (new Handles.DrawingScope(Handles.color * Color.Lerp(zHandleColor, s_LockedColor, s_LockedColorAmount)))
                            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
                        break;
                }
            }

            // collect and sort handle drawing functions for all enabled axes
            m_HandleFunctionDistances.Clear();
            m_XMinHandle.GetControlIDs();
            m_XMaxHandle.GetControlIDs();
            m_YMinHandle.GetControlIDs();
            m_YMaxHandle.GetControlIDs();
            m_ZMinHandle.GetControlIDs();
            m_ZMaxHandle.GetControlIDs();
            if (drawX)
            {
                using (new Handles.DrawingScope(Handles.matrix * s_XHandleOffset))
                {
                    DrawArc(m_XMinHandle, m_XMaxHandle, xHandleColor, ArcType.Wire);

                    m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawXMinHandle, GetSortingDistance(m_XMinHandle)));
                    m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawXMaxHandle, GetSortingDistance(m_XMaxHandle)));
                }
            }
            using (new Handles.DrawingScope(m_SecondaryAxesMatrix))
            {
                if (drawY)
                {
                    DrawArc(m_YMinHandle, m_YMaxHandle, yHandleColor, ArcType.Wire);

                    m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawYMinHandle, GetSortingDistance(m_YMinHandle)));
                    m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawYMaxHandle, GetSortingDistance(m_YMaxHandle)));
                }
                if (drawZ)
                {
                    using (new Handles.DrawingScope(Handles.matrix * s_ZHandleOffset))
                    {
                        DrawArc(m_ZMinHandle, m_ZMaxHandle, zHandleColor, ArcType.Wire);

                        m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawZMinHandle, GetSortingDistance(m_ZMinHandle)));
                        m_HandleFunctionDistances.Add(new KeyValuePair<Action, float>(DrawZMaxHandle, GetSortingDistance(m_ZMaxHandle)));
                    }
                }
            }
            m_HandleFunctionDistances.Sort(CompareHandleFunctionsByDistance);

            // draw handles
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
                        Handles.DrawSolidArc(Vector3.zero, Vector3.up, Vector3.forward, 360f, radius);
                    Handles.DrawSolidArc(Vector3.zero, Vector3.up, forward, angle % 360f, radius);
                }
                else
                {
                    for (int i = 0, revolutions = (int)Mathf.Abs(angle) / 360; i < revolutions; ++i)
                        Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, 360f, radius);
                    Handles.DrawWireArc(Vector3.zero, Vector3.up, forward, angle % 360f, radius);
                }
            }
        }

        private void DrawMultiaxialFillShape()
        {
            Quaternion toXMin = Quaternion.AngleAxis(xMin, Vector3.left);
            Quaternion toXMax = Quaternion.AngleAxis(xMax, Vector3.left);
            Quaternion toYMin = Quaternion.AngleAxis(yMin, Vector3.up);
            Quaternion toYMax = Quaternion.AngleAxis(yMax, Vector3.up);

            Color fillAlphaScalar = new Color(1f, 1f, 1f, fillAlpha);
            Vector3 start;
            using (new Handles.DrawingScope(Handles.color * (yHandleColor * fillAlphaScalar)))
            {
                float yAngle = yMax - yMin;

                start = toXMax * toYMax * Vector3.forward;
                Handles.DrawSolidArc(Vector3.zero, toXMax * Vector3.down, start, yAngle, radius);

                start = toXMin * toYMax * Vector3.forward;
                Handles.DrawSolidArc(Vector3.zero, toXMin * Vector3.down, start, yAngle, radius);
            }
            using (new Handles.DrawingScope(Handles.color * (xHandleColor * fillAlphaScalar)))
            {
                float xAngle = xMax - xMin;

                start = toXMax * toYMax * Vector3.forward;
                Handles.DrawSolidArc(Vector3.zero, Vector3.right, start, xAngle, radius);

                start = toXMax * toYMin * Vector3.forward;
                Handles.DrawSolidArc(Vector3.zero, Vector3.right, start, xAngle, radius);
            }
        }

        private void DrawXMinHandle()
        {
            using (new Handles.DrawingScope(Handles.matrix * s_XHandleOffset))
            {
                m_XMinHandle.DrawHandle();
                m_XMinHandle.angle = Mathf.Clamp(m_XMinHandle.angle, xRange.x, m_XMaxHandle.angle);
            }
        }

        private void DrawXMaxHandle()
        {
            using (new Handles.DrawingScope(Handles.matrix * s_XHandleOffset))
            {
                m_XMaxHandle.DrawHandle();
                m_XMaxHandle.angle = Mathf.Clamp(m_XMaxHandle.angle, m_XMinHandle.angle, xRange.y);
            }
        }

        private void DrawYMinHandle()
        {
            using (new Handles.DrawingScope(m_SecondaryAxesMatrix))
            {
                m_YMinHandle.DrawHandle();
                m_YMinHandle.angle = Mathf.Clamp(m_YMinHandle.angle, yRange.x, m_YMaxHandle.angle);
            }
        }

        private void DrawYMaxHandle()
        {
            using (new Handles.DrawingScope(m_SecondaryAxesMatrix))
            {
                m_YMaxHandle.DrawHandle();
                m_YMaxHandle.angle = Mathf.Clamp(m_YMaxHandle.angle, m_YMinHandle.angle, yRange.y);
            }
        }

        private void DrawZMinHandle()
        {
            using (new Handles.DrawingScope(m_SecondaryAxesMatrix * s_ZHandleOffset))
            {
                m_ZMinHandle.DrawHandle();
                m_ZMinHandle.angle = Mathf.Clamp(m_ZMinHandle.angle, zRange.x, m_ZMaxHandle.angle);
            }
        }

        private void DrawZMaxHandle()
        {
            using (new Handles.DrawingScope(m_SecondaryAxesMatrix * s_ZHandleOffset))
            {
                m_ZMaxHandle.DrawHandle();
                m_ZMaxHandle.angle = Mathf.Clamp(m_ZMaxHandle.angle, m_ZMinHandle.angle, zRange.y);
            }
        }
    }
}
