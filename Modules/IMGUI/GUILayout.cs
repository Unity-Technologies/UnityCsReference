// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: IMGUIFramework not yet converted
using System;

namespace UnityEngine
{
    ///<summary>The GUILayout class is the interface for Unity gui with automatic layout. Unlike the standard GUI class which requires manual coordinates, GUILayout arranges controls based on their content and container.</summary>
    ///<remarks>.</remarks>
    ///<example>
    ///  <code><![CDATA[ // This example creates a simple UI using the IMGUI system.
    ///using UnityEngine;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    private string playerName = "";
    ///    private int playerAge = 0;
    ///
    ///    void OnGUI()
    ///    {
    ///        // Begin a horizontal group
    ///        GUILayout.BeginHorizontal();
    ///
    ///        // Add GUI elements that will be arranged horizontally
    ///        GUILayout.Label("Name: ", GUILayout.Width(50));
    ///        playerName = GUILayout.TextField(playerName, GUILayout.Width(100));
    ///
    ///        GUILayout.Label("Age: ", GUILayout.Width(40));
    ///        playerAge = int.Parse(GUILayout.TextField(playerAge.ToString(), GUILayout.Width(30)));
    ///
    ///        // End the horizontal group
    ///        GUILayout.EndHorizontal();
    ///    }
    ///}]]></code>
    ///</example>
    ///<seealso href="xref:gui-Layout">GUI Layout tutorial</seealso>
    public partial class GUILayout
    {
        ///<summary>Make an auto-layout label.</summary>
        ///<remarks>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control
        ///
        ///<img src="GUILayoutLabel.png" />
        ///
        ///Label in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the label.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a texture and a label after the Texture
        ///    // using GUILayout.
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Label(tex);
        ///        GUILayout.Label("This is an sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Label(Texture image, params GUILayoutOption[] options)                      { DoLabel(GUIContent.Temp(image), GUI.skin.label, options); }
        ///<summary>Make an auto-layout label.</summary>
        ///<remarks>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control
        ///
        ///<img src="GUILayoutLabel.png" />
        ///
        ///Label in the Game View.</remarks>
        ///<param name="text">Text to display on the label.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a texture and a label after the Texture
        ///    // using GUILayout.
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Label(tex);
        ///        GUILayout.Label("This is an sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Label(string text, params GUILayoutOption[] options)                        { DoLabel(GUIContent.Temp(text), GUI.skin.label, options); }
        ///<summary>Make an auto-layout label.</summary>
        ///<remarks>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control
        ///
        ///<img src="GUILayoutLabel.png" />
        ///
        ///Label in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this label.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a texture and a label after the Texture
        ///    // using GUILayout.
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Label(tex);
        ///        GUILayout.Label("This is an sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Label(GUIContent content, params GUILayoutOption[] options)                 { DoLabel(content, GUI.skin.label, options); }
        ///<summary>Make an auto-layout label.</summary>
        ///<remarks>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control
        ///
        ///<img src="GUILayoutLabel.png" />
        ///
        ///Label in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the label.</param>
        ///<param name="style">The style to use. If left out, the <c>label</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a texture and a label after the Texture
        ///    // using GUILayout.
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Label(tex);
        ///        GUILayout.Label("This is an sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Label(Texture image, GUIStyle style, params GUILayoutOption[] options)      { DoLabel(GUIContent.Temp(image), style, options); }
        ///<summary>Make an auto-layout label.</summary>
        ///<remarks>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control
        ///
        ///<img src="GUILayoutLabel.png" />
        ///
        ///Label in the Game View.</remarks>
        ///<param name="text">Text to display on the label.</param>
        ///<param name="style">The style to use. If left out, the <c>label</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a texture and a label after the Texture
        ///    // using GUILayout.
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Label(tex);
        ///        GUILayout.Label("This is an sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Label(string text, GUIStyle style, params GUILayoutOption[] options)        { DoLabel(GUIContent.Temp(text), style, options); }
        ///<summary>Make an auto-layout label.</summary>
        ///<remarks>Labels have no user interaction, do not catch mouse clicks and are always rendered in normal style. If you want to make a control that responds visually to user input, use a <see cref="Box" /> control
        ///
        ///<img src="GUILayoutLabel.png" />
        ///
        ///Label in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this label.</param>
        ///<param name="style">The style to use. If left out, the <c>label</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a texture and a label after the Texture
        ///    // using GUILayout.
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Label(tex);
        ///        GUILayout.Label("This is an sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Label(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { DoLabel(content, style, options); }
        static void DoLabel(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { GUI.Label(GUILayoutUtility.GetRect(content, style, options), content, style); }

        ///<summary>Make an auto-layout box.</summary>
        ///<remarks>This will make a box that contains static text or images but not other GUI controls. If you want to make a rectangular container for a set of GUI controls, use one of the grouping functions (<see cref="BeginHorizontal" />, <see cref="BeginVertical" />, <see cref="BeginArea" />, etc...).
        ///
        ///<img src="GUILayoutBox.png" />
        ///
        ///Boxes in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the box.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Box(tex);
        ///        GUILayout.Box("This is a sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Box(Texture image, params GUILayoutOption[] options)                        { DoBox(GUIContent.Temp(image), GUI.skin.box, options); }
        ///<summary>Make an auto-layout box.</summary>
        ///<remarks>This will make a box that contains static text or images but not other GUI controls. If you want to make a rectangular container for a set of GUI controls, use one of the grouping functions (<see cref="BeginHorizontal" />, <see cref="BeginVertical" />, <see cref="BeginArea" />, etc...).
        ///
        ///<img src="GUILayoutBox.png" />
        ///
        ///Boxes in the Game View.</remarks>
        ///<param name="text">Text to display on the box.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Box(tex);
        ///        GUILayout.Box("This is a sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Box(string text, params GUILayoutOption[] options)                          { DoBox(GUIContent.Temp(text), GUI.skin.box, options); }
        ///<summary>Make an auto-layout box.</summary>
        ///<remarks>This will make a box that contains static text or images but not other GUI controls. If you want to make a rectangular container for a set of GUI controls, use one of the grouping functions (<see cref="BeginHorizontal" />, <see cref="BeginVertical" />, <see cref="BeginArea" />, etc...).
        ///
        ///<img src="GUILayoutBox.png" />
        ///
        ///Boxes in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this box.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Box(tex);
        ///        GUILayout.Box("This is a sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Box(GUIContent content, params GUILayoutOption[] options)                   { DoBox(content, GUI.skin.box, options); }
        ///<summary>Make an auto-layout box.</summary>
        ///<remarks>This will make a box that contains static text or images but not other GUI controls. If you want to make a rectangular container for a set of GUI controls, use one of the grouping functions (<see cref="BeginHorizontal" />, <see cref="BeginVertical" />, <see cref="BeginArea" />, etc...).
        ///
        ///<img src="GUILayoutBox.png" />
        ///
        ///Boxes in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the box.</param>
        ///<param name="style">The style to use. If left out, the <c>box</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Box(tex);
        ///        GUILayout.Box("This is a sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Box(Texture image, GUIStyle style, params GUILayoutOption[] options)        { DoBox(GUIContent.Temp(image), style, options); }
        ///<summary>Make an auto-layout box.</summary>
        ///<remarks>This will make a box that contains static text or images but not other GUI controls. If you want to make a rectangular container for a set of GUI controls, use one of the grouping functions (<see cref="BeginHorizontal" />, <see cref="BeginVertical" />, <see cref="BeginArea" />, etc...).
        ///
        ///<img src="GUILayoutBox.png" />
        ///
        ///Boxes in the Game View.</remarks>
        ///<param name="text">Text to display on the box.</param>
        ///<param name="style">The style to use. If left out, the <c>box</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Box(tex);
        ///        GUILayout.Box("This is a sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Box(string text, GUIStyle style, params GUILayoutOption[] options)          { DoBox(GUIContent.Temp(text), style, options); }
        ///<summary>Make an auto-layout box.</summary>
        ///<remarks>This will make a box that contains static text or images but not other GUI controls. If you want to make a rectangular container for a set of GUI controls, use one of the grouping functions (<see cref="BeginHorizontal" />, <see cref="BeginVertical" />, <see cref="BeginArea" />, etc...).
        ///
        ///<img src="GUILayoutBox.png" />
        ///
        ///Boxes in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this box.</param>
        ///<param name="style">The style to use. If left out, the <c>box</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("Missing texture, assign a texture in the inspector");
        ///        }
        ///        GUILayout.Box(tex);
        ///        GUILayout.Box("This is a sized label");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public void Box(GUIContent content, GUIStyle style, params GUILayoutOption[] options)   { DoBox(content, style, options); }
        static void DoBox(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { GUI.Box(GUILayoutUtility.GetRect(content, style, options), content, style); }

        ///<summary>Make a single press button.</summary>
        ///<remarks>Create a <see cref="Button" /> that can be pressed and released as
        ///                a normal button.  When this <see cref="Button" /> is released the Button returns the
        ///                expected <c>true</c> value. If the mouse is moved off the button it is not clicked.
        ///
        ///<img src="GUILayoutButton.png" />
        ///
        ///Buttons in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.Button(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.Button("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Button(Texture image, params GUILayoutOption[] options)                         { return DoButton(GUIContent.Temp(image), GUI.skin.button, options); }
        ///<summary>Make a single press button.</summary>
        ///<remarks>Create a <see cref="Button" /> that can be pressed and released as
        ///                a normal button.  When this <see cref="Button" /> is released the Button returns the
        ///                expected <c>true</c> value. If the mouse is moved off the button it is not clicked.
        ///
        ///<img src="GUILayoutButton.png" />
        ///
        ///Buttons in the Game View.</remarks>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.Button(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.Button("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Button(string text, params GUILayoutOption[] options)                           { return DoButton(GUIContent.Temp(text), GUI.skin.button, options); }
        ///<summary>Make a single press button.</summary>
        ///<remarks>Create a <see cref="Button" /> that can be pressed and released as
        ///                a normal button.  When this <see cref="Button" /> is released the Button returns the
        ///                expected <c>true</c> value. If the mouse is moved off the button it is not clicked.
        ///
        ///<img src="GUILayoutButton.png" />
        ///
        ///Buttons in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.Button(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.Button("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Button(GUIContent content, params GUILayoutOption[] options)                    { return DoButton(content, GUI.skin.button, options); }
        ///<summary>Make a single press button.</summary>
        ///<remarks>Create a <see cref="Button" /> that can be pressed and released as
        ///                a normal button.  When this <see cref="Button" /> is released the Button returns the
        ///                expected <c>true</c> value. If the mouse is moved off the button it is not clicked.
        ///
        ///<img src="GUILayoutButton.png" />
        ///
        ///Buttons in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.Button(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.Button("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Button(Texture image, GUIStyle style, params GUILayoutOption[] options)         { return DoButton(GUIContent.Temp(image), style, options); }
        ///<summary>Make a single press button.</summary>
        ///<remarks>Create a <see cref="Button" /> that can be pressed and released as
        ///                a normal button.  When this <see cref="Button" /> is released the Button returns the
        ///                expected <c>true</c> value. If the mouse is moved off the button it is not clicked.
        ///
        ///<img src="GUILayoutButton.png" />
        ///
        ///Buttons in the Game View.</remarks>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.Button(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.Button("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Button(string text, GUIStyle style, params GUILayoutOption[] options)           { return DoButton(GUIContent.Temp(text), style, options); }
        ///<summary>Make a single press button.</summary>
        ///<remarks>Create a <see cref="Button" /> that can be pressed and released as
        ///                a normal button.  When this <see cref="Button" /> is released the Button returns the
        ///                expected <c>true</c> value. If the mouse is moved off the button it is not clicked.
        ///
        ///<img src="GUILayoutButton.png" />
        ///
        ///Buttons in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the users clicks the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.Button(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.Button("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options)    { return DoButton(content, style, options); }
        static bool DoButton(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { return GUI.Button(GUILayoutUtility.GetRect(content, style, options), content, style); }

        ///<summary>Make a repeating button. The button returns true as long as the user holds down the mouse.</summary>
        ///<remarks>
        ///  <img src="GUILayoutButton.png" />
        ///
        ///Repeat Buttons in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the holds down the mouse.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.RepeatButton(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.RepeatButton("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool RepeatButton(Texture image, params GUILayoutOption[] options)                       { return DoRepeatButton(GUIContent.Temp(image), GUI.skin.button, options); }
        ///<summary>Make a repeating button. The button returns true as long as the user holds down the mouse.</summary>
        ///<remarks>
        ///  <img src="GUILayoutButton.png" />
        ///
        ///Repeat Buttons in the Game View.</remarks>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the holds down the mouse.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.RepeatButton(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.RepeatButton("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool RepeatButton(string text, params GUILayoutOption[] options)                         { return DoRepeatButton(GUIContent.Temp(text), GUI.skin.button, options); }
        ///<summary>Make a repeating button. The button returns true as long as the user holds down the mouse.</summary>
        ///<remarks>
        ///  <img src="GUILayoutButton.png" />
        ///
        ///Repeat Buttons in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the holds down the mouse.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.RepeatButton(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.RepeatButton("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool RepeatButton(GUIContent content, params GUILayoutOption[] options)                  { return DoRepeatButton(content, GUI.skin.button, options); }
        ///<summary>Make a repeating button. The button returns true as long as the user holds down the mouse.</summary>
        ///<remarks>
        ///  <img src="GUILayoutButton.png" />
        ///
        ///Repeat Buttons in the Game View.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the holds down the mouse.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.RepeatButton(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.RepeatButton("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool RepeatButton(Texture image, GUIStyle style, params GUILayoutOption[] options)       { return DoRepeatButton(GUIContent.Temp(image), style, options); }
        ///<summary>Make a repeating button. The button returns true as long as the user holds down the mouse.</summary>
        ///<remarks>
        ///  <img src="GUILayoutButton.png" />
        ///
        ///Repeat Buttons in the Game View.</remarks>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the holds down the mouse.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.RepeatButton(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.RepeatButton("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool RepeatButton(string text, GUIStyle style, params GUILayoutOption[] options)         { return DoRepeatButton(GUIContent.Temp(text), style, options); }
        ///<summary>Make a repeating button. The button returns true as long as the user holds down the mouse.</summary>
        ///<remarks>
        ///  <img src="GUILayoutButton.png" />
        ///
        ///Repeat Buttons in the Game View.</remarks>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>true when the holds down the mouse.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a button with an image and a button with text
        ///    Texture tex;
        ///    void OnGUI()
        ///    {
        ///        if (!tex)
        ///        {
        ///            Debug.LogError("No texture found, please assign a texture on the inspector");
        ///        }
        ///
        ///        if (GUILayout.RepeatButton(tex))
        ///        {
        ///            Debug.Log("Clicked the image");
        ///        }
        ///        if (GUILayout.RepeatButton("I am a regular Automatic Layout Button"))
        ///        {
        ///            Debug.Log("Clicked Button");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool RepeatButton(GUIContent content, GUIStyle style, params GUILayoutOption[] options)  { return DoRepeatButton(content, style, options); }
        static bool DoRepeatButton(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { return GUI.RepeatButton(GUILayoutUtility.GetRect(content, style, options), content, style); }

        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextField.png" />
        ///
        ///Text field in the GameView.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextField(stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextField(string text, params GUILayoutOption[] options)                                  { return DoTextField(text, -1, false, GUI.skin.textField, options); }
        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextField.png" />
        ///
        ///Text field in the GameView.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextField(stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextField(string text, int maxLength, params GUILayoutOption[] options)                   { return DoTextField(text, maxLength, false, GUI.skin.textField, options); }
        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextField.png" />
        ///
        ///Text field in the GameView.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="style">The style to use. If left out, the <c>textArea</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextField(stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextField(string text, GUIStyle style, params GUILayoutOption[] options)                  { return DoTextField(text, -1, false, style, options); }
        ///<summary>Make a single-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextField.png" />
        ///
        ///Text field in the GameView.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="style">The style to use. If left out, the <c>textArea</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a text field that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextField(stringToEdit, 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextField(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options)   { return DoTextField(text, maxLength, false, style, options); }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<remarks>
        ///  <img src="GUILayoutPasswordField.png" />
        ///
        ///Password field in the Game View.</remarks>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a password field that modifies passwordToEdit.
        ///        passwordToEdit = GUILayout.PasswordField(passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(string password, char maskChar, params GUILayoutOption[] options)
        {
            return PasswordField(password, maskChar, -1, GUI.skin.textField, options);
        }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<remarks>
        ///  <img src="GUILayoutPasswordField.png" />
        ///
        ///Password field in the Game View.</remarks>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a password field that modifies passwordToEdit.
        ///        passwordToEdit = GUILayout.PasswordField(passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(string password, char maskChar, int maxLength, params GUILayoutOption[] options)
        {
            return PasswordField(password, maskChar, maxLength, GUI.skin.textField, options);
        }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<remarks>
        ///  <img src="GUILayoutPasswordField.png" />
        ///
        ///Password field in the Game View.</remarks>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a password field that modifies passwordToEdit.
        ///        passwordToEdit = GUILayout.PasswordField(passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(string password, char maskChar, GUIStyle style, params GUILayoutOption[] options)
        {
            return PasswordField(password, maskChar, -1, style, options);
        }

        ///<summary>Make a text field where the user can enter a password.</summary>
        ///<remarks>
        ///  <img src="GUILayoutPasswordField.png" />
        ///
        ///Password field in the Game View.</remarks>
        ///<param name="password">Password to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maskChar">Character to mask the password with.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The edited password.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string passwordToEdit = "My Password";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a password field that modifies passwordToEdit.
        ///        passwordToEdit = GUILayout.PasswordField(passwordToEdit, "*"[0], 25);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static string PasswordField(string password, char maskChar, int maxLength, GUIStyle style, params GUILayoutOption[] options)
        {
            GUIContent t = GUIContent.Temp(GUI.PasswordFieldGetStrToShow(password, maskChar));
            return GUI.PasswordField(GUILayoutUtility.GetRect(t, GUI.skin.textField, options), password, maskChar, maxLength, style);
        }

        ///<summary>Make a multi-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextArea.png" />
        ///
        ///Text area in the Game View.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&amp;amp;lt;br&amp;amp;gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextArea(stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextArea(string text, params GUILayoutOption[] options)                                   { return DoTextField(text, -1, true, GUI.skin.textArea, options); }
        ///<summary>Make a multi-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextArea.png" />
        ///
        ///Text area in the Game View.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&amp;amp;lt;br&amp;amp;gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextArea(stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextArea(string text, int maxLength, params GUILayoutOption[] options)                    { return DoTextField(text, maxLength, true, GUI.skin.textArea, options); }
        ///<summary>Make a multi-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextArea.png" />
        ///
        ///Text area in the Game View.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&amp;amp;lt;br&amp;amp;gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextArea(stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextArea(string text, GUIStyle style, params GUILayoutOption[] options)                   { return DoTextField(text, -1, true, style, options); }
        ///<summary>Make a multi-line text field where the user can edit a string.</summary>
        ///<remarks>
        ///  <img src="GUILayoutTextArea.png" />
        ///
        ///Text area in the Game View.</remarks>
        ///<param name="text">Text to edit. The return value of this function should be assigned back to the string as shown in the example.</param>
        ///<param name="maxLength">The maximum length of the string. If left out, the user can type for ever and ever.</param>
        ///<param name="style">The style to use. If left out, the <c>textField</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&amp;amp;lt;br&amp;amp;gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The edited string.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    string stringToEdit = "Hello World\nI've got 2 lines...";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Make a multiline text area that modifies stringToEdit.
        ///        stringToEdit = GUILayout.TextArea(stringToEdit, 200);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static string TextArea(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options)    { return DoTextField(text, maxLength, true, style, options); }

        static string DoTextField(string text, int maxLength, bool multiline, GUIStyle style, GUILayoutOption[] options)
        {
            int id = GUIUtility.GetControlID(FocusType.Keyboard);
            GUIContent content = GUIContent.Temp(text);
            Rect r;
            if (GUIUtility.keyboardControl != id)
                content = GUIContent.Temp(text);
            else
                content = GUIContent.Temp(text + GUIUtility.compositionString);

            r = GUILayoutUtility.GetRect(content, style, options);
            if (GUIUtility.keyboardControl == id)
                content = GUIContent.Temp(text);
            GUI.DoTextField(r, id, content, multiline, maxLength, style);
            return content.text;
        }

        ///<summary>Make an on/off toggle button.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToggle.png" />
        ///
        ///Toggle button in the Game View.</remarks>
        ///<param name="value">Is the button on or off?</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws 2 toggle controls, one with a text, the other with an image.
        ///    Texture aTexture;
        ///
        ///    bool toggleTxt = false;
        ///    bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///        toggleTxt = GUILayout.Toggle(toggleTxt, "A Toggle text");
        ///        toggleImg = GUILayout.Toggle(toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Toggle(bool value, Texture image, params GUILayoutOption[] options)                         { return DoToggle(value, GUIContent.Temp(image), GUI.skin.toggle, options); }
        ///<summary>Make an on/off toggle button.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToggle.png" />
        ///
        ///Toggle button in the Game View.</remarks>
        ///<param name="value">Is the button on or off?</param>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws 2 toggle controls, one with a text, the other with an image.
        ///    Texture aTexture;
        ///
        ///    bool toggleTxt = false;
        ///    bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///        toggleTxt = GUILayout.Toggle(toggleTxt, "A Toggle text");
        ///        toggleImg = GUILayout.Toggle(toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Toggle(bool value, string text, params GUILayoutOption[] options)                           { return DoToggle(value, GUIContent.Temp(text), GUI.skin.toggle, options); }
        ///<summary>Make an on/off toggle button.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToggle.png" />
        ///
        ///Toggle button in the Game View.</remarks>
        ///<param name="value">Is the button on or off?</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws 2 toggle controls, one with a text, the other with an image.
        ///    Texture aTexture;
        ///
        ///    bool toggleTxt = false;
        ///    bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///        toggleTxt = GUILayout.Toggle(toggleTxt, "A Toggle text");
        ///        toggleImg = GUILayout.Toggle(toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Toggle(bool value, GUIContent content, params GUILayoutOption[] options)                    { return DoToggle(value, content, GUI.skin.toggle, options); }
        ///<summary>Make an on/off toggle button.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToggle.png" />
        ///
        ///Toggle button in the Game View.</remarks>
        ///<param name="value">Is the button on or off?</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws 2 toggle controls, one with a text, the other with an image.
        ///    Texture aTexture;
        ///
        ///    bool toggleTxt = false;
        ///    bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///        toggleTxt = GUILayout.Toggle(toggleTxt, "A Toggle text");
        ///        toggleImg = GUILayout.Toggle(toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Toggle(bool value, Texture image, GUIStyle style, params GUILayoutOption[] options)         { return DoToggle(value, GUIContent.Temp(image), style, options); }
        ///<summary>Make an on/off toggle button.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToggle.png" />
        ///
        ///Toggle button in the Game View.</remarks>
        ///<param name="value">Is the button on or off?</param>
        ///<param name="text">Text to display on the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws 2 toggle controls, one with a text, the other with an image.
        ///    Texture aTexture;
        ///
        ///    bool toggleTxt = false;
        ///    bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///        toggleTxt = GUILayout.Toggle(toggleTxt, "A Toggle text");
        ///        toggleImg = GUILayout.Toggle(toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Toggle(bool value, string text, GUIStyle style, params GUILayoutOption[] options)           { return DoToggle(value, GUIContent.Temp(text), style, options); }
        ///<summary>Make an on/off toggle button.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToggle.png" />
        ///
        ///Toggle button in the Game View.</remarks>
        ///<param name="value">Is the button on or off?</param>
        ///<param name="content">Text, image and tooltip for this button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The new value of the button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws 2 toggle controls, one with a text, the other with an image.
        ///    Texture aTexture;
        ///
        ///    bool toggleTxt = false;
        ///    bool toggleImg = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Please assign a texture in the inspector.");
        ///            return;
        ///        }
        ///        toggleTxt = GUILayout.Toggle(toggleTxt, "A Toggle text");
        ///        toggleImg = GUILayout.Toggle(toggleImg, aTexture);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        static public bool Toggle(bool value, GUIContent content, GUIStyle style, params GUILayoutOption[] options)    { return DoToggle(value, content, style, options); }

        static bool DoToggle(bool value, GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { return GUI.Toggle(GUILayoutUtility.GetRect(content, style, options), value, content, style); }

        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="texts">An array of strings to show on the buttons.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, string[] texts, params GUILayoutOption[] options)                      { return Toolbar(selected, GUIContent.Temp(texts), GUI.skin.button, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="images">An array of textures on the buttons.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, Texture[] images, params GUILayoutOption[] options)                    { return Toolbar(selected, GUIContent.Temp(images), GUI.skin.button, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="contents">An array of text, image and tooltips for the button.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, GUIContent[] contents, params GUILayoutOption[] options)                { return Toolbar(selected, contents, GUI.skin.button, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="texts">An array of strings to show on the buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, string[] texts, GUIStyle style, params GUILayoutOption[] options)      { return Toolbar(selected, GUIContent.Temp(texts), style, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="images">An array of textures on the buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, Texture[] images, GUIStyle style, params GUILayoutOption[] options)    { return Toolbar(selected, GUIContent.Temp(images), style, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="texts">An array of strings to show on the buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<param name="buttonSize">Determines how toolbar button size is calculated.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, string[] texts, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options)   { return Toolbar(selected, GUIContent.Temp(texts), style, buttonSize, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="images">An array of textures on the buttons.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<param name="buttonSize">Determines how toolbar button size is calculated.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, Texture[] images, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options) { return Toolbar(selected, GUIContent.Temp(images), style, buttonSize, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="contents">An array of text, image and tooltips for the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, GUIContent[] contents, GUIStyle style, params GUILayoutOption[] options) { return Toolbar(selected, contents, style, GUI.ToolbarButtonSize.Fixed, options); }
        ///<summary>Make a toolbar.</summary>
        ///<remarks>
        ///  <img src="GUILayoutToolbar.png" />
        ///
        ///Toolbar in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="contents">An array of text, image and tooltips for the button.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<param name="buttonSize">Determines how toolbar button size is calculated.</param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int toolbarInt = 0;
        ///    string[] toolbarStrings = {"Toolbar1", "Toolbar2", "Toolbar3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int Toolbar(int selected, GUIContent[] contents, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options) { return Toolbar(selected, contents, null, style, buttonSize, options); }
        public static int Toolbar(int selected, GUIContent[] contents, bool[] enabled, GUIStyle style, params GUILayoutOption[] options) { return Toolbar(selected, contents, enabled, style, GUI.ToolbarButtonSize.Fixed, options); }
        internal static int Toolbar(int selected, GUIContent[] contents, bool[] enabled, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, params GUILayoutOption[] options) { return Toolbar(selected, contents, enabled, style, firstStyle, midStyle, lastStyle, GUI.ToolbarButtonSize.Fixed, options); }

        public static int Toolbar(int selected, GUIContent[] contents, bool[] enabled, GUIStyle style, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options)
        {
            GUIStyle firstStyle, midStyle, lastStyle;
            GUI.FindStyles(ref style, out firstStyle, out midStyle, out lastStyle, "left", "mid", "right");

            return Toolbar(selected, contents, enabled, style, firstStyle, midStyle, lastStyle, buttonSize, options);
        }

        internal static int Toolbar(int selected, GUIContent[] contents, bool[] enabled, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, GUI.ToolbarButtonSize buttonSize, params GUILayoutOption[] options)
        {
            Vector2 size = new Vector2();
            int count = contents.Length;
            GUIStyle currentStyle = count > 1 ? firstStyle : style;
            GUIStyle nextStyle = count > 1 ? midStyle : style;
            GUIStyle endStyle = count > 1 ? lastStyle : style;
            float margins = 0;

            for (int i = 0; i < contents.Length; i++)
            {
                if (i == count - 2)
                    nextStyle = endStyle;

                Vector2 thisSize = currentStyle.CalcSize(contents[i]);
                switch (buttonSize)
                {
                    case GUI.ToolbarButtonSize.Fixed:
                        if (thisSize.x > size.x)
                            size.x = thisSize.x;
                        break;
                    case GUI.ToolbarButtonSize.FitToContents:
                        size.x += thisSize.x;
                        break;
                }

                if (thisSize.y > size.y)
                    size.y = thisSize.y;

                // add spacing
                if (i == count - 1)
                    margins += currentStyle.margin.right;
                else
                    margins += Mathf.Max(currentStyle.margin.right, nextStyle.margin.left);

                currentStyle = nextStyle;
            }

            switch (buttonSize)
            {
                case GUI.ToolbarButtonSize.Fixed:
                    size.x = size.x * contents.Length + margins;
                    break;
                case GUI.ToolbarButtonSize.FitToContents:
                    size.x += margins;
                    break;
            }

            return GUI.Toolbar(GUILayoutUtility.GetRect(size.x, size.y, style, options), selected, contents, null, style, firstStyle, midStyle, lastStyle, buttonSize, enabled);
        }

        ///<summary>Make a Selection Grid.</summary>
        ///<remarks>
        ///  <img src="GUILayoutSelectionGrid.png" />
        ///
        ///Selection grid in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="texts">An array of strings to show on the buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The elements will be scaled to fit unless the style defines a fixedWidth to use. The height of the control will be determined from the number of elements.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int selGridInt = 0;
        ///    string[] selStrings = {"radio1", "radio2", "radio3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical("Box");
        ///        selGridInt = GUILayout.SelectionGrid(selGridInt, selStrings, 1);
        ///        if (GUILayout.Button("Start"))
        ///        {
        ///            Debug.Log("You chose " + selStrings[selGridInt]);
        ///        }
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int SelectionGrid(int selected, string[] texts, int xCount, params GUILayoutOption[] options)                    { return SelectionGrid(selected, GUIContent.Temp(texts), xCount, GUI.skin.button, options); }
        ///<summary>Make a Selection Grid.</summary>
        ///<remarks>
        ///  <img src="GUILayoutSelectionGrid.png" />
        ///
        ///Selection grid in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="images">An array of textures on the buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The elements will be scaled to fit unless the style defines a fixedWidth to use. The height of the control will be determined from the number of elements.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int selGridInt = 0;
        ///    string[] selStrings = {"radio1", "radio2", "radio3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical("Box");
        ///        selGridInt = GUILayout.SelectionGrid(selGridInt, selStrings, 1);
        ///        if (GUILayout.Button("Start"))
        ///        {
        ///            Debug.Log("You chose " + selStrings[selGridInt]);
        ///        }
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int SelectionGrid(int selected, Texture[] images, int xCount, params GUILayoutOption[] options)                  { return SelectionGrid(selected, GUIContent.Temp(images), xCount, GUI.skin.button, options); }
        ///<summary>Make a Selection Grid.</summary>
        ///<remarks>
        ///  <img src="GUILayoutSelectionGrid.png" />
        ///
        ///Selection grid in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The elements will be scaled to fit unless the style defines a fixedWidth to use. The height of the control will be determined from the number of elements.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int selGridInt = 0;
        ///    string[] selStrings = {"radio1", "radio2", "radio3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical("Box");
        ///        selGridInt = GUILayout.SelectionGrid(selGridInt, selStrings, 1);
        ///        if (GUILayout.Button("Start"))
        ///        {
        ///            Debug.Log("You chose " + selStrings[selGridInt]);
        ///        }
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int SelectionGrid(int selected, GUIContent[] content, int xCount, params GUILayoutOption[] options)              { return SelectionGrid(selected, content, xCount, GUI.skin.button, options); }
        ///<summary>Make a Selection Grid.</summary>
        ///<remarks>
        ///  <img src="GUILayoutSelectionGrid.png" />
        ///
        ///Selection grid in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="texts">An array of strings to show on the buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The elements will be scaled to fit unless the style defines a fixedWidth to use. The height of the control will be determined from the number of elements.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int selGridInt = 0;
        ///    string[] selStrings = {"radio1", "radio2", "radio3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical("Box");
        ///        selGridInt = GUILayout.SelectionGrid(selGridInt, selStrings, 1);
        ///        if (GUILayout.Button("Start"))
        ///        {
        ///            Debug.Log("You chose " + selStrings[selGridInt]);
        ///        }
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int SelectionGrid(int selected, string[] texts, int xCount, GUIStyle style, params GUILayoutOption[] options)    { return SelectionGrid(selected, GUIContent.Temp(texts), xCount, style, options); }
        ///<summary>Make a Selection Grid.</summary>
        ///<remarks>
        ///  <img src="GUILayoutSelectionGrid.png" />
        ///
        ///Selection grid in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="images">An array of textures on the buttons.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The elements will be scaled to fit unless the style defines a fixedWidth to use. The height of the control will be determined from the number of elements.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int selGridInt = 0;
        ///    string[] selStrings = {"radio1", "radio2", "radio3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical("Box");
        ///        selGridInt = GUILayout.SelectionGrid(selGridInt, selStrings, 1);
        ///        if (GUILayout.Button("Start"))
        ///        {
        ///            Debug.Log("You chose " + selStrings[selGridInt]);
        ///        }
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int SelectionGrid(int selected, Texture[] images, int xCount, GUIStyle style, params GUILayoutOption[] options)  { return SelectionGrid(selected, GUIContent.Temp(images), xCount, style, options); }
        ///<summary>Make a Selection Grid.</summary>
        ///<remarks>
        ///  <img src="GUILayoutSelectionGrid.png" />
        ///
        ///Selection grid in the Game View.</remarks>
        ///<param name="selected">The index of the selected button.</param>
        ///<param name="contents">An array of text, image and tooltips for the button.</param>
        ///<param name="xCount">How many elements to fit in the horizontal direction. The elements will be scaled to fit unless the style defines a fixedWidth to use. The height of the control will be determined from the number of elements.</param>
        ///<param name="style">The style to use. If left out, the <c>button</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The index of the selected button.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    int selGridInt = 0;
        ///    string[] selStrings = {"radio1", "radio2", "radio3"};
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical("Box");
        ///        selGridInt = GUILayout.SelectionGrid(selGridInt, selStrings, 1);
        ///        if (GUILayout.Button("Start"))
        ///        {
        ///            Debug.Log("You chose " + selStrings[selGridInt]);
        ///        }
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static int SelectionGrid(int selected, GUIContent[] contents, int xCount, GUIStyle style, params GUILayoutOption[] options)
        {
            return GUI.SelectionGrid(GUIGridSizer.GetRect(contents, xCount, style, options), selected, contents, xCount, style);
        }

        ///<summary>A horizontal slider the user can drag to change a value between a min and a max.</summary>
        ///<remarks>
        ///  <img src="GUILayoutHorizontalSlider.png" />
        ///
        ///Horizontal slider in the GameView.</remarks>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="leftValue">The value at the left end of the slider.</param>
        ///<param name="rightValue">The value at the right end of the slider.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float hSliderValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSliderValue = GUILayout.HorizontalSlider(hSliderValue, 0.0f, 10.0f);
        ///        GUILayout.Label("This is a text that makes space");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public float HorizontalSlider(float value, float leftValue, float rightValue, params GUILayoutOption[] options)
        { return DoHorizontalSlider(value, leftValue, rightValue, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, options); }
        ///<summary>A horizontal slider the user can drag to change a value between a min and a max.</summary>
        ///<remarks>
        ///  <img src="GUILayoutHorizontalSlider.png" />
        ///
        ///Horizontal slider in the GameView.</remarks>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="leftValue">The value at the left end of the slider.</param>
        ///<param name="rightValue">The value at the right end of the slider.</param>
        ///<param name="slider">The <see cref="GUIStyle" /> to use for displaying the dragging area. If left out, the <c>horizontalSlider</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="thumb">The <see cref="GUIStyle" /> to use for displaying draggable thumb. If left out, the <c>horizontalSliderThumb</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float hSliderValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSliderValue = GUILayout.HorizontalSlider(hSliderValue, 0.0f, 10.0f);
        ///        GUILayout.Label("This is a text that makes space");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public float HorizontalSlider(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options)
        { return DoHorizontalSlider(value, leftValue, rightValue, slider, thumb, options); }
        static float DoHorizontalSlider(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUILayoutOption[] options)
        { return GUI.HorizontalSlider(GUILayoutUtility.GetRect(GUIContent.Temp("mmmm"), slider, options), value, leftValue, rightValue, slider, thumb); }

        ///<summary>A vertical slider the user can drag to change a value between a min and a max.</summary>
        ///<remarks>
        ///  <img src="GUILayoutVerticalSlider.png" />
        ///
        ///Vertical slider in the Game View.</remarks>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a vertical slider control that goes from  10 (top) to 0 (bottom)
        ///    float vSliderValue = 0.0f;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSliderValue = GUILayout.VerticalSlider(vSliderValue, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public float VerticalSlider(float value, float leftValue, float rightValue, params GUILayoutOption[] options)
        { return DoVerticalSlider(value, leftValue, rightValue, GUI.skin.verticalSlider, GUI.skin.verticalSliderThumb, options); }
        ///<summary>A vertical slider the user can drag to change a value between a min and a max.</summary>
        ///<remarks>
        ///  <img src="GUILayoutVerticalSlider.png" />
        ///
        ///Vertical slider in the Game View.</remarks>
        ///<param name="value">The value the slider shows. This determines the position of the draggable thumb.</param>
        ///<param name="slider">The <see cref="GUIStyle" /> to use for displaying the dragging area. If left out, the <c>horizontalSlider</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="thumb">The <see cref="GUIStyle" /> to use for displaying draggable thumb. If left out, the <c>horizontalSliderThumb</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The value that has been set by the user.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a vertical slider control that goes from  10 (top) to 0 (bottom)
        ///    float vSliderValue = 0.0f;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSliderValue = GUILayout.VerticalSlider(vSliderValue, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public float VerticalSlider(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options)
        { return DoVerticalSlider(value, leftValue, rightValue, slider, thumb, options); }
        static float DoVerticalSlider(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options)
        { return GUI.VerticalSlider(GUILayoutUtility.GetRect(GUIContent.Temp("\n\n\n\n\n"), slider, options), value, leftValue, rightValue, slider, thumb); }

        ///<summary>Make a horizontal scrollbar.</summary>
        ///<remarks>
        ///  <para>A scrollbar control returns a float value that represents the position of the draggable "thumb" withtin the bar. You can use the value to adjust another GUI element to reflect the scroll position. However, most scrollable views can be handled more easily using a scroll view control.
        ///
        ///<img src="GUILayoutHorizontalScrollBar.png" />
        ///
        ///Horizontal Scrollbar in the Game View.</para>
        ///  <para>The styles of the scroll buttons at the end of the bar can be located in the current skin by adding "leftbutton" and "rightbutton" to the style name.
        ///The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</para>
        ///  <para />
        ///</remarks>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="leftValue">The value at the left end of the scrollbar.</param>
        ///<param name="rightValue">The value at the right end of the scrollbar.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float hSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///        GUILayout.Label("This is a text that makes space");
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
        ///    public float scrollPos = 0.5F;
        ///    // This will use the following style names to determine the size / placement of the buttons
        ///    // MyScrollbarleftbutton    - Name of style used for the left button.
        ///    // MyScrollbarrightbutton - Name of style used for the right button.
        ///    // MyScrollbarthumb         - Name of style used for the draggable thumb.
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUILayout.HorizontalScrollbar(scrollPos, 1, 0, 100, "MyScrollbar");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BeginScrollView" />
        ///<seealso cref="VerticalScrollbar" />
        public static float HorizontalScrollbar(float value, float size, float leftValue, float rightValue, params GUILayoutOption[] options)
        { return HorizontalScrollbar(value, size, leftValue, rightValue, GUI.skin.horizontalScrollbar, options); }
        ///<summary>Make a horizontal scrollbar.</summary>
        ///<remarks>
        ///  <para>A scrollbar control returns a float value that represents the position of the draggable "thumb" withtin the bar. You can use the value to adjust another GUI element to reflect the scroll position. However, most scrollable views can be handled more easily using a scroll view control.
        ///
        ///<img src="GUILayoutHorizontalScrollBar.png" />
        ///
        ///Horizontal Scrollbar in the Game View.</para>
        ///  <para>The styles of the scroll buttons at the end of the bar can be located in the current skin by adding "leftbutton" and "rightbutton" to the style name.
        ///The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</para>
        ///  <para />
        ///</remarks>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="leftValue">The value at the left end of the scrollbar.</param>
        ///<param name="rightValue">The value at the right end of the scrollbar.</param>
        ///<param name="style">The style to use for the scrollbar background. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float hSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        hSbarValue = GUILayout.HorizontalScrollbar(hSbarValue, 1.0f, 0.0f, 10.0f);
        ///        GUILayout.Label("This is a text that makes space");
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
        ///    public float scrollPos = 0.5F;
        ///    // This will use the following style names to determine the size / placement of the buttons
        ///    // MyScrollbarleftbutton    - Name of style used for the left button.
        ///    // MyScrollbarrightbutton - Name of style used for the right button.
        ///    // MyScrollbarthumb         - Name of style used for the draggable thumb.
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUILayout.HorizontalScrollbar(scrollPos, 1, 0, 100, "MyScrollbar");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BeginScrollView" />
        ///<seealso cref="VerticalScrollbar" />
        public static float HorizontalScrollbar(float value, float size, float leftValue, float rightValue, GUIStyle style, params GUILayoutOption[] options)
        { return GUI.HorizontalScrollbar(GUILayoutUtility.GetRect(GUIContent.Temp("mmmm"), style, options), value, size, leftValue, rightValue, style); }

        ///<summary>Make a vertical scrollbar.</summary>
        ///<remarks>
        ///  <para>A scrollbar control returns a float value that represents the position of the draggable "thumb" withtin the bar. You can use the value to adjust another GUI element to reflect the scroll position. However, most scrollable views can be handled more easily using a scroll view control.
        ///
        ///<img src="GUILayoutVerticalScrollBar.png" />
        ///
        ///Vertical Scrollbar in the Game View.</para>
        ///  <para>The styles of the scroll buttons at the end of the bar can be located in the current skin by adding "upbutton" and "downbutton" to the style name.  The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</para>
        ///  <para />
        ///</remarks>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="topValue">The value at the top end of the scrollbar.</param>
        ///<param name="bottomValue">The value at the bottom end of the scrollbar.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float vSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSbarValue = GUILayout.VerticalScrollbar(vSbarValue, 1.0f, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float scrollPos = 0.5f;
        ///
        ///    // This will use the following style names to determine the size / placement of the buttons
        ///    // MyVerticalScrollbarupbutton    - Name of style used for the up button.
        ///    // MyVerticalScrollbardownbutton - Name of style used for the down button.
        ///    // MyVerticalScrollbarthumb         - Name of style used for the draggable thumb.
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUILayout.HorizontalScrollbar(scrollPos, 1, 0, 100, "MyVerticalScrollbar");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BeginScrollView" />
        ///<seealso cref="HorizontalScrollbar" />
        public static float VerticalScrollbar(float value, float size, float topValue, float bottomValue, params GUILayoutOption[] options)
        { return VerticalScrollbar(value, size, topValue, bottomValue, GUI.skin.verticalScrollbar, options); }
        ///<summary>Make a vertical scrollbar.</summary>
        ///<remarks>
        ///  <para>A scrollbar control returns a float value that represents the position of the draggable "thumb" withtin the bar. You can use the value to adjust another GUI element to reflect the scroll position. However, most scrollable views can be handled more easily using a scroll view control.
        ///
        ///<img src="GUILayoutVerticalScrollBar.png" />
        ///
        ///Vertical Scrollbar in the Game View.</para>
        ///  <para>The styles of the scroll buttons at the end of the bar can be located in the current skin by adding "upbutton" and "downbutton" to the style name.  The name of the scrollbar thumb (the thing you drag) is found by appending "thumb" to the style name.</para>
        ///  <para />
        ///</remarks>
        ///<param name="value">The position between min and max.</param>
        ///<param name="size">How much can we see?</param>
        ///<param name="topValue">The value at the top end of the scrollbar.</param>
        ///<param name="bottomValue">The value at the bottom end of the scrollbar.</param>
        ///<param name="style">The style to use for the scrollbar background. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.</param>
        ///<returns>The modified value. This can be changed by the user by dragging the scrollbar, or clicking the arrows at the end.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float vSbarValue;
        ///
        ///    void OnGUI()
        ///    {
        ///        vSbarValue = GUILayout.VerticalScrollbar(vSbarValue, 1.0f, 10.0f, 0.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float scrollPos = 0.5f;
        ///
        ///    // This will use the following style names to determine the size / placement of the buttons
        ///    // MyVerticalScrollbarupbutton    - Name of style used for the up button.
        ///    // MyVerticalScrollbardownbutton - Name of style used for the down button.
        ///    // MyVerticalScrollbarthumb         - Name of style used for the draggable thumb.
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUILayout.HorizontalScrollbar(scrollPos, 1, 0, 100, "MyVerticalScrollbar");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BeginScrollView" />
        ///<seealso cref="HorizontalScrollbar" />
        public static float VerticalScrollbar(float value, float size, float topValue, float bottomValue, GUIStyle style, params GUILayoutOption[] options)
        { return GUI.VerticalScrollbar(GUILayoutUtility.GetRect(GUIContent.Temp("\n\n\n\n"), style, options), value, size, topValue, bottomValue, style); }

        ///<summary>Insert a space in the current layout group.</summary>
        ///<remarks>
        ///  <para>The direction of the space is dependent on the layout group you're currently in when issuing the command. If in a vertical group, the space will be vertical.
        ///**Note:** This will override the <see cref="GUILayout.ExpandWidth" /> and <see cref="GUILayout.ExpandHeight" /><img src="GUILayoutSpace.png" />
        ///
        ///Space of 20px between two buttons.</para>
        ///  <para>In horizontal groups, the <c>pixels</c> are measured horizontally:</para>
        ///  <para>An example that is based on <see cref="T:UnityEditor.EditorWindow" />:</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUILayout.Button("I'm the first button");
        ///
        ///        // Insert 20 pixels of space between the 2 buttons.
        ///        GUILayout.Space(20);
        ///
        ///        GUILayout.Button("I'm a bit further down");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginHorizontal();
        ///        GUILayout.Button("I'm the first button");
        ///
        ///        // Insert 20 pixels of space between the 2 buttons.
        ///        GUILayout.Space(20);
        ///
        ///        GUILayout.Button("I'm the second button");
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEditor;
        ///
        /// // Example of using GUILayout.Space inside an EditorWindow.
        /// // Clicking on the buttons changes the size of the Space.
        ///
        ///public class ExampleClass : EditorWindow
        ///{
        ///    [MenuItem("Examples/GUILayout.Space")]
        ///    static void CreateWindow()
        ///    {
        ///        EditorWindow window = GetWindow<ExampleClass>();
        ///        window.Show();
        ///    }
        ///
        ///    private float spaceSize = 20.0f;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (GUILayout.Button("Button1: Move Button2 down by 2 pixels"))
        ///        {
        ///            spaceSize = spaceSize + 2.0f;
        ///        }
        ///
        ///        GUILayout.Space(spaceSize);
        ///
        ///        if (GUILayout.Button("Button2: Move up by 1 pixel"))
        ///        {
        ///            spaceSize = spaceSize - 1.0f;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public void Space(float pixels)
        {
            GUIUtility.CheckOnGUI();
            if (GUILayoutUtility.current.topLevel.isVertical)
                GUILayoutUtility.GetRect(0, pixels, GUILayoutUtility.spaceStyle, GUILayout.Height(pixels));
            else
                GUILayoutUtility.GetRect(pixels, 0, GUILayoutUtility.spaceStyle, GUILayout.Width(pixels));
            // Instead of handling margins normally, we just want to insert the size.
            // This ensures that Space(1) adds ONE space, and doesn't prevent margin collapse.

            if (Event.current.type == EventType.Layout)
            {
                GUILayoutUtility.current.topLevel.entries[GUILayoutUtility.current.topLevel.entries.Count - 1].consideredForMargin = false;
            }
        }

        ///<summary>Insert a flexible space element.</summary>
        ///<remarks>Flexible spaces use up any leftover space in a layout.
        ///
        ///**Note:** This will override the <see cref="GUILayout.ExpandWidth" /> and <see cref="GUILayout.ExpandHeight" /><img src="GUILayoutFlexibleSpace.png" />
        ///
        ///Flexible Space in a GUILayout Area.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    float sliderValue = 1.0f;
        ///
        ///    void OnGUI()
        ///    {
        ///        // Wrap everything in the designated GUI Area
        ///        GUILayout.BeginArea(new Rect(0, 0, 200, 60));
        ///        // Begin the singular Horizontal Group
        ///        GUILayout.BeginHorizontal();
        ///        // Place a Button normally
        ///        GUILayout.RepeatButton("A button with\ntwo lines");
        ///        // Place a space between the button and the vertical area
        ///        // so it fits the whole area
        ///        GUILayout.FlexibleSpace();
        ///        // Arrange two more Controls vertically beside the Button
        ///        GUILayout.BeginVertical();
        ///        GUILayout.Box("Value:" + Mathf.Round(sliderValue));
        ///        sliderValue = GUILayout.HorizontalSlider(sliderValue, 0.0f, 10f);
        ///
        ///        // End the Groups and Area
        ///        GUILayout.EndVertical();
        ///        GUILayout.EndHorizontal();
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public void FlexibleSpace()
        {
            GUIUtility.CheckOnGUI();
            GUILayoutOption op;
            if (GUILayoutUtility.current.topLevel.isVertical)
                op = ExpandHeight(true);
            else
                op = ExpandWidth(true);

            op = new GUILayoutOption(op.type, 10000);
            GUILayoutUtility.GetRect(0, 0, GUILayoutUtility.spaceStyle, op);

            if (Event.current.type == EventType.Layout)
            {
                GUILayoutUtility.current.topLevel.entries[GUILayoutUtility.current.topLevel.entries.Count - 1].consideredForMargin = false;
            }
        }

        ///<summary>Begin a Horizontal control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed horizontally next to each other. The group must be closed with a call to EndHorizontal.
        ///
        ///<img src="GUILayoutHorizontal.png" />
        ///
        ///Horizontal Layout.</remarks>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a horizontal group
        ///        GUILayout.BeginHorizontal("box");
        ///
        ///        GUILayout.Button("I'm the first button");
        ///        GUILayout.Button("I'm to the right");
        ///
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginHorizontal(params GUILayoutOption[] options) { BeginHorizontal(GUIContent.none, GUIStyle.none, options); }
        ///<summary>Begin a Horizontal control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed horizontally next to each other. The group must be closed with a call to EndHorizontal.
        ///
        ///<img src="GUILayoutHorizontal.png" />
        ///
        ///Horizontal Layout.</remarks>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a horizontal group
        ///        GUILayout.BeginHorizontal("box");
        ///
        ///        GUILayout.Button("I'm the first button");
        ///        GUILayout.Button("I'm to the right");
        ///
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) { BeginHorizontal(GUIContent.none, style, options); }
        ///<summary>Begin a Horizontal control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed horizontally next to each other. The group must be closed with a call to EndHorizontal.
        ///
        ///<img src="GUILayoutHorizontal.png" />
        ///
        ///Horizontal Layout.</remarks>
        ///<param name="text">Text to display on group.</param>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a horizontal group
        ///        GUILayout.BeginHorizontal("box");
        ///
        ///        GUILayout.Button("I'm the first button");
        ///        GUILayout.Button("I'm to the right");
        ///
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginHorizontal(string text, GUIStyle style, params GUILayoutOption[] options) { BeginHorizontal(GUIContent.Temp(text), style, options); }

        ///<summary>Begin a Horizontal control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed horizontally next to each other. The group must be closed with a call to EndHorizontal.
        ///
        ///<img src="GUILayoutHorizontal.png" />
        ///
        ///Horizontal Layout.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on group.</param>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a horizontal group
        ///        GUILayout.BeginHorizontal("box");
        ///
        ///        GUILayout.Button("I'm the first button");
        ///        GUILayout.Button("I'm to the right");
        ///
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginHorizontal(Texture image, GUIStyle style, params GUILayoutOption[] options)
        { BeginHorizontal(GUIContent.Temp(image), style, options); }
        ///<summary>Begin a Horizontal control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed horizontally next to each other. The group must be closed with a call to EndHorizontal.
        ///
        ///<img src="GUILayoutHorizontal.png" />
        ///
        ///Horizontal Layout.</remarks>
        ///<param name="content">Text, image, and tooltip for this group.</param>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a horizontal group
        ///        GUILayout.BeginHorizontal("box");
        ///
        ///        GUILayout.Button("I'm the first button");
        ///        GUILayout.Button("I'm to the right");
        ///
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayoutGroup g = GUILayoutUtility.BeginLayoutGroup(style, options, typeof(GUILayoutGroup));
            g.isVertical = false;
            if (style != GUIStyle.none || content != GUIContent.none)
                GUI.Box(g.rect, content, style);
        }

        ///<summary>Close a group started with BeginHorizontal.</summary>
        ///<remarks>
        ///  <img src="GUILayoutHorizontal.png" />
        ///
        ///Horizontal Layout.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginHorizontal("box");
        ///
        ///        GUILayout.Button("I'm the first button");
        ///        GUILayout.Button("I'm to the right");
        ///
        ///        // End the horizontal group we began above
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void EndHorizontal()
        {
            GUILayoutUtility.EndLayoutGroup();
        }

        ///<summary>Begin a vertical control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed vertically below each other. The group must be closed with a call to EndVertical.
        ///
        ///<img src="GUILayoutVertical.png" />
        ///
        ///Vertical Layout.</remarks>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a vertical group
        ///        GUILayout.BeginVertical("box");
        ///
        ///        GUILayout.Button("I'm the top button");
        ///        GUILayout.Button("I'm the bottom button");
        ///
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginVertical(params GUILayoutOption[] options) { BeginVertical(GUIContent.none, GUIStyle.none, options); }
        ///<summary>Begin a vertical control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed vertically below each other. The group must be closed with a call to EndVertical.
        ///
        ///<img src="GUILayoutVertical.png" />
        ///
        ///Vertical Layout.</remarks>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a vertical group
        ///        GUILayout.BeginVertical("box");
        ///
        ///        GUILayout.Button("I'm the top button");
        ///        GUILayout.Button("I'm the bottom button");
        ///
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) { BeginVertical(GUIContent.none, style, options); }
        ///<summary>Begin a vertical control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed vertically below each other. The group must be closed with a call to EndVertical.
        ///
        ///<img src="GUILayoutVertical.png" />
        ///
        ///Vertical Layout.</remarks>
        ///<param name="text">Text to display on group.</param>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a vertical group
        ///        GUILayout.BeginVertical("box");
        ///
        ///        GUILayout.Button("I'm the top button");
        ///        GUILayout.Button("I'm the bottom button");
        ///
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginVertical(string text, GUIStyle style, params GUILayoutOption[] options) { BeginVertical(GUIContent.Temp(text), style, options); }
        ///<summary>Begin a vertical control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed vertically below each other. The group must be closed with a call to EndVertical.
        ///
        ///<img src="GUILayoutVertical.png" />
        ///
        ///Vertical Layout.</remarks>
        ///<param name="image">
        ///  <see cref="Texture" /> to display on group.</param>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a vertical group
        ///        GUILayout.BeginVertical("box");
        ///
        ///        GUILayout.Button("I'm the top button");
        ///        GUILayout.Button("I'm the bottom button");
        ///
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginVertical(Texture image, GUIStyle style, params GUILayoutOption[] options) { BeginVertical(GUIContent.Temp(image), style, options); }

        ///<summary>Begin a vertical control group.</summary>
        ///<remarks>All controls rendered inside this element will be placed vertically below each other. The group must be closed with a call to EndVertical.
        ///
        ///<img src="GUILayoutVertical.png" />
        ///
        ///Vertical Layout.</remarks>
        ///<param name="content">Text, image, and tooltip for this group.</param>
        ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a vertical group
        ///        GUILayout.BeginVertical("box");
        ///
        ///        GUILayout.Button("I'm the top button");
        ///        GUILayout.Button("I'm the bottom button");
        ///
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static void BeginVertical(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayoutGroup g = GUILayoutUtility.BeginLayoutGroup(style, options, typeof(GUILayoutGroup));
            g.isVertical = true;
            if (style != GUIStyle.none || content != GUIContent.none)
                GUI.Box(g.rect, content, style);
        }

        ///<summary>Close a group started with BeginVertical.</summary>
        ///<remarks>
        ///  <img src="GUILayoutVertical.png" />
        ///
        ///Vertical Layout.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical("box");
        ///
        ///        GUILayout.Button("I'm the top button");
        ///        GUILayout.Button("I'm the bottom button");
        ///
        ///        // End the vertical group we started above
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static void EndVertical()
        {
            GUILayoutUtility.EndLayoutGroup();
        }

        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect)                                  { BeginArea(screenRect, GUIContent.none, GUIStyle.none); }
        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<param name="text">Optional text to display in the area.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect, string text)                     { BeginArea(screenRect, GUIContent.Temp(text), GUIStyle.none); }
        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<param name="image">Optional texture to display in the area.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect, Texture image)                   { BeginArea(screenRect, GUIContent.Temp(image), GUIStyle.none); }
        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<param name="content">Optional text, image and tooltip top display for this area.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect, GUIContent content)              { BeginArea(screenRect, content, GUIStyle.none); }
        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<param name="style">The style to use. If left out, the empty <see cref="GUIStyle" /> (<see cref="GUIStyle.none" />) is used, giving a transparent background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect, GUIStyle style)                  { BeginArea(screenRect, GUIContent.none, style); }
        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<param name="text">Optional text to display in the area.</param>
        ///<param name="style">The style to use. If left out, the empty <see cref="GUIStyle" /> (<see cref="GUIStyle.none" />) is used, giving a transparent background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect, string text, GUIStyle style)     { BeginArea(screenRect, GUIContent.Temp(text), style); }
        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<param name="image">Optional texture to display in the area.</param>
        ///<param name="style">The style to use. If left out, the empty <see cref="GUIStyle" /> (<see cref="GUIStyle.none" />) is used, giving a transparent background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect, Texture image, GUIStyle style)   { BeginArea(screenRect, GUIContent.Temp(image), style); }

        ///<summary>Begin a GUILayout block of GUI controls in a fixed screen area.</summary>
        ///<remarks>
        ///  <para>By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use GUILayout.BeginArea to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</para>
        ///  <para>This function is very useful when mixing GUILayout code. It must be matched with a call to EndArea. BeginArea / EndArea cannot be nested.</para>
        ///</remarks>
        ///<param name="content">Optional text, image and tooltip top display for this area.</param>
        ///<param name="style">The style to use. If left out, the empty <see cref="GUIStyle" /> (<see cref="GUIStyle.none" />) is used, giving a transparent background.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts an area to draw elements
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="EndArea" />
        static public void BeginArea(Rect screenRect, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            GUILayoutGroup g = GUILayoutUtility.BeginLayoutArea(style, typeof(GUILayoutGroup));
            if (Event.current.type == EventType.Layout)
            {
                g.resetCoords = true;
                g.minWidth = g.maxWidth = screenRect.width;
                g.minHeight = g.maxHeight = screenRect.height;
                g.rect = Rect.MinMaxRect(screenRect.xMin, screenRect.yMin, g.rect.xMax, g.rect.yMax);
            }

            GUI.BeginGroup(g.rect, content, style);
        }

        ///<summary>Close a GUILayout block started with BeginArea.</summary>
        ///<remarks>
        ///  <img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginArea(new Rect(10, 10, 100, 100));
        ///        GUILayout.Button("Click me");
        ///        GUILayout.Button("Or me");
        ///        // Ends the area started above
        ///        GUILayout.EndArea();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public void EndArea()
        {
            GUIUtility.CheckOnGUI();
            GUILayoutUtility.EndLayoutArea();
            if (Event.current.type == EventType.Used)
                return;
            GUI.EndGroup();
        }

        ///<summary>Begin an automatically laid out scrollview.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<param name="scrollPosition">The position to use display.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)        { return BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options); }
        ///<summary>Begin an automatically laid out scrollview.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<param name="scrollPosition">The position to use display.</param>
        ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when the content inside the ScrollView is wider than the scrollview itself.</param>
        ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when content inside the ScrollView is taller than the scrollview itself.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)        { return BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options); }
        ///<summary>Begin an automatically laid out scrollview.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<param name="scrollPosition">The position to use display.</param>
        ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)      { return BeginScrollView(scrollPosition, false, false, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView, options); }

