// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Presets
{
    class DefaultPresetReorderableList : ReorderableList
    {
        static class Style
        {
            public static GUIStyle backgroundEven = "OL EntryBackEven";
            public static GUIStyle backgroundOdd = "OL EntryBackOdd";

            public static GUIStyle alignedRight;

            static Style()
            {
                alignedRight = new GUIStyle(EditorStyles.label);
                alignedRight.alignment = TextAnchor.MiddleRight;
            }
        }

        class PresetChanged : PresetSelectorReceiver
        {
            public SerializedProperty property;
            public int undoGroup;

            public override void OnSelectionChanged(Preset selection)
            {
                property.objectReferenceValue = selection;
                property.serializedObject.ApplyModifiedProperties();
            }

            public override void OnSelectionClosed(Preset selection)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                property.objectReferenceValue = selection;
                property.serializedObject.ApplyModifiedProperties();
                Undo.CollapseUndoOperations(undoGroup);
                DestroyImmediate(this);
            }
        }

        public string className { get; }
        public string fullClassName { get; }

        public DefaultPresetReorderableList(SerializedObject serializedObject, SerializedProperty propertyArray, PresetType type)
            : base(serializedObject, propertyArray)
        {
            draggable = true;
            headerHeight *= 2f;
            fullClassName = type.GetManagedTypeName();
            className = ClassName(type.GetManagedTypeName());
            var content = new GUIContent(className, type.GetIcon());
            var fullNameContent = new GUIContent($"({fullClassName})");
            drawHeaderCallback = rect => DrawHeaderCallback(rect, content, fullNameContent);
            drawElementCallback = (rect, index, active, focused) => DrawElementCallback(rect, index, active, focused, type, serializedProperty);
            drawElementBackgroundCallback = DrawElementBackgroundCallback;
        }

        static string ClassName(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName))
                return "Unsupported Type";
            int lastDot = fullTypeName.LastIndexOf(".");
            if (lastDot == -1)
                return fullTypeName;
            return fullTypeName.Substring(lastDot + 1);
        }

        static void DrawHeaderCallback(Rect rect, GUIContent content, GUIContent fullNameContent)
        {
            rect.x -= EditorGUI.indent;
            var firstLine = rect;
            firstLine.yMax = rect.yMin + rect.height / 2f;
            var size = Style.alignedRight.CalcSize(fullNameContent);
            var firstLineLeft = firstLine;
            firstLineLeft.xMax = firstLineLeft.xMax - size.x - 3f + EditorGUI.indent;
            EditorGUI.LabelField(firstLineLeft, content);
            var firstLineRight = firstLine;
            firstLineRight.xMin = firstLineRight.xMax - size.x - EditorGUI.indent;
            firstLineRight.x += EditorGUI.indent;
            EditorGUI.SelectableLabel(firstLineRight, fullNameContent.text, Style.alignedRight);

            var secondLine = rect;
            secondLine.yMin = rect.yMax - rect.height / 2f;
            secondLine.xMin += 18f;

            var filterRect = secondLine;
            filterRect.xMax = filterRect.xMin + secondLine.width / 2f;
            EditorGUI.LabelField(filterRect, GUIContent.Temp("Filter"));

            const float limitHeightOfDivider = 4f;
            Rect dividerRect = new Rect(filterRect.xMin - 5, filterRect.y + limitHeightOfDivider, 1f, filterRect.height - 2 * limitHeightOfDivider);
            dividerRect.x += EditorGUI.indent;
            EditorGUI.DrawRect(dividerRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

            var presetRect = secondLine;
            presetRect.xMin = filterRect.xMax;
            EditorGUI.LabelField(presetRect, GUIContent.Temp("Preset"));
            dividerRect.x = presetRect.xMin - 5 + EditorGUI.indent;
            EditorGUI.DrawRect(dividerRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        static void DrawElementBackgroundCallback(Rect rect, int index, bool isactive, bool isfocused)
        {
            if (Event.current.rawType != EventType.Repaint)
                return;

            if (isactive || isfocused)
            {
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isactive, isfocused, true);
                return;
            }
            Style.backgroundOdd.Draw(rect, false, false, false, false);
            if (index % 2 == 1)
                Style.backgroundEven.Draw(rect, false, false, false, false);
        }

        static void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused, PresetType keyType, SerializedProperty listProperty)
        {
            rect.yMin += 2f;
            rect.yMax = rect.yMin + EditorGUIUtility.singleLineHeight;
            var defaultProperty = listProperty.GetArrayElementAtIndex(index);
            var presetProperty = defaultProperty.FindPropertyRelative("m_Preset");
            var disabledProperty = defaultProperty.FindPropertyRelative("m_Disabled");
            var presetObject = (Preset)presetProperty.objectReferenceValue;

            var enableRect = rect;
            enableRect.y -= 1f;
            enableRect.xMax = enableRect.xMin + enableRect.height;
            using (new EditorGUI.PropertyScope(enableRect, GUIContent.none, disabledProperty))
            {
                var toggleValue = EditorGUI.Toggle(enableRect, GUIContent.none, !disabledProperty.boolValue);
                if (toggleValue == disabledProperty.boolValue)
                {
                    disabledProperty.boolValue = !disabledProperty.boolValue;
                }
            }
            rect.xMin += enableRect.width + 2f;
            using (new EditorGUI.DisabledScope(disabledProperty.boolValue))
            {
                var filterRect = rect;
                filterRect.xMax = filterRect.xMin + rect.width / 2f;
                DrawFilterField(filterRect, defaultProperty.FindPropertyRelative("m_Filter"));

                var presetFieldRect = rect;
                presetFieldRect.xMin = presetFieldRect.xMax - rect.width / 2f;
                DrawPresetField(presetFieldRect, presetProperty, keyType, presetObject);
            }
        }

        static void DrawPresetField(Rect rect, SerializedProperty presetProperty, PresetType keyType, Preset presetObject)
        {
            // lets hack a bit the ObjectField because we want to open our own preset selector.
            var buttonRect = new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height);
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (buttonRect.Contains(Event.current.mousePosition))
                    {
                        var changed = ScriptableObject.CreateInstance<PresetChanged>();
                        changed.property = presetProperty;
                        changed.undoGroup = Undo.GetCurrentGroup();
                        PresetSelector.ShowSelector(keyType, presetObject, false, changed);
                        Event.current.Use();
                    }

                    break;
            }

            var controlID = GUIUtility.GetControlID(GUIContent.Temp(presetProperty.displayName), FocusType.Passive);
            var dropRect = rect;
            dropRect.xMax -= rect.height;
            EditorGUI.DoObjectField(rect, dropRect, controlID, typeof(Preset), presetProperty, (references, type, property, options) => PresetFieldDropValidator(references, keyType), false);
        }

        static Object PresetFieldDropValidator(Object[] references, PresetType presetType)
        {
            if (references.Length == 1)
            {
                var preset = references[0] as Preset;
                if (preset != null && presetType == preset.GetPresetType())
                {
                    return references[0];
                }
            }
            return null;
        }

        static void DrawFilterField(Rect position, SerializedProperty filter)
        {
            var fieldRect = position;
            fieldRect.xMax -= 3f;
            using (new EditorGUI.PropertyScope(fieldRect, GUIContent.none, filter))
            {
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    var newValue = EditorGUI.ToolbarSearchField(fieldRect, filter.stringValue, false);
                    if (changed.changed)
                        filter.stringValue = newValue;
                }
            }
        }
    }
}
