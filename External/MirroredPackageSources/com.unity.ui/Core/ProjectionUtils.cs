using UnityEngine;

namespace UnityEngine.UIElements
{
    static class ProjectionUtils
    {
        // Returns a matrix that maps (left, bottom, near) to (-1, -1, -1) and (right, top, far) to (1, 1, 1)
        // Warning: Do not confuse this with Matrix4x4.Ortho.
        public static Matrix4x4 Ortho(float left, float right, float bottom, float top, float near, float far)
        {
            var result = new Matrix4x4();

            float rightMinusLeft = right - left;
            float topMinusBottom = top - bottom;
            float farMinusNear = far - near;

            result.m00 = 2 / rightMinusLeft;
            result.m11 = 2 / topMinusBottom;
            result.m22 = 2 / farMinusNear;

            result.m03 = -(right + left) / rightMinusLeft;
            result.m13 = -(top + bottom) / topMinusBottom;
            result.m23 = -(far + near) / farMinusNear;

            result.m33 = 1;

            return result;
        }
    }
}
