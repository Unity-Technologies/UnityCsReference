// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class SphereBoundsHandle : PrimitiveBoundsHandle
    {
        [Obsolete("Use parameterless constructor instead.")]
        public SphereBoundsHandle(int controlIDHint) : base(controlIDHint) {}

        public SphereBoundsHandle() : base() {}

        public float radius
        {
            get
            {
                Vector3 size = GetSize();
                float diameter = 0f;
                for (int axis = 0; axis < 3; ++axis)
                {
                    // only consider size values on enabled axes
                    if (IsAxisEnabled(axis))
                        diameter = Mathf.Max(diameter, Mathf.Abs(size[axis]));
                }
                return diameter * 0.5f;
            }
            set { SetSize(2f * value * Vector3.one); }
        }

        protected override void DrawWireframe()
        {
            bool x = IsAxisEnabled(Axes.X);
            bool y = IsAxisEnabled(Axes.Y);
            bool z = IsAxisEnabled(Axes.Z);
            if (x && y)
                Handles.DrawWireArc(center, Vector3.forward, Vector3.up, 360f, radius);
            if (x && z)
                Handles.DrawWireArc(center, Vector3.up, Vector3.right, 360f, radius);
            if (y && z)
                Handles.DrawWireArc(center, Vector3.right, Vector3.forward, 360f, radius);
            if (x && !y && !z)
                Handles.DrawLine(Vector3.right * radius, Vector3.left * radius);
            if (!x && y && !z)
                Handles.DrawLine(Vector3.up * radius, Vector3.down * radius);
            if (!x && !y && z)
                Handles.DrawLine(Vector3.forward * radius, Vector3.back * radius);
        }

        protected override Bounds OnHandleChanged(HandleDirection handle, Bounds boundsOnClick, Bounds newBounds)
        {
            Vector3 upperBound = newBounds.max;
            Vector3 lowerBound = newBounds.min;
            // ensure radius changes uniformly
            int changedAxis = 0;
            switch (handle)
            {
                case HandleDirection.NegativeY:
                case HandleDirection.PositiveY:
                    changedAxis = 1;
                    break;
                case HandleDirection.NegativeZ:
                case HandleDirection.PositiveZ:
                    changedAxis = 2;
                    break;
            }
            float rad = 0.5f * (upperBound[changedAxis] - lowerBound[changedAxis]);
            for (int axis = 0; axis < 3; ++axis)
            {
                if (axis == changedAxis)
                    continue;
                lowerBound[axis] = center[axis] - rad;
                upperBound[axis] = center[axis] + rad;
            }
            return new Bounds((upperBound + lowerBound) * 0.5f, upperBound - lowerBound);
        }
    }
}
