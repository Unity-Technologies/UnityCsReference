// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;

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

[System.Obsolete ("This component is part of the legacy UI system and will be removed in a future release.")]
public sealed partial class GUIText : GUIElement
{
    public extern  string text
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  Material material
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_GetPixelOffset (out Vector2 output) ;

    private void Internal_SetPixelOffset (Vector2 p) {
        INTERNAL_CALL_Internal_SetPixelOffset ( this, ref p );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetPixelOffset (GUIText self, ref Vector2 p);
    public Vector2 pixelOffset { get { Vector2 p; Internal_GetPixelOffset(out p); return p; } set { Internal_SetPixelOffset(value); }  }
    
    
    public extern Font font
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextAlignment alignment
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextAnchor anchor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float lineSpacing
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float tabSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int fontSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern FontStyle fontStyle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool richText
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Color color
    {
        get { Color tmp; INTERNAL_get_color(out tmp); return tmp;  }
        set { INTERNAL_set_color(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_color (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_color (ref Color value) ;

}

[RequireComponent(typeof(Transform), typeof(MeshRenderer))]
[NativeClass("TextRenderingPrivate::TextMesh")]
public sealed partial class TextMesh : Component
{
    public extern  string text
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern Font font
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern int fontSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern FontStyle fontStyle
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float offsetZ
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextAlignment alignment
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern TextAnchor anchor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float characterSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float lineSpacing
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern float tabSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern bool richText
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public Color color
    {
        get { Color tmp; INTERNAL_get_color(out tmp); return tmp;  }
        set { INTERNAL_set_color(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_color (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_set_color (ref Color value) ;

}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct CharacterInfo
{
    
            public int index;
    [System.Obsolete ("CharacterInfo.uv is deprecated. Use uvBottomLeft, uvBottomRight, uvTopRight or uvTopLeft instead.")]
            public Rect uv;
    [System.Obsolete ("CharacterInfo.vert is deprecated. Use minX, maxX, minY, maxY instead.")]
            public Rect vert;
    [System.Obsolete ("CharacterInfo.width is deprecated. Use advance instead.")]
            public float width;
            public int size;
            public FontStyle style;
    [System.Obsolete ("CharacterInfo.flipped is deprecated. Use uvBottomLeft, uvBottomRight, uvTopRight or uvTopLeft instead, which will be correct regardless of orientation.")]
            public bool flipped;
    
    
    
        #pragma warning disable 0618
            public int advance
        {
            get { return (int)width; }
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
            get { return flipped ? uvTopLeftUnFlipped : uvBottomRightUnFlipped;  }
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
            get { return flipped ? uvBottomRightUnFlipped : uvTopLeftUnFlipped;  }
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

public sealed partial class Font : Object
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetOSInstalledFontNames () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateFont ([Writable] Font _font, string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateDynamicFont ([Writable] Font _font, string[] _names, int size) ;

    public static Font CreateDynamicFontFromOSFont(string fontname, int size)
        {
            var font = new Font(new string[] {fontname}, size);
            return font;
        }
    
    
    public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size)
        {
            var font = new Font(fontnames, size);
            return font;
        }
    
    
    public Font() { Internal_CreateFont(this, null); }
    
    
    public Font(string name) { Internal_CreateFont(this, name); }
    
    
    private Font(string[] names, int size)
        {
            Internal_CreateDynamicFont(this, names, size);
        }
    
    
    public extern Material material
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool HasCharacter (char c) ;

    public extern  string[] fontNames
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  CharacterInfo[] characterInfo
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void RequestCharactersInTexture (string characters, [uei.DefaultValue("0")]  int size , [uei.DefaultValue("FontStyle.Normal")]  FontStyle style ) ;

    [uei.ExcludeFromDocs]
    public void RequestCharactersInTexture (string characters, int size ) {
        FontStyle style = FontStyle.Normal;
        RequestCharactersInTexture ( characters, size, style );
    }

    [uei.ExcludeFromDocs]
    public void RequestCharactersInTexture (string characters) {
        FontStyle style = FontStyle.Normal;
        int size = 0;
        RequestCharactersInTexture ( characters, size, style );
    }

    public static event Action<Font> textureRebuilt;
    
    
    [RequiredByNativeCode]
    private static void InvokeTextureRebuilt_Internal(Font font)
        {
            var callback = textureRebuilt;
            if (callback != null)
                callback(font);

            if (font.m_FontTextureRebuildCallback != null)
                font.m_FontTextureRebuildCallback();
        }
    
    
    private event FontTextureRebuildCallback m_FontTextureRebuildCallback;
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public delegate void FontTextureRebuildCallback(); 
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Obsolete ("Font.textureRebuildCallback has been deprecated. Use Font.textureRebuilt instead.")]
    public FontTextureRebuildCallback textureRebuildCallback
        {
            get { return m_FontTextureRebuildCallback; }
            set { m_FontTextureRebuildCallback = value; }
        }
    
    
    public static int GetMaxVertsForString(string str)
        {
            return str.Length * 4 + 4;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public bool GetCharacterInfo (char ch, out CharacterInfo info, [uei.DefaultValue("0")]  int size , [uei.DefaultValue("FontStyle.Normal")]  FontStyle style ) ;

    [uei.ExcludeFromDocs]
    public bool GetCharacterInfo (char ch, out CharacterInfo info, int size ) {
        FontStyle style = FontStyle.Normal;
        return GetCharacterInfo ( ch, out info, size, style );
    }

    [uei.ExcludeFromDocs]
    public bool GetCharacterInfo (char ch, out CharacterInfo info) {
        FontStyle style = FontStyle.Normal;
        int size = 0;
        return GetCharacterInfo ( ch, out info, size, style );
    }

    public extern  bool dynamic
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int ascent
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int lineHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern int fontSize
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct UICharInfo
{
    public Vector2 cursorPos;
    public float charWidth;
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct UILineInfo
{
    public int startCharIdx;
    public int height;
    public float topY;
    public float leading;
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct UIVertex
{
    public Vector3 position;
    public Vector3 normal;
    public Color32 color;
    public Vector2 uv0;
    public Vector2 uv1;
    public Vector2 uv2;
    public Vector2 uv3;
    public Vector4 tangent;
    
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

[StructLayout(LayoutKind.Sequential)]
[UsedByNativeCode]
public sealed partial class TextGenerator : IDisposable
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
            private int m_Id;
            private static readonly Dictionary<int, WeakReference> s_Instances = new Dictionary<int, WeakReference>();
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Init () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Dispose_cpp () ;

    internal bool Populate_Internal(
            string str, Font font, Color color,
            int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText,
            bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize,
            VerticalWrapMode verticalOverFlow, HorizontalWrapMode horizontalOverflow, bool updateBounds,
            TextAnchor anchor, Vector2 extents, Vector2 pivot, bool generateOutOfBounds, bool alignByGeometry,
            out TextGenerationError error)
        {
            uint uerror = 0;
            if (font == null)
            {
                error = TextGenerationError.NoFont;
                return false;
            }
            else
            {
                bool res = Populate_Internal_cpp(
                        str, font, color,
                        fontSize, scaleFactor, lineSpacing, style, richText,
                        resizeTextForBestFit, resizeTextMinSize, resizeTextMaxSize,
                        (int)verticalOverFlow, (int)horizontalOverflow, updateBounds,
                        anchor, extents.x, extents.y, pivot.x, pivot.y, generateOutOfBounds, alignByGeometry, out uerror);
                error = (TextGenerationError)uerror;
                return res;

            }
        }
    
    
    internal bool Populate_Internal_cpp (
            string str, Font font, Color color,
            int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText,
            bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize,
            int verticalOverFlow, int horizontalOverflow, bool updateBounds,
            TextAnchor anchor, float extentsX, float extentsY, float pivotX, float pivotY,
            bool generateOutOfBounds, bool alignByGeometry,
            out uint error) {
        return INTERNAL_CALL_Populate_Internal_cpp ( this, str, font, ref color, fontSize, scaleFactor, lineSpacing, style, richText, resizeTextForBestFit, resizeTextMinSize, resizeTextMaxSize, verticalOverFlow, horizontalOverflow, updateBounds, anchor, extentsX, extentsY, pivotX, pivotY, generateOutOfBounds, alignByGeometry, out error );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Populate_Internal_cpp (TextGenerator self, string str, Font font, ref Color color, int fontSize, float scaleFactor, float lineSpacing, FontStyle style, bool richText, bool resizeTextForBestFit, int resizeTextMinSize, int resizeTextMaxSize, int verticalOverFlow, int horizontalOverflow, bool updateBounds, TextAnchor anchor, float extentsX, float extentsY, float pivotX, float pivotY, bool generateOutOfBounds, bool alignByGeometry, out uint error);
    public  Rect rectExtents
    {
        get { Rect tmp; INTERNAL_get_rectExtents(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_rectExtents (out Rect value) ;


    public extern  int vertexCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetVerticesInternal (object vertices) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public UIVertex[] GetVerticesArray () ;

    public extern  int characterCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public int characterCountVisible { get { return characterCount - 1; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetCharactersInternal (object characters) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public UICharInfo[] GetCharactersArray () ;

    public extern  int lineCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void GetLinesInternal (object lines) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public UILineInfo[] GetLinesArray () ;

    public extern  int fontSizeUsedForBestFit
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}


}
