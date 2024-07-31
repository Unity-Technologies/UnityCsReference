// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditorInternal
{
    [CustomPropertyDrawer(typeof(ParticleSystem.MinMaxCurve))]
    public class MinMaxCurvePropertyDrawer : PropertyDrawer
    {
        class PropertyData
        {
            public SerializedProperty mode;
            public SerializedProperty constantMin;
            public SerializedProperty constantMax;
            public SerializedProperty curveMultiplier;
            public SerializedProperty curveMin;
            public SerializedProperty curveMax;
        }

        internal bool isNativeProperty { get; set; }
        internal string xAxisLabel { get; set; } = "time";

        // Its possible that the PropertyDrawer may be used to draw more than one MinMaxCurve property(arrays, lists)
        Dictionary<string, PropertyData> m_PropertyDataPerPropertyPath = new Dictionary<string, PropertyData>();
        PropertyData m_Property;

        class Styles
        {
            public readonly float floatFieldDragWidth = 20;
            public readonly float stateButtonWidth = 18;
            public readonly Color curveColor = Color.green;
            public readonly Color curveBackgroundColor = new Color(0.337f, 0.337f, 0.337f, 1f);
            public readonly AnimationCurve defaultCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1)) { postWrapMode = WrapMode.ClampForever, preWrapMode = WrapMode.ClampForever };
            public readonly GUIContent[] modes = new[]
            {
                EditorGUIUtility.TrTextContent("Constant"),
                EditorGUIUtility.TrTextContent("Curve"),
                EditorGUIUtility.TrTextContent("Random Between Two Curves"),
                EditorGUIUtility.TrTextContent("Random Between Two Constants")
            };
        }
        static Styles s_Styles;

        static int s_CurveId;

        void Init(SerializedProperty property)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            if (m_PropertyDataPerPropertyPath.TryGetValue(property.propertyPath, out m_Property))
                return;

            m_Property = new PropertyData()
            {
                mode = property.FindPropertyRelative(isNativeProperty ? "minMaxState" : "m_Mode"),
                constantMax = property.FindPropertyRelative(isNativeProperty ? "scalar" : "m_ConstantMax"),
                constantMin = property.FindPropertyRelative(isNativeProperty ? "minScalar" : "m_ConstantMin"),
                curveMin = property.FindPropertyRelative(isNativeProperty ? "minCurve" : "m_CurveMin"),
                curveMax = property.FindPropertyRelative(isNativeProperty ? "maxCurve" : "m_CurveMax"),

                // In native we use the same value for multiplier and max scalar.
                curveMultiplier = property.FindPropertyRelative(isNativeProperty ? "scalar" : "m_CurveMultiplier")
            };

            m_PropertyDataPerPropertyPath.Add(property.propertyPath, m_Property);
            InitCurves();
        }

        // Ensure curves are not empty if they are in use.
        void InitCurves()
        {
            var state = (MinMaxCurveState)m_Property.mode.intValue;
            if ((state == MinMaxCurveState.k_Curve || state == MinMaxCurveState.k_TwoCurves) && m_Property.curveMax.animationCurveValue.keys.Length == 0)
            {
                if (Mathf.Approximately(m_Property.curveMultiplier.floatValue, 0))
                    m_Property.curveMultiplier.floatValue = 1;
                m_Property.curveMax.animationCurveValue = s_Styles.defaultCurve;
            }
            if (state == MinMaxCurveState.k_TwoCurves && m_Property.curveMin.animationCurveValue.keys.Length == 0)
                m_Property.curveMin.animationCurveValue = s_Styles.defaultCurve;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            Rect fieldRect;
            var mode = (MinMaxCurveState)m_Property.mode.intValue;

            // Only curves require manually drawing a label, the controls for scalar handle the label drawing themselves.
            if (!m_Property.mode.hasMultipleDifferentValues && (mode == MinMaxCurveState.k_Scalar || mode == MinMaxCurveState.k_TwoScalars))
                fieldRect = position;
            else
                fieldRect = EditorGUI.PrefixLabel(position, label);

            // Mode
            fieldRect.width -= s_Styles.stateButtonWidth;
            var modeRect = new Rect(fieldRect.xMax, fieldRect.y, s_Styles.stateButtonWidth, fieldRect.height);
            EditorGUI.BeginProperty(modeRect, GUIContent.none, m_Property.mode);
            EditorGUI.BeginChangeCheck();
            int newSelection = EditorGUI.Popup(modeRect, null, m_Property.mode.intValue, s_Styles.modes, EditorStyles.minMaxStateDropdown);
            if (EditorGUI.EndChangeCheck())
            {
                m_Property.mode.intValue = newSelection;
                InitCurves();
                AnimationCurvePreviewCache.ClearCache();
            }
            EditorGUI.EndProperty();

            if (m_Property.mode.hasMultipleDifferentValues)
            {
                EditorGUI.LabelField(fieldRect, GUIContent.Temp("-"));
                return;
            }

            switch (mode)
            {
                case MinMaxCurveState.k_Scalar:
                    EditorGUI.PropertyField(fieldRect, m_Property.constantMax, label);
                    break;

                case MinMaxCurveState.k_Curve:
                    DoMinMaxCurvesField(fieldRect, m_Property.curveMax.GetHashCode(), m_Property.curveMax, null, m_Property.curveMultiplier, s_Styles.curveColor, s_Styles.curveBackgroundColor, xAxisLabel);
                    break;

                case MinMaxCurveState.k_TwoCurves:
                    DoMinMaxCurvesField(fieldRect, m_Property.curveMin.GetHashCode(), m_Property.curveMax, m_Property.curveMin, m_Property.curveMultiplier, s_Styles.curveColor, s_Styles.curveBackgroundColor, xAxisLabel);
                    break;

                case MinMaxCurveState.k_TwoScalars:
                    float fieldWidth = (fieldRect.width - EditorGUIUtility.labelWidth) * 0.5f;
                    var rectMin = new Rect(fieldRect.x, fieldRect.y, fieldRect.width - fieldWidth - (s_Styles.floatFieldDragWidth * 0.5f), fieldRect.height);
                    EditorGUI.PropertyField(rectMin, m_Property.constantMin, label);
                    var rectMax = new Rect(rectMin.xMax + s_Styles.floatFieldDragWidth, fieldRect.y, fieldWidth - (s_Styles.floatFieldDragWidth * 0.5f), fieldRect.height);
                    var rectMaxDragArea = new Rect(rectMax.xMin - s_Styles.floatFieldDragWidth, fieldRect.y, s_Styles.floatFieldDragWidth, fieldRect.height);
                    EditorGUI.BeginProperty(rectMax, GUIContent.none, m_Property.constantMax);
                    EditorGUI.BeginChangeCheck();
                    float newConstantMax = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, rectMax, rectMaxDragArea, m_Property.constantMax.GetHashCode(), m_Property.constantMax.floatValue, "g7", EditorStyles.numberField, true);
                    if (EditorGUI.EndChangeCheck())
                        m_Property.constantMax.floatValue = newConstantMax;
                    EditorGUI.EndProperty();
                    break;
            }
        }

        static void DoMinMaxCurvesField(Rect position, int id, SerializedProperty propertyMax, SerializedProperty propertyMin, SerializedProperty scalar, Color color, Color backgroundColor, string xLabel)
        {
            var evt = Event.current;

            if (MinMaxCurveEditorWindow.visible && Event.current.type != EventType.Layout && GUIUtility.keyboardControl == id)
            {
                if (s_CurveId != id)
                {
                    s_CurveId = id;
                    if (MinMaxCurveEditorWindow.visible)
                    {
                        MinMaxCurveEditorWindow.xAxisLabel = xLabel;
                        MinMaxCurveEditorWindow.SetCurves(propertyMax, propertyMin, scalar, color);
                        MinMaxCurveEditorWindow.ShowPopup(GUIView.current);
                    }
                }
                else
                {
                    if (MinMaxCurveEditorWindow.visible && Event.current.type == EventType.Repaint)
                    {
                        MinMaxCurveEditorWindow.xAxisLabel = xLabel;
                        MinMaxCurveEditorWindow.SetCurves(propertyMax, propertyMin, scalar, color);
                        MinMaxCurveEditorWindow.instance.Repaint();
                    }
                }
            }

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition))
                    {
                        s_CurveId = id;
                        GUIUtility.keyboardControl = id;
                        MinMaxCurveEditorWindow.xAxisLabel = xLabel;
                        MinMaxCurveEditorWindow.SetCurves(propertyMax, propertyMin, scalar, color);
                        MinMaxCurveEditorWindow.ShowPopup(GUIView.current);
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    break;

                case EventType.Repaint:
                    if (propertyMin != null)
                        EditorGUIUtility.DrawRegionSwatch(position, propertyMax, propertyMin, color, backgroundColor);
                    else
                        EditorGUIUtility.DrawCurveSwatch(position, null, propertyMax, color, backgroundColor);
                    EditorStyles.colorPickerBox.Draw(position, GUIContent.none, id, false);
                    break;

                case EventType.ExecuteCommand:

                    if (s_CurveId == id && evt.commandName == "CurveChanged")
                    {
                        GUI.changed = true;
                        AnimationCurvePreviewCache.ClearCache();
                        HandleUtility.Repaint();
                        if (propertyMax != null && MinMaxCurveEditorWindow.instance.maxCurve != null)
                        {
                            propertyMax.animationCurveValue = MinMaxCurveEditorWindow.instance.maxCurve;
                        }
                        if (propertyMin != null)
                        {
                            propertyMin.animationCurveValue = MinMaxCurveEditorWindow.instance.minCurve;
                        }
                    }
                    break;

                case EventType.KeyDown:
                    if (evt.MainActionKeyForControl(id))
                    {
                        s_CurveId = id;
                        MinMaxCurveEditorWindow.xAxisLabel = xLabel;
                        MinMaxCurveEditorWindow.SetCurves(propertyMax, propertyMin, scalar, color);
                        MinMaxCurveEditorWindow.ShowPopup(GUIView.current);
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    break;
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            const string StylesheetPath = "StyleSheets/ParticleSystem/ParticleSystem.uss";
            const string AlignClass = "unity-base-field__aligned";
            const string InputClass = "unity-base-popup-field__input";
            const string RowClass = "unity-particle-system-min-max-curve__row";
            const string GrowClass = "unity-particle-system-min-max-curve__grow";

            PropertyField PrepareProperty(SerializedProperty prop, string label, VisualElement parent)
            {
                var propField = new PropertyField(prop, label);
                propField.AddToClassList(UIElementsUtility.hiddenClassName);
                propField.AddToClassList(GrowClass);
                parent.Add(propField);
                return propField;
            }

            Init(property);

            var container = new VisualElement();
            container.AddStyleSheetPath(StylesheetPath);
            container.AddToClassList(RowClass);

            var constantMin = PrepareProperty(m_Property.constantMin, property.name, container);
            var constantMax = PrepareProperty(m_Property.constantMax, property.name, container);

            var region = new IMGUIContainer(() =>
            {
                var label = new GUIContent(preferredLabel);
                var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(rect, label);
                rect = EditorGUI.PrefixLabel(rect, label);

                EditorGUI.BeginChangeCheck();
                if ((MinMaxCurveState)m_Property.mode.intValue == MinMaxCurveState.k_Curve)
                    DoMinMaxCurvesField(rect, m_Property.curveMax.GetHashCode(), m_Property.curveMax, null, m_Property.curveMultiplier, s_Styles.curveColor, s_Styles.curveBackgroundColor, xAxisLabel);
                else DoMinMaxCurvesField(rect, m_Property.curveMin.GetHashCode(), m_Property.curveMax, m_Property.curveMin, m_Property.curveMultiplier, s_Styles.curveColor, s_Styles.curveBackgroundColor, xAxisLabel);
                if (EditorGUI.EndChangeCheck()) m_Property.curveMax.serializedObject.ApplyModifiedProperties();
            });
            region.AddToClassList(UIElementsUtility.hiddenClassName);
            region.AddToClassList(GrowClass);
            container.Add(region);

            var mode = new PopupField<string>(s_Styles.modes.Select(m => m.text).ToList(), m_Property.mode.intValue);
            container.Add(mode);

            EventCallback<ChangeEvent<string>> valueChangeAction = (e) =>
            {
                MinMaxCurveState state = (MinMaxCurveState)s_Styles.modes.Select(m => m.text).ToList().IndexOf(e.newValue);
                m_Property.mode.intValue = (int)state;
                m_Property.mode.serializedObject.ApplyModifiedProperties();

                constantMax.EnableInClassList(UIElementsUtility.hiddenClassName, state != MinMaxCurveState.k_Scalar && state != MinMaxCurveState.k_TwoScalars);

                if(constantMax.Children().Count() > 0)
                {
                    constantMax.Children().First().EnableInClassList(AlignClass, state != MinMaxCurveState.k_TwoScalars);

                    var label = constantMax.Query<Label>().Build().First();
                    if (state == MinMaxCurveState.k_Scalar)
                    {
                        label.ResetPositionProperties();
                        label.text = preferredLabel;
                    }
                    else
                    {
                        label.text = string.Empty;
                        label.style.width = label.style.minWidth = s_Styles.floatFieldDragWidth;
                    }
                }

                constantMin.EnableInClassList(UIElementsUtility.hiddenClassName, state != MinMaxCurveState.k_TwoScalars);
                region.EnableInClassList(UIElementsUtility.hiddenClassName, state != MinMaxCurveState.k_Curve && state != MinMaxCurveState.k_TwoCurves);
                AnimationCurvePreviewCache.ClearCache();
            };
            mode.RegisterValueChangedCallback(valueChangeAction);

            container.RegisterCallback<AttachToPanelEvent>((e) =>
            {
                var popup = mode.Children().First();
                popup.Remove(popup.Children().First());
                popup.RemoveFromClassList(InputClass);

                valueChangeAction.Invoke(ChangeEvent<string>.GetPooled("", s_Styles.modes[m_Property.mode.intValue].text));
            });

            return container;
        }
    }
}
