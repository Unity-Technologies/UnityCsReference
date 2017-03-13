// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal static Vector2 DoRectHandles(Quaternion rotation, Vector3 position, Vector2 size)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            float halfWidth = 0.5f * size.x;
            float halfHeight = 0.5f * size.y;

            Vector3 topRight =      position + up * halfHeight + right * halfWidth;
            Vector3 bottomRight =   position - up * halfHeight + right * halfWidth;
            Vector3 bottomLeft =    position - up * halfHeight - right * halfWidth;
            Vector3 topLeft =       position + up * halfHeight - right * halfWidth;

            // Draw rectangle
            DrawLine(topRight, bottomRight);
            DrawLine(bottomRight, bottomLeft);
            DrawLine(bottomLeft, topLeft);
            DrawLine(topLeft, topRight);

            // Give handles twice the alpha of the lines
            Color col = Handles.color;
            col.a = Mathf.Clamp01(col.a * 2);
            Handles.color = col;

            // Draw handles
            halfHeight = SizeSlider(position, up, halfHeight);
            halfHeight = SizeSlider(position, -up, halfHeight);
            halfWidth = SizeSlider(position, right, halfWidth);
            halfWidth = SizeSlider(position, -right, halfWidth);

            // Draw the area light's normal only if it will not overlap with the current tool
            if (!((Tools.current == Tool.Move || Tools.current == Tool.Scale) && Tools.pivotRotation == PivotRotation.Local))
                DrawLine(position, position + forward);

            size.x = 2.0f * halfWidth;
            size.y = 2.0f * halfHeight;

            return size;
        }
    }
}
