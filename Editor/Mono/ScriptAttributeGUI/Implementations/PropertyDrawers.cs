// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    // Built-in PropertyDrawers. See matching attributes in PropertyAttribute.cs

    [CustomPropertyDrawer(typeof(RangeAttribute))]
    internal sealed class RangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            RangeAttribute range = (RangeAttribute)attribute;
            if (property.propertyType == SerializedPropertyType.Float)
                EditorGUI.Slider(position, property, range.min, range.max, label);
            else if (property.propertyType == SerializedPropertyType.Integer)
                EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max, label);
            else
                EditorGUI.LabelField(position, label.text, "Use Range with float or int.");
        }
    }

    [CustomPropertyDrawer(typeof(MinAttribute))]
    internal sealed class MinDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.DefaultPropertyField(position, property, label);
            if (EditorGUI.EndChangeCheck())
            {
                MinAttribute minAttribute = (MinAttribute)attribute;
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
                    EditorGUI.LabelField(position, label.text, "Use Min with float, int or Vector.");
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(MultilineAttribute))]
    internal sealed class MultilineDrawer : PropertyDrawer
    {
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
                EditorGUI.LabelField(position, label.text, "Use Multiline with string.");
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
                EditorGUI.LabelField(position, label.text, "Use TextAreaDrawer with string.");
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
    }

    [CustomPropertyDrawer(typeof(GradientUsageAttribute))]
    internal sealed class GradientUsageDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var colorUsage = (GradientUsageAttribute)attribute;

            EditorGUI.BeginChangeCheck();
            Gradient newGradient = EditorGUI.GradientField(position, label, property.gradientValue, colorUsage.hdr);
            if (EditorGUI.EndChangeCheck())
            {
                property.gradientValue = newGradient;
            }
        }
    }

    [CustomPropertyDrawer(typeof(DelayedAttribute))]
    internal sealed class DelayedDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Float)
                EditorGUI.DelayedFloatField(position, property, label);
            else if (property.propertyType == SerializedPropertyType.Integer)
                EditorGUI.DelayedIntField(position, property, label);
            else if (property.propertyType == SerializedPropertyType.String)
                EditorGUI.DelayedTextField(position, property, label);
            else
                EditorGUI.LabelField(position, label.text, "Use Delayed with float, int, or string.");
        }
    }
}
