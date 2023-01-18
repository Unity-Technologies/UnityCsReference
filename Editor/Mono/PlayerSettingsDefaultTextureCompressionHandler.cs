// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;


namespace UnityEditor
{
    internal abstract class PlayerSettingsDefaultTextureCompressionHandler
    {
        private SerializedProperty m_BuildTargetDefaultTextureCompressionFormat;
        private SerializedObject m_SerializedObject;
        private SerializedProperty m_TextureArraySerializedProperty;
        private GUIContent m_TextureCompressionFormatsGUI;

        private TextureCompressionFormat[] m_LastSavedFormat;
        private List<TextureCompressionFormat> m_StoredTextureCompressionList;
        private bool m_IsDirty = false;
        private bool m_ToBeUpdated = true;
        private ReorderableList m_TextureCompressionsList;
        private Func<TextureCompressionFormat[]> m_AvailableTextureCompressionsProvider;
        private Func<bool> m_IsPreset;
        private Action m_EditorSerialization;

        private const string k_BuildTargetKey = "m_BuildTarget";
        private const string k_FormatsKey = "m_Formats";
        private const string k_BuildTargetDefaultTextureCompressionFormatName = "m_BuildTargetDefaultTextureCompressionFormat";

        protected abstract string PlatformName { get; }

        protected abstract TextureCompressionFormat[] CurrentPlatformGlobalEditorTextureFormat { get; set; }

        protected abstract string ToString(TextureCompressionFormat textureCompression);

        private ReorderableList TextureCompressionsList
        {
            get
            {
                if (m_TextureCompressionsList == null)
                {
                    var textureCompressionsList = GetStoredCompressionFormatOrDefaultOne();
                    m_TextureCompressionsList = new ReorderableList(textureCompressionsList, typeof(TextureCompressionFormat), true, true, true, true)
                    {
                        onCanRemoveCallback = (list) => true,
                        onRemoveCallback = RemoveTextureCompressionElement,
                        onReorderCallback = ReorderTextureCompressionElement,
                        drawHeaderCallback = (rect) => GUI.Label(rect, m_TextureCompressionFormatsGUI, EditorStyles.label),
                        onAddDropdownCallback = AddTextureCompressionElement,
                        drawElementCallback = DrawTextureCompressionElement,
                        onChangedCallback = Onchanged,
                        elementHeight = 16
                    };
                }

                return m_TextureCompressionsList;
            }
        }


        private SerializedProperty TextureFormatArraySerializedProperty
        {
            get
            {
                if (m_TextureArraySerializedProperty == null)
                {
                    foreach (SerializedProperty element in m_BuildTargetDefaultTextureCompressionFormat)
                    {
                        var buildTarget = element.FindPropertyRelative(k_BuildTargetKey).stringValue;
                        if (buildTarget == PlatformName)
                        {
                            m_TextureArraySerializedProperty = element.FindPropertyRelative(k_FormatsKey);
                        }
                    }
                }

                return m_TextureArraySerializedProperty;
            }
        }

        public List<TextureCompressionFormat> StoredTextureCompressionList
        {
            get
            {
                if (m_StoredTextureCompressionList == null)
                {
                    m_StoredTextureCompressionList = GetStoredCompressionFormatOrDefaultOne();
                }

                return m_StoredTextureCompressionList;
            }

            private set { m_StoredTextureCompressionList = value; }
        }

        protected bool IsGlobalProjectSetting
        {
            get { return !IsAPreset; }
        }

        protected bool IsAPreset
        {
            get
            {
                if (m_IsPreset != null)
                {
                    return m_IsPreset.Invoke();
                }

                return false;
            }
        }

        public void Reset()
        {
            m_TextureArraySerializedProperty = null;
            m_TextureCompressionsList = null;
            m_LastSavedFormat = null;
            m_StoredTextureCompressionList = null;
            m_IsDirty = false;
            m_ToBeUpdated = true;
        }

        public PlayerSettingsDefaultTextureCompressionHandler(Func<TextureCompressionFormat[]> availableTextureCompressionsProvider)
        {
            m_AvailableTextureCompressionsProvider = availableTextureCompressionsProvider;
        }

        public PlayerSettingsDefaultTextureCompressionHandler SetupSerializedObject(SerializedObject serializedObject, string variableName = k_BuildTargetDefaultTextureCompressionFormatName)
        {
            m_SerializedObject = serializedObject;
            m_BuildTargetDefaultTextureCompressionFormat = FindPropertyAssert(variableName);
            return this;
        }

        public PlayerSettingsDefaultTextureCompressionHandler SetEditorSerializationAction(Action editorSerialization)
        {
            m_EditorSerialization = editorSerialization;
            return this;
        }

        public PlayerSettingsDefaultTextureCompressionHandler SetIsPreset(Func<bool> isPreset)
        {
            m_IsPreset = isPreset;
            return this;
        }

        public PlayerSettingsDefaultTextureCompressionHandler SetGUIContent(GUIContent guiContent)
        {
            m_TextureCompressionFormatsGUI = guiContent;
            return this;
        }

