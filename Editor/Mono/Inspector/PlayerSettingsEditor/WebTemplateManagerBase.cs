// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor
{
    internal abstract class WebTemplateManagerBase
    {
        class Styles
        {
            public GUIStyle thumbnail = "IN ThumbnailShadow";
            public GUIStyle thumbnailLabel = "IN ThumbnailSelection";
        }

        private static Styles s_Styles;

        private WebTemplate[] s_Templates = null;
        private GUIContent[] s_TemplateGUIThumbnails = null;

        // Template layout constants
        const float kWebTemplateGridPadding = 15.0f;
        const float kThumbnailSize = 80.0f;
        const float kThumbnailLabelHeight = 20.0f;
        const float kThumbnailPadding = 5.0f;

        public abstract string customTemplatesFolder
        {
            get;
        }

        public abstract string builtinTemplatesFolder
        {
            get;
        }

        public abstract Texture2D defaultIcon
        {
            get;
        }

        public WebTemplate[] Templates
        {
            get
            {
                if (s_Templates == null || s_TemplateGUIThumbnails == null)
                {
                    BuildTemplateList();
                }

                return s_Templates;
            }
        }

        public GUIContent[] TemplateGUIThumbnails
        {
            get
            {
                if (s_Templates == null || s_TemplateGUIThumbnails == null)
                {
                    BuildTemplateList();
                }

                return s_TemplateGUIThumbnails;
            }
        }

        public abstract string[] GetCustomKeys(string path);

        public int GetTemplateIndex(string path)
        {
            for (int i = 0; i < Templates.Length; i++)
            {
                if (path.Equals(Templates[i].ToString()))
                {
                    return i;
                }
            }
            return -1;
        }

        public void ClearTemplates()
        {
            s_Templates = null;
            s_TemplateGUIThumbnails = null;
        }

        private void BuildTemplateList()
        {
            List<WebTemplate> templates = new List<WebTemplate>();

            if (Directory.Exists(customTemplatesFolder))
            {
                templates.AddRange(ListTemplates(customTemplatesFolder));
            }

            if (Directory.Exists(builtinTemplatesFolder))
            {
                templates.AddRange(ListTemplates(builtinTemplatesFolder));
            }
            else
            {
                Debug.LogError("Did not find built-in templates.");
            }

            s_Templates = templates.ToArray();

            s_TemplateGUIThumbnails = new GUIContent[s_Templates.Length];
            for (int i = 0; i < s_TemplateGUIThumbnails.Length; i++)
            {
                s_TemplateGUIThumbnails[i] = s_Templates[i].ToGUIContent(defaultIcon);
            }
        }

        private WebTemplate Load(string path)
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            WebTemplate template = new WebTemplate();

            string[] splitPath = path.Split(new char[] {'/', '\\'});
            template.m_Name = splitPath[splitPath.Length - 1];
            if (splitPath.Length > 3 && splitPath[splitPath.Length - 3].Equals("Assets"))
            {
                template.m_Path = "PROJECT:" + template.m_Name;
            }
            else
            {
                template.m_Path = "APPLICATION:" + template.m_Name;
            }

            string thumbnailPath = Path.Combine(path, "thumbnail.png");
            if (File.Exists(thumbnailPath))
            {
                template.m_Thumbnail = new Texture2D(2, 2);
                template.m_Thumbnail.LoadImage(File.ReadAllBytes(thumbnailPath));
            }

            template.m_CustomKeys = GetCustomKeys(path);

            return template;
        }

        private List<WebTemplate> ListTemplates(string path)
        {
            List<WebTemplate> templates = new List<WebTemplate>();
            string[] directories = Directory.GetDirectories(path);
            foreach (string directory in directories)
            {
                // skip WebGLIncludes
                string[] separated_dirs = directory.Split(Path.DirectorySeparatorChar);
                if (separated_dirs[separated_dirs.Length - 1].Equals("WebGLIncludes")) {
                    continue;
                }
                WebTemplate template = Load(directory);
                if (template != null)
                {
                    templates.Add(template);
                }
            }
            return templates;
        }

        public void SelectionUI(SerializedProperty templateProp)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            bool templateChanged = false;
            using (var vertical = new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUI.PropertyScope(vertical.rect, GUIContent.none, templateProp))
                {

                    var selectedTemplateIndex = GetTemplateIndex(templateProp.stringValue);
                    if (selectedTemplateIndex < 0)
                        EditorGUILayout.HelpBox("No valid template is selected. Choose a template to proceed.", MessageType.Error);

                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        if (TemplateGUIThumbnails.Length < 1)
                        {
                            GUILayout.Label(EditorGUIUtility.TrTextContent("No templates found."));
                        }
                        else
                        {
                            int numCols = Mathf.Min((int)Mathf.Max((Screen.width - kWebTemplateGridPadding * 2.0f) / kThumbnailSize, 1.0f), TemplateGUIThumbnails.Length);
                            int numRows = Mathf.Max((int)Mathf.Ceil((float)TemplateGUIThumbnails.Length / (float)numCols), 1);

                            var updatedSelectedTemplateIndex =  ThumbnailList(
                                GUILayoutUtility.GetRect(numCols * kThumbnailSize, numRows * (kThumbnailSize + kThumbnailLabelHeight), GUILayout.ExpandWidth(false)),
                                selectedTemplateIndex,
                                TemplateGUIThumbnails,
                                numCols
                            );
                            templateChanged = selectedTemplateIndex != updatedSelectedTemplateIndex;

                            // Only set/update templateProp and selectedTemplateIndex if there is a valid template selection.
                            if (updatedSelectedTemplateIndex > -1)
                            {
                                templateProp.stringValue = Templates[updatedSelectedTemplateIndex].ToString();
                                selectedTemplateIndex = updatedSelectedTemplateIndex;
                            }
                        }
                    }

                    bool orgChanged = GUI.changed;
                    GUI.changed = false;

                    var templateCustomKeys = new string[]{};
                    if (selectedTemplateIndex > -1)
                    {
                        templateCustomKeys = Templates[GetTemplateIndex(templateProp.stringValue)].CustomKeys;
                        foreach (string key in templateCustomKeys)
                        {
                            string value = PlayerSettings.GetTemplateCustomValue(key);
                            value = EditorGUILayout.TextField(PrettyTemplateKeyName(key), value);
                            PlayerSettings.SetTemplateCustomValue(key, value);
                        }
                    }

                    if (GUI.changed)
                        templateProp.serializedObject.Update();
                    GUI.changed |= orgChanged;

                    if (templateChanged)
                    {
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        templateProp.serializedObject.ApplyModifiedProperties();
                        PlayerSettings.templateCustomKeys = templateCustomKeys;
                        templateProp.serializedObject.Update();
                    }
                }
            }
        }

        static int ThumbnailList(Rect rect, int selection, GUIContent[] thumbnails, int maxRowItems)
        {
            for (int y = 0, i = 0; i < thumbnails.Length; y++)
            {
                for (int x = 0; x < maxRowItems && i < thumbnails.Length; x++, i++)
                {
                    if (ThumbnailListItem(
                        new Rect(rect.x + x * kThumbnailSize, rect.y + y * (kThumbnailSize + kThumbnailLabelHeight), kThumbnailSize, (kThumbnailSize + kThumbnailLabelHeight)),
                        i == selection, thumbnails[i]))
                    {
                        selection = i;
                    }
                }
            }
            return selection;
        }

        static bool ThumbnailListItem(Rect rect, bool selected, GUIContent content)
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        if (!selected)
                        {
                            GUI.changed = true;
                        }
                        selected = true;
                        Event.current.Use();
                    }
                    break;
                case EventType.Repaint:
                    Rect thumbRect = new Rect(rect.x + kThumbnailPadding, rect.y + kThumbnailPadding, rect.width - kThumbnailPadding * 2.0f, rect.height - kThumbnailLabelHeight - kThumbnailPadding * 2.0f);
                    s_Styles.thumbnail.Draw(thumbRect, content.image, false, false, selected, selected);
                    s_Styles.thumbnailLabel.Draw(new Rect(rect.x, rect.y + rect.height - kThumbnailLabelHeight, rect.width, kThumbnailLabelHeight), content.text, false, false, selected, selected);
                    break;
            }
            return selected;
        }

        static string PrettyTemplateKeyName(string name)
        {
            string[] elements = name.Split('_');

            elements[0] = UppercaseFirst(elements[0].ToLower());
            for (int i = 1; i < elements.Length; i++)
            {
                elements[i] = elements[i].ToLower();
            }

            return string.Join(" ", elements);
        }

        static string UppercaseFirst(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return string.Empty;
            }
            return char.ToUpper(target[0]) + target.Substring(1);
        }
    }
}
