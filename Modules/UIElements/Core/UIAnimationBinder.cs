// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.UIElements.StyleSheets;


namespace UnityEngine.UIElements
{
    internal sealed partial class UIAnimationBinder
    {
        internal List<KeyValuePair<string, VisualElement>> m_Elements;
        internal Dictionary<PropertyName, VisualElement> m_ElementsMap;

        internal int GetChannelCount(StylePropertyId id)
        {
            return m_ChannelCount[(int)id];
        }

        internal PropertyType GetPropertyTypeMapping(StylePropertyId id)
        {
            return m_PropertyTypeMapping[(int)id];
        }

        internal void SetFloatValue(int elementIndex, int propertyId, int channel, float value)
        {
            if (elementIndex < 0 || elementIndex >= m_Elements.Count)
                return;

            var e = m_Elements[elementIndex].Value;
            StylePropertyId id = (StylePropertyId)propertyId;

            Debug.Assert(channel < GetChannelCount(id));

            switch (GetPropertyTypeMapping(id))
            {
                case PropertyType.Float:
                    e.computedStyle.ApplyPropertyAnimation(e, id, value);
                    break;

                case PropertyType.Rotate:
                    var r = e.computedStyle.ReadPropertyAnimationRotate(id);
                    r.angle = Angle.Degrees(value);
                    e.computedStyle.ApplyPropertyAnimation(e, id, r);
                    break;

                case PropertyType.Translate:
                {
                    Translate t = e.computedStyle.ReadPropertyAnimationTranslate(id);
                    if (channel == 0)
                        t.x = value;
                    else if (channel == 1)
                        t.y = value;
                    else if (channel == 2)
                        t.z = value;
                    e.computedStyle.ApplyPropertyAnimation(e, id, t);
                }
                break;

                case PropertyType.Scale:
                {
                    var s = e.computedStyle.ReadPropertyAnimationScale(id).value;
                    if (channel == 0)
                        s.x = value;
                    else if (channel == 1)
                        s.y = value;
                    else if (channel == 2)
                        s.z = value;
                    e.computedStyle.ApplyPropertyAnimation(e, id, new Scale(s));
                }
                break;

                case PropertyType.Color:
                    var c = e.computedStyle.ReadPropertyAnimationColor(id);
                    if (channel == 0)
                        c.r = value;
                    else if (channel == 1)
                        c.g = value;
                    else if (channel == 2)
                        c.b = value;
                    else if (channel == 3)
                        c.a = value;
                    e.computedStyle.ApplyPropertyAnimation(e, id, c);
                    break;

                case PropertyType.Ratio:
                    e.computedStyle.ApplyPropertyAnimation(e, id, new Ratio(value));
                    break;

                case PropertyType.Int or PropertyType.Enum:
                    e.computedStyle.ApplyPropertyAnimation(e, id, (int)(value));
                    break;

                case PropertyType.Length:
                    e.computedStyle.ApplyPropertyAnimation(e, id, Length.Pixels(value));
                    break;

            }

        }

