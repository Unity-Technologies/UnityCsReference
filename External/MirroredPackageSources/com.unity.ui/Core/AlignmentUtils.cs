namespace UnityEngine.UIElements
{
    class AlignmentUtils
    {
        // round(-0.52 to 0.48) returns 0 => round(0.5 +/- eps) returns 1
        internal static float RoundToPixelGrid(float v, float pixelsPerPoint, float offset = 0.02f)
        {
            return Mathf.Floor(v * pixelsPerPoint + 0.5f + offset) / pixelsPerPoint;
        }

        // ceil(-0.02 to 0.98) returns 0 => ceil(0 +/- eps) returns 0
        internal static float CeilToPixelGrid(float v, float pixelsPerPoint, float offset = -0.02f)
        {
            return Mathf.Ceil(v * pixelsPerPoint + offset) / pixelsPerPoint;
        }

        // floor(-0.02 to 0.98) returns 0 => floor(0 +/- eps) returns 0
        internal static float FloorToPixelGrid(float v, float pixelsPerPoint, float offset = 0.02f)
        {
            return Mathf.Floor(v * pixelsPerPoint + offset) / pixelsPerPoint;
        }
    }
}
