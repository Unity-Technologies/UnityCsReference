// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using System.Globalization;
using System.Collections.Generic;

namespace UnityEngine
{
    // Representation of RGBA colors.

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Math/Color.h")]
    [NativeClass("ColorRGBAf")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    public struct Color : IEquatable<Color>, IFormattable
    {
        // Red component of the color.
        public float r;

        // Green component of the color.
        public float g;

        // Blue component of the color.
        public float b;

        // Alpha component of the color.
        public float a;


        // Constructs a new Color with given r,g,b,a components.
        public Color(float r, float g, float b, float a)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }

        // Constructs a new Color with given r,g,b components and sets /a/ to 1.
        public Color(float r, float g, float b)
        {
            this.r = r; this.g = g; this.b = b; this.a = 1.0F;
        }

        /// *listonly*
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override string ToString()
        {
            return ToString(null, null);
        }

        // Returns a nicely formatted string of this color.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F3";
            formatProvider ??= CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("RGBA({0}, {1}, {2}, {3})", r.ToString(format, formatProvider), g.ToString(format, formatProvider), b.ToString(format, formatProvider), a.ToString(format, formatProvider));
        }

        // used to allow Colors to be used as keys in hash tables
        public override int GetHashCode()
        {
            return ((Vector4)this).GetHashCode();
        }

        // also required for being able to use Colors as keys in hash tables
        public override bool Equals(object other)
        {
            if (other is Color color)
                return Equals(color);
            return false;
        }

        public bool Equals(Color other)
        {
            return r.Equals(other.r) && g.Equals(other.g) && b.Equals(other.b) && a.Equals(other.a);
        }

        // Adds two colors together. Each component is added separately.
        public static Color operator+(Color a, Color b) { return new Color(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a); }

        // Subtracts color /b/ from color /a/. Each component is subtracted separately.
        public static Color operator-(Color a, Color b) { return new Color(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a); }

