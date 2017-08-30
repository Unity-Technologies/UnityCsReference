// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using scm = System.ComponentModel;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine
{
    // Representation of RGBA colors in 32 bit format
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Explicit)]
    public partial struct Color32
    {
        // An rgba int has been unioned with the four r g b a bytes.
        // This ensures the struct is 4 byte aligned for webgl.
        [FieldOffset(0)]
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
            return new Color32((byte)(Mathf.Clamp01(c.r) * 255), (byte)(Mathf.Clamp01(c.g) * 255), (byte)(Mathf.Clamp01(c.b) * 255), (byte)(Mathf.Clamp01(c.a) * 255));
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

        public override string ToString()
        {
            return UnityString.Format("RGBA({0}, {1}, {2}, {3})", r, g, b, a);
        }

        public string ToString(string format)
        {
            return UnityString.Format("RGBA({0}, {1}, {2}, {3})", r.ToString(format), g.ToString(format), b.ToString(format), a.ToString(format));
        }
    }
} //namespace
