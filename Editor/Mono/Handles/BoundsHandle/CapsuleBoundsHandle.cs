// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class CapsuleBoundsHandle : PrimitiveBoundsHandle
    {
        public enum HeightAxis { X, Y, Z }

        private const int k_DirectionX = 0;
        private const int k_DirectionY = 1;
        private const int k_DirectionZ = 2;

        private static readonly Vector3[] s_HeightAxes = new[] { Vector3.right, Vector3.up, Vector3.forward };
        private static readonly int[] s_NextAxis = new[] { 1, 2, 0 };

        public HeightAxis heightAxis
        {
            get { return (HeightAxis)m_HeightAxis; }
            set
            {
                int newValue = (int)value;
                if (m_HeightAxis == newValue)
                    return;
                Vector3 size = Vector3.one * radius * 2f;
                size[newValue] = GetSize()[m_HeightAxis];
                m_HeightAxis = newValue;
                SetSize(size);
            }
        }
        private int m_HeightAxis = k_DirectionY;

        public float height
        {
            get
            {
                // zero out height if height axis is disabled
                return !IsAxisEnabled(m_HeightAxis) ? 0f : Mathf.Max(GetSize()[m_HeightAxis], 2f * radius);
            }
            set
            {
                // height cannot be less than diameter
                value = Mathf.Max(Mathf.Abs(value), 2f * radius);
                if (height == value)
                    return;
                Vector3 size = GetSize();
                size[m_HeightAxis] = value;
                SetSize(size);
            }
        }

        public float radius
        {
            get
            {
                int radiusAxis;
                // return 0 if only enabled axis is a single radius axis
                if (GetRadiusAxis(out radiusAxis) || IsAxisEnabled(m_HeightAxis))
                    return 0.5f * GetSize()[radiusAxis];
                else
                    return 0f;
            }
            set
            {
                Vector3 size = GetSize();
                float diameter = 2f * value;
                // height cannot be less than diameter
                for (int axis = 0; axis < 3; ++axis)
                    size[axis] = axis == m_HeightAxis ? Mathf.Max(size[axis], diameter) : diameter;
                SetSize(size);
            }
        }

        [Obsolete("Use parameterless constructor instead.")]
        public CapsuleBoundsHandle(int controlIDHint) : base(controlIDHint) {}

        public CapsuleBoundsHandle() : base() {}

        protected override void DrawWireframe()
        {
            HeightAxis radAxis1 = HeightAxis.Y;
            HeightAxis radAxis2 = HeightAxis.Z;
            switch (heightAxis)
            {
                case HeightAxis.Y:
                    radAxis1 = HeightAxis.Z;
                    radAxis2 = HeightAxis.X;
                    break;
                case HeightAxis.Z:
                    radAxis1 = HeightAxis.X;
                    radAxis2 = HeightAxis.Y;
                    break;
            }
            bool doHeightAxis = IsAxisEnabled((int)heightAxis);
            bool doRadiusAxis1 = IsAxisEnabled((int)radAxis1);
            bool doRadiusAxis2 = IsAxisEnabled((int)radAxis2);

            Vector3 hgtAx = s_HeightAxes[m_HeightAxis];
            Vector3 radAx1 = s_HeightAxes[s_NextAxis[m_HeightAxis]];
            Vector3 radAx2 = s_HeightAxes[s_NextAxis[s_NextAxis[m_HeightAxis]]];
            float rad = radius;
            float hgt = height;
            Vector3 top = center + hgtAx * (hgt * 0.5f - rad);
            Vector3 bottom = center - hgtAx * (hgt * 0.5f - rad);

            // draw caps and connecting lines for each enabled axis if height axis is enabled
            if (doHeightAxis)
            {
                if (doRadiusAxis2)
                {
                    Handles.DrawWireArc(top, radAx1, radAx2, 180f, rad);
                    Handles.DrawWireArc(bottom, radAx1, radAx2, -180f, rad);
                    Handles.DrawLine(top + radAx2 * rad, bottom + radAx2 * rad);
                    Handles.DrawLine(top - radAx2 * rad, bottom - radAx2 * rad);
                }
                if (doRadiusAxis1)
                {
                    Handles.DrawWireArc(top, radAx2, radAx1, -180f, rad);
                    Handles.DrawWireArc(bottom, radAx2, radAx1, 180f, rad);
                    Handles.DrawLine(top + radAx1 * rad, bottom + radAx1 * rad);
                    Handles.DrawLine(top - radAx1 * rad, bottom - radAx1 * rad);
                }
            }

            // do cross-section if both radius axes are enabled
            if (doRadiusAxis1 && doRadiusAxis2)
            {
                Handles.DrawWireArc(top, hgtAx, radAx1, 360f, rad);
                Handles.DrawWireArc(bottom, hgtAx, radAx1, -360f, rad);
            }
        }

        protected override Bounds OnHandleChanged(HandleDirection handle, Bounds boundsOnClick, Bounds newBounds)
        {
            int changedAxis = k_DirectionX;
            switch (handle)
            {
                case HandleDirection.NegativeY:
                case HandleDirection.PositiveY:
                    changedAxis = k_DirectionY;
                    break;
                case HandleDirection.NegativeZ:
                case HandleDirection.PositiveZ:
                    changedAxis = k_DirectionZ;
                    break;
            }

            Vector3 upperBound = newBounds.max;
            Vector3 lowerBound = newBounds.min;

            // ensure height cannot be made less than diameter
            if (changedAxis == m_HeightAxis)
            {
                int radiusAxis;
                GetRadiusAxis(out radiusAxis);
                float diameter = upperBound[radiusAxis] - lowerBound[radiusAxis];
                float newHeight = upperBound[m_HeightAxis] - lowerBound[m_HeightAxis];
                if (newHeight < diameter)
                {
                    if (handle == HandleDirection.PositiveX || handle == HandleDirection.PositiveY || handle == HandleDirection.PositiveZ)
                        upperBound[m_HeightAxis] = lowerBound[m_HeightAxis] + diameter;
                    else
                        lowerBound[m_HeightAxis] = upperBound[m_HeightAxis] - diameter;
                }
            }
            // ensure radius changes uniformly and enlarges the height if necessary
            else
            {
                // try to return height to its value at the time handle was clicked
                upperBound[m_HeightAxis] = boundsOnClick.center[m_HeightAxis] + 0.5f * boundsOnClick.size[m_HeightAxis];
                lowerBound[m_HeightAxis] = boundsOnClick.center[m_HeightAxis] - 0.5f * boundsOnClick.size[m_HeightAxis];

                float rad = 0.5f * (upperBound[changedAxis] - lowerBound[changedAxis]);
                float halfCurrentHeight = 0.5f * (upperBound[m_HeightAxis] - lowerBound[m_HeightAxis]);
                for (int axis = 0; axis < 3; ++axis)
                {
                    if (axis == changedAxis)
                        continue;
                    float amt = axis == m_HeightAxis ? Mathf.Max(halfCurrentHeight, rad) : rad;
                    lowerBound[axis] = center[axis] - amt;
                    upperBound[axis] = center[axis] + amt;
                }
            }
            return new Bounds((upperBound + lowerBound) * 0.5f, upperBound - lowerBound);
        }

        // returns true only if both radius axes are enabled
        private bool GetRadiusAxis(out int radiusAxis)
        {
            radiusAxis = s_NextAxis[m_HeightAxis];
            if (!IsAxisEnabled(radiusAxis))
            {
                radiusAxis = s_NextAxis[radiusAxis];
                return false;
            }
            return IsAxisEnabled(s_NextAxis[radiusAxis]);
        }
    }
}