        // Multiplies two colors together. Each component is multiplied separately.
        public static Color operator*(Color a, Color b) { return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a); }

        // Multiplies color /a/ by the float /b/. Each color component is scaled separately.
        public static Color operator*(Color a, float b) { return new Color(a.r * b, a.g * b, a.b * b, a.a * b); }

        // Multiplies color /a/ by the float /b/. Each color component is scaled separately.
        public static Color operator*(float b, Color a) { return new Color(a.r * b, a.g * b, a.b * b, a.a * b); }

        // Divides color /a/ by the float /b/. Each color component is scaled separately.
        public static Color operator/(Color a, float b) { return new Color(a.r / b, a.g / b, a.b / b, a.a / b); }

        //*undoc*
        public static bool operator==(Color lhs, Color rhs)
        {
            // Returns false in the presence of NaN values.
            return (Vector4)lhs == (Vector4)rhs;
        }

        //*undoc*
        public static bool operator!=(Color lhs, Color rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Interpolates between colors /a/ and /b/ by /t/.
        public static Color Lerp(Color a, Color b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(
                a.r + (b.r - a.r) * t,
                a.g + (b.g - a.g) * t,
                a.b + (b.b - a.b) * t,
                a.a + (b.a - a.a) * t
            );
        }

        // Interpolates between colors /a/ and /b/ by /t/ without clamping the interpolant
        public static Color LerpUnclamped(Color a, Color b, float t)
        {
            return new Color(
                a.r + (b.r - a.r) * t,
                a.g + (b.g - a.g) * t,
                a.b + (b.b - a.b) * t,
                a.a + (b.a - a.a) * t
            );
        }

        // Returns new color that has RGB components multiplied, but leaving alpha untouched.
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal Color RGBMultiplied(float multiplier) { return new Color(r * multiplier, g * multiplier, b * multiplier, a); }
        // Returns new color that has RGB components multiplied, but leaving alpha untouched.
        internal Color AlphaMultiplied(float multiplier) { return new Color(r, g, b, a * multiplier); }
        // Returns new color that has RGB components multiplied, but leaving alpha untouched.
        internal Color RGBMultiplied(Color multiplier) { return new Color(r * multiplier.r, g * multiplier.g, b * multiplier.b, a); }

        // The grayscale value of the color (RO)
        public float grayscale { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return 0.299F * r + 0.587F * g + 0.114F * b; } }

        // A version of the color that has had the inverse gamma curve applied
        public Color linear
        {
            get
            {
                return new Color(Mathf.GammaToLinearSpace(r), Mathf.GammaToLinearSpace(g), Mathf.GammaToLinearSpace(b), a);
            }
        }

        // A version of the color that has had the gamma curve applied
        public Color gamma
        {
            get
            {
                return new Color(Mathf.LinearToGammaSpace(r), Mathf.LinearToGammaSpace(g), Mathf.LinearToGammaSpace(b), a);
            }
        }

        public float maxColorComponent
        {
            get
            {
                return Mathf.Max(Mathf.Max(r, g), b);
            }
        }

        // Colors can be implicitly converted to and from [[Vector4]].
        public static implicit operator Vector4(Color c)
        {
            return new Vector4(c.r, c.g, c.b, c.a);
        }

        // Colors can be implicitly converted to and from [[Vector4]].
        public static implicit operator Color(Vector4 v)
        {
            return new Color(v.x, v.y, v.z, v.w);
        }

        // Access the r, g, b,a components using [0], [1], [2], [3] respectively.
        public float this[int index]
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
                        throw new IndexOutOfRangeException("Invalid Color index(" + index + ")!");
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
                        throw new IndexOutOfRangeException("Invalid Color index(" + index + ")!");
                }
            }
        }

        // Convert a color from RGB to HSV color space.
        public static void RGBToHSV(Color rgbColor, out float H, out float S, out float V)
        {
            // when blue is highest valued
            if ((rgbColor.b > rgbColor.g) && (rgbColor.b > rgbColor.r))
                RGBToHSVHelper((float)4, rgbColor.b, rgbColor.r, rgbColor.g, out H, out S, out V);
            //when green is highest valued
            else if (rgbColor.g > rgbColor.r)
                RGBToHSVHelper((float)2, rgbColor.g, rgbColor.b, rgbColor.r, out H, out S, out V);
            //when red is highest valued
            else
                RGBToHSVHelper((float)0, rgbColor.r, rgbColor.g, rgbColor.b, out H, out S, out V);
        }

        static void RGBToHSVHelper(float offset, float dominantcolor, float colorone, float colortwo, out float H, out float S, out float V)
        {
            V = dominantcolor;
            //we need to find out which is the minimum color
            if (V != 0)
            {
                //we check which color is smallest
                float small = 0;
                if (colorone > colortwo) small = colortwo;
                else small = colorone;

                float diff = V - small;

                //if the two values are not the same, we compute the like this
                if (diff != 0)
                {
                    //S = max-min/max
                    S = diff / V;
                    //H = hue is offset by X, and is the difference between the two smallest colors
                    H = offset + ((colorone - colortwo) / diff);
                }
                else
                {
                    //S = 0 when the difference is zero
                    S = 0;
                    //H = 4 + (R-G) hue is offset by 4 when blue, and is the difference between the two smallest colors
                    H = offset + (colorone - colortwo);
                }

                H /= 6;

                //conversion values
                if (H < 0)
                    H += 1.0f;
            }
            else
            {
                S = 0;
                H = 0;
            }
        }

        public static Color HSVToRGB(float H, float S, float V)
        {
            return HSVToRGB(H, S, V, true);
        }

        // Convert a set of HSV values to an RGB Color.
        public static Color HSVToRGB(float H, float S, float V, bool hdr)
        {
            Color retval = white;
            if (S == 0)
            {
                retval.r = V;
                retval.g = V;
                retval.b = V;
            }
            else if (V == 0)
            {
                retval.r = 0;
                retval.g = 0;
                retval.b = 0;
            }
            else
            {
                retval.r = 0;
                retval.g = 0;
                retval.b = 0;

                //crazy hsv conversion
                float t_S, t_V, h_to_floor;

                t_S = S;
                t_V = V;
                h_to_floor = H * 6.0f;

                int temp = (int)Mathf.Floor(h_to_floor);
                float t = h_to_floor - ((float)temp);
                float var_1 = (t_V) * (1 - t_S);
                float var_2 = t_V * (1 - t_S *  t);
                float var_3 = t_V * (1 - t_S * (1 - t));

                switch (temp)
                {
                    case 0:
                        retval.r = t_V;
                        retval.g = var_3;
                        retval.b = var_1;
                        break;

                    case 1:
                        retval.r = var_2;
                        retval.g = t_V;
                        retval.b = var_1;
                        break;

                    case 2:
                        retval.r = var_1;
                        retval.g = t_V;
                        retval.b = var_3;
                        break;

                    case 3:
                        retval.r = var_1;
                        retval.g = var_2;
                        retval.b = t_V;
                        break;

                    case 4:
                        retval.r = var_3;
                        retval.g = var_1;
                        retval.b = t_V;
                        break;

                    case 5:
                        retval.r = t_V;
                        retval.g = var_1;
                        retval.b = var_2;
                        break;

                    case 6:
                        retval.r = t_V;
                        retval.g = var_3;
                        retval.b = var_1;
                        break;

                    case -1:
                        retval.r = t_V;
                        retval.g = var_1;
                        retval.b = var_2;
                        break;
                }

                if (!hdr)
                {
                    retval.r = Mathf.Clamp(retval.r, 0.0f, 1.0f);
                    retval.g = Mathf.Clamp(retval.g, 0.0f, 1.0f);
                    retval.b = Mathf.Clamp(retval.b, 0.0f, 1.0f);
                }
            }
            return retval;
        }

        #region Preset Colors

        // The original reference: https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.colors

        // Color Preset of @@RGBA(0.9411765f, 0.9725491f, 1f, 1f)@@
        public static Color aliceBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9411765f, 0.9725491f, 1f, 1f); }

        // Color Preset of @@RGBA(0.9803922f, 0.9215687f, 0.8431373f, 1f)@@
        public static Color antiqueWhite { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9803922f, 0.9215687f, 0.8431373f, 1f); }

        // Color Preset of @@RGBA(0.4980392f, 1f, 0.8313726f, 1f)@@
        public static Color aquamarine { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4980392f, 1f, 0.8313726f, 1f); }

        // Color Preset of @@RGBA(0.9411765f, 1f, 1f, 1f)@@
        public static Color azure { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9411765f, 1f, 1f, 1f); }

        // Color Preset of @@RGBA(0.9607844f, 0.9607844f, 0.8627452f, 1f)@@
        public static Color beige { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9607844f, 0.9607844f, 0.8627452f, 1f); }

        // Color Preset of @@RGBA(1f, 0.8941177f, 0.7686275f, 1f)@@
        public static Color bisque { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.8941177f, 0.7686275f, 1f); }

        // Color Preset of @@RGBA(0f, 0f, 0f, 1f)@@
        public static Color black { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0f, 0f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9215687f, 0.8039216f, 1f)@@
        public static Color blanchedAlmond { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9215687f, 0.8039216f, 1f); }

        // Color Preset of @@RGBA(0f, 0f, 1f, 1f)@@
        public static Color blue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0f, 1f, 1f); }

        // Color Preset of @@RGBA(0.5411765f, 0.1686275f, 0.8862746f, 1f)@@
        public static Color blueViolet { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5411765f, 0.1686275f, 0.8862746f, 1f); }

        // Color Preset of @@RGBA(0.6470588f, 0.1647059f, 0.1647059f, 1f)@@
        public static Color brown { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6470588f, 0.1647059f, 0.1647059f, 1f); }

        // Color Preset of @@RGBA(0.8705883f, 0.7215686f, 0.5294118f, 1f)@@
        public static Color burlywood { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8705883f, 0.7215686f, 0.5294118f, 1f); }

        // Color Preset of @@RGBA(0.372549f, 0.6196079f, 0.627451f, 1f)@@
        public static Color cadetBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.372549f, 0.6196079f, 0.627451f, 1f); }

        // Color Preset of @@RGBA(0.4980392f, 1f, 0f, 1f)@@
        public static Color chartreuse { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4980392f, 1f, 0f, 1f); }

        // Color Preset of @@RGBA(0.8235295f, 0.4117647f, 0.1176471f, 1f)@@
        public static Color chocolate { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8235295f, 0.4117647f, 0.1176471f, 1f); }

        // Color Preset of @@RGBA(0f, 0f, 0f, 0f)@@
        public static Color clear { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0f, 0f, 0f); }

        // Color Preset of @@RGBA(1f, 0.4980392f, 0.3137255f, 1f)@@
        public static Color coral { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.4980392f, 0.3137255f, 1f); }

        // Color Preset of @@RGBA(0.3921569f, 0.5843138f, 0.9294118f, 1f)@@
        public static Color cornflowerBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.3921569f, 0.5843138f, 0.9294118f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9725491f, 0.8627452f, 1f)@@
        public static Color cornsilk { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9725491f, 0.8627452f, 1f); }

        // Color Preset of @@RGBA(0.8627452f, 0.07843138f, 0.2352941f, 1f)@@
        public static Color crimson { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8627452f, 0.07843138f, 0.2352941f, 1f); }

        // Color Preset of @@RGBA(0f, 1f, 1f, 1f)@@
        public static Color cyan { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 1f, 1f, 1f); }

        // Color Preset of @@RGBA(0f, 0f, 0.5450981f, 1f)@@
        public static Color darkBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0f, 0.5450981f, 1f); }

        // Color Preset of @@RGBA(0f, 0.5450981f, 0.5450981f, 1f)@@
        public static Color darkCyan { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0.5450981f, 0.5450981f, 1f); }

        // Color Preset of @@RGBA(0.7215686f, 0.5254902f, 0.04313726f, 1f)@@
        public static Color darkGoldenRod { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.7215686f, 0.5254902f, 0.04313726f, 1f); }

        // Color Preset of @@RGBA(0.6627451f, 0.6627451f, 0.6627451f, 1f)@@
        public static Color darkGray { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6627451f, 0.6627451f, 0.6627451f, 1f); }

        // Color Preset of @@RGBA(0f, 0.3921569f, 0f, 1f)@@
        public static Color darkGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0.3921569f, 0f, 1f); }

        // Color Preset of @@RGBA(0.7411765f, 0.7176471f, 0.4196079f, 1f)@@
        public static Color darkKhaki { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.7411765f, 0.7176471f, 0.4196079f, 1f); }

        // Color Preset of @@RGBA(0.5450981f, 0f, 0.5450981f, 1f)@@
        public static Color darkMagenta { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5450981f, 0f, 0.5450981f, 1f); }

        // Color Preset of @@RGBA(0.3333333f, 0.4196079f, 0.1843137f, 1f)@@
        public static Color darkOliveGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.3333333f, 0.4196079f, 0.1843137f, 1f); }

        // Color Preset of @@RGBA(1f, 0.5490196f, 0f, 1f)@@
        public static Color darkOrange { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.5490196f, 0f, 1f); }

        // Color Preset of @@RGBA(0.6f, 0.1960784f, 0.8000001f, 1f)@@
        public static Color darkOrchid { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6f, 0.1960784f, 0.8000001f, 1f); }

        // Color Preset of @@RGBA(0.5450981f, 0f, 0f, 1f)@@
        public static Color darkRed { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5450981f, 0f, 0f, 1f); }

        // Color Preset of @@RGBA(0.9137256f, 0.5882353f, 0.4784314f, 1f)@@
        public static Color darkSalmon { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9137256f, 0.5882353f, 0.4784314f, 1f); }

        // Color Preset of @@RGBA(0.5607843f, 0.7372549f, 0.5607843f, 1f)@@
        public static Color darkSeaGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5607843f, 0.7372549f, 0.5607843f, 1f); }

        // Color Preset of @@RGBA(0.282353f, 0.2392157f, 0.5450981f, 1f)@@
        public static Color darkSlateBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.282353f, 0.2392157f, 0.5450981f, 1f); }

        // Color Preset of @@RGBA(0.1843137f, 0.3098039f, 0.3098039f, 1f)@@
        public static Color darkSlateGray { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1843137f, 0.3098039f, 0.3098039f, 1f); }

        // Color Preset of @@RGBA(0f, 0.8078432f, 0.8196079f, 1f)@@
        public static Color darkTurquoise { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0.8078432f, 0.8196079f, 1f); }

        // Color Preset of @@RGBA(0.5803922f, 0f, 0.8274511f, 1f)@@
        public static Color darkViolet { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5803922f, 0f, 0.8274511f, 1f); }

        // Color Preset of @@RGBA(1f, 0.07843138f, 0.5764706f, 1f)@@
        public static Color deepPink { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.07843138f, 0.5764706f, 1f); }

        // Color Preset of @@RGBA(0f, 0.7490196f, 1f, 1f)@@
        public static Color deepSkyBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0.7490196f, 1f, 1f); }

        // Color Preset of @@RGBA(0.4117647f, 0.4117647f, 0.4117647f, 1f)@@
        public static Color dimGray { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4117647f, 0.4117647f, 0.4117647f, 1f); }

        // Color Preset of @@RGBA(0.1176471f, 0.5647059f, 1f, 1f)@@
        public static Color dodgerBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1176471f, 0.5647059f, 1f, 1f); }

        // Color Preset of @@RGBA(0.6980392f, 0.1333333f, 0.1333333f, 1f)@@
        public static Color firebrick { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6980392f, 0.1333333f, 0.1333333f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9803922f, 0.9411765f, 1f)@@
        public static Color floralWhite { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9803922f, 0.9411765f, 1f); }

        // Color Preset of @@RGBA(0.1333333f, 0.5450981f, 0.1333333f, 1f)@@
        public static Color forestGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1333333f, 0.5450981f, 0.1333333f, 1f); }

        // Color Preset of @@RGBA(0.8627452f, 0.8627452f, 0.8627452f, 1f)@@
        public static Color gainsboro { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8627452f, 0.8627452f, 0.8627452f, 1f); }

        // Color Preset of @@RGBA(0.9725491f, 0.9725491f, 1f, 1f)@@
        public static Color ghostWhite { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9725491f, 0.9725491f, 1f, 1f); }

        // Color Preset of @@RGBA(1f, 0.8431373f, 0f, 1f)@@
        public static Color gold { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.8431373f, 0f, 1f); }

        // Color Preset of @@RGBA(0.854902f, 0.6470588f, 0.1254902f, 1f)@@
        public static Color goldenRod { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.854902f, 0.6470588f, 0.1254902f, 1f); }

        // Color Preset of @@RGBA(0.5f, 0.5f, 0.5f, 1f)@@
        public static Color gray => gray5;

        // Color Preset of @@RGBA(0.5f, 0.5f, 0.5f, 1f)@@
        public static Color grey => gray5;

        // Color Preset of @@RGBA(0.1f, 0.1f, 0.1f, 1f)@@
        public static Color gray1 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1f, 0.1f, 0.1f, 1f); }

        // Color Preset of @@RGBA(0.2f, 0.2f, 0.2f, 1f)@@
        public static Color gray2 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.2f, 0.2f, 0.2f, 1f); }

        // Color Preset of @@RGBA(0.3f, 0.3f, 0.3f, 1f)@@
        public static Color gray3 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.3f, 0.3f, 0.3f, 1f); }

        // Color Preset of @@RGBA(0.4f, 0.4f, 0.4f, 1f)@@
        public static Color gray4 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4f, 0.4f, 0.4f, 1f); }

        // Color Preset of @@RGBA(0.5f, 0.5f, 0.5f, 1f)@@
        public static Color gray5 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5f, 0.5f, 0.5f, 1f); }

        // Color Preset of @@RGBA(0.6f, 0.6f, 0.6f, 1f)@@
        public static Color gray6 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6f, 0.6f, 0.6f, 1f); }

        // Color Preset of @@RGBA(0.7f, 0.7f, 0.7f, 1f)@@
        public static Color gray7 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.7f, 0.7f, 0.7f, 1f); }

        // Color Preset of @@RGBA(0.8f, 0.8f, 0.8f, 1f)@@
        public static Color gray8 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8f, 0.8f, 0.8f, 1f); }

        // Color Preset of @@RGBA(0.9f, 0.9f, 0.9f, 1f)@@
        public static Color gray9 { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9f, 0.9f, 0.9f, 1f); }

        // Color Preset of @@RGBA(0f, 1f, 0f, 1f)@@
        public static Color green { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 1f, 0f, 1f); }

        // Color Preset of @@RGBA(0.6784314f, 1f, 0.1843137f, 1f)@@
        public static Color greenYellow { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6784314f, 1f, 0.1843137f, 1f); }

        // Color Preset of @@RGBA(0.9411765f, 1f, 0.9411765f, 1f)@@
        public static Color honeydew { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9411765f, 1f, 0.9411765f, 1f); }

        // Color Preset of @@RGBA(1f, 0.4117647f, 0.7058824f, 1f)@@
        public static Color hotPink { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.4117647f, 0.7058824f, 1f); }

        // Color Preset of @@RGBA(0.8039216f, 0.3607843f, 0.3607843f, 1f)@@
        public static Color indianRed { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8039216f, 0.3607843f, 0.3607843f, 1f); }

        // Color Preset of @@RGBA(0.2941177f, 0f, 0.509804f, 1f)@@
        public static Color indigo { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.2941177f, 0f, 0.509804f, 1f); }

        // Color Preset of @@RGBA(1f, 1f, 0.9411765f, 1f)@@
        public static Color ivory { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 1f, 0.9411765f, 1f); }

        // Color Preset of @@RGBA(0.9411765f, 0.9019608f, 0.5490196f, 1f)@@
        public static Color khaki { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9411765f, 0.9019608f, 0.5490196f, 1f); }

        // Color Preset of @@RGBA(0.9019608f, 0.9019608f, 0.9803922f, 1f)@@
        public static Color lavender { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9019608f, 0.9019608f, 0.9803922f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9411765f, 0.9607844f, 1f)@@
        public static Color lavenderBlush { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9411765f, 0.9607844f, 1f); }

        // Color Preset of @@RGBA(0.4862745f, 0.9882354f, 0f, 1f)@@
        public static Color lawnGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4862745f, 0.9882354f, 0f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9803922f, 0.8039216f, 1f)@@
        public static Color lemonChiffon { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9803922f, 0.8039216f, 1f); }

        // Color Preset of @@RGBA(0.6784314f, 0.8470589f, 0.9019608f, 1f)@@
        public static Color lightBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6784314f, 0.8470589f, 0.9019608f, 1f); }

        // Color Preset of @@RGBA(0.9411765f, 0.5019608f, 0.5019608f, 1f)@@
        public static Color lightCoral { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9411765f, 0.5019608f, 0.5019608f, 1f); }

        // Color Preset of @@RGBA(0.8784314f, 1f, 1f, 1f)@@
        public static Color lightCyan { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8784314f, 1f, 1f, 1f); }

        // Color Preset of @@RGBA(0.9333334f, 0.8666667f, 0.509804f, 1f)@@
        public static Color lightGoldenRod { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9333334f, 0.8666667f, 0.509804f, 1f); }

        // Color Preset of @@RGBA(0.9803922f, 0.9803922f, 0.8235295f, 1f)@@
        public static Color lightGoldenRodYellow { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9803922f, 0.9803922f, 0.8235295f, 1f); }

        // Color Preset of @@RGBA(0.8274511f, 0.8274511f, 0.8274511f, 1f)@@
        public static Color lightGray { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8274511f, 0.8274511f, 0.8274511f, 1f); }

        // Color Preset of @@RGBA(0.5647059f, 0.9333334f, 0.5647059f, 1f)@@
        public static Color lightGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5647059f, 0.9333334f, 0.5647059f, 1f); }

        // Color Preset of @@RGBA(1f, 0.7137255f, 0.7568628f, 1f)@@
        public static Color lightPink { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.7137255f, 0.7568628f, 1f); }

        // Color Preset of @@RGBA(1f, 0.627451f, 0.4784314f, 1f)@@
        public static Color lightSalmon { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.627451f, 0.4784314f, 1f); }

        // Color Preset of @@RGBA(0.1254902f, 0.6980392f, 0.6666667f, 1f)@@
        public static Color lightSeaGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1254902f, 0.6980392f, 0.6666667f, 1f); }

        // Color Preset of @@RGBA(0.5294118f, 0.8078432f, 0.9803922f, 1f)@@
        public static Color lightSkyBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5294118f, 0.8078432f, 0.9803922f, 1f); }

        // Color Preset of @@RGBA(0.5176471f, 0.4392157f, 1f, 1f)@@
        public static Color lightSlateBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5176471f, 0.4392157f, 1f, 1f); }

        // Color Preset of @@RGBA(0.4666667f, 0.5333334f, 0.6f, 1f)@@
        public static Color lightSlateGray { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4666667f, 0.5333334f, 0.6f, 1f); }

        // Color Preset of @@RGBA(0.6901961f, 0.7686275f, 0.8705883f, 1f)@@
        public static Color lightSteelBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6901961f, 0.7686275f, 0.8705883f, 1f); }

        // Color Preset of @@RGBA(1f, 1f, 0.8784314f, 1f)@@
        public static Color lightYellow { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 1f, 0.8784314f, 1f); }

        // Color Preset of @@RGBA(0.1960784f, 0.8039216f, 0.1960784f, 1f)@@
        public static Color limeGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1960784f, 0.8039216f, 0.1960784f, 1f); }

        // Color Preset of @@RGBA(0.9803922f, 0.9411765f, 0.9019608f, 1f)@@
        public static Color linen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9803922f, 0.9411765f, 0.9019608f, 1f); }

        // Color Preset of @@RGBA(1f, 0f, 1f, 1f)@@
        public static Color magenta { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0f, 1f, 1f); }

        // Color Preset of @@RGBA(0.6901961f, 0.1882353f, 0.3764706f, 1f)@@
        public static Color maroon { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6901961f, 0.1882353f, 0.3764706f, 1f); }

        // Color Preset of @@RGBA(0.4f, 0.8039216f, 0.6666667f, 1f)@@
        public static Color mediumAquamarine { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4f, 0.8039216f, 0.6666667f, 1f); }

        // Color Preset of @@RGBA(0f, 0f, 0.8039216f, 1f)@@
        public static Color mediumBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0f, 0.8039216f, 1f); }

        // Color Preset of @@RGBA(0.7294118f, 0.3333333f, 0.8274511f, 1f)@@
        public static Color mediumOrchid { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.7294118f, 0.3333333f, 0.8274511f, 1f); }

        // Color Preset of @@RGBA(0.5764706f, 0.4392157f, 0.8588236f, 1f)@@
        public static Color mediumPurple { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5764706f, 0.4392157f, 0.8588236f, 1f); }

        // Color Preset of @@RGBA(0.2352941f, 0.7019608f, 0.4431373f, 1f)@@
        public static Color mediumSeaGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.2352941f, 0.7019608f, 0.4431373f, 1f); }

        // Color Preset of @@RGBA(0.482353f, 0.4078432f, 0.9333334f, 1f)@@
        public static Color mediumSlateBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.482353f, 0.4078432f, 0.9333334f, 1f); }

        // Color Preset of @@RGBA(0f, 0.9803922f, 0.6039216f, 1f)@@
        public static Color mediumSpringGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0.9803922f, 0.6039216f, 1f); }

        // Color Preset of @@RGBA(0.282353f, 0.8196079f, 0.8000001f, 1f)@@
        public static Color mediumTurquoise { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.282353f, 0.8196079f, 0.8000001f, 1f); }

        // Color Preset of @@RGBA(0.7803922f, 0.08235294f, 0.5215687f, 1f)@@
        public static Color mediumVioletRed { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.7803922f, 0.08235294f, 0.5215687f, 1f); }

        // Color Preset of @@RGBA(0.09803922f, 0.09803922f, 0.4392157f, 1f)@@
        public static Color midnightBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.09803922f, 0.09803922f, 0.4392157f, 1f); }

        // Color Preset of @@RGBA(0.9607844f, 1f, 0.9803922f, 1f)@@
        public static Color mintCream { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9607844f, 1f, 0.9803922f, 1f); }

        // Color Preset of @@RGBA(1f, 0.8941177f, 0.882353f, 1f)@@
        public static Color mistyRose { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.8941177f, 0.882353f, 1f); }

        // Color Preset of @@RGBA(1f, 0.8941177f, 0.7098039f, 1f)@@
        public static Color moccasin { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.8941177f, 0.7098039f, 1f); }

        // Color Preset of @@RGBA(1f, 0.8705883f, 0.6784314f, 1f)@@
        public static Color navajoWhite { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.8705883f, 0.6784314f, 1f); }

        // Color Preset of @@RGBA(0f, 0f, 0.5019608f, 1f)@@
        public static Color navyBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0f, 0.5019608f, 1f); }

        // Color Preset of @@RGBA(0.9921569f, 0.9607844f, 0.9019608f, 1f)@@
        public static Color oldLace { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9921569f, 0.9607844f, 0.9019608f, 1f); }

        // Color Preset of @@RGBA(0.5019608f, 0.5019608f, 0f, 1f)@@
        public static Color olive { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5019608f, 0.5019608f, 0f, 1f); }

        // Color Preset of @@RGBA(0.4196079f, 0.5568628f, 0.1372549f, 1f)@@
        public static Color oliveDrab { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4196079f, 0.5568628f, 0.1372549f, 1f); }

        // Color Preset of @@RGBA(1f, 0.6470588f, 0f, 1f)@@
        public static Color orange { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.6470588f, 0f, 1f); }

        // Color Preset of @@RGBA(1f, 0.2705882f, 0f, 1f)@@
        public static Color orangeRed { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.2705882f, 0f, 1f); }

        // Color Preset of @@RGBA(0.854902f, 0.4392157f, 0.8392158f, 1f)@@
        public static Color orchid { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.854902f, 0.4392157f, 0.8392158f, 1f); }

        // Color Preset of @@RGBA(0.9333334f, 0.909804f, 0.6666667f, 1f)@@
        public static Color paleGoldenRod { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9333334f, 0.909804f, 0.6666667f, 1f); }

        // Color Preset of @@RGBA(0.5960785f, 0.9843138f, 0.5960785f, 1f)@@
        public static Color paleGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5960785f, 0.9843138f, 0.5960785f, 1f); }

        // Color Preset of @@RGBA(0.6862745f, 0.9333334f, 0.9333334f, 1f)@@
        public static Color paleTurquoise { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6862745f, 0.9333334f, 0.9333334f, 1f); }

        // Color Preset of @@RGBA(0.8588236f, 0.4392157f, 0.5764706f, 1f)@@
        public static Color paleVioletRed { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8588236f, 0.4392157f, 0.5764706f, 1f); }

        // Color Preset of @@RGBA(1f, 0.937255f, 0.8352942f, 1f)@@
        public static Color papayaWhip { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.937255f, 0.8352942f, 1f); }

        // Color Preset of @@RGBA(1f, 0.854902f, 0.7254902f, 1f)@@
        public static Color peachPuff { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.854902f, 0.7254902f, 1f); }

        // Color Preset of @@RGBA(0.8039216f, 0.5215687f, 0.2470588f, 1f)@@
        public static Color peru { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8039216f, 0.5215687f, 0.2470588f, 1f); }

        // Color Preset of @@RGBA(1f, 0.7529413f, 0.7960785f, 1f)@@
        public static Color pink { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.7529413f, 0.7960785f, 1f); }

        // Color Preset of @@RGBA(0.8666667f, 0.627451f, 0.8666667f, 1f)@@
        public static Color plum { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8666667f, 0.627451f, 0.8666667f, 1f); }

        // Color Preset of @@RGBA(0.6901961f, 0.8784314f, 0.9019608f, 1f)@@
        public static Color powderBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6901961f, 0.8784314f, 0.9019608f, 1f); }

        // Color Preset of @@RGBA(0.627451f, 0.1254902f, 0.9411765f, 1f)@@
        public static Color purple { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.627451f, 0.1254902f, 0.9411765f, 1f); }

        // Color Preset of @@RGBA(0.4f, 0.2f, 0.6f, 1f)@@
        public static Color rebeccaPurple { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4f, 0.2f, 0.6f, 1f); }

        // Color Preset of @@RGBA(1f, 0f, 0f, 1f)@@
        public static Color red { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0f, 0f, 1f); }

        // Color Preset of @@RGBA(0.7372549f, 0.5607843f, 0.5607843f, 1f)@@
        public static Color rosyBrown { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.7372549f, 0.5607843f, 0.5607843f, 1f); }

        // Color Preset of @@RGBA(0.254902f, 0.4117647f, 0.882353f, 1f)@@
        public static Color royalBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.254902f, 0.4117647f, 0.882353f, 1f); }

        // Color Preset of @@RGBA(0.5450981f, 0.2705882f, 0.07450981f, 1f)@@
        public static Color saddleBrown { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5450981f, 0.2705882f, 0.07450981f, 1f); }

        // Color Preset of @@RGBA(0.9803922f, 0.5019608f, 0.4470589f, 1f)@@
        public static Color salmon { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9803922f, 0.5019608f, 0.4470589f, 1f); }

        // Color Preset of @@RGBA(0.9568628f, 0.6431373f, 0.3764706f, 1f)@@
        public static Color sandyBrown { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9568628f, 0.6431373f, 0.3764706f, 1f); }

        // Color Preset of @@RGBA(0.1803922f, 0.5450981f, 0.3411765f, 1f)@@
        public static Color seaGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1803922f, 0.5450981f, 0.3411765f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9607844f, 0.9333334f, 1f)@@
        public static Color seashell { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9607844f, 0.9333334f, 1f); }

        // Color Preset of @@RGBA(0.627451f, 0.3215686f, 0.1764706f, 1f)@@
        public static Color sienna { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.627451f, 0.3215686f, 0.1764706f, 1f); }

        // Color Preset of @@RGBA(0.7529413f, 0.7529413f, 0.7529413f, 1f)@@
        public static Color silver { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.7529413f, 0.7529413f, 0.7529413f, 1f); }

        // Color Preset of @@RGBA(0.5294118f, 0.8078432f, 0.9215687f, 1f)@@
        public static Color skyBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5294118f, 0.8078432f, 0.9215687f, 1f); }

        // Color Preset of @@RGBA(0.4156863f, 0.3529412f, 0.8039216f, 1f)@@
        public static Color slateBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4156863f, 0.3529412f, 0.8039216f, 1f); }

        // Color Preset of @@RGBA(0.4392157f, 0.5019608f, 0.5647059f, 1f)@@
        public static Color slateGray { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.4392157f, 0.5019608f, 0.5647059f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9803922f, 0.9803922f, 1f)@@
        public static Color snow { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9803922f, 0.9803922f, 1f); }

        // Color Preset of @@RGBA(0.8627452f, 0.1921569f, 0.1960784f, 1f); }
        public static Color softRed { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8627452f, 0.1921569f, 0.1960784f, 1f); }

        // Color Preset of @@RGBA(0.1882353f, 0.682353f, 0.7490196f, 1f); }
        public static Color softBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.1882353f, 0.682353f, 0.7490196f, 1f); }

        // Color Preset of @@RGBA(0.5490196f, 0.7882354f, 0.1411765f, 1f)@@
        public static Color softGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.5490196f, 0.7882354f, 0.1411765f, 1f); }

        // Color Preset of @@RGBA(1f, 0.9333334f, 0.5490196f, 1f)@@
        public static Color softYellow { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.9333334f, 0.5490196f, 1f); }

        // Color Preset of @@RGBA(0f, 1f, 0.4980392f, 1f)@@
        public static Color springGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 1f, 0.4980392f, 1f); }

        // Color Preset of @@RGBA(0.2745098f, 0.509804f, 0.7058824f, 1f)@@
        public static Color steelBlue { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.2745098f, 0.509804f, 0.7058824f, 1f); }

        // Color Preset of @@RGBA(0.8235295f, 0.7058824f, 0.5490196f, 1f)@@
        public static Color tan { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8235295f, 0.7058824f, 0.5490196f, 1f); }

        // Color Preset of @@RGBA(0f, 0.5019608f, 0.5019608f, 1f)@@
        public static Color teal { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0f, 0.5019608f, 0.5019608f, 1f); }

        // Color Preset of @@RGBA(0.8470589f, 0.7490196f, 0.8470589f, 1f)@@
        public static Color thistle { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8470589f, 0.7490196f, 0.8470589f, 1f); }

        // Color Preset of @@RGBA(1f, 0.3882353f, 0.2784314f, 1f)@@
        public static Color tomato { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 0.3882353f, 0.2784314f, 1f); }

        // Color Preset of @@RGBA(0.2509804f, 0.8784314f, 0.8156863f, 1f)@@
        public static Color turquoise { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.2509804f, 0.8784314f, 0.8156863f, 1f); }

        // Color Preset of @@RGBA(0.9333334f, 0.509804f, 0.9333334f, 1f)@@
        public static Color violet { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9333334f, 0.509804f, 0.9333334f, 1f); }

        // Color Preset of @@RGBA(0.8156863f, 0.1254902f, 0.5647059f, 1f)@@
        public static Color violetRed { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.8156863f, 0.1254902f, 0.5647059f, 1f); }

        // Color Preset of @@RGBA(0.9607844f, 0.8705883f, 0.7019608f, 1f)@@
        public static Color wheat { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9607844f, 0.8705883f, 0.7019608f, 1f); }

        // Color Preset of @@RGBA(1f, 1f, 1f, 1f); }
        public static Color white { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 1f, 1f, 1f); }

        // Color Preset of @@RGBA(0.9607844f, 0.9607844f, 0.9607844f, 1f)@@
        public static Color whiteSmoke { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.9607844f, 0.9607844f, 0.9607844f, 1f); }

        // Color Preset of @@RGBA(1f, 0.92f, 0.016f, 1f)@@
        public static Color yellow { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(1f, 235f / 255f, 4f / 255f, 1f); }

        // Color Preset of @@RGBA(0.6039216f, 0.8039216f, 0.1960784f, 1f)@@
        public static Color yellowGreen { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(0.6039216f, 0.8039216f, 0.1960784f, 1f); }

        // Color Preset of @@RGBA(1f, 0.92f, 0.016f, 1f)@@
        public static Color yellowNice { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new (1f, 235f / 255f, 4f / 255f, 1f); }

        #endregion

        internal static Dictionary<Color, string> defaultColorNames => m_defaultColorNames ??= InitializeColorNames();
        static Dictionary<Color, string> m_defaultColorNames;
        static Dictionary<Color, string> InitializeColorNames()
        {
            // The order of this dictionary is the order that the colors will be filled into the color picker left->right row by row
            return new Dictionary<Color, string>
            {
                {red, nameof(red)},
                {green, nameof(green)},
                {blue, nameof(blue)},
                {yellow, nameof(yellow)},
                {cyan, nameof(cyan)},
                {magenta, nameof(magenta)},
                {gray1, nameof(gray1)},
                {gray2, nameof(gray2)},
                {gray3, nameof(gray3)},
                {gray4, nameof(gray4)},
                {gray5, nameof(gray5)},
                {gray6, nameof(gray6)},
                {gray7, nameof(gray7)},
                {gray8, nameof(gray8)},
                {gray9, nameof(gray9)},
                {white, nameof(white)},
                {whiteSmoke, nameof(whiteSmoke)},
                {gainsboro, nameof(gainsboro)},
                {lightGray, nameof(lightGray)},
                {silver, nameof(silver)},
                {darkGray, nameof(darkGray)},
                {dimGray, nameof(dimGray)},
                {black, nameof(black)},
                {darkRed, nameof(darkRed)},
                {brown, nameof(brown)},
                {firebrick, nameof(firebrick)},
                {crimson, nameof(crimson)},
                {softRed, nameof(softRed)},
                {indianRed, nameof(indianRed)},
                {violetRed, nameof(violetRed)},
                {mediumVioletRed, nameof(mediumVioletRed)},
                {deepPink, nameof(deepPink)},
                {hotPink, nameof(hotPink)},
                {lightPink, nameof(lightPink)},
                {pink, nameof(pink)},
                {paleVioletRed, nameof(paleVioletRed)},
                {maroon, nameof(maroon)},
                {rosyBrown, nameof(rosyBrown)},
                {lightCoral, nameof(lightCoral)},
                {salmon, nameof(salmon)},
                {tomato, nameof(tomato)},
                {darkSalmon, nameof(darkSalmon)},
                {coral, nameof(coral)},
                {orangeRed, nameof(orangeRed)},
                {lightSalmon, nameof(lightSalmon)},
                {sienna, nameof(sienna)},
                {saddleBrown, nameof(saddleBrown)},
                {chocolate, nameof(chocolate)},
                {sandyBrown, nameof(sandyBrown)},
                {peru, nameof(peru)},
                {darkOrange, nameof(darkOrange)},
                {burlywood, nameof(burlywood)},
                {tan, nameof(tan)},
                {moccasin, nameof(moccasin)},
                {peachPuff, nameof(peachPuff)},
                {bisque, nameof(bisque)},
                {navajoWhite, nameof(navajoWhite)},
                {wheat, nameof(wheat)},
                {orange, nameof(orange)},
                {darkGoldenRod, nameof(darkGoldenRod)},
                {goldenRod, nameof(goldenRod)},
                {lightGoldenRod, nameof(lightGoldenRod)},
                {gold, nameof(gold)},
                {softYellow, nameof(softYellow)},
                {lightGoldenRodYellow, nameof(lightGoldenRodYellow)},
                {beige, nameof(beige)},
                {lemonChiffon, nameof(lemonChiffon)},
                {lightYellow, nameof(lightYellow)},
                //{yellowNice, nameof(yellowNice)},
                {khaki, nameof(khaki)},
                {paleGoldenRod, nameof(paleGoldenRod)},
                {darkKhaki, nameof(darkKhaki)},
                {olive, nameof(olive)},
                {oliveDrab, nameof(oliveDrab)},
                {yellowGreen, nameof(yellowGreen)},
                {darkOliveGreen, nameof(darkOliveGreen)},
                {softGreen, nameof(softGreen)},
                {greenYellow, nameof(greenYellow)},
                {chartreuse, nameof(chartreuse)},
                {lawnGreen, nameof(lawnGreen)},
                {darkGreen, nameof(darkGreen)},
                {forestGreen, nameof(forestGreen)},
                {limeGreen, nameof(limeGreen)},
                {darkSeaGreen, nameof(darkSeaGreen)},
                {lightGreen, nameof(lightGreen)},
                {paleGreen, nameof(paleGreen)},
                {seaGreen, nameof(seaGreen)},
                {mediumSeaGreen, nameof(mediumSeaGreen)},
                {springGreen, nameof(springGreen)},
                {mediumSpringGreen, nameof(mediumSpringGreen)},
                {aquamarine, nameof(aquamarine)},
                {mediumAquamarine, nameof(mediumAquamarine)},
                {turquoise, nameof(turquoise)},
                {mediumTurquoise, nameof(mediumTurquoise)},
                {lightSeaGreen, nameof(lightSeaGreen)},
                {lightSlateGray, nameof(lightSlateGray)},
                {slateGray, nameof(slateGray)},
                {darkSlateGray, nameof(darkSlateGray)},
                {teal, nameof(teal)},
                {darkCyan, nameof(darkCyan)},
                {lightCyan, nameof(lightCyan)},
                {mintCream, nameof(mintCream)},
                {honeydew, nameof(honeydew)},
                {azure, nameof(azure)},
                {paleTurquoise, nameof(paleTurquoise)},
                {darkTurquoise, nameof(darkTurquoise)},
                {cadetBlue, nameof(cadetBlue)},
                {powderBlue, nameof(powderBlue)},
                {softBlue, nameof(softBlue)},
                {lightBlue, nameof(lightBlue)},
                {deepSkyBlue, nameof(deepSkyBlue)},
                {skyBlue, nameof(skyBlue)},
                {lightSkyBlue, nameof(lightSkyBlue)},
                {steelBlue, nameof(steelBlue)},
                {dodgerBlue, nameof(dodgerBlue)},
                {lightSteelBlue, nameof(lightSteelBlue)},
                {ghostWhite, nameof(ghostWhite)},
                {aliceBlue, nameof(aliceBlue)},
                {lavender, nameof(lavender)},
                {cornflowerBlue, nameof(cornflowerBlue)},
                {royalBlue, nameof(royalBlue)},
                {navyBlue, nameof(navyBlue)},
                {midnightBlue, nameof(midnightBlue)},
                {darkBlue, nameof(darkBlue)},
                {mediumBlue, nameof(mediumBlue)},
                {slateBlue, nameof(slateBlue)},
                {lightSlateBlue, nameof(lightSlateBlue)},
                {mediumSlateBlue, nameof(mediumSlateBlue)},
                {darkSlateBlue, nameof(darkSlateBlue)},
                {mediumPurple, nameof(mediumPurple)},
                {rebeccaPurple, nameof(rebeccaPurple)},
                {blueViolet, nameof(blueViolet)},
                {indigo, nameof(indigo)},
                {purple, nameof(purple)},
                {darkOrchid, nameof(darkOrchid)},
                {darkViolet, nameof(darkViolet)},
                {mediumOrchid, nameof(mediumOrchid)},
                {darkMagenta, nameof(darkMagenta)},
                {violet, nameof(violet)},
                {plum, nameof(plum)},
                {thistle, nameof(thistle)},
                {orchid, nameof(orchid)},
                {lavenderBlush, nameof(lavenderBlush)},
                {seashell, nameof(seashell)},
                {blanchedAlmond, nameof(blanchedAlmond)},
                {papayaWhip, nameof(papayaWhip)},
                {cornsilk, nameof(cornsilk)},
                {ivory, nameof(ivory)},
                {linen, nameof(linen)},
                {floralWhite, nameof(floralWhite)},
                {antiqueWhite, nameof(antiqueWhite)},
                {oldLace, nameof(oldLace)},
                {mistyRose, nameof(mistyRose)},
                {snow, nameof(snow)},
            };
        }
    }
} //namespace
