// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AnchoredJoint2D), true)]
    [CanEditMultipleObjects]
    internal class AnchoredJoint2DEditor : Joint2DEditor
    {
        const float k_SnapDistance = 0.13f;
        AnchoredJoint2D anchorJoint2D;

        public void OnSceneGUI()
        {
            anchorJoint2D = (AnchoredJoint2D)target;

            // Ignore disabled joint.
            if (!anchorJoint2D.enabled)
                return;

            Vector3 worldAnchor = TransformPoint(anchorJoint2D.transform, anchorJoint2D.anchor);
            Vector3 worldConnectedAnchor = anchorJoint2D.connectedAnchor;
            if (anchorJoint2D.connectedBody)
                worldConnectedAnchor = TransformPoint(anchorJoint2D.connectedBody.transform, worldConnectedAnchor);

            // Draw line between anchors
            Vector3 startPoint = worldAnchor + (worldConnectedAnchor - worldAnchor).normalized * HandleUtility.GetHandleSize(worldAnchor) * 0.1f;
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(new Vector3[] { startPoint, worldConnectedAnchor });

            // Connected anchor
            if (HandleAnchor(ref worldConnectedAnchor, true))
            {
                worldConnectedAnchor = SnapToSprites(worldConnectedAnchor);
                worldConnectedAnchor = SnapToPoint(worldConnectedAnchor, worldAnchor, k_SnapDistance);

                if (anchorJoint2D.connectedBody)
                    worldConnectedAnchor = InverseTransformPoint(anchorJoint2D.connectedBody.transform, worldConnectedAnchor);

                Undo.RecordObject(anchorJoint2D, "Move Connected Anchor");
                anchorJoint2D.connectedAnchor = worldConnectedAnchor;
            }

            // Anchor
            if (HandleAnchor(ref worldAnchor, false))
            {
                worldAnchor = SnapToSprites(worldAnchor);
                worldAnchor = SnapToPoint(worldAnchor, worldConnectedAnchor, k_SnapDistance);

                Undo.RecordObject(anchorJoint2D, "Move Anchor");
                anchorJoint2D.anchor = InverseTransformPoint(anchorJoint2D.transform, worldAnchor);
            }
        }

        Vector3 SnapToSprites(Vector3 position)
        {
            SpriteRenderer spriteRenderer = anchorJoint2D.GetComponent<SpriteRenderer>();
            position = SnapToSprite(spriteRenderer, position, k_SnapDistance);

            if (anchorJoint2D.connectedBody)
            {
                spriteRenderer = anchorJoint2D.connectedBody.GetComponent<SpriteRenderer>();
                position = SnapToSprite(spriteRenderer, position, k_SnapDistance);
            }

            return position;
        }
    }
}
