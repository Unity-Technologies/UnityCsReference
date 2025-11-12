// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Export/Math/SphericalHarmonicsL2.bindings.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct SphericalHarmonicsL2 : IEquatable<SphericalHarmonicsL2>
    {
        private float shr0, shr1, shr2, shr3, shr4, shr5, shr6, shr7, shr8;
        private float shg0, shg1, shg2, shg3, shg4, shg5, shg6, shg7, shg8;
        private float shb0, shb1, shb2, shb3, shb4, shb5, shb6, shb7, shb8;

        public void Clear()
        {
            SetZero();
        }

        private extern void SetZero();

        [FreeFunction]
        private extern static void Internal_AddAmbientLight(ref SphericalHarmonicsL2 sh, in Color color);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void AddAmbientLight(Color color) => Internal_AddAmbientLight(ref this, in color);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void AddAmbientLight(in Color color) => Internal_AddAmbientLight(ref this, in color);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void AddDirectionalLight(Vector3 direction, Color color, float intensity)
        {
            var colorAndIntensity = color * (2.0f * intensity);
            AddDirectionalLightInternal(ref this, in direction, in colorAndIntensity);
        }

        public void AddDirectionalLight(in Vector3 direction, in Color color, float intensity)
        {
            var colorAndIntensity = color * (2.0f * intensity);
            AddDirectionalLightInternal(ref this, in direction, in colorAndIntensity);
        }

        [FreeFunction]
        private extern static void AddDirectionalLightInternal(ref SphericalHarmonicsL2 sh, in Vector3 direction, in Color color);

        public readonly void Evaluate(Vector3[] directions, Color[] results)
        {
            if (directions == null)
                throw new ArgumentNullException("directions");

            if (results == null)
                throw new ArgumentNullException("results");

            if (directions.Length == 0)
                return;

            if (directions.Length != results.Length)
                throw new ArgumentException("Length of the directions array and the results array must match.");

            EvaluateInternal(in this, directions, results);
        }

        [FreeFunction]
        private extern static void EvaluateInternal(in SphericalHarmonicsL2 sh, Vector3[] directions, [Out] Color[] results);

        public float this[int rgb, int coefficient]
        {
            readonly get
            {
                int idx = rgb * 9 + coefficient;
                switch (idx)
                {
                    case  0: return shr0;
                    case  1: return shr1;
                    case  2: return shr2;
                    case  3: return shr3;
                    case  4: return shr4;
                    case  5: return shr5;
                    case  6: return shr6;
                    case  7: return shr7;
                    case  8: return shr8;
                    case  9: return shg0;
                    case 10: return shg1;
                    case 11: return shg2;
                    case 12: return shg3;
                    case 13: return shg4;
                    case 14: return shg5;
                    case 15: return shg6;
                    case 16: return shg7;
                    case 17: return shg8;
                    case 18: return shb0;
                    case 19: return shb1;
                    case 20: return shb2;
                    case 21: return shb3;
                    case 22: return shb4;
                    case 23: return shb5;
                    case 24: return shb6;
                    case 25: return shb7;
                    case 26: return shb8;
                    default:
                        throw new IndexOutOfRangeException("Invalid index!");
                }
            }

            set
            {
                int idx = rgb * 9 + coefficient;
                switch (idx)
                {
                    case  0: shr0 = value; break;
                    case  1: shr1 = value; break;
                    case  2: shr2 = value; break;
                    case  3: shr3 = value; break;
                    case  4: shr4 = value; break;
                    case  5: shr5 = value; break;
                    case  6: shr6 = value; break;
                    case  7: shr7 = value; break;
                    case  8: shr8 = value; break;
                    case  9: shg0 = value; break;
                    case 10: shg1 = value; break;
                    case 11: shg2 = value; break;
                    case 12: shg3 = value; break;
                    case 13: shg4 = value; break;
                    case 14: shg5 = value; break;
                    case 15: shg6 = value; break;
                    case 16: shg7 = value; break;
                    case 17: shg8 = value; break;
                    case 18: shb0 = value; break;
                    case 19: shb1 = value; break;
                    case 20: shb2 = value; break;
                    case 21: shb3 = value; break;
                    case 22: shb4 = value; break;
                    case 23: shb5 = value; break;
                    case 24: shb6 = value; break;
                    case 25: shb7 = value; break;
                    case 26: shb8 = value; break;
                    default:
                        throw new IndexOutOfRangeException("Invalid index!");
                }
            }
        }

        public override readonly int GetHashCode()
        {
            // Hash code idea from http://stackoverflow.com/a/263416

            // // Overflow is fine, just wrap
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + shr0.GetHashCode();
                hash = hash * 23 + shr1.GetHashCode();
                hash = hash * 23 + shr2.GetHashCode();
                hash = hash * 23 + shr3.GetHashCode();
                hash = hash * 23 + shr4.GetHashCode();
                hash = hash * 23 + shr5.GetHashCode();
                hash = hash * 23 + shr6.GetHashCode();
                hash = hash * 23 + shr7.GetHashCode();
                hash = hash * 23 + shr8.GetHashCode();
                hash = hash * 23 + shg0.GetHashCode();
                hash = hash * 23 + shg1.GetHashCode();
                hash = hash * 23 + shg2.GetHashCode();
                hash = hash * 23 + shg3.GetHashCode();
                hash = hash * 23 + shg4.GetHashCode();
                hash = hash * 23 + shg5.GetHashCode();
                hash = hash * 23 + shg6.GetHashCode();
                hash = hash * 23 + shg7.GetHashCode();
                hash = hash * 23 + shg8.GetHashCode();
                hash = hash * 23 + shb0.GetHashCode();
                hash = hash * 23 + shb1.GetHashCode();
                hash = hash * 23 + shb2.GetHashCode();
                hash = hash * 23 + shb3.GetHashCode();
                hash = hash * 23 + shb4.GetHashCode();
                hash = hash * 23 + shb5.GetHashCode();
                hash = hash * 23 + shb6.GetHashCode();
                hash = hash * 23 + shb7.GetHashCode();
                hash = hash * 23 + shb8.GetHashCode();
                return hash;
            }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other) => other is SphericalHarmonicsL2 sh && Equals(in sh);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(SphericalHarmonicsL2 other) => this == other;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in SphericalHarmonicsL2 other) => this == other;

        static public SphericalHarmonicsL2 operator*(SphericalHarmonicsL2 lhs, float rhs) => new SphericalHarmonicsL2() {
            shr0 = lhs.shr0 * rhs,
            shr1 = lhs.shr1 * rhs,
            shr2 = lhs.shr2 * rhs,
            shr3 = lhs.shr3 * rhs,
            shr4 = lhs.shr4 * rhs,
            shr5 = lhs.shr5 * rhs,
            shr6 = lhs.shr6 * rhs,
            shr7 = lhs.shr7 * rhs,
            shr8 = lhs.shr8 * rhs,
            shg0 = lhs.shg0 * rhs,
            shg1 = lhs.shg1 * rhs,
            shg2 = lhs.shg2 * rhs,
            shg3 = lhs.shg3 * rhs,
            shg4 = lhs.shg4 * rhs,
            shg5 = lhs.shg5 * rhs,
            shg6 = lhs.shg6 * rhs,
            shg7 = lhs.shg7 * rhs,
            shg8 = lhs.shg8 * rhs,
            shb0 = lhs.shb0 * rhs,
            shb1 = lhs.shb1 * rhs,
            shb2 = lhs.shb2 * rhs,
            shb3 = lhs.shb3 * rhs,
            shb4 = lhs.shb4 * rhs,
            shb5 = lhs.shb5 * rhs,
            shb6 = lhs.shb6 * rhs,
            shb7 = lhs.shb7 * rhs,
            shb8 = lhs.shb8 * rhs
        };

        static public SphericalHarmonicsL2 operator*(float lhs, SphericalHarmonicsL2 rhs) => new SphericalHarmonicsL2() {
            shr0 = rhs.shr0 * lhs,
            shr1 = rhs.shr1 * lhs,
            shr2 = rhs.shr2 * lhs,
            shr3 = rhs.shr3 * lhs,
            shr4 = rhs.shr4 * lhs,
            shr5 = rhs.shr5 * lhs,
            shr6 = rhs.shr6 * lhs,
            shr7 = rhs.shr7 * lhs,
            shr8 = rhs.shr8 * lhs,
            shg0 = rhs.shg0 * lhs,
            shg1 = rhs.shg1 * lhs,
            shg2 = rhs.shg2 * lhs,
            shg3 = rhs.shg3 * lhs,
            shg4 = rhs.shg4 * lhs,
            shg5 = rhs.shg5 * lhs,
            shg6 = rhs.shg6 * lhs,
            shg7 = rhs.shg7 * lhs,
            shg8 = rhs.shg8 * lhs,
            shb0 = rhs.shb0 * lhs,
            shb1 = rhs.shb1 * lhs,
            shb2 = rhs.shb2 * lhs,
            shb3 = rhs.shb3 * lhs,
            shb4 = rhs.shb4 * lhs,
            shb5 = rhs.shb5 * lhs,
            shb6 = rhs.shb6 * lhs,
            shb7 = rhs.shb7 * lhs,
            shb8 = rhs.shb8 * lhs
        };

        static public SphericalHarmonicsL2 operator+(SphericalHarmonicsL2 lhs, SphericalHarmonicsL2 rhs) => new SphericalHarmonicsL2() {
            shr0 = lhs.shr0 + rhs.shr0,
            shr1 = lhs.shr1 + rhs.shr1,
            shr2 = lhs.shr2 + rhs.shr2,
            shr3 = lhs.shr3 + rhs.shr3,
            shr4 = lhs.shr4 + rhs.shr4,
            shr5 = lhs.shr5 + rhs.shr5,
            shr6 = lhs.shr6 + rhs.shr6,
            shr7 = lhs.shr7 + rhs.shr7,
            shr8 = lhs.shr8 + rhs.shr8,
            shg0 = lhs.shg0 + rhs.shg0,
            shg1 = lhs.shg1 + rhs.shg1,
            shg2 = lhs.shg2 + rhs.shg2,
            shg3 = lhs.shg3 + rhs.shg3,
            shg4 = lhs.shg4 + rhs.shg4,
            shg5 = lhs.shg5 + rhs.shg5,
            shg6 = lhs.shg6 + rhs.shg6,
            shg7 = lhs.shg7 + rhs.shg7,
            shg8 = lhs.shg8 + rhs.shg8,
            shb0 = lhs.shb0 + rhs.shb0,
            shb1 = lhs.shb1 + rhs.shb1,
            shb2 = lhs.shb2 + rhs.shb2,
            shb3 = lhs.shb3 + rhs.shb3,
            shb4 = lhs.shb4 + rhs.shb4,
            shb5 = lhs.shb5 + rhs.shb5,
            shb6 = lhs.shb6 + rhs.shb6,
            shb7 = lhs.shb7 + rhs.shb7,
            shb8 = lhs.shb8 + rhs.shb8
        };

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(SphericalHarmonicsL2 lhs, SphericalHarmonicsL2 rhs) =>
            lhs.shr0 == rhs.shr0 &&
            lhs.shr1 == rhs.shr1 &&
            lhs.shr2 == rhs.shr2 &&
            lhs.shr3 == rhs.shr3 &&
            lhs.shr4 == rhs.shr4 &&
            lhs.shr5 == rhs.shr5 &&
            lhs.shr6 == rhs.shr6 &&
            lhs.shr7 == rhs.shr7 &&
            lhs.shr8 == rhs.shr8 &&
            lhs.shg0 == rhs.shg0 &&
            lhs.shg1 == rhs.shg1 &&
            lhs.shg2 == rhs.shg2 &&
            lhs.shg3 == rhs.shg3 &&
            lhs.shg4 == rhs.shg4 &&
            lhs.shg5 == rhs.shg5 &&
            lhs.shg6 == rhs.shg6 &&
            lhs.shg7 == rhs.shg7 &&
            lhs.shg8 == rhs.shg8 &&
            lhs.shb0 == rhs.shb0 &&
            lhs.shb1 == rhs.shb1 &&
            lhs.shb2 == rhs.shb2 &&
            lhs.shb3 == rhs.shb3 &&
            lhs.shb4 == rhs.shb4 &&
            lhs.shb5 == rhs.shb5 &&
            lhs.shb6 == rhs.shb6 &&
            lhs.shb7 == rhs.shb7 &&
            lhs.shb8 == rhs.shb8;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(SphericalHarmonicsL2 lhs, SphericalHarmonicsL2 rhs) => !(lhs == rhs);

    }
}
