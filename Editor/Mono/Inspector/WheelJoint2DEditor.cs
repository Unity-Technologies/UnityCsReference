// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(WheelJoint2D))]
    [CanEditMultipleObjects]
    internal class WheelJoint2DEditor : AnchoredJoint2DEditor
    {
        new public void OnSceneGUI()
        {
            var wheelJoint2D = (WheelJoint2D)target;

            // Ignore disabled joint.
            if (!wheelJoint2D.enabled)
                return;

            var anchor = TransformPoint(wheelJoint2D.transform, wheelJoint2D.anchor);

            // Draw lines for slider angle and limits
            Vector3 upper = anchor;
            Vector3 lower = anchor;
            Vector3 direction = RotateVector2(Vector3.right, -wheelJoint2D.suspension.angle - wheelJoint2D.transform.eulerAngles.z);

            Handles.color = Color.green;

            direction *= HandleUtility.GetHandleSize(anchor) * 0.3f;
            upper += direction;
            lower -= direction;

            DrawAALine(upper, lower);

            base.OnSceneGUI();
        }
    }
}
