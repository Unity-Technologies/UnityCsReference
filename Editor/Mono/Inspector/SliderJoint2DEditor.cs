// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(SliderJoint2D))]
    [CanEditMultipleObjects]
    internal class SliderJoint2DEditor : AnchoredJoint2DEditor
    {
        new public void OnSceneGUI()
        {
            var sliderJoint2D = (SliderJoint2D)target;

            // Ignore disabled joint.
            if (!sliderJoint2D.enabled)
                return;

            var anchor = TransformPoint(sliderJoint2D.transform, sliderJoint2D.anchor);

            // Draw lines for slider angle and limits
            Vector3 upper = anchor;
            Vector3 lower = anchor;
            Vector3 direction = RotateVector2(Vector3.right, -sliderJoint2D.angle - sliderJoint2D.transform.eulerAngles.z);

            Handles.color = Color.green;

            if (sliderJoint2D.useLimits)
            {
                upper = anchor + direction * sliderJoint2D.limits.max;
                lower = anchor + direction * sliderJoint2D.limits.min;

                Vector3 normal = Vector3.Cross(direction, Vector3.forward);
                float upperSize = HandleUtility.GetHandleSize(upper) * 0.16f;
                float lowerSize = HandleUtility.GetHandleSize(lower) * 0.16f;

                DrawAALine(upper + normal * upperSize, upper - normal * upperSize);
                DrawAALine(lower + normal * lowerSize, lower - normal * lowerSize);
            }
            else
            {
                direction *= HandleUtility.GetHandleSize(anchor) * 0.3f;
                upper += direction;
                lower -= direction;
            }

            DrawAALine(upper, lower);

            base.OnSceneGUI();
        }
    }
}
