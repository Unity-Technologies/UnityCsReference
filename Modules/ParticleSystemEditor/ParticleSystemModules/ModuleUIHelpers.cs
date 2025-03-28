// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Rendering;
using Enum = System.Enum;
using GetBoundsFunc = System.Func<UnityEngine.Bounds>;
using Object = UnityEngine.Object;
using System.Linq;

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
        static public float k_CompactFixedModuleWidth = 400f;
        static public float k_SpaceBetweenModules = 5;

        public static readonly GUIStyle s_ControlRectStyle = new GUIStyle { margin = new RectOffset(0, 0, 2, 2) };
        public static readonly GUIContent s_AddItem = EditorGUIUtility.TrTextContent(string.Empty, "Add Item");
        public static readonly GUIContent s_RemoveItem = EditorGUIUtility.TrTextContent(string.Empty, "Remove Item");

        // Alternative to BeginProperty when dealing with properties that should be logically combined but are not parent child properties.
        public class PropertyGroupScope : GUI.Scope
        {
            bool wasBoldFont;

            public PropertyGroupScope(params SerializedProperty[] properties)
            {
                wasBoldFont = EditorGUIUtility.GetBoldDefaultFont();

                bool bold = false;
                foreach (var serializedProperty in properties)
                {
                    if (serializedProperty.serializedObject.targetObjectsCount == 1 && serializedProperty.isInstantiatedPrefab)
                    {
                        if (serializedProperty.prefabOverride)
                        {
                            bold = true;
                            break;
                        }
                    }
                }
                EditorGUIUtility.SetBoldDefaultFont(bold);
            }

            protected override void CloseScope()
            {
                EditorGUIUtility.SetBoldDefaultFont(wasBoldFont);
            }
        }

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

        internal static Rect PrefixLabel(Rect totalPosition, GUIContent label)
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
            return GUI.Button(new Rect(position.x - 2, position.y - 2, 12, 13),  s_AddItem, "OL Plus");
        }

        protected static bool MinusButton(Rect position)
        {
            return GUI.Button(new Rect(position.x - 2, position.y - 2, 12, 13), s_RemoveItem, "OL Minus");
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
            Rect r = GetControlRect(kSingleLineHeight, layoutOptions);
            guiContent = EditorGUI.BeginProperty(r, guiContent, vecProp);
            r = PrefixLabel(r, guiContent);

            GUIContent[] labels = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
            float elementWidth = (r.width - 2 * kSpacingSubLabel) / 3f;
            r.width = elementWidth;

            SerializedProperty cur = vecProp.Copy();
            cur.Next(true);

            Vector3 vec = vecProp.vector3Value;

            for (int i = 0; i < 3; ++i)
            {
                EditorGUI.BeginProperty(r, GUIContent.none, cur);
                Label(r, labels[i]);
                EditorGUI.BeginChangeCheck();
                float newValue = FloatDraggable(r, cur.floatValue, 1.0f, 25.0f, "g5");
                if (EditorGUI.EndChangeCheck())
                    cur.floatValue = newValue;
                EditorGUI.EndProperty();
                cur.Next(false);
                r.x += elementWidth + kSpacingSubLabel;
            }

            EditorGUI.EndProperty();
            return vec;
        }

        public static Vector2 GUIVector2Field(GUIContent guiContent, SerializedProperty vecProp, params GUILayoutOption[] layoutOptions)
        {
            Rect r = GetControlRect(kSingleLineHeight, layoutOptions);
            guiContent = EditorGUI.BeginProperty(r, guiContent, vecProp);
            r = PrefixLabel(r, guiContent);

            GUIContent[] labels = { new GUIContent("X"), new GUIContent("Y") };
            float elementWidth = (r.width - 2 * kSpacingSubLabel) / 2f;
            r.width = elementWidth;

            SerializedProperty cur = vecProp.Copy();
            cur.Next(true);

            Vector2 vec = vecProp.vector2Value;

            for (int i = 0; i < 2; ++i)
            {
                EditorGUI.BeginProperty(r, GUIContent.none, cur);
                Label(r, labels[i]);
                EditorGUI.BeginChangeCheck();
                float newValue = FloatDraggable(r, cur.floatValue, 1.0f, 25.0f, "g5");
                if (EditorGUI.EndChangeCheck())
                    cur.floatValue = newValue;
                EditorGUI.EndProperty();
                cur.Next(false);
                r.x += elementWidth + kSpacingSubLabel;
            }

            EditorGUI.EndProperty();
            return vec;
        }

        public static float GUIFloat(GUIContent guiContent, SerializedProperty floatProp, params GUILayoutOption[] layoutOptions)
        {
            return GUIFloat(guiContent, floatProp, kFormatString, layoutOptions);
        }

        public static float GUIFloat(GUIContent guiContent, SerializedProperty floatProp, string formatString, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            return GUIFloat(rect, guiContent, floatProp, formatString);
        }

        public static float GUIFloat(Rect rect, GUIContent guiContent, SerializedProperty floatProp, string formatString = kFormatString)
        {
            guiContent = EditorGUI.BeginProperty(rect, guiContent, floatProp);
            PrefixLabel(rect, guiContent);
            float val = FloatDraggable(rect, floatProp, 1f, EditorGUIUtility.labelWidth, formatString);
            EditorGUI.EndProperty();
            return val;
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

        public static void GUISortingLayerField(GUIContent guiContent, SerializedProperty sortProperty, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            var label = EditorGUI.BeginProperty(rect, guiContent, sortProperty);
            EditorGUI.SortingLayerField(rect, label, sortProperty, ParticleSystemStyles.Get().popup, ParticleSystemStyles.Get().label);
            EditorGUI.EndProperty();
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
            guiContent = EditorGUI.BeginProperty(rect, guiContent, boolProp);
            rect = PrefixLabel(rect, guiContent);
            bool toggleValue = Toggle(rect, boolProp);
            EditorGUI.EndProperty();
            return toggleValue;
        }

        public static void GUILayerMask(GUIContent guiContent, SerializedProperty layerMaskProp, params GUILayoutOption[] layoutOptions)
        {
            string[] m_FlagNames = new string[0];
            int[] m_FlagValues = new int[0];
            TagManager.GetDefinedLayers(ref m_FlagNames, ref m_FlagValues);
            MaskFieldGUI.GetMaskButtonValue(layerMaskProp.intValue, m_FlagNames, m_FlagValues, out var toggleLabel, out var toggleLabelMixed);

            // the returned rect is off by two pixels so we adjust its position manually
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect.y -= 2;

            // adjusting the rect further to have proper alignment
            rect = EditorGUI.PrefixLabel(rect, guiContent, ParticleSystemStyles.Get().label);
            rect.width += 2;
            rect.x -= 2;
            rect.y += 2;

            var toggleContent = layerMaskProp.hasMultipleDifferentValues ? EditorGUI.mixedValueContent : MaskFieldGUI.DoMixedLabel(toggleLabel, toggleLabelMixed, rect, ParticleSystemStyles.Get().popup);
            bool toggled = EditorGUI.DropdownButton(rect, toggleContent, FocusType.Keyboard, ParticleSystemStyles.Get().popup);
            if (toggled)
            {
                PopupWindow.Show(rect, new MaskFieldDropDown(layerMaskProp));
                GUIUtility.ExitGUI();
            }
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
            guiContent = EditorGUI.BeginProperty(rect, guiContent, boolProp);
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
            EditorGUI.EndProperty();
        }

        public static void GUIToggleWithIntField(string name, SerializedProperty boolProp, SerializedProperty floatProp, bool invertToggle, params GUILayoutOption[] layoutOptions)
        {
            GUIToggleWithIntField(EditorGUIUtility.TempContent(name), boolProp, floatProp, invertToggle, layoutOptions);
        }

        public static void GUIToggleWithIntField(GUIContent guiContent, SerializedProperty boolProp, SerializedProperty intProp, bool invertToggle, params GUILayoutOption[] layoutOptions)
        {
            Rect lineRect = GetControlRect(kSingleLineHeight, layoutOptions);
            guiContent = EditorGUI.BeginProperty(lineRect, guiContent, boolProp);
            Rect labelRect = PrefixLabel(lineRect, guiContent);

            Rect toggleRect = labelRect;
            toggleRect.xMax = toggleRect.x + k_toggleWidth;
            bool toggleValue = Toggle(toggleRect, boolProp);
            toggleValue = invertToggle ? !toggleValue : toggleValue;

            if (toggleValue)
            {
                float dragWidth = 25f;
                Rect intDragRect = new Rect(toggleRect.xMax, lineRect.y, lineRect.width - toggleRect.xMax + k_toggleWidth, lineRect.height);
                EditorGUI.BeginChangeCheck();
                int newValue = IntDraggable(intDragRect, null, intProp.intValue, dragWidth);
                if (EditorGUI.EndChangeCheck())
                    intProp.intValue = newValue;
            }
            EditorGUI.EndProperty();
        }

        public static void GUIObject(GUIContent label, SerializedProperty objectProp, params GUILayoutOption[] layoutOptions)
        {
            GUIObject(label, objectProp, null, layoutOptions);
        }

        public static void GUIObject(GUIContent label, SerializedProperty objectProp, System.Type objType, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            GUIObject(rect, label, objectProp, objType);
        }

        public static void GUIObject(Rect rect, GUIContent label, SerializedProperty objectProp, System.Type objType)
        {
            label = EditorGUI.BeginProperty(rect, label, objectProp);
            rect = PrefixLabel(rect, label);
            EditorGUI.ObjectField(rect, objectProp, objType, GUIContent.none, ParticleSystemStyles.Get().objectField);
            EditorGUI.EndProperty();
        }

        public static void GUIObjectFieldAndToggle(GUIContent label, SerializedProperty objectProp, SerializedProperty boolProp, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            label = EditorGUI.BeginProperty(rect, label, objectProp);
            rect = PrefixLabel(rect, label);

            rect.xMax -= k_toggleWidth + 10;
            EditorGUI.ObjectField(rect, objectProp, GUIContent.none);

            if (boolProp != null)
            {
                rect.x += rect.width + 10;
                rect.width = k_toggleWidth;
                Toggle(rect, boolProp);
            }

            EditorGUI.EndProperty();
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

        public void GUIListOfObjectFields(GUIContent label, SerializedProperty[] objectProps, SerializedProperty[] additionalProps, params GUILayoutOption[] layoutOptions)
        {
            int numObjects = objectProps.Length;
            Rect rect = GUILayoutUtility.GetRect(0, (kSingleLineHeight + 2) * numObjects, layoutOptions);
            rect.height = kSingleLineHeight;

            var propsForScope = (additionalProps != null) ? objectProps.Concat(additionalProps).ToArray() : objectProps;
            using (new PropertyGroupScope(propsForScope))
            {
                PrefixLabel(rect, label);

                float indent = EditorGUIUtility.labelWidth;
                float objectFieldWidth = rect.width - indent;

                for (int i = 0; i < numObjects; ++i)
                {
                    Rect r2 = new Rect(rect.x + indent, rect.y, objectFieldWidth, rect.height);
                    Rect r3 = r2;

                    int id = GUIUtility.GetControlID(1235498, FocusType.Keyboard, r2);

                    if (additionalProps != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        r2.width -= 60.0f;
                        r3.width = 55.0f;
                        r3.x = r2.xMax + 5.0f;
                    }

                    SerializedProperty objectProp = objectProps[i];
                    EditorGUI.BeginProperty(r2, GUIContent.none, objectProp);
                    EditorGUI.DoObjectField(r2, r2, id, null, objectProp, null, true, ParticleSystemStyles.Get().objectField);
                    EditorGUI.EndProperty();

                    if (additionalProps != null)
                    {
                        SerializedProperty additionalProp = additionalProps[i];
                        EditorGUI.BeginProperty(r3, GUIContent.none, additionalProp);
                        FloatDraggable(r3, additionalProp, 1.0f, 10.0f);
                        EditorGUI.EndProperty();

                        EditorGUILayout.EndHorizontal();
                    }

                    rect.y += kSingleLineHeight + 2;
                }
            }
        }

        public static void GUIIntDraggableX2(GUIContent mainLabel, GUIContent label1, SerializedProperty intProp1, GUIContent label2, SerializedProperty intProp2, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);

            using (new PropertyGroupScope(intProp1, intProp2))
            {
                rect = PrefixLabel(rect, mainLabel);
            }

            float room = (rect.width - kSpacingSubLabel) * 0.5f;
            Rect rectProp = new Rect(rect.x, rect.y, room, rect.height);
            IntDraggable(rectProp, label1, intProp1, kSubLabelWidth);
            rectProp.x += room + kSpacingSubLabel;
            IntDraggable(rectProp, label2, intProp2, kSubLabelWidth);
        }

        public static int IntDraggable(Rect rect, GUIContent label, SerializedProperty intProp, float dragWidth)
        {
            label = EditorGUI.BeginProperty(rect, label, intProp);

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

            guiContent = EditorGUI.BeginProperty(rect, guiContent, intProp);
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
            return EditorGUI.DoIntField(EditorGUI.s_RecycledEditor, intFieldRect, dragZoneRect, id, value, EditorGUI.kIntFieldFormatString, ParticleSystemStyles.Get().numberField, true, dragSensitity);
        }

        public static void GUIMinMaxRange(GUIContent label, SerializedProperty vec2Prop, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            label = EditorGUI.BeginProperty(rect, label, vec2Prop);
            rect = SubtractPopupWidth(rect); // There is actually no popup in this control but the layout is nicer if the fields line up.
            rect = PrefixLabel(rect, label);

            float floatFieldWidth = (rect.width - kDragSpace) * 0.5f;

            SerializedProperty cur = vec2Prop.Copy();
            cur.Next(true);

            EditorGUI.BeginChangeCheck();

            rect.width = floatFieldWidth;
            rect.xMin -= kDragSpace;
            FloatDraggable(rect, cur, 1f, kDragSpace, kFormatString);
            cur.Next(true);

            rect.x += floatFieldWidth + kDragSpace;
            FloatDraggable(rect, cur, 1f, kDragSpace, kFormatString);
            cur.Next(true);

            EditorGUI.EndProperty();
        }

        public static bool GUIBoolAsPopup(GUIContent label, SerializedProperty boolProp, string[] options, params GUILayoutOption[] layoutOptions)
        {
            System.Diagnostics.Debug.Assert(options.Length == 2);

            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            label = EditorGUI.BeginProperty(rect, label, boolProp);
            rect = PrefixLabel(rect, label);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.Popup(rect, null, boolProp.boolValue ? 1 : 0, EditorGUIUtility.TempContent(options), ParticleSystemStyles.Get().popup);
            if (EditorGUI.EndChangeCheck())
                boolProp.boolValue = newValue > 0;

            EditorGUI.EndProperty();
            return newValue > 0;
        }

        public static void GUIEnumMaskUVChannelFlags(GUIContent label, SerializedProperty enumProperty, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            label = EditorGUI.BeginProperty(rect, label, enumProperty);
            rect = PrefixLabel(rect, label);
            EditorGUI.BeginChangeCheck();
            int enumVal = (int)(UVChannelFlags)EditorGUI.EnumFlagsField(rect, (UVChannelFlags)enumProperty.intValue, ParticleSystemStyles.Get().popup);
            if (EditorGUI.EndChangeCheck())
                enumProperty.intValue = enumVal;
            EditorGUI.EndProperty();
        }

        public static void GUIMask(GUIContent label, SerializedProperty intProp, string[] options, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            GUIMask(rect, label, intProp, options);
        }

        public static void GUIMask(Rect rect, GUIContent label, SerializedProperty intProp, string[] options)
        {
            label = EditorGUI.BeginProperty(rect, label, intProp);
            rect = PrefixLabel(rect, label);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.MaskField(rect, intProp.intValue, options, ParticleSystemStyles.Get().popup);
            if (EditorGUI.EndChangeCheck())
                intProp.intValue = newValue;

            EditorGUI.EndProperty();
        }

        public static int GUIPopup(GUIContent label, SerializedProperty intProp, GUIContent[] options, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            return GUIPopup(rect, label, intProp, options);
        }

        public static int GUIPopup(Rect rect, GUIContent label, SerializedProperty intProp, GUIContent[] options)
        {
            label = EditorGUI.BeginProperty(rect, label, intProp);
            rect = PrefixLabel(rect, label);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.Popup(rect, null, intProp.intValue, options, ParticleSystemStyles.Get().popup);
            if (EditorGUI.EndChangeCheck())
                intProp.intValue = newValue;

            EditorGUI.EndProperty();
            return newValue;
        }

        public static int GUIPopup(GUIContent label, int intValue, GUIContent[] options, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            rect = PrefixLabel(rect, label);
            var index = EditorGUI.Popup(rect, intValue, options, ParticleSystemStyles.Get().popup);
            return index;
        }

        public static int GUIPopup(GUIContent label, int intValue, GUIContent[] options, SerializedProperty property, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            label = EditorGUI.BeginProperty(rect, label, property);
            rect = PrefixLabel(rect, label);
            var index = EditorGUI.Popup(rect, intValue, options, ParticleSystemStyles.Get().popup);
            EditorGUI.EndProperty();
            return index;
        }

        public static Enum GUIEnumPopup(GUIContent label, Enum selected, SerializedProperty property, params GUILayoutOption[] layoutOptions)
        {
            Rect rect = GetControlRect(kSingleLineHeight, layoutOptions);
            label = EditorGUI.BeginProperty(rect, label, property);
            rect = PrefixLabel(rect, label);
            var e = EditorGUI.EnumPopup(rect, selected, ParticleSystemStyles.Get().popup);
            EditorGUI.EndProperty();
            return e;
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
                    if (evt.commandName == EventCommandNames.UndoRedoPerformed)
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
            label = EditorGUI.BeginProperty(rect, label, mmCurve.rootProperty);
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
            EditorGUI.EndProperty();
        }

        public static Rect GUIMinMaxCurveInline(Rect rect, SerializedMinMaxCurve mmCurve, float dragWidth)
        {
            EditorGUI.BeginProperty(rect, GUIContent.none, mmCurve.rootProperty);
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
            EditorGUI.EndProperty();

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
            label = EditorGUI.BeginProperty(rect, label, minMaxGradient.m_RootProperty);
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
                        // The color field looks too large compared to the other fields because it does not have a border.
                        //We will shrink it to give it a border of 1 pixel from the bacground.
                        gradientRect.height -= 2;
                        gradientRect.y++;
                        GUIColor(gradientRect, minMaxGradient.m_MaxColor, hdr);
                        break;

                    case MinMaxGradientState.k_RandomBetweenTwoColors:
                        // The color field looks too large compared to the other fields because it does not have a border.
                        //We will shrink it to give it a border of 1 pixel from the bacground.
                        gradientRect.height -= 2;
                        gradientRect.y++;

                        GUIColor(gradientRect, minMaxGradient.m_MaxColor, hdr);

                        gradientRect.y += gradientRect.height + 2.0f;

                        GUIColor(gradientRect, minMaxGradient.m_MinColor, hdr);
                        break;

                    case MinMaxGradientState.k_Gradient:
                    case MinMaxGradientState.k_RandomColor:
                        EditorGUI.BeginProperty(gradientRect, GUIContent.none, minMaxGradient.m_MaxGradient);
                        EditorGUI.GradientField(gradientRect, minMaxGradient.m_MaxGradient, hdr);
                        EditorGUI.EndProperty();
                        break;

                    case MinMaxGradientState.k_RandomBetweenTwoGradients:
                        EditorGUI.BeginProperty(gradientRect, GUIContent.none, minMaxGradient.m_MaxGradient);
                        EditorGUI.GradientField(gradientRect, minMaxGradient.m_MaxGradient, hdr);
                        EditorGUI.EndProperty();

                        gradientRect.y += gradientRect.height;

                        EditorGUI.BeginProperty(gradientRect, GUIContent.none, minMaxGradient.m_MinGradient);
                        EditorGUI.GradientField(gradientRect, minMaxGradient.m_MinGradient, hdr);
                        EditorGUI.EndProperty();
                        break;
                }
            }
            GUIMMGradientPopUp(popupRect, minMaxGradient);
            EditorGUI.EndProperty();
        }

        private static void GUIColor(Rect rect, SerializedProperty colorProp)
        {
            GUIColor(rect, colorProp, false);
        }

        private static void GUIColor(Rect rect, SerializedProperty colorProp, bool hdr)
        {
            EditorGUI.BeginProperty(rect, GUIContent.none, colorProp);
            EditorGUI.BeginChangeCheck();
            Color newValue = EditorGUI.ColorField(rect, GUIContent.none, colorProp.colorValue, false, true, hdr);
            if (EditorGUI.EndChangeCheck())
                colorProp.colorValue = newValue;
            EditorGUI.EndProperty();
        }

        public void GUITripleMinMaxCurve(GUIContent label, GUIContent x, SerializedMinMaxCurve xCurve, GUIContent y, SerializedMinMaxCurve yCurve, GUIContent z, SerializedMinMaxCurve zCurve, SerializedProperty randomizePerFrame, params GUILayoutOption[] layoutOptions)
        {
            using (new PropertyGroupScope(xCurve.rootProperty, yCurve.rootProperty, zCurve.rootProperty))
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

                float labelBorder = 2;
                float[] labelWidth = new float[3]
                {
                    ParticleSystemStyles.Get().label.CalcSize(GUIContent.Temp(x.text)).x + labelBorder,
                    ParticleSystemStyles.Get().label.CalcSize(GUIContent.Temp(y.text)).x + labelBorder,
                    ParticleSystemStyles.Get().label.CalcSize(GUIContent.Temp(z.text)).x + labelBorder
                };

                float fieldWidth = (rect.width - labelWidth[0] - labelWidth[1] - labelWidth[2]) / 3.0f;

                if (numLines > 1)
                {
                    r.height = kSingleLineHeight;
                }
                if (showMainLabel)
                {
                    PrefixLabel(rect, label);
                    r.y += r.height;
                }

                // Scalar fields
                GUIContent[] labels = { x, y, z };
                SerializedMinMaxCurve[] curves = { xCurve, yCurve, zCurve };

                if (mixedState)
                {
                    r.width = fieldWidth + labelWidth[0];
                    Label(r, GUIContent.Temp("-"));
                }
                else
                {
                    if (state == MinMaxCurveState.k_Scalar)
                    {
                        for (int i = 0; i < curves.Length; ++i)
                        {
                            r.width = fieldWidth + labelWidth[i] - labelBorder * 2;

                            EditorGUI.BeginProperty(r, labels[i], curves[i].scalar);
                            Label(r, labels[i]);
                            EditorGUI.BeginChangeCheck();
                            float newValue = FloatDraggable(r, curves[i].scalar, curves[i].m_RemapValue, labelWidth[i]);
                            if (EditorGUI.EndChangeCheck() && !curves[i].signedRange)
                                curves[i].scalar.floatValue = Mathf.Max(newValue, 0f);

                            r.x += fieldWidth + labelWidth[i] + labelBorder;
                            EditorGUI.EndProperty();
                        }
                    }
                    else if (state == MinMaxCurveState.k_TwoScalars)
                    {
                        for (int i = 0; i < curves.Length; ++i)
                        {
                            r.width = fieldWidth + labelWidth[i] - labelBorder * 2;

                            Label(r, labels[i]);

                            float minConstant = curves[i].minConstant;
                            float maxConstant = curves[i].maxConstant;

                            EditorGUI.BeginChangeCheck();
                            maxConstant = FloatDraggable(r, maxConstant, curves[i].m_RemapValue, labelWidth[i], "g5");
                            if (EditorGUI.EndChangeCheck())
                            {
                                curves[i].maxConstant = maxConstant;
                            }

                            r.y += kSingleLineHeight;

                            EditorGUI.BeginChangeCheck();
                            minConstant = FloatDraggable(r, minConstant, curves[i].m_RemapValue, labelWidth[i], "g5");
                            if (EditorGUI.EndChangeCheck())
                            {
                                curves[i].minConstant = minConstant;
                            }

                            r.x += fieldWidth + labelWidth[i] + labelBorder;
                            r.y -= kSingleLineHeight;
                        }
                    }
                    else
                    {
                        Rect previewRange = xCurve.signedRange ? kSignedRange : kUnsignedRange;
                        for (int i = 0; i < curves.Length; ++i)
                        {
                            r.width = fieldWidth + labelWidth[i] - labelBorder * 2;
                            SerializedProperty minCurve = (state == MinMaxCurveState.k_TwoCurves) ? curves[i].minCurve : null;

                            using (minCurve == null ? new EditorGUI.PropertyScope(r, labels[i], curves[i].maxCurve) : (GUI.Scope) new PropertyGroupScope(curves[i].maxCurve, minCurve))
                            {
                                Label(r, labels[i]);
                                Rect r2 = r;
                                r2.xMin += labelWidth[i];

                                GUICurveField(r2, curves[i].maxCurve, minCurve, GetColor(curves[i]), previewRange, curves[i].OnCurveAreaMouseDown);
                                r.x += fieldWidth + labelWidth[i] + labelBorder;
                            }
                        }
                    }
                }

                // Toggle minmax
                GUIMMCurveStateList(popupRect, curves);
            }
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
            // Get SerializedObject (through any SerializedProperty will do) and mark override dirty.
            SerializedObject serializedObject = data.minMaxCurves[0].minScalar.serializedObject;
            serializedObject.SetIsDifferentCacheDirty();
            serializedObject.ApplyModifiedProperties();
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

                GUIContent[] texts =        {   EditorGUIUtility.TrTextContent("Constant"),
                                                EditorGUIUtility.TrTextContent("Curve"),
                                                EditorGUIUtility.TrTextContent("Random Between Two Constants"),
                                                EditorGUIUtility.TrTextContent("Random Between Two Curves") };
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
            // Get SerializedObject (through any SerializedProperty will do) and mark override dirty.
            SerializedObject serializedObject = data.gradientProp.m_MinColor.serializedObject;
            serializedObject.SetIsDifferentCacheDirty();
            serializedObject.ApplyModifiedProperties();
        }

        public static void GUIMMGradientPopUp(Rect rect, SerializedMinMaxGradient gradientProp)
        {
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Passive, ParticleSystemStyles.Get().minMaxCurveStateDropDown))
            {
                GUIContent[] texts = {  EditorGUIUtility.TrTextContent("Color"),
                                        EditorGUIUtility.TrTextContent("Gradient"),
                                        EditorGUIUtility.TrTextContent("Random Between Two Colors"),
                                        EditorGUIUtility.TrTextContent("Random Between Two Gradients"),
                                        EditorGUIUtility.TrTextContent("Random Color")};
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
            // Get SerializedObject (through any SerializedProperty will do) and mark override dirty.
            SerializedObject serializedObject = data.boolProp.serializedObject;
            serializedObject.SetIsDifferentCacheDirty();
            serializedObject.ApplyModifiedProperties();
        }

        public static void GUIMMColorPopUp(Rect rect, SerializedProperty boolProp)
        {
            if (EditorGUI.DropdownButton(rect, GUIContent.none, FocusType.Passive, ParticleSystemStyles.Get().minMaxCurveStateDropDown))
            {
                GenericMenu menu = new GenericMenu();
                GUIContent[] texts = { EditorGUIUtility.TrTextContent("Constant Color"), EditorGUIUtility.TrTextContent("Random Between Two Colors") };
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