        ///<summary>Begin an automatically laid out scrollview.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<param name="scrollPosition">The position to use display.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle style)
        {
            GUILayoutOption[] option = null;
            return BeginScrollView(scrollPosition, style, option);
        }

        ///<summary>Begin an automatically laid out scrollview.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<param name="scrollPosition">The position to use display.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
        {
            string name = style.name;

            GUIStyle vertical = GUI.skin.FindStyle(name + "VerticalScrollbar");
            if (vertical == null)
                vertical = GUI.skin.verticalScrollbar;
            GUIStyle horizontal = GUI.skin.FindStyle(name + "HorizontalScrollbar");
            if (horizontal == null)
                horizontal = GUI.skin.horizontalScrollbar;
            return BeginScrollView(scrollPosition, false, false, horizontal, vertical, style, options);
        }

        ///<summary>Begin an automatically laid out scrollview.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<param name="scrollPosition">The position to use display.</param>
        ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when the content inside the ScrollView is wider than the scrollview itself.</param>
        ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when content inside the ScrollView is taller than the scrollview itself.</param>
        ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
        { return BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView, options); }

        ///<summary>Begin an automatically laid out scrollview.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<param name="scrollPosition">The position to use display.</param>
        ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when the content inside the ScrollView is wider than the scrollview itself.</param>
        ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when content inside the ScrollView is taller than the scrollview itself.</param>
        ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<returns>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            GUIUtility.CheckOnGUI();

