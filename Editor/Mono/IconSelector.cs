// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class IconSelector : EditorWindow
    {
        public delegate void MonoScriptIconChangedCallback(MonoScript monoScript);
        class Styles
        {
            public GUIStyle background = "sv_iconselector_back";
            public GUIStyle seperator = "sv_iconselector_sep";
            public GUIStyle selection = "sv_iconselector_selection";
            public GUIStyle selectionLabel = "sv_iconselector_labelselection";
            public GUIStyle noneButton = "sv_iconselector_button";
        }

        static IconSelector s_IconSelector = null;
        static long s_LastClosedTime = 0;
        static int s_LastInstanceID = -1;
        static int s_HashIconSelector = "IconSelector".GetHashCode();
        static Styles m_Styles;

        Object m_TargetObject;
        Texture2D m_StartIcon;
        bool m_ShowLabelIcons;
        private GUIContent[] m_LabelLargeIcons;
        private GUIContent[] m_LabelIcons;
        private GUIContent[] m_LargeIcons;
        private GUIContent[] m_SmallIcons;
        private GUIContent m_NoneButtonContent;
        private MonoScriptIconChangedCallback m_MonoScriptIconChangedCallback;

        private GUIContent[] GetTextures(string baseName, string postFix, int startIndex, int count)
        {
            GUIContent[] textures = new GUIContent[count];
            for (int i = 0; i < count; ++i)
                textures[i] = EditorGUIUtility.IconContent(baseName + (startIndex + i) + postFix);
            return textures;
        }

        void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
        }

        void OnDisable()
        {
            SaveIconChanges();

            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            s_IconSelector = null;
        }

        void SaveIconChanges()
        {
            Texture2D currentIcon = EditorGUIUtility.GetIconForObject(m_TargetObject);

            // Only save if we had any changes
            if (currentIcon != m_StartIcon)
            {
                MonoScript monoScript = m_TargetObject as MonoScript;
                if (monoScript != null)
                {
                    // If callback is set then it is the callback owner responsiblity to call CopyMonoScriptIconToImporters
                    if (m_MonoScriptIconChangedCallback != null)
                        m_MonoScriptIconChangedCallback(monoScript);
                    else
                        MonoImporter.CopyMonoScriptIconToImporters(monoScript);
                }
            }
        }

        // Returns true if shown
        internal static bool ShowAtPosition(Object targetObj, Rect activatorRect, bool showLabelIcons)
        {
            int instanceID = targetObj.GetInstanceID();
            // We could not use realtimeSinceStartUp since it is resetted when entering playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;
            if (instanceID != s_LastInstanceID || !justClosed)
            {
                Event.current.Use();
                s_LastInstanceID = instanceID;
                if (s_IconSelector == null)
                    s_IconSelector = ScriptableObject.CreateInstance<IconSelector>();
                s_IconSelector.Init(targetObj, activatorRect, showLabelIcons);
                return true;
            }

            return false;
        }

        internal static void SetMonoScriptIconChangedCallback(MonoScriptIconChangedCallback callback)
        {
            if (s_IconSelector != null)
                s_IconSelector.m_MonoScriptIconChangedCallback = callback;
            else
                Debug.Log("ERROR: setting callback on hidden IconSelector");
        }

        void Init(Object targetObj, Rect activatorRect, bool showLabelIcons)
        {
            m_TargetObject = targetObj;
            m_StartIcon = EditorGUIUtility.GetIconForObject(m_TargetObject);
            m_ShowLabelIcons = showLabelIcons;
            Rect screenActivatorRect = GUIUtility.GUIToScreenRect(activatorRect);

            // Remove any keyboard control when opening this window
            GUIUtility.keyboardControl = 0;

            // Init GUIContents
            m_LabelLargeIcons = GetTextures("sv_label_", "", 0, 8);
            m_LabelIcons = GetTextures("sv_icon_name", "", 0, 8);
            m_SmallIcons = GetTextures("sv_icon_dot", "_sml", 0, 16);
            m_LargeIcons = GetTextures("sv_icon_dot", "_pix16_gizmo", 0, 16);
            m_NoneButtonContent = EditorGUIUtility.IconContent("sv_icon_none");
            m_NoneButtonContent.text = "None";

            // Deal with window size
            float k_Width = 140;
            float k_Height = 86;
            if (m_ShowLabelIcons)
                k_Height = 126;

            ShowAsDropDown(screenActivatorRect, new Vector2(k_Width, k_Height));
        }

        private Texture2D ConvertLargeIconToSmallIcon(Texture2D largeIcon, ref bool isLabelIcon)
        {
            if (largeIcon == null)
                return null;

            isLabelIcon = true;
            for (int i = 0; i < m_LabelLargeIcons.Length; ++i)
                if (m_LabelLargeIcons[i].image == largeIcon)
                    return (Texture2D)m_LabelIcons[i].image;

            isLabelIcon = false;
            for (int i = 0; i < m_LargeIcons.Length; ++i)
                if (m_LargeIcons[i].image == largeIcon)
                    return (Texture2D)m_SmallIcons[i].image;

            return largeIcon;
        }

        private Texture2D ConvertSmallIconToLargeIcon(Texture2D smallIcon, bool labelIcon)
        {
            if (labelIcon)
            {
                for (int i = 0; i < m_LabelIcons.Length; ++i)
                    if (m_LabelIcons[i].image == smallIcon)
                        return (Texture2D)m_LabelLargeIcons[i].image;
            }
            else
            {
                for (int i = 0; i < m_SmallIcons.Length; ++i)
                    if (m_SmallIcons[i].image == smallIcon)
                        return (Texture2D)m_LargeIcons[i].image;
            }
            return smallIcon;
        }

        void DoButton(GUIContent content, Texture2D selectedIcon, bool labelIcon)
        {
            int controlID = GUIUtility.GetControlID(s_HashIconSelector, FocusType.Keyboard);

            // Draw selection background if this is the selected icon
            if (content.image == selectedIcon)
            {
                // When placing our selection background we assume that icons rendered from top left corner
                Rect rect = GUILayoutUtility.topLevel.PeekNext();
                float pad = 2;
                rect.x -= pad;
                rect.y -= pad;
                rect.width = selectedIcon.width + 2 * pad;
                rect.height = selectedIcon.height + 2 * pad;
                GUI.Label(rect, GUIContent.none, labelIcon ? m_Styles.selectionLabel : m_Styles.selection);
            }

            // Do icon
            if (EditorGUILayout.IconButton(controlID, content, GUIStyle.none))
            {
                // Map to new icon
                Texture2D largeIcon = ConvertSmallIconToLargeIcon((Texture2D)content.image, labelIcon);
                Undo.RecordObject(m_TargetObject, "Set Icon On GameObject");
                EditorGUIUtility.SetIconForObject(m_TargetObject, largeIcon);

                // We assume that we are setting icon in an inspector or annotation window (todo: make repaint delegate)
                EditorUtility.ForceReloadInspectors();
                AnnotationWindow.IconChanged();

                // Close on double click
                if (Event.current.clickCount == 2)
                    CloseWindow();
            }
        }

        void DoTopSection(bool anySelected)
        {
            Rect selectIconRect = new Rect(6, 4, 110, 20);
            GUI.Label(selectIconRect, "Select Icon");

            // Draw selection background if none is selected
            using (new EditorGUI.DisabledScope(!anySelected))
            {
                Rect noneButtonRect = new Rect(93, 6, 43, 12);
                if (GUI.Button(noneButtonRect, m_NoneButtonContent, m_Styles.noneButton))
                {
                    EditorGUIUtility.SetIconForObject(m_TargetObject, null);
                    EditorUtility.ForceReloadInspectors();
                    AnnotationWindow.IconChanged();
                }
            }
        }

        private void CloseWindow()
        {
            Close();
            GUI.changed = true;
            GUIUtility.ExitGUI();
        }

        internal void OnGUI()
        {
            // Here due to static vars being thrown out when reloading assemblies
            if (m_Styles == null)
                m_Styles = new Styles();

            // Escape pressed?
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                CloseWindow();
            }

            Texture2D selectedIcon = EditorGUIUtility.GetIconForObject(m_TargetObject);
            bool isSelectionALabelIcon = false;
            if (Event.current.type == EventType.Repaint)
            {
                selectedIcon = ConvertLargeIconToSmallIcon(selectedIcon, ref isSelectionALabelIcon);
            }

            Event evt = Event.current;
            EventType eventType = evt.type;

            GUI.BeginGroup(new Rect(0, 0, position.width, position.height), m_Styles.background);

            DoTopSection(selectedIcon != null);

            GUILayout.Space(22);
            GUILayout.BeginHorizontal();
            GUILayout.Space(1);
            GUI.enabled = false;
            GUILayout.Label("", m_Styles.seperator);
            GUI.enabled = true;
            GUILayout.Space(1);
            GUILayout.EndHorizontal();
            GUILayout.Space(3);

            if (m_ShowLabelIcons)
            {
                // Label icons
                GUILayout.BeginHorizontal();
                GUILayout.Space(6);
                for (int i = 0; i < m_LabelIcons.Length / 2; ++i)
                    DoButton(m_LabelIcons[i], selectedIcon, true);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                GUILayout.Space(6);
                for (int i = m_LabelIcons.Length / 2; i < m_LabelIcons.Length; ++i)
                    DoButton(m_LabelIcons[i], selectedIcon, true);
                GUILayout.EndHorizontal();

                GUILayout.Space(3);
                GUILayout.BeginHorizontal();
                GUILayout.Space(1);
                GUI.enabled = false;
                GUILayout.Label("", m_Styles.seperator);
                GUI.enabled = true;
                GUILayout.Space(1);
                GUILayout.EndHorizontal();
                GUILayout.Space(3);
            }

            // Small icons
            GUILayout.BeginHorizontal();
            GUILayout.Space(9);
            for (int i = 0; i < m_SmallIcons.Length / 2; ++i)
                DoButton(m_SmallIcons[i], selectedIcon, false);
            GUILayout.Space(3);
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Space(9);
            for (int i = m_SmallIcons.Length / 2; i < m_SmallIcons.Length; ++i)
                DoButton(m_SmallIcons[i], selectedIcon, false);
            GUILayout.Space(3);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);

            // Object selector section
            GUI.backgroundColor = new Color(1, 1, 1, 0.7f);
            bool clicked = false;
            int id = GUIUtility.GetControlID(s_HashIconSelector, FocusType.Keyboard);
            if (GUILayout.Button(EditorGUIUtility.TempContent("Other...")))
            {
                GUIUtility.keyboardControl = id;
                clicked = true;
            }
            GUI.backgroundColor = new Color(1, 1, 1, 1);
            GUI.EndGroup();

            if (clicked)
            {
                ObjectSelector.get.Show(m_TargetObject, typeof(Texture2D), null, false);
                ObjectSelector.get.objectSelectorID = id;
                GUI.backgroundColor = new Color(1, 1, 1, 0.7f);
                evt.Use();
                GUIUtility.ExitGUI();
            }

            // Check for icon selected from ObjectSelector
            switch (eventType)
            {
                case EventType.ExecuteCommand:
                    string commandName = evt.commandName;
                    if (commandName == "ObjectSelectorUpdated" && ObjectSelector.get.objectSelectorID == id && GUIUtility.keyboardControl == id)
                    {
                        Texture2D icon =  ObjectSelector.GetCurrentObject() as Texture2D;
                        Undo.RecordObject(m_TargetObject, "Set Icon On GameObject");
                        EditorGUIUtility.SetIconForObject(m_TargetObject, icon);

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
            }
        }
    }
}
