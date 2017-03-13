// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    public sealed partial class Random
    {
        public static Color ColorHSV()
        {
            return ColorHSV(0f, 1f, 0f, 1f, 0f, 1f, 1f, 1f);
        }

        public static Color ColorHSV(float hueMin, float hueMax)
        {
            return ColorHSV(hueMin, hueMax, 0f, 1f, 0f, 1f, 1f, 1f);
        }

        public static Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax)
        {
            return ColorHSV(hueMin, hueMax, saturationMin, saturationMax, 0f, 1f, 1f, 1f);
        }

        public static Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax)
        {
            return ColorHSV(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax, 1f, 1f);
        }

        public static Color ColorHSV(float hueMin, float hueMax, float saturationMin, float saturationMax, float valueMin, float valueMax, float alphaMin, float alphaMax)
        {
            var h = Mathf.Lerp(hueMin, hueMax, Random.value);
            var s = Mathf.Lerp(saturationMin, saturationMax, Random.value);
            var v = Mathf.Lerp(valueMin, valueMax, Random.value);
            var color = Color.HSVToRGB(h, s, v, true);
            color.a = Mathf.Lerp(alphaMin, alphaMax, Random.value);
            return color;
        }
    }
}
