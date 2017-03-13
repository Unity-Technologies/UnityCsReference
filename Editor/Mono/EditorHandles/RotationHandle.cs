// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        public static Quaternion DoRotationHandle(Quaternion rotation, Vector3 position)
        {
            float size = HandleUtility.GetHandleSize(position);
            Color temp = color;

            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));
            color = isStatic ? Color.Lerp(xAxisColor, staticColor, staticBlend) : xAxisColor;
            rotation = Disc(rotation, position, rotation * Vector3.right, size, true, SnapSettings.rotation);

            color = isStatic ? Color.Lerp(yAxisColor, staticColor, staticBlend) : yAxisColor;
            rotation = Disc(rotation, position, rotation * Vector3.up, size, true, SnapSettings.rotation);

            color = isStatic ? Color.Lerp(zAxisColor, staticColor, staticBlend) : zAxisColor;
            rotation = Disc(rotation, position, rotation * Vector3.forward, size, true, SnapSettings.rotation);

            if (!isStatic)
            {
                color = centerColor; rotation = Disc(rotation, position, Camera.current.transform.forward, size * 1.1f, false, 0);
                rotation = FreeRotateHandle(rotation, position, size);
            }

            color = temp;
            return rotation;
        }
    }
}
