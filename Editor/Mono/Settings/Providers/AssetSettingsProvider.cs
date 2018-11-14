// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.StyleSheets;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor
{
    public class AssetSettingsProvider : SettingsProvider
    {
        static class Styles
        {
            public static GUIStyle settingsStyle = "SettingsIconButton";
            public static StyleBlock settingsBtn => EditorResources.GetStyle("settings-icon-btn");
        }

        Func<Editor> m_EditorCreator;

        public Editor settingsEditor { get; private set; }

        public AssetSettingsProvider(string settingsWindowPath, Func<Editor> editorCreator, IEnumerable<string> keywords = null)
            : base(settingsWindowPath, SettingsScope.Project, keywords)
        {
            m_EditorCreator = editorCreator;
        }

        public AssetSettingsProvider(string settingsWindowPath, Func<UnityEngine.Object> settingsGetter)
            : this(settingsWindowPath, () => CreateEditorFromSettingsObject(settingsGetter()))
        {
        }

        public static AssetSettingsProvider CreateProviderFromAssetPath(string settingsWindowPath, string assetPath, IEnumerable<string> keywords = null)
        {
            return new AssetSettingsProvider(settingsWindowPath, () =>
            {
                var settingsObj = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                if (settingsObj != null)
                {
                    return Editor.CreateEditor(settingsObj);
                }
                return null;
            }, keywords);
        }

        public static AssetSettingsProvider CreateProviderFromObject(string settingsWindowPath, UnityEngine.Object settingsObj, IEnumerable<string> keywords = null)
        {
            return new AssetSettingsProvider(settingsWindowPath, () => CreateEditorFromSettingsObject(settingsObj), keywords);
        }

        public static AssetSettingsProvider CreateProviderFromResourcePath(string settingsWindowPath, string resourcePath, IEnumerable<string> keywords = null)
        {
            return new AssetSettingsProvider(settingsWindowPath, () =>
            {
                var resourceObj = Resources.Load(resourcePath);
                if (resourceObj != null)
                {
                    return Editor.CreateEditor(resourceObj);
                }
                return null;
            }, keywords);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            settingsEditor = m_EditorCreator?.Invoke();
            base.OnActivate(searchContext, rootElement);
        }

        public override void OnDeactivate()
        {
            if (settingsEditor != null)
            {
                var info = settingsEditor.GetType().GetMethod("OnDisable");
                info?.Invoke(settingsEditor, null);
            }

            settingsEditor = null;
            base.OnDeactivate();
        }

        public override void OnGUI(string searchContext)
        {
            if (settingsEditor != null)
            {
                using (new EditorGUI.DisabledScope(!settingsEditor.IsEnabled()))
                {
                    using (new SettingsWindow.GUIScope())
                        settingsEditor.OnInspectorGUI();

                    // Emulate the Inspector by handling DnD at the native level.
                    var remainingRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true));
                    if ((Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform) && remainingRect.Contains(Event.current.mousePosition))
                    {
                        DragAndDrop.visualMode = InternalEditorUtility.InspectorWindowDrag(new[] { settingsEditor.target }, Event.current.type == EventType.DragPerform);
                        if (Event.current.type == EventType.DragPerform)
                            DragAndDrop.AcceptDrag();
                    }
                }
            }

            base.OnGUI(searchContext);
        }

        public override void OnTitleBarGUI()
        {
            if (settingsEditor != null)
            {
                using (new EditorGUI.DisabledScope(!settingsEditor.IsEnabled()))
                {
                    var tagrObjects = new[] { settingsEditor.serializedObject.targetObject };
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
                base.OnTitleBarGUI();
            }
        }

        public override void OnFooterBarGUI()
        {
            if (settingsEditor != null)
                InspectorWindow.DrawVCSShortInfo(settingsWindow, settingsEditor);
        }

        private static Editor CreateEditorFromSettingsObject(UnityEngine.Object settingsObj)
        {
            if (settingsObj != null)
            {
                return Editor.CreateEditor(settingsObj);
            }
            return null;
        }
    }
}
