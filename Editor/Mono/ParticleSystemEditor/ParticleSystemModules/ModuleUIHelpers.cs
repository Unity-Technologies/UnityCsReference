// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using Enum = System.Enum;
using GetBoundsFunc = System.Func<UnityEngine.Bounds>;

namespace UnityEditor
{
    partial class ModuleUI
    {
        protected static readonly bool kUseSignedRange = true;
        protected static readonly Rect kUnsignedRange = new Rect(0, 0, 1, 1); // Vertical range 0...1
        protected static readonly Rect kSignedRange = new Rect(0, -1, 1, 2); // Vertical range -1...1
        protected const int kSingleLineHeight = 13;
        protected const float k_minMaxToggleWidth = 13;
        protected const float k_toggleWidth = 9;
        protected const float kDragSpace = 20;
        protected const int kPlusAddRemoveButtonWidth = 12;
        protected const int kPlusAddRemoveButtonSpacing = 5;
        protected const int kSpacingSubLabel = 4;
        protected const int kSubLabelWidth = 10;
        protected const string kFormatString = "g7";
        protected const float kReorderableListElementHeight = 16;


        // Module rulers that are fixed
        static public float k_CompactFixedModuleWidth = 295f;
        static public float k_SpaceBetweenModules = 5;

        public static readonly GUIStyle s_ControlRectStyle = new GUIStyle { margin = new RectOffset(0, 0, 2, 2) };

        private static void Label(Rect rect, GUIContent guiContent)
        {
            GUI.Label(rect, guiContent, ParticleSystemStyles.Get().label);
        }

        protected static Rect GetControlRect(int height, params GUILayoutOption[] layoutOptions)
        {
            return GUILayoutUtility.GetRect(0, height, s_ControlRectStyle, layoutOptions);
        }

        protected static Rect FieldPosition(Rect totalPosition, out Rect labelPosition)
        {
            labelPosition = new Rect(totalPosition.x + EditorGUI.indent, totalPosition.y, EditorGUIUtility.labelWidth - EditorGUI.indent, kSingleLineHeight);
            return new Rect(totalPosition.x + EditorGUIUtility.labelWidth, totalPosition.y, totalPosition.width - EditorGUIUtility.labelWidth, totalPosition.height);
        }

        protected static Rect PrefixLabel(Rect totalPosition, GUIContent label)
        {
            if (!EditorGUI.LabelHasContent(label))
                return EditorGUI.IndentedRect(totalPosition);

            Rect labelPosition;
            Rect fieldPosition = FieldPosition(totalPosition, out labelPosition);
            EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, 0, ParticleSystemStyles.Get().label);
            return fieldPosition;
        }

        protected static Rect SubtractPopupWidth(Rect position)
        {
            position.width -= 1 + k_minMaxToggleWidth;
            return position;
        }

        protected static Rect GetPopupRect(Rect position)
        {
            position.xMin = position.xMax - k_minMaxToggleWidth;
            return position;
        }

        protected static bool PlusButton(Rect position)
        {
            return GUI.Button(new Rect(position.x - 2, position.y - 2, 12, 13), GUIContent.none, "OL Plus");
        }

        protected static bool MinusButton(Rect position)
        {
            return GUI.Button(new Rect(position.x - 2, position.y - 2, 12, 13), GUIContent.none, "OL Minus");
        }

        private static float FloatDraggable(Rect rect, SerializedProperty floatProp, float remap, float dragWidth)
        {
            return FloatDraggable(rect, floatProp, remap, dragWidth, kFormatString);
        }

