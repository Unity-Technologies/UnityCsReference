// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Properties;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.StyleSheets;

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

        public StyleBackgroundPosition GetStyleBackgroundPosition(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleBackgroundPosition(inline.position);
            return StyleKeyword.Null;
        }

        public StyleBackgroundRepeat GetStyleBackgroundRepeat(StylePropertyId id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleBackgroundRepeat(inline.repeat);
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
        static InlineStyleAccess()
        {
            PropertyBag.Register(new InlineStyleAccessPropertyBag());
        }

        private static StylePropertyReader s_StylePropertyReader = new StylePropertyReader();

        private List<StyleValueManaged> m_ValuesManaged;
        private VisualElement ve { get; set; }

        private bool m_HasInlineCursor;
        private StyleCursor m_InlineCursor;

        private bool m_HasInlineTextShadow;
        private StyleTextShadow m_InlineTextShadow;

        private bool m_HasInlineTransformOrigin;
        private StyleTransformOrigin m_InlineTransformOrigin;

        private bool m_HasInlineTranslate;
        private StyleTranslate m_InlineTranslateOperation;

        private bool m_HasInlineRotate;
        private StyleRotate m_InlineRotateOperation;

        private bool m_HasInlineScale;
        private StyleScale m_InlineScale;

        private bool m_HasInlineBackgroundSize;
        public StyleBackgroundSize m_InlineBackgroundSize;

        private InlineRule m_InlineRule;
        public InlineRule inlineRule => m_InlineRule;

        internal struct InlineRule
        {
            public StyleSheet sheet;
            public StyleRule rule;
            public StyleProperty[] properties => rule.properties;
            public StylePropertyId[] propertyIds;
        }

        public InlineStyleAccess(VisualElement ve)
        {
            this.ve = ve;
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
            m_InlineRule.rule = rule;
            m_InlineRule.propertyIds = StyleSheetCache.GetPropertyIds(rule);

            ApplyInlineStyles(ref ve.computedStyle);
        }

        public bool IsValueSet(StylePropertyId id)
        {
            foreach (var sv in m_Values)
            {
                if (sv.id == id)
                    return true;
            }

            if (m_ValuesManaged != null)
            {
                foreach (var sv in m_ValuesManaged)
                {
                    if (sv.id == id)
                        return true;
                }
            }

            switch (id)
            {
                case StylePropertyId.Cursor:
                    return m_HasInlineCursor;
                case StylePropertyId.TextShadow:
                    return m_HasInlineTextShadow;
                case StylePropertyId.TransformOrigin:
                    return m_HasInlineTransformOrigin;
                case StylePropertyId.Translate:
                    return m_HasInlineTranslate;
                case StylePropertyId.Rotate:
                    return m_HasInlineRotate;
                case StylePropertyId.Scale:
                    return m_HasInlineScale;
                case StylePropertyId.BackgroundSize:
                    return m_HasInlineBackgroundSize;
                default:
                    return false;
            }
        }

        public void ApplyInlineStyles(ref ComputedStyle computedStyle)
        {
            // Apply inline rule coming from UXML if any
            var parent = ve.hierarchy.parent;
            ref var parentStyle = ref parent?.computedStyle != null ? ref parent.computedStyle : ref InitialStyle.Get();

            if (m_InlineRule.sheet != null)
            {
                s_StylePropertyReader.SetInlineContext(m_InlineRule.sheet, m_InlineRule.rule.properties, m_InlineRule.propertyIds);
                computedStyle.ApplyProperties(s_StylePropertyReader, ref parentStyle);
            }

            // Apply values coming from IStyle if any
            foreach (var sv in m_Values)
            {
                computedStyle.ApplyStyleValue(sv, ref parentStyle);
            }

            if (m_ValuesManaged != null)
            {
                foreach (var sv in m_ValuesManaged)
                {
                    computedStyle.ApplyStyleValueManaged(sv, ref parentStyle);
                }
            }

            if (ve.style.cursor.keyword != StyleKeyword.Null)
            {
                computedStyle.ApplyStyleCursor(ve.style.cursor.value);
            }

            if (ve.style.textShadow.keyword != StyleKeyword.Null)
            {
                computedStyle.ApplyStyleTextShadow(ve.style.textShadow.value);
            }

            if (m_HasInlineTransformOrigin)
            {
                computedStyle.ApplyStyleTransformOrigin(ve.style.transformOrigin.value);
            }

            if (m_HasInlineTranslate)
            {
                computedStyle.ApplyStyleTranslate(ve.style.translate.value);
            }

            if (m_HasInlineScale)
            {
                computedStyle.ApplyStyleScale(ve.style.scale.value);
            }

            if (m_HasInlineRotate)
            {
                computedStyle.ApplyStyleRotate(ve.style.rotate.value);
            }

            if (m_HasInlineBackgroundSize)
            {
                computedStyle.ApplyStyleBackgroundSize(ve.style.backgroundSize.value);
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
                if (SetInlineCursor(value))
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
                if (SetInlineTextShadow(value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Repaint);
                }
            }
        }

        StyleBackgroundSize IStyle.backgroundSize
        {
            get
            {
                var inlineBackgroundSize = new StyleBackgroundSize();
                if (TryGetInlineBackgroundSize(ref inlineBackgroundSize))
                    return inlineBackgroundSize;
                return StyleKeyword.Null;
            }
            set
            {
                if (SetInlineBackgroundSize(value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        private StyleList<T> GetStyleList<T>(StylePropertyId id)
        {
            var inline = new StyleValueManaged();
            if (TryGetStyleValueManaged(id, ref inline))
            {
                return new StyleList<T>(inline.value as List<T>, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        private void SetStyleValueManaged(StyleValueManaged value)
        {
            if (m_ValuesManaged == null)
                m_ValuesManaged = new List<StyleValueManaged>();

            for (int i = 0; i < m_ValuesManaged.Count; i++)
            {
                if (m_ValuesManaged[i].id == value.id)
                {
                    if (value.keyword == StyleKeyword.Null)
                    {
                        m_ValuesManaged.RemoveAt(i);
                    }
                    else
                    {
                        m_ValuesManaged[i] = value;
                    }
                    return;
                }
            }

            m_ValuesManaged.Add(value);
        }

        private bool TryGetStyleValueManaged(StylePropertyId id, ref StyleValueManaged value)
        {
            value.id = StylePropertyId.Unknown;
            if (m_ValuesManaged == null)
                return false;

            foreach (var inlineStyle in m_ValuesManaged)
            {
                if (inlineStyle.id == id)
                {
                    value = inlineStyle;
                    return true;
                }
            }
            return false;
        }

        StyleTransformOrigin IStyle.transformOrigin
        {
            get
            {
                var inlineTransformOrigin = new StyleTransformOrigin();
                if (TryGetInlineTransformOrigin(ref inlineTransformOrigin))
                    return inlineTransformOrigin;
                return StyleKeyword.Null;
            }
            set
            {
                if (SetInlineTransformOrigin(value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Transform);
                }
            }
        }

        StyleTranslate IStyle.translate
        {
            get
            {
                var inlineTranslate = new StyleTranslate();
                if (TryGetInlineTranslate(ref inlineTranslate))
                    return inlineTranslate;
                return StyleKeyword.Null;
            }
            set
            {
                if (SetInlineTranslate(value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Transform);
                }
            }
        }

        StyleRotate IStyle.rotate
        {
            get
            {
                var inlineRotate = new StyleRotate();
                if (TryGetInlineRotate(ref inlineRotate))
                    return inlineRotate;
                return StyleKeyword.Null;
            }
            set
            {
                if (SetInlineRotate(value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Transform);
                }
            }
        }

        StyleScale IStyle.scale
        {
            get
            {
                var inlineScale = new StyleScale();
                if (TryGetInlineScale(ref inlineScale))
                    return inlineScale;
                return StyleKeyword.Null;
            }
            set
            {
                // The layout need to be regenerated because the TextNative requires the scale to mesure it's size to be pixel perfect.
                if (SetInlineScale(value))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Transform);
                }
            }
        }

        private bool SetStyleValue(StylePropertyId id, StyleBackgroundPosition inlineValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.position == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.position = inlineValue.value;

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleBackgroundRepeat inlineValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.repeat == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.repeat = inlineValue.value;

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleLength inlineValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.length == inlineValue.ToLength() && sv.keyword == inlineValue.keyword)
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
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleFloat inlineValue)
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
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleInt inlineValue)
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
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleColor inlineValue)
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
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue<T>(StylePropertyId id, StyleEnum<T> inlineValue) where T : struct, IConvertible
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
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleBackground inlineValue)
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
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleFontDefinition inlineValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                var font = sv.resource.IsAllocated ? sv.resource.Target as Font : null;
                var fontAsset = sv.resource.IsAllocated ? sv.resource.Target as FontAsset : null;
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
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue(StylePropertyId id, StyleFont inlineValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                var font = sv.resource.IsAllocated ? sv.resource.Target as Font : null;
                if (font == inlineValue.value && sv.keyword == inlineValue.keyword)
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
            sv.resource = inlineValue.value != null ? GCHandle.Alloc(inlineValue.value) : new GCHandle();

            SetStyleValue(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetStyleValue<T>(StylePropertyId id, StyleList<T> inlineValue)
        {
            var sv = new StyleValueManaged();
            if (TryGetStyleValueManaged(id, ref sv))
            {
                if (sv.keyword == inlineValue.keyword)
                {
                    if (sv.value == null && inlineValue.value == null)
                        return false;

                    if (sv.value is List<T> list && inlineValue.value != null && list.SequenceEqual(inlineValue.value))
                        return false;
                }
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            if (inlineValue.value != null)
            {
                if (sv.value == null)
                {
                    sv.value = new List<T>(inlineValue.value);
                }
                else
                {
                    var list = (List<T>)sv.value;
                    list.Clear();
                    list.AddRange(inlineValue.value);
                }
            }
            else
            {
                sv.value = null;
            }

            SetStyleValueManaged(sv);

            if (inlineValue.keyword == StyleKeyword.Null)
                return RemoveInlineStyle(id);

            ApplyStyleValue(sv);
            return true;
        }

        private bool SetInlineCursor(StyleCursor inlineValue)
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

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                m_HasInlineCursor = false;
                return RemoveInlineStyle(StylePropertyId.Cursor);
            }

            m_InlineCursor = styleCursor;
            m_HasInlineCursor = true;
            ApplyStyleCursor(styleCursor);

            return true;
        }

        private void ApplyStyleCursor(StyleCursor cursor)
        {
            ve.computedStyle.ApplyStyleCursor(cursor.value);

            if (ve.elementPanel?.GetTopElementUnderPointer(PointerId.mousePointerId) == ve)
                ve.elementPanel.cursorManager.SetCursor(cursor.value);
        }

        private bool SetInlineTextShadow(StyleTextShadow inlineValue)
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

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                m_HasInlineTextShadow = false;
                return RemoveInlineStyle(StylePropertyId.TextShadow);
            }

            m_InlineTextShadow = styleTextShadow;
            m_HasInlineTextShadow = true;
            ApplyStyleTextShadow(styleTextShadow);

            return true;
        }

        private void ApplyStyleTextShadow(StyleTextShadow textShadow)
        {
            ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

            bool startedTransition = false;
            if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                ve.computedStyle.GetTransitionProperty(StylePropertyId.TextShadow, out var t))
            {
                startedTransition = ComputedStyle.StartAnimationInlineTextShadow(ve, ref ve.computedStyle,
                    textShadow, t.durationMs, t.delayMs, t.easingCurve);
            }
            else
            {
                // In case there were older animations running, cancel them.
                ve.styleAnimation.CancelAnimation(StylePropertyId.TextShadow);
            }

            if (!startedTransition)
            {
                ve.computedStyle.ApplyStyleTextShadow(textShadow.value);
            }
        }

        private bool SetInlineTransformOrigin(StyleTransformOrigin inlineValue)
        {
            var styleTransformOrigin = new StyleTransformOrigin();
            if (TryGetInlineTransformOrigin(ref styleTransformOrigin))
            {
                if (styleTransformOrigin.value == inlineValue.value && styleTransformOrigin.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }


            if (inlineValue.keyword == StyleKeyword.Null)
            {
                m_HasInlineTransformOrigin = false;
                return RemoveInlineStyle(StylePropertyId.TransformOrigin);
            }

            m_InlineTransformOrigin = inlineValue;
            m_HasInlineTransformOrigin = true;
            ApplyStyleTransformOrigin(inlineValue);

            return true;
        }

        private void ApplyStyleTransformOrigin(StyleTransformOrigin transformOrigin)
        {
            ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

            bool startedTransition = false;
            if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                ve.computedStyle.GetTransitionProperty(StylePropertyId.TransformOrigin, out var t))
            {
                startedTransition = ComputedStyle.StartAnimationInlineTransformOrigin(ve, ref ve.computedStyle,
                    transformOrigin, t.durationMs, t.delayMs, t.easingCurve);
            }
            else
            {
                // In case there were older animations running, cancel them.
                ve.styleAnimation.CancelAnimation(StylePropertyId.TransformOrigin);
            }


            if (!startedTransition)
            {
                ve.computedStyle.ApplyStyleTransformOrigin(transformOrigin.value);
            }
        }

        private bool SetInlineTranslate(StyleTranslate inlineValue)
        {
            var styleTranslate = new StyleTranslate();
            if (TryGetInlineTranslate(ref styleTranslate))
            {
                if (styleTranslate.value == inlineValue.value && styleTranslate.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }


            if (inlineValue.keyword == StyleKeyword.Null)
            {
                m_HasInlineTranslate = false;
                return RemoveInlineStyle(StylePropertyId.Translate);
            }

            m_InlineTranslateOperation = inlineValue;
            m_HasInlineTranslate = true;
            ApplyStyleTranslate(inlineValue);

            return true;
        }

        private void ApplyStyleTranslate(StyleTranslate translate)
        {
            ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

            bool startedTransition = false;
            if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                ve.computedStyle.GetTransitionProperty(StylePropertyId.Translate, out var t))
            {
                startedTransition = ComputedStyle.StartAnimationInlineTranslate(ve, ref ve.computedStyle,
                    translate, t.durationMs, t.delayMs, t.easingCurve);
            }
            else
            {
                // In case there were older animations running, cancel them.
                ve.styleAnimation.CancelAnimation(StylePropertyId.Translate);
            }

            if (!startedTransition)
            {
                ve.computedStyle.ApplyStyleTranslate(translate.value);
            }
        }

        private bool SetInlineScale(StyleScale inlineValue)
        {
            var styleScale = new StyleScale();
            if (TryGetInlineScale(ref styleScale))
            {
                if (styleScale.value == inlineValue.value && styleScale.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            if (inlineValue.keyword == StyleKeyword.Null)
            {

                m_HasInlineScale = false;
                return RemoveInlineStyle(StylePropertyId.Scale);
            }

            m_InlineScale = inlineValue;
            m_HasInlineScale = true;
            ApplyStyleScale(inlineValue);

            return true;
        }

        private void ApplyStyleScale(StyleScale scale)
        {
            ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

            bool startedTransition = false;
            if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                ve.computedStyle.GetTransitionProperty(StylePropertyId.Scale, out var t))
            {
                startedTransition = ComputedStyle.StartAnimationInlineScale(ve, ref ve.computedStyle,
                    scale, t.durationMs, t.delayMs, t.easingCurve);
            }
            else
            {
                // In case there were older animations running, cancel them.
                ve.styleAnimation.CancelAnimation(StylePropertyId.Scale);
            }


            if (!startedTransition)
            {
                ve.computedStyle.ApplyStyleScale(scale.value);
            }
        }

        private bool SetInlineRotate(StyleRotate inlineValue)
        {
            var styleRotate = new StyleRotate();
            if (TryGetInlineRotate(ref styleRotate))
            {
                if (styleRotate.value == inlineValue.value && styleRotate.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }


            if (inlineValue.keyword == StyleKeyword.Null)
            {
                m_HasInlineRotate = false;
                return RemoveInlineStyle(StylePropertyId.Rotate);
            }

            m_InlineRotateOperation = inlineValue;
            m_HasInlineRotate = true;
            ApplyStyleRotate(inlineValue);

            return true;
        }

        private void ApplyStyleRotate(StyleRotate rotate)
        {
            var parent = ve.hierarchy.parent;
            ref var parentStyle = ref parent?.computedStyle != null ? ref parent.computedStyle : ref InitialStyle.Get();
            ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

            bool startedTransition = false;
            if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                ve.computedStyle.GetTransitionProperty(StylePropertyId.Rotate, out var t))
            {
                startedTransition = ComputedStyle.StartAnimationInlineRotate(ve, ref ve.computedStyle,
                    rotate, t.durationMs, t.delayMs, t.easingCurve);
            }
            else
            {
                // In case there were older animations running, cancel them.
                ve.styleAnimation.CancelAnimation(StylePropertyId.Rotate);
            }


            if (!startedTransition)
            {
                ve.computedStyle.ApplyStyleRotate(rotate.value);
            }
        }

        private bool SetInlineBackgroundSize(StyleBackgroundSize inlineValue)
        {
            var styleBackgroundSize = new StyleBackgroundSize();
            if (TryGetInlineBackgroundSize(ref styleBackgroundSize))
            {
                if (styleBackgroundSize.value == inlineValue.value && styleBackgroundSize.keyword == inlineValue.keyword)
                    return false;
            }
            else if (inlineValue.keyword == StyleKeyword.Null)
            {
                return false;
            }

            if (inlineValue.keyword == StyleKeyword.Null)
            {
                m_HasInlineBackgroundSize = false;
                return RemoveInlineStyle(StylePropertyId.BackgroundSize);
            }

            m_InlineBackgroundSize = inlineValue;
            m_HasInlineBackgroundSize = true;
            ApplyStyleBackgroundSize(inlineValue);

            return true;
        }

        private void ApplyStyleBackgroundSize(StyleBackgroundSize backgroundSize)
        {
            ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

            bool startedTransition = false;
            if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                ve.computedStyle.GetTransitionProperty(StylePropertyId.BackgroundSize, out var t))
            {
                startedTransition = ComputedStyle.StartAnimationInlineBackgroundSize(ve, ref ve.computedStyle,
                    backgroundSize, t.durationMs, t.delayMs, t.easingCurve);
            }
            else
            {
                // In case there were older animations running, cancel them.
                ve.styleAnimation.CancelAnimation(StylePropertyId.TransformOrigin);
            }

            if (!startedTransition)
            {
                ve.computedStyle.ApplyStyleBackgroundSize(backgroundSize.value);
            }
        }

        private void ApplyStyleValue(StyleValue value)
        {
            var parent = ve.hierarchy.parent;
            ref var parentStyle = ref parent?.computedStyle != null ? ref parent.computedStyle : ref InitialStyle.Get();
            bool startedTransition = false;

            if (StylePropertyUtil.IsAnimatable(value.id))
            {
                ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

                if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                    ve.computedStyle.GetTransitionProperty(value.id, out var t))
                {
                    startedTransition = ComputedStyle.StartAnimationInline(ve, value.id, ref ve.computedStyle,
                        value, t.durationMs, t.delayMs, t.easingCurve);
                }
                else
                {
                    // In case there were older animations running, cancel them.
                    ve.styleAnimation.CancelAnimation(value.id);
                }
            }

            if (!startedTransition)
            {
                ve.computedStyle.ApplyStyleValue(value, ref parentStyle);
            }
        }

        private void ApplyStyleValue(StyleValueManaged value)
        {
            // No need to check for transitions because all StyleValueManaged cannot be animated
            var parent = ve.hierarchy.parent;
            ref var parentStyle = ref parent?.computedStyle != null ? ref parent.computedStyle : ref InitialStyle.Get();
            ve.computedStyle.ApplyStyleValueManaged(value, ref parentStyle);
        }

        //return true if another style was applied when removing the inlineStyle, false if notthing was applied
        private bool RemoveInlineStyle(StylePropertyId id)
        {
            var rulesHash = ve.computedStyle.matchingRulesHash;
            if (rulesHash == 0)
            {
                ApplyFromComputedStyle(id, ref InitialStyle.Get());
                return true;
            }

            if (StyleCache.TryGetValue(rulesHash, out var baseComputedStyle))
            {
                ApplyFromComputedStyle(id, ref baseComputedStyle);
                return true;
            }

            return false;
        }

        private void ApplyFromComputedStyle(StylePropertyId id, ref ComputedStyle newStyle)
        {
            bool startedTransition = false;

            if (StylePropertyUtil.IsAnimatable(id))
            {
                ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);

                if (ve.computedStyle.hasTransition && ve.styleInitialized &&
                    ve.computedStyle.GetTransitionProperty(id, out var t))
                {
                    startedTransition = ComputedStyle.StartAnimation(ve, id, ref ve.computedStyle, ref newStyle, t.durationMs, t.delayMs, t.easingCurve);
                }
                else
                {
                    // In case there were older animations running, cancel them.
                    ve.styleAnimation.CancelAnimation(id);
                }
            }

            if (!startedTransition)
            {
                ve.computedStyle.ApplyFromComputedStyle(id, ref newStyle);
            }
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

        public bool TryGetInlineTextShadow(ref StyleTextShadow value)
        {
            if (m_HasInlineTextShadow)
            {
                value = m_InlineTextShadow;
                return true;
            }
            return false;
        }

        public bool TryGetInlineTransformOrigin(ref StyleTransformOrigin value)
        {
            if (m_HasInlineTransformOrigin)
            {
                value = m_InlineTransformOrigin;
                return true;
            }
            return false;
        }

        public bool TryGetInlineTranslate(ref StyleTranslate value)
        {
            if (m_HasInlineTranslate)
            {
                value = m_InlineTranslateOperation;
                return true;
            }
            return false;
        }

        public bool TryGetInlineRotate(ref StyleRotate value)
        {
            if (m_HasInlineRotate)
            {
                value = m_InlineRotateOperation;
                return true;
            }
            return false;
        }

        public bool TryGetInlineScale(ref StyleScale value)
        {
            if (m_HasInlineScale)
            {
                value = m_InlineScale;
                return true;
            }
            return false;
        }

        public bool TryGetInlineBackgroundSize(ref StyleBackgroundSize value)
        {
            if (m_HasInlineBackgroundSize)
            {
                value = m_InlineBackgroundSize;
                return true;
            }
            return false;
        }

        StyleEnum<ScaleMode> IStyle.unityBackgroundScaleMode
        {
            get
            {
                return new StyleEnum<ScaleMode>(BackgroundPropertyHelper.ResolveUnityBackgroundScaleMode(ve.style.backgroundPositionX.value,
                    ve.style.backgroundPositionY.value, ve.style.backgroundRepeat.value, ve.style.backgroundSize.value));
            }

            set
            {
                ve.style.backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(value.value);
                ve.style.backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(value.value);
                ve.style.backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(value.value);
                ve.style.backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(value.value);
            }
        }
    }
}
