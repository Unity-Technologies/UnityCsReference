// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    static class PropertyDrawerUtility
    {
        // For fields that show a struct property through boxedValue, which binding can't do. Adds what binding
        // would: alignment, tooltip, context menu, mixed values and prefab override styling.
        public static void TrackProperty<TValue>(BaseField<TValue> field, SerializedProperty property, Action<SerializedProperty> update)
        {
            PropertyField.ConfigureFieldStyles<BaseField<TValue>, TValue>(field);
            field.tooltip = property.tooltip;
            BindingsStyleHelpers.RegisterRightClickMenu(field, property);

            void Refresh(SerializedProperty changed)
            {
                update(changed);

                // The tracker disposes the changed property after the callback returns
                field.schedule.Execute(() => BindingsStyleHelpers.UpdateElementStyle(field, property));
            }

            Refresh(property);
            field.TrackPropertyValue(property, Refresh);
        }
    }
}
