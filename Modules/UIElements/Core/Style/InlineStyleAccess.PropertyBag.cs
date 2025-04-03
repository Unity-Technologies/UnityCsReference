// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    partial class InlineStyleAccessPropertyBag : PropertyBag<InlineStyleAccess>, INamedProperties<InlineStyleAccess>
    {
        readonly List<IProperty<InlineStyleAccess>> m_PropertiesList;

        readonly Dictionary<string, IProperty<InlineStyleAccess>> m_PropertiesHash;

        abstract class InlineStyleProperty<TStyleValue, TValue> : Property<InlineStyleAccess, TStyleValue>
            where TStyleValue : IStyleValue<TValue>, new()
        {
            protected InlineStyleProperty()
            {
                // Defines basic conversions between value, keyword and StyleValue
                ConverterGroups.RegisterGlobal((ref TStyleValue sv) => sv.value);
                ConverterGroups.RegisterGlobal((ref TValue v) => new TStyleValue {value = v});
                ConverterGroups.RegisterGlobal((ref TStyleValue sv) => sv.keyword);
                ConverterGroups.RegisterGlobal((ref StyleKeyword kw) => new TStyleValue {keyword = kw});
            }

            public abstract string ussName { get; }
        }

        abstract class InlineStyleEnumProperty<TValue> : InlineStyleProperty<StyleEnum<TValue>, TValue>
            where TValue : struct, IConvertible
        {
        }

        abstract class InlineStyleColorProperty : InlineStyleProperty<StyleColor, Color>
        {
            protected InlineStyleColorProperty()
            {
                ConverterGroups.RegisterGlobal((ref Color32 v) => new StyleColor(v));
                ConverterGroups.RegisterGlobal((ref StyleColor sv) => (Color32) sv.value);
            }
        }

        abstract class InlineStyleBackgroundProperty : InlineStyleProperty<StyleBackground, Background>
        {
            protected InlineStyleBackgroundProperty()
            {
                ConverterGroups.RegisterGlobal((ref Texture2D v) => new StyleBackground(v));
                ConverterGroups.RegisterGlobal((ref Sprite v) => new StyleBackground(v));
                ConverterGroups.RegisterGlobal((ref VectorImage v) => new StyleBackground(v));
                ConverterGroups.RegisterGlobal((ref StyleBackground sv) => sv.value.texture);
                ConverterGroups.RegisterGlobal((ref StyleBackground sv) => sv.value.sprite);
                ConverterGroups.RegisterGlobal((ref StyleBackground sv) => sv.value.renderTexture);
                ConverterGroups.RegisterGlobal((ref StyleBackground sv) => sv.value.vectorImage);
            }
        }

        abstract class InlineStyleLengthProperty : InlineStyleProperty<StyleLength, Length>
        {
            protected InlineStyleLengthProperty()
            {
                ConverterGroups.RegisterGlobal((ref float v) => new StyleLength(v));
                ConverterGroups.RegisterGlobal((ref int v) => new StyleLength(v));
                ConverterGroups.RegisterGlobal((ref StyleLength sv) => sv.value.value);
                ConverterGroups.RegisterGlobal((ref StyleLength sv) => (int)sv.value.value);
            }
        }

        abstract class InlineStyleFloatProperty : InlineStyleProperty<StyleFloat, float>
        {
            protected InlineStyleFloatProperty()
            {
                ConverterGroups.RegisterGlobal((ref int v) => new StyleFloat(v));
                ConverterGroups.RegisterGlobal((ref StyleFloat sv) => (int)sv.value);
            }
        }

        abstract class InlineStyleListProperty<T> : InlineStyleProperty<StyleList<T>, List<T>>
        {
        }

        abstract class InlineStyleFontProperty : InlineStyleProperty<StyleFont, Font>
        {
        }

        abstract class InlineStyleFontDefinitionProperty : InlineStyleProperty<StyleFontDefinition, FontDefinition>
        {
            protected InlineStyleFontDefinitionProperty()
            {
                ConverterGroups.RegisterGlobal((ref Font v) => new StyleFontDefinition(v));
                ConverterGroups.RegisterGlobal((ref FontAsset v) => new StyleFontDefinition(v));
                ConverterGroups.RegisterGlobal((ref StyleFontDefinition sv) => sv.value.font);
                ConverterGroups.RegisterGlobal((ref StyleFontDefinition sv) => sv.value.fontAsset);
            }
        }

        abstract class InlineStyleIntProperty : InlineStyleProperty<StyleInt, int>
        {
        }

        abstract class InlineStyleRotateProperty : InlineStyleProperty<StyleRotate, Rotate>
        {
        }

        abstract class InlineStyleScaleProperty : InlineStyleProperty<StyleScale, Scale>
        {
        }

        abstract class InlineStyleCursorProperty : InlineStyleProperty<StyleCursor, Cursor>
        {
        }

        abstract class InlineStyleTextShadowProperty : InlineStyleProperty<StyleTextShadow, TextShadow>
        {
        }

        abstract class InlineStyleTransformOriginProperty : InlineStyleProperty<StyleTransformOrigin, TransformOrigin>
        {
        }

        abstract class InlineStyleTranslateProperty : InlineStyleProperty<StyleTranslate, Translate>
        {
        }

        abstract class InlineStyleBackgroundPositionProperty : InlineStyleProperty<StyleBackgroundPosition, BackgroundPosition>
        {
        }

        abstract class InlineStyleBackgroundRepeatProperty : InlineStyleProperty<StyleBackgroundRepeat, BackgroundRepeat>
        {
        }

        abstract class InlineStyleBackgroundSizeProperty : InlineStyleProperty<StyleBackgroundSize, BackgroundSize>
        {
        }


        void AddProperty<TStyleValue, TValue>(InlineStyleProperty<TStyleValue, TValue> property)
            where TStyleValue : IStyleValue<TValue>, new()
        {
            m_PropertiesList.Add(property);
            m_PropertiesHash.Add(property.Name, property);
            if (string.CompareOrdinal(property.Name, property.ussName) != 0)
                m_PropertiesHash.Add(property.ussName, property);
        }

        public override PropertyCollection<InlineStyleAccess> GetProperties()
            => new PropertyCollection<InlineStyleAccess>(m_PropertiesList);

        public override PropertyCollection<InlineStyleAccess> GetProperties(ref InlineStyleAccess container)
            => new PropertyCollection<InlineStyleAccess>(m_PropertiesList);

        public bool TryGetProperty(ref InlineStyleAccess container, string name,
            out IProperty<InlineStyleAccess> property)
            => m_PropertiesHash.TryGetValue(name, out property);
    }
}
