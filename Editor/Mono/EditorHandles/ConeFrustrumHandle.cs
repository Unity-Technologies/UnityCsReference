// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        // Description
        // 'radius' is near-radius at start of cone frustrum (far-radius is derived from 'angle' and near-radius)
        // 'angle' is cone angle from near disc of conefrustrum (if near-radius is 0 then angle is the half angle of view frustrum)
        // 'range' is height of conefrustrum
        internal static Vector3 DoConeFrustrumHandle(Quaternion rotation, Vector3 position, Vector3 radiusAngleRange)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            float nearRadius = radiusAngleRange.x;
            float angle = radiusAngleRange.y;
            float range = radiusAngleRange.z;

            angle = Mathf.Max(0.0f, angle);

            // Range handle at the center of the cone (allow negative range)
            bool temp = GUI.changed;
            range = SizeSlider(position, forward, range);
            GUI.changed |= temp;

            // Near plane of cone frustrum
            temp = GUI.changed;
            GUI.changed = false;
            nearRadius = SizeSlider(position , up, nearRadius);
            nearRadius = SizeSlider(position , -up, nearRadius);
            nearRadius = SizeSlider(position , right, nearRadius);
            nearRadius = SizeSlider(position , -right, nearRadius);
            if (GUI.changed)
                nearRadius = Mathf.Max(0.0f, nearRadius);
            GUI.changed |= temp;

            // Far plane of cone frustrum
            temp = GUI.changed;
            GUI.changed = false;
            float farRadius = Mathf.Min(1000f, Mathf.Abs(range * Mathf.Tan(Mathf.Deg2Rad * angle)) + nearRadius);
            farRadius = SizeSlider(position + forward * range, up, farRadius);
            farRadius = SizeSlider(position + forward * range, -up, farRadius);
            farRadius = SizeSlider(position + forward * range, right, farRadius);
            farRadius = SizeSlider(position + forward * range, -right, farRadius);
            if (GUI.changed)
                angle = Mathf.Clamp((Mathf.Rad2Deg * Mathf.Atan((farRadius - nearRadius) / Mathf.Abs(range))), 0.0F, 90f);
            GUI.changed |= temp;

            // Draw
            if (nearRadius > 0)
                DrawWireDisc(position, forward, nearRadius);

            if (farRadius > 0)
                DrawWireDisc(position + range * forward, forward, farRadius);

            DrawLine(position + up * nearRadius, (position + forward * range) + up * farRadius);
            DrawLine(position - up * nearRadius, (position + forward * range) - up * farRadius);
            DrawLine(position + right * nearRadius, (position + forward * range) + right * farRadius);
            DrawLine(position - right * nearRadius, (position + forward * range) - right * farRadius);

            return new Vector3(nearRadius, angle, range);
        }
    }
}
