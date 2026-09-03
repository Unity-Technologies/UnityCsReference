// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements;

partial class InlineStyleAccess
{
    bool TryGetObject<T>(StylePropertyId id, out T value, out StyleKeyword keyword) where T : class
    {
        var inline = new StyleValueManaged();
        if (TryGetStyleValueManaged(id, ref inline))
        {
            value = inline.value as T;
            keyword = inline.keyword;
            return true;
        }
        keyword = default;
        value = null;
        return false;
    }

    StyleBackground GetStyleBackground(StylePropertyId id)
    {
        var inline = new StyleValueManaged();
        if (TryGetStyleValueManaged(id, ref inline))
        {
            // Inline value: full Background (typed setter) or plain Object (legacy asset).
            Background bg = inline.value is Background b ? b : Background.FromObject(inline.value as Object);
            StyleBackground styleBg = bg;
            styleBg.keyword = inline.keyword;
            return styleBg;
        }
        return StyleKeyword.Null;
    }

    StyleUIAnimationClip GetStyleUIAnimationClip(StylePropertyId id)
    {
        if (TryGetObject(id, out UIAnimationClip clip, out StyleKeyword keyword))
        {
            StyleUIAnimationClip style = clip;
            style.keyword = keyword;
            return style;
        }

        return StyleKeyword.Null;
    }

    private bool SetStyleValue(StylePropertyId id, StyleUIAnimationClip inlineValue)
    {
        StyleUIAnimationClip current = GetStyleUIAnimationClip(id);

        if (current == inlineValue)
            return false;

        var sv = new StyleValueManaged();
        sv.id = id;
        sv.keyword = inlineValue.keyword;
        sv.value = inlineValue.value;
        SetStyleValueManaged(sv);

        if (inlineValue.keyword == StyleKeyword.Null)
            return RemoveInlineStyle(id);

        ApplyStyleValue(sv);
        return true;
    }

    bool SetStyleValue(StylePropertyId id, StyleBackground inlineValue)
    {
        StyleBackground current = GetStyleBackground(id);

        if (current == inlineValue)
            return false;

        var sv = new StyleValueManaged();
        sv.id = id;
        sv.keyword = inlineValue.keyword;
        // Preserve the full Background struct so subsequent reads round-trip losslessly.
        // Note: When gradient is empty, use a code path that does not box.
        var bg = inlineValue.value;
        sv.value = bg.gradient.IsEmpty() ? bg.GetSelectedImage() : bg;
        SetStyleValueManaged(sv);

        if (inlineValue.keyword == StyleKeyword.Null)
            return RemoveInlineStyle(id);

        ApplyStyleValue(sv);
        return true;
    }

    public StyleFontDefinition GetStyleFontDefinition(StylePropertyId id)
    {
        if (TryGetObject(id, out Object obj, out StyleKeyword keyword))
        {
            StyleFontDefinition styleFontDef = FontDefinition.FromObject(obj);
            styleFontDef.keyword = keyword;
            return styleFontDef;
        }
        return StyleKeyword.Null;
    }

    private bool SetStyleValue(StylePropertyId id, StyleFontDefinition inlineValue)
    {
        StyleFontDefinition current = GetStyleFontDefinition(id);

        if (current == inlineValue)
            return false;

        var sv = new StyleValueManaged();
        sv.id = id;
        sv.keyword = inlineValue.keyword;
        sv.value = inlineValue.value.GetSelectedFont();
        SetStyleValueManaged(sv);

        if (inlineValue.keyword == StyleKeyword.Null)
            return RemoveInlineStyle(id);

        ApplyStyleValue(sv);
        return true;
    }


    public StyleFont GetStyleFont(StylePropertyId id)
    {
        if (TryGetObject(id, out Font font, out StyleKeyword keyword))
        {
            StyleFont styleFont = font;
            styleFont.keyword = keyword;
            return styleFont;
        }

        return StyleKeyword.Null;
    }

    private bool SetStyleValue(StylePropertyId id, StyleFont inlineValue)
    {
        StyleFont current = GetStyleFont(id);

        if (current == inlineValue)
            return false;

        var sv = new StyleValueManaged();
        sv.id = id;
        sv.keyword = inlineValue.keyword;
        sv.value = inlineValue.value;
        SetStyleValueManaged(sv);

        if (inlineValue.keyword == StyleKeyword.Null)
            return RemoveInlineStyle(id);

        ApplyStyleValue(sv);
        return true;
    }

    public StyleMaterialDefinition GetStyleMaterialDefinition(StylePropertyId id)
    {
        if (TryGetObject(id, out object obj, out StyleKeyword keyword))
        {
            StyleMaterialDefinition styleMatDef = (MaterialDefinition)obj;
            styleMatDef.keyword = keyword;
            return styleMatDef;
        }
        return StyleKeyword.Null;
    }


    private bool SetStyleValue(StylePropertyId id, StyleMaterialDefinition inlineValue)
    {
        StyleMaterialDefinition current = GetStyleMaterialDefinition(id);

        if (current == inlineValue)
            return false;

        var sv = new StyleValueManaged();
        sv.id = id;
        sv.keyword = inlineValue.keyword;
        sv.value = inlineValue.value; // here we're voluntarily boxing the struct
        SetStyleValueManaged(sv);

        if (inlineValue.keyword == StyleKeyword.Null)
            return RemoveInlineStyle(id);

        ApplyStyleValue(sv);
        return true;
    }


}
