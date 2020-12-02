using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    internal class StyleValueCollection
    {
        internal List<StyleValue> m_Values = new List<StyleValue>();

        public StyleLength GetStyleLength(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleLength(inline.length, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleFloat GetStyleFloat(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleFloat(inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleInt GetStyleInt(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleInt((int)inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleColor GetStyleColor(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleColor(inline.color, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleBackground GetStyleBackground(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
            {
                var texture = inline.resource.IsAllocated ? inline.resource.Target as Texture2D : null;
                if (texture != null)
                    return new StyleBackground(texture, inline.keyword);

                var sprite = inline.resource.IsAllocated ? inline.resource.Target as Sprite : null;
                if (sprite != null)
                    return new StyleBackground(sprite, inline.keyword);

                var vectorImage = inline.resource.IsAllocated ? inline.resource.Target as VectorImage : null;
                if (vectorImage != null)
                    return new StyleBackground(vectorImage, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        public StyleFont GetStyleFont(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
            {
                var font = inline.resource.IsAllocated ? inline.resource.Target as Font : null;
                return new StyleFont(font, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        public StyleFontDefinition GetStyleFontDefinition(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
            {
                var font = inline.resource.IsAllocated ? inline.resource.Target as object : null;
                return new StyleFontDefinition(font, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        public bool TryGetStyleValue(StylePropertyId id, ref StyleValue value)
        {
            value.id = StylePropertyId.Unknown;
            foreach (var inlineStyle in m_Values)
            {
                if (inlineStyle.id == id)
                {
                    value = inlineStyle;
                    return true;
                }
            }
            return false;
        }

        public void SetStyleValue(StyleValue value)
        {
            for (int i = 0; i < m_Values.Count; i++)
            {
                if (m_Values[i].id == value.id)
                {
                    if (value.keyword == StyleKeyword.Null)
                    {
                        m_Values.RemoveAt(i);
                    }
                    else
                    {
                        m_Values[i] = value;
                    }
                    return;
                }
            }

            m_Values.Add(value);
        }
    }

    internal partial class InlineStyleAccess : StyleValueCollection
    {
        private static StylePropertyReader s_StylePropertyReader = new StylePropertyReader();

        private VisualElement ve { get; set; }

        private bool m_HasInlineCursor;
        private StyleCursor m_InlineCursor;

        private bool m_HasInlineTextShadow;
        private StyleTextShadow m_InlineTextShadow;

        private InlineRule m_InlineRule;
        public InlineRule inlineRule => m_InlineRule;

        internal struct InlineRule
        {
            public StyleSheet sheet;
            public StyleProperty[] properties;
            public StylePropertyId[] propertyIds;
        }

        public InlineStyleAccess(VisualElement ve)
        {
            this.ve = ve;

            if (ve.computedStyle.isShared)
            {
                var inlineStyle = ComputedStyle.CreateUninitialized(false);
                inlineStyle.CopyShared(ve.m_SharedStyle);

                ve.m_Style = inlineStyle;
            }
        }

        ~InlineStyleAccess()
        {
            StyleValue inlineValue = new StyleValue();
            if (TryGetStyleValue(StylePropertyId.BackgroundImage, ref inlineValue))
            {
                if (inlineValue.resource.IsAllocated)
                    inlineValue.resource.Free();
            }
            if (TryGetStyleValue(StylePropertyId.UnityFont, ref inlineValue))
            {
                if (inlineValue.resource.IsAllocated)
                    inlineValue.resource.Free();
            }
        }

        public void SetInlineRule(StyleSheet sheet, StyleRule rule)
        {
            m_InlineRule.sheet = sheet;
            m_InlineRule.properties = rule.properties;
            m_InlineRule.propertyIds = StyleSheetCache.GetPropertyIds(rule);

            ApplyInlineStyles(ve.sharedStyle);
        }

        public void ApplyInlineStyles(ComputedStyle sharedStyle)
        {
            Debug.Assert(!ve.m_Style.isShared);

            // Recreate the computed style in 3 steps
            // 1- Init from shared styles
            ve.m_Style.CopyShared(sharedStyle);

            // 2- Apply inline rule coming from UXML if any
            if (m_InlineRule.sheet != null)
            {
                var parentStyle = ve.hierarchy.parent?.computedStyle;
                s_StylePropertyReader.SetInlineContext(m_InlineRule.sheet, m_InlineRule.properties, m_InlineRule.propertyIds);
                ve.m_Style.ApplyProperties(s_StylePropertyReader, parentStyle);
            }

            // 3- Apply values coming from IStyle if any
            foreach (var sv in m_Values)
            {
                ApplyStyleValue(sv);
            }

            if (ve.style.cursor.keyword != StyleKeyword.Null)
            {
                ve.computedStyle.ApplyStyleCursor(ve.style.cursor.value);
            }
        }

        StyleCursor IStyle.cursor
        {
            get
            {
                var inlineCursor = new StyleCursor();
                if (TryGetInlineCursor(ref inlineCursor))
                    return inlineCursor;
                return StyleKeyword.Null;
            }
            set
            {
                if (SetInlineCursor(value, ve.sharedStyle.cursor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles);
                }
            }
        }

        StyleTextShadow IStyle.textShadow
        {
            get
            {
                var inlineTextShadow = new StyleTextShadow();
                if (TryGetInlineTextShadow(ref inlineTextShadow))
                    return inlineTextShadow;
                return StyleKeyword.Null;
            }
            set
            {
                if (SetInlineTextShadow(value, ve.sharedStyle.textShadow))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        private bool SetStyleValue(StylePropertyId id, StyleLength inlineValue, Length sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.length == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.length = inlineValue.ToLength();

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                sv.length = sharedValue;
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleFloat inlineValue, float sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.number == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = inlineValue.value;

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                sv.number = sharedValue;
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleInt inlineValue, int sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.number == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = inlineValue.value;

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                sv.number = sharedValue;
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleColor inlineValue, Color sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.color == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.color = inlineValue.value;

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                sv.color = sharedValue;
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue<T>(StylePropertyId id, StyleEnum<T> inlineValue, T sharedValue) where T : struct, IConvertible
        {
            var sv = new StyleValue();
            int intValue = UnsafeUtility.EnumToInt(inlineValue.value);
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.number == intValue && sv.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = intValue;

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                sv.number = UnsafeUtility.EnumToInt(sharedValue);
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleBackground inlineValue, Background sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                var vectorImage = sv.resource.IsAllocated ? sv.resource.Target as VectorImage : null;
                var sprite = sv.resource.IsAllocated ? sv.resource.Target as Sprite : null;
                var texture = sv.resource.IsAllocated ? sv.resource.Target as Texture2D : null;
                var renderTexture = sv.resource.IsAllocated ? sv.resource.Target as RenderTexture : null;
                if ((vectorImage == inlineValue.value.vectorImage &&
                     texture == inlineValue.value.texture &&
                     sprite == inlineValue.value.sprite &&
                     renderTexture == inlineValue.value.renderTexture) && sv.keyword == inlineValue.keyword)
                    return false;

                if (sv.resource.IsAllocated)
                    sv.resource.Free();
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            if (inlineValue.value.vectorImage != null)
                sv.resource = GCHandle.Alloc(inlineValue.value.vectorImage);
            else if (inlineValue.value.sprite != null)
                sv.resource = GCHandle.Alloc(inlineValue.value.sprite);
            else if (inlineValue.value.texture != null)
                sv.resource = GCHandle.Alloc(inlineValue.value.texture);
            else if (inlineValue.value.renderTexture != null)
                sv.resource = GCHandle.Alloc(inlineValue.value.renderTexture);
            else
                sv.resource = new GCHandle();

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                if (sharedValue.texture != null)
                    sv.resource = GCHandle.Alloc(sharedValue.texture);
                else if (sharedValue.sprite != null)
                    sv.resource = GCHandle.Alloc(sharedValue.sprite);
                else if (sharedValue.renderTexture != null)
                    sv.resource = GCHandle.Alloc(sharedValue.renderTexture);
                else if (sharedValue.vectorImage != null)
                    sv.resource = GCHandle.Alloc(sharedValue.vectorImage);
                else
                    sv.resource = new GCHandle();
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleFontDefinition inlineValue, FontDefinition sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                var font = sv.resource.IsAllocated ? sv.resource.Target as Font : null;
                var obj = sv.resource.Target as Object;
                var fontAsset = sv.resource.IsAllocated && TextDelegates.IsFontAssetSafe(obj) ? obj : null;
                if ((font == inlineValue.value.font && fontAsset == inlineValue.value.fontAsset) && sv.keyword == inlineValue.keyword)
                    return false;

                if (sv.resource.IsAllocated)
                    sv.resource.Free();
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            if (inlineValue.value.font != null)
                sv.resource = GCHandle.Alloc(inlineValue.value.font);
            else if (inlineValue.value.fontAsset != null)
                sv.resource = GCHandle.Alloc(inlineValue.value.fontAsset);
            else
                sv.resource = new GCHandle();

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                if (sharedValue.font != null)
                    sv.resource = GCHandle.Alloc(sharedValue.font);
                else if (sharedValue.fontAsset != null)
                    sv.resource = GCHandle.Alloc(sharedValue.fontAsset);
                else
                    sv.resource = new GCHandle();
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleFont inlineValue, Font sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.resource.IsAllocated)
                {
                    var font = sv.resource.IsAllocated ? sv.resource.Target as Font : null;
                    if (font == inlineValue.value && sv.keyword == inlineValue.keyword)
                        return false;

                    if (sv.resource.IsAllocated)
                        sv.resource.Free();
                }
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.resource = inlineValue.value != null ? GCHandle.Alloc(inlineValue.value) : new GCHandle();

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                sv.resource = sharedValue != null ? GCHandle.Alloc(sharedValue) : new GCHandle();
            }

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetInlineCursor(StyleCursor inlineValue, Cursor sharedValue)
        {
            var styleCursor = new StyleCursor();
            if (TryGetInlineCursor(ref styleCursor))
            {
                if (styleCursor.value == inlineValue.value && styleCursor.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            styleCursor.value = inlineValue.value;
            styleCursor.keyword = inlineValue.keyword;

            SetInlineCursor(styleCursor);

            if (styleCursor.keyword == StyleKeyword.Null)
            {
                styleCursor.value = sharedValue;
            }

            ve.computedStyle.ApplyStyleCursor(styleCursor.value);
            return true;
        }

        private bool SetInlineTextShadow(StyleTextShadow inlineValue, StyleTextShadow sharedValue)
        {
            var styleTextShadow = new StyleTextShadow();
            if (TryGetInlineTextShadow(ref styleTextShadow))
            {
                if (styleTextShadow.value == inlineValue.value && styleTextShadow.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            styleTextShadow.value = inlineValue.value;
            styleTextShadow.keyword = inlineValue.keyword;

            SetInlineTextShadow(styleTextShadow);

            if (styleTextShadow.keyword == StyleKeyword.Null)
            {
                styleTextShadow.keyword = sharedValue.keyword;
                styleTextShadow.value = sharedValue.value;
            }

            ve.computedStyle.ApplyStyleTextShadow(styleTextShadow.value);
            return true;
        }

        private void ApplyStyleValue(StyleValue value)
        {
            var parentStyle = ve.hierarchy.parent?.computedStyle;
            ve.computedStyle.ApplyStyleValue(value, parentStyle);
        }

        public bool TryGetInlineCursor(ref StyleCursor value)
        {
            if (m_HasInlineCursor)
            {
                value = m_InlineCursor;
                return true;
            }
            return false;
        }

        public void SetInlineCursor(StyleCursor value)
        {
            m_InlineCursor = value;
            m_HasInlineCursor = true;
        }

        public bool TryGetInlineTextShadow(ref StyleTextShadow value)
        {
            if (m_HasInlineTextShadow)
            {
                value = m_InlineTextShadow;
                return true;
            }
            return false;
        }

        public void SetInlineTextShadow(StyleTextShadow value)
        {
            m_InlineTextShadow = value;
            m_HasInlineTextShadow = true;
        }
    }
}
