namespace UnityEngine.UIElements
{
    internal static class UIRUtility
    {
        public static readonly string k_DefaultShaderName = UIR.Shaders.k_Runtime;
        public static readonly string k_DefaultWorldSpaceShaderName = UIR.Shaders.k_RuntimeWorld;

        // We provide our own epsilon to avoid issues such as case 1335430. Some native plugin
        // disable float-denormalization, which can lead to the wrong Mathf.Epsilon being used.
        public const float k_Epsilon = 1.0E-30f;

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
            return !(style.borderTopLeftRadius < k_Epsilon &&
                style.borderTopRightRadius < k_Epsilon &&
                style.borderBottomLeftRadius < k_Epsilon &&
                style.borderBottomRightRadius < k_Epsilon);
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
