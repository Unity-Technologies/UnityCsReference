// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Mathematics.Editor
{
    [CustomPropertyDrawer(typeof(bool2)), CustomPropertyDrawer(typeof(bool3)), CustomPropertyDrawer(typeof(bool4))]
    [CustomPropertyDrawer(typeof(double2)), CustomPropertyDrawer(typeof(double3)), CustomPropertyDrawer(typeof(double4))]
    [CustomPropertyDrawer(typeof(float2)), CustomPropertyDrawer(typeof(float3)), CustomPropertyDrawer(typeof(float4))]
    [CustomPropertyDrawer(typeof(int2)), CustomPropertyDrawer(typeof(int3)), CustomPropertyDrawer(typeof(int4))]
    [CustomPropertyDrawer(typeof(uint2)), CustomPropertyDrawer(typeof(uint3)), CustomPropertyDrawer(typeof(uint4))]
    [CustomPropertyDrawer(typeof(DoNotNormalizeAttribute))]
    class PrimitiveVectorDrawer : PropertyDrawer
    {
        private string _PropertyType;

        string GetPropertyType(SerializedProperty property)
        {
            if (_PropertyType == null)
            {
                _PropertyType = property.type;
                var isManagedRef = property.type.StartsWith("managedReference", StringComparison.Ordinal);
                if (isManagedRef)
                {
                    var startIndex = "managedReference<".Length;
                    var length = _PropertyType.Length - startIndex - 1;
                    _PropertyType = _PropertyType.Substring("managedReference<".Length, length);
                }
            }

            return _PropertyType;
        }

        static class Content
        {
            public static readonly string doNotNormalizeCompatibility = L10n.Tr(
                $"{typeof(DoNotNormalizeAttribute).Name} only works with {typeof(quaternion)} and primitive vector types."
            );
            public static readonly GUIContent doNotNormalizeContent = new GUIContent("", 
                L10n.Tr("This value is not normalized, which may produce unexpected results."));

            public static readonly GUIContent[] labels2 = { new GUIContent("X"), new GUIContent("Y") };
            public static readonly GUIContent[] labels3 = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
            public static readonly GUIContent[] labels4 = { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z"), new GUIContent("W") };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight;
            if (!EditorGUIUtility.wideMode)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var subLabels = Content.labels4;
            var startIter = "x";
            var propertyType = GetPropertyType(property);
            switch (propertyType[propertyType.Length - 1])
            {
                case '2':
                    subLabels = Content.labels2;
                    break;
                case '3':
                    subLabels = Content.labels3;
                    break;
                case '4':
                    subLabels = Content.labels4;
                    break;
                default:
                {
                    if (property.type == nameof(quaternion))
                        startIter = "value.x";
                    else if (attribute is DoNotNormalizeAttribute)
                    {
                        EditorGUI.HelpBox(EditorGUI.PrefixLabel(position, label), Content.doNotNormalizeCompatibility, MessageType.None);
                        return;
                    }
                    break;
                }
            }

            if (attribute is DoNotNormalizeAttribute && string.IsNullOrEmpty(label.tooltip))
            {
                Content.doNotNormalizeContent.text = label.text;
                label = Content.doNotNormalizeContent;
            }
            label = EditorGUI.BeginProperty(position, label, property);
            var valuesIterator = property.FindPropertyRelative(startIter);
            MultiPropertyField(position, subLabels, valuesIterator, label);
            EditorGUI.EndProperty();
        }

        void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, GUIContent label)
        {
            EditorGUICopy.MultiPropertyField(position, subLabels, valuesIterator, label);
        }
    }

    internal class EditorGUICopy
    {
        internal const float kSpacingSubLabel = 4;
        private const float kIndentPerLevel = 15;
        internal const float kPrefixPaddingRight = 2;
        internal static int indentLevel = 0;
        private static readonly int s_FoldoutHash = "Foldout".GetHashCode();

        // internal static readonly SVC<float> kVerticalSpacingMultiField = new SVC<float>("--theme-multifield-vertical-spacing", 0.0f);
        // kVerticalSpacingMultiField should actually look like the above line ^^^ but we don't have access to SVC<T>,
        // so instead we just set this value to what is observed in the debugger with the Unity dark theme.
        internal const float kVerticalSpacingMultiField = 2;

        internal enum PropertyVisibility
        {
            All,
            OnlyVisible
        }

        // This code is basically EditorGUI.MultiPropertyField(Rect, GUIContent[], SerializedProperty, GUIContent),
        // but with the property visibility assumed to be "All" instead of "OnlyVisible". We really want to have "All"
        // because it's possible for someone to hide something in the inspector with [HideInInspector] but then manually
        // draw it themselves later. In this case, if you called EditorGUI.MultiPropertyField() directly, you'd
        // end up with some fields that point to some unrelated visible property.
        public static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, subLabels.Length);
            position.height = EditorGUIUtility.singleLineHeight;
            MultiPropertyFieldInternal(position, subLabels, valuesIterator, PropertyVisibility.All);
        }

        internal static void BeginDisabled(bool disabled)
        {
            // Unused, but left here to minimize changes in EditorGUICopy.MultiPropertyFieldInternal().
        }

        internal static void EndDisabled()
        {
            // Unused, but left here to minimize changes in EditorGUICopy.MultiPropertyFieldInternal().
        }

        internal static float CalcPrefixLabelWidth(GUIContent label, GUIStyle style = null)
        {
            if (style == null)
                style = EditorStyles.label;
            return style.CalcSize(label).x;
        }

        internal static void MultiPropertyFieldInternal(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, PropertyVisibility visibility, bool[] disabledMask = null, float prefixLabelWidth = -1)
        {
            int eCount = subLabels.Length;
            float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
            Rect nr = new Rect(position) {width = w};
            float t = EditorGUIUtility.labelWidth;
            int l = indentLevel;
            indentLevel = 0;
            for (int i = 0; i < subLabels.Length; i++)
            {
                EditorGUIUtility.labelWidth = prefixLabelWidth > 0 ? prefixLabelWidth : CalcPrefixLabelWidth(subLabels[i]);

                if (disabledMask != null)
                    BeginDisabled(disabledMask[i]);
                EditorGUI.PropertyField(nr, valuesIterator, subLabels[i]);
                if (disabledMask != null)
                    EndDisabled();
                nr.x += w + kSpacingSubLabel;

                switch (visibility)
                {
                    case PropertyVisibility.All:
                        valuesIterator.Next(false);
                        break;

                    case PropertyVisibility.OnlyVisible:
                        valuesIterator.NextVisible(false);
                        break;
                }
            }
            EditorGUIUtility.labelWidth = t;
            indentLevel = l;
        }

        internal static bool LabelHasContent(GUIContent label)
        {
            if (label == null)
            {
                return true;
            }
            // @TODO: find out why checking for GUIContent.none doesn't work
            return label.text != string.Empty || label.image != null;
        }

        internal static float indent => indentLevel * kIndentPerLevel;

        internal static Rect MultiFieldPrefixLabel(Rect totalPosition, int id, GUIContent label, int columns)
        {
            if (!LabelHasContent(label))
            {
                return EditorGUI.IndentedRect(totalPosition);
            }

            if (EditorGUIUtility.wideMode)
            {
                Rect labelPosition = new Rect(totalPosition.x + indent, totalPosition.y, EditorGUIUtility.labelWidth - indent, EditorGUIUtility.singleLineHeight);
                Rect fieldPosition = totalPosition;
                fieldPosition.xMin += EditorGUIUtility.labelWidth + kPrefixPaddingRight;

                // If there are 2 columns we use the same column widths as if there had been 3 columns
                // in order to make columns line up neatly.
                if (columns == 2)
                {
                    float columnWidth = (fieldPosition.width - (3 - 1) * kSpacingSubLabel) / 3f;
                    fieldPosition.xMax -= (columnWidth + kSpacingSubLabel);
                }

                EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id);
                return fieldPosition;
            }
            else
            {
                Rect labelPosition = new Rect(totalPosition.x + indent, totalPosition.y, totalPosition.width - indent, EditorGUIUtility.singleLineHeight);
                Rect fieldPosition = totalPosition;
                fieldPosition.xMin += indent + kIndentPerLevel;
                fieldPosition.yMin += EditorGUIUtility.singleLineHeight + kVerticalSpacingMultiField;
                EditorGUI.HandlePrefixLabel(totalPosition, labelPosition, label, id);
                return fieldPosition;
            }
        }
    }
}
