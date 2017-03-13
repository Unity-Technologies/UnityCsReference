// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(RelativeJoint2D))]
    [CanEditMultipleObjects]
    internal class RelativeJoint2DEditor : Joint2DEditor
    {
        public void OnSceneGUI()
        {
            var relativeJoint2D = (RelativeJoint2D)target;

            // Ignore disabled joint.
            if (!relativeJoint2D.enabled)
                return;

            // Fetch the anchors.
            var anchor = (Vector3)relativeJoint2D.target;
            var connectedAnchor = relativeJoint2D.connectedBody ? relativeJoint2D.connectedBody.transform.position : Vector3.zero;

            // Draw a line between the bodies.
            Handles.color = Color.green;
            DrawAALine(anchor, connectedAnchor);

            // Draw the source point.
            var sourceScale = HandleUtility.GetHandleSize(connectedAnchor) * 0.16f;
            var horzSource = Vector3.left * sourceScale;
            var vertSource = Vector3.up * sourceScale;
            DrawAALine(connectedAnchor - horzSource, connectedAnchor + horzSource);
            DrawAALine(connectedAnchor - vertSource, connectedAnchor + vertSource);

            // Draw the target point.
            var targetScale = HandleUtility.GetHandleSize(anchor) * 0.16f;
            var horzTarget = Vector3.left * targetScale;
            var vertTarget = Vector3.up * targetScale;
            DrawAALine(anchor - horzTarget, anchor + horzTarget);
            DrawAALine(anchor - vertTarget, anchor + vertTarget);
        }
    }
}
