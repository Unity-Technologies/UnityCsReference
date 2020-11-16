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

        public static void Multiply2D(this Quaternion rotation, ref Vector2 point)
        {
            // Even though Quaternion coordinates aren't the same as Euler angles, it so happens that a rotation only
            // in the z axis will also have only a z (and w) value that is non-zero. Cool, heh!
            // Here we'll assume rotation.x = rotation.y = 0.
            float z = rotation.z * 2f;
            float zz = 1f - rotation.z * z;
            float wz = rotation.w * z;
            point = new Vector2(zz * point.x - wz * point.y, wz * point.x + zz * point.y);
        }

        public static bool IsVectorImageBackground(VisualElement ve)
        {
            return ve.computedStyle.backgroundImage.vectorImage != null;
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

        public static int GetPrevPow2(int n)
        {
            int bits = 0;
            while (n > 1)
            {
                n >>= 1;
                ++bits;
            }

            return 1 << bits;
        }

        public static int GetNextPow2(int n)
        {
            int test = 1;
            while (test < n)
                test <<= 1;
            return test;
        }

        public static int GetNextPow2Exp(int n)
        {
            int test = 1;
            int exp = 0;
            while (test < n)
            {
                test <<= 1;
                ++exp;
            }

            return exp;
        }
    }
}
