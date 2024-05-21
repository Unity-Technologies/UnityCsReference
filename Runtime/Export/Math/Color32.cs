// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Representation of RGBA colors in 32 bit format
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Explicit)]
    public partial struct Color32 : IEquatable<Color32>, IFormattable
    {
        // An rgba int has been unioned with the four r g b a bytes.
        // This ensures the struct is 4 byte aligned for webgl.
        [FieldOffset(0)]
        [Ignore(DoesNotContributeToSize = true)]
        private int rgba;
        // Red component of the color.
        [FieldOffset(0)]
        public byte r;
        // Green component of the color.
        [FieldOffset(1)]
        public byte g;
        // Blue component of the color.
        [FieldOffset(2)]
        public byte b;
        // Alpha component of the color.
        [FieldOffset(3)]
        public byte a;

        // Constructs a new Color with given r, g, b, a components.
        public Color32(byte r, byte g, byte b, byte a)
        {
            rgba = 0; this.r = r; this.g = g; this.b = b; this.a = a;
        }

        // Color32 can be implicitly converted to and from [[Color]].
        public static implicit operator Color32(Color c)
        {
            return new Color32((byte)(Mathf.Round((Mathf.Clamp01(c.r) * 255f))),
                (byte)(Mathf.Round((Mathf.Clamp01(c.g) * 255f))),
                (byte)(Mathf.Round((Mathf.Clamp01(c.b) * 255f))),
                (byte)(Mathf.Round((Mathf.Clamp01(c.a) * 255f))));
        }

        // Color32 can be implicitly converted to and from [[Color]].
        public static implicit operator Color(Color32 c)
        {
            return new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        }

        // Interpolates between colors /a/ and /b/ by /t/.
        public static Color32 Lerp(Color32 a, Color32 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t),
                (byte)(a.a + (b.a - a.a) * t)
            );
        }

        // Interpolates between colors /a/ and /b/ by /t/ without clamping the interpolant
        public static Color32 LerpUnclamped(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)(a.r + (b.r - a.r) * t),
                (byte)(a.g + (b.g - a.g) * t),
                (byte)(a.b + (b.b - a.b) * t),
                (byte)(a.a + (b.a - a.a) * t)
            );
        }

        // Access the r, g, b,a components using [0], [1], [2], [3] respectively.
        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return r;
                    case 1: return g;
                    case 2: return b;
                    case 3: return a;
                    default:
                        throw new IndexOutOfRangeException("Invalid Color32 index(" + index + ")!");
                }
            }

            set
            {
                switch (index)
                {
                    case 0: r = value; break;
                    case 1: g = value; break;
                    case 2: b = value; break;
                    case 3: a = value; break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Color32 index(" + index + ")!");
                }
            }
        }

        public override int GetHashCode()
        {
            return rgba.GetHashCode();
        }

        public override bool Equals(object other)
        {
            if (other is Color32 color)
                return Equals(color);
            return false;
        }

        public bool Equals(Color32 other)
        {
            return rgba == other.rgba;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override string ToString()
        {
            return ToString(null, null);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return UnityString.Format("RGBA({0}, {1}, {2}, {3})", r.ToString(format, formatProvider), g.ToString(format, formatProvider), b.ToString(format, formatProvider), a.ToString(format, formatProvider));
        }
    }
} //namespace
