// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class ArcHandle
    {
        static readonly float s_DefaultAngleHandleSize = 0.08f;
        static readonly float s_DefaultAngleHandleSizeRatio = 1.25f;

        static readonly float s_DefaultRadiusHandleSize = 0.03f;

        static float DefaultAngleHandleSizeFunction(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position) * s_DefaultAngleHandleSize;
        }

        static float DefaultRadiusHandleSizeFunction(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position) * s_DefaultRadiusHandleSize;
        }

        static void DefaultRadiusHandleDrawFunction(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
            )
        {
            Handles.DotHandleCap(controlID, position, rotation, size, eventType);
        }

        private bool m_ControlIDsReserved = false;
        private int m_AngleHandleControlID;
        private int[] m_RadiusHandleControlIDs = new int[4];
        private Quaternion m_MostRecentValidAngleHandleOrientation = Quaternion.identity;

        public float angle { get; set; }

        public float radius { get; set; }

        public Color angleHandleColor { get; set; }

        public Color radiusHandleColor { get; set; }

        public Color fillColor { get; set; }

        public Color wireframeColor { get; set; }

        public Handles.CapFunction angleHandleDrawFunction { get; set; }

        public Handles.SizeFunction angleHandleSizeFunction { get; set; }

        public Handles.CapFunction radiusHandleDrawFunction { get; set; }

        public Handles.SizeFunction radiusHandleSizeFunction { get; set; }

        public ArcHandle()
        {
            radius = 1f;
            SetColorWithoutRadiusHandle(Color.white, 0.1f);
        }

        public void SetColorWithoutRadiusHandle(Color color, float fillColorAlpha)
        {
            SetColorWithRadiusHandle(color, fillColorAlpha);
            radiusHandleColor = Color.clear;
            wireframeColor = color;
        }

        public void SetColorWithRadiusHandle(Color color, float fillColorAlpha)
        {
            fillColor = color * new Color(1f, 1f, 1f, fillColorAlpha);
            angleHandleColor = color;
            radiusHandleColor = color;
            wireframeColor = color;
        }

        public void DrawHandle()
        {
            if (!m_ControlIDsReserved)
                GetControlIDs();
            m_ControlIDsReserved = false;

            if (Handles.color.a == 0f)
                return;

            Vector3 scale = Handles.matrix.MultiplyPoint3x4(Vector3.one) - Handles.matrix.MultiplyPoint3x4(Vector3.zero);
            if (scale.x == 0f && scale.z == 0f)
                return;

            Vector3 angleHandlePosition = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * radius;

            float absAngle = Mathf.Abs(angle);
            float excessAngle = angle % 360f;

            using (new Handles.DrawingScope(Handles.color * fillColor))
            {
                if (Handles.color.a > 0f)
                {
                    for (int i = 0, revolutions = (int)absAngle / 360; i < revolutions; ++i)
                        Handles.DrawSolidArc(Vector3.zero, Vector3.up, Vector3.forward, 360f, radius);
                    Handles.DrawSolidArc(Vector3.zero, Vector3.up, Vector3.forward, excessAngle, radius);
                }
            }

            using (new Handles.DrawingScope(Handles.color * wireframeColor))
            {
                if (Handles.color.a > 0f)
                {
                    Handles.DrawWireArc(
                        Vector3.zero, Vector3.up, Vector3.forward, absAngle >= 360f ? 360f : excessAngle, radius
                        );
                }
            }

            // unless holding alt while already manipulating a control, exit before drawing control handles when holding alt, since alt-click will rotate scene view
            if (Event.current.alt)
            {
                bool exit = true;
                foreach (var id in m_RadiusHandleControlIDs)
                {
                    if (id == GUIUtility.hotControl)
                    {
                        exit = false;
                        break;
                    }
                }
                if (exit && GUIUtility.hotControl != m_AngleHandleControlID)
                    return;
            }

            using (new Handles.DrawingScope(Handles.color * radiusHandleColor))
            {
                if (Handles.color.a > 0f)
                {
                    // draw one radius handle in each cardinal direction encompassed by the arc (i.e. every 90 degrees)
                    float direction = Mathf.Sign(angle);
                    int numRadiusHandles = Mathf.Min(1 + (int)(Mathf.Min(360f, absAngle) * 0.01111111111f), 4);
                    for (int i = 0; i < numRadiusHandles; ++i)
                    {
                        Quaternion handleOrientation = Quaternion.AngleAxis(i * 90f * direction, Vector3.up);
                        using (new Handles.DrawingScope(Handles.matrix * Matrix4x4.TRS(Vector3.zero, handleOrientation, Vector3.one)))
                        {
                            Vector3 radiusHandlePosition = Vector3.forward * radius;
                            Vector3 newPosition;
                            EditorGUI.BeginChangeCheck();
                            {
                                float size = radiusHandleSizeFunction == null ?
                                    DefaultRadiusHandleSizeFunction(radiusHandlePosition) :
                                    radiusHandleSizeFunction(radiusHandlePosition);
                                newPosition = Handles.Slider(
                                        m_RadiusHandleControlIDs[i],
                                        radiusHandlePosition,
                                        Vector3.forward,
                                        size,
                                        radiusHandleDrawFunction ?? DefaultRadiusHandleDrawFunction,
                                        SnapSettings.move.z
                                        );
                            }
                            if (EditorGUI.EndChangeCheck())
                            {
                                radius += (newPosition - radiusHandlePosition).z;
                            }
                        }
                    }
                }
            }

            // draw angle handle last so it will always take precedence when overlapping a radius handle
            using (new Handles.DrawingScope(Handles.color * angleHandleColor))
            {
                if (Handles.color.a > 0f)
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        float size = angleHandleSizeFunction == null ?
                            DefaultAngleHandleSizeFunction(angleHandlePosition) :
                            angleHandleSizeFunction(angleHandlePosition);
                        angleHandlePosition = Handles.Slider2D(
                                m_AngleHandleControlID,
                                angleHandlePosition,
                                Vector3.up,
                                Vector3.forward,
                                Vector3.right,
                                size,
                                angleHandleDrawFunction ?? DefaultAngleHandleDrawFunction,
                                Vector2.zero
                                );
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        float newAngle = Vector3.Angle(Vector3.forward, angleHandlePosition) *
                            Mathf.Sign(Vector3.Dot(Vector3.right, angleHandlePosition));
                        angle += Mathf.DeltaAngle(angle, newAngle);
                        angle = Handles.SnapValue(angle, SnapSettings.rotation);
                    }
                }
            }
        }

        internal void GetControlIDs()
        {
            m_AngleHandleControlID = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);
            for (int i = 0; i < m_RadiusHandleControlIDs.Length; ++i)
                m_RadiusHandleControlIDs[i] = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);
            m_ControlIDsReserved = true;
        }

        private void DefaultAngleHandleDrawFunction(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
            )
        {
            Handles.DrawLine(Vector3.zero, position);

            // draw a cylindrical "hammer head" to indicate the direction the handle will move
            Vector3 worldPosition = Handles.matrix.MultiplyPoint3x4(position);
            Vector3 normal = worldPosition - Handles.matrix.MultiplyPoint3x4(Vector3.zero);
            Vector3 tangent = Handles.matrix.MultiplyVector(Quaternion.AngleAxis(90f, Vector3.up) * position);
            m_MostRecentValidAngleHandleOrientation = rotation = tangent.sqrMagnitude == 0f ?
                    m_MostRecentValidAngleHandleOrientation : Quaternion.LookRotation(tangent, normal);
            Matrix4x4 matrix =
                Matrix4x4.TRS(worldPosition, rotation, (Vector3.one + Vector3.forward * s_DefaultAngleHandleSizeRatio));
            using (new Handles.DrawingScope(matrix))
                Handles.CylinderHandleCap(controlID, Vector3.zero, Quaternion.identity, size, eventType);
        }
    }
}
