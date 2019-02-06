// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.TextCore
{
    enum ColorMode
    {
        Single,
        HorizontalGradient,
        VerticalGradient,
        FourCornersGradient
    }

    [System.Serializable]
    class TextGradientPreset : ScriptableObject
    {
        public ColorMode colorMode;

        public Color topLeft;
        public Color topRight;
        public Color bottomLeft;
        public Color bottomRight;

        /// <summary>
        /// Default Constructor which sets each of the colors as white.
        /// </summary>
        public TextGradientPreset()
        {
            colorMode = ColorMode.FourCornersGradient;
            topLeft = Color.white;
            topRight = Color.white;
            bottomLeft = Color.white;
            bottomRight = Color.white;
        }

        /// <summary>
        /// Constructor allowing to set the default color of the Color Gradient.
        /// </summary>
        /// <param name="color"></param>
        public TextGradientPreset(Color color)
        {
            colorMode = ColorMode.FourCornersGradient;
            topLeft = color;
            topRight = color;
            bottomLeft = color;
            bottomRight = color;
        }

        /// <summary>
        /// The vertex colors at the corners of the characters.
        /// </summary>
        /// <param name="color0">Top left color.</param>
        /// <param name="color1">Top right color.</param>
        /// <param name="color2">Bottom left color.</param>
        /// <param name="color3">Bottom right color.</param>
        public TextGradientPreset(Color color0, Color color1, Color color2, Color color3)
        {
            colorMode = ColorMode.FourCornersGradient;
            topLeft = color0;
            topRight = color1;
            bottomLeft = color2;
            bottomRight = color3;
        }
    }
}
