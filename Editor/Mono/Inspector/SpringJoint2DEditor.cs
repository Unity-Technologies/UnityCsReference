// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(SpringJoint2D))]
    [CanEditMultipleObjects]
    internal class SpringJoint2DEditor : AnchoredJoint2DEditor
    {
        new public void OnSceneGUI()
        {
            var springJoint2D = (SpringJoint2D)target;

            // Ignore disabled joint.
            if (!springJoint2D.enabled)
                return;

            // Start and end points for distance gizmo
            Vector3 anchor = TransformPoint(springJoint2D.transform, springJoint2D.anchor);
            Vector3 connectedAnchor = springJoint2D.connectedAnchor;

            // If connectedBody present, convert the position to match that
            if (springJoint2D.connectedBody)
                connectedAnchor = TransformPoint(springJoint2D.connectedBody.transform, connectedAnchor);

            DrawDistanceGizmo(anchor, connectedAnchor, springJoint2D.distance);

            base.OnSceneGUI();
        }
    }
}
