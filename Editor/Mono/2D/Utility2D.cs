// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class Utility2D
    {
        public static Vector3 ScreenToLocal(Transform transform, Vector2 screenPosition)
        {
            Plane plane = new Plane(transform.forward * -1f, transform.position);

            Ray ray;
            if (Camera.current.orthographic)
            {
                Vector2 screen = GUIClip.Unclip(screenPosition);
                screen.y = Screen.height - screen.y;
                Vector3 cameraWorldPoint = Camera.current.ScreenToWorldPoint(screen);
                ray = new Ray(cameraWorldPoint, Camera.current.transform.forward);
            }
            else
            {
                ray = HandleUtility.GUIPointToWorldRay(screenPosition);
            }

            float result;
            plane.Raycast(ray, out result);
            Vector3 world = ray.GetPoint(result);
            return transform.InverseTransformPoint(world);
        }
    }
}
