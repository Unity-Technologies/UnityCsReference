// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.Burst.Editor
{
    internal static class BurstMath
    {
        private const float HitBoxAdjust = 2f;

        /// <summary>
        /// Rotates <see cref="point"/> around a pivot point according to angle given in degrees.
        /// </summary>
        /// <param name="angle">Angle in degrees.</param>
        /// <param name="point">Point to rotate.</param>
        /// <param name="pivotPoint">Pivot point to rotate around.</param>
        /// <returns>The rotated point.</returns>
        internal static Vector2 AnglePoint(float angle, Vector2 point, Vector2 pivotPoint)
        {
            // https://matthew-brett.github.io/teaching/rotation_2d.html
            // Problem Angle is calculates as angle clockwise, and here we use it as it was counterclockwise!
            var s = Mathf.Sin(angle);
            var c = Mathf.Cos(angle);
            point -= pivotPoint;

            return new Vector2(c * point.x - s * point.y, s * point.x + c * point.y) + pivotPoint;
        }

        /// <summary>
        /// Calculated angle in degrees between two points.
        /// </summary>
        /// <param name="start">Starting point.</param>
        /// <param name="end">End point.</param>
        /// <returns>Angle in degrees.</returns>
        internal static float CalculateAngle(Vector2 start, Vector2 end)
        {
            var distance = end - start;
            var angle = Mathf.Rad2Deg * Mathf.Atan(distance.y / distance.x);
            if (distance.x < 0)
            {
                angle += 180;
            }

            return angle;
        }

        /// <summary>
        /// Checks if <see cref="point"/> is within <see cref="rect"/> enlarged by <see cref="HitBoxAdjust"/>.
        /// </summary>
        /// <param name="rect">Give rect to enlarge and check with.</param>
        /// <param name="point">Given point to match in enlarged <see cref="rect"/>.</param>
        /// <returns>Whether <see cref="point"/> is within enlarged <see cref="rect"/>.</returns>
        internal static bool AdjustedContains(Rect rect, Vector2 point)
        {
            return rect.yMax + HitBoxAdjust >= point.y && rect.yMin - HitBoxAdjust <= point.y
                                                        && rect.xMax + HitBoxAdjust >= point.x && rect.xMin - HitBoxAdjust <= point.x;
        }

        /// <summary>
        /// Checks if <see cref="num"/> is within the closed interval defined by endPoint1 and endPoint2.
        /// </summary>
        /// <param name="endPoint1">One side of range.</param>
        /// <param name="endPoint2">Other side of range.</param>
        /// <param name="num">Number to check if it is in between <see cref="endPoint1"/> and <see cref="endPoint2"/>.</param>
        /// <returns>Whether <see cref="num"/> is within given range.</returns>
        internal static bool WithinRange(float endPoint1, float endPoint2, float num)
        {
            float start, end;
            if (endPoint1 < endPoint2)
            {
                start   = endPoint1;
                end     = endPoint2;
            }
            else
            {
                start   = endPoint2;
                end     = endPoint1;
            }
            return start <= num && num <= end;
        }

        /// <summary>
        /// Rounds down to nearest amount specified by <see cref="to"/>.
        /// </summary>
        /// <param name="number">Number to round down.</param>
        /// <param name="to">Specifies what amount to round down to.</param>
        /// <returns><see cref="number"/> rounded down to amount <see cref="to"/>.</returns>
        internal static float RoundDownToNearest(float number, float to)
        {
            //https://www.programmingnotes.org/7601/cs-how-to-round-a-number-to-the-nearest-x-using-cs/
            float inverse = 1 / to;
            float dividend = Mathf.Floor(number * inverse);
            return dividend / inverse;
        }
    }
}