        private SerializedProperty FindPropertyAssert(string name)
        {
            var property = m_SerializedObject.FindProperty(name);
            if (property == null)
            {
                Debug.LogError($"Failed to find: {name}");
            }

            return property;
        }

        public void RenderingSectionGUI()
        {
            EditorGUILayout.Space();
            var updated = false;
            if (m_IsDirty)
            {
                updated = UpdateSerialization();
            }

            TextureCompressionsList.DoLayoutList();
            if (updated || m_ToBeUpdated)
            {
                var textureCompressions = GetStoredCompressionFormatOrDefaultOne();
                TextureCompressionsList.list = textureCompressions;
                m_ToBeUpdated = false;
            }

            EditorGUILayout.Space();
        }

        private List<TextureCompressionFormat> GetTextureCompressionFormatFromSerialization(SerializedProperty serializedProperty)
        {
            var formats = new List<TextureCompressionFormat>();
            if (serializedProperty != null)
            {
                var arraysize = serializedProperty.arraySize;
                for (var i = 0; i < arraysize; i++)
                {
                    var format = (TextureCompressionFormat)serializedProperty.GetArrayElementAtIndex(i).intValue;
                    formats.Add(format);
                }
            }

            return formats;
        }

        private List<TextureCompressionFormat> GetStoredCompressionFormatOrDefaultOne()
        {
            var formats = GetTextureCompressionFormatFromSerialization(TextureFormatArraySerializedProperty);

            if (formats.Count > 0)
            {
                return formats;
            }

            if (CurrentPlatformGlobalEditorTextureFormat != null)
            {
                return CurrentPlatformGlobalEditorTextureFormat.ToList();
            }

            return new List<TextureCompressionFormat>();
        }

        private bool UpdateTextureCompressionFormatSerialization(SerializedProperty serializedProperty, TextureCompressionFormat[] formats)
        {
            var same = m_LastSavedFormat != null && m_LastSavedFormat.Length == formats.Length && m_LastSavedFormat.SequenceEqual(formats);
            if (same) return false;
            m_LastSavedFormat = formats;
            var intValues = Array.ConvertAll(formats, format => (int)format);
            serializedProperty.SetValue(SerializedPropertyExtensions.Setter, intValues);

            return true;
        }

        private bool UpdateSerialization()
        {
            var updated = UpdateTextureCompressionFormatSerialization(TextureFormatArraySerializedProperty, StoredTextureCompressionList.ToArray());
            if (updated)
            {
                m_BuildTargetDefaultTextureCompressionFormat.serializedObject.ApplyModifiedProperties();
                if (IsGlobalProjectSetting)
                {
                    CurrentPlatformGlobalEditorTextureFormat = StoredTextureCompressionList.ToArray();
                    m_EditorSerialization?.Invoke();
                }

                m_SerializedObject.ApplyModifiedProperties();

                TextureCompressionsList.list = StoredTextureCompressionList;
            }

            m_IsDirty = false;
            return updated;
        }

        private void AddTextureCompressionElement(Rect rect, ReorderableList list)
        {
            var availableTextureCompressions = m_AvailableTextureCompressionsProvider();
            var names = availableTextureCompressions
                .Select(m => ToString(m))
                .ToArray();
            var enabled = availableTextureCompressions
                .Select(m => !list.list.Contains(m))
                .ToArray();

            EditorUtility.DisplayCustomMenu(rect, names, enabled, null, AddTextureCompressionMenuSelected, availableTextureCompressions);
        }

        private void AddTextureCompressionMenuSelected(object userData, string[] options, int selected)
        {
            var textureCompressions = (TextureCompressionFormat[])userData;
            if (textureCompressions[selected] == TextureCompressionFormat.DXTC_RGTC)
            {
                StoredTextureCompressionList.Remove(TextureCompressionFormat.DXTC);
            }
            else if (textureCompressions[selected] == TextureCompressionFormat.DXTC)
            {
                StoredTextureCompressionList.Remove(TextureCompressionFormat.DXTC_RGTC);
            }

            StoredTextureCompressionList.Add(textureCompressions[selected]);
            m_IsDirty = true;
        }

        private string GetTextureFormatAsAString(TextureCompressionFormat[] formats)
        {
            return String.Join(",", formats);
        }

        private void Onchanged(ReorderableList list)
        {
            m_IsDirty = true;
        }

        private void RemoveTextureCompressionElement(ReorderableList list)
        {
            // don't allow removing the last TextureCompression
            if (StoredTextureCompressionList.Count < 2)
            {
                EditorApplication.Beep();
                return;
            }

            StoredTextureCompressionList.RemoveAt(list.index);
            m_IsDirty = true;
        }

        private void ReorderTextureCompressionElement(ReorderableList list)
        {
            var textureCompressionsList = (List<TextureCompressionFormat>)list.list;
            StoredTextureCompressionList = textureCompressionsList;
            m_IsDirty = true;
        }

        private void DrawTextureCompressionElement(Rect rect, int index, bool selected, bool focused)
        {
            var textureCompression = TextureCompressionsList.list[index];
            GUI.Label(rect, ToString((TextureCompressionFormat)textureCompression), EditorStyles.label);
        }
    }
}
