// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    // Built-in PropertyDrawers. See matching attributes in PropertyAttribute.cs

    [CustomPropertyDrawer(typeof(RangeAttribute))]
    internal sealed class RangeDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use Range with float or int.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RangeAttribute range = (RangeAttribute)attribute;
            if (property.propertyType == SerializedPropertyType.Float)
                EditorGUI.Slider(position, property, range.min, range.max, label);
            else if (property.propertyType == SerializedPropertyType.Integer)
                EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max, label);
            else
                EditorGUI.LabelField(position, label.text, s_InvalidTypeMessage);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            RangeAttribute range = (RangeAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                var slider = new Slider(property.displayName, range.min, range.max);
                slider.bindingPath = property.propertyPath;
                slider.showInputField = true;
                return slider;
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                var intSlider = new SliderInt(property.displayName, (int)range.min, (int)range.max);
                intSlider.bindingPath = property.propertyPath;
                intSlider.showInputField = true;
                return intSlider;
            }

            return new Label(s_InvalidTypeMessage);
        }
    }

    [CustomPropertyDrawer(typeof(MinAttribute))]
    internal sealed class MinDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use Min with float, int or Vector.");

        private MinAttribute minAttribute
        {
            get
            {
                return attribute as MinAttribute;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.DefaultPropertyField(position, property, label);
            if (EditorGUI.EndChangeCheck())
            {
                if (property.propertyType == SerializedPropertyType.Float)
                {
                    property.floatValue = Mathf.Max(minAttribute.min, property.floatValue);
                }
                else if (property.propertyType == SerializedPropertyType.Integer)
                {
                    property.intValue = Mathf.Max((int)minAttribute.min, property.intValue);
                }
                else if (property.propertyType == SerializedPropertyType.Vector2)
                {
                    var value = property.vector2Value;
                    property.vector2Value = new Vector2(Mathf.Max(minAttribute.min, value.x), Mathf.Max(minAttribute.min, value.y));
                }
                else if (property.propertyType == SerializedPropertyType.Vector2Int)
                {
                    var value = property.vector2IntValue;
                    property.vector2IntValue = new Vector2Int(Mathf.Max((int)minAttribute.min, value.x), Mathf.Max((int)minAttribute.min, value.y));
                }
                else if (property.propertyType == SerializedPropertyType.Vector3)
                {
                    var value = property.vector3Value;
                    property.vector3Value = new Vector3(Mathf.Max(minAttribute.min, value.x), Mathf.Max(minAttribute.min, value.y), Mathf.Max(minAttribute.min, value.z));
                }
                else if (property.propertyType == SerializedPropertyType.Vector3Int)
                {
                    var value = property.vector3IntValue;
                    property.vector3IntValue = new Vector3Int(Mathf.Max((int)minAttribute.min, value.x), Mathf.Max((int)minAttribute.min, value.y), Mathf.Max((int)minAttribute.min, value.z));
                }
                else if (property.propertyType == SerializedPropertyType.Vector4)
                {
                    var value = property.vector4Value;
                    property.vector4Value = new Vector4(Mathf.Max(minAttribute.min, value.x), Mathf.Max(minAttribute.min, value.y), Mathf.Max(minAttribute.min, value.z), Mathf.Max(minAttribute.min, value.w));
                }
                else
                {
                    EditorGUI.LabelField(position, label.text, s_InvalidTypeMessage);
                }
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            BindableElement newField = null;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                if (property.type == "float")
                {
                    newField = new FloatField(property.displayName);
                    ((BaseField<float>)newField).onValidateValue += OnValidateValue;
                }
                else if (property.type == "double")
                {
                    newField = new DoubleField(property.displayName);
                    ((BaseField<double>)newField).onValidateValue += OnValidateValue;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                if (property.type == "int")
                {
                    newField = new IntegerField(property.displayName);
                    ((BaseField<int>)newField).onValidateValue += OnValidateValue;
                }
                else if (property.type == "long")
                {
                    newField = new LongField(property.displayName);
                    ((BaseField<long>)newField).onValidateValue += OnValidateValue;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Vector2)
            {
                newField = new Vector2Field(property.displayName);
                ((BaseField<Vector2>)newField).onValidateValue += OnValidateValue;
            }
            else if (property.propertyType == SerializedPropertyType.Vector2Int)
            {
                newField = new Vector2IntField(property.displayName);
                ((BaseField<Vector2Int>)newField).onValidateValue += OnValidateValue;
            }
            else if (property.propertyType == SerializedPropertyType.Vector3)
            {
                newField =  new Vector3Field(property.displayName);
                ((BaseField<Vector3>)newField).onValidateValue += OnValidateValue;
            }
            else if (property.propertyType == SerializedPropertyType.Vector3Int)
            {
                newField = new Vector3IntField(property.displayName);
                ((BaseField<Vector3Int>)newField).onValidateValue += OnValidateValue;
            }
            else if (property.propertyType == SerializedPropertyType.Vector4)
            {
                newField = new Vector4Field(property.displayName);
                ((BaseField<Vector4>)newField).onValidateValue += OnValidateValue;
            }

            if (newField != null)
            {
                newField.bindingPath = property.propertyPath;
                return newField;
            }

            return new Label(s_InvalidTypeMessage);
        }

        private float OnValidateValue(float value)
        {
            return Mathf.Max(minAttribute.min, value);
        }

        private double OnValidateValue(double value)
        {
            return Math.Max(minAttribute.min, value);
        }

        private int OnValidateValue(int value)
        {
            return Mathf.Max((int)minAttribute.min, value);
        }

        private long OnValidateValue(long value)
        {
            return Math.Max((long)minAttribute.min, value);
        }

        private Vector2 OnValidateValue(Vector2 value)
        {
            return new Vector2(Mathf.Max(minAttribute.min, value.x), Mathf.Max(minAttribute.min, value.y));
        }

        private Vector2Int OnValidateValue(Vector2Int value)
        {
            return new Vector2Int(Mathf.Max((int)minAttribute.min, value.x), Mathf.Max((int)minAttribute.min, value.y));
        }

        private Vector3 OnValidateValue(Vector3 value)
        {
            return new Vector3(Mathf.Max(minAttribute.min, value.x), Mathf.Max(minAttribute.min, value.y), Mathf.Max(minAttribute.min, value.z));
        }

        private Vector3Int OnValidateValue(Vector3Int value)
        {
            return new Vector3Int(Mathf.Max((int)minAttribute.min, value.x), Mathf.Max((int)minAttribute.min, value.y), Mathf.Max((int)minAttribute.min, value.z));
        }

        private Vector4 OnValidateValue(Vector4 value)
        {
            return new Vector4(Mathf.Max(minAttribute.min, value.x), Mathf.Max(minAttribute.min, value.y), Mathf.Max(minAttribute.min, value.z), Mathf.Max(minAttribute.min, value.w));
        }
    }

    [CustomPropertyDrawer(typeof(MultilineAttribute))]
    internal sealed class MultilineDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use Multiline with string.");
        private const int kLineHeight = 13;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                label = EditorGUI.BeginProperty(position, label, property);

                position = EditorGUI.MultiFieldPrefixLabel(position, 0, label, 1);

                EditorGUI.BeginChangeCheck();
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0; // The MultiFieldPrefixLabel already applied indent, so avoid indent of TextArea itself.
                string newValue = EditorGUI.TextArea(position, property.stringValue);
                EditorGUI.indentLevel = oldIndent;
                if (EditorGUI.EndChangeCheck())
                    property.stringValue = newValue;

                EditorGUI.EndProperty();
            }
            else
                EditorGUI.LabelField(position, label.text, s_InvalidTypeMessage);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                var lines = ((MultilineAttribute)attribute).lines;
                var field = new TextField(property.displayName);
                field.multiline = true;
                field.bindingPath = property.propertyPath;
                field.style.height = EditorGUI.kSingleLineHeight + (lines - 1) * kLineHeight;
                return field;
            }

            return new Label(s_InvalidTypeMessage);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.wideMode ? 0 : (int)EditorGUI.kSingleLineHeight) // header
                + EditorGUI.kSingleLineHeight // first line
                + (((MultilineAttribute)attribute).lines - 1) * kLineHeight; // remaining lines
        }
    }

    [CustomPropertyDrawer(typeof(TextAreaAttribute))]
    internal sealed class TextAreaDrawer : PropertyDrawer
    {
        private const int kLineHeight = 13;
        private static string s_InvalidTypeMessage = L10n.Tr("Use TextAreaDrawer with string.");
        private Vector2 m_ScrollPosition = new Vector2();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                label = EditorGUI.BeginProperty(position, label, property);
                Rect labelPosition = EditorGUI.IndentedRect(position);
                labelPosition.height = EditorGUI.kSingleLineHeight;
                position.yMin += labelPosition.height;
                EditorGUI.HandlePrefixLabel(position, labelPosition, label);

                EditorGUI.BeginChangeCheck();
                string newValue = EditorGUI.ScrollableTextAreaInternal(position, property.stringValue, ref m_ScrollPosition, EditorStyles.textArea);
                if (EditorGUI.EndChangeCheck())
                    property.stringValue = newValue;

                EditorGUI.EndProperty();
            }
            else
                EditorGUI.LabelField(position, label.text, s_InvalidTypeMessage);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                var textAreaAttribute = attribute as TextAreaAttribute;
                var element = new VisualElement();
                var label = new Label(property.displayName);
                var scrollView = new ScrollView();
                var textField = new TextField();
                textField.multiline = true;
                var minHeight = EditorGUI.kSingleLineHeight + (textAreaAttribute.minLines - 1) * kLineHeight;
                var maxHeight = minHeight;

                element.Add(label);
                element.Add(scrollView);

                scrollView.Add(textField);
                scrollView.style.minHeight = minHeight;
                scrollView.style.maxHeight = maxHeight;

                textField.style.minHeight = minHeight;
                textField.bindingPath = property.propertyPath;

                return element;
            }

            return new Label(s_InvalidTypeMessage);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            TextAreaAttribute textAreaAttribute = attribute as TextAreaAttribute;
            string text = property.stringValue;

            float fullTextHeight = EditorStyles.textArea.CalcHeight(GUIContent.Temp(text), EditorGUIUtility.contextWidth);
            int lines = Mathf.CeilToInt(fullTextHeight / kLineHeight);

            lines = Mathf.Clamp(lines, textAreaAttribute.minLines, textAreaAttribute.maxLines);

            return EditorGUI.kSingleLineHeight // header
                + EditorGUI.kSingleLineHeight // first line
                + (lines - 1) * kLineHeight; // remaining lines
        }
    }

    [CustomPropertyDrawer(typeof(ColorUsageAttribute))]
    internal sealed class ColorUsageDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use ColorUsageDrawer with color.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var colorUsage = (ColorUsageAttribute)attribute;
            if (property.propertyType == SerializedPropertyType.Color)
            {
                label = EditorGUI.BeginProperty(position, label, property);
                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUI.ColorField(position, label, property.colorValue, true, colorUsage.showAlpha, colorUsage.hdr);
                if (EditorGUI.EndChangeCheck())
                {
                    property.colorValue = newColor;
                }
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.ColorField(position, label, property.colorValue, true, colorUsage.showAlpha, colorUsage.hdr);
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Color)
            {
                var colorUsage = (ColorUsageAttribute)attribute;
                var field = new ColorField(property.displayName);
                field.showAlpha = colorUsage.showAlpha;
                field.hdr = colorUsage.hdr;
                field.bindingPath = property.propertyPath;
                return field;
            }

            return new Label(s_InvalidTypeMessage);
        }
    }

    [CustomPropertyDrawer(typeof(GradientUsageAttribute))]
    internal sealed class GradientUsageDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use GradientUsageDrawer with gradient.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var colorUsage = (GradientUsageAttribute)attribute;

            EditorGUI.BeginChangeCheck();
            Gradient newGradient = EditorGUI.GradientField(position, label, property.gradientValue, colorUsage.hdr, colorUsage.colorSpace);
            if (EditorGUI.EndChangeCheck())
            {
                property.gradientValue = newGradient;
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Gradient)
            {
                var gradientUsage = (GradientUsageAttribute)attribute;
                var field = new GradientField(property.displayName);
                field.hdr = gradientUsage.hdr;
                field.colorSpace = gradientUsage.colorSpace;
                field.bindingPath = property.propertyPath;
                return field;
            }

            return new Label(s_InvalidTypeMessage);
        }
    }

    [CustomPropertyDrawer(typeof(DelayedAttribute))]
    internal sealed class DelayedDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use Delayed with float, int, or string.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Float)
                EditorGUI.DelayedFloatField(position, property, label);
            else if (property.propertyType == SerializedPropertyType.Integer)
                EditorGUI.DelayedIntField(position, property, label);
            else if (property.propertyType == SerializedPropertyType.String)
                EditorGUI.DelayedTextField(position, property, label);
            else
                EditorGUI.LabelField(position, label.text, s_InvalidTypeMessage);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            BindableElement newField = null;
            if (property.propertyType == SerializedPropertyType.Float)
            {
                if (property.type == "float")
                {
                    newField = new FloatField(property.displayName);
                    ((TextInputBaseField<float>)newField).isDelayed = true;
                }
                else if (property.type == "double")
                {
                    newField = new DoubleField(property.displayName);
                    ((TextInputBaseField<double>)newField).isDelayed = true;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                if (property.type == "int")
                {
                    newField = new IntegerField(property.displayName);
                    ((TextInputBaseField<int>)newField).isDelayed = true;
                }
                else if (property.type == "long")
                {
                    newField = new LongField(property.displayName);
                    ((TextInputBaseField<long>)newField).isDelayed = true;
                }
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                newField = new TextField(property.displayName);
                ((TextInputBaseField<string>)newField).isDelayed = true;
            }

            if (newField != null)
            {
                newField.bindingPath = property.propertyPath;
                return newField;
            }

            return new Label(s_InvalidTypeMessage);
        }
    }
}
