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
            // We must call ALL of the GetControlIDs call here to get consistent IDs
            // If one call is skipped from time to time, IDs will not be consistent
            var xId = GUIUtility.GetControlID(s_xAxisScaleHandleHash, FocusType.Passive);
            var yId = GUIUtility.GetControlID(s_yAxisScaleHandleHash, FocusType.Passive);
            var zId = GUIUtility.GetControlID(s_zAxisScaleHandleHash, FocusType.Passive);

            // Calculate the camera view vector in Handle draw space
            // this handle the case where the matrix is skewed
            var handlePosition = matrix.MultiplyPoint3x4(position);
            var drawToWorldMatrix = matrix * Matrix4x4.TRS(position, rotation, Vector3.one);
            var invDrawToWorldMatrix = drawToWorldMatrix.inverse;
            var viewVectorDrawSpace = GetCameraViewFrom(handlePosition, invDrawToWorldMatrix);

            var xCameraViewLerp = xId == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, Vector3.right);
            var yCameraViewLerp = yId == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, Vector3.up);
            var zCameraViewLerp = zId == GUIUtility.hotControl ? 0 : GetCameraViewLerpForWorldAxis(viewVectorDrawSpace, Vector3.forward);

            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));
            color = isStatic ? Color.Lerp(xAxisColor, staticColor, staticBlend) : xAxisColor;
            if (xCameraViewLerp <= kCameraViewThreshold)
            {
                color = Color.Lerp(color, Color.clear, xCameraViewLerp);
                scale.x = UnityEditorInternal.SliderScale.DoAxis(xId, scale.x, position, rotation * Vector3.right, rotation, size, SnapSettings.scale);
            }

            color = isStatic ? Color.Lerp(yAxisColor, staticColor, staticBlend) : yAxisColor;
            if (yCameraViewLerp <= kCameraViewThreshold)
            {
                color = Color.Lerp(color, Color.clear, yCameraViewLerp);
                scale.y = UnityEditorInternal.SliderScale.DoAxis(yId, scale.y, position, rotation * Vector3.up, rotation, size, SnapSettings.scale);
            }

            color = isStatic ? Color.Lerp(zAxisColor, staticColor, staticBlend) : zAxisColor;

            if (zCameraViewLerp <= kCameraViewThreshold)
            {
                color = Color.Lerp(color, Color.clear, zCameraViewLerp);
                scale.z = UnityEditorInternal.SliderScale.DoAxis(zId, scale.z, position, rotation * Vector3.forward, rotation, size, SnapSettings.scale);
            }

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
