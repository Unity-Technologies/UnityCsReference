// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(DistanceJoint2D))]
    [CanEditMultipleObjects]
    internal class DistanceJoint2DEditor : AnchoredJoint2DEditor
    {
        new public void OnSceneGUI()
        {
            var distanceJoint2D = (DistanceJoint2D)target;

            // Ignore disabled joint.
            if (!distanceJoint2D.enabled)
                return;

            // Start and end points for distance gizmo
            Vector3 anchor = TransformPoint(distanceJoint2D.transform, distanceJoint2D.anchor);
            Vector3 connectedAnchor = distanceJoint2D.connectedAnchor;

            // If connectedBody present, convert the position to match that
            if (distanceJoint2D.connectedBody)
                connectedAnchor = TransformPoint(distanceJoint2D.connectedBody.transform, connectedAnchor);

            DrawDistanceGizmo(anchor, connectedAnchor, distanceJoint2D.distance);

            base.OnSceneGUI();
        }
    }
}
