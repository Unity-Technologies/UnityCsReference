// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(TargetJoint2D))]
    [CanEditMultipleObjects]
    internal class TargetJoint2DEditor : Joint2DEditor
    {
        public void OnSceneGUI()
        {
            var targetJoint2D = (TargetJoint2D)target;

            // Ignore disabled joint.
            if (!targetJoint2D.enabled)
                return;

            // Fetch the anchor/target.
            var jointAnchor = TransformPoint(targetJoint2D.transform, targetJoint2D.anchor);
            var jointTarget = (Vector3)targetJoint2D.target;

            // Draw a line between the bodies.
            Handles.color = Color.green;
            Handles.DrawDottedLine(jointAnchor, jointTarget, 5.0f);

            // Draw the anchor point.
            if (HandleAnchor(ref jointAnchor, false))
            {
                Undo.RecordObject(targetJoint2D, "Move Anchor");
                targetJoint2D.anchor = InverseTransformPoint(targetJoint2D.transform, jointAnchor);
            }

            // Draw the target point.
            var targetScale = HandleUtility.GetHandleSize(jointTarget) * 0.3f;
            var horzTarget = Vector3.left * targetScale;
            var vertTarget = Vector3.up * targetScale;
            DrawAALine(jointTarget - horzTarget, jointTarget + horzTarget);
            DrawAALine(jointTarget - vertTarget, jointTarget + vertTarget);
            if (HandleAnchor(ref jointTarget, true))
            {
                Undo.RecordObject(targetJoint2D, "Move Target");
                targetJoint2D.target = jointTarget;
            }
        }
    }
}
