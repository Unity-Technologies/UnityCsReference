// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    [CustomPropertyDrawer(typeof(Hash128))]
    sealed class EntitiesHash128PropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new TextField(preferredLabel) { isReadOnly = true };
            PropertyDrawerUtility.TrackProperty(field, property, changed => field.SetValueWithoutNotify(GetDisplayString(changed)));
            return field;
        }

        static string GetDisplayString(SerializedProperty property)
        {
            // The serializer reads any struct named Hash128 as a UnityEngine.Hash128 with the same bytes
            Hash128 value = property.hash128Value;
            return value.ToString();
        }
    }
}
