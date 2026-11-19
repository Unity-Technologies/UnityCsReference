// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    abstract class FixedStringPropertyDrawer<T> : PropertyDrawer
        where T : unmanaged, INativeList<byte>, IUTF8Bytes
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // maxLength counts characters but the capacity counts UTF-8 bytes, so CopyFromTruncated enforces the byte limit
            var field = new TextField(preferredLabel) { isDelayed = true, maxLength = default(T).Capacity };
            PropertyDrawerUtility.TrackProperty(field, property, changed => field.SetValueWithoutNotify(changed.boxedValue.ToString()));
            field.RegisterValueChangedCallback(evt =>
            {
                if (!property.isValid)
                    return;

                var value = default(T);
                value.CopyFromTruncated(evt.newValue);
                property.boxedValue = value;
                property.serializedObject.ApplyModifiedProperties();
                field.SetValueWithoutNotify(value.ToString());
            });

            return field;
        }
    }

    [CustomPropertyDrawer(typeof(FixedString32Bytes))]
    sealed class FixedString32BytesPropertyDrawer : FixedStringPropertyDrawer<FixedString32Bytes>
    {
    }

    [CustomPropertyDrawer(typeof(FixedString64Bytes))]
    sealed class FixedString64BytesPropertyDrawer : FixedStringPropertyDrawer<FixedString64Bytes>
    {
    }

    [CustomPropertyDrawer(typeof(FixedString128Bytes))]
    sealed class FixedString128BytesPropertyDrawer : FixedStringPropertyDrawer<FixedString128Bytes>
    {
    }

    [CustomPropertyDrawer(typeof(FixedString512Bytes))]
    sealed class FixedString512BytesPropertyDrawer : FixedStringPropertyDrawer<FixedString512Bytes>
    {
    }

    [CustomPropertyDrawer(typeof(FixedString4096Bytes))]
    sealed class FixedString4096BytesPropertyDrawer : FixedStringPropertyDrawer<FixedString4096Bytes>
    {
    }
}
