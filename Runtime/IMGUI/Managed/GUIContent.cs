// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // The contents of a GUI element.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public class GUIContent
    {
        // MUST MATCH MEMORY LAYOUT IN GUICONTENT.CPP
        [SerializeField]
        string m_Text = string.Empty;
        [SerializeField]
        Texture m_Image;
        [SerializeField]
        string m_Tooltip = string.Empty;

        private static readonly GUIContent s_Text      = new GUIContent();
        private static readonly GUIContent s_Image     = new GUIContent();
        private static readonly GUIContent s_TextImage = new GUIContent();

        // The text contained.
        public string text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        // The icon image contained.
        public Texture image
        {
            get { return m_Image; }
            set { m_Image = value; }
        }

        // The tooltip of this element.
        public string tooltip
        {
            get { return m_Tooltip; }
            set { m_Tooltip = value; }
        }

        // Constructor for GUIContent in all shapes and sizes
        public GUIContent() {}

        // Build a GUIContent object containing only text.
        public GUIContent(string text) :
            this(text, null, string.Empty)
        {}

        // Build a GUIContent object containing only an image.
        public GUIContent(Texture image) :
            this(string.Empty, image, string.Empty)
        {}

        // Build a GUIContent object containing both /text/ and an image.
        public GUIContent(string text, Texture image) :
            this(text, image, string.Empty)
        {}

        // Build a GUIContent containing some /text/. When the user hovers the mouse over it, the global GUI::ref::tooltip is set to the /tooltip/.
        public GUIContent(string text, string tooltip) :
            this(text, null, tooltip)
        {}

        // Build a GUIContent containing an image. When the user hovers the mouse over it, the global GUI::ref::tooltip is set to the /tooltip/.
        public GUIContent(Texture image, string tooltip) :
            this(string.Empty, image, tooltip)
        {}

        // Build a GUIContent that contains both /text/, an /image/ and has a /tooltip/ defined. When the user hovers the mouse over it, the global GUI::ref::tooltip is set to the /tooltip/.
        public GUIContent(string text, Texture image, string tooltip)
        {
            this.text = text;
            this.image = image;
            this.tooltip = tooltip;
        }

        // Build a GUIContent as a copy of another GUIContent.
        public GUIContent(GUIContent src)
        {
            text = src.m_Text;
            image = src.m_Image;
            tooltip = src.m_Tooltip;
        }

        // Shorthand for empty content.
        public static GUIContent none = new GUIContent("");

        // *undocumented*
        internal int hash
        {
            get
            {
                int h = 0;
                if (!string.IsNullOrEmpty(m_Text))
                    h = m_Text.GetHashCode() * 37;
                return h;
            }
        }

        internal static GUIContent Temp(string t)
        {
            s_Text.m_Text = t;
            s_Text.m_Tooltip = string.Empty;
            return s_Text;
        }

        internal static GUIContent Temp(string t, string tooltip)
        {
            s_Text.m_Text = t;
            s_Text.m_Tooltip = tooltip;
            return s_Text;
        }

        internal static GUIContent Temp(Texture i)
        {
            s_Image.m_Image = i;
            s_Image.m_Tooltip = string.Empty;
            return s_Image;
        }

        internal static GUIContent Temp(Texture i, string tooltip)
        {
            s_Image.m_Image = i;
            s_Image.m_Tooltip = tooltip;
            return s_Image;
        }

        internal static GUIContent Temp(string t, Texture i)
        {
            s_TextImage.m_Text = t;
            s_TextImage.m_Image = i;
            return s_TextImage;
        }

        internal static void ClearStaticCache()
        {
            s_Text.m_Text = null;
            s_Text.m_Tooltip = string.Empty;
            s_Image.m_Image = null;
            s_Image.m_Tooltip = string.Empty;
            s_TextImage.m_Text = null;
            s_TextImage.m_Image = null;
        }

        internal static GUIContent[] Temp(string[] texts)
        {
            GUIContent[] retval = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                retval[i] = new GUIContent(texts[i]);
            }
            return retval;
        }

        internal static GUIContent[] Temp(Texture[] images)
        {
            GUIContent[] retval = new GUIContent[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                retval[i] = new GUIContent(images[i]);
            }
            return retval;
        }
    }
}