            GUIScrollGroup g = (GUIScrollGroup)GUILayoutUtility.BeginLayoutGroup(background, null, typeof(GUIScrollGroup));
            switch (Event.current.type)
            {
                case EventType.Layout:
                    g.resetCoords = true;
                    g.isVertical = true;
                    g.stretchWidth = 1;
                    g.stretchHeight = 1;
                    g.verticalScrollbar = verticalScrollbar;
                    g.horizontalScrollbar = horizontalScrollbar;
                    g.needsVerticalScrollbar = alwaysShowVertical;
                    g.needsHorizontalScrollbar = alwaysShowHorizontal;
                    g.ApplyOptions(options);
                    break;
                default:
                    break;
            }
            return GUI.BeginScrollView(g.rect, scrollPosition, new Rect(0, 0, g.clientWidth, g.clientHeight), alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
        }

        ///<summary>End a scroll view begun with a call to BeginScrollView.</summary>
        ///<remarks>
        ///  <img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        scrollPosition = GUILayout.BeginScrollView(
        ///            scrollPosition, GUILayout.Width(100), GUILayout.Height(100));
        ///
        ///        // We just add a single label to go inside the scroll view. Note how the
        ///        // scrollbars will work correctly with wordwrap.
        ///        GUILayout.Label(longString);
        ///
        ///        // Add a button to clear the string. This is inside the scroll area, so it
        ///        // will be scrolled as well. Note how the button becomes narrower to make room
        ///        // for the vertical scrollbar
        ///        if (GUILayout.Button("Clear"))
        ///            longString = "";
        ///
        ///        // End the scrollview we began above.
        ///        GUILayout.EndScrollView();
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.BeginScrollView" />
        public static void EndScrollView()
        {
            EndScrollView(true);
        }

