// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    // Links a field to a serialized property it cannot be bound to, because the field shows the stored value in another form, such as degrees for a stored direction.
    // The field gets what binding gives a bound field: the mixed-value dash, prefab override styling, the property's context menu, and a refresh whenever the property changes outside the field, including by undo.
    // Only a user edit writes to the property, so showing a value never modifies, dirties or overrides anything.
    // A text field linked this way should be delayed, so it writes only when editing ends and the refresh that follows never rewrites partly typed text.
    internal static class SerializedPropertyProxy
    {
        // Link the field to the property, showing the value read from the property and writing each user edit back to every selected object.
        // The refreshed action runs after every refresh, for anything else that shows the property's value.
        internal static void Link<T>(BaseField<T> field, SerializedProperty property, Func<SerializedProperty, T> read, Action<SerializedProperty, T> write, Action<SerializedProperty> refreshed = null)
        {
            var linkedProperty = property.Copy();

            Refresh(linkedProperty);

            BindingsStyleHelpers.RegisterRightClickMenu(field, linkedProperty);
            field.TrackPropertyValue(linkedProperty, Refresh);
            field.RegisterCallback<AttachToPanelEvent>(_ => Refresh(linkedProperty));

            // Reverting an override whose value matches the prefab changes no value, so the styling is also refreshed on any change to the objects, as binding does for a bound field.
            field.TrackSerializedObjectValue(linkedProperty.serializedObject, _ => UpdateStyle(field, linkedProperty));
            field.RegisterValueChangedCallback(UserChanged);

            #region Locals

            // Show the stored value with its mixed and override state.
            void Refresh(SerializedProperty changedProperty)
            {
                field.SetValueWithoutNotify(read(changedProperty));
                field.showMixedValue = changedProperty.hasMultipleDifferentValues;
                UpdateStyle(field, changedProperty);
                refreshed?.Invoke(changedProperty);
            }

            // Write the user's edit to every selected object.
            void UserChanged(ChangeEvent<T> evt)
            {
                write(linkedProperty, evt.newValue);
                linkedProperty.serializedObject.ApplyModifiedProperties();
            }

            #endregion
        }

        // Show the property's prefab override styling on the field, once the field is in a panel.
        // Styling a detached field marks it as overridden without creating its override bar, and the bar is then never created, so a detached field is styled when it is attached.
        internal static void UpdateStyle(VisualElement field, SerializedProperty property)
        {
            if (field.panel != null)
                BindingsStyleHelpers.UpdateElementStyle(field, property);
        }
    }
}
