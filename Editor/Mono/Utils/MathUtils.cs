// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    public class MathUtils
    {
        // We cannot round to more decimals than 15 according to docs for System.Math.Round.
        private const int kMaxDecimals = 15;

        internal static float ClampToFloat(double value)
        {
            if (double.IsPositiveInfinity(value))
                return float.PositiveInfinity;

            if (double.IsNegativeInfinity(value))
                return float.NegativeInfinity;

            if (value < float.MinValue)
                return float.MinValue;

            if (value > float.MaxValue)
                return float.MaxValue;

            return (float)value;
        }

        internal static int ClampToInt(long value)
        {
            if (value < int.MinValue)
                return int.MinValue;

            if (value > int.MaxValue)
                return int.MaxValue;

            return (int)value;
        }

        internal static float RoundToMultipleOf(float value, float roundingValue)
        {
            if (roundingValue == 0)
                return value;
            return Mathf.Round(value / roundingValue) * roundingValue;
        }

        internal static float GetClosestPowerOfTen(float positiveNumber)
        {
            if (positiveNumber <= 0)
                return 1;
            return Mathf.Pow(10, Mathf.RoundToInt(Mathf.Log10(positiveNumber)));
        }

        internal static int GetNumberOfDecimalsForMinimumDifference(float minDifference)
        {
            return Mathf.Clamp(-Mathf.FloorToInt(Mathf.Log10(Mathf.Abs(minDifference))), 0, kMaxDecimals);
        }

        internal static int GetNumberOfDecimalsForMinimumDifference(double minDifference)
        {
            return (int)System.Math.Max(0.0, -System.Math.Floor(System.Math.Log10(System.Math.Abs(minDifference))));
        }

        internal static float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
        {
            if (minDifference == 0)
                return DiscardLeastSignificantDecimal(valueToRound);
            return (float)System.Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), System.MidpointRounding.AwayFromZero);
        }

        internal static double RoundBasedOnMinimumDifference(double valueToRound, double minDifference)
        {
            if (minDifference == 0)
                return DiscardLeastSignificantDecimal(valueToRound);
            return System.Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), System.MidpointRounding.AwayFromZero);
        }

        internal static float DiscardLeastSignificantDecimal(float v)
        {
            int decimals = Mathf.Clamp((int)(5 - Mathf.Log10(Mathf.Abs(v))), 0, kMaxDecimals);
            return (float)System.Math.Round(v, decimals, System.MidpointRounding.AwayFromZero);
        }

        internal static double DiscardLeastSignificantDecimal(double v)
        {
            int decimals = System.Math.Max(0, (int)(5 - System.Math.Log10(System.Math.Abs(v))));
            try
            {
                return System.Math.Round(v, decimals);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                // This can happen for very small numbers.
                return 0;
            }
        }

        public static float GetQuatLength(Quaternion q)
        {
            return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        }

        public static Quaternion GetQuatConjugate(Quaternion q)
        {
            return new Quaternion(-q.x, -q.y, -q.z, q.w);
        }

        public static Matrix4x4 OrthogonalizeMatrix(Matrix4x4 m)
        {
            Matrix4x4 n = Matrix4x4.identity;

            Vector3 i = m.GetColumn(0);
            Vector3 j = m.GetColumn(1);
            Vector3 k = m.GetColumn(2);
            k = k.normalized;
            i = Vector3.Cross(j, k).normalized;
            j = Vector3.Cross(k, i).normalized;

            n.SetColumn(0, i);
            n.SetColumn(1, j);
            n.SetColumn(2, k);

            return n;
        }

        public static void QuaternionNormalize(ref Quaternion q)
        {
            float invMag = 1.0f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            q.x *= invMag;
            q.y *= invMag;
            q.z *= invMag;
            q.w *= invMag;
        }

        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q = new Quaternion();
            q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
            // normalize
            QuaternionNormalize(ref q);
            return q;
        }

        /// <summary>
        /// Logarithm of a unit quaternion. The result is not necessary a unit quaternion.
        /// </summary>
        public static Quaternion GetQuatLog(Quaternion q)
        {
            Quaternion res = q;
            res.w = 0;

            if (Mathf.Abs(q.w) < 1.0f)
            {
                float theta = Mathf.Acos(q.w);
                float sin_theta = Mathf.Sin(theta);

                if (Mathf.Abs(sin_theta) > 0.0001)
                {
                    float coef = theta / sin_theta;
                    res.x = q.x * coef;
                    res.y = q.y * coef;
                    res.z = q.z * coef;
                }
            }

            return res;
        }

        public static Quaternion GetQuatExp(Quaternion q)
        {
            Quaternion res = q;

            float fAngle = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z);
            float fSin = Mathf.Sin(fAngle);

            res.w = Mathf.Cos(fAngle);

            if (Mathf.Abs(fSin) > 0.0001)
            {
                float coef = fSin / fAngle;
                res.x = coef * q.x;
                res.y = coef * q.y;
                res.z = coef * q.z;
            }

            return res;
        }

        /// <summary>
        /// SQUAD Spherical Quadrangle interpolation [Shoe87]
        /// </summary>
        public static Quaternion GetQuatSquad(float t, Quaternion q0, Quaternion q1, Quaternion a0, Quaternion a1)
        {
            float slerpT = 2.0f * t * (1.0f - t);

            Quaternion slerpP = Slerp(q0, q1, t);
            Quaternion slerpQ = Slerp(a0, a1, t);
            Quaternion slerp = Slerp(slerpP, slerpQ, slerpT);

            // normalize quaternion
            float l = Mathf.Sqrt(slerp.x * slerp.x + slerp.y * slerp.y + slerp.z * slerp.z + slerp.w * slerp.w);
            slerp.x /= l;
            slerp.y /= l;
            slerp.z /= l;
            slerp.w /= l;

            return slerp;
        }

        public static Quaternion GetSquadIntermediate(Quaternion q0, Quaternion q1, Quaternion q2)
        {
            Quaternion q1Inv = GetQuatConjugate(q1);
            Quaternion p0 = GetQuatLog(q1Inv * q0);
            Quaternion p2 = GetQuatLog(q1Inv * q2);
            Quaternion sum = new Quaternion(-0.25f * (p0.x + p2.x), -0.25f * (p0.y + p2.y), -0.25f * (p0.z + p2.z), -0.25f * (p0.w + p2.w));

            return q1 * GetQuatExp(sum);
        }

        /// <summary>
        /// Smooths the input parameter t.
        /// If less than k1 ir greater than k2, it uses a sin.
        /// Between k1 and k2 it uses linear interp.
        /// </summary>
        public static float Ease(float t, float k1, float k2)
        {
            float f; float s;

            f = k1 * 2 / Mathf.PI + k2 - k1 + (1.0f - k2) * 2 / Mathf.PI;

            if (t < k1)
            {
                s = k1 * (2 / Mathf.PI) * (Mathf.Sin((t / k1) * Mathf.PI / 2 - Mathf.PI / 2) + 1);
            }
            else if (t < k2)
            {
                s = (2 * k1 / Mathf.PI + t - k1);
            }
            else
            {
                s = 2 * k1 / Mathf.PI + k2 - k1 + ((1 - k2) * (2 / Mathf.PI)) * Mathf.Sin(((t - k2) / (1.0f - k2)) * Mathf.PI / 2);
            }

            return (s / f);
        }

        /// <summary>
        /// We need this because Quaternion.Slerp always uses the shortest arc.
        /// </summary>
        public static Quaternion Slerp(Quaternion p, Quaternion q, float t)
        {
            Quaternion ret;

            float fCos = Quaternion.Dot(p, q);

            if ((1.0f + fCos) > 0.00001)
            {
                float fCoeff0, fCoeff1;

                if ((1.0f - fCos) > 0.00001)
                {
                    float omega = Mathf.Acos(fCos);
                    float invSin = 1.0f / Mathf.Sin(omega);
                    fCoeff0 = Mathf.Sin((1.0f - t) * omega) * invSin;
                    fCoeff1 = Mathf.Sin(t * omega) * invSin;
                }
                else
                {
                    fCoeff0 = 1.0f - t;
                    fCoeff1 = t;
                }

                ret.x = fCoeff0 * p.x + fCoeff1 * q.x;
                ret.y = fCoeff0 * p.y + fCoeff1 * q.y;
                ret.z = fCoeff0 * p.z + fCoeff1 * q.z;
                ret.w = fCoeff0 * p.w + fCoeff1 * q.w;
            }
            else
            {
                float fCoeff0 = Mathf.Sin((1.0f - t) * Mathf.PI * 0.5f);
                float fCoeff1 = Mathf.Sin(t * Mathf.PI * 0.5f);

                ret.x = fCoeff0 * p.x - fCoeff1 * p.y;
                ret.y = fCoeff0 * p.y + fCoeff1 * p.x;
                ret.z = fCoeff0 * p.z - fCoeff1 * p.w;
                ret.w = p.z;
            }

            return ret;
        }

        // intersect_RayTriangle(): intersect a ray with a 3D triangle
        //    Input:  a ray R, and 3 vector3 forming a triangle
        //    Output: *I = intersection point (when it exists)
        //    Return: null = no intersection
        //            RaycastHit = intersection

        //  -1 = triangle is degenerate (a segment or point)
        //             0 = disjoint (no intersect)
        //             1 = intersect in unique point I1
        //             2 = are in the same plane
        public static object IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, bool bidirectional)
        {
            Vector3 ab = v1 - v0;
            Vector3 ac = v2 - v0;

            // Compute triangle normal. Can be precalculated or cached if
            // intersecting multiple segments against the same triangle
            Vector3 n = Vector3.Cross(ab, ac);

            // Compute denominator d. If d <= 0, segment is parallel to or points
            // away from triangle, so exit early
            float d = Vector3.Dot(-ray.direction, n);
            if (d <= 0.0f) return null;

            // Compute intersection t value of pq with plane of triangle. A ray
            // intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
            // dividing by d until intersection has been found to pierce triangle
            Vector3 ap = ray.origin - v0;
            float t = Vector3.Dot(ap, n);
            if ((t < 0.0f) && (!bidirectional)) return null;
            //if (t > d) return null; // For segment; exclude this code line for a ray test

            // Compute barycentric coordinate components and test if within bounds
            Vector3 e = Vector3.Cross(-ray.direction, ap);
            float v = Vector3.Dot(ac, e);
            if (v < 0.0f || v > d) return null;

            float w = -Vector3.Dot(ab, e);
            if (w < 0.0f || v + w > d) return null;

            // Segment/ray intersects triangle. Perform delayed division and
            // compute the last barycentric coordinate component
            float ood = 1.0f / d;
            t *= ood;
            v *= ood;
            w *= ood;
            float u = 1.0f - v - w;

            RaycastHit hit = new RaycastHit();

            hit.point = ray.origin + t * ray.direction;
            hit.distance = t;
            hit.barycentricCoordinate = new Vector3(u, v, w);
            hit.normal = Vector3.Normalize(n);

            return hit;
        }

        // Returns closest point on segment
        // squaredDist = squared distance between the two closest points
        // s = offset along segment
        public static Vector3 ClosestPtSegmentRay(Vector3 p1, Vector3 q1, Ray ray, out float squaredDist, out float s, out Vector3 closestRay)
        {
            Vector3 p2 = ray.origin;
            Vector3 q2 = ray.GetPoint(10000.0f);

            Vector3 d1 = q1 - p1; // Direction vector of segment S1
            Vector3 d2 = q2 - p2; // Direction vector of segment S2
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1); // Squared length of segment S1, always nonnegative
            float e = Vector3.Dot(d2, d2); // Squared length of segment S2, always nonnegative
            float f = Vector3.Dot(d2, r);

            float t = 0.0f;

            // Check if either or both segments degenerate into points
            if (a <= Mathf.Epsilon && e <= Mathf.Epsilon)
            {
                // Both segments degenerate into points
                squaredDist = Vector3.Dot(p1 - p2, p1 - p2);
                s = 0.0f;
                closestRay = p2;
                return p1;
            }

            if (a <= Mathf.Epsilon)
            {
                // First segment degenerates into a point
                s = 0.0f;
                t = f / e; // s = 0 => t = (b*s + f) / e = f / e
                t = Mathf.Clamp(t, 0.0f, 1.0f);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= Mathf.Epsilon)
                {
                    // Second segment degenerates into a point
                    t = 0.0f;
                    s = Mathf.Clamp(-c / a, 0.0f, 1.0f); // t = 0 => s = (b*t - c) / a = -c / a
                }
                else
                {
                    // The general nondegenerate case starts here
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b; // Always nonnegative

                    // If segments not parallel, compute closest point on L1 to L2, and
                    // clamp to segment S1. Else pick arbitrary s (here 0)
                    if (denom != 0.0f)
                    {
                        s = Mathf.Clamp((b * f - c * e) / denom, 0.0f, 1.0f);
                    }
                    else s = 0.0f;

                    // Compute point on L2 closest to S1(s) using
                    // t = Dot((P1+D1*s)-P2,D2) / Dot(D2,D2) = (b*s + f) / e
                    t = (b * s + f) / e;

                    // If t in [0,1] done. Else clamp t, recompute s for the new value
                    // of t using s = Dot((P2+D2*t)-P1,D1) / Dot(D1,D1)= (t*b - c) / a
                    // and clamp s to [0, 1]
                    if (t < 0.0f)
                    {
                        t = 0.0f;
                        s = Mathf.Clamp(-c / a, 0.0f, 1.0f);
                    }
                    else if (t > 1.0f)
                    {
                        t = 1.0f;
                        s = Mathf.Clamp((b - c) / a, 0.0f, 1.0f);
                    }
                }
            }

            Vector3 c1 = p1 + d1 * s;
            Vector3 c2 = p2 + d2 * t;
            squaredDist = Vector3.Dot(c1 - c2, c1 - c2);
            closestRay = c2;
            return c1;
        }

        public static bool IntersectRaySphere(Ray ray, Vector3 sphereOrigin, float sphereRadius, ref float t, ref Vector3 q)
        {
            Vector3 m = ray.origin - sphereOrigin;
            float b = Vector3.Dot(m, ray.direction);
            float c = Vector3.Dot(m, m) - (sphereRadius * sphereRadius);
            // Exit if r�s origin outside s (c > 0)and r pointing away from s (b > 0)
            if ((c > 0.0f) && (b > 0.0f)) return false;
            float discr = (b * b) - c;

            // A negative discriminant corresponds to ray missing sphere
            if (discr < 0.0f) return false;

            // Ray now found to intersect sphere, compute smallest t value of intersection
            t = -b - Mathf.Sqrt(discr);

            // If t is negative, ray started inside sphere so clamp t to zero
            if (t < 0.0f) t = 0.0f;
            q = ray.origin + t * ray.direction;
            return true;
        }

        // Closest point
        public static bool ClosestPtRaySphere(Ray ray, Vector3 sphereOrigin, float sphereRadius, ref float t, ref Vector3 q)
        {
            Vector3 m = ray.origin - sphereOrigin;
            float b = Vector3.Dot(m, ray.direction);
            float c = Vector3.Dot(m, m) - (sphereRadius * sphereRadius);
            // Exit if r�s origin outside s (c > 0)and r pointing away from s (b > 0)
            if ((c > 0.0f) && (b > 0.0f))
            {
                // ray origin is closest
                t = 0.0f;
                q = ray.origin;
                return true;
            }

            float discr = (b * b) - c;

            // A negative discriminant corresponds to ray missing sphere
            if (discr < 0.0f)
            {
                discr = 0.0f;
            }

            // Ray now found to intersect sphere, compute smallest t value of intersection
            t = -b - Mathf.Sqrt(discr);

            // If t is negative, ray started inside sphere so clamp t to zero
            if (t < 0.0f) t = 0.0f;
            q = ray.origin + t * ray.direction;
            return true;
        }
    }
}
