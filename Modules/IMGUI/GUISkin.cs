// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
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
        ///<summary>Should double-clicking select words in text fields.</summary>
        ///<remarks>By default is set to true.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    string str = "This is a string with \n two lines of text";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.settings.doubleClickSelectsWord = false;
        ///        str = GUILayout.TextArea(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool doubleClickSelectsWord { get { return m_DoubleClickSelectsWord; } set { m_DoubleClickSelectsWord = value; } }
        [SerializeField]
        bool m_DoubleClickSelectsWord = true;

        ///<summary>Should triple-clicking select whole text in text fields.</summary>
        ///<remarks>Bu default is set to true.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Disables line selecting with triple click on the text area
        ///    string str = "This is a string with \n two lines of text";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.settings.tripleClickSelectsLine = false;
        ///        str = GUILayout.TextArea(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool tripleClickSelectsLine { get { return m_TripleClickSelectsLine; } set { m_TripleClickSelectsLine = value; } }
        [SerializeField]
        bool m_TripleClickSelectsLine = true;

        ///<summary>The color of the cursor in text fields.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set the cursor color to Cyan.
        ///    string str = "This is a string with \n two lines of text";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.settings.cursorColor = Color.cyan;
        ///        str = GUILayout.TextArea(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Color cursorColor { get { return m_CursorColor; } set { m_CursorColor = value; } }
        [SerializeField]
        Color m_CursorColor = Color.white;

        ///<summary>The speed of text field cursor flashes.</summary>
        ///<remarks>This is how many flashes / second. If you set it to 0, flashing will be disabled. If you set it to -1, the flashing speed will match the system default of the end user.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    string str = "This is a string with \n two lines of text";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.settings.cursorFlashSpeed = 3;
        ///        str = GUILayout.TextArea(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>The color of the selection rect in text fields.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    string str = "This is a string with \n two lines of text";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.settings.selectionColor = Color.cyan;
        ///        str = GUILayout.TextArea(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Color selectionColor { get { return m_SelectionColor; } set { m_SelectionColor = value; } }
        [SerializeField]
        Color m_SelectionColor = new Color(.5f, .5f, 1f);
    }

    ///<summary>Defines how GUI looks and behaves.</summary>
    ///<remarks>GUISkin contains GUI settings and a collection of <see cref="GUIStyle" /> objects that together specify GUI skin.
    ///
    ///Active GUI skin is get and set through <see cref="GUI.skin" />.</remarks>
    [Serializable]
    [ExecuteInEditMode]
    [RequiredByNativeCode]
    [AssetFileNameExtension("guiskin")]
    public sealed partial class GUISkin : ScriptableObject
    {
        [SerializeField]
        Font m_Font;

        // *undocumented*
        ///<exclude />
        public GUISkin()
        {
            m_CustomStyles = new GUIStyle[1];
        }

        internal void OnEnable()
        {
            Apply();
        }

        ///<summary>The default font to use for all styles.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies the font only of the current GUISkin.
        ///
        ///    public Font font;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!font)
        ///        {
        ///            Debug.LogError("No font found, assign one in the inspector.");
        ///            return;
        ///        }
        ///        GUI.skin.font = font;
        ///
        ///        GUILayout.Label("This is a label with the font");
        ///        GUILayout.Button("And this is a button");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Font font { get { return m_Font; } set { m_Font = value; if (current == this) GUIStyle.SetDefaultFont(m_Font); Apply(); } }

        [SerializeField]  //yes the attribute applies to all fields on the line below.
        GUIStyle m_box, m_button, m_toggle, m_label, m_textField, m_textArea, m_window;

        ///<summary>Style used by default for <see cref="GUI.Box" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the box style of the current GUISkin
        ///
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.box = style;
        ///        GUILayout.Box("This is a box.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle box { get { return m_box; } set { m_box = value; Apply(); } }

        ///<summary>Style used by default for <see cref="GUI.Label" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the label style of the current GUISkin
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.label = style;
        ///        GUILayout.Label("This is a label.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle label { get { return m_label; } set { m_label = value; Apply(); } }

        ///<summary>Style used by default for <see cref="GUI.TextField" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the textField style of the current GUISkin
        ///
        ///    GUIStyle style;
        ///    string str = "A string...";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.textField = style;
        ///        str = GUILayout.TextField(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle textField { get { return m_textField; } set { m_textField = value; Apply(); } }

        ///<summary>Style used by default for <see cref="GUI.TextArea" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the textArea style of the current GUISkin
        ///
        ///    GUIStyle style;
        ///    string str = "A string...\nWith two lines.";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.textArea = style;
        ///        str = GUILayout.TextArea(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle textArea { get { return m_textArea; } set { m_textArea = value; Apply(); } }

        ///<summary>Style used by default for <see cref="GUI.Button" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the button style of the current GUISkin
        ///
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.box = style;
        ///        GUILayout.Button("This is a button.");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle button { get { return m_button; } set { m_button = value; Apply(); } }

        ///<summary>Style used by default for <see cref="GUI.Toggle" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the toggle style of the current GUISkin
        ///
        ///    GUIStyle style;
        ///    public bool val = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.toggle = style;
        ///        val = GUILayout.Toggle(val, "A Toggle control");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle toggle { get { return m_toggle; } set { m_toggle = value; Apply(); } }

        ///<summary>Style used by default for Window controls ().</summary>
        ///<seealso cref="GUI.Window" />
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the window style of the current GUISkin
        ///
        ///    GUIStyle style;
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.window = style;
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button will size to fit the window
        ///        if (GUILayout.Button("Hello World"))
        ///            print("Got a click");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle window { get { return m_window; } set { m_window = value; Apply(); } }

        [SerializeField]
        GUIStyle m_horizontalSlider;
        [SerializeField]
        GUIStyle m_horizontalSliderThumb;
        [NonSerialized]
        GUIStyle m_horizontalSliderThumbExtent;
        [SerializeField]
        GUIStyle m_verticalSlider;
        [SerializeField]
        GUIStyle m_verticalSliderThumb;
        [NonSerialized]
        GUIStyle m_verticalSliderThumbExtent;
        [NonSerialized]
        GUIStyle m_SliderMixed;

        ///<summary>Style used by default for the background part of <see cref="GUI.HorizontalSlider" /> controls.</summary>
        ///<remarks>The padding property is used to determine the size of the area the thumb can be dragged within.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the horizontal slider style of the current GUISkin
        ///
        ///    float hSliderValue = 0.0f;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.horizontalSlider = style;
        ///        hSliderValue = GUILayout.HorizontalSlider(hSliderValue, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle horizontalSlider { get { return m_horizontalSlider; } set { m_horizontalSlider = value; Apply(); } }

        ///<summary>Style used by default for the thumb that is dragged in <see cref="GUI.HorizontalSlider" /> controls.</summary>
        ///<remarks>The padding property is used to determine the size of the thumb.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the horizontal slider style of the current GUISkin
        ///
        ///    float hSliderValue = 0.0f;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.horizontalSliderThumb = style;
        ///        hSliderValue = GUILayout.HorizontalSlider(hSliderValue, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle horizontalSliderThumb { get { return m_horizontalSliderThumb; } set { m_horizontalSliderThumb = value; Apply(); } }

        // Style used by default for the extended region around the thumb in GUI::ref::HorizontalSlider controls.
        internal GUIStyle horizontalSliderThumbExtent { get { return m_horizontalSliderThumbExtent; } set { m_horizontalSliderThumbExtent = value; Apply(); } }

        //Style used for thumb and thumbextent when multiple objects are selected
        internal GUIStyle sliderMixed { get { return m_SliderMixed; } set { m_SliderMixed = value; Apply(); } }

        ///<summary>Style used by default for the background part of <see cref="GUI.VerticalSlider" /> controls.</summary>
        ///<remarks>The padding property is used to determine the size of the area the thumb can be dragged within.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the vertical slider style of the current GUISkin
        ///
        ///    float vSliderValue = 0.0f;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.verticalSlider = style;
        ///        vSliderValue = GUILayout.VerticalSlider(vSliderValue, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle verticalSlider { get { return m_verticalSlider; } set { m_verticalSlider = value; Apply(); } }

        ///<summary>Style used by default for the thumb that is dragged in <see cref="GUI.VerticalSlider" /> controls.</summary>
        ///<remarks>The padding property is used to determine the size of the thumb.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the vertical slider thumb style of the current GUISkin
        ///
        ///    float vSliderValue = 0.0f;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.verticalSliderThumb = style;
        ///        vSliderValue = GUILayout.VerticalSlider(vSliderValue, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle verticalSliderThumb { get { return m_verticalSliderThumb; } set { m_verticalSliderThumb = value; Apply(); } }

        // Style used by default for the extended region around the thumb in GUI::ref::VerticalSlider controls.
        internal GUIStyle verticalSliderThumbExtent { get { return m_verticalSliderThumbExtent; } set { m_verticalSliderThumbExtent = value; Apply(); } }

        [SerializeField]
        GUIStyle m_horizontalScrollbar;
        [SerializeField]
        GUIStyle m_horizontalScrollbarThumb;
        [SerializeField]
        GUIStyle m_horizontalScrollbarLeftButton;
        [SerializeField]
        GUIStyle m_horizontalScrollbarRightButton;

        ///<summary>Style used by default for the background part of <see cref="GUI.HorizontalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the default background of the
        ///    // horizontal scrollbar of the current GUISkin
        ///
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.horizontalScrollbar = style;
        ///        hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle horizontalScrollbar { get { return m_horizontalScrollbar; } set { m_horizontalScrollbar = value; Apply(); } }
        ///<summary>Style used by default for the thumb that is dragged in <see cref="GUI.HorizontalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the horizontal scrollbar thumb of the current GUISkin
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.horizontalScrollbarThumb = style;
        ///        hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle horizontalScrollbarThumb { get { return m_horizontalScrollbarThumb; } set { m_horizontalScrollbarThumb = value; Apply(); } }
        ///<summary>Style used by default for the left button on <see cref="GUI.HorizontalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the horizontal scrollbar
        ///    // left button of the current GUISkin
        ///
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.horizontalScrollbarLeftButton = style;
        ///        hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle horizontalScrollbarLeftButton { get { return m_horizontalScrollbarLeftButton; } set { m_horizontalScrollbarLeftButton = value; Apply(); } }
        ///<summary>Style used by default for the right button on <see cref="GUI.HorizontalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the horizontal scrollbar
        ///    // left button of the current GUISkin
        ///
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.horizontalScrollbarRightButton = style;
        ///        hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle horizontalScrollbarRightButton { get { return m_horizontalScrollbarRightButton; } set { m_horizontalScrollbarRightButton = value; Apply(); } }

        [SerializeField]
        GUIStyle m_verticalScrollbar;
        [SerializeField]
        GUIStyle m_verticalScrollbarThumb;
        [SerializeField]
        GUIStyle m_verticalScrollbarUpButton;
        [SerializeField]
        GUIStyle m_verticalScrollbarDownButton;

        ///<summary>Style used by default for the background part of <see cref="GUI.VerticalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the default background of the
        ///    // vertical scrollbar of the current GUISkin
        ///
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.verticalScrollbar = style;
        ///        hSbarValue = GUILayout.VerticalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle verticalScrollbar { get { return m_verticalScrollbar; } set { m_verticalScrollbar = value; Apply(); } }
        ///<summary>Style used by default for the thumb that is dragged in <see cref="GUI.VerticalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the vertical scrollbar thumb of the current GUISkin
        ///
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.verticalScrollbarThumb = style;
        ///        hSbarValue = GUILayout.VerticalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle verticalScrollbarThumb { get { return m_verticalScrollbarThumb; } set { m_verticalScrollbarThumb = value; Apply(); } }
        ///<summary>Style used by default for the up button on <see cref="GUI.VerticalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the vertical scrollbar
        ///    // up button of the current GUISkin
        ///
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.verticalScrollbarUpButton = style;
        ///        hSbarValue = GUILayout.VerticalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle verticalScrollbarUpButton { get { return m_verticalScrollbarUpButton; } set { m_verticalScrollbarUpButton = value; Apply(); } }
        ///<summary>Style used by default for the down button on <see cref="GUI.VerticalScrollbar" /> controls.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the vertical scrollbar
        ///    // down button of the current GUISkin
        ///
        ///    float hSbarValue;
        ///    GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.verticalScrollbarDownButton = style;
        ///        hSbarValue = GUILayout.VerticalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle verticalScrollbarDownButton { get { return m_verticalScrollbarDownButton; } set { m_verticalScrollbarDownButton = value; Apply(); } }

        // Background style for scroll views.
        [SerializeField]
        GUIStyle m_ScrollView;

        ///<summary>Style used by default for the background of ScrollView controls (see <see cref="GUI.BeginScrollView" />).</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Modifies only the background of the ScrollView controls
        ///    // of the current GUISkin.
        ///
        ///    Vector2 scrollPosition = Vector2.zero;
        ///    public GUIStyle style;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.scrollView = style;
        ///        // rect and put it in a small rect on the screen.
        ///        scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100),
        ///            scrollPosition, new Rect(0, 0, 220, 200));
        ///
        ///        // Make four buttons - one in each corner. The coordinate system is defined
        ///        // by the last parameter to BeginScrollView.
        ///        GUI.Button(new Rect(0, 0, 100, 20), "Top-left");
        ///        GUI.Button(new Rect(120, 0, 100, 20), "Top-right");
        ///        GUI.Button(new Rect(0, 180, 100, 20), "Bottom-left");
        ///        GUI.Button(new Rect(120, 180, 100, 20), "Bottom-right");
        ///
        ///        // End the scroll view that we began above.
        ///        GUI.EndScrollView();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle scrollView { get { return m_ScrollView; } set { m_ScrollView = value; Apply(); } }

        ///<exclude />
        [SerializeField]
        internal GUIStyle[] m_CustomStyles;

        ///<summary>Array of GUI styles for specific needs.</summary>
        public GUIStyle[] customStyles { get { return m_CustomStyles; } set { m_CustomStyles = value; Apply(); } }


        [SerializeField]
        private GUISettings m_Settings = new GUISettings();

        ///<summary>Generic settings for how controls should behave with this skin.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Sets the selection color to cyan
        ///
        ///    string str = "This is a string with\ntwo lines of text";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.settings.selectionColor = Color.cyan;
        ///        str = GUILayout.TextArea(str);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUISettings settings { get { return m_Settings; } }

        ///<exclude />
        [NoAutoStaticsCleanup] // lazy-cache sentinel GUIStyle; recreated on demand via null check; no user-assembly refs
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

            if (m_Styles == null)
                m_Styles = new Dictionary<string, GUIStyle>(StringComparer.OrdinalIgnoreCase);
            else
                m_Styles.Clear();

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

            if (!m_Styles.TryGetValue("HorizontalSliderThumbExtent", out m_horizontalSliderThumbExtent))
            {
                m_horizontalSliderThumbExtent = new GUIStyle();
                m_horizontalSliderThumbExtent.name = "horizontalsliderthumbextent";
                m_Styles["HorizontalSliderThumbExtent"] = m_horizontalSliderThumbExtent;
            }

            if (!m_Styles.TryGetValue("SliderMixed", out m_SliderMixed))
            {
                m_SliderMixed = new GUIStyle();
                m_SliderMixed.name = "SliderMixed";
                m_Styles["SliderMixed"] = m_SliderMixed;
            }

            if (!m_Styles.TryGetValue("VerticalSliderThumbExtent", out m_verticalSliderThumbExtent))
            {
                m_verticalSliderThumbExtent = new GUIStyle();
                m_Styles["VerticalSliderThumbExtent"] = m_verticalSliderThumbExtent;
                m_verticalSliderThumbExtent.name = "verticalsliderthumbextent";
            }

            error.stretchHeight = true;
            error.normal.textColor = Color.red;
        }

        ///<summary>Get a named <see cref="GUIStyle" />.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    bool b;
        ///
        ///    void OnGUI()
        ///    {
        ///        b = GUILayout.Toggle(b, "A toggle button", GUI.skin.GetStyle("Button"));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = FindStyle(styleName);
            if (s != null)
                return s;
            Debug.LogWarning("Unable to find style '" + styleName + "' in skin '" + name + "' " + (Event.current != null ? Event.current.type.ToString() : "<called outside OnGUI>"));
            return error;
        }

        ///<summary>Try to search for a <see cref="GUIStyle" />. This functions returns NULL and does not give an error.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Checks if a style name exists
        ///
        ///    string aStyleName = "A Style I have";
        ///
        ///    void OnGUI()
        ///    {
        ///        if (GUI.skin.FindStyle(aStyleName) == null)
        ///        {
        ///            Debug.LogWarning("No style named \"" + aStyleName + "\" could be found");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyle FindStyle(string styleName)
        {
            if (m_Styles == null)
                BuildStyleCache();

            GUIStyle style;
            if (m_Styles.TryGetValue(styleName, out style))
                return style;

            return null;
        }

        internal delegate void SkinChangedDelegate();
        ///<exclude />
        [NoAutoStaticsCleanup] // wired once by EditorGUIUtility's static ctor (UnityEditor.dll, never reloaded); clearing on reload orphans it permanently
        internal static SkinChangedDelegate m_SkinChanged;

        ///<summary>Make this the current skin used by the GUI.</summary>
        [NoAutoStaticsCleanup] // active skin reference set each frame by MakeCurrent; stale ref after reload is overwritten before any GUI render
        static internal GUISkin current;
        internal void MakeCurrent()
        {
            current = this;
            GUIStyle.SetDefaultFont(font);
            if (m_SkinChanged != null)
                m_SkinChanged();
        }

        //*undocumented* Documented separately
        ///<exclude />
        public IEnumerator GetEnumerator()
        {
            if (m_Styles == null)
                BuildStyleCache();
            return m_Styles.Values.GetEnumerator();
        }
    }
}
