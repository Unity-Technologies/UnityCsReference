// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    partial class ResolvedStyleAccessPropertyBag : PropertyBag<ResolvedStyleAccess>, INamedProperties<ResolvedStyleAccess>
    {
        readonly List<IProperty<ResolvedStyleAccess>> m_PropertiesList;

        readonly Dictionary<string, IProperty<ResolvedStyleAccess>> m_PropertiesHash;

        abstract class ResolvedStyleProperty<TValue> : Property<ResolvedStyleAccess, TValue>
        {
            public abstract string ussName { get; }
        }

        abstract class ResolvedEnumProperty<TValue> : ResolvedStyleProperty<TValue>
            where TValue : struct, IConvertible
        {
        }

        abstract class ResolvedColorProperty : ResolvedStyleProperty<Color>
        {
        }

        abstract class ResolvedBackgroundProperty : ResolvedStyleProperty<Background>
        {
        }

        abstract class ResolvedFloatProperty : ResolvedStyleProperty<float>
        {
        }

        abstract class ResolvedStyleFloatProperty : ResolvedStyleProperty<StyleFloat>
        {
        }

        abstract class ResolvedListProperty<T> : ResolvedStyleProperty<IEnumerable<T>>
        {
        }

        abstract class ResolvedFixedList4Property<T> : ResolvedStyleProperty<IEnumerable<T>>
        {
        }

        abstract class ResolvedFontProperty : ResolvedStyleProperty<Font>
        {
        }

        abstract class ResolvedFontDefinitionProperty : ResolvedStyleProperty<FontDefinition>
        {
        }

        abstract class ResolvedIntProperty : ResolvedStyleProperty<int>
        {
        }

        abstract class ResolvedRotateProperty : ResolvedStyleProperty<Rotate>
        {
        }

        abstract class ResolvedScaleProperty : ResolvedStyleProperty<Scale>
        {
        }

        abstract class ResolvedVector3Property : ResolvedStyleProperty<Vector3>
        {
        }

        abstract class ResolvedBackgroundPositionProperty : ResolvedStyleProperty<BackgroundPosition>
        {
        }

        abstract class ResolvedBackgroundRepeatProperty : ResolvedStyleProperty<BackgroundRepeat>
        {
        }

        abstract class ResolvedBackgroundSizeProperty : ResolvedStyleProperty<BackgroundSize>
        {
        }

        abstract class ResolvedMaterialProperty : ResolvedStyleProperty<Material>
        {
        }

        void AddProperty<TValue>(ResolvedStyleProperty<TValue> property)
        {
            m_PropertiesList.Add(property);
            m_PropertiesHash.Add(property.Name, property);
            if (string.CompareOrdinal(property.Name, property.ussName) != 0)
                m_PropertiesHash.Add(property.ussName, property);
        }

        public override PropertyCollection<ResolvedStyleAccess> GetProperties()
            => new PropertyCollection<ResolvedStyleAccess>(m_PropertiesList);

        public override PropertyCollection<ResolvedStyleAccess> GetProperties(ref ResolvedStyleAccess container)
            => new PropertyCollection<ResolvedStyleAccess>(m_PropertiesList);

        public bool TryGetProperty(ref ResolvedStyleAccess container, string name, out IProperty<ResolvedStyleAccess> property)
            => m_PropertiesHash.TryGetValue(name, out property);
    }
}
