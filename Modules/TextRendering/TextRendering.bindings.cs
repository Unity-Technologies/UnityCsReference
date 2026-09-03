// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    ///<summary>How multiline text should be aligned.</summary>
    ///<remarks>This is used by the <see cref="TextMesh.alignment" /> property.</remarks>
    public enum TextAlignment
    {
        ///<summary>Text lines are aligned on the left side.</summary>
        Left = 0,
        ///<summary>Text lines are centered.</summary>
        Center = 1,
        ///<summary>Text lines are aligned on the right side.</summary>
        Right = 2
    }

    ///<summary>Where the anchor of the text is placed.</summary>
    ///<remarks>This is used by <see cref="P:UnityEngine.UI.Text.anchor" /> property.</remarks>
    public enum TextAnchor
    {
        ///<summary>Text is anchored in upper left corner.</summary>
        UpperLeft = 0,
        ///<summary>Text is anchored in upper side, centered horizontally.</summary>
        UpperCenter = 1,
        ///<summary>Text is anchored in upper right corner.</summary>
        UpperRight = 2,
        ///<summary>Text is anchored in left side, centered vertically.</summary>
        MiddleLeft = 3,
        ///<summary>Text is centered both horizontally and vertically.</summary>
        MiddleCenter = 4,
        ///<summary>Text is anchored in right side, centered vertically.</summary>
        MiddleRight = 5,
        ///<summary>Text is anchored in lower left corner.</summary>
        LowerLeft = 6,
        ///<summary>Text is anchored in lower side, centered horizontally.</summary>
        LowerCenter = 7,
        ///<summary>Text is anchored in lower right corner.</summary>
        LowerRight = 8
    }

    /// <summary>
    /// Defines the types of text generators to use.
    /// </summary>
    /// <remarks>
    /// This enum is used to switch between Unity's standard and advanced text generators.
    /// </remarks>
    public enum TextGeneratorType
    {
        /// <summary>
        /// The standard text generator, which is the default option.
        /// </summary>
        Standard = 0,

        /// <summary>
        /// Supports comprehensive Unicode and text shaping for various languages and scripts, including right-to-left (RTL) languages.
        /// </summary>
        /// <remarks>
        /// Note that the advanced generator is in development and may not support all features of the standard generator.
        /// </remarks>
        Advanced = 1,
    }

    ///<summary>Wrapping modes for text that reaches the horizontal boundary.</summary>
    public enum HorizontalWrapMode
    {
        ///<summary>Text will word-wrap when reaching the horizontal boundary.</summary>
        Wrap = 0,
        ///<summary>Text can exceed the horizontal boundary.</summary>
        Overflow = 1
    }

    ///<summary>Wrapping modes for text that reaches the vertical boundary.</summary>
    public enum VerticalWrapMode
    {
        ///<summary>Text will be clipped when reaching the vertical boundary.</summary>
        Truncate = 0,
        ///<summary>Text well continue to generate when reaching vertical boundary.</summary>
        Overflow = 1
    }

    ///<summary>The TextMesh component allows you to display text in 3D [text mesh component](xref:class-TextMesh).
    ///
    ///This component dynamically generates a mesh that fits the text specified as input, it is great to make world space UI like displaying names above characters like the example below.
    ///
    ///Note that Text Mesh Pro is now the preferred solution for creating 3D text as it's more feature complete compared to TextMesh.</summary>
    ///<example>
    ///  <code><![CDATA[using UnityEngine;
    ///
    ///public class CharacterNameTag : MonoBehaviour
    ///{
    ///    public string characterName = "Player";
    ///    public Vector3 nameOffset = new Vector3(0, 2, 0);
    ///
    ///    GameObject nameTag;
    ///
    ///    void Start()
    ///    {
    ///        // Create a new GameObject for the name tag as a children
    ///        nameTag = new GameObject("NameTag");
    ///        nameTag.transform.SetParent(transform);
    ///
    ///        // Add a TextMesh component
    ///        var textMesh = nameTag.AddComponent<TextMesh>();
    ///
    ///        // Set text properties and make sure it stays centered.
    ///        textMesh.text = characterName;
    ///        textMesh.fontSize = 32;
    ///        textMesh.characterSize = 0.1f;
    ///        textMesh.anchor = TextAnchor.LowerCenter;
    ///        textMesh.alignment = TextAlignment.Center;
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        // Make sure the name tag follows the offset
    ///        nameTag.transform.position = transform.position + nameOffset;
    ///
    ///        // Make sure that the name tag is always facing the main camera
    ///        if (Camera.main != null)
    ///            nameTag.transform.rotation = Quaternion.LookRotation(nameTag.transform.position - Camera.main.transform.position);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso href="xref:class-TextMesh">text mesh component</seealso>
    [RequireComponent(typeof(Transform), typeof(MeshRenderer))]
    [NativeClass("TextRenderingPrivate::TextMesh", PersistentTypeId = 102),
     NativeHeader("Modules/TextRendering/Public/TextMesh.h")]
    public sealed class TextMesh : Component
    {
        ///<summary>The specified input text to display.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Set the text of the attached Text mesh
        ///        GetComponent<TextMesh>().text = "Hello World";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern string text { get; set; }
        ///<summary>The <see cref="Font" /> used.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Set the text of the attached Text mesh
        ///        Font newFont = new Font();
        ///        GetComponent<TextMesh>().font = newFont;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="xref:class-TextMesh">text mesh component</seealso>
        public extern Font font { get; set; }
        ///<summary>The font size to use (for <see cref="Font.dynamic">dynamic fonts</see>).</summary>
        ///<remarks>If this is set to a non-zero value, the font size specified in the font importer is overriden with a custom size.
        ///This is only supported for fonts set to use dynamic font rendering. Other fonts will always use the default font size.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().fontSize = 12;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern int fontSize { get; set; }
        ///<summary>The font style to use (for dynamic fonts).</summary>
        ///<remarks>If this is set to a value other then normal, the font style set in the font importer is overriden with a custom style.
        ///This is only supported for fonts set to use dynamic font rendering. Other fonts will always render in normal style.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().fontStyle = FontStyle.Bold;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern FontStyle fontStyle { get; set; }
        ///<summary>How far should the text be offset from the transform.position.z when drawing.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().offsetZ = 5;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern float offsetZ { get; set; }
        ///<summary>How lines of text are aligned (Left, Right, Center).</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().alignment = TextAlignment.Left;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern TextAlignment alignment { get; set; }
        ///<summary>Which point of the text shares the position of the Transform.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().anchor = TextAnchor.MiddleCenter;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern TextAnchor anchor { get; set; }
        ///<summary>The size of each character (This scales the whole text).</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().characterSize = 10;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern float characterSize { get; set; }
        ///<summary>How much space will be in-between lines of text.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().lineSpacing = 10;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern float lineSpacing { get; set; }
        ///<summary>How much space will be inserted for a tab '\t' character. This is a multiplum of the 'spacebar' character offset.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().tabSize = 5;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern float tabSize { get; set; }
        ///<summary>Enable HTML-style tags for Text Formatting Markup.</summary>
        ///<remarks>Supported tags are:
        ///
        ///&lt;color="htmlcolor"&gt;colored text&lt;/color&gt;, where "htmlcolor" is a html color string, like "#ff0000" or "red".
        ///
        ///&lt;b&gt;bold text&lt;/b&gt;
        ///
        ///&lt;i&gt;italic text&lt;/i&gt;
        ///
        ///&lt;size=20&gt;sized text&lt;/size&gt;
        ///
        ///&lt;material=1&gt;render using custom material index&lt;/material&gt;
        ///
        ///&lt;quad material=1 size=20 x=0.1 y=0.1 width=0.5 height=0.5/&gt;, to render a single quad using the given material and UVs, used for embedding images in text.
        ///
        ///These are only supported for fonts set to use dynamic font rendering, except for the 'color', 'material' and 'quad' tags.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<TextMesh>().richText = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern bool richText { get; set; }
        ///<summary>The color used to render the text.</summary>
        ///<remarks>This is the base color used to render the text. &lt;color&gt; tags in rich text markup will override this.</remarks>
        public extern Color color { get; set; }
    }

    ///<summary>Specification for how to render a character from the font texture. See <see cref="Font.characterInfo" />.</summary>
    ///<seealso cref="Font.RequestCharactersInTexture" />
    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct CharacterInfo
    {
        ///<summary>Unicode value of the character.</summary>
        public int index;
        ///<summary>UV coordinates for the character in the texture.</summary>
        [Obsolete("CharacterInfo.uv is deprecated. Use uvBottomLeft, uvBottomRight, uvTopRight or uvTopLeft instead.")]
        public Rect uv;
        ///<summary>Screen coordinates for the character in generated text meshes.</summary>
        [Obsolete("CharacterInfo.vert is deprecated. Use minX, maxX, minY, maxY instead.")]
        public Rect vert;
        ///<summary>How far to advance between the beginning of this charcater and the next.</summary>
        [Obsolete("CharacterInfo.width is deprecated. Use advance instead.")]
        [NativeName("advance")] public float width;
        ///<summary>The size of the character or 0 if it is the default font size.</summary>
        ///<remarks>Only used with characters generated at runtime for dynamic fonts).</remarks>
        public int size;
        ///<summary>The style of the character.</summary>
        ///<remarks>Only used with characters generated at runtime for dynamic fonts).</remarks>
        public FontStyle style;
        ///<summary>Is the character flipped?</summary>
        ///<remarks>Unity may flip the U and V coordinates of characters in font textures it generates
        ///to make more efficient use of texture space.</remarks>
        [Obsolete("CharacterInfo.flipped is deprecated. Use uvBottomLeft, uvBottomRight, uvTopRight or uvTopLeft instead, which will be correct regardless of orientation.")]
        public bool flipped;

        #pragma warning disable 0618
        ///<summary>The horizontal distance, rounded to the nearest integer, from the origin of this character to the origin of the next character.</summary>
        public int advance
        {
            get { return (int)Math.Round(width, MidpointRounding.AwayFromZero); }
            set { width = value; }
        }

        ///<summary>The width of the glyph image.</summary>
        public int glyphWidth
        {
            get { return (int)vert.width; }
            set { vert.width = value; }
        }

        ///<summary>The height of the glyph image.</summary>
        public int glyphHeight
        {
            get { return (int)-vert.height; }
            set
            {
                var old = vert.height;
                vert.height = -value;
                vert.y += old - vert.height;
            }
        }

        ///<summary>The horizontal distance from the origin of this glyph to the begining of the glyph image.</summary>
        public int bearing
        {
            get { return (int)vert.x; }
            set { vert.x = value; }
        }

        ///<summary>The minimum extend of the glyph image in the y-axis.</summary>
        public int minY
        {
            get { return (int)(vert.y + vert.height); }
            set { vert.height = value - vert.y; }
        }

        ///<summary>The maximum extend of the glyph image in the y-axis.</summary>
        public int maxY
        {
            get { return (int)vert.y; }
            set
            {
                var old = vert.y;
                vert.y = value;
                vert.height += old - vert.y;
            }
        }

        ///<summary>The minium extend of the glyph image in the x-axis.</summary>
        public int minX
        {
            get { return (int)vert.x; }
            set
            {
                var old = vert.x;
                vert.x = value;
                vert.width += old - vert.x;
            }
        }

        ///<summary>The maximum extend of the glyph image in the x-axis.</summary>
        public int maxX
        {
            get { return (int)(vert.x + vert.width); }
            set { vert.width = value - vert.x; }
        }

        internal Vector2 uvBottomLeftUnFlipped
        {
            get { return new Vector2(uv.x, uv.y); }
            set
            {
                var old = uvTopRightUnFlipped;
                uv.x = value.x;
                uv.y = value.y;
                uv.width = old.x - uv.x;
                uv.height = old.y - uv.y;
            }
        }

        internal Vector2 uvBottomRightUnFlipped
        {
            get { return new Vector2(uv.x + uv.width, uv.y); }
            set
            {
                var old = uvTopRightUnFlipped;
                uv.width = value.x - uv.x;
                uv.y = value.y;
                uv.height = old.y - uv.y;
            }
        }

        internal Vector2 uvTopRightUnFlipped
        {
            get { return new Vector2(uv.x + uv.width, uv.y + uv.height); }
            set
            {
                uv.width = value.x - uv.x;
                uv.height = value.y - uv.y;
            }
        }

        internal Vector2 uvTopLeftUnFlipped
        {
            get { return new Vector2(uv.x, uv.y + uv.height); }
            set
            {
                var old = uvTopRightUnFlipped;
                uv.x = value.x;
                uv.height = value.y - uv.y;
                uv.width = old.x - uv.x;
            }
        }

        ///<summary>The uv coordinate matching the bottom left of the glyph image in the font texture.</summary>
        public Vector2 uvBottomLeft
        {
            get { return uvBottomLeftUnFlipped; }
            set { uvBottomLeftUnFlipped = value; }
        }

        ///<summary>The uv coordinate matching the bottom right of the glyph image in the font texture.</summary>
        public Vector2 uvBottomRight
        {
            get { return flipped ? uvTopLeftUnFlipped : uvBottomRightUnFlipped; }
            set
            {
                if (flipped)
                    uvTopLeftUnFlipped = value;
                else
                    uvBottomRightUnFlipped = value;
            }
        }

        ///<summary>The uv coordinate matching the top right of the glyph image in the font texture.</summary>
        public Vector2 uvTopRight
        {
            get { return uvTopRightUnFlipped; }
            set { uvTopRightUnFlipped = value; }
        }

        ///<summary>The uv coordinate matching the top left of the glyph image in the font texture.</summary>
        public Vector2 uvTopLeft
        {
            get { return flipped ? uvBottomRightUnFlipped : uvTopLeftUnFlipped; }
            set
            {
                if (flipped)
                    uvBottomRightUnFlipped = value;
                else
                    uvTopLeftUnFlipped = value;
            }
        }
        #pragma warning restore 0618
    }

    ///<summary>Class that specifies some information about a renderable character.</summary>
    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct UICharInfo
    {
        ///<summary>Position of the character's origin in local space, typically where a cursor (or caret) is located.</summary>
        public Vector2 cursorPos;
        ///<summary>Character width.</summary>
        public float charWidth;
    }

    ///<summary>Information about a generated line of text.</summary>
    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct UILineInfo
    {
        ///<summary>Index of the first character in the line.</summary>
        public int startCharIdx;
        ///<summary>Height of the line.</summary>
        public int height;
        ///<summary>The upper Y position of the line in pixels. This is used for text annotation such as the caret and selection box in the InputField.</summary>
        public float topY;
        ///<summary>Space in pixels between this line and the next line.</summary>
        public float leading;
    }

    ///<summary>Vertex class used by a <see cref="T:UnityEngine.Canvas" /> for managing vertices.</summary>
    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct UIVertex
    {
        ///<summary>Vertex position.</summary>
        public Vector3 position;
        ///<summary>Normal.</summary>
        public Vector3 normal;
        ///<summary>Tangent.</summary>
        public Vector4 tangent;
        ///<summary>Vertex color.</summary>
        public Color32 color;
        ///<summary>The first texture coordinate set of the mesh. Used by UI elements by default.</summary>
        public Vector4 uv0;
        ///<summary>The second texture coordinate set of the mesh, if present.</summary>
        public Vector4 uv1;
        ///<summary>The Third texture coordinate set of the mesh, if present.</summary>
        public Vector4 uv2;
        ///<summary>The forth texture coordinate set of the mesh, if present.</summary>
        public Vector4 uv3;
        ///<summary>The previous position of the vertex, if present.</summary>
        public Vector4 prevPosition;

        private static readonly Color32 s_DefaultColor = new Color32(255, 255, 255, 255);
        private static readonly Vector4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);

        ///<summary>Simple UIVertex with sensible settings for use in the UI system.</summary>
        [NoAutoStaticsCleanup] // mutable public API default; cannot be made readonly
        public static UIVertex simpleVert = new UIVertex
        {
            position = Vector3.zero,
            normal = Vector3.back,
            tangent = s_DefaultTangent,
            color = s_DefaultColor,
            uv0 = Vector4.zero,
            uv1 = Vector4.zero,
            uv2 = Vector4.zero,
            uv3 = Vector4.zero,
            prevPosition = Vector4.zero
        };
    }

    ///<summary>Script interface for [font assets](xref:class-Font).</summary>
    ///<remarks>You can use this class to dynamically switch fonts on Text Meshes.</remarks>
    ///<seealso cref="TextMesh" />
    [NativeClass("TextRendering::Font", PersistentTypeId = 128),
     NativeHeader("Modules/TextRendering/Public/Font.h"),
     NativeHeader("Modules/TextRendering/Public/FontImpl.h"),
     StaticAccessor("TextRenderingPrivate", StaticAccessorType.DoubleColon)]
    public sealed partial class Font : Object
    {
        ///<summary>Set a function to be called when the dynamic font texture is rebuilt.</summary>
        ///<remarks>This lets you set a delegate function to be called when the dynamic font texture is rebuilt. This will happen when new characters added to the font no longer fit into the texture. The font texture will then be rebuilt to fit all needed characters. If you use custom meshes to render characters from the font, you will need to use this callback to regenerate such meshes, as previous UV coordinates from the Font will no longer be valid.</remarks>
        ///<seealso cref="RequestCharactersInTexture" />
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<Font> textureRebuilt;

        private event FontTextureRebuildCallback m_FontTextureRebuildCallback;
        ///<exclude />
        public delegate void FontTextureRebuildCallback();

        ///<summary>The material used for the font display.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Swap 3D Text font color each second
        ///    // Add this script to a text mesh object
        ///    bool flag = false;
        ///    float rate = 1f;
        ///    TextMesh t;
        ///
        ///    void Update()
        ///    {
        ///        t = transform.GetComponent<TextMesh>();
        ///        if (Time.time > rate)
        ///        {
        ///            if (flag)
        ///            {
        ///                t.font.material.color = Color.yellow;
        ///                flag = false;
        ///            }
        ///            else
        ///            {
        ///                t.font.material.color = Color.red;
        ///                flag = true;
        ///            }
        ///            rate += 1;
        ///        }
        ///        t.text = "This is a 3D text changing colors!";
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Material material { get; set; }
        ///<exclude />
        public extern string[] fontNames { [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)] get; [param: UnityMarshalAs(NativeType.ScriptingObjectPtr)] set; }
        ///<summary>Is the font a dynamic font.</summary>
        public extern bool dynamic { get; }
        // The subset recipe key when this font is a subset produced by the importer; empty otherwise.
        [VisibleToOtherModules("UnityEditor.TextCoreTextEngineModule")]
        internal extern string subsetKey { get; }
        internal extern bool includeFontData { [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")] get; }
        ///<summary>The ascent of the font.</summary>
        ///<remarks>The ascent of the font is the distance from the baseline to the top line of the font, as defined in the font's original data file.</remarks>
        public extern int ascent { get; }
        ///<summary>The default size of the font.</summary>
        public extern int fontSize { get; }

        ///<summary>Access an array of all characters contained in the font texture.</summary>
        ///<remarks>You can read this if you want to render the font texture using custom generated Meshes, or you can set it when you want to
        ///build your own custom font assets from scripts (or modify existing ones).</remarks>
        ///<seealso cref="GetCharacterInfo" />
        public extern CharacterInfo[] characterInfo
        {
            [FreeFunction("TextRenderingPrivate::GetFontCharacterInfo", HasExplicitThis = true)] get;
            [FreeFunction("TextRenderingPrivate::SetFontCharacterInfo", HasExplicitThis = true)] set;
        }

        ///<summary>The line height of the font.</summary>
        ///<remarks>This is the line height of the font, used to align lines of text above each other.</remarks>
        [NativeProperty("LineSpacing", false, TargetType.Function)] public extern int lineHeight { get; }

        ///<exclude />
        [Obsolete("Font.textureRebuildCallback has been deprecated. Use Font.textureRebuilt instead.")]
        public FontTextureRebuildCallback textureRebuildCallback
        {
            get { return m_FontTextureRebuildCallback; }
            set { m_FontTextureRebuildCallback = value; }
        }

        ///<summary>Create a new Font.</summary>
        ///<remarks>You may want to use this if you need to create Font objects programmatically to set up your own font by assigning the <see cref="Font.characterInfo" /> property.</remarks>
        public Font()
        {
            Internal_CreateFont(this, null);
        }

        ///<summary>Create a new Font.</summary>
        ///<remarks>You may want to use this if you need to create Font objects programmatically to set up your own font by assigning the <see cref="Font.characterInfo" /> property.</remarks>
        ///<param name="name">The name of the created Font object.</param>
        public Font(string name)
        {
            // Determine if string name contains the name of the font file or the path to the font file.
            bool isFileName = System.IO.Path.GetDirectoryName(name) == string.Empty;

            if (isFileName)
                Internal_CreateFont(this, name);
            else
                Internal_CreateFontFromPath(this, name);
        }

        private Font(string[] names, int size)
        {
            Internal_CreateDynamicFont(this, names, size);
        }

        ///<summary>Creates a Font object which lets you render a font installed on the user machine.</summary>
        ///<remarks>CreateDynamicFontFromOSFont creates a font object which references fonts from the OS. This lets you render text using any font installed on the user's machine. See <see cref="GetOSInstalledFontNames" /> for getting names of installed fonts at runtime, which can be used with this function.</remarks>
        ///<param name="fontname">The name of the OS font to use for this font object.</param>
        ///<param name="size">The default character size of the generated font.</param>
        ///<returns>The generate Font object.</returns>
        public static Font CreateDynamicFontFromOSFont(string fontname, int size)
        {
            return new Font(new[] {fontname}, size);
        }

        ///<summary>Creates a Font object which lets you render a font installed on the user machine.</summary>
        ///<remarks>CreateDynamicFontFromOSFont creates a font object which references fonts from the OS. This lets you render text using any font installed on the user's machine. See <see cref="GetOSInstalledFontNames" /> for getting names of installed fonts at runtime, which can be used with this function.</remarks>
        ///<param name="size">The default character size of the generated font.</param>
        ///<param name="fontnames">Am array of names of OS fonts to use for this font object. When rendering characters using this font object, the first font which is installed on the machine, which contains the requested character will be used.</param>
        ///<returns>The generate Font object.</returns>
        public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size)
        {
            return new Font(fontnames, size);
        }

        [RequiredByNativeCode]
        internal static void InvokeTextureRebuilt_Internal(Font font)
        {
            textureRebuilt?.Invoke(font);
            font.m_FontTextureRebuildCallback?.Invoke();
        }

        ///<summary>Returns the maximum number of verts that the text generator may return for a given string.</summary>
        ///<param name="str">Input string.</param>
        public static int GetMaxVertsForString(string str)
        {
            return str.Length * 4 + 4;
        }

        [VisibleToOtherModules("UnityEditor.TextRenderingModule", "UnityEngine.TextCoreTextEngineModule")]
        internal static extern Font GetDefault();

        ///<summary>Does this font have a specific character?</summary>
        ///<remarks>This function checks whether the font has a particular character defined. Some fonts do not have all characters defined (for example, no symbols, or no lower case characters).</remarks>
        ///<param name="c">The character to check for.</param>
        ///<returns>Whether or not the font has the character specified.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class FontCheck : MonoBehaviour
        ///{
        ///    // Detects if the current font of a 3D text
        ///    // supports '-' sign
        ///    TextMesh t;
        ///    void Start()
        ///    {
        ///        t = transform.GetComponent<TextMesh>();
        ///        if (t.font.HasCharacter('-'))
        ///        {
        ///            Debug.Log("Font supports '-' sign.");
        ///        }
        ///        else
        ///        {
        ///            Debug.LogWarning("This font doesnt support '-'");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool HasCharacter(char c)
        {
            return HasCharacter((int)c);
        }

        private extern bool HasCharacter(int c);

        ///<summary>Get names of fonts installed on the machine.</summary>
        ///<remarks>GetOSInstalledFontNames lets you get the names of all the fonts installed on the machine. These names can be passed to <see cref="CreateDynamicFontFromOSFont" />, to dynamically render text using any font installed on the user's OS.</remarks>
        ///<returns>An array of the names of all fonts installed on the machine.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // A simple UI to display a selection of OS fonts and allow changing the UI font to any of them.
        ///public class FontSelector : MonoBehaviour
        ///{
        ///    Vector2 scrollPos;
        ///    string[] fonts;
        ///
        ///    void Start()
        ///    {
        ///        fonts = Font.GetOSInstalledFontNames();
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        scrollPos = GUILayout.BeginScrollView(scrollPos);
        ///
        ///        foreach (var font in fonts)
        ///        {
        ///            if (GUILayout.Button(font))
        ///                GUI.skin.font = Font.CreateDynamicFontFromOSFont(font, 12);
        ///        }
        ///        GUILayout.EndScrollView();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern string[] GetOSInstalledFontNames();
        ///<summary>Gets the file paths of the fonts that are installed on the operating system.</summary>
        ///<remarks>This function lets you get the file paths and names of all the fonts installed on the machine. You can use these paths in conjunction with the <see cref="Font" />-ctor constructor to create new Font objects.</remarks>
        ///<returns>An array of the file paths of all fonts installed on the machine.</returns>
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        public static extern string[] GetPathsToOSFonts();
        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
        internal static extern string[] GetOSFallbacks();

        [NativeMethod(IsThreadSafe = true)][VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.CoreModule")]
        internal static extern bool IsFontSmoothingEnabled();

        private static extern void Internal_CreateFont([Writable] Font self, string name);
        private static extern void Internal_CreateFontFromPath([Writable] Font self, string fontPath);
        private static extern void Internal_CreateDynamicFont([Writable] Font self, [UnityMarshalAs(NativeType.ScriptingObjectPtr)]string[] _names, int size);

        ///<summary>Get rendering info for a specific character.</summary>
        ///<remarks>Note: You should only ever need to use this when you want to implement your own text rendering.
        ///If the character <c>ch</c> with the specified <c>size</c> and <c>style</c> is present in the font texture, then this method will
        ///return true, and info will contain the texture placement information for that character. If the character is not
        ///present, this method returns false. If <c>size</c> is zero, it will use the default size for the font.</remarks>
        ///<param name="ch">The character you need rendering information for.</param>
        ///<param name="info">Returns the CharacterInfo struct with the rendering information for the character (if available).</param>
        ///<param name="size">The size of the character (default value of zero will use font default size).</param>
        ///<param name="style">The style of the character.</param>
        ///<seealso cref="characterInfo" />
        ///<seealso cref="RequestCharactersInTexture" />
        [FreeFunction("TextRenderingPrivate::GetCharacterInfo", HasExplicitThis = true)]
        public extern bool GetCharacterInfo(char ch, out CharacterInfo info, [DefaultValue("0")] int size, [DefaultValue("FontStyle.Normal")] FontStyle style);
        [ExcludeFromDocs] public bool GetCharacterInfo(char ch, out CharacterInfo info, int size) { return GetCharacterInfo(ch, out info, size, FontStyle.Normal); }
        [ExcludeFromDocs] public bool GetCharacterInfo(char ch, out CharacterInfo info) { return GetCharacterInfo(ch, out info, 0, FontStyle.Normal); }

        ///<summary>Request characters to be added to the font texture (dynamic fonts only).</summary>
        ///<remarks>Note: You should only ever need to use this when you want to implement your own text rendering.
        ///Call this function to request Unity to make sure all the characters in the string <c>characters</c> are available
        ///in the font's font texture (and it's <c>characterInfo</c> property). This is useful when you want to implement your
        ///own code to render dynamic fonts. You can supply a custom font size and style for the characters. If <c>size</c> is zero
        ///(the default), it will use the default size for that font.
        ///
        ///RequestCharactersInTexture may cause the font texture to be regenerated if it does not have space to add all the
        ///requested characters. If the font texture is regenerated it will only contain characters which have been used
        ///using Font.RequestCharactersInTexture, or using Unity's text rendering functions during the last frame. So
        ///it is advisable to always call RequestCharactersInTexture for any text on the screen you wish to render using
        ///custom font rendering functions, even if the characters are currently present in the texture, to make sure they
        ///don't get purged during texture rebuild.</remarks>
        ///<param name="characters">The characters which are needed to be in the font texture.</param>
        ///<param name="size">The size of the requested characters (the default value of zero will use the font's default size).</param>
        ///<param name="style">The style of the requested characters.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class CustomFontMeshGenerator : MonoBehaviour
        ///{
        ///    Font font;
        ///    string str = "Hello World";
        ///    Mesh mesh;
        ///
        ///    void OnFontTextureRebuilt(Font changedFont)
        ///    {
        ///        if (changedFont != font)
        ///            return;
        ///
        ///        RebuildMesh();
        ///    }
        ///
        ///    void RebuildMesh()
        ///    {
        ///        // Generate a mesh for the characters we want to print.
        ///        var vertices = new Vector3[str.Length * 4];
        ///        var triangles = new int[str.Length * 6];
        ///        var uv = new Vector2[str.Length * 4];
        ///        Vector3 pos = Vector3.zero;
        ///        for (int i = 0; i < str.Length; i++)
        ///        {
        ///            // Get character rendering information from the font
        ///            CharacterInfo ch;
        ///            font.GetCharacterInfo(str[i], out ch);
        ///
        ///            vertices[4 * i + 0] = pos + new Vector3(ch.minX, ch.maxY, 0);
        ///            vertices[4 * i + 1] = pos + new Vector3(ch.maxX, ch.maxY, 0);
        ///            vertices[4 * i + 2] = pos + new Vector3(ch.maxX, ch.minY, 0);
        ///            vertices[4 * i + 3] = pos + new Vector3(ch.minX, ch.minY, 0);
        ///
        ///            uv[4 * i + 0] = ch.uvTopLeft;
        ///            uv[4 * i + 1] = ch.uvTopRight;
        ///            uv[4 * i + 2] = ch.uvBottomRight;
        ///            uv[4 * i + 3] = ch.uvBottomLeft;
        ///
        ///            triangles[6 * i + 0] = 4 * i + 0;
        ///            triangles[6 * i + 1] = 4 * i + 1;
        ///            triangles[6 * i + 2] = 4 * i + 2;
        ///
        ///            triangles[6 * i + 3] = 4 * i + 0;
        ///            triangles[6 * i + 4] = 4 * i + 2;
        ///            triangles[6 * i + 5] = 4 * i + 3;
        ///
        ///            // Advance character position
        ///            pos += new Vector3(ch.advance, 0, 0);
        ///        }
        ///        mesh.vertices = vertices;
        ///        mesh.triangles = triangles;
        ///        mesh.uv = uv;
        ///    }
        ///
        ///    void Start()
        ///    {
        ///        font = Font.CreateDynamicFontFromOSFont("Helvetica", 16);
        ///        // Set the rebuild callback so that the mesh is regenerated on font changes.
        ///        Font.textureRebuilt += OnFontTextureRebuilt;
        ///
        ///        // Request characters.
        ///        font.RequestCharactersInTexture(str);
        ///
        ///        // Set up mesh.
        ///        mesh = new Mesh();
        ///        GetComponent<MeshFilter>().mesh = mesh;
        ///        GetComponent<MeshRenderer>().material = font.material;
        ///
        ///        // Generate font mesh.
        ///        RebuildMesh();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // Keep requesting our characters each frame, so Unity will make sure that they stay in the font when regenerating the font texture.
        ///        font.RequestCharactersInTexture(str);
        ///    }
        ///
        ///    void OnDestroy()
        ///    {
        ///        Font.textureRebuilt -= OnFontTextureRebuilt;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="textureRebuilt" />
        ///<seealso cref="GetCharacterInfo" />
        public extern void RequestCharactersInTexture(string characters, [DefaultValue("0")] int size, [DefaultValue("FontStyle.Normal")] FontStyle style);
        [ExcludeFromDocs] public void RequestCharactersInTexture(string characters, int size) { RequestCharactersInTexture(characters, size, FontStyle.Normal); }
        [ExcludeFromDocs] public void RequestCharactersInTexture(string characters) { RequestCharactersInTexture(characters, 0, FontStyle.Normal); }
    }

    [UsedByNativeCode, StructLayout(LayoutKind.Sequential),
     NativeHeader("Modules/TextRendering/TextGenerator.h")]
    public sealed partial class TextGenerator
    {
        internal IntPtr m_Ptr;

        private string m_LastString;
        private TextGenerationSettings m_LastSettings;
        private bool m_HasGenerated;
        private TextGenerationError m_LastValid;

        private readonly List<UIVertex> m_Verts;
        private readonly List<UICharInfo> m_Characters;
        private readonly List<UILineInfo> m_Lines;

        private bool m_CachedVerts;
        private bool m_CachedCharacters;
        private bool m_CachedLines;

        [NoAutoStaticsCleanup] // monotonic counter; no user refs
        private static int s_NextId = 0;
        private readonly int m_Id;
        [AutoStaticsCleanupOnCodeReload]
        private static Dictionary<int, WeakReference> s_Instances = new Dictionary<int, WeakReference>();

        ///<summary>Extents of the generated text in rect format.</summary>
        public extern Rect rectExtents { get; }
        ///<summary>Number of vertices generated.</summary>
        public extern int vertexCount { get; }
        ///<summary>The number of characters that have been generated.</summary>
        ///<seealso cref="characterCountVisible" />
        public extern int characterCount { get; }
        ///<summary>Number of text lines generated.</summary>
        public extern int lineCount { get; }

        ///<summary>The size of the font that was found if using best fit mode.</summary>
        [NativeProperty("FontSizeFoundForBestFit", false, TargetType.Function)] public extern int fontSizeUsedForBestFit { get; }

        [NativeMethod(IsThreadSafe = true)] private static extern IntPtr Internal_Create();
        [NativeMethod(IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr ptr);

        internal extern bool Populate_Internal(
            string str, Font font, Color color,
            int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText,
            bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize,
            int verticalOverFlow, int horizontalOverflow, bool updateBounds,
            TextAnchor anchor, float extentsX, float extentsY, float pivotX, float pivotY,
            bool generateOutOfBounds, bool alignByGeometry,
            out uint error);

        internal bool Populate_Internal(
            string str, Font font, Color color,
            int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText,
            bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize,
            VerticalWrapMode verticalOverFlow, HorizontalWrapMode horizontalOverflow, bool updateBounds,
            TextAnchor anchor, Vector2 extents, Vector2 pivot, bool generateOutOfBounds, bool alignByGeometry,
            out TextGenerationError error)
        {
            if (font == null)
            {
                error = TextGenerationError.NoFont;
                return false;
            }

            uint uerror = 0;
            bool res = Populate_Internal(
                str, font, color,
                fontSize, scaleFactor, lineSpacing, style, richText,
                resizeTextForBestFit, resizeTextMinSize, resizeTextMaxSize,
                (int)verticalOverFlow, (int)horizontalOverflow, updateBounds,
                anchor, extents.x, extents.y, pivot.x, pivot.y, generateOutOfBounds, alignByGeometry, out uerror);
            error = (TextGenerationError)uerror;
            return res;
        }

        ///<summary>Returns the current UIVertex array.</summary>
        ///<returns>Vertices.</returns>
        public extern UIVertex[] GetVerticesArray();
        ///<summary>Returns the current UICharInfo.</summary>
        ///<returns>Character information.</returns>
        public extern UICharInfo[] GetCharactersArray();
        ///<summary>Returns the current UILineInfo.</summary>
        ///<returns>Line information.</returns>
        public extern UILineInfo[] GetLinesArray();

        private extern void GetVerticesInternal([NotNull, Out] List<UIVertex> vertices);
        private extern void GetCharactersInternal([NotNull, Out] List<UICharInfo> characters);
        private extern void GetLinesInternal([NotNull, Out] List<UILineInfo> lines);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(TextGenerator textGenerator) => textGenerator.m_Ptr;
        }
    }
}
