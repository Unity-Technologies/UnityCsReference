// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngineInternal;


namespace UnityEngine.Rendering
{


[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct SphericalHarmonicsL2
{
    
            private float shr0, shr1, shr2, shr3, shr4, shr5, shr6, shr7, shr8;
            private float shg0, shg1, shg2, shg3, shg4, shg5, shg6, shg7, shg8;
            private float shb0, shb1, shb2, shb3, shb4, shb5, shb6, shb7, shb8;
    
    
    public void Clear()
        {
            ClearInternal(ref this);
        }
    
    
    private static void ClearInternal (ref SphericalHarmonicsL2 sh) {
        INTERNAL_CALL_ClearInternal ( ref sh );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_ClearInternal (ref SphericalHarmonicsL2 sh);
    public void AddAmbientLight(Color color)
        {
            AddAmbientLightInternal(color, ref this);
        }
    
    
    private static void AddAmbientLightInternal (Color color, ref SphericalHarmonicsL2 sh) {
        INTERNAL_CALL_AddAmbientLightInternal ( ref color, ref sh );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddAmbientLightInternal (ref Color color, ref SphericalHarmonicsL2 sh);
    public void AddDirectionalLight(Vector3 direction, Color color, float intensity)
        {
            Color colorAndIntensity = color * (2.0f * intensity);
            AddDirectionalLightInternal(direction, colorAndIntensity, ref this);
        }
    
    
    private static void AddDirectionalLightInternal (Vector3 direction, Color color, ref SphericalHarmonicsL2 sh) {
        INTERNAL_CALL_AddDirectionalLightInternal ( ref direction, ref color, ref sh );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_AddDirectionalLightInternal (ref Vector3 direction, ref Color color, ref SphericalHarmonicsL2 sh);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Evaluate (Vector3[] directions, Color[] results) ;

    
            public float this[int rgb, int coefficient]
            {
            get
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
    
    public override int GetHashCode()
        {

            unchecked { 
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
    
    public override bool Equals(object other)
        {
            if (!(other is SphericalHarmonicsL2))
                return false;

            SphericalHarmonicsL2 rhs = (SphericalHarmonicsL2)other;
            return (this == rhs);
        }
    
            static public SphericalHarmonicsL2 operator*(SphericalHarmonicsL2 lhs, float rhs)
        {
            SphericalHarmonicsL2 r = new SphericalHarmonicsL2();
            r.shr0 = lhs.shr0 * rhs;
            r.shr1 = lhs.shr1 * rhs;
            r.shr2 = lhs.shr2 * rhs;
            r.shr3 = lhs.shr3 * rhs;
            r.shr4 = lhs.shr4 * rhs;
            r.shr5 = lhs.shr5 * rhs;
            r.shr6 = lhs.shr6 * rhs;
            r.shr7 = lhs.shr7 * rhs;
            r.shr8 = lhs.shr8 * rhs;
            r.shg0 = lhs.shg0 * rhs;
            r.shg1 = lhs.shg1 * rhs;
            r.shg2 = lhs.shg2 * rhs;
            r.shg3 = lhs.shg3 * rhs;
            r.shg4 = lhs.shg4 * rhs;
            r.shg5 = lhs.shg5 * rhs;
            r.shg6 = lhs.shg6 * rhs;
            r.shg7 = lhs.shg7 * rhs;
            r.shg8 = lhs.shg8 * rhs;
            r.shb0 = lhs.shb0 * rhs;
            r.shb1 = lhs.shb1 * rhs;
            r.shb2 = lhs.shb2 * rhs;
            r.shb3 = lhs.shb3 * rhs;
            r.shb4 = lhs.shb4 * rhs;
            r.shb5 = lhs.shb5 * rhs;
            r.shb6 = lhs.shb6 * rhs;
            r.shb7 = lhs.shb7 * rhs;
            r.shb8 = lhs.shb8 * rhs;
            return r;
        }
    
            static public SphericalHarmonicsL2 operator*(float lhs, SphericalHarmonicsL2 rhs)
        {
            SphericalHarmonicsL2 r = new SphericalHarmonicsL2();
            r.shr0 = rhs.shr0 * lhs;
            r.shr1 = rhs.shr1 * lhs;
            r.shr2 = rhs.shr2 * lhs;
            r.shr3 = rhs.shr3 * lhs;
            r.shr4 = rhs.shr4 * lhs;
            r.shr5 = rhs.shr5 * lhs;
            r.shr6 = rhs.shr6 * lhs;
            r.shr7 = rhs.shr7 * lhs;
            r.shr8 = rhs.shr8 * lhs;
            r.shg0 = rhs.shg0 * lhs;
            r.shg1 = rhs.shg1 * lhs;
            r.shg2 = rhs.shg2 * lhs;
            r.shg3 = rhs.shg3 * lhs;
            r.shg4 = rhs.shg4 * lhs;
            r.shg5 = rhs.shg5 * lhs;
            r.shg6 = rhs.shg6 * lhs;
            r.shg7 = rhs.shg7 * lhs;
            r.shg8 = rhs.shg8 * lhs;
            r.shb0 = rhs.shb0 * lhs;
            r.shb1 = rhs.shb1 * lhs;
            r.shb2 = rhs.shb2 * lhs;
            r.shb3 = rhs.shb3 * lhs;
            r.shb4 = rhs.shb4 * lhs;
            r.shb5 = rhs.shb5 * lhs;
            r.shb6 = rhs.shb6 * lhs;
            r.shb7 = rhs.shb7 * lhs;
            r.shb8 = rhs.shb8 * lhs;
            return r;
        }
    
            static public SphericalHarmonicsL2 operator+(SphericalHarmonicsL2 lhs, SphericalHarmonicsL2 rhs)
        {
            SphericalHarmonicsL2 r = new SphericalHarmonicsL2();
            r.shr0 = lhs.shr0 + rhs.shr0;
            r.shr1 = lhs.shr1 + rhs.shr1;
            r.shr2 = lhs.shr2 + rhs.shr2;
            r.shr3 = lhs.shr3 + rhs.shr3;
            r.shr4 = lhs.shr4 + rhs.shr4;
            r.shr5 = lhs.shr5 + rhs.shr5;
            r.shr6 = lhs.shr6 + rhs.shr6;
            r.shr7 = lhs.shr7 + rhs.shr7;
            r.shr8 = lhs.shr8 + rhs.shr8;
            r.shg0 = lhs.shg0 + rhs.shg0;
            r.shg1 = lhs.shg1 + rhs.shg1;
            r.shg2 = lhs.shg2 + rhs.shg2;
            r.shg3 = lhs.shg3 + rhs.shg3;
            r.shg4 = lhs.shg4 + rhs.shg4;
            r.shg5 = lhs.shg5 + rhs.shg5;
            r.shg6 = lhs.shg6 + rhs.shg6;
            r.shg7 = lhs.shg7 + rhs.shg7;
            r.shg8 = lhs.shg8 + rhs.shg8;
            r.shb0 = lhs.shb0 + rhs.shb0;
            r.shb1 = lhs.shb1 + rhs.shb1;
            r.shb2 = lhs.shb2 + rhs.shb2;
            r.shb3 = lhs.shb3 + rhs.shb3;
            r.shb4 = lhs.shb4 + rhs.shb4;
            r.shb5 = lhs.shb5 + rhs.shb5;
            r.shb6 = lhs.shb6 + rhs.shb6;
            r.shb7 = lhs.shb7 + rhs.shb7;
            r.shb8 = lhs.shb8 + rhs.shb8;
            return r;
        }
    
            public static bool operator==(SphericalHarmonicsL2 lhs, SphericalHarmonicsL2 rhs)
        {
            return
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
        }
    
            public static bool operator!=(SphericalHarmonicsL2 lhs, SphericalHarmonicsL2 rhs)
        {
            return !(lhs == rhs);
        }
    
    
}

}
