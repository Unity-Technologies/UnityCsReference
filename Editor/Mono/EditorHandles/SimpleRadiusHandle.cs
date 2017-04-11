// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal static float DoSimpleEdgeHandle(Quaternion rotation, Vector3 position, float radius)
        {
            Vector3 right = rotation * Vector3.right;

            // Radius handles at ends
            EditorGUI.BeginChangeCheck();
            radius = SizeSlider(position, right, radius);
            radius = SizeSlider(position, -right, radius);
            if (EditorGUI.EndChangeCheck())
                radius = Mathf.Max(0.0f, radius);

            // Draw gizmo
            if (radius > 0)
                DrawLine(position - right * radius, position + right * radius);

            return radius;
        }

        internal static float DoSimpleRadiusHandle(Quaternion rotation, Vector3 position, float radius, bool hemisphere)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            // Radius handle in zenith
            bool temp = GUI.changed;
            GUI.changed = false;
            radius = SizeSlider(position, forward, radius);
            if (!hemisphere)
                radius = SizeSlider(position, -forward, radius);
            if (GUI.changed)
                radius = Mathf.Max(0.0f, radius);
            GUI.changed |= temp;

            // Radius handles at disc
            temp = GUI.changed;
            GUI.changed = false;
            radius = SizeSlider(position, up, radius);
            radius = SizeSlider(position, -up, radius);
            radius = SizeSlider(position, right, radius);
            radius = SizeSlider(position, -right, radius);
            if (GUI.changed)
                radius = Mathf.Max(0.0f, radius);
            GUI.changed |= temp;

            // Draw gizmo
            if (radius > 0)
            {
                DrawWireDisc(position, forward, radius);
                DrawWireArc(position, up, -right, hemisphere ? 180 : 360, radius);
                DrawWireArc(position, right, up, hemisphere ? 180 : 360, radius);
                //DrawPeriphery (position, radius);
            }
            return radius;
        }

        //  static void DrawHemispherePeriphery (Vector3 position, float radius)
        //  {
        //      Vector3 planeNormal = position - Camera.current.transform.position; // vector from camera to center
        //      float sqrDist = planeNormal.sqrMagnitude; // squared distance from camera to center
        //      float sqrRadius = radius * radius; // squared radius
        //      float sqrOffset = sqrRadius * sqrRadius / sqrDist; // squared distance from actual center to drawn disc center
        //      float insideAmount = sqrOffset / sqrRadius;
        //
        //      // If we are not inside the sphere, calculate where to draw the periphery
        //      if (insideAmount < 1)
        //      {
        //          float drawnRadius = Mathf.Sqrt(sqrRadius - sqrOffset); // the radius of the drawn disc
        //
        //          // Draw periphery circle
        //          Vector3 tangent = Vector3.Cross(planeNormal, Vector3.up);
        //          if (tangent.sqrMagnitude < .001f)
        //              tangent = Vector3.Cross(planeNormal, Vector3.right);
        //          DrawWireArc(position - sqrRadius * planeNormal / sqrDist, planeNormal, tangent, -180, drawnRadius);
        //      }
        //  }
    }
}
