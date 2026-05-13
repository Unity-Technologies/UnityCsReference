// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    partial class InlineStyleAccessPropertyBag : PropertyBag<InlineStyleAccess>, INamedProperties<InlineStyleAccess>
    {
        readonly List<IProperty<InlineStyleAccess>> m_PropertiesList;

        readonly Dictionary<string, IProperty<InlineStyleAccess>> m_PropertiesHash;

        internal interface IStyleProperty : IProperty<InlineStyleAccess>
        {
            string ussName { get; }
        }

        abstract class InlineStyleProperty<TStyleValue> : Property<InlineStyleAccess, TStyleValue>, IStyleProperty
        {
            public abstract string ussName { get; }
        }

        abstract class InlineStyleEnumProperty<TValue> : InlineStyleProperty<StyleEnum<TValue>>
            where TValue : struct, IConvertible
        {
        }

        abstract class InlineStyleColorProperty : InlineStyleProperty<StyleColor>
        {
        }

        abstract class InlineStyleRatioProperty : InlineStyleProperty<StyleRatio>
        {
        }

        abstract class InlineStyleBackgroundProperty : InlineStyleProperty<StyleBackground>
        {
        }

        abstract class InlineStyleLengthProperty : InlineStyleProperty<StyleLength>
        {
        }

        abstract class InlineStyleFloatProperty : InlineStyleProperty<StyleFloat>
        {
        }

        abstract class InlineStyleListProperty<T> : InlineStyleProperty<StyleList<T>>
        {
        }

        abstract class InlineStyleFontProperty : InlineStyleProperty<StyleFont>
        {
        }

        abstract class InlineStyleFontDefinitionProperty : InlineStyleProperty<StyleFontDefinition>
        {
        }

        abstract class InlineStyleIntProperty : InlineStyleProperty<StyleInt>
        {
        }

        abstract class InlineStyleRotateProperty : InlineStyleProperty<StyleRotate>
        {
        }

        abstract class InlineStyleScaleProperty : InlineStyleProperty<StyleScale>
        {
        }

        abstract class InlineStyleCursorProperty : InlineStyleProperty<StyleCursor>
        {
        }

        abstract class InlineStyleTextShadowProperty : InlineStyleProperty<StyleTextShadow>
        {
        }

        abstract class InlineStyleTextAutoSizeProperty: InlineStyleProperty<StyleTextAutoSize>
        {
        }

        abstract class InlineStyleTransformOriginProperty : InlineStyleProperty<StyleTransformOrigin>
        {
        }

        abstract class InlineStyleTranslateProperty : InlineStyleProperty<StyleTranslate>
        {
        }

        abstract class InlineStyleBackgroundPositionProperty : InlineStyleProperty<StyleBackgroundPosition>
        {
        }

        abstract class InlineStyleBackgroundRepeatProperty : InlineStyleProperty<StyleBackgroundRepeat>
        {
        }

        abstract class InlineStyleBackgroundSizeProperty : InlineStyleProperty<StyleBackgroundSize>
        {
        }

        abstract class InlineStyleMaterialDefinitionProperty : InlineStyleProperty<StyleMaterialDefinition>
        {
        }

        void AddPropertyRange(params IStyleProperty[] properties)
        {
            foreach (var property in properties)
            {
                m_PropertiesList.Add(property);
                m_PropertiesHash.Add(property.Name, property);
                if (string.CompareOrdinal(property.Name, property.ussName) != 0)
                    m_PropertiesHash.Add(property.ussName, property);
            }
        }

        abstract class InlineStyleUIAnimationClipProperty : InlineStyleProperty<StyleUIAnimationClip>
        {
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
