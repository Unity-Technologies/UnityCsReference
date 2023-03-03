// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    // Provides a default appearance for a generic reorderable list that is typically used in inspector to draw arrays
    internal class ReorderableListWrapper
    {
        public static class Constants
        {
            public const float kHeaderPadding = 3f;
            public const float kArraySizeWidth = 48f;
            public const float kDefaultFoldoutHeaderHeight = 18;
        }

        internal ReorderableList m_ReorderableList;
        float m_HeaderHeight;
        bool m_Reorderable = false;
        bool m_ListIsPatchedInPrefabModeInContext = false;
        bool m_DisableListElements = false;

        SerializedProperty m_OriginalProperty;
        SerializedProperty m_ArraySize;
        string m_PropertyPath = string.Empty;
        string m_PropertyPathArraySize = string.Empty;

        internal static Rect s_ToolTipRect;

        int m_LastArraySize = -1;
        internal SerializedProperty Property
        {
            get
            {
                return m_OriginalProperty;
            }
            set
            {
                m_OriginalProperty = value;
                if (!m_OriginalProperty.isValid)
                {
                    m_ArraySize = null;
                    m_PropertyPath = string.Empty;
                    m_PropertyPathArraySize = string.Empty;
                    return;
                }
                m_ArraySize = m_OriginalProperty.FindPropertyRelative("Array.size");
                m_PropertyPath = m_OriginalProperty.propertyPath;
                m_PropertyPathArraySize = m_OriginalProperty + ".Array.size";

                if (m_ReorderableList != null)
                {
                    bool versionChanged = !SerializedProperty.VersionEquals(m_ReorderableList.serializedProperty, m_OriginalProperty);

                    m_ReorderableList.serializedProperty = m_OriginalProperty;
                    UpdatePrefabPatchState(m_OriginalProperty.serializedObject.targetObject);

                    if (versionChanged || m_ArraySize != null && m_LastArraySize != m_ArraySize.intValue)
                    {
                        m_ReorderableList.InvalidateCacheRecursive();
                        ReorderableList.InvalidateParentCaches(m_ReorderableList.serializedProperty.propertyPath);

                        if (m_ArraySize != null) m_LastArraySize = m_ArraySize.intValue;
                    }
                }
            }
        }

        public static string GetPropertyIdentifier(SerializedProperty serializedProperty)
        {
            // Property may be disposed
            try
            {
                return serializedProperty?.propertyPath + serializedProperty.serializedObject.targetObject.GetInstanceID() + (GUIView.current?.nativeHandle.ToInt32() ?? -1);
            }
            catch (NullReferenceException)
            {
                return string.Empty;
            }
        }

        ReorderableListWrapper() {}

        public ReorderableListWrapper(SerializedProperty property, GUIContent label, bool reorderable = true)
        {
            Init(reorderable, property);
        }

        void Init(bool reorderable, SerializedProperty property)
        {
            m_Reorderable = reorderable;
            SerializedProperty childProperty = property.Copy();
            childProperty.Next(true);

            m_ReorderableList = new ReorderableList(property.serializedObject, property.Copy(), m_Reorderable, false, true, true);
            m_ReorderableList.headerHeight = ReorderableList.Defaults.minHeaderHeight;
            m_ReorderableList.m_IsEditable = true;
            m_ReorderableList.multiSelect = true;
            // Check to see if the list has any elements, and use one to find out if serialized property type has children
            m_ReorderableList.m_HasPropertyDrawer = (childProperty != null) ? childProperty.hasChildren : false;

            m_ReorderableList.onCanAddCallback += (list) =>
            {
                return !m_ListIsPatchedInPrefabModeInContext;
            };

            m_ReorderableList.onCanRemoveCallback += (list) =>
            {
                return !m_ListIsPatchedInPrefabModeInContext;
            };

            Property = property;
            m_HeaderHeight = Constants.kDefaultFoldoutHeaderHeight;
        }

        internal void InvalidateCache() => m_ReorderableList.InvalidateCache();

        public float GetHeight()
        {
            return m_HeaderHeight + (Property.isExpanded && m_ReorderableList != null ? Constants.kHeaderPadding + m_ReorderableList.GetHeight() : 0.0f);
        }

        void UpdatePrefabPatchState(Object serializedObjectTarget)
        {
            m_DisableListElements = false;
            m_ListIsPatchedInPrefabModeInContext = false;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                m_ListIsPatchedInPrefabModeInContext = prefabStage.HasPatchedPropertyModificationsFor(serializedObjectTarget, m_PropertyPath);
                if (m_ListIsPatchedInPrefabModeInContext)
                {
                    m_DisableListElements = prefabStage.HasPatchedPropertyModificationsFor(serializedObjectTarget, m_PropertyPathArraySize);
                }
            }

            if (m_ReorderableList != null)
                m_ReorderableList.draggable = m_Reorderable && !m_ListIsPatchedInPrefabModeInContext;
        }

        public void Draw(GUIContent label, Rect r, Rect visibleArea, string tooltip, bool includeChildren)
        {
            r.xMin += EditorGUI.indent;

            Rect headerRect = new Rect(r.x, r.y, r.width, m_HeaderHeight);
            Rect sizeRect = new Rect(headerRect.xMax - Constants.kArraySizeWidth - EditorGUI.indent * EditorGUI.indentLevel, headerRect.y,
                Constants.kArraySizeWidth + EditorGUI.indent * EditorGUI.indentLevel, m_HeaderHeight);

            Event evt = Event.current;
            EventType prevType = evt.type;
            if (!string.IsNullOrEmpty(tooltip) && prevType == EventType.Repaint)
            {
                bool hovered = headerRect.Contains(evt.mousePosition);

                if (hovered && GUIClip.visibleRect.Contains(evt.mousePosition))
                {
                    if (!GUIStyle.IsTooltipActive(tooltip))
                        s_ToolTipRect = new Rect(evt.mousePosition, Vector2.zero);
                    GUIStyle.SetMouseTooltip(tooltip, s_ToolTipRect);
                }
            }
            if (Event.current.type == EventType.MouseUp && sizeRect.Contains(Event.current.mousePosition))
            {
                Event.current.type = EventType.Used;
            }

            EditorGUI.BeginChangeCheck();
            if (!m_OriginalProperty.hasMultipleDifferentValues) EditorGUI.BeginProperty(headerRect, GUIContent.none, m_OriginalProperty);

            bool prevEnabled = GUI.enabled;
            GUI.enabled = true;
            Property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(headerRect, Property.isExpanded, label ?? GUIContent.Temp(Property.displayName));
            EditorGUI.EndFoldoutHeaderGroup();
            GUI.enabled = prevEnabled;

            if (!m_OriginalProperty.hasMultipleDifferentValues) EditorGUI.EndProperty();

            if (EditorGUI.EndChangeCheck())
            {
                if (Event.current.alt)
                {
                    EditorGUI.SetExpandedRecurse(Property, Property.isExpanded);
                }

                m_ReorderableList.InvalidateCacheRecursive();
            }

            if (m_DisableListElements)
                GUI.enabled = false;

            DrawChildren(r, headerRect, sizeRect, visibleArea, prevType);
            GUI.enabled = prevEnabled;
        }

        void DrawChildren(Rect listRect, Rect headerRect, Rect sizeRect, Rect visibleRect, EventType previousEvent)
        {
            if (Event.current.type == EventType.Used && sizeRect.Contains(Event.current.mousePosition)) Event.current.type = previousEvent;

            EditorGUI.BeginChangeCheck();
            EditorGUI.DefaultPropertyField(sizeRect, m_ArraySize, GUIContent.none);
            EditorGUI.LabelField(sizeRect, new GUIContent("", "Array Size"));
            if (EditorGUI.EndChangeCheck())
                m_ReorderableList.InvalidateForGUI();

            if (headerRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
                {
                    Object[] objReferences = DragAndDrop.objectReferences;
                    foreach (var o in objReferences)
                    {
                        Object validatedObject = EditorGUI.ValidateObjectFieldAssignment(new[] { o }, typeof(Object), m_ReorderableList.serializedProperty, EditorGUI.ObjectFieldValidatorOptions.None);
                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        }
                        else continue;

                        if (Event.current.type == EventType.DragPerform) ReorderableList.defaultBehaviours.DoAddButton(m_ReorderableList, validatedObject);
                    }
                    DragAndDrop.AcceptDrag();
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.DragExited)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
                Event.current.Use();
            }

            if (Property.isExpanded)
            {
                listRect.y += m_HeaderHeight + Constants.kHeaderPadding;
                listRect.height -= m_HeaderHeight + Constants.kHeaderPadding;

                visibleRect.y -= listRect.y;
                m_ReorderableList.DoList(listRect, visibleRect);
            }
        }
    }
}
