// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: IMGUIFramework not yet converted
using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public partial class GUI
    {
        private const float s_ScrollStepSize = 10f;
        [NoAutoStaticsCleanup] // frame-level scroller tracking ID; reset to 0 on mouse-up, no cross-reload invariants
        private static int s_ScrollControlId;

        [AutoStaticsCleanupOnCodeReload]
        private static int s_HotTextField = -1;

        private static readonly int s_BoxHash               = "Box".GetHashCode();
        private static readonly int s_ButonHash             = "Button".GetHashCode();
        private static readonly int s_RepeatButtonHash      = "repeatButton".GetHashCode();
        private static readonly int s_ToggleHash            = "Toggle".GetHashCode();
        private static readonly int s_ButtonGridHash        = "ButtonGrid".GetHashCode();
        private static readonly int s_SliderHash            = "Slider".GetHashCode();
        private static readonly int s_BeginGroupHash        = "BeginGroup".GetHashCode();
        private static readonly int s_ScrollviewHash        = "scrollView".GetHashCode();

        [NoAutoStaticsCleanup] // which side of the scroll trough is held; resets on mouse-up each frame
        internal static int scrollTroughSide { get; set; }
        [NoAutoStaticsCleanup] // scroll timing sentinel; DateTime value type, stale after reload but overwritten before next scroll interaction
        internal static DateTime nextScrollStepTime { get; set; } = DateTime.Now;

        [AutoStaticsCleanupOnCodeReload]
        private static GUISkin s_Skin;

        ///<summary>The global skin to use.</summary>
        ///<remarks>You can set this at any point to change the look of your GUI. If you set it to null, the skin will revert to the default Unity skin.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Press space to change between added GUI skins.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public GUISkin[] s1;
        ///
        ///    private float hSliderValue = 0.0F;
        ///    private float vSliderValue = 0.0F;
        ///    private float hSValue = 0.0F;
        ///    private float vSValue = 0.0F;
        ///    private int cont = 0;
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            cont++;
        ///        }
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.skin = s1[cont % s1.Length];
        ///
        ///        if (s1.Length == 0)
        ///        {
        ///            Debug.LogError("Assign at least 1 skin on the array");
        ///            return;
        ///        }
        ///
        ///        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        ///        GUI.Box(new Rect(10, 50, 50, 50), "A BOX");
        ///
        ///        if (GUI.Button(new Rect(10, 110, 70, 30), "A button"))
        ///        {
        ///            Debug.Log("Button has been pressed");
        ///        }
        ///
        ///        hSliderValue = GUI.HorizontalSlider(new Rect(10, 150, 100, 30), hSliderValue, 0.0F, 10.0F);
        ///        vSliderValue = GUI.VerticalSlider(new Rect(10, 170, 100, 30), vSliderValue, 10.0F, 0.0F);
        ///        hSValue = GUI.HorizontalScrollbar(new Rect(10, 210, 100, 30), hSValue, 1.0F, 0.0F, 10.0F);
        ///        vSValue = GUI.VerticalScrollbar(new Rect(10, 230, 100, 30), vSValue, 1.0F, 10.0F, 0.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static GUISkin skin
        {
            set
            {
                GUIUtility.CheckOnGUI();
                DoSetSkin(value);
            }
            get
            {
                GUIUtility.CheckOnGUI();
                return s_Skin;
            }
        }

        internal static void DoSetSkin(GUISkin newSkin)
        {
            if (!newSkin)
                newSkin = GUIUtility.GetDefaultSkin();
            s_Skin = newSkin;
            newSkin.MakeCurrent();
        }

        ///<summary>The GUI transform matrix.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Example()
        ///    {
        ///        print(GUI.matrix);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Matrix4x4 matrix
        {
            get { return GUIClip.GetMatrix(); }
            set { GUIClip.SetMatrix(value); }
        }

        ///<summary>The tooltip of the control the mouse is currently over, or which has keyboard focus. (RO).</summary>
        ///<remarks>
        ///  <para>When you create GUI controls, you can pass in a tooltip for them. This is done by changing the content parameter
        ///to take a custom-made <see cref="GUIContent" /> object, rather than just passing in a string to display.
        ///
        ///When the mouse is over a control with a tooltip, it sets the global <see cref="GUI.tooltip" /> value to the tooltip you pass in.
        ///If the mouse is not hovering over any control, the value is set to the control which has keyboard focus.
        ///At the end of the OnGUI code, you can make a label showing the value of <see cref="GUI.tooltip" /><img src="GUITooltip.png" />
        ///
        ///GUI Tooltip on the Game view appears when the mouse is over the button.</para>
        ///  <para>You can use the ordering of elements to create 'hierarchical' tooltips:</para>
        ///  <para>Tooltips can also be used to implement an OnMouseOver / OnMouseOut messaging system:</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Make a button using a custom GUIContent parameter to pass in the tooltip.
        ///        GUI.Button(new Rect(10, 10, 100, 20), new GUIContent("Click me", "This is the tooltip"));
        ///
        ///        // Display the tooltip from the element that has mouseover or keyboard focus
        ///        GUI.Label(new Rect(10, 40, 100, 40), GUI.tooltip);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // This box is larger than many elements following it, and it has a tooltip.
        ///        GUI.Box(new Rect(5, 35, 110, 75), new GUIContent("Box", "this box has a tooltip"));
        ///
        ///        // This button is inside the box, but has no tooltip so it does not
        ///        // override the box's tooltip.
        ///        GUI.Button(new Rect(10, 55, 100, 20), "No tooltip here");
        ///
        ///        // This button is inside the box, and HAS a tooltip so it overrides
        ///        // the tooltip from the box.
        ///        GUI.Button(new Rect(10, 80, 100, 20), new GUIContent("I have a tooltip", "The button overrides the box"));
        ///
        ///        // finally, display the tooltip from the element that has
        ///        // mouseover or keyboard focus
        ///        GUI.Label(new Rect(10, 40, 100, 40), GUI.tooltip);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string lastTooltip = " ";
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.Button(new GUIContent("Play Game", "Button1"));
        ///        GUILayout.Button(new GUIContent("Quit", "Button2"));
        ///
        ///        if (Event.current.type == EventType.Repaint && GUI.tooltip != lastTooltip)
        ///        {
        ///            if (lastTooltip != "")
        ///            {
        ///                SendMessage(lastTooltip + "OnMouseOut", SendMessageOptions.DontRequireReceiver);
        ///            }
        ///
        ///            if (GUI.tooltip != "")
        ///            {
        ///                SendMessage(GUI.tooltip + "OnMouseOver", SendMessageOptions.DontRequireReceiver);
        ///            }
        ///
        ///            lastTooltip = GUI.tooltip;
        ///        }
        ///    }
        ///
        ///    void Button1OnMouseOver()
        ///    {
        ///        Debug.Log("Play game got focus");
        ///    }
        ///
        ///    void Button2OnMouseOut()
        ///    {
        ///        Debug.Log("Quit lost focus");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string tooltip
        {
            get { return Internal_GetTooltip(); }
            set { Internal_SetTooltip(value); }
        }

        ///<exclude />
        protected static string mouseTooltip => Internal_GetMouseTooltip();

        ///<exclude />
        protected static Rect tooltipRect
        {
            get { return s_ToolTipRect; }
            set { s_ToolTipRect = value; }
        }

        ///<exclude />
        [NoAutoStaticsCleanup] // tooltip hit-rect set every repaint; stale value after reload is harmless (overwritten before use)
        internal static Rect s_ToolTipRect;

        ///<summary>Make a text or texture label on screen.</summary>
        ///<remarks>
        ///  <para>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control.
        ///
        ///Example: Draw the classic Hello World! string:
        ///
        ///<img src="GUILabel.png" />
        ///
        ///Text label on the Game View.</para>
        ///  <para>Example: Draw a texture on-screen. Labels are also used to display textures, instead of a string, simply pass in a texture:
        ///
        ///<img src="GUILabelTexture.png" />
        ///
        ///Texture Label.</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the label.</param>
        ///<param name="text">Text to display on the label.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture2D textureToDisplay;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 40, textureToDisplay.width, textureToDisplay.height), textureToDisplay);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Label(Rect position, string text)
        {
            Label(position, GUIContent.Temp(text), s_Skin.label);
        }

        ///<summary>Make a text or texture label on screen.</summary>
        ///<remarks>
        ///  <para>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control.
        ///
        ///Example: Draw the classic Hello World! string:
        ///
        ///<img src="GUILabel.png" />
        ///
        ///Text label on the Game View.</para>
        ///  <para>Example: Draw a texture on-screen. Labels are also used to display textures, instead of a string, simply pass in a texture:
        ///
        ///<img src="GUILabelTexture.png" />
        ///
        ///Texture Label.</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the label.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the label.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture2D textureToDisplay;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 40, textureToDisplay.width, textureToDisplay.height), textureToDisplay);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Label(Rect position, Texture image)
        {
            Label(position, GUIContent.Temp(image), s_Skin.label);
        }

        ///<summary>Make a text or texture label on screen.</summary>
        ///<remarks>
        ///  <para>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control.
        ///
        ///Example: Draw the classic Hello World! string:
        ///
        ///<img src="GUILabel.png" />
        ///
        ///Text label on the Game View.</para>
        ///  <para>Example: Draw a texture on-screen. Labels are also used to display textures, instead of a string, simply pass in a texture:
        ///
        ///<img src="GUILabelTexture.png" />
        ///
        ///Texture Label.</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the label.</param>
        ///<param name="content">Text, image and tooltip for this label.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture2D textureToDisplay;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 40, textureToDisplay.width, textureToDisplay.height), textureToDisplay);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Label(Rect position, GUIContent content)
        {
            Label(position, content, s_Skin.label);
        }

        ///<summary>Make a text or texture label on screen.</summary>
        ///<remarks>
        ///  <para>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control.
        ///
        ///Example: Draw the classic Hello World! string:
        ///
        ///<img src="GUILabel.png" />
        ///
        ///Text label on the Game View.</para>
        ///  <para>Example: Draw a texture on-screen. Labels are also used to display textures, instead of a string, simply pass in a texture:
        ///
        ///<img src="GUILabelTexture.png" />
        ///
        ///Texture Label.</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the label.</param>
        ///<param name="text">Text to display on the label.</param>
        ///<param name="style">The style to use. If left out, the <c>label</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture2D textureToDisplay;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 40, textureToDisplay.width, textureToDisplay.height), textureToDisplay);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Label(Rect position, string text, GUIStyle style)
        {
            Label(position, GUIContent.Temp(text), style);
        }

        ///<summary>Make a text or texture label on screen.</summary>
        ///<remarks>
        ///  <para>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control.
        ///
        ///Example: Draw the classic Hello World! string:
        ///
        ///<img src="GUILabel.png" />
        ///
        ///Text label on the Game View.</para>
        ///  <para>Example: Draw a texture on-screen. Labels are also used to display textures, instead of a string, simply pass in a texture:
        ///
        ///<img src="GUILabelTexture.png" />
        ///
        ///Texture Label.</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the label.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the label.</param>
        ///<param name="style">The style to use. If left out, the <c>label</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture2D textureToDisplay;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 40, textureToDisplay.width, textureToDisplay.height), textureToDisplay);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Label(Rect position, Texture image, GUIStyle style)
        {
            Label(position, GUIContent.Temp(image), style);
        }

        ///<summary>Make a text or texture label on screen.</summary>
        ///<remarks>
        ///  <para>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control.
        ///
        ///Example: Draw the classic Hello World! string:
        ///
        ///<img src="GUILabel.png" />
        ///
        ///Text label on the Game View.</para>
        ///  <para>Example: Draw a texture on-screen. Labels are also used to display textures, instead of a string, simply pass in a texture:
        ///
        ///<img src="GUILabelTexture.png" />
        ///
        ///Texture Label.</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the label.</param>
        ///<param name="content">Text, image and tooltip for this label.</param>
        ///<param name="style">The style to use. If left out, the <c>label</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 10, 100, 20), "Hello World!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture2D textureToDisplay;
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Label(new Rect(10, 40, textureToDisplay.width, textureToDisplay.height), textureToDisplay);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Label(Rect position, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            DoLabel(position, content, style);
        }

        ///<summary>Draw a texture within a rectangle.</summary>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a texture in the left corner of the screen.
        /// // The texture is drawn in a window 60x60 pixels.
        /// // The source texture is given an aspect ratio of 10x1
        /// // and scaled to fit in the 60x60 rectangle.  Because
        /// // the aspect ratio is preserved, the texture will fit
        /// // inside a 60x10 pixel area of the screen rectangle.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Assign a Texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        GUI.DrawTexture(new Rect(10, 10, 60, 60), aTexture, ScaleMode.ScaleToFit, true, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.color" />
        ///<seealso cref="GUI.contentColor" />
        public static void DrawTexture(Rect position, Texture image)
        {
            DrawTexture(position, image, ScaleMode.StretchToFill);
        }

        ///<summary>Draw a texture within a rectangle.</summary>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<param name="scaleMode">How to scale the image when the aspect ratio of it doesn't fit the aspect ratio to be drawn within.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a texture in the left corner of the screen.
        /// // The texture is drawn in a window 60x60 pixels.
        /// // The source texture is given an aspect ratio of 10x1
        /// // and scaled to fit in the 60x60 rectangle.  Because
        /// // the aspect ratio is preserved, the texture will fit
        /// // inside a 60x10 pixel area of the screen rectangle.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Assign a Texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        GUI.DrawTexture(new Rect(10, 10, 60, 60), aTexture, ScaleMode.ScaleToFit, true, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.color" />
        ///<seealso cref="GUI.contentColor" />
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode)
        {
            DrawTexture(position, image, scaleMode, true);
        }

        ///<summary>Draw a texture within a rectangle.</summary>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<param name="scaleMode">How to scale the image when the aspect ratio of it doesn't fit the aspect ratio to be drawn within.</param>
        ///<param name="alphaBlend">Whether to apply alpha blending when drawing the image (enabled by default).</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a texture in the left corner of the screen.
        /// // The texture is drawn in a window 60x60 pixels.
        /// // The source texture is given an aspect ratio of 10x1
        /// // and scaled to fit in the 60x60 rectangle.  Because
        /// // the aspect ratio is preserved, the texture will fit
        /// // inside a 60x10 pixel area of the screen rectangle.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Assign a Texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        GUI.DrawTexture(new Rect(10, 10, 60, 60), aTexture, ScaleMode.ScaleToFit, true, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.color" />
        ///<seealso cref="GUI.contentColor" />
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode , bool alphaBlend)
        {
            DrawTexture(position, image, scaleMode, alphaBlend, 0);
        }

        ///<summary>Draw a texture within a rectangle.</summary>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<param name="scaleMode">How to scale the image when the aspect ratio of it doesn't fit the aspect ratio to be drawn within.</param>
        ///<param name="alphaBlend">Whether to apply alpha blending when drawing the image (enabled by default).</param>
        ///<param name="imageAspect">Aspect ratio to use for the source image. If 0 (the default), the aspect ratio from the image is used.  Pass in w/h for the desired aspect ratio.  This allows the aspect ratio of the source image to be adjusted without changing the pixel width and height.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a texture in the left corner of the screen.
        /// // The texture is drawn in a window 60x60 pixels.
        /// // The source texture is given an aspect ratio of 10x1
        /// // and scaled to fit in the 60x60 rectangle.  Because
        /// // the aspect ratio is preserved, the texture will fit
        /// // inside a 60x10 pixel area of the screen rectangle.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Assign a Texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        GUI.DrawTexture(new Rect(10, 10, 60, 60), aTexture, ScaleMode.ScaleToFit, true, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.color" />
        ///<seealso cref="GUI.contentColor" />
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect)
        {
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, GUI.color, 0, 0);
        }

        ///<summary>Draws a border with rounded corners within a rectangle. The texture is used to pattern the border.  Note that this method only works on shader model 2.5 and above.</summary>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<param name="scaleMode">How to scale the image when the aspect ratio of it doesn't fit the aspect ratio to be drawn within.</param>
        ///<param name="alphaBlend">Whether to apply alpha blending when drawing the image (enabled by default).</param>
        ///<param name="imageAspect">Aspect ratio to use for the source image. If 0 (the default), the aspect ratio from the image is used.  Pass in w/h for the desired aspect ratio.  This allows the aspect ratio of the source image to be adjusted without changing the pixel width and height.</param>
        ///<param name="color">A tint color to apply on the texture.</param>
        ///<param name="borderWidth">The width of the border. If 0, the full texture is drawn.</param>
        ///<param name="borderRadius">The radius for rounded corners. If 0, corners will not be rounded.</param>
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, float borderWidth, float borderRadius)
        {
            var borderWidths = Vector4.one * borderWidth;
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, color, borderWidths, borderRadius);
        }

        ///<summary>Draws a border with rounded corners within a rectangle. The texture is used to pattern the border.  Note that this method only works on shader model 2.5 and above.</summary>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<param name="scaleMode">How to scale the image when the aspect ratio of it doesn't fit the aspect ratio to be drawn within.</param>
        ///<param name="alphaBlend">Whether to apply alpha blending when drawing the image (enabled by default).</param>
        ///<param name="imageAspect">Aspect ratio to use for the source image. If 0 (the default), the aspect ratio from the image is used.  Pass in w/h for the desired aspect ratio.  This allows the aspect ratio of the source image to be adjusted without changing the pixel width and height.</param>
        ///<param name="color">A tint color to apply on the texture.</param>
        ///<param name="borderWidths">The width of the borders (left, top, right and bottom). If Vector4.zero, the full texture is drawn.</param>
        ///<param name="borderRadius">The radius for rounded corners. If 0, corners will not be rounded.</param>
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, Vector4 borderWidths, float borderRadius)
        {
            var borderRadiuses = Vector4.one * borderRadius;
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, color, borderWidths, borderRadiuses);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend
            , float imageAspect, Color color, Vector4 borderWidths, Vector4 borderRadiuses)
        {
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, color, borderWidths, borderRadiuses, true);
        }

        internal static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend
            , float imageAspect, Color color, Vector4 borderWidths, Vector4 borderRadiuses, bool drawSmoothCorners)
        {
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, color, color, color, color
                , borderWidths, borderRadiuses, drawSmoothCorners);
        }

        // Draw a texture within a rectangle.
        internal static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color leftColor, Color topColor, Color rightColor, Color bottomColor, Vector4 borderWidths, Vector4 borderRadiuses)
        {
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, leftColor, topColor, rightColor, bottomColor, borderWidths, borderRadiuses, true);
        }

        internal static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color leftColor, Color topColor, Color rightColor, Color bottomColor, Vector4 borderWidths, Vector4 borderRadiuses, bool drawSmoothCorners)
        {
            GUIUtility.CheckOnGUI();
            if (Event.current.type == EventType.Repaint)
            {
                if (image == null)
                {
                    Debug.LogWarning("null texture passed to GUI.DrawTexture");
                    return;
                }

                if (imageAspect == 0)
                    imageAspect = (float)image.width / image.height;

                Material mat = null;
                if (borderWidths != Vector4.zero)
                {
                    if ((leftColor != topColor) || (leftColor != rightColor) || (leftColor != bottomColor))
                    {
                        mat = roundedRectWithColorPerBorderMaterial;
                    }
                    else
                    {
                        mat = roundedRectMaterial;
                    }
                }
                else if (borderRadiuses != Vector4.zero)
                {
                    mat = roundedRectMaterial;
                }
                else
                {
                    mat = alphaBlend ? blendMaterial : blitMaterial;
                }

                Internal_DrawTextureArguments arguments = new Internal_DrawTextureArguments
                {
                    leftBorder = 0,
                    rightBorder = 0,
                    topBorder = 0,
                    bottomBorder = 0,
                    color = leftColor,
                    leftBorderColor = leftColor,
                    topBorderColor = topColor,
                    rightBorderColor = rightColor,
                    bottomBorderColor = bottomColor,
                    borderWidths = borderWidths,
                    cornerRadiuses = borderRadiuses,
                    texture = image,
                    smoothCorners = drawSmoothCorners,
                    mat = mat
                };
                CalculateScaledTextureRects(position, scaleMode, imageAspect, ref arguments.screenRect, ref arguments.sourceRect);
                Graphics.Internal_DrawTexture(ref arguments);
            }
        }

        // Calculate screenrect and sourcerect for different scalemodes
        internal static bool CalculateScaledTextureRects(Rect position, ScaleMode scaleMode, float imageAspect, ref Rect outScreenRect, ref Rect outSourceRect)
        {
            float destAspect = position.width / position.height;
            bool ret = false;

            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    outScreenRect = position;
                    outSourceRect = new Rect(0, 0, 1, 1);
                    ret = true;
                    break;
                case ScaleMode.ScaleAndCrop:
                    if (destAspect > imageAspect)
                    {
                        float stretch = imageAspect / destAspect;
                        outScreenRect = position;
                        outSourceRect = new Rect(0, (1 - stretch) * .5f, 1, stretch);
                        ret = true;
                    }
                    else
                    {
                        float stretch = destAspect / imageAspect;
                        outScreenRect = position;
                        outSourceRect = new Rect(.5f - stretch * .5f, 0, stretch, 1);
                        ret = true;
                    }
                    break;
                case ScaleMode.ScaleToFit:
                    if (destAspect > imageAspect)
                    {
                        float stretch = imageAspect / destAspect;
                        outScreenRect = new Rect(position.xMin + position.width * (1.0f - stretch) * .5f, position.yMin, stretch * position.width, position.height);
                        outSourceRect = new Rect(0, 0, 1, 1);
                        ret = true;
                    }
                    else
                    {
                        float stretch = destAspect / imageAspect;
                        outScreenRect = new Rect(position.xMin, position.yMin + position.height * (1.0f - stretch) * .5f, position.width, stretch * position.height);
                        outSourceRect = new Rect(0, 0, 1, 1);
                        ret = true;
                    }
                    break;
            }

            return ret;
        }

        ///<summary>Draw a texture within a rectangle with the given texture coordinates.</summary>
        ///<remarks>Use this function for clipping or tiling the image within the given rectangle.  The second <see cref="Rect" /><c>texCoords</c> describes how the texture is adjusted to fit the position <see cref="Rect" />.  The first rectangle has its size in pixels provided; the second rectangle is given in a 0.0 to 1.0 range.</remarks>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<param name="texCoords">How to scale the image when the aspect ratio of it doesn't fit the aspect ratio to be drawn within.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Use DrawTextureWithTexCoords() to draw a texture.  The texture is draw on the window
        /// // inside a given pixel rectangle.  The size of the drawn texture is based on the value
        /// // of hor.  This ranges from 0.5 to 1.25 so the bottom left half of the texture to a
        /// // greater than normal value.
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Texture2D tex;
        ///    private Rect rect;
        ///    private float hor;
        ///    private Rect hs;
        ///    private Rect label;
        ///
        ///    void Start()
        ///    {
        ///        float center = Screen.width / 2.0f;
        ///        rect = new Rect(center - 200, 200, 400, 250);
        ///        hs = new Rect(center - 200, 125, 400, 30);
        ///        label = new Rect(center - 20, 155, 50, 30);
        ///        hor = 0.5f;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        hor = GUI.HorizontalSlider(hs, hor, 0.5f, 1.25f);
        ///        GUI.Label(label, hor.ToString("F3"));
        ///        GUI.DrawTextureWithTexCoords(rect, tex, new Rect(0.0f, 0.0f, hor, hor));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.color" />
        ///<seealso cref="GUI.contentColor" />
        public static void DrawTextureWithTexCoords(Rect position, Texture image, Rect texCoords)
        {
            DrawTextureWithTexCoords(position, image, texCoords, true);
        }

        ///<summary>Draw a texture within a rectangle with the given texture coordinates.</summary>
        ///<remarks>Use this function for clipping or tiling the image within the given rectangle.  The second <see cref="Rect" /><c>texCoords</c> describes how the texture is adjusted to fit the position <see cref="Rect" />.  The first rectangle has its size in pixels provided; the second rectangle is given in a 0.0 to 1.0 range.</remarks>
        ///<param name="position">Rectangle on the screen to draw the texture within.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display.</param>
        ///<param name="texCoords">How to scale the image when the aspect ratio of it doesn't fit the aspect ratio to be drawn within.</param>
        ///<param name="alphaBlend">Whether to alpha blend the image on to the display (the default). If false, the picture is drawn on to the display.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Use DrawTextureWithTexCoords() to draw a texture.  The texture is draw on the window
        /// // inside a given pixel rectangle.  The size of the drawn texture is based on the value
        /// // of hor.  This ranges from 0.5 to 1.25 so the bottom left half of the texture to a
        /// // greater than normal value.
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    public Texture2D tex;
        ///    private Rect rect;
        ///    private float hor;
        ///    private Rect hs;
        ///    private Rect label;
        ///
        ///    void Start()
        ///    {
        ///        float center = Screen.width / 2.0f;
        ///        rect = new Rect(center - 200, 200, 400, 250);
        ///        hs = new Rect(center - 200, 125, 400, 30);
        ///        label = new Rect(center - 20, 155, 50, 30);
        ///        hor = 0.5f;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        hor = GUI.HorizontalSlider(hs, hor, 0.5f, 1.25f);
        ///        GUI.Label(label, hor.ToString("F3"));
        ///        GUI.DrawTextureWithTexCoords(rect, tex, new Rect(0.0f, 0.0f, hor, hor));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUI.color" />
        ///<seealso cref="GUI.contentColor" />
        public static void DrawTextureWithTexCoords(Rect position, Texture image, Rect texCoords, bool alphaBlend)
        {
            GUIUtility.CheckOnGUI();

            if (Event.current.type == EventType.Repaint)
            {
                Material mat = alphaBlend ? blendMaterial : blitMaterial;

                Internal_DrawTextureArguments arguments = new Internal_DrawTextureArguments();
                arguments.texture = image;
                arguments.mat = mat;
                arguments.leftBorder = 0; arguments.rightBorder = 0; arguments.topBorder = 0; arguments.bottomBorder = 0;
                arguments.color = GUI.color;
                arguments.leftBorderColor = GUI.color;
                arguments.topBorderColor = GUI.color;
                arguments.rightBorderColor = GUI.color;
                arguments.bottomBorderColor = GUI.color;
                arguments.screenRect = position;
                arguments.sourceRect = texCoords;
                Graphics.Internal_DrawTexture(ref arguments);
            }
        }

        ///<summary>Create a Box on the GUI Layer.</summary>
        ///<remarks>
        ///  <para>A Box can contain text, an image, or a combination of these along
        ///        with an optional tooltip, through using a <see cref="GUIContent" /> parameter. You may also use
        ///        a <see cref="GUIStyle" /> to adjust the layout of items in a box, text colour and other properties.
        ///
        ///
        ///
        ///Here is an example of a Box containing Text:</para>
        ///  <para>Here is an example of a Box containing a Texture:</para>
        ///  <para>Here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip:</para>
        ///  <para>Here is an example of a Box containing Text, with options set in a GUIStyle to position the Text in the center of the Box.</para>
        ///  <para>Here is an example of a Box containing a Texture, with options set in a GUIStyle to position the Texture in the center of the Box.</para>
        ///  <para>Finally, here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip, with positional information contained in the GUIStyle parameter:</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the box.</param>
        ///<param name="text">Text to display on the box.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxExample : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextStyleExample : MonoBehaviour
        ///{
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        // Position the Text in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box", style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIStyle style = new GUIStyle();
        ///
        ///
        ///    void Start()
        ///    {
        ///        // Position the Texture in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///
        ///        // Position the Text and Texture in the center of the box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///
        ///        // Position the Text below the Texture (rather than to the right of it)
        ///        style.imagePosition = ImagePosition.ImageAbove;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Box(Rect position, string text)
        {
            Box(position, GUIContent.Temp(text), s_Skin.box);
        }

        ///<summary>Create a Box on the GUI Layer.</summary>
        ///<remarks>
        ///  <para>A Box can contain text, an image, or a combination of these along
        ///        with an optional tooltip, through using a <see cref="GUIContent" /> parameter. You may also use
        ///        a <see cref="GUIStyle" /> to adjust the layout of items in a box, text colour and other properties.
        ///
        ///
        ///
        ///Here is an example of a Box containing Text:</para>
        ///  <para>Here is an example of a Box containing a Texture:</para>
        ///  <para>Here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip:</para>
        ///  <para>Here is an example of a Box containing Text, with options set in a GUIStyle to position the Text in the center of the Box.</para>
        ///  <para>Here is an example of a Box containing a Texture, with options set in a GUIStyle to position the Texture in the center of the Box.</para>
        ///  <para>Finally, here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip, with positional information contained in the GUIStyle parameter:</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the box.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the box.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxExample : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextStyleExample : MonoBehaviour
        ///{
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        // Position the Text in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box", style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIStyle style = new GUIStyle();
        ///
        ///
        ///    void Start()
        ///    {
        ///        // Position the Texture in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///
        ///        // Position the Text and Texture in the center of the box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///
        ///        // Position the Text below the Texture (rather than to the right of it)
        ///        style.imagePosition = ImagePosition.ImageAbove;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Box(Rect position, Texture image)
        {
            Box(position, GUIContent.Temp(image), s_Skin.box);
        }

        ///<summary>Create a Box on the GUI Layer.</summary>
        ///<remarks>
        ///  <para>A Box can contain text, an image, or a combination of these along
        ///        with an optional tooltip, through using a <see cref="GUIContent" /> parameter. You may also use
        ///        a <see cref="GUIStyle" /> to adjust the layout of items in a box, text colour and other properties.
        ///
        ///
        ///
        ///Here is an example of a Box containing Text:</para>
        ///  <para>Here is an example of a Box containing a Texture:</para>
        ///  <para>Here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip:</para>
        ///  <para>Here is an example of a Box containing Text, with options set in a GUIStyle to position the Text in the center of the Box.</para>
        ///  <para>Here is an example of a Box containing a Texture, with options set in a GUIStyle to position the Texture in the center of the Box.</para>
        ///  <para>Finally, here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip, with positional information contained in the GUIStyle parameter:</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the box.</param>
        ///<param name="content">Text, image and tooltip for this box.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxExample : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextStyleExample : MonoBehaviour
        ///{
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        // Position the Text in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box", style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIStyle style = new GUIStyle();
        ///
        ///
        ///    void Start()
        ///    {
        ///        // Position the Texture in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///
        ///        // Position the Text and Texture in the center of the box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///
        ///        // Position the Text below the Texture (rather than to the right of it)
        ///        style.imagePosition = ImagePosition.ImageAbove;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Box(Rect position, GUIContent content)
        {
            Box(position, content, s_Skin.box);
        }

        ///<summary>Create a Box on the GUI Layer.</summary>
        ///<remarks>
        ///  <para>A Box can contain text, an image, or a combination of these along
        ///        with an optional tooltip, through using a <see cref="GUIContent" /> parameter. You may also use
        ///        a <see cref="GUIStyle" /> to adjust the layout of items in a box, text colour and other properties.
        ///
        ///
        ///
        ///Here is an example of a Box containing Text:</para>
        ///  <para>Here is an example of a Box containing a Texture:</para>
        ///  <para>Here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip:</para>
        ///  <para>Here is an example of a Box containing Text, with options set in a GUIStyle to position the Text in the center of the Box.</para>
        ///  <para>Here is an example of a Box containing a Texture, with options set in a GUIStyle to position the Texture in the center of the Box.</para>
        ///  <para>Finally, here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip, with positional information contained in the GUIStyle parameter:</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the box.</param>
        ///<param name="text">Text to display on the box.</param>
        ///<param name="style">The style to use. If left out, the <c>box</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxExample : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextStyleExample : MonoBehaviour
        ///{
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        // Position the Text in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box", style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIStyle style = new GUIStyle();
        ///
        ///
        ///    void Start()
        ///    {
        ///        // Position the Texture in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///
        ///        // Position the Text and Texture in the center of the box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///
        ///        // Position the Text below the Texture (rather than to the right of it)
        ///        style.imagePosition = ImagePosition.ImageAbove;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Box(Rect position, string text, GUIStyle style)
        {
            Box(position, GUIContent.Temp(text), style);
        }

        ///<summary>Create a Box on the GUI Layer.</summary>
        ///<remarks>
        ///  <para>A Box can contain text, an image, or a combination of these along
        ///        with an optional tooltip, through using a <see cref="GUIContent" /> parameter. You may also use
        ///        a <see cref="GUIStyle" /> to adjust the layout of items in a box, text colour and other properties.
        ///
        ///
        ///
        ///Here is an example of a Box containing Text:</para>
        ///  <para>Here is an example of a Box containing a Texture:</para>
        ///  <para>Here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip:</para>
        ///  <para>Here is an example of a Box containing Text, with options set in a GUIStyle to position the Text in the center of the Box.</para>
        ///  <para>Here is an example of a Box containing a Texture, with options set in a GUIStyle to position the Texture in the center of the Box.</para>
        ///  <para>Finally, here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip, with positional information contained in the GUIStyle parameter:</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the box.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the box.</param>
        ///<param name="style">The style to use. If left out, the <c>box</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxExample : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextStyleExample : MonoBehaviour
        ///{
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        // Position the Text in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box", style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIStyle style = new GUIStyle();
        ///
        ///
        ///    void Start()
        ///    {
        ///        // Position the Texture in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///
        ///        // Position the Text and Texture in the center of the box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///
        ///        // Position the Text below the Texture (rather than to the right of it)
        ///        style.imagePosition = ImagePosition.ImageAbove;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Box(Rect position, Texture image, GUIStyle style)
        {
            Box(position, GUIContent.Temp(image), style);
        }

        ///<summary>Create a Box on the GUI Layer.</summary>
        ///<remarks>
        ///  <para>A Box can contain text, an image, or a combination of these along
        ///        with an optional tooltip, through using a <see cref="GUIContent" /> parameter. You may also use
        ///        a <see cref="GUIStyle" /> to adjust the layout of items in a box, text colour and other properties.
        ///
        ///
        ///
        ///Here is an example of a Box containing Text:</para>
        ///  <para>Here is an example of a Box containing a Texture:</para>
        ///  <para>Here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip:</para>
        ///  <para>Here is an example of a Box containing Text, with options set in a GUIStyle to position the Text in the center of the Box.</para>
        ///  <para>Here is an example of a Box containing a Texture, with options set in a GUIStyle to position the Texture in the center of the Box.</para>
        ///  <para>Finally, here is an example of a Box containing a GUIContent, combining Text, Texture and Tooltip, with positional information contained in the GUIStyle parameter:</para>
        ///</remarks>
        ///<param name="position">Rectangle on the screen to use for the box.</param>
        ///<param name="content">Text, image and tooltip for this box.</param>
        ///<param name="style">The style to use. If left out, the <c>box</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxExample : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;      // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextStyleExample : MonoBehaviour
        ///{
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        // Position the Text in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "This is a box", style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithTextureStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIStyle style = new GUIStyle();
        ///
        ///
        ///    void Start()
        ///    {
        ///        // Position the Texture in the center of the Box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), BoxTexture, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BoxWithContentStyleExample : MonoBehaviour
        ///{
        ///    public Texture BoxTexture;              // Drag a Texture onto this item in the Inspector
        ///
        ///    GUIContent content;
        ///    GUIStyle style = new GUIStyle();
        ///
        ///    void Start()
        ///    {
        ///        content = new GUIContent("This is a box", BoxTexture, "This is a tooltip");
        ///
        ///        // Position the Text and Texture in the center of the box
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///
        ///        // Position the Text below the Texture (rather than to the right of it)
        ///        style.imagePosition = ImagePosition.ImageAbove;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), content, style);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void Box(Rect position, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_BoxHash, FocusType.Passive);
            if (Event.current.type == EventType.Repaint)
            {
                style.Draw(position, content, id, false, position.Contains(Event.current.mousePosition));
            }
        }

        ///<summary>Make a single press button. The user clicks them and something happens immediately.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="text">Text to display on the button.</param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // And print a message when they got clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.Button(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Button(Rect position, string text)
        {
            return Button(position, GUIContent.Temp(text), s_Skin.button);
        }

        ///<summary>Make a single press button. The user clicks them and something happens immediately.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // And print a message when they got clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.Button(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Button(Rect position, Texture image)
        {
            return Button(position, GUIContent.Temp(image), s_Skin.button);
        }

        ///<summary>Make a single press button. The user clicks them and something happens immediately.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // And print a message when they got clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.Button(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Button(Rect position, GUIContent content)
        {
            return Button(position, content, s_Skin.button);
        }

        ///<summary>Make a single press button. The user clicks them and something happens immediately.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // And print a message when they got clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.Button(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Button(Rect position, string text, GUIStyle style)
        {
            return Button(position, GUIContent.Temp(text), style);
        }

        ///<summary>Make a single press button. The user clicks them and something happens immediately.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // And print a message when they got clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.Button(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Button(Rect position, Texture image, GUIStyle style)
        {
            return Button(position, GUIContent.Temp(image), style);
        }

        ///<summary>Make a single press button. The user clicks them and something happens immediately.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // And print a message when they got clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.Button(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.Button(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Button(Rect position, GUIContent content, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_ButonHash, FocusType.Passive, position);
            return Button(position, id, content, style);
        }

        internal static bool Button(Rect position, int id, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoButton(position, id, content, style);
        }

        ///<summary>Make a button that is active as long as the user holds it down.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="text">Text to display on the button.</param>
        ///<returns>True when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // Prints a message when they get clicked.
        ///
        /// // Prints a message when they get clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool RepeatButton(Rect position, string text)
        {
            return DoRepeatButton(position, GUIContent.Temp(text), s_Skin.button, FocusType.Passive);
        }

        ///<summary>Make a button that is active as long as the user holds it down.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<returns>True when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // Prints a message when they get clicked.
        ///
        /// // Prints a message when they get clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool RepeatButton(Rect position, Texture image)
        {
            return DoRepeatButton(position, GUIContent.Temp(image), s_Skin.button, FocusType.Passive);
        }

        ///<summary>Make a button that is active as long as the user holds it down.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<returns>True when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // Prints a message when they get clicked.
        ///
        /// // Prints a message when they get clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool RepeatButton(Rect position, GUIContent content)
        {
            return DoRepeatButton(position, content, s_Skin.button, FocusType.Passive);
        }

        ///<summary>Make a button that is active as long as the user holds it down.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>True when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // Prints a message when they get clicked.
        ///
        /// // Prints a message when they get clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool RepeatButton(Rect position, string text, GUIStyle style)
        {
            return DoRepeatButton(position, GUIContent.Temp(text), style, FocusType.Passive);
        }

        ///<summary>Make a button that is active as long as the user holds it down.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>True when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // Prints a message when they get clicked.
        ///
        /// // Prints a message when they get clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool RepeatButton(Rect position, Texture image, GUIStyle style)
        {
            return DoRepeatButton(position, GUIContent.Temp(image), style, FocusType.Passive);
        }

        ///<summary>Make a button that is active as long as the user holds it down.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>True when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 buttons, one with an image, and other with a text
        /// // Prints a message when they get clicked.
        ///
        /// // Prints a message when they get clicked.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Texture btnTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!btnTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture on the inspector");
        ///            return;
        ///        }
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 10, 50, 50), btnTexture))
        ///            Debug.Log("Clicked the button with an image");
        ///
        ///        if (GUI.RepeatButton(new Rect(10, 70, 50, 30), "Click"))
        ///            Debug.Log("Clicked the button with text");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool RepeatButton(Rect position, GUIContent content, GUIStyle style)
        {
            return DoRepeatButton(position, content, style, FocusType.Passive);
        }

        private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_RepeatButtonHash, focusType, position);
            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // If the mouse is inside the button, we say that we're the hot control
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    return false;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the button or not)
                        Event.current.Use();

                        // But we only return true if the button was actually clicked
                        return position.Contains(Event.current.mousePosition);
                    }
                    return false;
                case EventType.Repaint:
                    style.Draw(position, content, id, false, position.Contains(Event.current.mousePosition));
                    return id == GUIUtility.hotControl && position.Contains(Event.current.mousePosition);
            }
            return false;
        }

        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUI.TextField(new Rect(10, 10, 200, 20), stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextField(Rect position, string text)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, -1, GUI.skin.textField);
            return t.text;
        }

        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUI.TextField(new Rect(10, 10, 200, 20), stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextField(Rect position, string text, int maxLength)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, GUI.skin.textField);
            return t.text;
        }

        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUI.TextField(new Rect(10, 10, 200, 20), stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextField(Rect position, string text, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, -1, style);
            return t.text;
        }

        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUI.TextField(new Rect(10, 10, 200, 20), stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextField(Rect position, string text, int maxLength, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, style);
            return t.text;
        }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        passwordToEdit = GUI.PasswordField(new Rect(10, 10, 200, 20), passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(Rect position, string password, char maskChar)
        {
            return PasswordField(position, password, maskChar, -1, GUI.skin.textField);
        }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        passwordToEdit = GUI.PasswordField(new Rect(10, 10, 200, 20), passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(Rect position, string password, char maskChar, int maxLength)
        {
            return PasswordField(position, password, maskChar, maxLength, GUI.skin.textField);
        }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        passwordToEdit = GUI.PasswordField(new Rect(10, 10, 200, 20), passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(Rect position, string password, char maskChar, GUIStyle style)
        {
            return PasswordField(position, password, maskChar, -1, style);
        }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        passwordToEdit = GUI.PasswordField(new Rect(10, 10, 200, 20), passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(Rect position, string password, char maskChar, int maxLength, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();

            string strPassword = PasswordFieldGetStrToShow(password, maskChar);
            GUIContent t = GUIContent.Temp(strPassword);

            bool oldGUIChanged = GUI.changed;
            GUI.changed = false;

            if (TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed)
                DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard), t, false, maxLength, style, password, maskChar);
            else
                DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, style);

            strPassword = GUI.changed ? t.text : password;

            GUI.changed |= oldGUIChanged;

            return strPassword;
        }

        ///<exclude />
        internal static string PasswordFieldGetStrToShow(string password, char maskChar)
        {
            return (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown) ?
                "".PadRight(password.Length, maskChar) : password;
        }

        ///<summary>Make a Multi-line text area where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUI.TextArea(new Rect(10, 10, 200, 100), stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextArea(Rect position, string text)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, true, -1, GUI.skin.textArea);
            return t.text;
        }

        ///<summary>Make a Multi-line text area where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUI.TextArea(new Rect(10, 10, 200, 100), stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextArea(Rect position, string text, int maxLength)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, true, maxLength, GUI.skin.textArea);
            return t.text;
        }

        ///<summary>Make a Multi-line text area where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="style">The style to use. If left out, the <c>textArea</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUI.TextArea(new Rect(10, 10, 200, 100), stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextArea(Rect position, string text, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, true, -1, style);
            return t.text;
        }

        ///<summary>Make a Multi-line text area where the user can edit a string.</summary>
        ///<param name="position">Rectangle on the screen to use for the text field.</param>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="style">The style to use. If left out, the <c>textArea</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUI.TextArea(new Rect(10, 10, 200, 100), stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string TextArea(Rect position, string text, int maxLength, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, true, maxLength, style);
            return t.text;
        }

        internal static void DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style)
        {
            DoTextField(position, id, content, multiline, maxLength, style, null);
        }

        internal static void DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style, string secureText)
        {
            DoTextField(position, id, content, multiline, maxLength, style, secureText, '\0');
        }

        internal static void DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style, string secureText, char maskChar)
        {
            GUIUtility.CheckOnGUI();

            //Pre-cull input string to maxLength.
            if (maxLength >= 0 && content.text.Length > maxLength)
                content.text = content.text.Substring(0, maxLength);


            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), id);
            editor.text = content.text;
            editor.SaveBackup();
            editor.position = position;
            editor.style = style;
            editor.isMultiline = multiline;
            editor.controlID = id;
            editor.DetectFocusChange();

            if (TouchScreenKeyboard.isSupported && !TouchScreenKeyboard.isInPlaceEditingAllowed)
            {
                HandleTextFieldEventForTouchscreen(position, id, content, multiline, maxLength, style, secureText, maskChar, editor);
            }
            else // Not supported means we have a physical keyboard attached
            {
                HandleTextFieldEventForDesktop(position, id, content, multiline, maxLength, style, editor);
            }

            // Scroll offset might need to be updated
            editor.UpdateScrollOffsetIfNeeded(Event.current);
        }

        private static void HandleTextFieldEventForTouchscreen(Rect position, int id, GUIContent content, bool multiline, int maxLength,
            GUIStyle style, string secureText, char maskChar, TextEditor editor)
        {
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;

                        // Disable keyboard for previously active text field, if any
                        if (s_HotTextField != -1 && s_HotTextField != id)
                        {
                            TextEditor currentEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), s_HotTextField);
                            currentEditor.keyboardOnScreen = null;
                        }

                        s_HotTextField = id;

                        // in player setting keyboard control calls OnFocus every time, don't want that. In editor it does not do that for some reason
                        if (GUIUtility.keyboardControl != id)
                            GUIUtility.keyboardControl = id;

                        editor.keyboardOnScreen = TouchScreenKeyboard.Open(
                            secureText ?? content.text,
                            TouchScreenKeyboardType.Default,
                            true,     // autocorrection
                            multiline,
                            (secureText != null));

                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (editor.keyboardOnScreen != null)
                    {
                        content.text = editor.keyboardOnScreen.text;
                        if (maxLength >= 0 && content.text.Length > maxLength)
                            content.text = content.text.Substring(0, maxLength);

                        if (editor.keyboardOnScreen.status != TouchScreenKeyboard.Status.Visible)
                        {
                            editor.keyboardOnScreen = null;
                            changed = true;
                        }
                    }

                    // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                    // so before drawing make sure we hide them ourselves
                    string clearText = content.text;

                    if (secureText != null)
                        content.text = PasswordFieldGetStrToShow(clearText, maskChar);

                    style.Draw(position, content, id, false);
                    content.text = clearText;

                    break;
            }
        }

        private static void HandleTextFieldEventForDesktop(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style, TextEditor editor)
        {
            var evt = Event.current;

            bool change = false;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        editor.m_HasFocus = true;
                        editor.MoveCursorToPosition(Event.current.mousePosition);
                        if (Event.current.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
                        {
                            editor.SelectCurrentWord();
                            editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
                            editor.MouseDragSelectsWholeWords(true);
                        }
                        if (Event.current.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
                        {
                            editor.SelectCurrentParagraph();
                            editor.MouseDragSelectsWholeWords(true);
                            editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
                        }
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (evt.shift)
                            editor.MoveCursorToPosition(Event.current.mousePosition);
                        else
                            editor.SelectToPosition(Event.current.mousePosition);
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        editor.MouseDragSelectsWholeWords(false);
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl != id)
                        return;

                    if (editor.HandleKeyEvent(evt))
                    {
                        evt.Use();
                        change = true;
                        content.text = editor.text;
                        break;
                    }

                    // Ignore tab & shift-tab in textfields
                    if (evt.keyCode == KeyCode.Tab || evt.character == '\t')
                        return;

                    char c = evt.character;

                    if (c == '\n' && !multiline && !evt.alt)
                        return;


                    // Simplest test: only allow the character if the display font supports it.
                    Font font = style.font;
                    if (!font)
                        font = GUI.skin.font;

                    if (font.HasCharacter(c) || c == '\n')
                    {
                        editor.Insert(c);
                        change = true;
                        break;
                    }

                    // On windows, keypresses also send events with keycode but no character. Eat them up here.
                    if (c == 0)
                    {
                        // if we have a composition string, make sure we clear the previous selection.
                        if (GUIUtility.compositionString.Length > 0)
                        {
                            editor.ReplaceSelection("");
                            change = true;
                        }

                        evt.Use();
                    }
                    //              else {
                    // REALLY USEFUL:
                    //              Debug.Log ("unhandled " +evt);
                    //              evt.Use ();
                    //          }
                    break;
                case EventType.Repaint:
                    // If we have keyboard focus, draw the cursor
                    // TODO:    check if this OpenGL view has keyboard focus
                    editor.UpdateTextHandle();
                    if (GUIUtility.keyboardControl != id)
                    {
                        style.Draw(position, content, id, false);
                    }
                    else
                    {
                        editor.DrawCursor(content.text);
                    }
                    break;
            }

            if (GUIUtility.keyboardControl == id)
                GUIUtility.textFieldInput = true;

            if (change)
            {
                changed = true;
                content.text = editor.text;
                if (maxLength >= 0 && content.text.Length > maxLength)
                    content.text = content.text.Substring(0, maxLength);
                evt.Use();
            }
        }

        ///<summary>Make an on/off toggle button.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="value">Is this button on or off?</param>
        ///<param name="text">Text to display on the button.</param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 toggle controls, one with a text, the other with an image.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    private bool toggleTxt = false;
        ///    private bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        toggleTxt = GUI.Toggle(new Rect(10, 10, 100, 30), toggleTxt, "A Toggle text");
        ///        toggleImg = GUI.Toggle(new Rect(10, 50, 50, 50), toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Toggle(Rect position, bool value, string text)
        {
            return Toggle(position, value, GUIContent.Temp(text), s_Skin.toggle);
        }

        ///<summary>Make an on/off toggle button.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="value">Is this button on or off?</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 toggle controls, one with a text, the other with an image.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    private bool toggleTxt = false;
        ///    private bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        toggleTxt = GUI.Toggle(new Rect(10, 10, 100, 30), toggleTxt, "A Toggle text");
        ///        toggleImg = GUI.Toggle(new Rect(10, 50, 50, 50), toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Toggle(Rect position, bool value, Texture image)
        {
            return Toggle(position, value, GUIContent.Temp(image), s_Skin.toggle);
        }

        ///<summary>Make an on/off toggle button.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="value">Is this button on or off?</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 toggle controls, one with a text, the other with an image.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    private bool toggleTxt = false;
        ///    private bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        toggleTxt = GUI.Toggle(new Rect(10, 10, 100, 30), toggleTxt, "A Toggle text");
        ///        toggleImg = GUI.Toggle(new Rect(10, 50, 50, 50), toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Toggle(Rect position, bool value, GUIContent content)
        {
            return Toggle(position, value, content, s_Skin.toggle);
        }

        ///<summary>Make an on/off toggle button.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="value">Is this button on or off?</param>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>toggle</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 toggle controls, one with a text, the other with an image.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    private bool toggleTxt = false;
        ///    private bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        toggleTxt = GUI.Toggle(new Rect(10, 10, 100, 30), toggleTxt, "A Toggle text");
        ///        toggleImg = GUI.Toggle(new Rect(10, 50, 50, 50), toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Toggle(Rect position, bool value, string text, GUIStyle style)
        {
            return Toggle(position, value, GUIContent.Temp(text), style);
        }

        ///<summary>Make an on/off toggle button.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="value">Is this button on or off?</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>toggle</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 toggle controls, one with a text, the other with an image.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    private bool toggleTxt = false;
        ///    private bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        toggleTxt = GUI.Toggle(new Rect(10, 10, 100, 30), toggleTxt, "A Toggle text");
        ///        toggleImg = GUI.Toggle(new Rect(10, 50, 50, 50), toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Toggle(Rect position, bool value, Texture image, GUIStyle style)
        {
            return Toggle(position, value, GUIContent.Temp(image), style);
        }

        ///<summary>Make an on/off toggle button.</summary>
        ///<param name="position">Rectangle on the screen to use for the button.</param>
        ///<param name="value">Is this button on or off?</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="style">The style to use. If left out, the <c>toggle</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws 2 toggle controls, one with a text, the other with an image.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Texture aTexture;
        ///
        ///    private bool toggleTxt = false;
        ///    private bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///
        ///        toggleTxt = GUI.Toggle(new Rect(10, 10, 100, 30), toggleTxt, "A Toggle text");
        ///        toggleImg = GUI.Toggle(new Rect(10, 50, 50, 50), toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool Toggle(Rect position, bool value, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoToggle(position, GUIUtility.GetControlID(s_ToggleHash, FocusType.Passive, position), value, content, style);
        }

        public static bool Toggle(Rect position, int id, bool value, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoToggle(position, id, value, content, style);
        }

        ///<summary>Determines how toolbar button size is calculated.</summary>
        public enum ToolbarButtonSize
        {
            ///<summary>Calculates the button size by dividing the available width by the number of buttons. The minimum size is the maximum content width.</summary>
            Fixed,
            ///<summary>The width of each toolbar button is calculated based on the width of its content.</summary>
            FitToContents
        }

        ///<summary>Make a toolbar.</summary>
        ///<param name="position">Rectangle on the screen to use for the toolbar.</param>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="texts">An array of strings to show on the toolbar buttons.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int toolbarInt = 0;
        ///    public string[] toolbarStrings = new string[] {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUI.Toolbar(new Rect(25, 25, 250, 30), toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int Toolbar(Rect position, int selected, string[] texts)
        {
            return Toolbar(position, selected, GUIContent.Temp(texts), s_Skin.button);
        }

        ///<summary>Make a toolbar.</summary>
        ///<param name="position">Rectangle on the screen to use for the toolbar.</param>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="images">An array of textures on the toolbar buttons.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int toolbarInt = 0;
        ///    public string[] toolbarStrings = new string[] {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUI.Toolbar(new Rect(25, 25, 250, 30), toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int Toolbar(Rect position, int selected, Texture[] images)
        {
            return Toolbar(position, selected, GUIContent.Temp(images), s_Skin.button);
        }

        ///<summary>Make a toolbar.</summary>
        ///<param name="position">Rectangle on the screen to use for the toolbar.</param>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="contents">An array of text, image and tooltips for the toolbar buttons.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int toolbarInt = 0;
        ///    public string[] toolbarStrings = new string[] {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUI.Toolbar(new Rect(25, 25, 250, 30), toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int Toolbar(Rect position, int selected, GUIContent[] contents)
        {
            return Toolbar(position, selected, contents, s_Skin.button);
        }

        ///<summary>Make a toolbar.</summary>
        ///<param name="position">Rectangle on the screen to use for the toolbar.</param>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="texts">An array of strings to show on the toolbar buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int toolbarInt = 0;
        ///    public string[] toolbarStrings = new string[] {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUI.Toolbar(new Rect(25, 25, 250, 30), toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int Toolbar(Rect position, int selected, string[] texts, GUIStyle style)
        {
            return Toolbar(position, selected, GUIContent.Temp(texts), style);
        }

        ///<summary>Make a toolbar.</summary>
        ///<param name="position">Rectangle on the screen to use for the toolbar.</param>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="images">An array of textures on the toolbar buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int toolbarInt = 0;
        ///    public string[] toolbarStrings = new string[] {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUI.Toolbar(new Rect(25, 25, 250, 30), toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int Toolbar(Rect position, int selected, Texture[] images, GUIStyle style)
        {
            return Toolbar(position, selected, GUIContent.Temp(images), style);
        }

        ///<summary>Make a toolbar.</summary>
        ///<param name="position">Rectangle on the screen to use for the toolbar.</param>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="contents">An array of text, image and tooltips for the toolbar buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int toolbarInt = 0;
        ///    public string[] toolbarStrings = new string[] {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUI.Toolbar(new Rect(25, 25, 250, 30), toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int Toolbar(Rect position, int selected, GUIContent[] contents, GUIStyle style)
        {
            return Toolbar(position, selected, contents, null, style, ToolbarButtonSize.Fixed);
        }

        ///<summary>Make a toolbar.</summary>
        ///<param name="position">Rectangle on the screen to use for the toolbar.</param>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="contents">An array of text, image and tooltips for the toolbar buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="buttonSize">Determines how toolbar button size is calculated.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int toolbarInt = 0;
        ///    public string[] toolbarStrings = new string[] {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUI.Toolbar(new Rect(25, 25, 250, 30), toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int Toolbar(Rect position, int selected, GUIContent[] contents, GUIStyle style, ToolbarButtonSize buttonSize)
        {
            return Toolbar(position, selected, contents, null, style, buttonSize);
        }

        internal static int Toolbar(Rect position, int selected, GUIContent[] contents, string[] controlNames, GUIStyle style, ToolbarButtonSize buttonSize, bool[] contentsEnabled = null)
        {
            // Get the styles here
            GUIStyle firstStyle, midStyle, lastStyle;
            FindStyles(ref style, out firstStyle, out midStyle, out lastStyle, "left", "mid", "right");

            return Toolbar(position, selected, contents, controlNames, style, firstStyle, midStyle, lastStyle, buttonSize, contentsEnabled);
        }

        internal static int Toolbar(Rect position, int selected, GUIContent[] contents, string[] controlNames, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, ToolbarButtonSize buttonSize, bool[] contentsEnabled = null)
        {
            GUIUtility.CheckOnGUI();

            return DoButtonGrid(position, selected, contents, controlNames, contents.Length, style, firstStyle, midStyle, lastStyle, buttonSize, contentsEnabled);
        }

        ///<summary>Make a grid of buttons.</summary>
        ///<param name="position">Rectangle on the screen to use for the grid.</param>
        ///<param name="selected">The index of the selected grid button.</param>
        ///<param name="texts">An array of strings to show on the grid buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The controls will be scaled to fit unless the style defines a fixedWidth to use.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int selGridInt = 0;
        ///    public string[] selStrings = new string[] {"Grid 1", "Grid 2", "Grid 3", "Grid 4"};
        ///
        ///    void OnGUI()
        ///    {
        ///        // use 2 elements in the horizontal direction
        ///        selGridInt = GUI.SelectionGrid(new Rect(25, 25, 100, 30), selGridInt, selStrings, 2);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int SelectionGrid(Rect position, int selected, string[] texts, int xCount)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(texts), xCount, null);
        }

        ///<summary>Make a grid of buttons.</summary>
        ///<param name="position">Rectangle on the screen to use for the grid.</param>
        ///<param name="selected">The index of the selected grid button.</param>
        ///<param name="images">An array of textures on the grid buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The controls will be scaled to fit unless the style defines a fixedWidth to use.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int selGridInt = 0;
        ///    public string[] selStrings = new string[] {"Grid 1", "Grid 2", "Grid 3", "Grid 4"};
        ///
        ///    void OnGUI()
        ///    {
        ///        // use 2 elements in the horizontal direction
        ///        selGridInt = GUI.SelectionGrid(new Rect(25, 25, 100, 30), selGridInt, selStrings, 2);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int SelectionGrid(Rect position, int selected, Texture[] images, int xCount)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(images), xCount, null);
        }

        ///<summary>Make a grid of buttons.</summary>
        ///<param name="position">Rectangle on the screen to use for the grid.</param>
        ///<param name="selected">The index of the selected grid button.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The controls will be scaled to fit unless the style defines a fixedWidth to use.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int selGridInt = 0;
        ///    public string[] selStrings = new string[] {"Grid 1", "Grid 2", "Grid 3", "Grid 4"};
        ///
        ///    void OnGUI()
        ///    {
        ///        // use 2 elements in the horizontal direction
        ///        selGridInt = GUI.SelectionGrid(new Rect(25, 25, 100, 30), selGridInt, selStrings, 2);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int SelectionGrid(Rect position, int selected, GUIContent[] content, int xCount)
        {
            return SelectionGrid(position, selected, content, xCount, null);
        }

        ///<summary>Make a grid of buttons.</summary>
        ///<param name="position">Rectangle on the screen to use for the grid.</param>
        ///<param name="selected">The index of the selected grid button.</param>
        ///<param name="texts">An array of strings to show on the grid buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The controls will be scaled to fit unless the style defines a fixedWidth to use.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int selGridInt = 0;
        ///    public string[] selStrings = new string[] {"Grid 1", "Grid 2", "Grid 3", "Grid 4"};
        ///
        ///    void OnGUI()
        ///    {
        ///        // use 2 elements in the horizontal direction
        ///        selGridInt = GUI.SelectionGrid(new Rect(25, 25, 100, 30), selGridInt, selStrings, 2);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int SelectionGrid(Rect position, int selected, string[] texts, int xCount, GUIStyle style)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(texts), xCount, style);
        }

        ///<summary>Make a grid of buttons.</summary>
        ///<param name="position">Rectangle on the screen to use for the grid.</param>
        ///<param name="selected">The index of the selected grid button.</param>
        ///<param name="images">An array of textures on the grid buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The controls will be scaled to fit unless the style defines a fixedWidth to use.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int selGridInt = 0;
        ///    public string[] selStrings = new string[] {"Grid 1", "Grid 2", "Grid 3", "Grid 4"};
        ///
        ///    void OnGUI()
        ///    {
        ///        // use 2 elements in the horizontal direction
        ///        selGridInt = GUI.SelectionGrid(new Rect(25, 25, 100, 30), selGridInt, selStrings, 2);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int SelectionGrid(Rect position, int selected, Texture[] images, int xCount, GUIStyle style)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(images), xCount, style);
        }

        ///<summary>Make a grid of buttons.</summary>
        ///<param name="position">Rectangle on the screen to use for the grid.</param>
        ///<param name="selected">The index of the selected grid button.</param>
        ///<param name="contents">An array of text, image and tooltips for the grid button.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The controls will be scaled to fit unless the style defines a fixedWidth to use.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public int selGridInt = 0;
        ///    public string[] selStrings = new string[] {"Grid 1", "Grid 2", "Grid 3", "Grid 4"};
        ///
        ///    void OnGUI()
        ///    {
        ///        // use 2 elements in the horizontal direction
        ///        selGridInt = GUI.SelectionGrid(new Rect(25, 25, 100, 30), selGridInt, selStrings, 2);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static int SelectionGrid(Rect position, int selected, GUIContent[] contents, int xCount, GUIStyle style)
        {
            if (style == null) style = s_Skin.button;
            return DoButtonGrid(position, selected, contents, null, xCount, style, style, style, style, ToolbarButtonSize.Fixed);
        }

        // Find many GUIStyles from style.name permutations (Helper function for toolbars).
        internal static void FindStyles(ref GUIStyle style, out GUIStyle firstStyle, out GUIStyle midStyle, out GUIStyle lastStyle, string first, string mid, string last)
        {
            if (style == null)
                style = GUI.skin.button;
            string baseName = style.name;
            midStyle = GUI.skin.FindStyle(baseName + mid) ?? style;
            firstStyle = GUI.skin.FindStyle(baseName + first) ?? midStyle;
            lastStyle = GUI.skin.FindStyle(baseName + last) ?? midStyle;
        }

        internal static int CalcTotalHorizSpacing(int xCount, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle)
        {
            if (xCount < 2)
                return 0;
            if (xCount == 2)
                return Mathf.Max(firstStyle.margin.right, lastStyle.margin.left);

            int internalSpace = Mathf.Max(midStyle.margin.left, midStyle.margin.right);
            return Mathf.Max(firstStyle.margin.right, midStyle.margin.left) + Mathf.Max(midStyle.margin.right, lastStyle.margin.left) + internalSpace * (xCount - 3);
        }

        internal static bool DoControl(Rect position, int id, bool on, bool hover, GUIContent content, GUIStyle style)
        {
            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    style.Draw(position, content, id, on, hover);
                    break;
                case EventType.MouseDown:
                    if (GUIUtility.HitTest(position, evt))
                    {
                        GrabMouseControl(id);
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    bool anyModifiers = (evt.alt || evt.shift || evt.command || evt.control);
                    if ((evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !anyModifiers && GUIUtility.keyboardControl == id)
                    {
                        evt.Use();
                        changed = true;
                        return !on;
                    }
                    break;
                case EventType.MouseUp:
                    if (HasMouseControl(id))
                    {
                        ReleaseMouseControl();
                        evt.Use();
                        if (GUIUtility.HitTest(position, evt))
                        {
                            changed = true;
                            return !on;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (HasMouseControl(id))
                        evt.Use();
                    break;
            }
            return on;
        }

        private static void DoLabel(Rect position, GUIContent content, GUIStyle style)
        {
            var evt = Event.current;
            if (evt.type != EventType.Repaint)
                return;
            bool hovered = position.Contains(evt.mousePosition);
            style.Draw(position, content, hovered, false, false, false);

            // Is inside label AND inside guiclip visible rect (prevents tooltips on labels that are clipped)
            if (!String.IsNullOrEmpty(content.tooltip) && hovered && GUIClip.visibleRect.Contains(evt.mousePosition))
            {
                if (!GUIStyle.IsTooltipActive(content.tooltip))
                    s_ToolTipRect = new Rect(evt.mousePosition, Vector2.zero);
                GUIStyle.SetMouseTooltip(content.tooltip, s_ToolTipRect);
            }
        }

        internal static bool DoToggle(Rect position, int id, bool value, GUIContent content, GUIStyle style)
        {
            return DoControl(position, id, value, position.Contains(Event.current.mousePosition), content, style);
        }

        internal static bool DoButton(Rect position, int id, GUIContent content, GUIStyle style)
        {
            return DoControl(position, id, false, position.Contains(Event.current.mousePosition), content, style);
        }

        ///<exclude />
        internal delegate void CustomSelectionGridItemGUI(int item, Rect rect, GUIStyle style, int controlID);

        private static Rect[] CalcGridRectsFixedWidthFixedMargin(Rect position, int itemCount, int itemsPerRow, float elemWidth, float elemHeight, float spacingHorizontal, float spacingVertical)
        {
            int x = 0;
            float xPos = position.xMin, yPos = position.yMin;
            Rect[] retval = new Rect[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                retval[i] = new Rect(xPos, yPos, elemWidth, elemHeight);

                //we round the values to the dpi-aware pixel grid
                retval[i] = GUIUtility.AlignRectToDevice(retval[i]);

                xPos = retval[i].xMax + spacingHorizontal;

                if (++x >= itemsPerRow)
                {
                    x = 0;
                    yPos += elemHeight + spacingVertical;
                    xPos = position.xMin;
                }
            }
            return retval;
        }

        internal static int DoCustomSelectionGrid(Rect position, int selected, int itemCount, CustomSelectionGridItemGUI itemGUI, int itemsPerRow, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            if (itemCount == 0)
                return selected;
            if (itemsPerRow <= 0)
            {
                Debug.LogWarning("You are trying to create a SelectionGrid with zero or less elements to be displayed in the horizontal direction. Set itemsPerRow to a positive value.");
                return selected;
            }

            // Figure out how large each element should be
            int rows = (itemCount + itemsPerRow - 1) / itemsPerRow;
            float horizontalSpacing = Mathf.Max(style.margin.left, style.margin.right);
            float verticalSpacing = Mathf.Max(style.margin.top, style.margin.bottom);
            float elemWidth = style.fixedWidth != 0 ? style.fixedWidth : (position.width - CalcTotalHorizSpacing(itemsPerRow, style, style, style, style)) / itemsPerRow;
            float elemHeight = style.fixedHeight != 0 ? style.fixedHeight : (position.height - verticalSpacing * (rows - 1)) / rows;

            Rect[] buttonRects = CalcGridRectsFixedWidthFixedMargin(position, itemCount, itemsPerRow, elemWidth, elemHeight, horizontalSpacing, verticalSpacing);
            int selectedButtonControlID = 0;
            for (int buttonIndex = 0; buttonIndex < itemCount; ++buttonIndex)
            {
                var buttonRect = buttonRects[buttonIndex];

                var id = GUIUtility.GetControlID(s_ButtonGridHash, FocusType.Passive, buttonRect);
                if (buttonIndex == selected)
                    selectedButtonControlID = id;

                var evtType = Event.current.GetTypeForControl(id);
                switch (evtType)
                {
                    case EventType.MouseDown:
                        if (GUIUtility.HitTest(buttonRect, Event.current))
                        {
                            GUIUtility.hotControl = id;
                            Event.current.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id)
                            Event.current.Use();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();

                            GUI.changed = true;
                            return buttonIndex;
                        }
                        break;
                    case EventType.Repaint:
                        if (selected != buttonIndex)
                            itemGUI(buttonIndex, buttonRect, style, id);
                        break;
                }

                if (evtType != EventType.Repaint || selected != buttonIndex)
                    itemGUI(buttonIndex, buttonRect, style, id);
            }

            // draw selected button at the end so it overflows nicer
            if (selected >= 0 && selected < itemCount && Event.current.type == EventType.Repaint)
                itemGUI(selected, buttonRects[selected], style, selectedButtonControlID);

            return selected;
        }

        // Make a button grid
        private static int DoButtonGrid(Rect position, int selected, GUIContent[] contents, string[] controlNames, int itemsPerRow, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, ToolbarButtonSize buttonSize, bool[] contentsEnabled = null)
        {
            GUIUtility.CheckOnGUI();
            int itemCount = contents.Length;
            if (itemCount == 0)
                return selected;
            if (itemsPerRow <= 0)
            {
                Debug.LogWarning("You are trying to create a SelectionGrid with zero or less elements to be displayed in the horizontal direction. Set itemsPerRow to a positive value.");
                return selected;
            }

            if (contentsEnabled != null && contentsEnabled.Length != itemCount)
                throw new ArgumentException("contentsEnabled");

            // Figure out how large each element should be
            int rows = (itemCount + itemsPerRow - 1) / itemsPerRow;
            float elemWidth = style.fixedWidth != 0 ? style.fixedWidth : (position.width - CalcTotalHorizSpacing(itemsPerRow, style, firstStyle, midStyle, lastStyle)) / itemsPerRow;
            float elemHeight = style.fixedHeight != 0 ? style.fixedHeight : (position.height - Mathf.Max(style.margin.top, style.margin.bottom) * (rows - 1)) / rows;

            Rect[] buttonRects = CalcGridRects(position, contents, itemsPerRow, elemWidth, elemHeight, style, firstStyle, midStyle, lastStyle, buttonSize);
            GUIStyle selectedButtonStyle = null;
            int selectedButtonControlID = 0;
            for (int buttonIndex = 0; buttonIndex < itemCount; ++buttonIndex)
            {
                bool wasEnabled = enabled;
                enabled &= (contentsEnabled == null || contentsEnabled[buttonIndex]);
                var buttonRect = buttonRects[buttonIndex];
                var content = contents[buttonIndex];

                if (controlNames != null)
                    GUI.SetNextControlName(controlNames[buttonIndex]);
                var id = GUIUtility.GetControlID(s_ButtonGridHash, FocusType.Passive, buttonRect);
                if (buttonIndex == selected)
                    selectedButtonControlID = id;

                switch (Event.current.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (GUIUtility.HitTest(buttonRect, Event.current))
                        {
                            GUIUtility.hotControl = id;
                            Event.current.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id)
                            Event.current.Use();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();

                            GUI.changed = true;
                            return buttonIndex;
                        }
                        break;
                    case EventType.Repaint:
                        var buttonStyle = itemCount == 1 ? style : (buttonIndex == 0 ? firstStyle : (buttonIndex == itemCount - 1 ? lastStyle : midStyle));
                        var isMouseOver = buttonRect.Contains(Event.current.mousePosition);
                        var isHotControl = GUIUtility.hotControl == id;
                        var isSelected = selected == buttonIndex;

                        if (!isSelected)
                        {
                            if (buttonRect.Overlaps(GUIClip.visibleRect))
                            {
                                buttonStyle.Draw(buttonRect, content, enabled && isMouseOver && (isHotControl || GUIUtility.hotControl == 0), enabled && isHotControl, false, false);
                            }
                        }
                        else
                        {
                            selectedButtonStyle = buttonStyle;
                        }

                        if (isMouseOver)
                        {
                            GUIUtility.mouseUsed = true;
                            if (!string.IsNullOrEmpty(content.tooltip))
                                GUIStyle.SetMouseTooltip(content.tooltip, buttonRect);
                        }
                        break;
                }

                enabled = wasEnabled;
            }

            // draw selected button at the end so it overflows nicer
            if (selectedButtonStyle != null)
            {
                var buttonRect = buttonRects[selected];
                var content = contents[selected];
                var isMouseOver = buttonRect.Contains(Event.current.mousePosition);
                var isHotControl = GUIUtility.hotControl == selectedButtonControlID;
                var wasEnabled = enabled;
                enabled &= (contentsEnabled == null || contentsEnabled[selected]);
                if (buttonRect.Overlaps(GUIClip.visibleRect))
                {
                    selectedButtonStyle.Draw(buttonRect, content, enabled && isMouseOver && (isHotControl || GUIUtility.hotControl == 0), enabled && isHotControl, true, false);
                }
                enabled = wasEnabled;
            }

            return selected;
        }

        // Helper function: Get all mouse rects
        private static Rect[] CalcGridRects(Rect position, GUIContent[] contents, int xCount, float elemWidth, float elemHeight, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, ToolbarButtonSize buttonSize)
        {
            int count = contents.Length;
            int x = 0;
            float xPos = position.xMin, yPos = position.yMin;
            GUIStyle currentStyle = style;
            Rect[] retval = new Rect[count];
            if (count > 1)
                currentStyle = firstStyle;
            for (int i = 0; i < count; i++)
            {
                float w = 0;
                switch (buttonSize)
                {
                    case ToolbarButtonSize.Fixed:
                        w = elemWidth;
                        break;
                    case ToolbarButtonSize.FitToContents:
                        w = currentStyle.CalcSize(contents[i]).x;
                        break;
                }

                retval[i] = new Rect(xPos, yPos, w, elemHeight);

                //we round the values to the dpi-aware pixel grid
                retval[i] = GUIUtility.AlignRectToDevice(retval[i]);

                GUIStyle nextStyle = midStyle;
                if (i == count - 2 || i == xCount - 2)
                    nextStyle = lastStyle;

                xPos = retval[i].xMax + Mathf.Max(currentStyle.margin.right, nextStyle.margin.left);

                x++;
                if (x >= xCount)
                {
                    x = 0;
                    yPos += elemHeight + Mathf.Max(style.margin.top, style.margin.bottom);
                    xPos = position.xMin;
                    nextStyle = firstStyle;
                }

                currentStyle = nextStyle;
            }
            return retval;
        }

        ///<summary>A horizontal slider the user can drag to change a value between a min and a max.</summary>
        ///<param name="position">Rectangle on the screen to use for the slider.</param>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="leftValue">The value at the left end of the slider.</param>
        ///<param name="rightValue">The value at the right end of the slider.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a horizontal slider control that goes from 0 to 10.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float hSliderValue = 0.0F;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), hSliderValue, 0.0F, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue)
        {
            return Slider(position, value, 0, leftValue, rightValue, skin.horizontalSlider, skin.horizontalSliderThumb, true, 0, skin.horizontalSliderThumbExtent);
        }

        ///<summary>A horizontal slider the user can drag to change a value between a min and a max.</summary>
        ///<param name="position">Rectangle on the screen to use for the slider.</param>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="leftValue">The value at the left end of the slider.</param>
        ///<param name="rightValue">The value at the right end of the slider.</param>
        ///<param name="slider">The <see cref="GUIStyle" /> to use for displaying the dragging area. If left out, the <c>horizontalSlider</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="thumb">The <see cref="GUIStyle" /> to use for displaying draggable thumb. If left out, the <c>horizontalSliderThumb</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a horizontal slider control that goes from 0 to 10.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float hSliderValue = 0.0F;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSliderValue = GUI.HorizontalSlider(new Rect(25, 25, 100, 30), hSliderValue, 0.0F, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb)
        {
            return Slider(position, value, 0, leftValue, rightValue, slider, thumb, true, 0, null);
        }

        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle thumbExtent)
        {
            return Slider(position, value, 0, leftValue, rightValue, slider, thumb, true, 0, (thumbExtent == null && thumb == GUI.skin.horizontalSliderThumb) ? GUI.skin.horizontalSliderThumbExtent : thumbExtent);
        }

        ///<summary>A vertical slider the user can drag to change a value between a min and a max.</summary>
        ///<param name="position">Rectangle on the screen to use for the slider.</param>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="topValue">The value at the top end of the slider.</param>
        ///<param name="bottomValue">The value at the bottom end of the slider.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a vertical slider control that goes from  10 (top) to 0 (bottom)
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float vSliderValue = 0.0f;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSliderValue = GUI.VerticalSlider(new Rect(25, 25, 100, 30), vSliderValue, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue)
        {
            return Slider(position, value, 0, topValue, bottomValue, skin.verticalSlider, skin.verticalSliderThumb, false, 0, skin.verticalSliderThumbExtent);
        }

        ///<summary>A vertical slider the user can drag to change a value between a min and a max.</summary>
        ///<param name="position">Rectangle on the screen to use for the slider.</param>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="topValue">The value at the top end of the slider.</param>
        ///<param name="bottomValue">The value at the bottom end of the slider.</param>
        ///<param name="slider">The <see cref="GUIStyle" /> to use for displaying the dragging area. If left out, the <c>horizontalSlider</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="thumb">The <see cref="GUIStyle" /> to use for displaying draggable thumb. If left out, the <c>horizontalSliderThumb</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a vertical slider control that goes from  10 (top) to 0 (bottom)
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float vSliderValue = 0.0f;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSliderValue = GUI.VerticalSlider(new Rect(25, 25, 100, 30), vSliderValue, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue, GUIStyle slider, GUIStyle thumb)
        {
            return Slider(position, value, 0, topValue, bottomValue, slider, thumb, false, 0, null);
        }

        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue, GUIStyle slider, GUIStyle thumb, GUIStyle thumbExtent)
        {
            return Slider(position, value, 0, topValue, bottomValue, slider, thumb, false, 0, (thumbExtent == null && thumb == GUI.skin.verticalSliderThumb) ? GUI.skin.verticalSliderThumbExtent : thumbExtent);
        }

        // Main slider function.
        // Handles scrollbars & sliders in both horizontal & vertical directions.
        /// <exclude/>
        public static float Slider(Rect position, float value, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id, GUIStyle thumbExtent = null)
        {
            GUIUtility.CheckOnGUI();
            if (id == 0)
            {
                id = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive, position);
            }
            return new SliderHandler(position, value, size, start, end, slider, thumb, horiz, id, thumbExtent).Handle();
        }

        ///<summary>Make a horizontal scrollbar. Scrollbars are what you use to scroll through a document. Most likely, you want to use scrollViews instead.</summary>
        ///<remarks>**Finding extra elements:**
        ///
        ///The styles of the buttons at the end of the scrollbar are searched for in the current skin by adding "leftbutton" and "rightbutton" to the style name.
        ///The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</remarks>
        ///<param name="position">Rectangle on the screen to use for the scrollbar.</param>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="leftValue">The value at the left end of the scrollbar.</param>
        ///<param name="rightValue">The value at the right end of the scrollbar.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float hSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSbarValue = GUI.HorizontalScrollbar(new Rect(25, 25, 100, 30), hSbarValue, 1.0F, 0.0F, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float scrollPos = 0.5f;
        ///
        ///    // This will use the following style names to determine the size / placement of the buttons
        ///    // MyScrollbarleftbutton    - Name of style used for the left button.
        ///    // MyScrollbarrightbutton - Name of style used for the right button.
        ///    // MyScrollbarthumb         - Name of style used for the draggable thumb.
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUI.HorizontalScrollbar(new Rect(0, 0, 100, 20),  scrollPos, 1.0f, 0.0f, 100.0f, "Scroll");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue)
        {
            return Scroller(position, value, size, leftValue, rightValue, skin.horizontalScrollbar, skin.horizontalScrollbarThumb, skin.horizontalScrollbarLeftButton, skin.horizontalScrollbarRightButton, true);
        }

        ///<summary>Make a horizontal scrollbar. Scrollbars are what you use to scroll through a document. Most likely, you want to use scrollViews instead.</summary>
        ///<remarks>**Finding extra elements:**
        ///
        ///The styles of the buttons at the end of the scrollbar are searched for in the current skin by adding "leftbutton" and "rightbutton" to the style name.
        ///The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</remarks>
        ///<param name="position">Rectangle on the screen to use for the scrollbar.</param>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="leftValue">The value at the left end of the scrollbar.</param>
        ///<param name="rightValue">The value at the right end of the scrollbar.</param>
        ///<param name="style">The style to use for the scrollbar background. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float hSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSbarValue = GUI.HorizontalScrollbar(new Rect(25, 25, 100, 30), hSbarValue, 1.0F, 0.0F, 10.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float scrollPos = 0.5f;
        ///
        ///    // This will use the following style names to determine the size / placement of the buttons
        ///    // MyScrollbarleftbutton    - Name of style used for the left button.
        ///    // MyScrollbarrightbutton - Name of style used for the right button.
        ///    // MyScrollbarthumb         - Name of style used for the draggable thumb.
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUI.HorizontalScrollbar(new Rect(0, 0, 100, 20),  scrollPos, 1.0f, 0.0f, 100.0f, "Scroll");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle style)
        {
            return Scroller(position, value, size, leftValue, rightValue, style, skin.GetStyle(style.name + "thumb"), skin.GetStyle(style.name + "leftbutton"), skin.GetStyle(style.name + "rightbutton"), true);
        }

        ///<exclude />
        internal static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
        {
            bool hasChanged = false;
            if (DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
            {
                bool firstClick = s_ScrollControlId != scrollerID;
                s_ScrollControlId = scrollerID;

                if (firstClick)
                {
                    hasChanged = true;
                    nextScrollStepTime = DateTime.Now.AddMilliseconds(ScrollWaitDefinitions.firstWait);
                }
                else
                {
                    if (DateTime.Now >= nextScrollStepTime)
                    {
                        hasChanged = true;
                        nextScrollStepTime = DateTime.Now.AddMilliseconds(ScrollWaitDefinitions.regularWait);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                    InternalRepaintEditorWindow();
            }

            return hasChanged;
        }

        ///<summary>Make a vertical scrollbar. Scrollbars are what you use to scroll through a document. Most likely, you want to use scrollViews instead.</summary>
        ///<remarks>**Finding extra elements:**
        ///
        ///The styles of the buttons at the end of the scrollbar are searched for in the current skin by adding "upbutton" and "downbutton" to the style name.
        ///The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</remarks>
        ///<param name="position">Rectangle on the screen to use for the scrollbar.</param>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="topValue">The value at the top of the scrollbar.</param>
        ///<param name="bottomValue">The value at the bottom of the scrollbar.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float vSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSbarValue = GUI.VerticalScrollbar(new Rect(25, 25, 100, 30), vSbarValue, 1.0F, 10.0F, 0.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // This will use the following style names to determine the size / placement of the buttons
        /// // MyVertScrollbarupbutton   - Name of style used for the up button.
        /// // MyVertScrollbardownbutton - Name of style used for the down button.
        /// // MyVertScrollbarthumb      - Name of style used for the draggable thumb.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float scrollPos = 0.5f;
        ///
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUI.VerticalScrollbar(new Rect(0, 0, 100, 20), scrollPos, 1, 0, 100, "Scroll");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue)
        {
            return Scroller(position, value, size, topValue, bottomValue, skin.verticalScrollbar, skin.verticalScrollbarThumb, skin.verticalScrollbarUpButton, skin.verticalScrollbarDownButton, false);
        }

        ///<summary>Make a vertical scrollbar. Scrollbars are what you use to scroll through a document. Most likely, you want to use scrollViews instead.</summary>
        ///<remarks>**Finding extra elements:**
        ///
        ///The styles of the buttons at the end of the scrollbar are searched for in the current skin by adding "upbutton" and "downbutton" to the style name.
        ///The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</remarks>
        ///<param name="position">Rectangle on the screen to use for the scrollbar.</param>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="topValue">The value at the top of the scrollbar.</param>
        ///<param name="bottomValue">The value at the bottom of the scrollbar.</param>
        ///<param name="style">The style to use for the scrollbar background. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float vSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSbarValue = GUI.VerticalScrollbar(new Rect(25, 25, 100, 30), vSbarValue, 1.0F, 10.0F, 0.0F);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // This will use the following style names to determine the size / placement of the buttons
        /// // MyVertScrollbarupbutton   - Name of style used for the up button.
        /// // MyVertScrollbardownbutton - Name of style used for the down button.
        /// // MyVertScrollbarthumb      - Name of style used for the draggable thumb.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float scrollPos = 0.5f;
        ///
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUI.VerticalScrollbar(new Rect(0, 0, 100, 20), scrollPos, 1, 0, 100, "Scroll");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue, GUIStyle style)
        {
            return Scroller(position, value, size, topValue, bottomValue, style, skin.GetStyle(style.name + "thumb"), skin.GetStyle(style.name + "upbutton"), skin.GetStyle(style.name + "downbutton"), false);
        }

        internal static float Scroller(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive, position);

            Rect sliderRect, minRect, maxRect;

            if (horiz)
            {
                sliderRect = new Rect(
                    position.x + leftButton.fixedWidth, position.y,
                    position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height
                );
                minRect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
                maxRect = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
            }
            else
            {
                sliderRect = new Rect(
                    position.x, position.y + leftButton.fixedHeight,
                    position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight
                );
                minRect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
                maxRect = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
            }

            value = Slider(sliderRect, value, size, leftValue, rightValue, slider, thumb, horiz, id);

            bool wasMouseUpEvent = Event.current.type == EventType.MouseUp;

            if (ScrollerRepeatButton(id, minRect, leftButton))
                value -= s_ScrollStepSize * (leftValue < rightValue ? 1f : -1f);

            if (ScrollerRepeatButton(id, maxRect, rightButton))
                value += s_ScrollStepSize * (leftValue < rightValue ? 1f : -1f);

            if (wasMouseUpEvent && Event.current.type == EventType.Used) // repeat buttons ate mouse up event - release scrolling
                s_ScrollControlId = 0;

            if (leftValue < rightValue)
                value = Mathf.Clamp(value, leftValue, rightValue - size);
            else
                value = Mathf.Clamp(value, rightValue, leftValue - size);
            return value;
        }

        public static void BeginClip(Rect position, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset)
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Push(position, scrollOffset, renderOffset, resetOffset);
        }

        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position)                                { BeginGroup(position, GUIContent.none, GUIStyle.none); }
        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<param name="text">Text to display on the group.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position, string text)                   { BeginGroup(position, GUIContent.Temp(text), GUIStyle.none); }
        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the group.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position, Texture image)                 { BeginGroup(position, GUIContent.Temp(image), GUIStyle.none); }
        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<param name="content">Text, image and tooltip for this group. If supplied, any mouse clicks are "captured" by the group and not If left out, no background is rendered, and mouse clicks are passed.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position, GUIContent content)            { BeginGroup(position, content, GUIStyle.none); }
        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<param name="style">The style to use for the background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position, GUIStyle style)                { BeginGroup(position, GUIContent.none, style); }
        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<param name="text">Text to display on the group.</param>
        ///<param name="style">The style to use for the background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position, string text, GUIStyle style)   { BeginGroup(position, GUIContent.Temp(text), style); }
        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the group.</param>
        ///<param name="style">The style to use for the background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position, Texture image, GUIStyle style) { BeginGroup(position, GUIContent.Temp(image), style); }

        ///<summary>Begin a group. Must be matched with a call to <see cref="EndGroup" />.</summary>
        ///<remarks>When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<param name="position">Rectangle on the screen to use for the group.</param>
        ///<param name="content">Text, image and tooltip for this group. If supplied, any mouse clicks are "captured" by the group and not If left out, no background is rendered, and mouse clicks are passed.</param>
        ///<param name="style">The style to use for the background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="matrix" />
        ///<seealso cref="BeginScrollView" />
        public static void BeginGroup(Rect position, GUIContent content, GUIStyle style) { BeginGroup(position, content, style, Vector2.zero); }

        // Begin a group. Must be matched with a call to ::ref::EndGroup.
        internal static void BeginGroup(Rect position, GUIContent content, GUIStyle style, Vector2 scrollOffset)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_BeginGroupHash, FocusType.Passive);

            if (content != GUIContent.none || style != GUIStyle.none)
            {
                switch (Event.current.type)
                {
                    case EventType.Repaint:
                        style.Draw(position, content, id);
                        break;
                    default:
                        if (position.Contains(Event.current.mousePosition))
                            GUIUtility.mouseUsed = true;
                        break;
                }
            }
            GUIClip.Push(position, scrollOffset, Vector2.zero, false);
        }

        ///<summary>End a group.</summary>
        ///<remarks>Should be attached with <see cref="GUI.BeginGroup" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
        ///
        ///        // Draw a box in the new coordinate space defined by the BeginGroup.
        ///        // Notice how (0,0) has now been moved on-screen
        ///        GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///
        ///        // We need to match all BeginGroup calls with an EndGroup
        ///        GUI.EndGroup();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BeginGroup" />
        public static void EndGroup()
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Internal_Pop();
        }

        // Begin a clipping rect. Must be matched with a call to ::ref::EndClip.
        // Similar to BeginGroup but does not use GUIUtility.GetControlID () for style rendering
        // and can therefore be used in Repaint events only. BeginGroup needs to be called
        // on every event for consistent controlIDs.
        ///<exclude />
        public static void BeginClip(Rect position)
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Push(position, Vector2.zero, Vector2.zero, false);
        }

        // End a BeginClip
        ///<exclude />
        public static void EndClip()
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Pop();
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static UnityEngineInternal.GenericStack scrollViewStates { get; set; } = new UnityEngineInternal.GenericStack();

        ///<summary>Begin a scrolling view inside your GUI.</summary>
        ///<remarks>ScrollViews let you make a smaller area on-screen look 'into' a much larger area, using scrollbars placed on the sides of the ScrollView.</remarks>
        ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
        ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
        ///<param name="viewRect">The rectangle used inside the scrollview.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The position on of the scrolling viewport
        ///    public Vector2 scrollPosition = Vector2.zero;
        ///
        ///    void OnGUI()
        ///    {
        ///        // An absolute-positioned example: We make a scrollview that has a really large client
        ///        // rect and put it in a small rect on the screen.
        ///        scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200));
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
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect)
        {
            return BeginScrollView(position, scrollPosition, viewRect, false, false, skin.horizontalScrollbar, skin.verticalScrollbar, GUI.skin.scrollView);
        }

        ///<summary>Begin a scrolling view inside your GUI.</summary>
        ///<remarks>ScrollViews let you make a smaller area on-screen look 'into' a much larger area, using scrollbars placed on the sides of the ScrollView.</remarks>
        ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
        ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
        ///<param name="viewRect">The rectangle used inside the scrollview.</param>
        ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when <c>viewRect</c> is wider than <c>position</c>.</param>
        ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when <c>viewRect</c> is taller than <c>position</c>.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The position on of the scrolling viewport
        ///    public Vector2 scrollPosition = Vector2.zero;
        ///
        ///    void OnGUI()
        ///    {
        ///        // An absolute-positioned example: We make a scrollview that has a really large client
        ///        // rect and put it in a small rect on the screen.
        ///        scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200));
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
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical)
        {
            return BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, skin.horizontalScrollbar, skin.verticalScrollbar, GUI.skin.scrollView);
        }

        ///<summary>Begin a scrolling view inside your GUI.</summary>
        ///<remarks>ScrollViews let you make a smaller area on-screen look 'into' a much larger area, using scrollbars placed on the sides of the ScrollView.</remarks>
        ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
        ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
        ///<param name="viewRect">The rectangle used inside the scrollview.</param>
        ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The position on of the scrolling viewport
        ///    public Vector2 scrollPosition = Vector2.zero;
        ///
        ///    void OnGUI()
        ///    {
        ///        // An absolute-positioned example: We make a scrollview that has a really large client
        ///        // rect and put it in a small rect on the screen.
        ///        scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200));
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
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
        {
            return BeginScrollView(position, scrollPosition, viewRect, false, false, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView);
        }

        ///<summary>Begin a scrolling view inside your GUI.</summary>
        ///<remarks>ScrollViews let you make a smaller area on-screen look 'into' a much larger area, using scrollbars placed on the sides of the ScrollView.</remarks>
        ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
        ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
        ///<param name="viewRect">The rectangle used inside the scrollview.</param>
        ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when <c>viewRect</c> is wider than <c>position</c>.</param>
        ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when <c>viewRect</c> is taller than <c>position</c>.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The position on of the scrolling viewport
        ///    public Vector2 scrollPosition = Vector2.zero;
        ///
        ///    void OnGUI()
        ///    {
        ///        // An absolute-positioned example: We make a scrollview that has a really large client
        ///        // rect and put it in a small rect on the screen.
        ///        scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200));
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
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
        {
            return BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, skin.scrollView);
        }

        ///<exclude />
        protected static Vector2 DoBeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            return BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
        }

        internal static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            GUIUtility.CheckOnGUI();
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition))
            {
                if (Mathf.Abs(Event.current.mousePosition.y - position.y) < 8)
                {
                    scrollPosition.y -= 16;
                    InternalRepaintEditorWindow();
                }
                else if (Mathf.Abs(Event.current.mousePosition.y - position.yMax) < 8)
                {
                    scrollPosition.y += 16;
                    InternalRepaintEditorWindow();
                }
            }

            int id = GUIUtility.GetControlID(s_ScrollviewHash, FocusType.Passive);
            ScrollViewState state = (ScrollViewState)GUIUtility.GetStateObject(typeof(ScrollViewState), id);

            if (state.apply)
            {
                scrollPosition = state.scrollPosition;
                state.apply = false;
            }
            state.position = position;
            state.scrollPosition = scrollPosition;
            state.visibleRect = state.viewRect = viewRect;
            state.visibleRect.width = position.width;
            state.visibleRect.height = position.height;
            scrollViewStates.Push(state);

            Rect clipRect = new Rect(position);
            switch (Event.current.type)
            {
                case EventType.Layout:
                    GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    break;
                case EventType.Used:
                    break;
                default:
                    bool needsVertical = alwaysShowVertical, needsHorizontal = alwaysShowHorizontal;

                    // Check if we need a horizontal scrollbar
                    if (needsHorizontal || viewRect.width > clipRect.width)
                    {
                        state.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                        clipRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                        needsHorizontal = true;
                    }
                    if (needsVertical || viewRect.height > clipRect.height)
                    {
                        state.visibleRect.width = position.width - verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
                        clipRect.width -= verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
                        needsVertical = true;
                        if (!needsHorizontal && viewRect.width > clipRect.width)
                        {
                            state.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                            clipRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                            needsHorizontal = true;
                        }
                    }

                    if (Event.current.type == EventType.Repaint && background != GUIStyle.none)
                    {
                        background.Draw(position, position.Contains(Event.current.mousePosition), false, needsHorizontal && needsVertical, false);
                    }
                    if (needsHorizontal && horizontalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.x = HorizontalScrollbar(new Rect(position.x, position.yMax - horizontalScrollbar.fixedHeight, clipRect.width, horizontalScrollbar.fixedHeight),
                            scrollPosition.x, Mathf.Min(clipRect.width, viewRect.width), 0, viewRect.width,
                            horizontalScrollbar);
                    }
                    else
                    {
                        GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        scrollPosition.x = horizontalScrollbar != GUIStyle.none ? 0 : Mathf.Clamp(scrollPosition.x, 0, Mathf.Max(viewRect.width - position.width, 0));
                    }

                    if (needsVertical && verticalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.y = VerticalScrollbar(new Rect(clipRect.xMax + verticalScrollbar.margin.left, clipRect.y, verticalScrollbar.fixedWidth, clipRect.height),
                            scrollPosition.y, Mathf.Min(clipRect.height, viewRect.height), 0, viewRect.height,
                            verticalScrollbar);
                    }
                    else
                    {
                        GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        scrollPosition.y = verticalScrollbar != GUIStyle.none ? 0 : Mathf.Clamp(scrollPosition.y, 0, Mathf.Max(viewRect.height - position.height, 0));
                    }
                    break;
            }
            GUIClip.Push(clipRect, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x), Mathf.Round(-scrollPosition.y - viewRect.y)), Vector2.zero, false);
            return scrollPosition;
        }

        ///<summary>Ends a scrollview started with a call to BeginScrollView.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The position on of the scrolling viewport
        ///    public Vector2 scrollPosition = Vector2.zero;
        ///
        ///    void OnGUI()
        ///    {
        ///        // An absolute-positioned example: We make a scrollview that has a really large client
        ///        // rect and put it in a small rect on the screen.
        ///        scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200));
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
        public static void EndScrollView()
        {
            EndScrollView(true);
        }

        ///<summary>Ends a scrollview started with a call to BeginScrollView.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The position on of the scrolling viewport
        ///    public Vector2 scrollPosition = Vector2.zero;
        ///
        ///    void OnGUI()
        ///    {
        ///        // An absolute-positioned example: We make a scrollview that has a really large client
        ///        // rect and put it in a small rect on the screen.
        ///        scrollPosition = GUI.BeginScrollView(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200));
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
        public static void EndScrollView(bool handleScrollWheel)
        {
            GUIUtility.CheckOnGUI();

            if (scrollViewStates.Count == 0)
                return;
            ScrollViewState state = (ScrollViewState)scrollViewStates.Peek();

            GUIClip.Pop();

            scrollViewStates.Pop();

            bool needApply = false;

            float deltaTime = Time.realtimeSinceStartup - state.previousTimeSinceStartup;
            state.previousTimeSinceStartup = Time.realtimeSinceStartup;
            // If touch scroll, then handle inertia
            if (Event.current.type == EventType.Repaint && state.velocity != Vector2.zero)
            {
                for (int axis = 0; axis < 2; axis++)
                {
                    state.velocity[axis] *= Mathf.Pow(0.1f, deltaTime); // Decrease in a timely fashion (~/10 per second)
                    float velocityToSubstract = 0.1f / deltaTime;
                    if (Mathf.Abs(state.velocity[axis]) < velocityToSubstract)
                        state.velocity[axis] = 0;
                    else
                    {
                        state.velocity[axis] += state.velocity[axis] < 0 ? velocityToSubstract : -velocityToSubstract; // Substract directly to stop it faster on low velocity
                        state.scrollPosition[axis] += state.velocity[axis] * deltaTime;

                        needApply = true;
                        // Reset the scrolling start info so that dragging works fine after inertia
                        state.touchScrollStartMousePosition = Event.current.mousePosition;
                        state.touchScrollStartPosition = state.scrollPosition;
                    }
                }

                if (state.velocity != Vector2.zero)
                    InternalRepaintEditorWindow(); // Repaint to smooth the scroll
            }

            // This is the mac way of handling things: if the mouse is over a scrollview, the scrollview gets the event.
            if (handleScrollWheel &&
                (Event.current.type == EventType.ScrollWheel
                 || Event.current.type == EventType.TouchDown
                 || Event.current.type == EventType.TouchUp
                 || Event.current.type == EventType.TouchMove)
                // avoid eating scroll events if a scroll view is not necessary
                && (state.viewRect.width > state.visibleRect.width || state.viewRect.height > state.visibleRect.height)
            )
            {
                // Using scrollwheel
                if (Event.current.type == EventType.ScrollWheel
                    // avoid eating scroll events if a scroll view is not necessary
                    && ((state.viewRect.width > state.visibleRect.width && !Mathf.Approximately(0f, Event.current.delta.x))
                        || (state.viewRect.height > state.visibleRect.height && !Mathf.Approximately(0f, Event.current.delta.y)))
                    && state.position.Contains(Event.current.mousePosition)
                )
                {
                    state.scrollPosition.x = Mathf.Clamp(state.scrollPosition.x + (Event.current.delta.x * 20f), 0f, state.viewRect.width - state.visibleRect.width);
                    state.scrollPosition.y = Mathf.Clamp(state.scrollPosition.y + (Event.current.delta.y * 20f), 0f, state.viewRect.height - state.visibleRect.height);
                    Event.current.Use();

                    needApply = true;
                }
                // Using touch
                else if (Event.current.type == EventType.TouchDown && (Event.current.modifiers & EventModifiers.Alt) == EventModifiers.Alt && state.position.Contains(Event.current.mousePosition))
                {
                    state.isDuringTouchScroll = true;
                    state.touchScrollStartMousePosition = Event.current.mousePosition;
                    state.touchScrollStartPosition = state.scrollPosition;

                    GUIUtility.hotControl = GUIUtility.GetControlID(s_ScrollviewHash, FocusType.Passive, state.position);;
                    Event.current.Use();
                }
                else if (state.isDuringTouchScroll && Event.current.type == EventType.TouchUp)
                    state.isDuringTouchScroll = false;
                else if (state.isDuringTouchScroll && Event.current.type == EventType.TouchMove)
                {
                    Vector2 previousPosition = state.scrollPosition;

                    state.scrollPosition.x = Mathf.Clamp(state.touchScrollStartPosition.x - (Event.current.mousePosition.x - state.touchScrollStartMousePosition.x), 0f, state.viewRect.width - state.visibleRect.width);
                    state.scrollPosition.y = Mathf.Clamp(state.touchScrollStartPosition.y - (Event.current.mousePosition.y - state.touchScrollStartMousePosition.y), 0f, state.viewRect.height - state.visibleRect.height);
                    Event.current.Use();

                    // Sets the new volicity
                    Vector2 newVelocity = (state.scrollPosition - previousPosition) / deltaTime;
                    state.velocity = Vector2.Lerp(state.velocity, newVelocity, deltaTime * 10);

                    needApply = true;
                }
            }
            if (needApply)
            {
                // If one of the visible rect dimensions is larger than the view rect dimensions
                if (state.scrollPosition.x < 0f)
                    state.scrollPosition.x = 0f;
                if (state.scrollPosition.y < 0f)
                    state.scrollPosition.y = 0f;
                state.apply = true;
            }
        }

        internal static ScrollViewState GetTopScrollView()
        {
            if (scrollViewStates.Count != 0)
                return (ScrollViewState)scrollViewStates.Peek();
            return null;
        }

        ///<summary>Scrolls all enclosing scrollviews so they try to make <c>position</c> visible.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Draws a Scroll view with 2 buttons inside.
        /// // When clicked each button it moves the scroll
        /// // where the other button is located
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Vector2 scrollPos = Vector2.zero;
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUI.BeginScrollView(new Rect(10, 10, 100, 50), scrollPos, new Rect(0, 0, 220, 10));
        ///
        ///        if (GUI.Button(new Rect(0, 0, 100, 20), "Go Right"))
        ///            GUI.ScrollTo(new Rect(120, 0, 100, 20));
        ///
        ///        if (GUI.Button(new Rect(120, 0, 100, 20), "Go Left"))
        ///            GUI.ScrollTo(new Rect(0, 0, 100, 20));
        ///
        ///        GUI.EndScrollView();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void ScrollTo(Rect position)
        {
            ScrollViewState topmost = GetTopScrollView();
            topmost?.ScrollTo(position);
        }

        // Scrolls all enclosing scrollviews towards making /position/ visible.
        public static bool ScrollTowards(Rect position, float maxDelta)
        {
            ScrollViewState topmost = GetTopScrollView();
            if (topmost == null)
                return false;
            return topmost.ScrollTowards(position, maxDelta);
        }

        [RequiredByNativeCode]
        internal static bool ScrollTowardsFromNative(float positionX, float positionY, float positionWidth, float positionHeight, float maxDelta)
        {
            var position = new Rect(positionX, positionY, positionWidth, positionHeight);
            return ScrollTowards(position, maxDelta);
        }

        ///<summary>Callback to draw GUI within a window (used with <see cref="GUI.Window" />).</summary>
        ///<remarks>This function takes the ID number of the window to be drawn. Its body should contain GUI calls to display the window, much like a standard OnGUI function. This function can then be passed as a parameter to <see cref="GUI.Window" /> to draw the appropriate contents.</remarks>
        public delegate void WindowFunction(int id);
        ///<summary>Make a popup window.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user. Unlike other controls, you need to pass them a separate function that renders the GUI controls inside the window.
        ///
        ///**Note:** If you are using <see cref="GUILayout" /> to place your components inside the window, you should use <see cref="GUILayout.Window" />. Also, if <see cref="MonoBehaviour.useGUILayout" /> is set to false then a call to GUI.Window will not have any effect, even though it is not a GUILayout function.</para>
        ///  <para>You can use the same function to create multiple windows. Just make sure that each window has its own ID. Example:</para>
        ///  <para>To stop showing a window, simply stop calling GUI.Window from inside your main OnGUI function:</para>
        ///  <para>To make a window that gets its size from automatic GUI layouting, use <see cref="GUILayout.Window" />.
        ///    **Call Ordering**
        ///    Windows need to be drawn back-to-front; windows on top of other windows need to be drawn later than the ones below them. This means that you can not count on your DoWindow functions to
        ///    be called in any particular order. In order for this to work seamlessly, the following values are stored when you create your window (using the **Window** function), and retrieved when your DoWindow gets called:
        ///        <see cref="GUI.skin" />, <see cref="GUI.enabled" />, <see cref="GUI.color" />, <see cref="GUI.backgroundColor" />, <see cref="GUI.contentColor" />, <see cref="GUI.matrix" />.</para>
        ///  <para>Note that you can use the alpha component of <see cref="GUI.color" /> to fade windows in and out.
        ///
        ///</para>
        ///</remarks>
        ///<param name="id">ID number for the window (can be any value as long as it is unique).</param>
        ///<param name="clientRect">Onscreen rectangle denoting the window's position and size.</param>
        ///<param name="func">Script function to display the window's contents.</param>
        ///<param name="text">Text to render inside the window.</param>
        ///<returns>Onscreen rectangle denoting the window's position and size.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. We create two windows that use the same function
        ///        // Notice that their IDs differ
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "My Window");
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window " + windowID);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // boolean variable to decide whether to show the window or not.
        /// // Change this from the in-game GUI, scripting, the inspector or anywhere else to
        /// // decide whether the window is visible
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public bool doWindow0 = true;
        ///
        ///    // Make the contents of the window.
        ///    void DoWindow0(int windowID)
        ///    {
        ///        GUI.Button(new Rect(10, 30, 80, 20), "Click Me!");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a toggle button for hiding and showing the window
        ///        doWindow0 = GUI.Toggle(new Rect(10, 10, 100, 20), doWindow0, "Window 0");
        ///
        ///        // Make sure we only call GUI.Window if doWindow0 is true.
        ///        if (doWindow0)
        ///        {
        ///            GUI.Window(0, new Rect(110, 10, 200, 60), DoWindow0, "Basic Window");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Here we make 2 windows. We set the GUI.color value to something before each.
        ///        GUI.color = Color.red;
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "Red Window");
        ///
        ///        GUI.color = Color.green;
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "Green Window");
        ///    }
        ///
        ///    // Make the contents of the window.
        ///    // The value of GUI.color is set to what it was when the window
        ///    // was created in the code above.
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window with color " + GUI.color);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="DragWindow" />
        ///<seealso cref="BringWindowToFront" />
        ///<seealso cref="BringWindowToBack" />
        public static Rect Window(int id, Rect clientRect, WindowFunction func, string text)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(text), GUI.skin.window, GUI.skin, true);
        }

        ///<summary>Make a popup window.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user. Unlike other controls, you need to pass them a separate function that renders the GUI controls inside the window.
        ///
        ///**Note:** If you are using <see cref="GUILayout" /> to place your components inside the window, you should use <see cref="GUILayout.Window" />. Also, if <see cref="MonoBehaviour.useGUILayout" /> is set to false then a call to GUI.Window will not have any effect, even though it is not a GUILayout function.</para>
        ///  <para>You can use the same function to create multiple windows. Just make sure that each window has its own ID. Example:</para>
        ///  <para>To stop showing a window, simply stop calling GUI.Window from inside your main OnGUI function:</para>
        ///  <para>To make a window that gets its size from automatic GUI layouting, use <see cref="GUILayout.Window" />.
        ///    **Call Ordering**
        ///    Windows need to be drawn back-to-front; windows on top of other windows need to be drawn later than the ones below them. This means that you can not count on your DoWindow functions to
        ///    be called in any particular order. In order for this to work seamlessly, the following values are stored when you create your window (using the **Window** function), and retrieved when your DoWindow gets called:
        ///        <see cref="GUI.skin" />, <see cref="GUI.enabled" />, <see cref="GUI.color" />, <see cref="GUI.backgroundColor" />, <see cref="GUI.contentColor" />, <see cref="GUI.matrix" />.</para>
        ///  <para>Note that you can use the alpha component of <see cref="GUI.color" /> to fade windows in and out.
        ///
        ///</para>
        ///</remarks>
        ///<param name="id">ID number for the window (can be any value as long as it is unique).</param>
        ///<param name="clientRect">Onscreen rectangle denoting the window's position and size.</param>
        ///<param name="func">Script function to display the window's contents.</param>
        ///<param name="image">Image to render inside the window.</param>
        ///<returns>Onscreen rectangle denoting the window's position and size.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. We create two windows that use the same function
        ///        // Notice that their IDs differ
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "My Window");
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window " + windowID);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // boolean variable to decide whether to show the window or not.
        /// // Change this from the in-game GUI, scripting, the inspector or anywhere else to
        /// // decide whether the window is visible
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public bool doWindow0 = true;
        ///
        ///    // Make the contents of the window.
        ///    void DoWindow0(int windowID)
        ///    {
        ///        GUI.Button(new Rect(10, 30, 80, 20), "Click Me!");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a toggle button for hiding and showing the window
        ///        doWindow0 = GUI.Toggle(new Rect(10, 10, 100, 20), doWindow0, "Window 0");
        ///
        ///        // Make sure we only call GUI.Window if doWindow0 is true.
        ///        if (doWindow0)
        ///        {
        ///            GUI.Window(0, new Rect(110, 10, 200, 60), DoWindow0, "Basic Window");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Here we make 2 windows. We set the GUI.color value to something before each.
        ///        GUI.color = Color.red;
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "Red Window");
        ///
        ///        GUI.color = Color.green;
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "Green Window");
        ///    }
        ///
        ///    // Make the contents of the window.
        ///    // The value of GUI.color is set to what it was when the window
        ///    // was created in the code above.
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window with color " + GUI.color);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="DragWindow" />
        ///<seealso cref="BringWindowToFront" />
        ///<seealso cref="BringWindowToBack" />
        public static Rect Window(int id, Rect clientRect, WindowFunction func, Texture image)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(image), GUI.skin.window, GUI.skin, true);
        }

        ///<summary>Make a popup window.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user. Unlike other controls, you need to pass them a separate function that renders the GUI controls inside the window.
        ///
        ///**Note:** If you are using <see cref="GUILayout" /> to place your components inside the window, you should use <see cref="GUILayout.Window" />. Also, if <see cref="MonoBehaviour.useGUILayout" /> is set to false then a call to GUI.Window will not have any effect, even though it is not a GUILayout function.</para>
        ///  <para>You can use the same function to create multiple windows. Just make sure that each window has its own ID. Example:</para>
        ///  <para>To stop showing a window, simply stop calling GUI.Window from inside your main OnGUI function:</para>
        ///  <para>To make a window that gets its size from automatic GUI layouting, use <see cref="GUILayout.Window" />.
        ///    **Call Ordering**
        ///    Windows need to be drawn back-to-front; windows on top of other windows need to be drawn later than the ones below them. This means that you can not count on your DoWindow functions to
        ///    be called in any particular order. In order for this to work seamlessly, the following values are stored when you create your window (using the **Window** function), and retrieved when your DoWindow gets called:
        ///        <see cref="GUI.skin" />, <see cref="GUI.enabled" />, <see cref="GUI.color" />, <see cref="GUI.backgroundColor" />, <see cref="GUI.contentColor" />, <see cref="GUI.matrix" />.</para>
        ///  <para>Note that you can use the alpha component of <see cref="GUI.color" /> to fade windows in and out.
        ///
        ///</para>
        ///</remarks>
        ///<param name="id">ID number for the window (can be any value as long as it is unique).</param>
        ///<param name="clientRect">Onscreen rectangle denoting the window's position and size.</param>
        ///<param name="func">Script function to display the window's contents.</param>
        ///<param name="content">GUIContent to render inside the window.</param>
        ///<returns>Onscreen rectangle denoting the window's position and size.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. We create two windows that use the same function
        ///        // Notice that their IDs differ
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "My Window");
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window " + windowID);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // boolean variable to decide whether to show the window or not.
        /// // Change this from the in-game GUI, scripting, the inspector or anywhere else to
        /// // decide whether the window is visible
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public bool doWindow0 = true;
        ///
        ///    // Make the contents of the window.
        ///    void DoWindow0(int windowID)
        ///    {
        ///        GUI.Button(new Rect(10, 30, 80, 20), "Click Me!");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a toggle button for hiding and showing the window
        ///        doWindow0 = GUI.Toggle(new Rect(10, 10, 100, 20), doWindow0, "Window 0");
        ///
        ///        // Make sure we only call GUI.Window if doWindow0 is true.
        ///        if (doWindow0)
        ///        {
        ///            GUI.Window(0, new Rect(110, 10, 200, 60), DoWindow0, "Basic Window");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Here we make 2 windows. We set the GUI.color value to something before each.
        ///        GUI.color = Color.red;
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "Red Window");
        ///
        ///        GUI.color = Color.green;
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "Green Window");
        ///    }
        ///
        ///    // Make the contents of the window.
        ///    // The value of GUI.color is set to what it was when the window
        ///    // was created in the code above.
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window with color " + GUI.color);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="DragWindow" />
        ///<seealso cref="BringWindowToFront" />
        ///<seealso cref="BringWindowToBack" />
        public static Rect Window(int id, Rect clientRect, WindowFunction func, GUIContent content)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, content, GUI.skin.window, GUI.skin, true);
        }

        ///<summary>Make a popup window.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user. Unlike other controls, you need to pass them a separate function that renders the GUI controls inside the window.
        ///
        ///**Note:** If you are using <see cref="GUILayout" /> to place your components inside the window, you should use <see cref="GUILayout.Window" />. Also, if <see cref="MonoBehaviour.useGUILayout" /> is set to false then a call to GUI.Window will not have any effect, even though it is not a GUILayout function.</para>
        ///  <para>You can use the same function to create multiple windows. Just make sure that each window has its own ID. Example:</para>
        ///  <para>To stop showing a window, simply stop calling GUI.Window from inside your main OnGUI function:</para>
        ///  <para>To make a window that gets its size from automatic GUI layouting, use <see cref="GUILayout.Window" />.
        ///    **Call Ordering**
        ///    Windows need to be drawn back-to-front; windows on top of other windows need to be drawn later than the ones below them. This means that you can not count on your DoWindow functions to
        ///    be called in any particular order. In order for this to work seamlessly, the following values are stored when you create your window (using the **Window** function), and retrieved when your DoWindow gets called:
        ///        <see cref="GUI.skin" />, <see cref="GUI.enabled" />, <see cref="GUI.color" />, <see cref="GUI.backgroundColor" />, <see cref="GUI.contentColor" />, <see cref="GUI.matrix" />.</para>
        ///  <para>Note that you can use the alpha component of <see cref="GUI.color" /> to fade windows in and out.
        ///
        ///</para>
        ///</remarks>
        ///<param name="id">ID number for the window (can be any value as long as it is unique).</param>
        ///<param name="clientRect">Onscreen rectangle denoting the window's position and size.</param>
        ///<param name="func">Script function to display the window's contents.</param>
        ///<param name="text">Text to render inside the window.</param>
        ///<param name="style">Style information for the window.</param>
        ///<returns>Onscreen rectangle denoting the window's position and size.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. We create two windows that use the same function
        ///        // Notice that their IDs differ
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "My Window");
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window " + windowID);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // boolean variable to decide whether to show the window or not.
        /// // Change this from the in-game GUI, scripting, the inspector or anywhere else to
        /// // decide whether the window is visible
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public bool doWindow0 = true;
        ///
        ///    // Make the contents of the window.
        ///    void DoWindow0(int windowID)
        ///    {
        ///        GUI.Button(new Rect(10, 30, 80, 20), "Click Me!");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a toggle button for hiding and showing the window
        ///        doWindow0 = GUI.Toggle(new Rect(10, 10, 100, 20), doWindow0, "Window 0");
        ///
        ///        // Make sure we only call GUI.Window if doWindow0 is true.
        ///        if (doWindow0)
        ///        {
        ///            GUI.Window(0, new Rect(110, 10, 200, 60), DoWindow0, "Basic Window");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Here we make 2 windows. We set the GUI.color value to something before each.
        ///        GUI.color = Color.red;
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "Red Window");
        ///
        ///        GUI.color = Color.green;
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "Green Window");
        ///    }
        ///
        ///    // Make the contents of the window.
        ///    // The value of GUI.color is set to what it was when the window
        ///    // was created in the code above.
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window with color " + GUI.color);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="DragWindow" />
        ///<seealso cref="BringWindowToFront" />
        ///<seealso cref="BringWindowToBack" />
        public static Rect Window(int id, Rect clientRect, WindowFunction func, string text, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(text), style, GUI.skin, true);
        }

        ///<summary>Make a popup window.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user. Unlike other controls, you need to pass them a separate function that renders the GUI controls inside the window.
        ///
        ///**Note:** If you are using <see cref="GUILayout" /> to place your components inside the window, you should use <see cref="GUILayout.Window" />. Also, if <see cref="MonoBehaviour.useGUILayout" /> is set to false then a call to GUI.Window will not have any effect, even though it is not a GUILayout function.</para>
        ///  <para>You can use the same function to create multiple windows. Just make sure that each window has its own ID. Example:</para>
        ///  <para>To stop showing a window, simply stop calling GUI.Window from inside your main OnGUI function:</para>
        ///  <para>To make a window that gets its size from automatic GUI layouting, use <see cref="GUILayout.Window" />.
        ///    **Call Ordering**
        ///    Windows need to be drawn back-to-front; windows on top of other windows need to be drawn later than the ones below them. This means that you can not count on your DoWindow functions to
        ///    be called in any particular order. In order for this to work seamlessly, the following values are stored when you create your window (using the **Window** function), and retrieved when your DoWindow gets called:
        ///        <see cref="GUI.skin" />, <see cref="GUI.enabled" />, <see cref="GUI.color" />, <see cref="GUI.backgroundColor" />, <see cref="GUI.contentColor" />, <see cref="GUI.matrix" />.</para>
        ///  <para>Note that you can use the alpha component of <see cref="GUI.color" /> to fade windows in and out.
        ///
        ///</para>
        ///</remarks>
        ///<param name="id">ID number for the window (can be any value as long as it is unique).</param>
        ///<param name="clientRect">Onscreen rectangle denoting the window's position and size.</param>
        ///<param name="func">Script function to display the window's contents.</param>
        ///<param name="image">Image to render inside the window.</param>
        ///<param name="style">Style information for the window.</param>
        ///<returns>Onscreen rectangle denoting the window's position and size.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. We create two windows that use the same function
        ///        // Notice that their IDs differ
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "My Window");
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window " + windowID);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // boolean variable to decide whether to show the window or not.
        /// // Change this from the in-game GUI, scripting, the inspector or anywhere else to
        /// // decide whether the window is visible
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public bool doWindow0 = true;
        ///
        ///    // Make the contents of the window.
        ///    void DoWindow0(int windowID)
        ///    {
        ///        GUI.Button(new Rect(10, 30, 80, 20), "Click Me!");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a toggle button for hiding and showing the window
        ///        doWindow0 = GUI.Toggle(new Rect(10, 10, 100, 20), doWindow0, "Window 0");
        ///
        ///        // Make sure we only call GUI.Window if doWindow0 is true.
        ///        if (doWindow0)
        ///        {
        ///            GUI.Window(0, new Rect(110, 10, 200, 60), DoWindow0, "Basic Window");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Here we make 2 windows. We set the GUI.color value to something before each.
        ///        GUI.color = Color.red;
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "Red Window");
        ///
        ///        GUI.color = Color.green;
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "Green Window");
        ///    }
        ///
        ///    // Make the contents of the window.
        ///    // The value of GUI.color is set to what it was when the window
        ///    // was created in the code above.
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window with color " + GUI.color);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="DragWindow" />
        ///<seealso cref="BringWindowToFront" />
        ///<seealso cref="BringWindowToBack" />
        public static Rect Window(int id, Rect clientRect, WindowFunction func, Texture image, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(image), style, GUI.skin, true);
        }

        ///<summary>Make a popup window.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user. Unlike other controls, you need to pass them a separate function that renders the GUI controls inside the window.
        ///
        ///**Note:** If you are using <see cref="GUILayout" /> to place your components inside the window, you should use <see cref="GUILayout.Window" />. Also, if <see cref="MonoBehaviour.useGUILayout" /> is set to false then a call to GUI.Window will not have any effect, even though it is not a GUILayout function.</para>
        ///  <para>You can use the same function to create multiple windows. Just make sure that each window has its own ID. Example:</para>
        ///  <para>To stop showing a window, simply stop calling GUI.Window from inside your main OnGUI function:</para>
        ///  <para>To make a window that gets its size from automatic GUI layouting, use <see cref="GUILayout.Window" />.
        ///    **Call Ordering**
        ///    Windows need to be drawn back-to-front; windows on top of other windows need to be drawn later than the ones below them. This means that you can not count on your DoWindow functions to
        ///    be called in any particular order. In order for this to work seamlessly, the following values are stored when you create your window (using the **Window** function), and retrieved when your DoWindow gets called:
        ///        <see cref="GUI.skin" />, <see cref="GUI.enabled" />, <see cref="GUI.color" />, <see cref="GUI.backgroundColor" />, <see cref="GUI.contentColor" />, <see cref="GUI.matrix" />.</para>
        ///  <para>Note that you can use the alpha component of <see cref="GUI.color" /> to fade windows in and out.
        ///
        ///</para>
        ///</remarks>
        ///<param name="id">ID number for the window (can be any value as long as it is unique).</param>
        ///<param name="clientRect">Onscreen rectangle denoting the window's position and size.</param>
        ///<param name="func">Script function to display the window's contents.</param>
        ///<param name="style">Style information for the window.</param>
        ///<param name="title">Text displayed in the window's title bar.</param>
        ///<returns>Onscreen rectangle denoting the window's position and size.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. We create two windows that use the same function
        ///        // Notice that their IDs differ
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "My Window");
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window " + windowID);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        /// // boolean variable to decide whether to show the window or not.
        /// // Change this from the in-game GUI, scripting, the inspector or anywhere else to
        /// // decide whether the window is visible
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public bool doWindow0 = true;
        ///
        ///    // Make the contents of the window.
        ///    void DoWindow0(int windowID)
        ///    {
        ///        GUI.Button(new Rect(10, 30, 80, 20), "Click Me!");
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a toggle button for hiding and showing the window
        ///        doWindow0 = GUI.Toggle(new Rect(10, 10, 100, 20), doWindow0, "Window 0");
        ///
        ///        // Make sure we only call GUI.Window if doWindow0 is true.
        ///        if (doWindow0)
        ///        {
        ///            GUI.Window(0, new Rect(110, 10, 200, 60), DoWindow0, "Basic Window");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect0 = new Rect(20, 20, 120, 50);
        ///    public Rect windowRect1 = new Rect(20, 100, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Here we make 2 windows. We set the GUI.color value to something before each.
        ///        GUI.color = Color.red;
        ///        windowRect0 = GUI.Window(0, windowRect0, DoMyWindow, "Red Window");
        ///
        ///        GUI.color = Color.green;
        ///        windowRect1 = GUI.Window(1, windowRect1, DoMyWindow, "Green Window");
        ///    }
        ///
        ///    // Make the contents of the window.
        ///    // The value of GUI.color is set to what it was when the window
        ///    // was created in the code above.
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        if (GUI.Button(new Rect(10, 20, 100, 20), "Hello World"))
        ///        {
        ///            print("Got a click in window with color " + GUI.color);
        ///        }
        ///
        ///        // Make the windows be draggable.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="DragWindow" />
        ///<seealso cref="BringWindowToFront" />
        ///<seealso cref="BringWindowToBack" />
        public static Rect Window(int id, Rect clientRect, WindowFunction func, GUIContent title, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, title, style, GUI.skin, true);
        }

        ///<summary>Show a Modal Window.</summary>
        ///<remarks>Similar to <see cref="GUI.Window" />, however the window will always be on top of all other GUI, and while displayed, is guaranteed to be sole recipient of all GUI input and events. While a ModalWindow is being displayed, other controls will not be processing input. Note that only one ModalWindow can be displayed at a time.</remarks>
        ///<param name="id">A unique id number.</param>
        ///<param name="clientRect">Position and size of the window.</param>
        ///<param name="func">A function which contains the immediate mode GUI code to draw the contents of your window.</param>
        ///<param name="text">Text to appear in the title-bar area of the window, if any.</param>
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, string text)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(text), GUI.skin.window, GUI.skin);
        }

        ///<summary>Show a Modal Window.</summary>
        ///<remarks>Similar to <see cref="GUI.Window" />, however the window will always be on top of all other GUI, and while displayed, is guaranteed to be sole recipient of all GUI input and events. While a ModalWindow is being displayed, other controls will not be processing input. Note that only one ModalWindow can be displayed at a time.</remarks>
        ///<param name="id">A unique id number.</param>
        ///<param name="clientRect">Position and size of the window.</param>
        ///<param name="func">A function which contains the immediate mode GUI code to draw the contents of your window.</param>
        ///<param name="image">An image to appear in the title bar of the window, if any.</param>
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, Texture image)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(image), GUI.skin.window, GUI.skin);
        }

        ///<summary>Show a Modal Window.</summary>
        ///<remarks>Similar to <see cref="GUI.Window" />, however the window will always be on top of all other GUI, and while displayed, is guaranteed to be sole recipient of all GUI input and events. While a ModalWindow is being displayed, other controls will not be processing input. Note that only one ModalWindow can be displayed at a time.</remarks>
        ///<param name="id">A unique id number.</param>
        ///<param name="clientRect">Position and size of the window.</param>
        ///<param name="func">A function which contains the immediate mode GUI code to draw the contents of your window.</param>
        ///<param name="content">GUIContent to appear in the title bar of the window, if any.</param>
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, GUIContent content)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, content, GUI.skin.window, GUI.skin);
        }

        ///<summary>Show a Modal Window.</summary>
        ///<remarks>Similar to <see cref="GUI.Window" />, however the window will always be on top of all other GUI, and while displayed, is guaranteed to be sole recipient of all GUI input and events. While a ModalWindow is being displayed, other controls will not be processing input. Note that only one ModalWindow can be displayed at a time.</remarks>
        ///<param name="id">A unique id number.</param>
        ///<param name="clientRect">Position and size of the window.</param>
        ///<param name="func">A function which contains the immediate mode GUI code to draw the contents of your window.</param>
        ///<param name="text">Text to appear in the title-bar area of the window, if any.</param>
        ///<param name="style">Style to apply to the window.</param>
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, string text, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(text), style, GUI.skin);
        }

        ///<summary>Show a Modal Window.</summary>
        ///<remarks>Similar to <see cref="GUI.Window" />, however the window will always be on top of all other GUI, and while displayed, is guaranteed to be sole recipient of all GUI input and events. While a ModalWindow is being displayed, other controls will not be processing input. Note that only one ModalWindow can be displayed at a time.</remarks>
        ///<param name="id">A unique id number.</param>
        ///<param name="clientRect">Position and size of the window.</param>
        ///<param name="func">A function which contains the immediate mode GUI code to draw the contents of your window.</param>
        ///<param name="image">An image to appear in the title bar of the window, if any.</param>
        ///<param name="style">Style to apply to the window.</param>
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, Texture image, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(image), style, GUI.skin);
        }

        ///<summary>Show a Modal Window.</summary>
        ///<remarks>Similar to <see cref="GUI.Window" />, however the window will always be on top of all other GUI, and while displayed, is guaranteed to be sole recipient of all GUI input and events. While a ModalWindow is being displayed, other controls will not be processing input. Note that only one ModalWindow can be displayed at a time.</remarks>
        ///<param name="id">A unique id number.</param>
        ///<param name="clientRect">Position and size of the window.</param>
        ///<param name="func">A function which contains the immediate mode GUI code to draw the contents of your window.</param>
        ///<param name="content">GUIContent to appear in the title bar of the window, if any.</param>
        ///<param name="style">Style to apply to the window.</param>
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, content, style, GUI.skin);
        }

        private static Rect DoWindow(int id, Rect clientRect, WindowFunction func, GUIContent title, GUIStyle style, GUISkin skin, bool forceRectOnLayout)
        {
            return Internal_DoWindow(id, GUIUtility.s_OriginalID, clientRect, func, title, style, skin, forceRectOnLayout);
        }

        private static Rect DoModalWindow(int id, Rect clientRect, WindowFunction func, GUIContent content, GUIStyle style, GUISkin skin)
        {
            return Internal_DoModalWindow(id, GUIUtility.s_OriginalID, clientRect, func, content, style, skin);
        }

        [RequiredByNativeCode]
        internal static void CallWindowDelegate(WindowFunction func, int id, EntityId entityId, GUISkin _skin, int forceRect, float width, float height, GUIStyle style)
        {
            GUILayoutUtility.SelectIDListWindow(id);
            GUISkin temp = skin;
            if (Event.current.type == EventType.Layout)
            {
                if (forceRect != 0)
                {
                    GUILayoutOption[] options = { GUILayout.Width(width), GUILayout.Height(height) };

                    // Tell the GUILayout system we're starting a window, our style and our size. Then layouting is just the same as anything else
                    GUILayoutUtility.BeginWindow(id, style, options);
                }
                else
                {
                    // If we don't want to force the rect (which is when we come from GUILayout.window), don't pass in the fixedsize options
                    GUILayoutUtility.BeginWindow(id, style, null);
                }
            }
            else
            {
                GUILayoutUtility.BeginWindow(id, GUIStyle.none, null);
            }

            skin = _skin;
            func?.Invoke(id);

            if (Event.current.type == EventType.Layout)
            {
                // Now layout the window.
                GUILayoutUtility.Layout();
            }
            skin = temp;
        }

        ///<summary>If you want to have the entire window background to act as a drag area, use the version of DragWindow that takes no parameters and put it at the end of the window function.</summary>
        ///<remarks>This will mean that any other controls will get precedence and the dragging will only be activated if nothing else has mouse focus.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUI.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        GUI.Button(new Rect(10, 20, 100, 20), "Can't drag me");
        ///        // Insert a huge dragging area at the end.
        ///        // This gets clipped to the window (like all other controls) so you can never
        ///        //  drag the window from outside it.
        ///        GUI.DragWindow(new Rect(0, 0, 10000, 20));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="DragWindow" />
        ///<seealso cref="BringWindowToFront" />
        ///<seealso cref="BringWindowToBack" />
        public static void DragWindow() { DragWindow(new Rect(0, 0, 10000, 10000)); }

        // Call at the beginning of a frame.
        // e event to process
        // windowInfo - the list of windows we're currently using.
        internal static void BeginWindows(int skinMode, EntityId editorWindowInstanceID)
        {
            // Let's just remember where we came from
            GUILayoutGroup oldTopLevel = GUILayoutUtility.current.topLevel;
            UnityEngineInternal.GenericStack oldLayoutGroups = GUILayoutUtility.current.layoutGroups;
            GUILayoutGroup oldWindows = GUILayoutUtility.current.windows;
            Matrix4x4 mat = GUI.matrix;

            // Call into C++ land
            Internal_BeginWindows();

            GUI.matrix = mat;
            GUILayoutUtility.current.topLevel = oldTopLevel;
            GUILayoutUtility.current.layoutGroups = oldLayoutGroups;
            GUILayoutUtility.current.windows = oldWindows;
        }

        // Call at the end of frame (at layer 0) to do all windows
        internal static void EndWindows()
        {
            // Let's just remember where we came from
            GUILayoutGroup oldTopLevel = GUILayoutUtility.current.topLevel;
            UnityEngineInternal.GenericStack oldLayoutGroups = GUILayoutUtility.current.layoutGroups;
            GUILayoutGroup oldWindows = GUILayoutUtility.current.windows;

            // Call Into C++ land
            Internal_EndWindows();

            GUILayoutUtility.current.topLevel = oldTopLevel;
            GUILayoutUtility.current.layoutGroups = oldLayoutGroups;
            GUILayoutUtility.current.windows = oldWindows;
        }
    }

    public partial class GUI
    {
        ///<exclude />
        public abstract class Scope : IDisposable
        {
            bool m_Disposed;

            internal virtual void Dispose(bool disposing)
            {
                if (m_Disposed)
                    return;
                if (disposing && !GUIUtility.guiIsExiting)
                    CloseScope();
                m_Disposed = true;
            }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
            ~Scope()
            {
                if (!m_Disposed && !GUIUtility.guiIsExiting)
                    Console.WriteLine($"{GetType().Name} was not disposed! You should use the 'using' keyword or manually call Dispose.");
                Dispose(false);
            }
#pragma warning restore UA5000

            ///<exclude />
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected abstract void CloseScope();
        }

        ///<summary>Disposable helper class for managing <see cref="BeginGroup" /> / <see cref="EndGroup" />.</summary>
        ///<remarks>::ref::BeginGroup is called at construction, and <see cref="EndGroup" /> is called when the instance is disposed.
        ///When you begin a group, the coordinate system for GUI controls are set so (0,0) is the top-left corner of the group. All controls are clipped to the group.
        ///Groups can be nested - if they are, children are clipped to their parents.
        ///
        ///This is very useful when moving a bunch of GUI elements around on screen. A common use case is designing your menus to fit on a specific screen size, then centering the GUI on larger displays.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Constrain all drawing to be within a 800x600 pixel area centered on the screen.
        ///        using (var groupScope = new GUI.GroupScope(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600)))
        ///        {
        ///            // Draw a box in the new coordinate space defined by the BeginGroup.
        ///            // Notice how (0,0) has now been moved on-screen.
        ///            GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
        ///        }
        ///        // The group is now ended.
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public class GroupScope : Scope
        {
            ///<summary>Create a new GroupScope and begin the corresponding group.</summary>
            ///<param name="position">Rectangle on the screen to use for the group.</param>
            public GroupScope(Rect position)
            {
                BeginGroup(position);
            }

            ///<summary>Create a new GroupScope and begin the corresponding group.</summary>
            ///<param name="position">Rectangle on the screen to use for the group.</param>
            ///<param name="text">Text to display on the group.</param>
            public GroupScope(Rect position, string text)
            {
                BeginGroup(position, text);
            }

            ///<summary>Create a new GroupScope and begin the corresponding group.</summary>
            ///<param name="position">Rectangle on the screen to use for the group.</param>
            ///<param name="image">
            ///  <see cref="Texture" /> to display on the group.</param>
            public GroupScope(Rect position, Texture image)
            {
                BeginGroup(position, image);
            }

            ///<summary>Create a new GroupScope and begin the corresponding group.</summary>
            ///<param name="position">Rectangle on the screen to use for the group.</param>
            ///<param name="content">Text, image and tooltip for this group. If supplied, any mouse clicks are "captured" by the group and not If left out, no background is rendered, and mouse clicks are passed.</param>
            public GroupScope(Rect position, GUIContent content)
            {
                BeginGroup(position, content);
            }

            ///<summary>Create a new GroupScope and begin the corresponding group.</summary>
            ///<param name="position">Rectangle on the screen to use for the group.</param>
            ///<param name="style">The style to use for the background.</param>
            public GroupScope(Rect position, GUIStyle style)
            {
                BeginGroup(position, style);
            }

            ///<summary>Create a new GroupScope and begin the corresponding group.</summary>
            ///<param name="position">Rectangle on the screen to use for the group.</param>
            ///<param name="text">Text to display on the group.</param>
            ///<param name="style">The style to use for the background.</param>
            public GroupScope(Rect position, string text, GUIStyle style)
            {
                BeginGroup(position, text, style);
            }

            ///<summary>Create a new GroupScope and begin the corresponding group.</summary>
            ///<param name="position">Rectangle on the screen to use for the group.</param>
            ///<param name="image">
            ///  <see cref="Texture" /> to display on the group.</param>
            ///<param name="style">The style to use for the background.</param>
            public GroupScope(Rect position, Texture image, GUIStyle style)
            {
                BeginGroup(position, image, style);
            }

            protected override void CloseScope()
            {
                EndGroup();
            }
        }

        ///<summary>Disposable helper class for managing <see cref="BeginScrollView" /> / <see cref="EndScrollView" />.</summary>
        ///<remarks>::ref::BeginScrollView is called at construction, and <see cref="EndScrollView" /> is called when the instance is disposed.
        ///ScrollViews let you make a smaller area on-screen look 'into' a much larger area, using scrollbars placed on the sides of the ScrollView.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The position of the scrolling viewport
        ///    public Vector2 scrollPosition = Vector2.zero;
        ///    void OnGUI()
        ///    {
        ///        // An absolute-positioned example: We make a scrollview that has a really large client
        ///        // rect and put it in a small rect on the screen.
        ///        using (var scrollScope = new GUI.ScrollViewScope(new Rect(10, 300, 100, 100), scrollPosition, new Rect(0, 0, 220, 200)))
        ///        {
        ///            scrollPosition = scrollScope.scrollPosition;
        ///
        ///            // Make four buttons - one in each corner. The coordinate system is defined
        ///            // by the last parameter to the ScrollScope constructor.
        ///            GUI.Button(new Rect(0, 0, 100, 20), "Top-left");
        ///            GUI.Button(new Rect(120, 0, 100, 20), "Top-right");
        ///            GUI.Button(new Rect(0, 180, 100, 20), "Bottom-left");
        ///            GUI.Button(new Rect(120, 180, 100, 20), "Bottom-right");
        ///        }
        ///        // Now the scroll view is ended.
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public class ScrollViewScope : Scope
        {
            ///<summary>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</summary>
            public Vector2 scrollPosition { get; private set; }
            ///<summary>Whether this ScrollView should handle scroll wheel events. (default: true).</summary>
            public bool handleScrollWheel { get; set; }

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
            ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
            ///<param name="viewRect">The rectangle used inside the scrollview.</param>
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect)
            {
                handleScrollWheel = true;
            #pragma warning disable UAL0015 // ScrollViewScope is used()-scoped within a single OnGUI call and never outlives it; BeginScrollView's transitive side effect on GUIStateObjects.s_StateCache is benign
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect);
            #pragma warning restore UAL0015
            }

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
            ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
            ///<param name="viewRect">The rectangle used inside the scrollview.</param>
            ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when <c>clientRect</c> is wider than <c>position</c>.</param>
            ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when <c>clientRect</c> is taller than <c>position</c>.</param>
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical)
            {
                handleScrollWheel = true;
            #pragma warning disable UAL0015 // ScrollViewScope is used()-scoped within a single OnGUI call and never outlives it; BeginScrollView's transitive side effect on GUIStateObjects.s_StateCache is benign
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical);
            #pragma warning restore UAL0015
            }

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
            ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
            ///<param name="viewRect">The rectangle used inside the scrollview.</param>
            ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
            {
                handleScrollWheel = true;
            #pragma warning disable UAL0015 // ScrollViewScope is used()-scoped within a single OnGUI call and never outlives it; BeginScrollView's transitive side effect on GUIStateObjects.s_StateCache is benign
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, horizontalScrollbar, verticalScrollbar);
            #pragma warning restore UAL0015
            }

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="position">Rectangle on the screen to use for the ScrollView.</param>
            ///<param name="scrollPosition">The pixel distance that the view is scrolled in the X and Y directions.</param>
            ///<param name="viewRect">The rectangle used inside the scrollview.</param>
            ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when <c>clientRect</c> is wider than <c>position</c>.</param>
            ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when <c>clientRect</c> is taller than <c>position</c>.</param>
            ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            public ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
            {
                handleScrollWheel = true;
            #pragma warning disable UAL0015 // ScrollViewScope is used()-scoped within a single OnGUI call and never outlives it; BeginScrollView's transitive side effect on GUIStateObjects.s_StateCache is benign
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar);
            #pragma warning restore UAL0015
            }

            internal ScrollViewScope(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
            {
                handleScrollWheel = true;
            #pragma warning disable UAL0015 // ScrollViewScope is used()-scoped within a single OnGUI call and never outlives it; BeginScrollView's transitive side effect on GUIStateObjects.s_StateCache is benign
                this.scrollPosition = BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
            #pragma warning restore UAL0015
            }

            protected override void CloseScope()
            {
                EndScrollView(handleScrollWheel);
            }
        }

        ///<exclude />
        public class ClipScope : Scope
        {
            ///<exclude />
            public ClipScope(Rect position)
            {
                BeginClip(position);
            }

            internal ClipScope(Rect position, Vector2 scrollOffset)
            {
                BeginClip(position, scrollOffset, new Vector2(), false);
            }

            ///<exclude />
            protected override void CloseScope()
            {
                EndClip();
            }
        }

        internal struct ColorScope : IDisposable
        {
            private bool m_Disposed;
            private Color m_PreviousColor;

            public ColorScope(Color newColor)
            {
                m_Disposed = false;
                m_PreviousColor = GUI.color;
                GUI.color = newColor;
            }

            public ColorScope(float r, float g, float b, float a = 1.0f) : this(new Color(r, g, b, a))
            {
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                GUI.color = m_PreviousColor;
            }
        }

        internal struct BackgroundColorScope : IDisposable
        {
            private bool m_Disposed;
            private Color m_PreviousColor;

            public BackgroundColorScope(Color newColor)
            {
                m_Disposed = false;
                m_PreviousColor = GUI.backgroundColor;
                GUI.backgroundColor = newColor;
            }

            public BackgroundColorScope(float r, float g, float b, float a = 1.0f) : this(new Color(r, g, b, a))
            {
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                GUI.backgroundColor = m_PreviousColor;
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
