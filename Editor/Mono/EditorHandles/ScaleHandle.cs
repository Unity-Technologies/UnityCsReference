// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        public static Vector3 DoScaleHandle(Vector3 scale, Vector3 position, Quaternion rotation, float size)
        {
            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));
            color = isStatic ? Color.Lerp(xAxisColor, staticColor, staticBlend) : xAxisColor;
            scale.x = ScaleSlider(scale.x, position, rotation * Vector3.right, rotation, size, SnapSettings.scale);

            color = isStatic ? Color.Lerp(yAxisColor, staticColor, staticBlend) : yAxisColor;
            scale.y = ScaleSlider(scale.y, position, rotation * Vector3.up, rotation, size, SnapSettings.scale);

            color = isStatic ? Color.Lerp(zAxisColor, staticColor, staticBlend) : zAxisColor;
            scale.z = ScaleSlider(scale.z, position, rotation * Vector3.forward, rotation, size, SnapSettings.scale);

            color = centerColor;
            EditorGUI.BeginChangeCheck();
            float s = ScaleValueHandle(scale.x, position, rotation, size, CubeHandleCap, SnapSettings.scale);
            if (EditorGUI.EndChangeCheck())
            {
                float dif = s / scale.x;
                scale.x = s;
                scale.y *= dif;
                scale.z *= dif;
            }
            return scale;
        }
    }
}
