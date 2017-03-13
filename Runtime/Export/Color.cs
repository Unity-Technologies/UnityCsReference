// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Representation of RGBA colors.

    [UsedByNativeCode]
    public struct Color
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
        override public string ToString()
        {
            return UnityString.Format("RGBA({0:F3}, {1:F3}, {2:F3}, {3:F3})", r, g, b, a);
        }

        // Returns a nicely formatted string of this color.
        public string ToString(string format)
        {
            return UnityString.Format("RGBA({0}, {1}, {2}, {3})", r.ToString(format), g.ToString(format), b.ToString(format), a.ToString(format));
        }

        // used to allow Colors to be used as keys in hash tables
        public override int GetHashCode()
        {
            return ((Vector4)this).GetHashCode();
        }

        // also required for being able to use Colors as keys in hash tables
        public override bool Equals(object other)
        {
            if (!(other is Color)) return false;
            Color rhs = (Color)other;
            return r.Equals(rhs.r) && g.Equals(rhs.g) && b.Equals(rhs.b) && a.Equals(rhs.a);
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
        internal Color RGBMultiplied(float multiplier) { return new Color(r * multiplier, g * multiplier, b * multiplier, a); }
        // Returns new color that has RGB components multiplied, but leaving alpha untouched.
        internal Color AlphaMultiplied(float multiplier) { return new Color(r, g, b, a * multiplier); }
        // Returns new color that has RGB components multiplied, but leaving alpha untouched.
        internal Color RGBMultiplied(Color multiplier) { return new Color(r * multiplier.r, g * multiplier.g, b * multiplier.b, a); }

        // Solid red. RGBA is (1, 0, 0, 1).
        public static Color red { get { return new Color(1F, 0F, 0F, 1F); } }
        // Solid green. RGBA is (0, 1, 0, 1).
        public static Color green { get { return new Color(0F, 1F, 0F, 1F); } }
        // Solid blue. RGBA is (0, 0, 1, 1).
        public static Color blue { get { return new Color(0F, 0F, 1F, 1F); } }
        // Solid white. RGBA is (1, 1, 1, 1).
        public static Color white { get { return new Color(1F, 1F, 1F, 1F); } }
        // Solid black. RGBA is (0, 0, 0, 1).
        public static Color black { get { return new Color(0F, 0F, 0F, 1F); } }
        // Yellow. RGBA is (1, 0.92, 0.016, 1), but the color is nice to look at!
        public static Color yellow { get { return new Color(1F, 235F / 255F, 4F / 255F, 1F); } }
        // Cyan. RGBA is (0, 1, 1, 1).
        public static Color cyan { get { return new Color(0F, 1F, 1F, 1F); } }
        // Magenta. RGBA is (1, 0, 1, 1).
        public static Color magenta { get { return new Color(1F, 0F, 1F, 1F); } }
        // Gray. RGBA is (0.5, 0.5, 0.5, 1).
        public static Color gray { get { return new Color(.5F, .5F, .5F, 1F); } }
        // English spelling for ::ref::gray. RGBA is the same (0.5, 0.5, 0.5, 1).
        public static Color grey { get { return new Color(.5F, .5F, .5F, 1F); } }
        // Completely transparent. RGBA is (0, 0, 0, 0).
        public static Color clear { get { return new Color(0F, 0F, 0F, 0F); } }

        // The grayscale value of the color (RO)
        public float grayscale { get { return 0.299F * r + 0.587F * g + 0.114F * b; } }

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
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
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
                        throw new IndexOutOfRangeException("Invalid Vector3 index!");
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
            Color retval = Color.white;
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
    }
} //namespace
