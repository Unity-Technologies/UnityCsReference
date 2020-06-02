// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
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
        GUIContent m_Header;
        float m_HeaderHeight;
        string m_DisplayName;
        bool m_Foldout = true;
        bool m_Reorderable = false;
        bool m_IsNotInPrefabContextModeWithOverrides = false;

        SerializedProperty m_OriginalProperty;
        SerializedProperty m_ArraySize;

        int m_lastArraySize = -1;
        int m_lastTargetHash = -1;
        internal SerializedProperty Property
        {
            get
            {
                return m_OriginalProperty;
            }
            set
            {
                m_OriginalProperty = value;
                m_ArraySize = m_OriginalProperty.FindPropertyRelative("Array.size");

                if (m_ReorderableList != null)
                {
                    m_ReorderableList.serializedProperty = m_OriginalProperty;
                    int targetHash = GetTargetHash(m_ReorderableList.serializedProperty.serializedObject);
                    if (m_lastArraySize != m_ArraySize.intValue || m_lastTargetHash != targetHash)
                    {
                        m_ReorderableList.ClearCacheRecursive();
                        m_lastArraySize = m_ArraySize.intValue;
                        m_lastTargetHash = targetHash;
                    }
                }
            }
        }

        int GetTargetHash(SerializedObject obj)
        {
            int hash = 0;
            for (int i = 0; i < obj.targetObjectsCount; i++)
            {
                hash ^= obj.targetObjects[i].GetInstanceID();
            }
            return hash;
        }

        public static string GetPropertyIdentifier(SerializedProperty serializedProperty)
        {
            return serializedProperty.propertyPath + serializedProperty.serializedObject.targetObject.GetInstanceID();
        }

        ReorderableListWrapper() {}

        public ReorderableListWrapper(SerializedProperty property, bool reorderable = true)
        {
            Property = property;
            m_DisplayName = Property.displayName;
            m_Header = new GUIContent(m_DisplayName);
            m_HeaderHeight = Constants.kDefaultFoldoutHeaderHeight;
            Init(reorderable);
        }

        void Init(bool reorderable)
        {
            m_Reorderable = reorderable;
            SerializedProperty childProperty = Property.Copy();
            childProperty.Next(true);

            m_ReorderableList = new ReorderableList(Property.serializedObject, Property.Copy(), m_Reorderable, false, true, true);
            m_ReorderableList.headerHeight = ReorderableList.Defaults.minHeaderHeight;
            m_ReorderableList.m_IsEditable = true;
            // Check to see if the list has any elements, and use one to find out if serialized property type has children
            m_ReorderableList.m_HasPropertyDrawer = (childProperty != null) ? childProperty.hasChildren : false;

            m_ReorderableList.onCanAddCallback += (list) =>
            {
                return m_IsNotInPrefabContextModeWithOverrides;
            };

            m_ReorderableList.onCanRemoveCallback += (list) =>
            {
                return m_IsNotInPrefabContextModeWithOverrides;
            };
        }

        internal void ClearCache()
        {
            m_ReorderableList.ClearCache();
        }

        public float GetHeight()
        {
            return m_HeaderHeight + (m_Foldout && m_ReorderableList != null ? Constants.kHeaderPadding + m_ReorderableList.GetHeight() : 0.0f);
        }

        public void Draw(Rect r)
        {
            Draw(r, ReorderableList.Defaults.infinityRect);
        }

        public void Draw(Rect r, Rect visibleArea)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            m_IsNotInPrefabContextModeWithOverrides = prefabStage == null || prefabStage.mode != PrefabStage.Mode.InContext || !PrefabStage.s_PatchAllOverriddenProperties
                || Selection.objects.All(obj => PrefabUtility.IsPartOfAnyPrefab(obj) && !AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)).Equals(AssetDatabase.AssetPathToGUID(prefabStage.assetPath)));
            m_ReorderableList.draggable = m_Reorderable && m_IsNotInPrefabContextModeWithOverrides;

            Rect headerRect = new Rect(r.x, r.y, r.width, m_HeaderHeight);
            Rect sizeRect = new Rect(headerRect.xMax - Constants.kArraySizeWidth, headerRect.y,
                Constants.kArraySizeWidth, m_HeaderHeight);

            EventType prevType = Event.current.type;
            if (Event.current.type == EventType.MouseUp && sizeRect.Contains(Event.current.mousePosition))
            {
                Event.current.type = EventType.Used;
            }

            EditorGUI.BeginChangeCheck();
            m_Foldout = EditorGUI.BeginFoldoutHeaderGroup(headerRect, m_Foldout, m_Header);
            EditorGUI.EndFoldoutHeaderGroup();
            if (EditorGUI.EndChangeCheck()) m_ReorderableList.ClearCacheRecursive();

            if (Event.current.type == EventType.Used && sizeRect.Contains(Event.current.mousePosition)) Event.current.type = prevType;

            EditorGUI.DefaultPropertyField(sizeRect, m_ArraySize, GUIContent.none);
            EditorGUI.LabelField(sizeRect, new GUIContent("", "Array Size"));

            if (headerRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
                {
                    Object[] objReferences = DragAndDrop.objectReferences;
                    foreach (var o in objReferences)
                    {
                        if (EditorGUI.ValidateObjectFieldAssignment(new[] { o }, typeof(Object), m_ReorderableList.serializedProperty,
                            EditorGUI.ObjectFieldValidatorOptions.None) != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        }
                        else continue;

                        if (Event.current.type == EventType.DragPerform) ReorderableList.defaultBehaviours.DoAddButton(m_ReorderableList, o);
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

            if (m_Foldout)
            {
                r.y += m_HeaderHeight + Constants.kHeaderPadding;
                r.height -= m_HeaderHeight + Constants.kHeaderPadding;

                visibleArea.y -= r.y;
                m_ReorderableList.DoList(r, visibleArea);
            }
        }
    }
}
