// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    [CustomEditor(typeof(TagManager))]
    internal class TagManagerInspector : ProjectSettingsBaseEditor
    {
        protected SerializedProperty m_Tags;
        protected SerializedProperty m_SortingLayers;
        protected SerializedProperty m_Layers;
        ReorderableList m_TagsList;
        ReorderableList m_SortLayersList;
        ReorderableList m_LayersList;

        protected bool m_IsEditable = false;

        private bool m_HaveRemovedTag = false;

        private static InitialExpansionState s_InitialExpansionState = InitialExpansionState.None;
        internal enum InitialExpansionState
        {
            None = 0,
            Tags = 1,
            Layers = 2,
            SortingLayers = 3
        }

        internal class Styles
        {
            public static GUIContent tags = EditorGUIUtility.TrTextContent("Tags");
            public static GUIContent sortingLayers = EditorGUIUtility.TrTextContent("Sorting Layers");
            public static GUIContent layers = EditorGUIUtility.TrTextContent("Layers");
        }

        public TagManager tagManager
        {
            get { return target as TagManager; }
        }

        public virtual void OnEnable()
        {
            // Tags.
            m_Tags = serializedObject.FindProperty("tags");

            CheckForRemovedTags();

            if (m_TagsList == null)
            {
                m_TagsList = new ReorderableList(serializedObject, m_Tags, false, false, true, true);
                m_TagsList.onAddDropdownCallback = NewElement;
                m_TagsList.onRemoveCallback = RemoveFromTagsList;
                m_TagsList.drawElementCallback = DrawTagListElement;
                m_TagsList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
                m_TagsList.headerHeight = 3;
            }

            // Sorting layers.
            m_SortingLayers = serializedObject.FindProperty("m_SortingLayers");
            if (m_SortLayersList == null)
            {
                m_SortLayersList = new ReorderableList(serializedObject, m_SortingLayers, true, false, true, true);
                m_SortLayersList.onReorderCallback = ReorderSortLayerList;
                m_SortLayersList.onAddCallback = AddToSortLayerList;
                m_SortLayersList.onRemoveCallback = RemoveFromSortLayerList;
                m_SortLayersList.onCanRemoveCallback = CanRemoveSortLayerEntry;
                m_SortLayersList.drawElementCallback = DrawSortLayerListElement;
                m_SortLayersList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
                m_SortLayersList.headerHeight = 3;
            }
            // Layers.
            m_Layers = serializedObject.FindProperty("layers");
            System.Diagnostics.Debug.Assert(m_Layers.arraySize ==  32);
            if (m_LayersList == null)
            {
                m_LayersList = new ReorderableList(serializedObject, m_Layers, false, false, false, false);
                m_LayersList.drawElementCallback = DrawLayerListElement;
                m_LayersList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
                m_LayersList.headerHeight = 3;
            }

            if (s_InitialExpansionState != InitialExpansionState.None)
            {
                m_Tags.isExpanded = false;
                m_SortingLayers.isExpanded = false;
                m_Layers.isExpanded = false;
                switch (s_InitialExpansionState)
                {
                    case InitialExpansionState.Tags:
                        m_Tags.isExpanded = true;
                        break;
                    case InitialExpansionState.Layers:
                        m_Layers.isExpanded = true;
                        break;
                    case InitialExpansionState.SortingLayers:
                        m_SortingLayers.isExpanded = true;
                        break;
                }
                s_InitialExpansionState = InitialExpansionState.None;
            }
        }

        internal static void ShowWithInitialExpansion(InitialExpansionState initialExpansionState)
        {
            s_InitialExpansionState = initialExpansionState;
            Selection.activeObject = EditorApplication.tagManager;
        }

        private void CheckForRemovedTags()
        {
            for (int i = 0; i < m_Tags.arraySize; i++)
            {
                if (string.IsNullOrEmpty(m_Tags.GetArrayElementAtIndex(i).stringValue))
                    m_HaveRemovedTag = true;
            }
        }

        class EnterNamePopup : PopupWindowContent
        {
            public delegate void EnterDelegate(string str);
            readonly EnterDelegate EnterCB;
            private string m_NewTagName = "New tag";
            private bool m_NeedsFocus = true;

            public EnterNamePopup(SerializedProperty tags, EnterDelegate cb)
            {
                EnterCB = cb;


                List<string> existingTagNames = new List<string>();
                for (int i = 0; i < tags.arraySize; i++)
                {
                    string tagName = tags.GetArrayElementAtIndex(i).stringValue;
                    if (!string.IsNullOrEmpty(tagName))
                        existingTagNames.Add(tagName);
                }
                m_NewTagName = ObjectNames.GetUniqueName(existingTagNames.ToArray(), m_NewTagName);
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(400, EditorGUI.kSingleLineHeight * 2 + EditorGUI.kControlVerticalSpacing + 14);
            }

            public override void OnGUI(Rect windowRect)
            {
                GUILayout.Space(5);
                Event evt = Event.current;
                bool hitEnter = evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter);
                GUI.SetNextControlName("TagName");
                m_NewTagName = EditorGUILayout.TextField("New Tag Name", m_NewTagName);

                if (m_NeedsFocus)
                {
                    m_NeedsFocus = false;
                    EditorGUI.FocusTextInControl("TagName");
                }

                GUI.enabled = m_NewTagName.Length != 0;
                var savePressed = GUILayout.Button("Save");
                if (!string.IsNullOrWhiteSpace(m_NewTagName) && (savePressed || hitEnter))
                {
                    EnterCB(m_NewTagName);
                    editorWindow.Close();
                }
            }
        }

        void NewElement(Rect buttonRect, ReorderableList list)
        {
            int sizeBeforeupdate = list.count;
            string[] tagListBeforeUpdate = InternalEditorUtility.tags;

            buttonRect.x -= 400;
            buttonRect.y -= 13;
            PopupWindow.Show(buttonRect, new EnterNamePopup(m_Tags, s => {
                InternalEditorUtility.AddTag(s);
                serializedObject.Update();

                string[] tagListAfterUpdate = InternalEditorUtility.tags;
                int nativeObjCount = tagListAfterUpdate.Length;

                int pos = Array.IndexOf(tagListBeforeUpdate, s);
                if (pos == -1)
                {
                    if (sizeBeforeupdate == list.count)
                    {
                        SerializedProperty arraySize = list.serializedProperty.FindPropertyRelative("Array.size");
                        arraySize.intValue++;
                        list.index = list.serializedProperty.arraySize - 1;
                        list.serializedProperty.GetArrayElementAtIndex(list.index).stringValue = tagListAfterUpdate[tagListAfterUpdate.Length - 1];
                        arraySize.serializedObject.ApplyModifiedProperties();
                        list.serializedProperty.serializedObject.ApplyModifiedProperties();
                    }
                }
            }));
        }

        private void RemoveFromTagsList(ReorderableList list)
        {
            SerializedProperty tag = m_Tags.GetArrayElementAtIndex(list.index);
            if (tag.stringValue == "") return;
            GameObject go = GameObject.FindWithTag(tag.stringValue);
            if (go != null)
            {
                EditorUtility.DisplayDialog("Error", "Can't remove this tag because it is being used by " + go.name, "OK");
            }
            else
            {
                InternalEditorUtility.RemoveTag(tag.stringValue);
                m_HaveRemovedTag = true;
            }
        }

        private void DrawTagListElement(Rect rect, int index, bool selected, bool focused)
        {
            // nicer looking with selected list row and a text field in it
            rect.yMin += 1;
            rect.yMax -= 1;

            string oldName = m_Tags.GetArrayElementAtIndex(index).stringValue;
            if (string.IsNullOrEmpty(oldName))
                oldName = "(Removed)";
            EditorGUI.LabelField(rect, " Tag " + index, oldName);
        }

        void AddToSortLayerList(ReorderableList list)
        {
            int sizeBeforeupdate = list.count;
            int nativeObjCount = InternalEditorUtility.GetSortingLayerCount();

            if (nativeObjCount <= list.count)
            {
                serializedObject.ApplyModifiedProperties();
                InternalEditorUtility.AddSortingLayer();
                serializedObject.Update();
            }

            list.index = list.serializedProperty.arraySize - 1; // select just added one

            if (sizeBeforeupdate == list.count)
            {
                SerializedProperty arraySize = list.serializedProperty.FindPropertyRelative("Array.size");
                arraySize.intValue++;
                arraySize.serializedObject.ApplyModifiedProperties();
                list.serializedProperty.GetArrayElementAtIndex(list.index).FindPropertyRelative("name").stringValue = "New Layer";
                list.serializedProperty.serializedObject.ApplyModifiedProperties();
            }

            if (SortingLayer.onLayerChanged != null)
                SortingLayer.onLayerChanged();
        }

        public void ReorderSortLayerList(ReorderableList list)
        {
            serializedObject.ApplyModifiedProperties();
            InternalEditorUtility.UpdateSortingLayersOrder();

            if (SortingLayer.onLayerChanged != null)
                SortingLayer.onLayerChanged();
        }

        private void RemoveFromSortLayerList(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            InternalEditorUtility.UpdateSortingLayersOrder();

            if (SortingLayer.onLayerChanged != null)
                SortingLayer.onLayerChanged();
        }

        private bool CanEditSortLayerEntry(int index)
        {
            if (index < 0 || index >= InternalEditorUtility.GetSortingLayerCount())
                return false;
            return !InternalEditorUtility.IsSortingLayerDefault(index);
        }

        private bool CanRemoveSortLayerEntry(ReorderableList list)
        {
            return CanEditSortLayerEntry(list.index);
        }

        private void DrawSortLayerListElement(Rect rect, int index, bool selected, bool focused)
        {
            // nicer looking with selected list row and a text field in it
            rect.yMin += 1;
            rect.yMax -= 1;

            bool oldEnabled = GUI.enabled;
            GUI.enabled = m_IsEditable && CanEditSortLayerEntry(index);

            string oldName = InternalEditorUtility.GetSortingLayerName(index);
            string newName = EditorGUI.TextField(rect, " Layer " + index, oldName);
            if (newName != oldName)
            {
                serializedObject.ApplyModifiedProperties();
                InternalEditorUtility.SetSortingLayerName(index, newName);
                serializedObject.Update();
            }

            GUI.enabled = oldEnabled;
        }

        private void DrawLayerListElement(Rect rect, int index, bool selected, bool focused)
        {
            // nicer looking with selected list row and a text field in it
            rect.yMin += 1;
            rect.yMax -= 1;

            // Layers up to 8 used to be reserved for Builtin Layers
            // As layers with indices 3, 6 and 7 were empty,
            // it was decided to change them to User Layers
            // However, we cannot shift layers around so we need to explicitly handle
            // the gap where layer index == 3 in the layer stack
            bool isUserLayer = index > 5 || index == 3;

            bool oldEnabled = GUI.enabled;
            GUI.enabled = m_IsEditable && isUserLayer;

            string oldName = m_Layers.GetArrayElementAtIndex(index).stringValue;
            string newName;
            if (isUserLayer)
            {
                newName = EditorGUI.TextField(rect, " User Layer " + index, oldName);
            }
            else
            {
                newName = EditorGUI.TextField(rect, " Builtin Layer " + index, oldName);
            }

            if (newName != oldName)
            {
                m_Layers.GetArrayElementAtIndex(index).stringValue = newName;
            }

            GUI.enabled = oldEnabled;
        }

        // Want something better than "TagManager"
        internal override string targetTitle
        {
            get { return "Tags & Layers"; }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            m_IsEditable = AssetDatabase.IsOpenForEdit("ProjectSettings/TagManager.asset", StatusQueryOptions.UseCachedIfPossible);

            bool oldEnabled = GUI.enabled;
            GUI.enabled = m_IsEditable;

            // Tags
            m_Tags.isExpanded = EditorGUILayout.Foldout(m_Tags.isExpanded, Styles.tags, true);
            if (m_Tags.isExpanded)
            {
                EditorGUI.indentLevel++;
                m_TagsList.DoLayoutList();
                EditorGUI.indentLevel--;
                if (m_HaveRemovedTag)
                    EditorGUILayout.HelpBox("There are removed tags. They will be removed from this list the next time the project is loaded.", MessageType.Info, true);
            }

            // Sorting layers
            m_SortingLayers.isExpanded = EditorGUILayout.Foldout(m_SortingLayers.isExpanded, Styles.sortingLayers, true);
            if (m_SortingLayers.isExpanded)
            {
                EditorGUI.indentLevel++;
                m_SortLayersList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            // Layers
            m_Layers.isExpanded = EditorGUILayout.Foldout(m_Layers.isExpanded, Styles.layers, true);
            if (m_Layers.isExpanded)
            {
                EditorGUI.indentLevel++;
                m_LayersList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            GUI.enabled = oldEnabled;

            serializedObject.ApplyModifiedProperties();
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Tags and Layers", "ProjectSettings/TagManager.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>());
            return provider;
        }
    }
}
