// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        internal static Vector2 DoRectHandles(Quaternion rotation, Vector3 position, Vector2 size, bool handlesOnly)
        {
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            float halfWidth = 0.5f * size.x;
            float halfHeight = 0.5f * size.y;

            if (!handlesOnly)
            {
                Vector3 topRight = position + up * halfHeight + right * halfWidth;
                Vector3 bottomRight = position - up * halfHeight + right * halfWidth;
                Vector3 bottomLeft = position - up * halfHeight - right * halfWidth;
                Vector3 topLeft = position + up * halfHeight - right * halfWidth;

                // Draw rectangle
                DrawLine(topRight, bottomRight);
                DrawLine(bottomRight, bottomLeft);
                DrawLine(bottomLeft, topLeft);
                DrawLine(topLeft, topRight);
            }

            // Give handles twice the alpha of the lines
            Color origCol = color;
            Color col = color;
            col.a = Mathf.Clamp01(color.a * 2);
            color = ToActiveColorSpace(col);

            // Draw handles
            halfHeight = SizeSlider(position, up, halfHeight);
            halfHeight = SizeSlider(position, -up, halfHeight);
            halfWidth = SizeSlider(position, right, halfWidth);
            halfWidth = SizeSlider(position, -right, halfWidth);

            size.x = Mathf.Max(0f, 2.0f * halfWidth);
            size.y = Mathf.Max(0f, 2.0f * halfHeight);

            color = origCol;

            return size;
        }
    }
}