        public static float FloatDraggable(Rect rect, float floatValue, float remap, float dragWidth, string formatString)
        {
            int id = EditorGUIUtility.GetControlID(1658656233, FocusType.Keyboard, rect);

            Rect dragZoneRect = rect;
            dragZoneRect.width = dragWidth;

            Rect floatFieldRect = rect;
            floatFieldRect.xMin += dragWidth;

            return EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, floatFieldRect, dragZoneRect, id, floatValue * remap, formatString, ParticleSystemStyles.Get().numberField, true) / remap;
        }

        public static float FloatDraggable(Rect rect, SerializedProperty floatProp, float remap, float dragWidth, string formatString)
        {
            EditorGUI.BeginProperty(rect, GUIContent.none, floatProp);

            EditorGUI.BeginChangeCheck();
            float floatValue = FloatDraggable(rect, floatProp.floatValue, remap, dragWidth, formatString);
            if (EditorGUI.EndChangeCheck())
                floatProp.floatValue = floatValue;

            EditorGUI.EndProperty();

            return floatValue;
        }

        public static Vector3 GUIVector3Field(GUIContent guiContent, SerializedProperty vecProp, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = vecProp.hasMultipleDifferentValues;

            Rect r = GetControlRect(kSingleLineHeight, layoutOptions);
            r = PrefixLabel(r, guiContent);

            GUIContent[] labels = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
            float elementWidth = (r.width - 2 * kSpacingSubLabel) / 3f;
            r.width = elementWidth;

            SerializedProperty cur = vecProp.Copy();
            cur.Next(true);

            Vector3 vec = vecProp.vector3Value;

            for (int i = 0; i < 3; ++i)
            {
                Label(r, labels[i]);
                EditorGUI.BeginChangeCheck();
                float newValue = FloatDraggable(r, cur, 1.0f, 25.0f, "g5");
                if (EditorGUI.EndChangeCheck())
                    cur.floatValue = newValue;
                cur.Next(false);
                r.x += elementWidth + kSpacingSubLabel;
            }

            EditorGUI.showMixedValue = false;
            return vec;
        }

        public static float GUIFloat(string label, SerializedProperty floatProp, params GUILayoutOption[] layoutOptions)
        {
            return GUIFloat(GUIContent.Temp(label), floatProp, layoutOptions);
        }

        public static float GUIFloat(GUIContent guiContent, SerializedProperty floatProp, params GUILayoutOption[] layoutOptions)
        {
            return GUIFloat(guiContent, floatProp, kFormatString, layoutOptions);
        }

        public static float GUIFloat(GUIContent guiContent, SerializedProperty floatProp, string formatString, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            PrefixLabel(rect, guiContent);
            return FloatDraggable(rect, floatProp, 1f, EditorGUIUtility.labelWidth, formatString);
        }

        public static float GUIFloat(GUIContent guiContent, float floatValue, string formatString, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            PrefixLabel(rect, guiContent);
            return FloatDraggable(rect, floatValue, 1f, EditorGUIUtility.labelWidth, formatString);
        }

        public static void GUIButtonGroup(EditMode.SceneViewEditMode[] modes, GUIContent[] guiContents, GetBoundsFunc getBoundsOfTargets, Editor caller)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            EditMode.DoInspectorToolbar(modes, guiContents, getBoundsOfTargets, caller);
            GUILayout.EndHorizontal();
        }

        private static bool Toggle(Rect rect, SerializedProperty boolProp)
        {
            EditorGUIInternal.mixedToggleStyle = ParticleSystemStyles.Get().toggleMixed;
            EditorGUI.BeginProperty(rect, GUIContent.none, boolProp);

            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUI.Toggle(rect, boolProp.boolValue, ParticleSystemStyles.Get().toggle);
            if (EditorGUI.EndChangeCheck())
                boolProp.boolValue = newValue;

            EditorGUI.EndProperty();
            EditorGUIInternal.mixedToggleStyle = EditorStyles.toggleMixed;

            return newValue;
        }

        public static bool GUIToggle(string label, SerializedProperty boolProp, params GUILayoutOption[] layoutOptions)
        {
            return GUIToggle(GUIContent.Temp(label), boolProp, layoutOptions);
        }

        public static bool GUIToggle(GUIContent guiContent, SerializedProperty boolProp, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, guiContent);
            bool toggleValue = Toggle(rect, boolProp);
            return toggleValue;
        }

        public static void GUILayerMask(GUIContent guiContent, SerializedProperty boolProp, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = boolProp.hasMultipleDifferentValues;

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, guiContent);
            EditorGUI.LayerMaskField(rect, boolProp, null, ParticleSystemStyles.Get().popup);

            EditorGUI.showMixedValue = false;
        }

        public static bool GUIToggle(GUIContent guiContent, bool boolValue, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, guiContent);
            boolValue = EditorGUI.Toggle(rect, boolValue, ParticleSystemStyles.Get().toggle);
            return boolValue;
        }

        public static void GUIToggleWithFloatField(string name, SerializedProperty boolProp, SerializedProperty floatProp, bool invertToggle, params GUILayoutOption[] layoutOptions)
        {
            GUIToggleWithFloatField(EditorGUIUtility.TempContent(name), boolProp, floatProp, invertToggle, layoutOptions);
        }

        public static void GUIToggleWithFloatField(GUIContent guiContent, SerializedProperty boolProp, SerializedProperty floatProp, bool invertToggle, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GUILayoutUtility.GetRect(0, kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, guiContent);

            Rect toggleRect = rect;
            toggleRect.xMax = toggleRect.x + k_toggleWidth;
            bool toggleValue = Toggle(toggleRect, boolProp);
            toggleValue = invertToggle ? !toggleValue : toggleValue;

            if (toggleValue)
            {
                float dragWidth = 25f;
                Rect floatDragRect = new Rect(rect.x + EditorGUIUtility.labelWidth + k_toggleWidth, rect.y, rect.width - k_toggleWidth, rect.height);
                FloatDraggable(floatDragRect, floatProp, 1f, dragWidth);
            }
        }

        public static void GUIToggleWithIntField(string name, SerializedProperty boolProp, SerializedProperty floatProp, bool invertToggle, params GUILayoutOption[] layoutOptions)
        {
            GUIToggleWithIntField(EditorGUIUtility.TempContent(name), boolProp, floatProp, invertToggle, layoutOptions);
        }

        public static void GUIToggleWithIntField(GUIContent guiContent, SerializedProperty boolProp, SerializedProperty intProp, bool invertToggle, params GUILayoutOption[] layoutOptions)
        {
            Rect lineRect = GetControlRect(kSingleLineHeight, layoutOptions);
            Rect labelRect = PrefixLabel(lineRect, guiContent);

            Rect toggleRect = labelRect;
            toggleRect.xMax = toggleRect.x + k_toggleWidth;
            bool toggleValue = Toggle(toggleRect, boolProp);
            toggleValue = invertToggle ? !toggleValue : toggleValue;

            if (toggleValue)
            {
                EditorGUI.showMixedValue = intProp.hasMultipleDifferentValues;

                float dragWidth = 25f;
                Rect intDragRect = new Rect(toggleRect.xMax, lineRect.y, lineRect.width - toggleRect.xMax + k_toggleWidth, lineRect.height);
                EditorGUI.BeginChangeCheck();
                int newValue = IntDraggable(intDragRect, null, intProp.intValue, dragWidth);
                if (EditorGUI.EndChangeCheck())
                    intProp.intValue = newValue;

                EditorGUI.showMixedValue = false;
            }
        }

        public static void GUIObject(GUIContent label, SerializedProperty objectProp, params GUILayoutOption[] layoutOptions)
        {
            GUIObject(label, objectProp, null, layoutOptions);
        }

        public static void GUIObject(GUIContent label, SerializedProperty objectProp, System.Type objType, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = objectProp.hasMultipleDifferentValues;

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);
            EditorGUI.ObjectField(rect, objectProp, objType, GUIContent.none, ParticleSystemStyles.Get().objectField);

            EditorGUI.showMixedValue = false;
        }

        public static void GUIObjectFieldAndToggle(GUIContent label, SerializedProperty objectProp, SerializedProperty boolProp, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);

            EditorGUI.showMixedValue = objectProp.hasMultipleDifferentValues;

            rect.xMax -= k_toggleWidth + 10;
            EditorGUI.ObjectField(rect, objectProp, GUIContent.none);

            EditorGUI.showMixedValue = false;

            if (boolProp != null)
            {
                rect.x += rect.width + 10;
                rect.width = k_toggleWidth;
                Toggle(rect, boolProp);
            }
        }

        internal Object ParticleSystemValidator(Object[] references, System.Type objType, SerializedProperty property)
        {
            foreach (Object obj in references)
            {
                if (obj != null)
                {
                    GameObject gameObj = obj as GameObject;
                    if (gameObj != null)
                    {
                        ParticleSystem s = gameObj.GetComponent<ParticleSystem>();
                        if (s)
                        {
                            return s;
                        }
                    }
                }
            }
            return null;
        }

        // returns the index of the button that was pressed otherwise -1
        public int GUIListOfFloatObjectToggleFields(GUIContent label, SerializedProperty[] objectProps, EditorGUI.ObjectFieldValidator validator, GUIContent buttonTooltip, bool allowCreation, params GUILayoutOption[] layoutOptions)
        {
            int buttonPressed = -1;
            int numObjects = objectProps.Length;
            Rect rect = GUILayoutUtility.GetRect(0, (kSingleLineHeight + 2) * numObjects, layoutOptions); // the +1 is that label is on it own line
            rect.height = kSingleLineHeight;

            float indent = 10f;
            float floatFieldWidth = 35f;
            float space = 10f;
            float objectFieldWidth = rect.width - indent - floatFieldWidth - space * 2 - k_toggleWidth;

            PrefixLabel(rect, label);

            for (int i = 0; i < numObjects; ++i)
            {
                SerializedProperty objectProp = objectProps[i];

                EditorGUI.showMixedValue = objectProp.hasMultipleDifferentValues;

                Rect r2 = new Rect(rect.x + indent + floatFieldWidth + space, rect.y, objectFieldWidth, rect.height);
                int id = EditorGUIUtility.GetControlID(1235498, FocusType.Keyboard, r2);

                EditorGUI.DoObjectField(r2, r2, id, null, null, objectProp, validator, true, ParticleSystemStyles.Get().objectField);

                EditorGUI.showMixedValue = false;

                if (objectProp.objectReferenceValue == null)
                {
                    r2 = new Rect(rect.xMax - k_toggleWidth, rect.y + 3, k_toggleWidth, k_toggleWidth);
                    if (!allowCreation || GUI.Button(r2, buttonTooltip ?? GUIContent.none, ParticleSystemStyles.Get().plus))
                        buttonPressed = i;
                }

                rect.y += kSingleLineHeight + 2;
            }

            return buttonPressed;
        }

        public static void GUIIntDraggableX2(GUIContent mainLabel, GUIContent label1, SerializedProperty intProp1, GUIContent label2, SerializedProperty intProp2, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, mainLabel);

            float room = (rect.width - kSpacingSubLabel) * 0.5f;
            Rect rectProp = new Rect(rect.x, rect.y, room, rect.height);
            IntDraggable(rectProp, label1, intProp1, kSubLabelWidth);
            rectProp.x += room + kSpacingSubLabel;
            IntDraggable(rectProp, label2, intProp2, kSubLabelWidth);
        }

        public static int IntDraggable(Rect rect, GUIContent label, SerializedProperty intProp, float dragWidth)
        {
            EditorGUI.BeginProperty(rect, GUIContent.none, intProp);

            EditorGUI.BeginChangeCheck();
            int newValue = IntDraggable(rect, label, intProp.intValue, dragWidth);
            if (EditorGUI.EndChangeCheck())
                intProp.intValue = newValue;

            EditorGUI.EndProperty();

            return intProp.intValue;
        }

        public static int GUIInt(GUIContent guiContent, SerializedProperty intProp, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GUILayoutUtility.GetRect(0, kSingleLineHeight, layoutOptions);

            EditorGUI.BeginProperty(rect, GUIContent.none, intProp);
            PrefixLabel(rect, guiContent);

            EditorGUI.BeginChangeCheck();
            int newValue = IntDraggable(rect, null, intProp.intValue, EditorGUIUtility.labelWidth);
            if (EditorGUI.EndChangeCheck())
                intProp.intValue = newValue;

            EditorGUI.EndProperty();

            return intProp.intValue;
        }

        public static int IntDraggable(Rect rect, GUIContent label, int value, float dragWidth)
        {
            float width = rect.width;
            Rect r = rect;
            r.width = width;
            int id = EditorGUIUtility.GetControlID(16586232, FocusType.Keyboard, r);

            Rect dragZoneRect = r;
            dragZoneRect.width = dragWidth;
            if (label != null && !string.IsNullOrEmpty(label.text))
                Label(dragZoneRect, label);

            Rect intFieldRect = r;
            intFieldRect.x += dragWidth;
            intFieldRect.width = width - dragWidth;

            // copied
            float dragSensitity = Mathf.Max(1, Mathf.Pow(Mathf.Abs((float)value), 0.5f) * .03f);
            //float dragSensitity = Mathf.Clamp01(Mathf.Max(1, Mathf.Pow(Mathf.Abs((float)value), 0.5f)) * 0.03f );
            return (int)EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, intFieldRect, dragZoneRect, id, value, EditorGUI.kIntFieldFormatString, ParticleSystemStyles.Get().numberField, true, dragSensitity);
        }

        public static void GUIMinMaxRange(GUIContent label, SerializedProperty vec2Prop, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = vec2Prop.hasMultipleDifferentValues;

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = SubtractPopupWidth(rect); // There is actually no popup in this control but the layout is nicer if the fields line up.
            rect = PrefixLabel(rect, label);

            float floatFieldWidth = (rect.width - kDragSpace) * 0.5f;
            Vector2 range = vec2Prop.vector2Value;

            EditorGUI.BeginChangeCheck();

            rect.width = floatFieldWidth;
            rect.xMin -= kDragSpace;
            range.x = FloatDraggable(rect, range.x, 1f, kDragSpace, kFormatString);

            rect.x += floatFieldWidth + kDragSpace;
            range.y = FloatDraggable(rect, range.y, 1f, kDragSpace, kFormatString);

            if (EditorGUI.EndChangeCheck())
                vec2Prop.vector2Value = range;

            EditorGUI.showMixedValue = false;
        }

        public static void GUISlider(SerializedProperty floatProp, float a, float b, float remap)
        {
            GUISlider("", floatProp, a, b, remap);
        }

        public static void GUISlider(string name, SerializedProperty floatProp, float a, float b, float remap)
        {
            EditorGUI.showMixedValue = floatProp.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider(name, floatProp.floatValue * remap, a, b, GUILayout.MinWidth(300)) / remap;
            if (EditorGUI.EndChangeCheck())
                floatProp.floatValue = newValue;

            EditorGUI.showMixedValue = false;
        }

        public static void GUIMinMaxSlider(GUIContent label, SerializedProperty vec2Prop, float a, float b, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = vec2Prop.hasMultipleDifferentValues;

            Rect rect = GetControlRect(kSingleLineHeight * 2, layoutOptions);
            Rect r = rect;
            r.height = kSingleLineHeight;
            r.y += 3;
            PrefixLabel(r, label);

            Vector2 v = vec2Prop.vector2Value;
            r.y += kSingleLineHeight;

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(r, ref v.x, ref v.y, a, b);
            if (EditorGUI.EndChangeCheck())
                vec2Prop.vector2Value = v;

            EditorGUI.showMixedValue = false;
        }

        public static bool GUIBoolAsPopup(GUIContent label, SerializedProperty boolProp, string[] options, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = boolProp.hasMultipleDifferentValues;

            System.Diagnostics.Debug.Assert(options.Length == 2);

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.Popup(rect, null, boolProp.boolValue ? 1 : 0, EditorGUIUtility.TempContent(options), ParticleSystemStyles.Get().popup);
            if (EditorGUI.EndChangeCheck())
                boolProp.boolValue = newValue > 0 ? true : false;

            EditorGUI.showMixedValue = false;
            return newValue > 0 ? true : false;
        }

        public static Enum GUIEnumMask(GUIContent label, Enum enumValue, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);
            return EditorGUI.EnumFlagsField(rect, enumValue, ParticleSystemStyles.Get().popup);
        }

        public static void GUIMask(GUIContent label, SerializedProperty intProp, string[] options, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = intProp.hasMultipleDifferentValues;

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.MaskField(rect, label, intProp.intValue, options, ParticleSystemStyles.Get().popup);
            if (EditorGUI.EndChangeCheck())
                intProp.intValue = newValue;

            EditorGUI.showMixedValue = false;
        }

        public static int GUIPopup(GUIContent label, SerializedProperty intProp, GUIContent[] options, params GUILayoutOption[] layoutOptions)
        {
            EditorGUI.showMixedValue = intProp.hasMultipleDifferentValues;

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.Popup(rect, null, intProp.intValue, options, ParticleSystemStyles.Get().popup);
            if (EditorGUI.EndChangeCheck())
                intProp.intValue = newValue;

            EditorGUI.showMixedValue = false;
            return intProp.intValue;
        }

        public static int GUIPopup(GUIContent label, int intValue, GUIContent[] options, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);
            return EditorGUI.Popup(rect, intValue, options, ParticleSystemStyles.Get().popup);
        }

        private static Color GetColor(SerializedMinMaxCurve mmCurve)
        {
            return mmCurve.m_Module.m_ParticleSystemUI.m_ParticleEffectUI.GetParticleSystemCurveEditor().GetCurveColor(mmCurve.maxCurve);
        }

        // Should return true to use the mousedown event
        public delegate bool CurveFieldMouseDownCallback(int button, Rect drawRect, Rect curveRanges);

        // returns buttonID if clicked otherwise -1
        private static void GUICurveField(Rect position, SerializedProperty maxCurve, SerializedProperty minCurve, Color color, Rect ranges, CurveFieldMouseDownCallback mouseDownCallback)
        {
            int id = EditorGUIUtility.GetControlID(1321321231, FocusType.Keyboard, position);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                {
                    Rect position2 = position;
                    if (minCurve == null)
                    {
                        EditorGUIUtility.DrawCurveSwatch(position2, null, maxCurve, color, EditorGUI.kCurveBGColor, ranges);
                    }
                    else
                    {
                        EditorGUIUtility.DrawRegionSwatch(position2, maxCurve, minCurve, color, EditorGUI.kCurveBGColor, ranges);
                    }
                    EditorStyles.colorPickerBox.Draw(position2, GUIContent.none, id, false);
                }
                break;
                case EventType.ValidateCommand:
                    if (evt.commandName == "UndoRedoPerformed")
                        AnimationCurvePreviewCache.ClearCache();
                    break;
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition))
                    {
                        if (mouseDownCallback != null && mouseDownCallback(evt.button, position, ranges))
                            evt.Use();
                    }
                    break;
            }
        }

        public static void GUIMinMaxCurve(string label, SerializedMinMaxCurve mmCurve, params GUILayoutOption[] layoutOptions)
        {
            GUIMinMaxCurve(GUIContent.Temp(label), mmCurve, layoutOptions);
        }

        public static void GUIMinMaxCurve(GUIContent label, SerializedMinMaxCurve mmCurve, params GUILayoutOption[] layoutOptions)
        {
            GUIMinMaxCurve(label, mmCurve, null, layoutOptions);
        }

        public static void GUIMinMaxCurve(SerializedProperty editableLabel, SerializedMinMaxCurve mmCurve, params GUILayoutOption[] layoutOptions)
        {
            GUIMinMaxCurve(null, mmCurve, editableLabel, layoutOptions);
        }

        internal static void GUIMinMaxCurve(GUIContent label, SerializedMinMaxCurve mmCurve, SerializedProperty editableLabel, params GUILayoutOption[] layoutOptions)
        {
            bool mixedState = mmCurve.stateHasMultipleDifferentValues;

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            Rect popupRect = GetPopupRect(rect);
            rect = SubtractPopupWidth(rect);
            Rect controlRect;

            if (editableLabel != null)
            {
                Rect labelPosition;
                controlRect = FieldPosition(rect, out labelPosition);
                labelPosition.width -= kSpacingSubLabel;

                // Draw with the minimum width so we don't lose the draggable functionality to the left of the float field
                float textWidth = ParticleSystemStyles.Get().editableLabel.CalcSize(GUIContent.Temp(editableLabel.stringValue)).x;
                labelPosition.width = Mathf.Min(labelPosition.width, textWidth + kSpacingSubLabel);

                EditorGUI.BeginProperty(labelPosition, GUIContent.none, editableLabel);

                EditorGUI.BeginChangeCheck();
                string newString = EditorGUI.TextFieldInternal(GUIUtility.GetControlID(FocusType.Passive, labelPosition), labelPosition, editableLabel.stringValue, ParticleSystemStyles.Get().editableLabel);
                if (EditorGUI.EndChangeCheck())
                    editableLabel.stringValue = newString;

                EditorGUI.EndProperty();
            }
            else
            {
                controlRect = PrefixLabel(rect, label);
            }

            if (mixedState)
            {
                Label(controlRect, GUIContent.Temp("-"));
            }
            else
            {
                MinMaxCurveState state = mmCurve.state;

                // Scalar field
                if (state == MinMaxCurveState.k_Scalar)
                {
                    EditorGUI.BeginChangeCheck();
                    float newValue = FloatDraggable(rect, mmCurve.scalar, mmCurve.m_RemapValue, EditorGUIUtility.labelWidth);
                    if (EditorGUI.EndChangeCheck() && !mmCurve.signedRange)
                        mmCurve.scalar.floatValue = Mathf.Max(newValue, 0f);
                }
                else if (state == MinMaxCurveState.k_TwoScalars)
                {
                    Rect halfRect = controlRect;
                    halfRect.width = (controlRect.width - kDragSpace) * 0.5f;

                    Rect halfRectWithDragger = halfRect;
                    halfRectWithDragger.xMin -= kDragSpace;

                    EditorGUI.BeginChangeCheck();
                    float newMinValue = FloatDraggable(halfRectWithDragger, mmCurve.minScalar, mmCurve.m_RemapValue, kDragSpace, "g5");
                    if (EditorGUI.EndChangeCheck() && !mmCurve.signedRange)
                        mmCurve.minScalar.floatValue = Mathf.Max(newMinValue, 0f);

                    halfRectWithDragger.x += halfRect.width + kDragSpace;

                    EditorGUI.BeginChangeCheck();
                    float newMaxValue = FloatDraggable(halfRectWithDragger, mmCurve.scalar, mmCurve.m_RemapValue, kDragSpace, "g5");
                    if (EditorGUI.EndChangeCheck() && !mmCurve.signedRange)
                        mmCurve.scalar.floatValue = Mathf.Max(newMaxValue, 0f);
                }
                else
                {
                    // Curve field
                    Rect previewRange = mmCurve.signedRange ? kSignedRange : kUnsignedRange;
                    SerializedProperty minCurve = (state == MinMaxCurveState.k_TwoCurves) ? mmCurve.minCurve : null;
                    GUICurveField(controlRect, mmCurve.maxCurve, minCurve, GetColor(mmCurve), previewRange, mmCurve.OnCurveAreaMouseDown);
                }
            }

            // PopUp minmaxState menu
            GUIMMCurveStateList(popupRect, mmCurve);
        }

        public static Rect GUIMinMaxCurveInline(Rect rect, SerializedMinMaxCurve mmCurve, float dragWidth)
        {
            bool mixedState = mmCurve.stateHasMultipleDifferentValues;

            if (mixedState)
            {
                Label(rect, GUIContent.Temp("-"));
            }
            else
            {
                MinMaxCurveState state = mmCurve.state;

                // Scalar field
                if (state == MinMaxCurveState.k_Scalar)
                {
                    EditorGUI.BeginChangeCheck();
                    float newValue = FloatDraggable(rect, mmCurve.scalar, mmCurve.m_RemapValue, dragWidth, "n0");
                    if (EditorGUI.EndChangeCheck() && !mmCurve.signedRange)
                        mmCurve.scalar.floatValue = Mathf.Max(newValue, 0f);
                }
                else if (state == MinMaxCurveState.k_TwoScalars)
                {
                    Rect halfRect = rect;
                    halfRect.width = (rect.width * 0.5f);

                    Rect halfRectWithDragger = halfRect;

                    EditorGUI.BeginChangeCheck();
                    float newMinValue = FloatDraggable(halfRectWithDragger, mmCurve.minScalar, mmCurve.m_RemapValue, dragWidth, "n0");
                    if (EditorGUI.EndChangeCheck() && !mmCurve.signedRange)
                        mmCurve.minScalar.floatValue = Mathf.Max(newMinValue, 0f);

                    halfRectWithDragger.x += halfRect.width;

                    EditorGUI.BeginChangeCheck();
                    float newMaxValue = FloatDraggable(halfRectWithDragger, mmCurve.scalar, mmCurve.m_RemapValue, dragWidth, "n0");
                    if (EditorGUI.EndChangeCheck() && !mmCurve.signedRange)
                        mmCurve.scalar.floatValue = Mathf.Max(newMaxValue, 0f);
                }
                else
                {
                    // Curve field
                    Rect previewRange = mmCurve.signedRange ? kSignedRange : kUnsignedRange;
                    SerializedProperty minCurve = (state == MinMaxCurveState.k_TwoCurves) ? mmCurve.minCurve : null;
                    GUICurveField(rect, mmCurve.maxCurve, minCurve, GetColor(mmCurve), previewRange, mmCurve.OnCurveAreaMouseDown);
                }
            }

            // PopUp minmaxState menu
            rect.width += k_minMaxToggleWidth;
            Rect popupRect = GetPopupRect(rect);
            GUIMMCurveStateList(popupRect, mmCurve);

            return rect;
        }

        public void GUIMinMaxGradient(GUIContent label, SerializedMinMaxGradient minMaxGradient, bool hdr, params GUILayoutOption[] layoutOptions)
        {
            GUIMinMaxGradient(label, minMaxGradient, null, hdr, layoutOptions);
        }

        public void GUIMinMaxGradient(SerializedProperty editableLabel, SerializedMinMaxGradient minMaxGradient, bool hdr, params GUILayoutOption[] layoutOptions)
        {
            GUIMinMaxGradient(null, minMaxGradient, editableLabel, hdr, layoutOptions);
        }

        internal void GUIMinMaxGradient(GUIContent label, SerializedMinMaxGradient minMaxGradient, SerializedProperty editableLabel, bool hdr, params GUILayoutOption[] layoutOptions)
        {
            bool mixedState = minMaxGradient.stateHasMultipleDifferentValues;

            MinMaxGradientState state = minMaxGradient.state;
            bool useRandomness = !mixedState && ((state == MinMaxGradientState.k_RandomBetweenTwoColors) || (state == MinMaxGradientState.k_RandomBetweenTwoGradients));

            Rect rect = GUILayoutUtility.GetRect(0, useRandomness ? 2 * kSingleLineHeight : kSingleLineHeight, layoutOptions);
            Rect popupRect = GetPopupRect(rect);
            rect = SubtractPopupWidth(rect);

            Rect gradientRect;
            if (editableLabel != null)
            {
                Rect labelPosition;
                gradientRect = FieldPosition(rect, out labelPosition);
                labelPosition.width -= kSpacingSubLabel;

                EditorGUI.BeginProperty(labelPosition, GUIContent.none, editableLabel);

                EditorGUI.BeginChangeCheck();
                string newString = EditorGUI.TextFieldInternal(GUIUtility.GetControlID(FocusType.Passive, labelPosition), labelPosition, editableLabel.stringValue, ParticleSystemStyles.Get().editableLabel);
                if (EditorGUI.EndChangeCheck())
                    editableLabel.stringValue = newString;

                EditorGUI.EndProperty();
            }
            else
            {
                gradientRect = PrefixLabel(rect, label);
            }

            gradientRect.height = kSingleLineHeight;

            if (mixedState)
            {
                Label(gradientRect, GUIContent.Temp("-"));
            }
            else
            {
                switch (state)
                {
                    case MinMaxGradientState.k_Color:
                        EditorGUI.showMixedValue = minMaxGradient.m_MaxColor.hasMultipleDifferentValues;
                        GUIColor(gradientRect, minMaxGradient.m_MaxColor, hdr);
                        EditorGUI.showMixedValue = false;
                        break;

                    case MinMaxGradientState.k_RandomBetweenTwoColors:
                        EditorGUI.showMixedValue = minMaxGradient.m_MaxColor.hasMultipleDifferentValues;
                        GUIColor(gradientRect, minMaxGradient.m_MaxColor, hdr);
                        EditorGUI.showMixedValue = false;

                        gradientRect.y += gradientRect.height;

                        EditorGUI.showMixedValue = minMaxGradient.m_MinColor.hasMultipleDifferentValues;
                        GUIColor(gradientRect, minMaxGradient.m_MinColor, hdr);
                        EditorGUI.showMixedValue = false;
                        break;

                    case MinMaxGradientState.k_Gradient:
                    case MinMaxGradientState.k_RandomColor:
                        EditorGUI.showMixedValue = minMaxGradient.m_MaxGradient.hasMultipleDifferentValues;
                        EditorGUI.GradientField(gradientRect, minMaxGradient.m_MaxGradient, hdr);
                        EditorGUI.showMixedValue = false;
                        break;

                    case MinMaxGradientState.k_RandomBetweenTwoGradients:
                        EditorGUI.showMixedValue = minMaxGradient.m_MaxGradient.hasMultipleDifferentValues;
                        EditorGUI.GradientField(gradientRect, minMaxGradient.m_MaxGradient, hdr);
                        EditorGUI.showMixedValue = false;

                        gradientRect.y += gradientRect.height;

                        EditorGUI.showMixedValue = minMaxGradient.m_MinGradient.hasMultipleDifferentValues;
                        EditorGUI.GradientField(gradientRect, minMaxGradient.m_MinGradient, hdr);
                        EditorGUI.showMixedValue = false;
                        break;
                }
            }

            GUIMMGradientPopUp(popupRect, minMaxGradient);
        }

        private static void GUIColor(Rect rect, SerializedProperty colorProp)
        {
            GUIColor(rect, colorProp, false);
        }

        private static void GUIColor(Rect rect, SerializedProperty colorProp, bool hdr)
        {
            EditorGUI.BeginChangeCheck();
            Color newValue = EditorGUI.ColorField(rect, GUIContent.none, colorProp.colorValue, false, true, hdr, ColorPicker.defaultHDRConfig);
            if (EditorGUI.EndChangeCheck())
                colorProp.colorValue = newValue;
        }

        public void GUITripleMinMaxCurve(GUIContent label, GUIContent x, SerializedMinMaxCurve xCurve, GUIContent y, SerializedMinMaxCurve yCurve, GUIContent z, SerializedMinMaxCurve zCurve, SerializedProperty randomizePerFrame, params GUILayoutOption[] layoutOptions)
        {
            bool mixedState = xCurve.stateHasMultipleDifferentValues;

            MinMaxCurveState state = xCurve.state; // just use xCurve state for all
            bool showMainLabel = label != GUIContent.none;
            int numLines = (showMainLabel) ? 2 : 1;
            if (state == MinMaxCurveState.k_TwoScalars)
                numLines++;
            Rect rect = GetControlRect(kSingleLineHeight * numLines, layoutOptions);
            Rect popupRect = GetPopupRect(rect);
            rect = SubtractPopupWidth(rect);
            Rect r = rect;

            float elementWidth = (rect.width - 2 * kSpacingSubLabel) / 3f;

            if (numLines > 1)
            {
                r.height = kSingleLineHeight;
            }
            if (showMainLabel)
            {
                PrefixLabel(rect, label);
                r.y += r.height;
            }
            r.width = elementWidth;

            // Scalar fields
            GUIContent[] labels = { x, y, z };
            SerializedMinMaxCurve[] curves = {xCurve, yCurve, zCurve};

            if (mixedState)
            {
                Label(r, GUIContent.Temp("-"));
            }
            else
            {
                if (state == MinMaxCurveState.k_Scalar)
                {
                    for (int i = 0; i < curves.Length; ++i)
                    {
                        Label(r, labels[i]);
                        EditorGUI.BeginChangeCheck();
                        float newValue = FloatDraggable(r, curves[i].scalar, curves[i].m_RemapValue, kSubLabelWidth);
                        if (EditorGUI.EndChangeCheck() && !curves[i].signedRange)
                            curves[i].scalar.floatValue = Mathf.Max(newValue, 0f);

                        r.x += elementWidth + kSpacingSubLabel;
                    }
                }
                else if (state == MinMaxCurveState.k_TwoScalars)
                {
                    for (int i = 0; i < curves.Length; ++i)
                    {
                        Label(r, labels[i]);

                        float minConstant = curves[i].minConstant;
                        float maxConstant = curves[i].maxConstant;

                        EditorGUI.BeginChangeCheck();
                        maxConstant = FloatDraggable(r, maxConstant, curves[i].m_RemapValue, kSubLabelWidth, "g5");
                        if (EditorGUI.EndChangeCheck())
                        {
                            curves[i].maxConstant = maxConstant;
                        }

                        r.y += kSingleLineHeight;

                        EditorGUI.BeginChangeCheck();
                        minConstant = FloatDraggable(r, minConstant, curves[i].m_RemapValue, kSubLabelWidth, "g5");
                        if (EditorGUI.EndChangeCheck())
                        {
                            curves[i].minConstant = minConstant;
                        }

                        r.x += elementWidth + kSpacingSubLabel;
                        r.y -= kSingleLineHeight;
                    }
                }
                else
                {
                    r.width = elementWidth;
                    Rect previewRange = xCurve.signedRange ? kSignedRange : kUnsignedRange;
                    for (int i = 0; i < curves.Length; ++i)
                    {
                        Label(r, labels[i]);
                        Rect r2 = r;
                        r2.xMin += kSubLabelWidth;
                        SerializedProperty minCurve = (state == MinMaxCurveState.k_TwoCurves) ? curves[i].minCurve : null;
                        GUICurveField(r2, curves[i].maxCurve, minCurve, GetColor(curves[i]), previewRange, curves[i].OnCurveAreaMouseDown);
                        r.x += elementWidth + kSpacingSubLabel;
                    }
                }
            }

            // Toggle minmax
            GUIMMCurveStateList(popupRect, curves);
        }

        private class CurveStateCallbackData
        {
            public CurveStateCallbackData(MinMaxCurveState state, SerializedMinMaxCurve[] curves)
            {
                minMaxCurves = curves;
                selectedState = state;
            }

            public SerializedMinMaxCurve[] minMaxCurves;
            public MinMaxCurveState selectedState;
        }

        static void SelectMinMaxCurveStateCallback(object obj)
        {
            CurveStateCallbackData data = (CurveStateCallbackData)obj;
            foreach (SerializedMinMaxCurve curve in data.minMaxCurves)
            {
                curve.state = data.selectedState;
            }
        }

        public static void GUIMMCurveStateList(Rect rect, SerializedMinMaxCurve minMaxCurves)
        {
            SerializedMinMaxCurve[] curve = { minMaxCurves };
            GUIMMCurveStateList(rect, curve);
        }

        public static void GUIMMCurveStateList(Rect rect, SerializedMinMaxCurve[] minMaxCurves)
        {
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Passive, ParticleSystemStyles.Get().minMaxCurveStateDropDown))
            {
                if (minMaxCurves.Length == 0)
                    return;

                GUIContent[] texts =        {   new GUIContent("Constant"),
                                                new GUIContent("Curve"),
                                                new GUIContent("Random Between Two Constants"),
                                                new GUIContent("Random Between Two Curves") };
                MinMaxCurveState[] states = {   MinMaxCurveState.k_Scalar,
                                                MinMaxCurveState.k_Curve,
                                                MinMaxCurveState.k_TwoScalars,
                                                MinMaxCurveState.k_TwoCurves };
                bool[] allowState =         {   minMaxCurves[0].m_AllowConstant,
                                                minMaxCurves[0].m_AllowCurves,
                                                minMaxCurves[0].m_AllowRandom,
                                                minMaxCurves[0].m_AllowRandom && minMaxCurves[0].m_AllowCurves };

                bool allowHighlight = !minMaxCurves[0].stateHasMultipleDifferentValues;
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < texts.Length; ++i)
                {
                    if (allowState[i])
                        menu.AddItem(texts[i], allowHighlight && (minMaxCurves[0].state == states[i]), SelectMinMaxCurveStateCallback, new CurveStateCallbackData(states[i], minMaxCurves));
                }
                menu.DropDown(rect);
                Event.current.Use();
            }
        }

        private class GradientCallbackData
        {
            public GradientCallbackData(MinMaxGradientState state, SerializedMinMaxGradient p)
            {
                gradientProp = p;
                selectedState = state;
            }

            public SerializedMinMaxGradient gradientProp;
            public MinMaxGradientState selectedState;
        }

        static void SelectMinMaxGradientStateCallback(object obj)
        {
            GradientCallbackData data = (GradientCallbackData)obj;
            data.gradientProp.state = data.selectedState;
        }

        public static void GUIMMGradientPopUp(Rect rect, SerializedMinMaxGradient gradientProp)
        {
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Passive, ParticleSystemStyles.Get().minMaxCurveStateDropDown))
            {
                GUIContent[] texts = {  new GUIContent("Color"),
                                        new GUIContent("Gradient"),
                                        new GUIContent("Random Between Two Colors"),
                                        new GUIContent("Random Between Two Gradients"),
                                        new GUIContent("Random Color")};
                MinMaxGradientState[] states = {    MinMaxGradientState.k_Color,
                                                    MinMaxGradientState.k_Gradient,
                                                    MinMaxGradientState.k_RandomBetweenTwoColors,
                                                    MinMaxGradientState.k_RandomBetweenTwoGradients,
                                                    MinMaxGradientState.k_RandomColor};
                bool[] allowState = {   gradientProp.m_AllowColor,
                                        gradientProp.m_AllowGradient,
                                        gradientProp.m_AllowRandomBetweenTwoColors,
                                        gradientProp.m_AllowRandomBetweenTwoGradients,
                                        gradientProp.m_AllowRandomColor};

                bool allowHighlight = !gradientProp.stateHasMultipleDifferentValues;
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < texts.Length; ++i)
                {
                    if (allowState[i])
                        menu.AddItem(texts[i], allowHighlight && (gradientProp.state == states[i]), SelectMinMaxGradientStateCallback, new GradientCallbackData(states[i], gradientProp));
                }
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private class ColorCallbackData
        {
            public ColorCallbackData(bool state, SerializedProperty bp)
            {
                boolProp = bp;
                selectedState = state;
            }

            public SerializedProperty boolProp;
            public bool selectedState;
        }

        static void SelectMinMaxColorStateCallback(object obj)
        {
            ColorCallbackData data = (ColorCallbackData)obj;
            data.boolProp.boolValue = data.selectedState;
        }

        public static void GUIMMColorPopUp(Rect rect, SerializedProperty boolProp)
        {
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Passive, ParticleSystemStyles.Get().minMaxCurveStateDropDown))
            {
                GenericMenu menu = new GenericMenu();
                GUIContent[] texts = { new GUIContent("Constant Color"), new GUIContent("Random Between Two Colors") };
                bool[] states = { false, true };

                for (int i = 0; i < texts.Length; ++i)
                {
                    menu.AddItem(texts[i], (boolProp.boolValue == states[i]), SelectMinMaxColorStateCallback, new ColorCallbackData(states[i], boolProp));
                }
                menu.ShowAsContext();
                Event.current.Use();
            }
        }
    } // end class ModuleUI
} // namespace UnityEditor
