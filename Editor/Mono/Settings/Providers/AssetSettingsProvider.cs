// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Internal;
using UnityEditor.StyleSheets;
using UnityEditorInternal;

namespace UnityEditor
{
    [ExcludeFromDocs]
    public class AssetSettingsProvider : SettingsProvider
    {
        static class Styles
        {
            public static GUIStyle settingsStyle = "SettingsIconButton";
            public static StyleBlock settingsBtn => EditorResources.GetStyle("settings-icon-btn");
        }

        protected Editor m_SettingsEditor;
        public Func<UnityEngine.Object> settingsFetcher { get; set; }
        public Action<Editor> onEditorCreated { get; set; }

        public AssetSettingsProvider(string preferencePath, string assetPath)
            : this(preferencePath, () => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath)) {}

        public AssetSettingsProvider(string preferencePath, UnityEngine.Object settings)
            : this(preferencePath, () => settings) {}

        public AssetSettingsProvider(string preferencePath, Func<UnityEngine.Object> settingsGetter)
            : base(preferencePath, SettingsScopes.Project)
        {
            settingsFetcher = settingsGetter;
        }

        public Editor CreateEditor()
        {
            var settings = settingsFetcher?.Invoke();
            if (settings != null)
            {
                var editor = Editor.CreateEditor(settings);
                onEditorCreated?.Invoke(editor);
                return editor;
            }

            return null;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SettingsEditor = CreateEditor();
        }

        public override void OnDeactivate()
        {
            if (m_SettingsEditor != null)
            {
                var info = m_SettingsEditor.GetType().GetMethod("OnDisable");
                info?.Invoke(m_SettingsEditor, null);
            }

            m_SettingsEditor = null;
        }

        public override void OnGUI(string searchContext)
        {
            if (m_SettingsEditor != null)
            {
                using (new EditorGUI.DisabledScope(!m_SettingsEditor.IsEnabled()))
                {
                    using (new SettingsWindow.GUIScope())
                        m_SettingsEditor.OnInspectorGUI();

                    // Emulate the Inspector by handling DnD at the native level.
                    var remainingRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true));
                    if ((Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform) && remainingRect.Contains(Event.current.mousePosition))
                    {
                        DragAndDrop.visualMode = InternalEditorUtility.InspectorWindowDrag(new[] { m_SettingsEditor.target }, Event.current.type == EventType.DragPerform);
                        if (Event.current.type == EventType.DragPerform)
                            DragAndDrop.AcceptDrag();
                    }
                }
            }

            base.OnGUI(searchContext);
        }

        public override void OnTitleBarGUI()
        {
            if (m_SettingsEditor != null)
            {
                using (new EditorGUI.DisabledScope(!m_SettingsEditor.IsEnabled()))
                {
                    var tagrObjects = new[] { m_SettingsEditor.serializedObject.targetObject };
                    var rect = GUILayoutUtility.GetRect(Styles.settingsBtn.GetFloat(StyleKeyword.width), Styles.settingsBtn.GetFloat(StyleKeyword.height));
                    rect.y = Styles.settingsBtn.GetFloat(StyleKeyword.marginTop);
                    EditorGUIUtility.DrawEditorHeaderItems(rect, tagrObjects);
                    var settingsRect = GUILayoutUtility.GetRect(Styles.settingsBtn.GetFloat(StyleKeyword.width), Styles.settingsBtn.GetFloat(StyleKeyword.height));
                    settingsRect.y = rect.y;
                    if (GUI.Button(settingsRect, EditorGUI.GUIContents.titleSettingsIcon, Styles.settingsStyle))
                    {
                        EditorUtility.DisplayObjectContextMenu(settingsRect, tagrObjects, 0);
                    }
                }
            }
        }

        public override void OnFooterBarGUI()
        {
            if (m_SettingsEditor != null)
                InspectorWindow.DrawVCSShortInfo(settingsWindow, m_SettingsEditor);
        }
    }
}
