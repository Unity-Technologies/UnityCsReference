namespace UnityEngine.UIElements
{
    internal static class UIRUtility
    {
        public static readonly string k_DefaultShaderName = UIR.Shaders.k_Runtime;
        public static readonly string k_DefaultWorldSpaceShaderName = UIR.Shaders.k_RuntimeWorld;

        public const float k_ClearZ = 0.99f; // At the far plane like standard Unity rendering
        public const float k_MeshPosZ = 0.0f; // The correct z value to use to draw a shape
        public const float k_MaskPosZ = 1.0f; // The correct z value to use to mark a clipping shape

        public static Vector4 ToVector4(Rect rc)
        {
            return new Vector4(rc.xMin, rc.yMin, rc.xMax, rc.yMax);
        }

        public static bool IsRoundRect(VisualElement ve)
        {
            var style = ve.resolvedStyle;
            return !(style.borderTopLeftRadius < Mathf.Epsilon &&
                style.borderTopRightRadius < Mathf.Epsilon &&
                style.borderBottomLeftRadius < Mathf.Epsilon &&
                style.borderBottomRightRadius < Mathf.Epsilon);
        }

        public static bool IsVectorImageBackground(VisualElement ve)
        {
            return ve.computedStyle.backgroundImage.value.vectorImage != null;
        }

        public static void Destroy(Object obj)
        {
            if (obj == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(obj);
            else
                Object.DestroyImmediate(obj);
        }
    }
}
