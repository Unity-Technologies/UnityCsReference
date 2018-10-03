// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum TextAlignment
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    public enum TextAnchor
    {
        UpperLeft = 0,
        UpperCenter = 1,
        UpperRight = 2,
        MiddleLeft = 3,
        MiddleCenter = 4,
        MiddleRight = 5,
        LowerLeft = 6,
        LowerCenter = 7,
        LowerRight = 8
    }

    public enum HorizontalWrapMode
    {
        Wrap = 0,
        Overflow = 1
    }

    public enum VerticalWrapMode
    {
        Truncate = 0,
        Overflow = 1
    }

    [Obsolete("This component is part of the legacy UI system and will be removed in a future release.", false)]
    [NativeClass("TextRenderingPrivate::GUIText"),
     NativeHeader("Runtime/Shaders/Material.h"),
     NativeHeader("Modules/TextRendering/Public/GUIText.h")]
    public sealed class GUIText : GUIElement
    {
        public extern string text { get; set; }

        public extern Material material
        {
            [FreeFunction("TextRenderingPrivate::GetGUITextMaterialWithFallback", HasExplicitThis = true)] get;
            set;
        }
        public extern Font font { get; set; }
        public extern TextAlignment alignment { get; set; }
        public extern TextAnchor anchor { get; set; }
        public extern float lineSpacing { get; set; }
        public extern float tabSize { get; set; }
        public extern int fontSize { get; set; }
        public extern FontStyle fontStyle { get; set; }
        public extern bool richText { get; set; }
        public extern Color color { get; set; }
        public extern Vector2 pixelOffset { get; set; }
    }

    [RequireComponent(typeof(Transform), typeof(MeshRenderer))]
    [NativeClass("TextRenderingPrivate::TextMesh"),
     NativeHeader("Modules/TextRendering/Public/TextMesh.h")]
    public sealed class TextMesh : Component
    {
        public extern string text { get; set; }
        public extern Font font { get; set; }
        public extern int fontSize { get; set; }
        public extern FontStyle fontStyle { get; set; }
        public extern float offsetZ { get; set; }
        public extern TextAlignment alignment { get; set; }
        public extern TextAnchor anchor { get; set; }
        public extern float characterSize { get; set; }
        public extern float lineSpacing { get; set; }
        public extern float tabSize { get; set; }
        public extern bool richText { get; set; }
        public extern Color color { get; set; }
    }

    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct CharacterInfo
    {
        public int index;
        [Obsolete("CharacterInfo.uv is deprecated. Use uvBottomLeft, uvBottomRight, uvTopRight or uvTopLeft instead.")]
        public Rect uv;
        [Obsolete("CharacterInfo.vert is deprecated. Use minX, maxX, minY, maxY instead.")]
        public Rect vert;
        [Obsolete("CharacterInfo.width is deprecated. Use advance instead.")]
        [NativeName("advance")] public float width;
        public int size;
        public FontStyle style;
        [Obsolete("CharacterInfo.flipped is deprecated. Use uvBottomLeft, uvBottomRight, uvTopRight or uvTopLeft instead, which will be correct regardless of orientation.")]
        public bool flipped;

        #pragma warning disable 0618
        public int advance
        {
            get { return (int)Math.Round(width, MidpointRounding.AwayFromZero); }
            set { width = value; }
        }

        public int glyphWidth
        {
            get { return (int)vert.width; }
            set { vert.width = value; }
        }

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

        public int bearing
        {
            get { return (int)vert.x; }
            set { vert.x = value; }
        }

        public int minY
        {
            get { return (int)(vert.y + vert.height); }
            set { vert.height = value - vert.y; }
        }

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

        public Vector2 uvBottomLeft
        {
            get { return uvBottomLeftUnFlipped; }
            set { uvBottomLeftUnFlipped = value; }
        }

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

        public Vector2 uvTopRight
        {
            get { return uvTopRightUnFlipped; }
            set { uvTopRightUnFlipped = value; }
        }

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

    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct UICharInfo
    {
        public Vector2 cursorPos;
        public float charWidth;
    }

    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct UILineInfo
    {
        public int startCharIdx;
        public int height;
        public float topY;
        public float leading;
    }

    [UsedByNativeCode, StructLayout(LayoutKind.Sequential)]
    public struct UIVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Color32 color;
        public Vector2 uv0;
        public Vector2 uv1;
        public Vector2 uv2;
        public Vector2 uv3;

        private static readonly Color32 s_DefaultColor = new Color32(255, 255, 255, 255);
        private static readonly Vector4 s_DefaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);

        public static UIVertex simpleVert = new UIVertex
        {
            position = Vector3.zero,
            normal = Vector3.back,
            tangent = s_DefaultTangent,
            color = s_DefaultColor,
            uv0 = Vector2.zero,
            uv1 = Vector2.zero,
            uv2 = Vector2.zero,
            uv3 = Vector2.zero
        };
    }

    [NativeClass("TextRendering::Font"),
     NativeHeader("Modules/TextRendering/Public/Font.h"),
     NativeHeader("Modules/TextRendering/Public/FontImpl.h"),
     StaticAccessor("TextRenderingPrivate", StaticAccessorType.DoubleColon)]
    public sealed class Font : Object
    {
        public static event Action<Font> textureRebuilt;

        private event FontTextureRebuildCallback m_FontTextureRebuildCallback;
        public delegate void FontTextureRebuildCallback();

        public extern Material material { get; set; }
        public extern string[] fontNames { get; set; }
        public extern bool dynamic { get; }
        public extern int ascent { get; }
        public extern int fontSize { get; }

        public extern CharacterInfo[] characterInfo
        {
            [FreeFunction("TextRenderingPrivate::GetFontCharacterInfo", HasExplicitThis = true)] get;
            [FreeFunction("TextRenderingPrivate::SetFontCharacterInfo", HasExplicitThis = true)] set;
        }

        [NativeProperty("LineSpacing", false, TargetType.Function)] public extern int lineHeight { get; }

        [Obsolete("Font.textureRebuildCallback has been deprecated. Use Font.textureRebuilt instead.")]
        public FontTextureRebuildCallback textureRebuildCallback
        {
            get { return m_FontTextureRebuildCallback; }
            set { m_FontTextureRebuildCallback = value; }
        }

        public Font()
        {
            Internal_CreateFont(this, null);
        }

        public Font(string name)
        {
            Internal_CreateFont(this, name);
        }

        private Font(string[] names, int size)
        {
            Internal_CreateDynamicFont(this, names, size);
        }

        public static Font CreateDynamicFontFromOSFont(string fontname, int size)
        {
            return new Font(new[] {fontname}, size);
        }

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

        public static int GetMaxVertsForString(string str)
        {
            return str.Length * 4 + 4;
        }

        internal static extern Font GetDefault();

        public bool HasCharacter(char c)
        {
            return HasCharacter((int)c);
        }

        private extern bool HasCharacter(int c);

        public static extern string[] GetOSInstalledFontNames();

        private static extern void Internal_CreateFont([Writable] Font self, string name);
        private static extern void Internal_CreateDynamicFont([Writable] Font self, string[] _names, int size);

        [FreeFunction("TextRenderingPrivate::GetCharacterInfo", HasExplicitThis = true)]
        public extern bool GetCharacterInfo(char ch, out CharacterInfo info, [DefaultValue("0")] int size, [DefaultValue("FontStyle.Normal")] FontStyle style);
        [ExcludeFromDocs] public bool GetCharacterInfo(char ch, out CharacterInfo info, int size) { return GetCharacterInfo(ch, out info, size, FontStyle.Normal); }
        [ExcludeFromDocs] public bool GetCharacterInfo(char ch, out CharacterInfo info) { return GetCharacterInfo(ch, out info, 0, FontStyle.Normal); }

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

        private static int s_NextId = 0;
        private readonly int m_Id;
        private static readonly Dictionary<int, WeakReference> s_Instances = new Dictionary<int, WeakReference>();

        public extern Rect rectExtents { get; }
        public extern int vertexCount { get; }
        public extern int characterCount { get; }
        public extern int lineCount { get; }

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

        public extern UIVertex[] GetVerticesArray();
        public extern UICharInfo[] GetCharactersArray();
        public extern UILineInfo[] GetLinesArray();

        [NativeThrows] private extern void GetVerticesInternal(object vertices);
        [NativeThrows] private extern void GetCharactersInternal(object characters);
        [NativeThrows] private extern void GetLinesInternal(object lines);
    }
}
