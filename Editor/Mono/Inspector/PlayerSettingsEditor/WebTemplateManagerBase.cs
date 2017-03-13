// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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

        public int GetTemplateIndex(string path)
        {
            for (int i = 0; i < Templates.Length; i++)
            {
                if (path.Equals(Templates[i].ToString()))
                {
                    return i;
                }
            }
            return 0;
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
            if (!Directory.Exists(path) || Directory.GetFiles(path, "index.*").Length < 1)
            {
                return null;
            }

            string[] splitPath = path.Split(new char[] {'/', '\\'});

            WebTemplate template = new WebTemplate();

            template.m_Name = splitPath[splitPath.Length - 1];
            if (splitPath.Length > 3 && splitPath[splitPath.Length - 3].Equals("Assets"))
            {
                template.m_Path = "PROJECT:" + template.m_Name;
            }
            else
            {
                template.m_Path = "APPLICATION:" + template.m_Name;
            }

            string[] thumbFiles = Directory.GetFiles(path, "thumbnail.*");
            if (thumbFiles.Length > 0)
            {
                template.m_Thumbnail = new Texture2D(2, 2);
                template.m_Thumbnail.LoadImage(File.ReadAllBytes(thumbFiles[0]));
            }

            List<string> keys = new List<string>();
            Regex customKeyFinder = new Regex("\\%UNITY_CUSTOM_([A-Z_]+)\\%");
            MatchCollection matches = customKeyFinder.Matches(File.ReadAllText(Directory.GetFiles(path, "index.*")[0]));
            foreach (Match match in matches)
            {
                string name = match.Value.Substring("%UNITY_CUSTOM_".Length);
                name = name.Substring(0, name.Length - 1);
                if (!keys.Contains(name))
                {
                    keys.Add(name);
                }
            }
            template.m_CustomKeys = keys.ToArray();

            return template;
        }

        private List<WebTemplate> ListTemplates(string path)
        {
            List<WebTemplate> templates = new List<WebTemplate>();
            string[] directories = Directory.GetDirectories(path);
            foreach (string directory in directories)
            {
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

            if (TemplateGUIThumbnails.Length < 1)
            {
                GUILayout.Label(EditorGUIUtility.TextContent("No templates found."));
            }
            else
            {
                int numCols = Mathf.Min((int)Mathf.Max((Screen.width - kWebTemplateGridPadding * 2.0f) / kThumbnailSize, 1.0f), TemplateGUIThumbnails.Length);
                int numRows = Mathf.Max((int)Mathf.Ceil((float)TemplateGUIThumbnails.Length / (float)numCols), 1);


                bool wasChanged = GUI.changed;

                templateProp.stringValue = Templates[
                        ThumbnailList(
                            GUILayoutUtility.GetRect(numCols * kThumbnailSize, numRows * (kThumbnailSize + kThumbnailLabelHeight), GUILayout.ExpandWidth(false)),
                            GetTemplateIndex(templateProp.stringValue),
                            TemplateGUIThumbnails,
                            numCols
                            )].ToString();

                bool templateChanged = !wasChanged && GUI.changed;

                bool orgChanged = GUI.changed;
                GUI.changed = false;
                foreach (string key in PlayerSettings.templateCustomKeys)
                {
                    string value = PlayerSettings.GetTemplateCustomValue(key);
                    value = EditorGUILayout.TextField(PrettyTemplateKeyName(key), value);
                    PlayerSettings.SetTemplateCustomValue(key, value);
                }
                if (GUI.changed)
                    templateProp.serializedObject.Update();
                GUI.changed |= orgChanged;

                if (templateChanged)
                {
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                    templateProp.serializedObject.ApplyModifiedProperties();
                    PlayerSettings.templateCustomKeys = Templates[GetTemplateIndex(templateProp.stringValue)].CustomKeys;
                    templateProp.serializedObject.Update();
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
