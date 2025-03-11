// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShortcutManagement
{
    class ShortcutHelperBar : ShortcutHelperBarUtility.IShortcutUpdate
    {
        public const int k_HelperBarMinWidth = 300;

        const int k_HelperBarMaxWidth = 8192;

        const string k_DisableHelperBar = "Disable Shortcut Helper Bar";

        bool m_Initialized;

        GUIView m_Window;

        GUIStyle m_HelpboxStyle;
        public GUIStyle helpboxStyle
        {
            get
            {
                if (m_HelpboxStyle != null)
                    return m_HelpboxStyle;

                m_HelpboxStyle = new GUIStyle(EditorStyles.helpBox);

                var previousMargins = m_HelpboxStyle.margin;
                m_HelpboxStyle.margin = new RectOffset(0, previousMargins.right, 0, 0);

                var previousPadding = m_HelpboxStyle.padding;
                m_HelpboxStyle.padding = new RectOffset(0, previousPadding.right, 0, 0);

                m_HelpboxStyle.fixedHeight = 18;
                m_HelpboxStyle.stretchHeight = false;

                return m_HelpboxStyle;
            }
        }

        GUIStyle m_LabelStyle;
        public GUIStyle labelStyle
        {
            get
            {
                if (m_LabelStyle != null)
                    return m_LabelStyle;

                m_LabelStyle = new GUIStyle(EditorStyles.label);

                var color = m_LabelStyle.normal.textColor;
                m_LabelStyle.normal.textColor = new Color(color.r, color.g, color.b, 0.5f);

                return m_LabelStyle;
            }
        }

        GUIStyle m_BoldLabelStyle;
        public GUIStyle boldLabelStyle
        {
            get
            {
                if (m_BoldLabelStyle != null)
                    return m_BoldLabelStyle;

                m_BoldLabelStyle = new GUIStyle(EditorStyles.boldLabel);

                var normalColor = m_BoldLabelStyle.normal.textColor;
                m_BoldLabelStyle.normal.textColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.6f);

                var hoverColor = m_BoldLabelStyle.hover.textColor;
                m_BoldLabelStyle.hover.textColor = new Color(hoverColor.r, hoverColor.g, hoverColor.b, 0.6f);

                return m_BoldLabelStyle;
            }
        }

        GUIStyle m_MiniButtonStyle;
        public GUIStyle miniButtonStyle
        {
            get
            {
                if (m_MiniButtonStyle != null)
                    return m_MiniButtonStyle;

                m_MiniButtonStyle = new GUIStyle(EditorStyles.miniButton);

                var previousMargins = m_MiniButtonStyle.margin;
                m_MiniButtonStyle.margin = new RectOffset(0, previousMargins.right, 0, 0);

                var previousPadding = m_MiniButtonStyle.padding;
                m_MiniButtonStyle.padding = new RectOffset(previousPadding.left, previousPadding.right, 0, 0);

                m_MiniButtonStyle.fixedHeight = 18;
                m_MiniButtonStyle.stretchHeight = false;

                return m_MiniButtonStyle;
            }
        }

        public ShortcutHelperBar(GUIView window)
        {
            m_Window = window;
        }

        public void DrawStatusBarShortcuts()
        {
            Init();

            EditorGUILayout.BeginHorizontalScrollView(Vector2.zero, false, GUIStyle.none, GUI.skin.scrollView);

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true),
                GUILayout.MaxWidth(k_HelperBarMaxWidth), GUILayout.MinWidth(k_HelperBarMinWidth));

            // Draw the modifiers that are held down.
            var drawEnabledShortcutModifiers = true;
            foreach (var kvp in ShortcutHelperBarUtility.groupedShortcuts)
            {
                if (drawEnabledShortcutModifiers) // Draw the modifiers that are held down.
                {
                    GUILayout.BeginHorizontal();

                    var modifierTextBuilder = new StringBuilder();
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        if (ShortcutHelperBarUtility.filterControl)
                            modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.controlModifierLabel);

                        if (ShortcutHelperBarUtility.filterAction)
                            modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.actionModifierLabel);
                    }
                    else
                    {
                        if (ShortcutHelperBarUtility.filterControl || ShortcutHelperBarUtility.filterAction)
                            modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.actionModifierLabel);
                    }

                    if (ShortcutHelperBarUtility.filterShift)
                        modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.shiftModifierLabel);

                    if (ShortcutHelperBarUtility.filterAlt)
                        modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.altModifierLabel);

                    // remove starting space and display
                    if (modifierTextBuilder.Length > 0)
                    {
                        modifierTextBuilder.Remove(0, 1);
                        GUILayout.Button(modifierTextBuilder.ToString(), miniButtonStyle);
                    }

                    drawEnabledShortcutModifiers = false;
                }
                else // Draw the shortcut buttons.
                {
                    var currentModifierGroup = kvp.Key;
                    var filterCondition = ShortcutHelperBarUtility.GetFilterCondition();
                    if (!currentModifierGroup.Equals(filterCondition))
                    {
                        GUILayout.BeginHorizontal(helpboxStyle);

                        var modifierTextBuilder = new StringBuilder();
                        var modifiersToDisplay = currentModifierGroup & ~filterCondition;
                        if (Application.platform == RuntimePlatform.OSXEditor)
                        {
                            if (modifiersToDisplay.HasFlag(ShortcutModifiers.Control))
                                modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.controlModifierLabel);

                            if (modifiersToDisplay.HasFlag(ShortcutModifiers.Action))
                                modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.actionModifierLabel);
                        }
                        else
                        {
                            if (modifiersToDisplay.HasFlag(ShortcutModifiers.Control)
                                || modifiersToDisplay.HasFlag(ShortcutModifiers.Action))
                                modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.actionModifierLabel);
                        }

                        if (modifiersToDisplay.HasFlag(ShortcutModifiers.Shift))
                            modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.shiftModifierLabel);

                        if (modifiersToDisplay.HasFlag(ShortcutModifiers.Alt))
                            modifierTextBuilder.Append(" ").Append(ShortcutHelperBarUtility.altModifierLabel);

                        // replace starting space and display
                        if (modifierTextBuilder.Length > 0)
                        {
                            modifierTextBuilder.Replace(' ', '+', 0, 1);
                            CreateModifierButton(new GUIContent(modifierTextBuilder.ToString()));
                        }
                    }
                }

                foreach (var shortcut in kvp.Value)
                    CreateShortcutBox(shortcut);

                GUILayout.EndHorizontal();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        void Init()
        {
            if (m_Initialized)
                return;

            ShortcutHelperBarUtility.OnClientEnable(this);
            m_Initialized = true;
        }

        public void OnShortcutUpdate()
        {
            m_Window.Repaint();
        }

        void CreateModifierButton(GUIContent content)
        {
            GUI.enabled = false;
            GUILayout.Button(content, miniButtonStyle);
            GUI.enabled = true;
        }

        void CreateShortcutBox(ShortcutEntry shortcut)
        {
            var name = Path.GetFileName(shortcut.displayName);
            var keyCode = shortcut.combinations[0].keyCode;
            var content = ShortcutHelperBarUtility.GetMouseContentForShortcut(shortcut);

            if (content != null && content.image != null)
            {
                content.text = name;
                CreateShortcutButton(content, shortcut);
            }
            else
            {
                if (content != null && content.image == null) // For mouse buttons Mouse3 to Mouse6.
                    GUILayout.Label(content.text, boldLabelStyle);
                else // For shortcuts with keyboard bindings.
                    GUILayout.Label(new GUIContent(keyCode.ToString()), boldLabelStyle);

                var shortcutNameContent = new GUIContent(name);
                CreateShortcutButton(shortcutNameContent, shortcut);
            }
        }

        void CreateShortcutButton(GUIContent content, ShortcutEntry shortcut)
        {
            if (GUILayout.Button(content, labelStyle))
                OpenShortcutManagerWindow(shortcut);
        }

        void OpenShortcutManagerWindow(ShortcutEntry shortcut)
        {
            var evt = Event.current;
            if (evt.button == 0)
            {
                ShortcutManagerWindow shortcutManager = EditorWindow.GetWindow<ShortcutManagerWindow>();
                shortcutManager.rootVisualElement.Q<ToolbarPopupSearchField>().value = shortcut.displayName;
                
                ShortcutHelperBarUtility.Reset();
                GUIUtility.ExitGUI();
            }
            else if (evt.button == 1)
            {
                GenericMenu contextMenu = new GenericMenu();
                contextMenu.AddItem(new GUIContent(k_DisableHelperBar), false,
                    () =>
                    {
                        EditorPrefs.SetBool("EnableShortcutHelperBar", false);
                        ShortcutIntegration.instance.contextManager.SetFocusedWindow(EditorWindow.focusedWindow);
                        ShortcutHelperBarUtility.RemoveAppStatusBarClient();
                        m_Initialized = false;
                    });
                contextMenu.ShowAsContext();
            }
        }

        public void OnDisable()
        {
            m_Initialized = false;
            ShortcutHelperBarUtility.OnClientDisable(this);
        }
    }
}
