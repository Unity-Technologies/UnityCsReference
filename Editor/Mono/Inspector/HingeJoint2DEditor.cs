// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(HingeJoint2D))]
    [CanEditMultipleObjects]
    internal class HingeJoint2DEditor : AnchoredJoint2DEditor
    {
        new public void OnSceneGUI()
        {
            var hingeJoint2D = (HingeJoint2D)target;

            // Ignore disabled joint.
            if (!hingeJoint2D.enabled)
                return;

            if (hingeJoint2D.useLimits)
            {
                var center = TransformPoint(hingeJoint2D.transform, hingeJoint2D.anchor);

                var limitMin = Mathf.Min(hingeJoint2D.limits.min, hingeJoint2D.limits.max);
                var limitMax = Mathf.Max(hingeJoint2D.limits.min, hingeJoint2D.limits.max);

                var arcAngle = limitMax - limitMin;
                var arcRadius = HandleUtility.GetHandleSize(center) * 0.8f;

                var hingeBodyAngle = hingeJoint2D.GetComponent<Rigidbody2D>().rotation;
                Vector3 fromDirection = RotateVector2(Vector3.right, -limitMax - hingeBodyAngle);
                var referencePosition = center + (Vector3)(RotateVector2(Vector3.right, -hingeJoint2D.jointAngle - hingeBodyAngle) * arcRadius);

                // "reference" line
                Handles.color = new Color(0, 1, 0, 0.7f);
                DrawAALine(center, referencePosition);

                // arc background
                Handles.color = new Color(0, 1, 0, 0.03f);
                Handles.DrawSolidArc(center, Vector3.back, fromDirection, arcAngle, arcRadius);

                // arc frame
                Handles.color = new Color(0, 1, 0, 0.7f);
                Handles.DrawWireArc(center, Vector3.back, fromDirection, arcAngle, arcRadius);

                DrawTick(center, arcRadius, 0, fromDirection, 1);
                DrawTick(center, arcRadius, arcAngle, fromDirection, 1);
            }

            base.OnSceneGUI();
        }

        void DrawTick(Vector3 center, float radius, float angle, Vector3 up, float length)
        {
            Vector3 direction = RotateVector2(up, angle).normalized;
            Vector3 start = center + direction * radius;
            Vector3 end = start + (center - start).normalized * radius * length;
            Handles.DrawAAPolyLine(new[] { new Color(0, 1, 0, 0.7f), new Color(0, 1, 0, 0) }, new[] { start, end });
        }
    }
}