        internal float GetFloatValue(int elementIndex, int propertyId, int channel)
        {
            if (elementIndex < 0 || elementIndex >= m_Elements.Count)
                return 0;
            var element = m_Elements[elementIndex].Value;
            StylePropertyId id = (StylePropertyId)propertyId;

            Debug.Assert(channel < GetChannelCount(id));

            return GetPropertyTypeMapping(id) switch
            {
                PropertyType.Length => element.computedStyle.ReadPropertyAnimationLength(id).pixelValue,
                PropertyType.Float => element.computedStyle.ReadPropertyAnimationFloat(id),
                PropertyType.Int => element.computedStyle.ReadPropertyAnimationInt(id),
                PropertyType.Enum => element.computedStyle.ReadPropertyAnimationInt(id),
                PropertyType.Color => element.computedStyle.ReadPropertyAnimationColor(id)[channel],
                PropertyType.Translate => channel switch
                {
                    0 => element.computedStyle.ReadPropertyAnimationTranslate(id).x.pixelValue,
                    1 => element.computedStyle.ReadPropertyAnimationTranslate(id).y.pixelValue,
                    2 => element.computedStyle.ReadPropertyAnimationTranslate(id).z,
                    _ => throw new NotImplementedException(),
                },
                PropertyType.Rotate => element.computedStyle.ReadPropertyAnimationRotate(id).angle.ToDegrees(),
                PropertyType.Scale => element.computedStyle.ReadPropertyAnimationScale(id).value[channel],
                PropertyType.Ratio => element.computedStyle.ReadPropertyAnimationRatio(id),


                //Not implemented
                PropertyType.TransformOrigin => throw new NotImplementedException(),
                PropertyType.Shorthand => throw new NotImplementedException(),
                PropertyType.BackgroundPosition => throw new NotImplementedException(),
                PropertyType.BackgroundRepeat => throw new NotImplementedException(),
                PropertyType.BackgroundSize => throw new NotImplementedException(),
                PropertyType.Background => throw new NotImplementedException(),
                PropertyType.Filter => throw new NotImplementedException(),
                PropertyType.Font => throw new NotImplementedException(),
                PropertyType.FontDefinition => throw new NotImplementedException(),
                PropertyType.Cursor => throw new NotImplementedException(),
                PropertyType.TextShadow => throw new NotImplementedException(),
                PropertyType.TextAutoSize => throw new NotImplementedException(),
                PropertyType.List => throw new NotImplementedException(),
                PropertyType.MaterialDefinition => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),// Why does c# think the list is not exaustive? seems related to the cast...
            };


        }

        internal void SetObjectValue(int elementIndex, int propertyId, int channel, EntityId value)
        {
            if (elementIndex < 0 || elementIndex >= m_Elements.Count)
                return;

            var e = m_Elements[elementIndex].Value;
            StylePropertyId id = (StylePropertyId)propertyId;

            Debug.Assert(channel < GetChannelCount(id));

            switch (GetPropertyTypeMapping(id))
            {
                case PropertyType.Background:
                case PropertyType.Font:
                case PropertyType.FontDefinition:
                case PropertyType.MaterialDefinition:
                    e.computedStyle.ApplyPropertyAnimation(e, id, value);
                    break;
            }
        }

        internal EntityId GetObjectValue(int elementIndex, int propertyId, int channel)
        {
            if (elementIndex < 0 || elementIndex >= m_Elements.Count)
                return EntityId.None;
            var element = m_Elements[elementIndex].Value;
            StylePropertyId id = (StylePropertyId)propertyId;

            Debug.Assert(channel < GetChannelCount(id));

            return GetPropertyTypeMapping(id) switch
            {
                PropertyType.Background => element.computedStyle.ReadPropertyAnimationEntityId(id),
                PropertyType.Font => element.computedStyle.ReadPropertyAnimationEntityId(id),
                PropertyType.FontDefinition => element.computedStyle.ReadPropertyAnimationEntityId(id),

                //invalid type
                PropertyType.Length => throw new InvalidOperationException(),
                PropertyType.Float => throw new InvalidOperationException(),
                PropertyType.Int => throw new InvalidOperationException(),
                PropertyType.Enum => throw new InvalidOperationException(),
                PropertyType.Color => throw new InvalidOperationException(),
                PropertyType.Translate => throw new InvalidOperationException(),
                PropertyType.Rotate => throw new InvalidOperationException(),
                PropertyType.Scale => throw new InvalidOperationException(),
                PropertyType.Ratio => throw new InvalidOperationException(),
                PropertyType.TransformOrigin => throw new InvalidOperationException(),
                PropertyType.Shorthand => throw new InvalidOperationException(),
                PropertyType.BackgroundPosition => throw new InvalidOperationException(),
                PropertyType.BackgroundRepeat => throw new InvalidOperationException(),
                PropertyType.BackgroundSize => throw new InvalidOperationException(),
                PropertyType.TextShadow => throw new InvalidOperationException(),
                PropertyType.TextAutoSize => throw new InvalidOperationException(),

                //Not implemented
                PropertyType.Filter => throw new NotImplementedException(),
                PropertyType.Cursor => throw new NotImplementedException(),
                PropertyType.List => throw new NotImplementedException(),
                PropertyType.MaterialDefinition => throw new NotImplementedException(),
                _ => throw new NotImplementedException(),// Why does c# think the list is not exhaustive? seems related to the cast...
            };


        }
    }
}
