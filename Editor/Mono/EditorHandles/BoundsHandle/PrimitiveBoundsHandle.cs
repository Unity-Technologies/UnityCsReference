// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor.IMGUI.Controls
{
    public abstract class PrimitiveBoundsHandle
    {
        [Flags]
        public enum Axes
        {
            None = 0,
            X = 1 << 0,
            Y = 1 << 1,
            Z = 1 << 2,
            All = X | Y | Z
        }

        protected enum HandleDirection
        {
            PositiveX,
            NegativeX,
            PositiveY,
            NegativeY,
            PositiveZ,
            NegativeZ
        }

        private static readonly float s_DefaultMidpointHandleSize = 0.03f;
        private static readonly int[] s_NextAxis = new[] { 1, 2, 0 };

        internal static GUIContent editModeButton
        {
            get
            {
                if (s_EditModeButton == null)
                {
                    s_EditModeButton = new GUIContent(
                            EditorGUIUtility.IconContent("EditCollider").image,
                            EditorGUIUtility.TextContent("Edit bounding volume.\n\n - Hold Alt after clicking control handle to pin center in place.\n - Hold Shift after clicking control handle to scale uniformly.").text
                            );
                }
                return s_EditModeButton;
            }
        }
        private static GUIContent s_EditModeButton;

        private static float DefaultMidpointHandleSizeFunction(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position) * s_DefaultMidpointHandleSize;
        }

        private int[] m_ControlIDs = new int[6] { 0, 0, 0, 0, 0, 0 };
        private Bounds m_Bounds;
        private Bounds m_BoundsOnClick;

        public Vector3 center
        {
            get { return m_Bounds.center; }
            set { m_Bounds.center = value; }
        }

        public Axes axes { get; set; }

        public Color handleColor { get; set; }

        public Color wireframeColor { get; set; }

        public Handles.CapFunction midpointHandleDrawFunction { get; set; }

        public Handles.SizeFunction midpointHandleSizeFunction { get; set; }

        [Obsolete("Use parameterless constructor instead.")]
        public PrimitiveBoundsHandle(int controlIDHint) : this() {}

        public PrimitiveBoundsHandle()
        {
            handleColor = Color.white;
            wireframeColor = Color.white;
            axes = Axes.X | Axes.Y | Axes.Z;
        }

        public void SetColor(Color color)
        {
            handleColor = color;
            wireframeColor = color;
        }

        public void DrawHandle()
        {
            for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                m_ControlIDs[i] = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

            // wireframe (before handles so handles are rendered top most)
            using (new Handles.DrawingScope(Handles.color * wireframeColor))
                DrawWireframe();

            // unless holding alt to pin center, exit before drawing control handles when holding alt, since alt-click will rotate scene view
            if (Event.current.alt)
            {
                bool exit = true;
                foreach (var id in m_ControlIDs)
                {
                    if (id == GUIUtility.hotControl)
                    {
                        exit = false;
                        break;
                    }
                }
                if (exit)
                    return;
            }

            // bounding box extents
            Vector3 minPos = m_Bounds.min;
            Vector3 maxPos = m_Bounds.max;

            // handles
            int prevHotControl = GUIUtility.hotControl;
            Vector3 cameraLocalPos = Handles.inverseMatrix.MultiplyPoint(Camera.current.transform.position);
            bool isCameraInsideBox = m_Bounds.Contains(cameraLocalPos);
            EditorGUI.BeginChangeCheck();
            using (new Handles.DrawingScope(Handles.color * handleColor))
                MidpointHandles(ref minPos, ref maxPos, isCameraInsideBox);
            bool changed = EditorGUI.EndChangeCheck();

            // detect if any handles got hotControl
            if (prevHotControl != GUIUtility.hotControl && GUIUtility.hotControl != 0)
            {
                m_BoundsOnClick = m_Bounds;
            }

            // update if changed
            if (changed)
            {
                // determine which handle changed to apply any further modifications
                m_Bounds.center = (maxPos + minPos) * 0.5f;
                m_Bounds.size = maxPos - minPos;
                for (int i = 0, count = m_ControlIDs.Length; i < count; ++i)
                {
                    if (GUIUtility.hotControl == m_ControlIDs[i])
                        m_Bounds = OnHandleChanged((HandleDirection)i, m_BoundsOnClick, m_Bounds);
                }

                // shift scales uniformly
                if (Event.current.shift)
                {
                    int hotControl = GUIUtility.hotControl;
                    Vector3 size = m_Bounds.size;
                    int scaleAxis = 0;
                    if (hotControl == m_ControlIDs[(int)HandleDirection.PositiveY] || hotControl == m_ControlIDs[(int)HandleDirection.NegativeY])
                    {
                        scaleAxis = 1;
                    }
                    if (hotControl == m_ControlIDs[(int)HandleDirection.PositiveZ] || hotControl == m_ControlIDs[(int)HandleDirection.NegativeZ])
                    {
                        scaleAxis = 2;
                    }
                    float scaleFactor = Mathf.Approximately(m_BoundsOnClick.size[scaleAxis], 0f) ?
                        1f : size[scaleAxis] / m_BoundsOnClick.size[scaleAxis];
                    int nextAxis = s_NextAxis[scaleAxis];
                    size[nextAxis] = scaleFactor * m_BoundsOnClick.size[nextAxis];
                    nextAxis = s_NextAxis[nextAxis];
                    size[nextAxis] = scaleFactor * m_BoundsOnClick.size[nextAxis];
                    m_Bounds.size = size;
                }

                // alt scales from the center
                if (Event.current.alt)
                    m_Bounds.center = m_BoundsOnClick.center;
            }
        }

        protected abstract void DrawWireframe();

        protected virtual Bounds OnHandleChanged(HandleDirection handle, Bounds boundsOnClick, Bounds newBounds)
        {
            return newBounds;
        }

        protected Vector3 GetSize()
        {
            Vector3 size = m_Bounds.size;
            // zero out size on disabled axes
            for (int axis = 0; axis < 3; ++axis)
            {
                if (!IsAxisEnabled(axis))
                    size[axis] = 0f;
            }
            return size;
        }

        protected void SetSize(Vector3 size)
        {
            m_Bounds.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }

        protected bool IsAxisEnabled(Axes axis)
        {
            return (axes & axis) == axis;
        }

        protected bool IsAxisEnabled(int vector3Axis)
        {
            switch (vector3Axis)
            {
                case 0:
                    return IsAxisEnabled(Axes.X);
                case 1:
                    return IsAxisEnabled(Axes.Y);
                case 2:
                    return IsAxisEnabled(Axes.Z);
                default:
                    throw new ArgumentOutOfRangeException("vector3Axis", "Must be 0, 1, or 2");
            }
        }

        private void MidpointHandles(ref Vector3 minPos, ref Vector3 maxPos, bool isCameraInsideBox)
        {
            Vector3 xAxis = Vector3.right;
            Vector3 yAxis = Vector3.up;
            Vector3 zAxis = Vector3.forward;
            Vector3 middle = (maxPos + minPos) * 0.5f;

            Vector3 localPos, newPos;
            if (IsAxisEnabled(Axes.X))
            {
                // +X
                localPos = new Vector3(maxPos.x, middle.y, middle.z);
                newPos = MidpointHandle(m_ControlIDs[(int)HandleDirection.PositiveX], localPos, yAxis, zAxis, isCameraInsideBox);
                maxPos.x = Mathf.Max(newPos.x, minPos.x);

                // -X
                localPos = new Vector3(minPos.x, middle.y, middle.z);
                newPos = MidpointHandle(m_ControlIDs[(int)HandleDirection.NegativeX], localPos, yAxis, -zAxis, isCameraInsideBox);
                minPos.x = Mathf.Min(newPos.x, maxPos.x);
            }

            if (IsAxisEnabled(Axes.Y))
            {
                // +Y
                localPos = new Vector3(middle.x, maxPos.y, middle.z);
                newPos = MidpointHandle(m_ControlIDs[(int)HandleDirection.PositiveY], localPos, xAxis, -zAxis, isCameraInsideBox);
                maxPos.y = Mathf.Max(newPos.y, minPos.y);

                // -Y
                localPos = new Vector3(middle.x, minPos.y, middle.z);
                newPos = MidpointHandle(m_ControlIDs[(int)HandleDirection.NegativeY], localPos, xAxis, zAxis, isCameraInsideBox);
                minPos.y = Mathf.Min(newPos.y, maxPos.y);
            }

            if (IsAxisEnabled(Axes.Z))
            {
                // +Z
                localPos = new Vector3(middle.x, middle.y, maxPos.z);
                newPos = MidpointHandle(m_ControlIDs[(int)HandleDirection.PositiveZ], localPos, yAxis, -xAxis, isCameraInsideBox);
                maxPos.z = Mathf.Max(newPos.z, minPos.z);

                // -Z
                localPos = new Vector3(middle.x, middle.y, minPos.z);
                newPos = MidpointHandle(m_ControlIDs[(int)HandleDirection.NegativeZ], localPos, yAxis, xAxis, isCameraInsideBox);
                minPos.z = Mathf.Min(newPos.z, maxPos.z);
            }
        }

        private Vector3 MidpointHandle(int id, Vector3 localPos, Vector3 localTangent, Vector3 localBinormal, bool isCameraInsideBox)
        {
            Color oldColor = Handles.color;

            AdjustMidpointHandleColor(localPos, localTangent, localBinormal, isCameraInsideBox);

            if (Handles.color.a > 0f)
            {
                Vector3 localDir = Vector3.Cross(localTangent, localBinormal).normalized;

                var drawFunc = midpointHandleDrawFunction ?? Handles.DotHandleCap;
                var sizeFunc = midpointHandleSizeFunction ?? DefaultMidpointHandleSizeFunction;

                localPos = UnityEditorInternal.Slider1D.Do(id, localPos, localDir, sizeFunc(localPos), drawFunc, SnapSettings.scale);
            }

            Handles.color = oldColor;
            return localPos;
        }

        private void AdjustMidpointHandleColor(Vector3 localPos, Vector3 localTangent, Vector3 localBinormal, bool isCameraInsideBox)
        {
            float alphaMultiplier = 1f;

            // if inside the box then ignore backfacing alpha multiplier (otherwise all handles will look disabled)
            if (!isCameraInsideBox && axes == (Axes.X | Axes.Y | Axes.Z))
            {
                // use tangent and binormal to calculate normal in case handle matrix is skewed
                Vector3 worldTangent = Handles.matrix.MultiplyVector(localTangent);
                Vector3 worldBinormal = Handles.matrix.MultiplyVector(localBinormal);
                Vector3 worldDir = Vector3.Cross(worldTangent, worldBinormal).normalized;

                // adjust color if handle is backfacing
                float cosV;

                if (Camera.current.orthographic)
                    cosV = Vector3.Dot(-Camera.current.transform.forward, worldDir);
                else
                    cosV = Vector3.Dot((Camera.current.transform.position - Handles.matrix.MultiplyPoint(localPos)).normalized, worldDir);

                if (cosV < -0.0001f)
                    alphaMultiplier *= Handles.backfaceAlphaMultiplier;
            }

            Handles.color *= new Color(1f, 1f, 1f, alphaMultiplier);
        }
    }
}
