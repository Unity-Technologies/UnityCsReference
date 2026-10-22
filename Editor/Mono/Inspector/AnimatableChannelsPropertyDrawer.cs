// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    // Draws a channel container as one row per channel, labelled with the channel name. A container
    // serializes as two parallel arrays, m_Names and m_Values; the rows edit m_Values through the
    // SerializedProperty, so multi-object editing, Undo, prefab overrides and animation mode all
    // behave as they do for any other field. The names are owned by the component, which fills them
    // from its asset, so they are labels rather than editable strings.
    //
    // A component can have a hundred channels or more, so both follow the default array drawers: UI
    // Toolkit shows the rows in a ListView capped in height, which only binds the rows in view, and
    // IMGUI only draws the rows inside the visible part of the Inspector, as ReorderableList does.
    [CustomPropertyDrawer(typeof(AnimatableFloatChannels))]
    [CustomPropertyDrawer(typeof(AnimatableIntChannels))]
    [CustomPropertyDrawer(typeof(AnimatableObjectChannels))]
    internal class AnimatableChannelsPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent k_NoChannels = L10n.TextContent("No channels", null, null, null);
        static readonly GUIContent k_DifferentChannels = L10n.TextContent("Selected objects have different channels", null, null, null);

        const float k_MaxListHeight = 520;

        static float rowHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var foldout = new Foldout { text = preferredLabel, value = property.isExpanded };
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == foldout)
                    property.isExpanded = evt.newValue;
            });

            var names = property.FindPropertyRelative("m_Names");
            var values = property.FindPropertyRelative("m_Values");
            if (names == null || values == null)
                return foldout;

            var message = new Label();
            var rows = new ListView
            {
                fixedItemHeight = rowHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                selectionType = SelectionType.None,
                makeItem = () => new PropertyField(),
                bindItem = (row, index) => BindRow((PropertyField)row, names, values, index),
                unbindItem = (row, index) => ((PropertyField)row).Unbind(),
            };
            // The same cap as a default array's list, in UnityEngine.UIElements.uss, which only reaches
            // a list directly under a PropertyField.
            rows.style.maxHeight = k_MaxListHeight;
            foldout.Add(message);
            foldout.Add(rows);
            UpdateRows(rows, message, names, values);

            // The component replaces its channel set at any time, for example when its asset changes.
            foldout.TrackPropertyValue(names, changedNames => UpdateRows(rows, message, changedNames, values));
            return foldout;
        }

        static void BindRow(PropertyField row, SerializedProperty names, SerializedProperty values, int index)
        {
            // Channel names are user data, not UI text, so they are not localized.
            row.label = names.GetArrayElementAtIndex(index).stringValue;
            row.BindProperty(values.GetArrayElementAtIndex(index));
        }

        static void UpdateRows(ListView rows, Label message, SerializedProperty names, SerializedProperty values)
        {
            // With different channel sets, the same position is a different channel on each object,
            // so editing it would write to channels the user did not pick.
            var channelCount = names.hasMultipleDifferentValues ? 0 : Mathf.Min(names.arraySize, values.arraySize);
            message.text = names.hasMultipleDifferentValues ? k_DifferentChannels.text : k_NoChannels.text;
            message.style.display = channelCount == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            rows.style.display = channelCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            var indices = new int[channelCount];
            for (int i = 0; i < channelCount; i++)
                indices[i] = i;

            rows.itemsSource = indices;
            rows.Rebuild();
        }

        static int GetRowCount(SerializedProperty names, SerializedProperty values)
        {
            if (names == null || values == null || names.hasMultipleDifferentValues)
                return 1;

            return Mathf.Max(Mathf.Min(names.arraySize, values.arraySize), 1);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            var rowCount = GetRowCount(property.FindPropertyRelative("m_Names"), property.FindPropertyRelative("m_Values"));

            // One row for the foldout, then one per channel (or one for a message).
            return EditorGUIUtility.singleLineHeight + rowHeight * rowCount;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            if (!property.isExpanded)
                return;

            var names = property.FindPropertyRelative("m_Names");
            var values = property.FindPropertyRelative("m_Values");

            using (new EditorGUI.IndentLevelScope())
            {
                rect.y += rowHeight;

                if (names == null || values == null)
                    return;

                // With different channel sets, the same position is a different channel on each
                // object, so editing it would write to channels the user did not pick.
                if (names.hasMultipleDifferentValues)
                {
                    EditorGUI.LabelField(rect, k_DifferentChannels);
                    return;
                }

                var channelCount = Mathf.Min(names.arraySize, values.arraySize);
                if (channelCount == 0)
                {
                    EditorGUI.LabelField(rect, k_NoChannels);
                    return;
                }

                var visibleRect = GUIClip.visibleRect;
                var first = Mathf.Clamp(Mathf.FloorToInt((visibleRect.yMin - rect.y) / rowHeight), 0, channelCount);
                var last = Mathf.Clamp(Mathf.CeilToInt((visibleRect.yMax - rect.y) / rowHeight), first, channelCount);
                for (int i = first; i < last; i++)
                    DrawRow(new Rect(rect.x, rect.y + rowHeight * i, rect.width, rect.height), names, values, i);
            }
        }

        static void DrawRow(Rect rect, SerializedProperty names, SerializedProperty values, int index)
        {
            // Channel names are user data, not UI text, so they are not localized.
            var channelLabel = EditorGUIUtility.TempContent(names.GetArrayElementAtIndex(index).stringValue);
            EditorGUI.PropertyField(rect, values.GetArrayElementAtIndex(index), channelLabel);
        }
    }
}
