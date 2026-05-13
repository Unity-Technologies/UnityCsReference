// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements.StyleSheets;


namespace UnityEngine.UIElements
{
    internal sealed partial class UIAnimationBinder
    {
        internal List<KeyValuePair<string, VisualElement>> m_Elements;
        internal Dictionary<PropertyName, VisualElement> m_ElementsMap;

        internal static int GetChannelCount(StylePropertyId id)
        {
            return m_ChannelCount[(int)id];
        }

        internal static PropertyType GetPropertyTypeMapping(StylePropertyId id)
        {
            return m_PropertyTypeMapping[(int)id];
        }

        // Per-channel metadata generated from AnimationBindingHelper in the UIElementsGenerator.
        // These are the single source of truth for channel suffixes (".value", ".offset.unit",
        // ".x.value", ...) and their curve kinds (Float for continuous, Int for discrete enum
        // selectors, PPtr for object references). Authoring UI and recording dispatch read
        // these instead of hardcoding composite-specific tables.
        //
        // The backing tables (m_ChannelSuffixes / m_ChannelKinds) are indexed by PropertyType
        // rather than StylePropertyId, since every style property with the same kind shares
        // the same sub-channel layout. We route through m_PropertyTypeMapping here so every
        // Length / Color / Float id reuses a single row.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static IReadOnlyList<string> GetChannelSuffixes(StylePropertyId id)
        {
            return m_ChannelSuffixes[(int)m_PropertyTypeMapping[(int)id]];
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static AnimationChannelKind GetChannelKind(StylePropertyId id, int channel)
        {
            return m_ChannelKinds[(int)m_PropertyTypeMapping[(int)id]][channel];
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static int StylePropertyIdCount
        {
            [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
            get => m_ChannelCount.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetElementCount() => m_Elements?.Count ?? -1;

        internal void SetFloatValue(int elementIndex, int propertyId, int channel, float value)
        {
            if (elementIndex < 0 || elementIndex >= GetElementCount())
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
                    e.computedStyle.ApplyPropertyAnimation(e, id, BitConverter.SingleToInt32Bits(value));
                    break;

                case PropertyType.Length:
                {
                    // Channels match AnimationLengthChannel layout (0 = value, 1 = unit).
                    var l = e.computedStyle.ReadPropertyAnimationLength(id);
                    l = AnimationLengthChannel.Write(l, channel, value);
                    e.computedStyle.ApplyPropertyAnimation(e, id, l);
                    break;
                }

                case PropertyType.BackgroundPosition:
                {
                    // Channel 0 encodes the BackgroundPositionKeyword as a discrete int.
                    // Channels 1/2 form a LengthBlock for the offset; forwarded to
                    // AnimationLengthChannel with a 1-channel shift so sub-channel 0 =
                    // offset.value, sub-channel 1 = offset.unit.
                    var bp = e.computedStyle.ReadPropertyAnimationBackgroundPosition(id);
                    if (channel == 0)
                        bp.keyword = (BackgroundPositionKeyword)BitConverter.SingleToInt32Bits(value);
                    else
                        bp.offset = AnimationLengthChannel.Write(bp.offset, channel - 1, value);
                    e.computedStyle.ApplyPropertyAnimation(e, id, bp);
                    break;
                }

                case PropertyType.BackgroundRepeat:
                {
                    // Channels 0 (x) and 1 (y) each carry a Repeat enum encoded as an int
                    // bit-pattern in a float.
                    var br = e.computedStyle.ReadPropertyAnimationBackgroundRepeat(id);
                    if (channel == 0)
                        br.x = (Repeat)BitConverter.SingleToInt32Bits(value);
                    else if (channel == 1)
                        br.y = (Repeat)BitConverter.SingleToInt32Bits(value);
                    e.computedStyle.ApplyPropertyAnimation(e, id, br);
                    break;
                }

                case PropertyType.BackgroundSize:
                {
                    // Channel 0 is the discrete sizeType. Channels 1/2 form a LengthBlock
                    // for .x, channels 3/4 another for .y; both forwarded through
                    // AnimationLengthChannel. The public setters on BackgroundSize mutate
                    // sibling fields (e.g. setting .x forces sizeType=Length) so we
                    // reassemble the struct via its internal 3-arg ctor.
                    var bs = e.computedStyle.ReadPropertyAnimationBackgroundSize(id);
                    var sizeType = bs.sizeType;
                    var x = bs.x;
                    var y = bs.y;
                    if (channel == 0)
                        sizeType = (BackgroundSizeType)BitConverter.SingleToInt32Bits(value);
                    else if (channel <= 2)
                        x = AnimationLengthChannel.Write(x, channel - 1, value);
                    else
                        y = AnimationLengthChannel.Write(y, channel - 3, value);
                    e.computedStyle.ApplyPropertyAnimation(e, id, new BackgroundSize(sizeType, x, y));
                    break;
                }

            }

        }
      
        internal float GetFloatValue(int elementIndex, int propertyId, int channel)
        {
            if (elementIndex < 0 || elementIndex >= GetElementCount())
                return 0;
            var element = m_Elements[elementIndex].Value;
            StylePropertyId id = (StylePropertyId)propertyId;

            Debug.Assert(channel < GetChannelCount(id));

            return GetPropertyTypeMapping(id) switch
            {
                PropertyType.Length => AnimationLengthChannel.ReadFloat(
                    element.computedStyle.ReadPropertyAnimationLength(id), channel),
                PropertyType.Float => element.computedStyle.ReadPropertyAnimationFloat(id),
                PropertyType.Int => BitConverter.Int32BitsToSingle(element.computedStyle.ReadPropertyAnimationInt(id)),
                PropertyType.Enum => BitConverter.Int32BitsToSingle(element.computedStyle.ReadPropertyAnimationInt(id)),
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
                PropertyType.BackgroundPosition => channel == 0
                    ? BitConverter.Int32BitsToSingle((int)element.computedStyle.ReadPropertyAnimationBackgroundPosition(id).keyword)
                    : AnimationLengthChannel.ReadFloat(element.computedStyle.ReadPropertyAnimationBackgroundPosition(id).offset, channel - 1),
                PropertyType.BackgroundRepeat => channel switch
                {
                    0 => BitConverter.Int32BitsToSingle((int)element.computedStyle.ReadPropertyAnimationBackgroundRepeat(id).x),
                    1 => BitConverter.Int32BitsToSingle((int)element.computedStyle.ReadPropertyAnimationBackgroundRepeat(id).y),
                    _ => throw new NotImplementedException(),
                },
                PropertyType.BackgroundSize => channel == 0
                    ? BitConverter.Int32BitsToSingle((int)element.computedStyle.ReadPropertyAnimationBackgroundSize(id).sizeType)
                    : channel <= 2
                        ? AnimationLengthChannel.ReadFloat(element.computedStyle.ReadPropertyAnimationBackgroundSize(id).x, channel - 1)
                        : AnimationLengthChannel.ReadFloat(element.computedStyle.ReadPropertyAnimationBackgroundSize(id).y, channel - 3),


                //Not implemented
                PropertyType.TransformOrigin => throw new NotImplementedException(),
                PropertyType.Shorthand => throw new NotImplementedException(),
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
            if (elementIndex < 0 || elementIndex >= GetElementCount())
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
            if (elementIndex < 0 || elementIndex >= GetElementCount())
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