        internal static void EndScrollView(bool handleScrollWheel)
        {
            GUILayoutUtility.EndLayoutGroup();
            GUI.EndScrollView(handleScrollWheel);
        }

        ///<summary>Make a popup window that layouts its contents automatically.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user.
        ///Unlike other controls, you need to pass them a separate function for the GUI controls to put inside the window. Here is a small example to get you started:
        ///
        ///<img src="GUILayoutWindow.png" />
        ///
        ///Window in the Game View.</para>
        ///  <para>The screen rectangle you pass in to the function only acts as a guide. To Apply extra limits to the window, pass in some extra layout options. The ones applied here will override the size calculated. Here is a small example:</para>
        ///</remarks>
        ///<param name="id">A unique ID to use for each window. This is the ID you'll use to interface to it.</param>
        ///<param name="screenRect">Rectangle on the screen to use for the window. The layouting system will attempt to fit the window inside it - if that cannot be done, it will adjust the rectangle to fit.</param>
        ///<param name="func">The function that creates the GUI <c>inside</c> the window. This function must take one parameter - the <c>id</c> of the window it's currently making GUI for.</param>
        ///<param name="text">Text to display as a title for the window.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c> or the <c>screenRect</c> you pass in.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The rectangle the window is at. This can be in a different position and have a different size than the one you passed in.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button will size to fit the window
        ///        if (GUILayout.Button("Hello World"))
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
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Here we instruct the layout system to
        ///        // make the window 100 pixels wide no matter what.
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window", GUILayout.Width(100));
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button is too large to fit the window
        ///        // Normally, the window would have been expanded to fit the button, but due to
        ///        // the GUILayout.Width call above the window will only ever be 100 pixels wide
        ///        if (GUILayout.Button("Please click me a lot"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string text, params GUILayoutOption[] options)                        { return DoWindow(id, screenRect, func, GUIContent.Temp(text), GUI.skin.window, options); }
        ///<summary>Make a popup window that layouts its contents automatically.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user.
        ///Unlike other controls, you need to pass them a separate function for the GUI controls to put inside the window. Here is a small example to get you started:
        ///
        ///<img src="GUILayoutWindow.png" />
        ///
        ///Window in the Game View.</para>
        ///  <para>The screen rectangle you pass in to the function only acts as a guide. To Apply extra limits to the window, pass in some extra layout options. The ones applied here will override the size calculated. Here is a small example:</para>
        ///</remarks>
        ///<param name="id">A unique ID to use for each window. This is the ID you'll use to interface to it.</param>
        ///<param name="screenRect">Rectangle on the screen to use for the window. The layouting system will attempt to fit the window inside it - if that cannot be done, it will adjust the rectangle to fit.</param>
        ///<param name="func">The function that creates the GUI <c>inside</c> the window. This function must take one parameter - the <c>id</c> of the window it's currently making GUI for.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display an image in the titlebar.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c> or the <c>screenRect</c> you pass in.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The rectangle the window is at. This can be in a different position and have a different size than the one you passed in.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button will size to fit the window
        ///        if (GUILayout.Button("Hello World"))
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
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Here we instruct the layout system to
        ///        // make the window 100 pixels wide no matter what.
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window", GUILayout.Width(100));
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button is too large to fit the window
        ///        // Normally, the window would have been expanded to fit the button, but due to
        ///        // the GUILayout.Width call above the window will only ever be 100 pixels wide
        ///        if (GUILayout.Button("Please click me a lot"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, Texture image, params GUILayoutOption[] options)              { return DoWindow(id, screenRect, func, GUIContent.Temp(image), GUI.skin.window, options); }
        ///<summary>Make a popup window that layouts its contents automatically.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user.
        ///Unlike other controls, you need to pass them a separate function for the GUI controls to put inside the window. Here is a small example to get you started:
        ///
        ///<img src="GUILayoutWindow.png" />
        ///
        ///Window in the Game View.</para>
        ///  <para>The screen rectangle you pass in to the function only acts as a guide. To Apply extra limits to the window, pass in some extra layout options. The ones applied here will override the size calculated. Here is a small example:</para>
        ///</remarks>
        ///<param name="id">A unique ID to use for each window. This is the ID you'll use to interface to it.</param>
        ///<param name="screenRect">Rectangle on the screen to use for the window. The layouting system will attempt to fit the window inside it - if that cannot be done, it will adjust the rectangle to fit.</param>
        ///<param name="func">The function that creates the GUI <c>inside</c> the window. This function must take one parameter - the <c>id</c> of the window it's currently making GUI for.</param>
        ///<param name="content">Text, image and tooltip for this window.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c> or the <c>screenRect</c> you pass in.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The rectangle the window is at. This can be in a different position and have a different size than the one you passed in.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button will size to fit the window
        ///        if (GUILayout.Button("Hello World"))
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
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Here we instruct the layout system to
        ///        // make the window 100 pixels wide no matter what.
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window", GUILayout.Width(100));
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button is too large to fit the window
        ///        // Normally, the window would have been expanded to fit the button, but due to
        ///        // the GUILayout.Width call above the window will only ever be 100 pixels wide
        ///        if (GUILayout.Button("Please click me a lot"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, params GUILayoutOption[] options)             { return DoWindow(id, screenRect, func, content, GUI.skin.window, options); }
        ///<summary>Make a popup window that layouts its contents automatically.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user.
        ///Unlike other controls, you need to pass them a separate function for the GUI controls to put inside the window. Here is a small example to get you started:
        ///
        ///<img src="GUILayoutWindow.png" />
        ///
        ///Window in the Game View.</para>
        ///  <para>The screen rectangle you pass in to the function only acts as a guide. To Apply extra limits to the window, pass in some extra layout options. The ones applied here will override the size calculated. Here is a small example:</para>
        ///</remarks>
        ///<param name="id">A unique ID to use for each window. This is the ID you'll use to interface to it.</param>
        ///<param name="screenRect">Rectangle on the screen to use for the window. The layouting system will attempt to fit the window inside it - if that cannot be done, it will adjust the rectangle to fit.</param>
        ///<param name="func">The function that creates the GUI <c>inside</c> the window. This function must take one parameter - the <c>id</c> of the window it's currently making GUI for.</param>
        ///<param name="text">Text to display as a title for the window.</param>
        ///<param name="style">An optional style to use for the window. If left out, the <c>window</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c> or the <c>screenRect</c> you pass in.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The rectangle the window is at. This can be in a different position and have a different size than the one you passed in.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button will size to fit the window
        ///        if (GUILayout.Button("Hello World"))
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
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Here we instruct the layout system to
        ///        // make the window 100 pixels wide no matter what.
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window", GUILayout.Width(100));
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button is too large to fit the window
        ///        // Normally, the window would have been expanded to fit the button, but due to
        ///        // the GUILayout.Width call above the window will only ever be 100 pixels wide
        ///        if (GUILayout.Button("Please click me a lot"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string text, GUIStyle style, params GUILayoutOption[] options)            { return DoWindow(id, screenRect, func, GUIContent.Temp(text), style, options); }
        ///<summary>Make a popup window that layouts its contents automatically.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user.
        ///Unlike other controls, you need to pass them a separate function for the GUI controls to put inside the window. Here is a small example to get you started:
        ///
        ///<img src="GUILayoutWindow.png" />
        ///
        ///Window in the Game View.</para>
        ///  <para>The screen rectangle you pass in to the function only acts as a guide. To Apply extra limits to the window, pass in some extra layout options. The ones applied here will override the size calculated. Here is a small example:</para>
        ///</remarks>
        ///<param name="id">A unique ID to use for each window. This is the ID you'll use to interface to it.</param>
        ///<param name="screenRect">Rectangle on the screen to use for the window. The layouting system will attempt to fit the window inside it - if that cannot be done, it will adjust the rectangle to fit.</param>
        ///<param name="func">The function that creates the GUI <c>inside</c> the window. This function must take one parameter - the <c>id</c> of the window it's currently making GUI for.</param>
        ///<param name="image">
        ///  <see cref="Texture" /> to display an image in the titlebar.</param>
        ///<param name="style">An optional style to use for the window. If left out, the <c>window</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c> or the <c>screenRect</c> you pass in.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The rectangle the window is at. This can be in a different position and have a different size than the one you passed in.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button will size to fit the window
        ///        if (GUILayout.Button("Hello World"))
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
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Here we instruct the layout system to
        ///        // make the window 100 pixels wide no matter what.
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window", GUILayout.Width(100));
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button is too large to fit the window
        ///        // Normally, the window would have been expanded to fit the button, but due to
        ///        // the GUILayout.Width call above the window will only ever be 100 pixels wide
        ///        if (GUILayout.Button("Please click me a lot"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, Texture image, GUIStyle style, params GUILayoutOption[] options)  { return DoWindow(id, screenRect, func, GUIContent.Temp(image), style, options); }

