// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Mathematics.Editor
{
    [CustomPropertyDrawer(typeof(PostNormalizeAttribute))]
    class PostNormalizedVectorDrawer : PrimitiveVectorDrawer
    {
        static class Content
        {
            public static readonly GUIContent Tooltip = new (
                string.Empty,
                L10n.Tr("Values you enter will be post-normalized. You will see the normalized result if you "
                    + "change selection and view the values again."));
        }

        class VectorPropertyGUIData
        {
            const int k_MaxElements = 4;

            public readonly bool Valid;

            // parent property
            readonly SerializedProperty m_VectorProperty;
            // relative paths of element child properties
            readonly IReadOnlyList<string> m_ElementPaths;
            // the number of element child properties
            readonly int m_NumElements;
            // per child property; value is null if there are multiple different values
            readonly double?[] m_PreNormalizedValues;
            // per target; used to revert actual values for each object after displaying pre-normalized values
            readonly Dictionary<SerializedProperty, double4> m_PostNormalizedValues = new Dictionary<SerializedProperty, double4>();

            public VectorPropertyGUIData(SerializedProperty property)
            {
                m_VectorProperty = property;
                var parentPath = m_VectorProperty.propertyPath;
                var i = 0;
                var elementPaths = new List<string>(k_MaxElements);
                var iterator = m_VectorProperty.Copy();
                while (iterator.Next(true) && iterator.propertyPath.StartsWith(parentPath))
                {
                    if (i >= k_MaxElements || iterator.propertyType != SerializedPropertyType.Float)
                        return;
                    elementPaths.Add(iterator.propertyPath.Substring(parentPath.Length + 1));
                    i++;
                }

                Valid = true;
		        int count = elementPaths.Count;		
                m_NumElements = count;
                m_ElementPaths = elementPaths;
		        m_PreNormalizedValues = new double?[count];
		
		        for (int idx = 0; idx < count; idx++)
		        {
		            m_PreNormalizedValues[idx] = null;
		        }

                UpdatePreNormalizedValues();
                UpdatePostNormalizedValues();
            }

            void UpdatePostNormalizedValues()
            {
                m_PostNormalizedValues.Clear();
                foreach (var target in m_VectorProperty.serializedObject.targetObjects)
                {
                    var postNormalizedValue = new double4();
                    var parentProperty = new SerializedObject(target).FindProperty(m_VectorProperty.propertyPath);
                    for (var i = 0; i < m_NumElements; ++i)
                        postNormalizedValue[i] = parentProperty.FindPropertyRelative(m_ElementPaths[i]).doubleValue;
                    m_PostNormalizedValues[parentProperty] = postNormalizedValue;
                }
            }

            public void UpdatePreNormalizedValues()
            {
                for (var i = 0; i < m_NumElements; ++i)
                {
                    var p = m_VectorProperty.FindPropertyRelative(m_ElementPaths[i]);
                    m_PreNormalizedValues[i] = p.hasMultipleDifferentValues ? (double?)null : p.doubleValue;
                }
            }

            public void ApplyPreNormalizedValues()
            {
                m_VectorProperty.serializedObject.ApplyModifiedProperties();
                for (var i = 0; i < m_NumElements; ++i)
                {
                    if (m_PreNormalizedValues[i] != null)
                        m_VectorProperty.FindPropertyRelative(m_ElementPaths[i]).doubleValue = m_PreNormalizedValues[i].Value;
                }
            }

            public void UnapplyPreNormalizedValues()
            {
                foreach (var target in m_PostNormalizedValues)
                {
                    target.Key.serializedObject.Update();
                    for (var i = 0; i < m_NumElements; ++i)
                    {
                        target.Key.FindPropertyRelative(m_ElementPaths[i]).doubleValue = target.Value[i];
                        target.Key.serializedObject.ApplyModifiedProperties();
                    }
                }
                m_VectorProperty.serializedObject.Update();
            }

            public void PostNormalize(Func<double4, double4> normalize)
            {
                m_VectorProperty.serializedObject.ApplyModifiedProperties();
                foreach (var target in m_PostNormalizedValues)
                {
                    target.Key.serializedObject.Update();
                    var postNormalizedValue = new double4();
                    for (var i = 0; i < m_NumElements; ++i)
                        postNormalizedValue[i] = target.Key.FindPropertyRelative(m_ElementPaths[i]).doubleValue;
                    postNormalizedValue = normalize(normalize(postNormalizedValue));
                    for (var i = 0; i < m_NumElements; ++i)
                        target.Key.FindPropertyRelative(m_ElementPaths[i]).doubleValue = postNormalizedValue[i];
                    target.Key.serializedObject.ApplyModifiedProperties();
                }
                UpdatePostNormalizedValues();
                m_VectorProperty.serializedObject.Update();
            }

            public void RebuildIfDirty()
            {
                foreach (var target in m_PostNormalizedValues)
                {
                    target.Key.serializedObject.Update();
                    for (var i = 0; i < m_NumElements; ++i)
                    {
                        var serialized = target.Key.FindPropertyRelative(m_ElementPaths[i]).doubleValue;
                        if (target.Value[i] != serialized)
                        {
                            UpdatePreNormalizedValues();
                            UpdatePostNormalizedValues();
                            return;
                        }
                    }
                }
            }
        }

        Dictionary<string, VectorPropertyGUIData> m_GUIDataPerPropertyPath = new Dictionary<string, VectorPropertyGUIData>();

        protected virtual SerializedProperty GetVectorProperty(SerializedProperty property)
        {
            return property;
        }

        protected virtual double4 Normalize(double4 value)
        {
            return math.normalizesafe(value);
        }

        VectorPropertyGUIData GetGUIData(SerializedProperty property)
        {
            VectorPropertyGUIData guiData;
            if (!m_GUIDataPerPropertyPath.TryGetValue(property.propertyPath, out guiData))
            {
                guiData = new VectorPropertyGUIData(GetVectorProperty(property));
                m_GUIDataPerPropertyPath[property.propertyPath] = guiData;
            }
            return guiData;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetGUIData(property).Valid ? base.GetPropertyHeight(property, label) : EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guiData = GetGUIData(property);
            if (!guiData.Valid)
            {
                EditorGUI.HelpBox(
                    EditorGUI.PrefixLabel(position, label),
                    L10n.Tr($"{typeof(PostNormalizeAttribute).Name} only works with decimal vector types."),
                    MessageType.None
                );
                return;
            }

            // Avoid polluting the passed-in GUIContent - it might be static and shared.
            if (string.IsNullOrEmpty(label.tooltip))
            {
                Content.Tooltip.text = label.text;
                label = Content.Tooltip;
            }

            guiData.RebuildIfDirty();
            guiData.ApplyPreNormalizedValues();
            EditorGUI.BeginChangeCheck();
            base.OnGUI(position, property, label);
            if (EditorGUI.EndChangeCheck())
            {
                guiData.UpdatePreNormalizedValues();
                guiData.PostNormalize(Normalize);
            }
            else
            {
                guiData.UnapplyPreNormalizedValues();
            }
        }
    }
}
