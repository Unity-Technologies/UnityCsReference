// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    ///<summary>The contents of a GUI element.</summary>
    ///<remarks>This works closely in relation with <see cref="GUIStyle" />. GUIContent defines <c>what</c> to render and <see cref="GUIStyle" /> defines <c>how</c> to render it.</remarks>
    ///<seealso cref="GUIStyle" />
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [NativeHeader("Modules/IMGUI/GUIContent.h")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    public class GUIContent
    {
        // MUST MATCH MEMORY LAYOUT IN GUICONTENT.CPP
        [SerializeField]
        string m_Text = string.Empty;
        [SerializeField]
        Texture m_Image;
        [SerializeField]
        string m_Tooltip = string.Empty;
        [SerializeField]
        string m_TextWithWhitespace = string.Empty;

        internal event Action OnTextChanged;

        private static readonly GUIContent s_Text      = new GUIContent();
        private static readonly GUIContent s_Image     = new GUIContent();
        private static readonly GUIContent s_TextImage = new GUIContent();

        internal static readonly string k_ZeroWidthSpace = "\u200B";

        ///<summary>The text contained.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Button(new Rect(0, 0, 100, 20), new GUIContent("Click me!"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public string text
        {
            get { return m_Text; }
            set
            {
                if (m_Text == value)
                    return;

                m_Text = value;
                textWithWhitespace = value;
                OnTextChanged?.Invoke();
            }
        }

        internal string textWithWhitespace
        {
            get
            {
                return string.IsNullOrEmpty(m_TextWithWhitespace) ? k_ZeroWidthSpace : m_TextWithWhitespace;
            }
            set =>

                //The NoWidthSpace unicode is added at the end of the string to make sure LineFeeds update the layout of the text.
                m_TextWithWhitespace = value + k_ZeroWidthSpace;
        }

        internal void SetTextWithoutNotify(string value)
        {
            m_Text = value;
            textWithWhitespace = value;
        }

        ///<summary>The icon image contained.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Texture icon;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!icon)
        ///        {
        ///            Debug.LogError("Add a texture on the inspector first");
        ///            return;
        ///        }
        ///        GUI.Button(new Rect(0, 0, 100, 20), new GUIContent(icon));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Texture image
        {
            get { return m_Image; }
            set { m_Image = value; }
        }

        ///<summary>The tooltip of this element.</summary>
        ///<remarks>The tooltip associated with this content. Read GUItooltip to get the tooltip of the gui element the user is currently over.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Button(new Rect(0, 0, 100, 20), new GUIContent("A Button", "This is the tooltip"));
        ///        // If the user hovers the mouse over the button, the global tooltip gets set
        ///        GUI.Label(new Rect(0, 40, 100, 40), GUI.tooltip);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public string tooltip
        {
            get { return m_Tooltip; }
            set { m_Tooltip = value; }
        }

        ///<summary>Constructor for GUIContent in all shapes and sizes.</summary>
        ///<remarks>Build an empty GUIContent.</remarks>
        public GUIContent() {}

        ///<summary>Build a GUIContent object containing only text.</summary>
        ///<remarks>When using the GUI, you don't need to create GUIContents for simple text strings - these two lines of code are functionally equivalent:</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Button(new Rect(0, 0, 100, 20), "Click Me");
        ///        GUI.Button(new Rect(0, 30, 100, 20), new GUIContent("Click Me"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIContent(string text) :
            this(text, null, string.Empty)
        {}

        ///<summary>Build a GUIContent object containing only an image.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Texture icon;
        ///    void OnGUI()
        ///    {
        ///        GUI.Button(new Rect(0, 30, 100, 20), new GUIContent(icon));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIContent(Texture image) :
            this(string.Empty, image, string.Empty)
        {}

        ///<summary>Build a GUIContent object containing both <c>text</c> and an image.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Texture icon;
        ///    void OnGUI()
        ///    {
        ///        GUI.Button(new Rect(0, 30, 100, 20), new GUIContent("Click me", icon));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIContent(string text, Texture image) :
            this(text, image, string.Empty)
        {}

        ///<summary>Build a GUIContent containing some <c>text</c>. When the user hovers the mouse over it, the global <see cref="GUI.tooltip" /> is set to the <c>tooltip</c>.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Button(new Rect(0, 0, 100, 20), new GUIContent("Click me", "This is the tooltip"));
        ///
        ///        // If the user hovers the mouse over the button, the global tooltip gets set
        ///        GUI.Label(new Rect(0, 40, 100, 40), GUI.tooltip);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIContent(string text, string tooltip) :
            this(text, null, tooltip)
        {}

        ///<summary>Build a GUIContent containing an image. When the user hovers the mouse over it, the global <see cref="GUI.tooltip" /> is set to the <c>tooltip</c>.</summary>
        public GUIContent(Texture image, string tooltip) :
            this(string.Empty, image, tooltip)
        {}

        ///<summary>Build a GUIContent that contains both <c>text</c>, an <c>image</c> and has a <c>tooltip</c> defined. When the user hovers the mouse over it, the global <see cref="GUI.tooltip" /> is set to the <c>tooltip</c>.</summary>
        public GUIContent(string text, Texture image, string tooltip)
        {
            this.text = text;
            this.image = image;
            this.tooltip = tooltip;
        }

        ///<summary>Build a GUIContent as a copy of another GUIContent.</summary>
        public GUIContent(GUIContent src)
        {
            text = src.m_Text;
            image = src.m_Image;
            tooltip = src.m_Tooltip;
        }

        ///<summary>Shorthand for empty content.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Button(new Rect(0, 0, 100, 20), GUIContent.none);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NoAutoStaticsCleanup] // static sentinel for empty content; no user types, safe to persist
        public static GUIContent none = new GUIContent("");

        // *undocumented*
        ///<exclude />
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
            s_Text.textWithWhitespace = t;
            s_Text.m_Tooltip = string.Empty;
            return s_Text;
        }

        internal static GUIContent Temp(string t, string tooltip)
        {
            s_Text.m_Text = t;
            s_Text.textWithWhitespace = t;
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
            s_Text.textWithWhitespace = t;
            s_TextImage.m_Image = i;
            return s_TextImage;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void ClearStaticCache()
        {
            s_Text.m_Text = null;
            s_Text.m_TextWithWhitespace = null;
            s_Text.m_Tooltip = string.Empty;
            s_Image.m_Image = null;
            s_Image.m_Tooltip = string.Empty;
            s_Image.m_TextWithWhitespace = null;
            s_TextImage.m_Text = null;
            s_TextImage.m_Image = null;
            s_TextImage.m_TextWithWhitespace = null;
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

        public override string ToString()
        {
            return text ?? tooltip ?? base.ToString();
        }
    }
}
