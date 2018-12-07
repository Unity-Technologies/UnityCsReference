// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Which platform to emulate.
    internal enum PlatformSelection
    {
        // The behaviour matches the platform the end user is running on.
        Native = 0,
        // The behaviour matches a Mac OS X machine.
        Mac = 1,
        // The behaviour matches a Windows machine.
        Windows = 2,
    }

    // General settings for how the GUI behaves
    [Serializable]
    public sealed partial class GUISettings
    {
        // Should double-clicking select words in text fields.
        public bool doubleClickSelectsWord { get { return m_DoubleClickSelectsWord; } set { m_DoubleClickSelectsWord = value; } }
        [SerializeField]
        bool m_DoubleClickSelectsWord = true;

        // Should triple-clicking select whole text in text fields.
        public bool tripleClickSelectsLine { get { return m_TripleClickSelectsLine; } set { m_TripleClickSelectsLine = value; } }
        [SerializeField]
        bool m_TripleClickSelectsLine = true;

        // The color of the cursor in text fields.
        public Color cursorColor { get { return m_CursorColor; } set { m_CursorColor = value; } }
        [SerializeField]
        Color m_CursorColor = Color.white;

        //  The speed of text field cursor flashes.
        public float cursorFlashSpeed
        {
            get
            {
                if (m_CursorFlashSpeed >= 0)
                    return m_CursorFlashSpeed;
                else
                {
                    return Internal_GetCursorFlashSpeed();
                }
            }
            set { m_CursorFlashSpeed = value; }
        }

        [SerializeField]
        float m_CursorFlashSpeed = -1;

        // The color of the selection rect in text fields.
        public Color selectionColor { get { return m_SelectionColor; } set { m_SelectionColor = value; } }
        [SerializeField]
        Color m_SelectionColor = new Color(.5f, .5f, 1f);
    }

    // Defines how GUI looks and behaves.
    [Serializable]
    [ExecuteInEditMode]
    [RequiredByNativeCode]
    public sealed class GUISkin : ScriptableObject
    {
        [SerializeField]
        Font m_Font;

        // *undocumented*
        public GUISkin()
        {
            m_CustomStyles = new GUIStyle[1];
        }

        internal void OnEnable()
        {
            Apply();
        }

        static internal void CleanupRoots()
        {
            // See GUI.CleanupRoots
            current = null;
            ms_Error = null;
        }

        // The default font to use for all styles.
        public Font font { get { return m_Font; } set { m_Font = value; if (current == this) GUIStyle.SetDefaultFont(m_Font); Apply(); } }

        [SerializeField]  //yes the attribute applies to all fields on the line below.
        GUIStyle m_box, m_button, m_toggle, m_label, m_textField, m_textArea, m_window;

        // Style used by default for GUI::ref::Box controls.
        public GUIStyle box { get { return m_box; } set { m_box = value; Apply(); } }

        // Style used by default for GUI::ref::Label controls.
        public GUIStyle label { get { return m_label; } set { m_label = value; Apply(); } }

        // Style used by default for GUI::ref::TextField controls.
        public GUIStyle textField { get { return m_textField; } set { m_textField = value; Apply(); } }

        // Style used by default for GUI::ref::TextArea controls.
        public GUIStyle textArea { get { return m_textArea; } set { m_textArea = value; Apply(); } }

        // Style used by default for GUI::ref::Button controls.
        public GUIStyle button { get { return m_button; } set { m_button = value; Apply(); } }

        // Style used by default for GUI::ref::Toggle controls.
        public GUIStyle toggle { get { return m_toggle; } set { m_toggle = value; Apply(); } }

        // Style used by default for Window controls (SA GUI::ref::Window).
        public GUIStyle window { get { return m_window; } set { m_window = value; Apply(); } }

        [SerializeField]
        GUIStyle m_horizontalSlider;
        [SerializeField]
        GUIStyle m_horizontalSliderThumb;
        [SerializeField]
        GUIStyle m_verticalSlider;
        [SerializeField]
        GUIStyle m_verticalSliderThumb;

        // Style used by default for the background part of GUI::ref::HorizontalSlider controls.
        public GUIStyle horizontalSlider { get { return m_horizontalSlider; } set { m_horizontalSlider = value; Apply(); } }

        // Style used by default for the thumb that is dragged in GUI::ref::HorizontalSlider controls.
        public GUIStyle horizontalSliderThumb { get { return m_horizontalSliderThumb; } set { m_horizontalSliderThumb = value; Apply(); } }

        // Style used by default for the background part of GUI::ref::VerticalSlider controls.
        public GUIStyle verticalSlider { get { return m_verticalSlider; } set { m_verticalSlider = value; Apply(); } }

        // Style used by default for the thumb that is dragged in GUI::ref::VerticalSlider controls.
        public GUIStyle verticalSliderThumb { get { return m_verticalSliderThumb; } set { m_verticalSliderThumb = value; Apply(); } }

        [SerializeField]
        GUIStyle m_horizontalScrollbar;
        [SerializeField]
        GUIStyle m_horizontalScrollbarThumb;
        [SerializeField]
        GUIStyle m_horizontalScrollbarLeftButton;
        [SerializeField]
        GUIStyle m_horizontalScrollbarRightButton;

        // Style used by default for the background part of GUI::ref::HorizontalScrollbar controls.
        public GUIStyle horizontalScrollbar { get { return m_horizontalScrollbar; } set { m_horizontalScrollbar = value; Apply(); } }
        // Style used by default for the thumb that is dragged in GUI::ref::HorizontalScrollbar controls.
        public GUIStyle horizontalScrollbarThumb { get { return m_horizontalScrollbarThumb; } set { m_horizontalScrollbarThumb = value; Apply(); } }
        // Style used by default for the left button on GUI::ref::HorizontalScrollbar controls.
        public GUIStyle horizontalScrollbarLeftButton { get { return m_horizontalScrollbarLeftButton; } set { m_horizontalScrollbarLeftButton = value; Apply(); } }
        // Style used by default for the right button on GUI::ref::HorizontalScrollbar controls.
        public GUIStyle horizontalScrollbarRightButton { get { return m_horizontalScrollbarRightButton; } set { m_horizontalScrollbarRightButton = value; Apply(); } }

        [SerializeField]
        GUIStyle m_verticalScrollbar;
        [SerializeField]
        GUIStyle m_verticalScrollbarThumb;
        [SerializeField]
        GUIStyle m_verticalScrollbarUpButton;
        [SerializeField]
        GUIStyle m_verticalScrollbarDownButton;

        // Style used by default for the background part of GUI::ref::VerticalScrollbar controls.
        public GUIStyle verticalScrollbar { get { return m_verticalScrollbar; } set { m_verticalScrollbar = value; Apply(); } }
        // Style used by default for the thumb that is dragged in GUI::ref::VerticalScrollbar controls.
        public GUIStyle verticalScrollbarThumb { get { return m_verticalScrollbarThumb; } set { m_verticalScrollbarThumb = value; Apply(); } }
        // Style used by default for the up button on GUI::ref::VerticalScrollbar controls.
        public GUIStyle verticalScrollbarUpButton { get { return m_verticalScrollbarUpButton; } set { m_verticalScrollbarUpButton = value; Apply(); } }
        // Style used by default for the down button on GUI::ref::VerticalScrollbar controls.
        public GUIStyle verticalScrollbarDownButton { get { return m_verticalScrollbarDownButton; } set { m_verticalScrollbarDownButton = value; Apply(); } }

        // Background style for scroll views.
        [SerializeField]
        GUIStyle m_ScrollView;

        // Style used by default for the background of ScrollView controls (see GUI::ref::BeginScrollView).
        public GUIStyle scrollView { get { return m_ScrollView; } set { m_ScrollView = value; Apply(); } }

        [SerializeField]
        internal GUIStyle[] m_CustomStyles;

        // Array of GUI styles for specific needs.
        public GUIStyle[] customStyles { get { return m_CustomStyles; } set { m_CustomStyles = value; Apply(); } }


        [SerializeField]
        private GUISettings m_Settings = new GUISettings();

        // Generic settings for how controls should behave with this skin.
        public GUISettings settings { get { return m_Settings; } }

        internal static GUIStyle ms_Error;

        internal static GUIStyle error
        {
            get
            {
                if (ms_Error == null)
                {
                    ms_Error = new GUIStyle();
                    ms_Error.name = "StyleNotFoundError";
                }
                return ms_Error;
            }
        }

        private Dictionary<string, GUIStyle> m_Styles = null;

        internal void Apply()
        {
            if (m_CustomStyles == null)
                Debug.Log("custom styles is null");

            BuildStyleCache();
        }

        private void BuildStyleCache()
        {
            if (m_box == null) m_box = new GUIStyle();
            if (m_button == null) m_button = new GUIStyle();
            if (m_toggle == null) m_toggle = new GUIStyle();
            if (m_label == null) m_label = new GUIStyle();
            if (m_window == null) m_window = new GUIStyle();
            if (m_textField == null) m_textField = new GUIStyle();
            if (m_textArea == null) m_textArea = new GUIStyle();
            if (m_horizontalSlider == null) m_horizontalSlider = new GUIStyle();
            if (m_horizontalSliderThumb == null) m_horizontalSliderThumb = new GUIStyle();
            if (m_verticalSlider == null) m_verticalSlider = new GUIStyle();
            if (m_verticalSliderThumb == null) m_verticalSliderThumb = new GUIStyle();
            if (m_horizontalScrollbar == null) m_horizontalScrollbar = new GUIStyle();
            if (m_horizontalScrollbarThumb == null) m_horizontalScrollbarThumb = new GUIStyle();
            if (m_horizontalScrollbarLeftButton == null) m_horizontalScrollbarLeftButton = new GUIStyle();
            if (m_horizontalScrollbarRightButton == null) m_horizontalScrollbarRightButton = new GUIStyle();
            if (m_verticalScrollbar == null) m_verticalScrollbar = new GUIStyle();
            if (m_verticalScrollbarThumb == null) m_verticalScrollbarThumb = new GUIStyle();
            if (m_verticalScrollbarUpButton == null) m_verticalScrollbarUpButton = new GUIStyle();
            if (m_verticalScrollbarDownButton == null) m_verticalScrollbarDownButton = new GUIStyle();
            if (m_ScrollView == null) m_ScrollView = new GUIStyle();

            m_Styles = new Dictionary<string, GUIStyle>(StringComparer.OrdinalIgnoreCase);

            m_Styles["box"] = m_box;
            m_box.name = "box";

            m_Styles["button"] = m_button;
            m_button.name = "button";

            m_Styles["toggle"] = m_toggle;
            m_toggle.name = "toggle";

            m_Styles["label"] = m_label;
            m_label.name = "label";

            m_Styles["window"] = m_window;
            m_window.name = "window";

            m_Styles["textfield"] = m_textField;
            m_textField.name = "textfield";

            m_Styles["textarea"] = m_textArea;
            m_textArea.name = "textarea";


            m_Styles["horizontalslider"] = m_horizontalSlider;
            m_horizontalSlider.name = "horizontalslider";

            m_Styles["horizontalsliderthumb"] = m_horizontalSliderThumb;
            m_horizontalSliderThumb.name = "horizontalsliderthumb";

            m_Styles["verticalslider"] = m_verticalSlider;
            m_verticalSlider.name = "verticalslider";

            m_Styles["verticalsliderthumb"] = m_verticalSliderThumb;
            m_verticalSliderThumb.name = "verticalsliderthumb";

            m_Styles["horizontalscrollbar"] = m_horizontalScrollbar;
            m_horizontalScrollbar.name = "horizontalscrollbar";

            m_Styles["horizontalscrollbarthumb"] = m_horizontalScrollbarThumb;
            m_horizontalScrollbarThumb.name = "horizontalscrollbarthumb";

            m_Styles["horizontalscrollbarleftbutton"] = m_horizontalScrollbarLeftButton;
            m_horizontalScrollbarLeftButton.name = "horizontalscrollbarleftbutton";

            m_Styles["horizontalscrollbarrightbutton"] = m_horizontalScrollbarRightButton;
            m_horizontalScrollbarRightButton.name = "horizontalscrollbarrightbutton";

            m_Styles["verticalscrollbar"] = m_verticalScrollbar;
            m_verticalScrollbar.name = "verticalscrollbar";

            m_Styles["verticalscrollbarthumb"] = m_verticalScrollbarThumb;
            m_verticalScrollbarThumb.name = "verticalscrollbarthumb";

            m_Styles["verticalscrollbarupbutton"] = m_verticalScrollbarUpButton;
            m_verticalScrollbarUpButton.name = "verticalscrollbarupbutton";

            m_Styles["verticalscrollbardownbutton"] = m_verticalScrollbarDownButton;
            m_verticalScrollbarDownButton.name = "verticalscrollbardownbutton";

            m_Styles["scrollview"] = m_ScrollView;
            m_ScrollView.name = "scrollview";

            if (m_CustomStyles != null)
            {
                for (int i = 0; i < m_CustomStyles.Length; i++)
                {
                    if (m_CustomStyles[i] == null)
                        continue;
                    m_Styles[m_CustomStyles[i].name] = m_CustomStyles[i];
                }
            }
            error.stretchHeight = true;
            error.normal.textColor = Color.red;
        }

        // Get a named [[GUIStyle]].
        public GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = FindStyle(styleName);
            if (s != null)
                return s;
            Debug.LogWarning("Unable to find style '" + styleName + "' in skin '" + name + "' " + (Event.current != null ? Event.current.type.ToString() : "<called outside OnGUI>"));
            return error;
        }

        // Try to search for a [[GUIStyle]]. This functions returns NULL and does not give an error.
        public GUIStyle FindStyle(string styleName)
        {
            if (this == null)
            {
                Debug.LogError("GUISkin is NULL");
                return null;
            }
            if (m_Styles == null)
                BuildStyleCache();

            GUIStyle style;
            if (m_Styles.TryGetValue(styleName, out style))
                return style;

            return null;
        }

        internal delegate void SkinChangedDelegate();
        internal static SkinChangedDelegate m_SkinChanged;

        // Make this the current skin used by the GUI
        static internal GUISkin current;
        internal void MakeCurrent()
        {
            current = this;
            GUIStyle.SetDefaultFont(font);
            if (m_SkinChanged != null)
                m_SkinChanged();
        }

        //*undocumented* Documented separately
        public IEnumerator GetEnumerator()
        {
            if (m_Styles == null)
                BuildStyleCache();
            return m_Styles.Values.GetEnumerator();
        }
    }
}
