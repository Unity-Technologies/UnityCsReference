// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEngine.TextCore.Text
{
    public enum ColorGradientMode
    {
        Single,
        HorizontalGradient,
        VerticalGradient,
        FourCornersGradient
    }

    [System.Serializable][ExcludeFromPresetAttribute][ExcludeFromObjectFactory]
    [NativeHeader("Modules/TextCoreTextEngine/TextColorGradient.h")]
    public class TextColorGradient : ScriptableObject
    {
        public ColorGradientMode colorMode = ColorGradientMode.FourCornersGradient;

        public Color topLeft;
        public Color topRight;
        public Color bottomLeft;
        public Color bottomRight;

        const ColorGradientMode k_DefaultColorMode = ColorGradientMode.FourCornersGradient;
        static readonly Color k_DefaultColor = Color.white;

        void OnValidate()
        {
            MarkNativeDirty();
        }

        void OnDisable()
        {
            if (m_NativeInstance != IntPtr.Zero)
            {
                DestroyNative(m_NativeInstance, MarshalledUnityObject.MarshalNotNull(this));
                m_NativeInstance = IntPtr.Zero;
            }
        }


        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal IntPtr nativeInstance
        {
            get
            {
                if (m_NativeInstance == IntPtr.Zero)
                {
                    m_NativeInstance = CreateNative(topLeft, topRight, bottomLeft, bottomRight, MarshalledUnityObject.MarshalNotNull(this));
                }
                return m_NativeInstance;
            }
        }
        IntPtr m_NativeInstance = IntPtr.Zero;

        /// <summary>
        /// Default Constructor which sets each of the colors as white.
        /// </summary>
        public TextColorGradient()
        {
            colorMode = k_DefaultColorMode;
            topLeft = k_DefaultColor;
            topRight = k_DefaultColor;
            bottomLeft = k_DefaultColor;
            bottomRight = k_DefaultColor;
        }

        /// <summary>
        /// Constructor allowing to set the default color of the Color Gradient.
        /// </summary>
        /// <param name="color"></param>
        public TextColorGradient(Color color)
        {
            colorMode = k_DefaultColorMode;
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
        public TextColorGradient(Color color0, Color color1, Color color2, Color color3)
        {
            colorMode = k_DefaultColorMode;
            this.topLeft = color0;
            this.topRight = color1;
            this.bottomLeft = color2;
            this.bottomRight = color3;
        }

        internal void MarkNativeDirty()
        {
            if (m_NativeInstance != IntPtr.Zero)
                UpdateNative(m_NativeInstance, topLeft, topRight, bottomLeft, bottomRight);
        }

        static extern IntPtr CreateNative(Color32 tl, Color32 tr, Color32 bl, Color32 br, IntPtr managedObject);
        static extern void DestroyNative(IntPtr nativeInstance, IntPtr managedObject);
        static extern void UpdateNative(IntPtr instance, Color32 tl, Color32 tr, Color32 bl, Color32 br);
    }
}