        ///<summary>Make a popup window that layouts its contents automatically.</summary>
        ///<remarks>
        ///  <para>Windows float above normal GUI controls, feature click-to-focus and can optionally be dragged around by the end user.
        ///Unlike other controls, you need to pass them a separate function for the GUI controls to put inside the window. Here is a small example to get you started:
        ///
        ///<img src="GUILayoutWindow.png" />
        ///
        ///Window in the Game View.</para>
        ///  <para>The screen rectangle you pass in to the function only acts as a guide. To Apply extra limits to the window, pass in some extra layout options. The ones applied here will override the size calculated. Here is a small example:</para>
        ///</remarks>
        ///<param name="id">A unique ID to use for each window. This is the ID you'll use to interface to it.</param>
        ///<param name="screenRect">Rectangle on the screen to use for the window. The layouting system will attempt to fit the window inside it - if that cannot be done, it will adjust the rectangle to fit.</param>
        ///<param name="func">The function that creates the GUI <c>inside</c> the window. This function must take one parameter - the <c>id</c> of the window it's currently making GUI for.</param>
        ///<param name="content">Text, image and tooltip for this window.</param>
        ///<param name="style">An optional style to use for the window. If left out, the <c>window</c> style from the current <see cref="GUISkin" /> is used.</param>
        ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c> or the <c>screenRect</c> you pass in.&lt;br&gt;
        ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
        ///<returns>The rectangle the window is at. This can be in a different position and have a different size than the one you passed in.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Notice the 3rd parameter
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window");
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button will size to fit the window
        ///        if (GUILayout.Button("Hello World"))
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
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    Rect windowRect = new Rect(20, 20, 120, 50);
        ///
        ///    void OnGUI()
        ///    {
        ///        // Register the window. Here we instruct the layout system to
        ///        // make the window 100 pixels wide no matter what.
        ///        windowRect = GUILayout.Window(0, windowRect, DoMyWindow, "My Window", GUILayout.Width(100));
        ///    }
        ///
        ///    // Make the contents of the window
        ///    void DoMyWindow(int windowID)
        ///    {
        ///        // This button is too large to fit the window
        ///        // Normally, the window would have been expanded to fit the button, but due to
        ///        // the GUILayout.Width call above the window will only ever be 100 pixels wide
        ///        if (GUILayout.Button("Please click me a lot"))
        ///        {
        ///            print("Got a click");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout.Width" />
        ///<seealso cref="GUILayout.Height" />
        ///<seealso cref="GUILayout.MinWidth" />
        ///<seealso cref="GUILayout.MaxWidth" />
        ///<seealso cref="GUILayout.MinHeight" />
        public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, GUIStyle style, params GUILayoutOption[] options) { return DoWindow(id, screenRect, func, content, style, options); }
        // Make an auto-sized draggable window...
        static Rect DoWindow(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            GUIUtility.CheckOnGUI();
            LayoutedWindow lw = new LayoutedWindow(func, screenRect, content, options, style);
            return GUI.Window(id, screenRect, lw.DoWindow, content, style);
        }

        private sealed class LayoutedWindow
        {
            readonly GUI.WindowFunction m_Func;
            readonly Rect m_ScreenRect;
            readonly GUILayoutOption[] m_Options;
            readonly GUIStyle m_Style;

            internal LayoutedWindow(GUI.WindowFunction f, Rect screenRect, GUIContent content, GUILayoutOption[] options, GUIStyle style)
            {
                m_Func = f;
                m_ScreenRect = screenRect;
                m_Options = options;
                m_Style = style;
            }

            public void DoWindow(int windowID)
            {
                GUILayoutGroup g = GUILayoutUtility.current.topLevel;

                switch (Event.current.type)
                {
                    case EventType.Layout:
                        // TODO: Add layoutoptions
                        // TODO: Take titlebar size into consideration
                        g.resetCoords = true;
                        g.rect = m_ScreenRect;
                        if (m_Options != null)
                            g.ApplyOptions(m_Options);
                        g.isWindow = true;
                        g.windowID = windowID;
                        g.style = m_Style;
                        break;
                    default:
                        g.ResetCursor();
                        break;
                }
                m_Func(windowID);
            }
        }

        ///<summary>Option passed to a control to give it an absolute width.</summary>
        ///<remarks>
        ///  <img src="GUILayoutWidth.png" />
        ///
        ///Fixed width for a GUI Control.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a  button with a  fixed width
        ///    void OnGUI()
        ///    {
        ///        GUILayout.Button("A Button with fixed width", GUILayout.Width(300));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public GUILayoutOption Width(float width)                   { return new GUILayoutOption(GUILayoutOption.Type.fixedWidth, width); }
        ///<summary>Option passed to a control to specify a minimum width.</summary>
        ///<remarks>**Note:** This option will override the Automatic width Layout parameter
        ///
        ///<img src="GUILayoutMinWidth.png" />
        ///
        ///Minimum allowed width for a Window.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a window you can resize between 80px and 200px height
        ///    // Just click the box inside the window and move your mouse
        ///    Rect windowRect = new Rect(10, 10, 100, 100);
        ///    bool scaling = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUILayout.Window(0, windowRect, ScalingWindow, "resizeable",
        ///            GUILayout.MinHeight(80), GUILayout.MaxHeight(200));
        ///    }
        ///
        ///    void ScalingWindow(int windowID)
        ///    {
        ///        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
        ///        if (Event.current.type == EventType.MouseUp)
        ///        {
        ///            scaling = false;
        ///        }
        ///        else if (Event.current.type == EventType.MouseDown &&
        ///                 GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        ///        {
        ///            scaling = true;
        ///        }
        ///
        ///        if (scaling)
        ///        {
        ///            windowRect = new Rect(windowRect.x, windowRect.y,
        ///                windowRect.width + Event.current.delta.x, windowRect.height + Event.current.delta.y);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public GUILayoutOption MinWidth(float minWidth)             { return new GUILayoutOption(GUILayoutOption.Type.minWidth, minWidth); }
        ///<summary>Option passed to a control to specify a maximum width.</summary>
        ///<remarks>
        ///  <img src="GUILayoutMaxWidth.png" />
        ///
        ///Maximum allowed width for a window.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a window you can resize between 80px and 200px height
        ///    // Just click the box inside the window and move your mouse
        ///    Rect windowRect = new Rect(10, 10, 100, 100);
        ///    bool scaling = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUILayout.Window(0, windowRect, ScalingWindow, "resizeable",
        ///            GUILayout.MinHeight(80), GUILayout.MaxHeight(200));
        ///    }
        ///
        ///    void ScalingWindow(int windowID)
        ///    {
        ///        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
        ///        if (Event.current.type == EventType.MouseUp)
        ///        {
        ///            scaling = false;
        ///        }
        ///        else if (Event.current.type == EventType.MouseDown &&
        ///                 GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        ///        {
        ///            scaling = true;
        ///        }
        ///
        ///        if (scaling)
        ///        {
        ///            windowRect = new Rect(windowRect.x, windowRect.y,
        ///                windowRect.width + Event.current.delta.x, windowRect.height + Event.current.delta.y);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public GUILayoutOption MaxWidth(float maxWidth)             { return new GUILayoutOption(GUILayoutOption.Type.maxWidth, maxWidth); }
        ///<summary>Option passed to a control to give it an absolute height.</summary>
        ///<remarks>**Note:** This option will override the Automatic height Layout parameter
        ///
        ///<img src="GUILayoutHeight.png" />
        ///
        ///Fixed Height for a GUI Control.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a  button with a  fixed height
        ///    void OnGUI()
        ///    {
        ///        GUILayout.Button("A Button with fixed height", GUILayout.Height(300));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public GUILayoutOption Height(float height)                 { return new GUILayoutOption(GUILayoutOption.Type.fixedHeight, height); }

        ///<summary>Option passed to a control to specify a minimum height.</summary>
        ///<remarks>
        ///  <img src="GUILayoutMinHeight.png" />
        ///
        ///Minimum height for a window.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a window you can resize between 80px and 200px height
        ///    // Just click the box inside the window and move your mouse
        ///    Rect windowRect = new Rect(10, 10, 100, 100);
        ///    bool scaling = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUILayout.Window(0, windowRect, ScalingWindow, "resizeable",
        ///            GUILayout.MinHeight(80), GUILayout.MaxHeight(200));
        ///    }
        ///
        ///    void ScalingWindow(int windowID)
        ///    {
        ///        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
        ///        if (Event.current.type == EventType.MouseUp)
        ///        {
        ///            scaling = false;
        ///        }
        ///        else if (Event.current.type == EventType.MouseDown &&
        ///                 GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        ///        {
        ///            scaling = true;
        ///        }
        ///
        ///        if (scaling)
        ///        {
        ///            windowRect = new Rect(windowRect.x, windowRect.y,
        ///                windowRect.width + Event.current.delta.x, windowRect.height + Event.current.delta.y);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public GUILayoutOption MinHeight(float minHeight)           { return new GUILayoutOption(GUILayoutOption.Type.minHeight, minHeight); }

        ///<summary>Option passed to a control to specify a maximum height.</summary>
        ///<remarks>
        ///  <img src="GUILayoutMaxHeight.png" />
        ///
        ///Maximum Height allowed for the window.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Draws a window you can resize between 80px and 200px height
        ///    // Just click the box inside the window and move your mouse
        ///    Rect windowRect = new Rect(10, 10, 100, 100);
        ///    bool scaling = false;
        ///
        ///    void OnGUI()
        ///    {
        ///        windowRect = GUILayout.Window(0, windowRect, ScalingWindow, "resizeable",
        ///            GUILayout.MinHeight(80), GUILayout.MaxHeight(200));
        ///    }
        ///
        ///    void ScalingWindow(int windowID)
        ///    {
        ///        GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
        ///        if (Event.current.type == EventType.MouseUp)
        ///        {
        ///            scaling = false;
        ///        }
        ///        else if (Event.current.type == EventType.MouseDown &&
        ///                 GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
        ///        {
        ///            scaling = true;
        ///        }
        ///
        ///        if (scaling)
        ///        {
        ///            windowRect = new Rect(windowRect.x, windowRect.y,
        ///                windowRect.width + Event.current.delta.x, windowRect.height + Event.current.delta.y);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public GUILayoutOption MaxHeight(float maxHeight)           { return new GUILayoutOption(GUILayoutOption.Type.maxHeight, maxHeight); }

        ///<summary>Option passed to a control to allow or disallow horizontal expansion.</summary>
        ///<remarks>If this is true, the enclosed UI elements can expand to fill the available horizontal width.
        ///
        ///<img src="ExpandWidth.png" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginVertical();
        ///        GUILayout.Button("Short Button", GUILayout.ExpandWidth(false));
        ///        GUILayout.Button("Very very long Button");
        ///        GUILayout.EndVertical();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public GUILayoutOption ExpandWidth(bool expand)             { return new GUILayoutOption(GUILayoutOption.Type.stretchWidth, expand ? 1 : 0); }
        ///<summary>Option passed to a control to allow or disallow vertical expansion.</summary>
        static public GUILayoutOption ExpandHeight(bool expand)            { return new GUILayoutOption(GUILayoutOption.Type.stretchHeight, expand ? 1 : 0); }

        ///<summary>Disposable helper class for managing <see cref="BeginHorizontal" /> / <see cref="EndHorizontal" />.</summary>
        ///<remarks>All controls rendered inside this element will be placed horizontally next to each other.  The <c>using</c> statement means <see cref="BeginHorizontal" /> and <see cref="EndHorizontal" /> are not needed.
        ///
        ///<img src="GUILayoutHorizontal.png" />
        ///
        ///Horizontal Layout.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a horizontal group
        ///        using (var horizontalScope = new GUILayout.HorizontalScope("box"))
        ///        {
        ///            GUILayout.Button("I'm the first button");
        ///            GUILayout.Button("I'm to the right");
        ///        }
        ///        // Now the group is ended.
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public class HorizontalScope : GUI.Scope
        {
            ///<summary>Create a new HorizontalScope and begin the corresponding horizontal group.</summary>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public HorizontalScope(params GUILayoutOption[] options)
            {
                BeginHorizontal(options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new HorizontalScope and begin the corresponding horizontal group.</summary>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public HorizontalScope(GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(style, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new HorizontalScope and begin the corresponding horizontal group.</summary>
            ///<param name="text">Text to display on group.</param>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public HorizontalScope(string text, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(text, style, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new HorizontalScope and begin the corresponding horizontal group.</summary>
            ///<param name="image">
            ///  <see cref="Texture" /> to display on group.</param>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public HorizontalScope(Texture image, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(image, style, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new HorizontalScope and begin the corresponding horizontal group.</summary>
            ///<param name="content">Text, image, and tooltip for this group.</param>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public HorizontalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginHorizontal(content, style, options);
            }
            #pragma warning restore UAL0015

            protected override void CloseScope()
            {
                EndHorizontal();
            }
        }

        ///<summary>Disposable helper class for managing <see cref="BeginVertical" /> / <see cref="EndVertical" />.</summary>
        ///<remarks>All controls rendered inside this element will be placed vertically below each other. The group is automatically closed when the scope ends.
        ///
        ///<img src="GUILayoutVertical.png" />
        ///
        ///Vertical Layout.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Starts a vertical group
        ///        using (var verticalScope = new VerticalScope("box"))
        ///        {
        ///            GUILayout.Button("I'm the top button");
        ///            GUILayout.Button("I'm the bottom button");
        ///        }
        ///        // The group is now ended
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public class VerticalScope : GUI.Scope
        {
            ///<summary>Create a new VerticalScope and begin the corresponding vertical group.</summary>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public VerticalScope(params GUILayoutOption[] options)
            {
                BeginVertical(options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new VerticalScope and begin the corresponding vertical group.</summary>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public VerticalScope(GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(style, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new VerticalScope and begin the corresponding vertical group.</summary>
            ///<param name="text">Text to display on group.</param>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public VerticalScope(string text, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(text, style, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new VerticalScope and begin the corresponding vertical group.</summary>
            ///<param name="image">
            ///  <see cref="Texture" /> to display on group.</param>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public VerticalScope(Texture image, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(image, style, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new VerticalScope and begin the corresponding vertical group.</summary>
            ///<param name="content">Text, image, and tooltip for this group.</param>
            ///<param name="style">The style to use for background image and padding values. If left out, the background is transparent.</param>
            ///<param name="options">An optional list of layout options that specify extra layouting properties. Any values passed in here will override settings defined by the <c>style</c>.&lt;br&gt;
            ///<see cref="GUILayout.MaxHeight" />, <see cref="GUILayout.ExpandWidth" />, <see cref="GUILayout.ExpandHeight" /></param>
            ///<seealso cref="GUILayout.Width" />
            ///<seealso cref="GUILayout.Height" />
            ///<seealso cref="GUILayout.MinWidth" />
            ///<seealso cref="GUILayout.MaxWidth" />
            ///<seealso cref="GUILayout.MinHeight" />
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public VerticalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
            {
                BeginVertical(content, style, options);
            }
            #pragma warning restore UAL0015

            protected override void CloseScope()
            {
                EndVertical();
            }
        }

        ///<summary>Disposable helper class for managing <see cref="BeginArea" /> / <see cref="EndArea" />.</summary>
        ///<remarks>::ref::BeginArea is called at construction, and <see cref="EndArea" /> is called when the instance is disposed.
        ///By default, any GUI controls made using GUILayout are placed in the top-left corner of the screen.
        ///If you want to place a series of automatically laid out controls in an arbitrary area, use <see cref="GUILayout.BeginArea" /> to define a new area for the automatic layouting system to use.
        ///
        ///<img src="GUILayoutArea.png" />
        ///
        ///Explained Area of the example.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        using (var areaScope = new GUILayout.AreaScope(new Rect(10, 10, 100, 100)))
        ///        {
        ///            GUILayout.Button("Click me");
        ///            GUILayout.Button("Or me");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="BeginArea" />
        ///<seealso cref="EndArea" />
        public class AreaScope : GUI.Scope
        {
            ///<summary>Create a new AreaScope and begin the corresponding Area.</summary>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public AreaScope(Rect screenRect)
            {
                BeginArea(screenRect);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new AreaScope and begin the corresponding Area.</summary>
            ///<param name="text">Optional text to display in the area.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public AreaScope(Rect screenRect, string text)
            {
                BeginArea(screenRect, text);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new AreaScope and begin the corresponding Area.</summary>
            ///<param name="image">Optional texture to display in the area.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public AreaScope(Rect screenRect, Texture image)
            {
                BeginArea(screenRect, image);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new AreaScope and begin the corresponding Area.</summary>
            ///<param name="content">Optional text, image and tooltip top display for this area.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public AreaScope(Rect screenRect, GUIContent content)
            {
                BeginArea(screenRect, content);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new AreaScope and begin the corresponding Area.</summary>
            ///<param name="text">Optional text to display in the area.</param>
            ///<param name="style">The style to use. If left out, the empty <see cref="GUIStyle" /> (<see cref="GUIStyle.none" />) is used, giving a transparent background.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public AreaScope(Rect screenRect, string text, GUIStyle style)
            {
                BeginArea(screenRect, text, style);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new AreaScope and begin the corresponding Area.</summary>
            ///<param name="image">Optional texture to display in the area.</param>
            ///<param name="style">The style to use. If left out, the empty <see cref="GUIStyle" /> (<see cref="GUIStyle.none" />) is used, giving a transparent background.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public AreaScope(Rect screenRect, Texture image, GUIStyle style)
            {
                BeginArea(screenRect, image, style);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new AreaScope and begin the corresponding Area.</summary>
            ///<param name="content">Optional text, image and tooltip top display for this area.</param>
            ///<param name="style">The style to use. If left out, the empty <see cref="GUIStyle" /> (<see cref="GUIStyle.none" />) is used, giving a transparent background.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public AreaScope(Rect screenRect, GUIContent content, GUIStyle style)
            {
                BeginArea(screenRect, content, style);
            }
            #pragma warning restore UAL0015

            protected override void CloseScope()
            {
                EndArea();
            }
        }

        ///<summary>Disposable helper class for managing <see cref="BeginScrollView" /> / <see cref="EndScrollView" />.</summary>
        ///<remarks>Automatically laid out scrollviews will take whatever content you have inside them and display normally. If it doesn't fit, scrollbars will appear. A call to BeginScrollView must always be matched with a call to EndScrollView.
        ///
        ///<img src="GUILayoutScrollView.png" />
        ///
        ///Scroll View in the Game View..</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEditor;
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // The variable to control where the scrollview 'looks' into its child elements.
        ///    public Vector2 scrollPosition;
        ///
        ///    // The string to display inside the scrollview. 2 buttons below add & clear this string.
        ///    public string longString = "This is a long-ish string";
        ///
        ///    void OnGUI()
        ///    {
        ///        // Begin a scroll view. All rects are calculated automatically -
        ///        // it will use up any available screen space and make sure contents flow correctly.
        ///        // This is kept small with the last two parameters to force scrollbars to appear.
        ///        using (var scrollViewScope = new ScrollViewScope(scrollPosition, GUILayout.Width(100), GUILayout.Height(100)))
        ///        {
        ///            scrollPosition = scrollViewScope.scrollPosition;
        ///
        ///            // We just add a single label to go inside the scroll view. Note how the
        ///            // scrollbars will work correctly with wordwrap.
        ///            GUILayout.Label(longString);
        ///
        ///            // Add a button to clear the string. This is inside the scroll area, so it
        ///            // will be scrolled as well. Note how the button becomes narrower to make room
        ///            // for the vertical scrollbar
        ///            if (GUILayout.Button("Clear"))
        ///                longString = "";
        ///        }
        ///
        ///        // Now we add a button outside the scrollview - this will be shown below
        ///        // the scrolling area.
        ///        if (GUILayout.Button("Add More Text"))
        ///            longString += "\nHere is another line";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public class ScrollViewScope : GUI.Scope
        {
            ///<summary>The modified scrollPosition. Feed this back into the variable you pass in, as shown in the example.</summary>
            public Vector2 scrollPosition { get; private set; }
            ///<summary>Whether this ScrollView should handle scroll wheel events. (default: true).</summary>
            public bool handleScrollWheel { get; set; }

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="scrollPosition">The position to use display.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="scrollPosition">The position to use display.</param>
            ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when the content inside the ScrollView is wider than the scrollview itself.</param>
            ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when content inside the ScrollView is taller than the scrollview itself.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="scrollPosition">The position to use display.</param>
            ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public ScrollViewScope(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, horizontalScrollbar, verticalScrollbar, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="scrollPosition">The position to use display.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public ScrollViewScope(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, style, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="scrollPosition">The position to use display.</param>
            ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when the content inside the ScrollView is wider than the scrollview itself.</param>
            ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when content inside the ScrollView is taller than the scrollview itself.</param>
            ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, options);
            }
            #pragma warning restore UAL0015

            ///<summary>Create a new ScrollViewScope and begin the corresponding ScrollView.</summary>
            ///<param name="scrollPosition">The position to use display.</param>
            ///<param name="alwaysShowHorizontal">Optional parameter to always show the horizontal scrollbar. If false or left out, it is only shown when the content inside the ScrollView is wider than the scrollview itself.</param>
            ///<param name="alwaysShowVertical">Optional parameter to always show the vertical scrollbar. If false or left out, it is only shown when content inside the ScrollView is taller than the scrollview itself.</param>
            ///<param name="horizontalScrollbar">Optional <see cref="GUIStyle" /> to use for the horizontal scrollbar. If left out, the <c>horizontalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            ///<param name="verticalScrollbar">Optional <see cref="GUIStyle" /> to use for the vertical scrollbar. If left out, the <c>verticalScrollbar</c> style from the current <see cref="GUISkin" /> is used.</param>
            #pragma warning disable UAL0015 // GUI.Scope subclasses below are used()-scoped within a single OnGUI call and never outlive it; the Begin* calls' transitive side effects on guiIsExiting/s_StateCache are benign
            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background, options);
            }
            #pragma warning restore UAL0015

            protected override void CloseScope()
            {
                EndScrollView(handleScrollWheel);
            }
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
