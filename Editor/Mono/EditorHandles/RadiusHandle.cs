// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal static float DoRadiusHandle(Quaternion rotation, Vector3 position, float radius, bool handlesOnly)
        {
            Vector3 planeNormal;
            float frontAngle = 90;
            Vector3[] dirs = new[] {
                rotation * Vector3.right,
                      rotation * Vector3.up,
                      rotation * Vector3.forward,
                      rotation * -Vector3.right,
                      rotation * -Vector3.up,
                      rotation * -Vector3.forward
            };

            if (Camera.current.orthographic)
            {
                planeNormal = Camera.current.transform.forward;

                if (!handlesOnly)
                {
                    // Draw periphery circle
                    DrawWireDisc(position, planeNormal, radius);

                    // Draw two-shaded axis-aligned circles
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 from = Vector3.Cross(dirs[i], planeNormal).normalized;
                        DrawTwoShadedWireDisc(position, dirs[i], from, 180, radius);
                    }
                }
            }
            else
            {
                // Since the geometry is transfromed by Handles.matrix during rendering, we transform the camera position
                // by the inverse matrix so that the two-shaded wireframe will have the proper orientation.
                Matrix4x4 invMatrix = Matrix4x4.Inverse(Handles.matrix);

                planeNormal = position - invMatrix.MultiplyPoint(Camera.current.transform.position); // vector from camera to center
                float sqrDist = planeNormal.sqrMagnitude; // squared distance from camera to center
                float sqrRadius = radius * radius; // squared radius
                float sqrOffset = sqrRadius * sqrRadius / sqrDist; // squared distance from actual center to drawn disc center
                float insideAmount = sqrOffset / sqrRadius;

                // If we are not inside the sphere, calculate where to draw the periphery
                if (insideAmount < 1)
                {
                    float drawnRadius = Mathf.Sqrt(sqrRadius - sqrOffset); // the radius of the drawn disc
                    frontAngle = Mathf.Atan2(drawnRadius, Mathf.Sqrt(sqrOffset)) * Mathf.Rad2Deg;

                    // Draw periphery circle
                    if (!handlesOnly)
                        DrawWireDisc(position - sqrRadius * planeNormal / sqrDist, planeNormal, drawnRadius);
                }
                else
                {
                    frontAngle = -1000;
                }

                if (!handlesOnly)
                {
                    // Draw two-shaded axis-aligned circles
                    for (int i = 0; i < 3; i++)
                    {
                        if (insideAmount < 1)
                        {
                            float Q = Vector3.Angle(planeNormal, dirs[i]);
                            Q = 90 - Mathf.Min(Q, 180 - Q);
                            float f = Mathf.Tan(Q * Mathf.Deg2Rad);
                            float g = Mathf.Sqrt(sqrOffset + f * f * sqrOffset) / radius;
                            if (g < 1)
                            {
                                float e = Mathf.Asin(g) * Mathf.Rad2Deg;
                                Vector3 from = Vector3.Cross(dirs[i], planeNormal).normalized;
                                from = Quaternion.AngleAxis(e, dirs[i]) * from;
                                DrawTwoShadedWireDisc(position, dirs[i], from, (90 - e) * 2, radius);
                            }
                            else
                            {
                                DrawTwoShadedWireDisc(position, dirs[i], radius);
                            }
                        }
                        else
                        {
                            DrawTwoShadedWireDisc(position, dirs[i], radius);
                        }
                    }
                }
            }

            Color origCol = color;

            // Draw handles
            for (int i = 0; i < 6; i++)
            {
                // Get the id no matter if we're drawing the slider or not,
                // in order to not corrupt the id order when slider handles pop in and out.
                int id = GUIUtility.GetControlID(s_RadiusHandleHash, FocusType.Passive);

                // The angle between the axis and the center-camera vector
                float angle = Vector3.Angle(dirs[i], -planeNormal);

                // Don't make the slider if the axis is pointing almost towards or away from the camera,
                // but always draw it if the user is already dragging it.
                if ((angle > 5 && angle < 175) || GUIUtility.hotControl == id)
                {
                    // Give handles twice the alpha of the lines.
                    // If handle is not on front side of sphere, make it dimmer.
                    Color col = origCol;
                    if (angle > frontAngle + 5f)
                        col.a = Mathf.Clamp01(backfaceAlphaMultiplier * origCol.a * 2);
                    else
                        col.a = Mathf.Clamp01(origCol.a * 2);
                    color = col;

                    Vector3 pos = position + radius * dirs[i];
                    bool temp = GUI.changed;
                    GUI.changed = false;
                    pos = Slider1D.Do(id, pos, dirs[i], HandleUtility.GetHandleSize(pos) * 0.03f, DotHandleCap, 0);


                    if (GUI.changed)
                        radius = Vector3.Distance(pos, position);
                    GUI.changed |= temp;
                }
            }

            color = origCol;

            return radius;
        }
    }
}
