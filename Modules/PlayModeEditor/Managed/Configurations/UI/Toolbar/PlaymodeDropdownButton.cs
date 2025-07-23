// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Toolbars;

namespace Unity.PlayMode.Editor
{
    class PlaymodeDropdownButton : EditorToolbarButton
    {
        const string k_Stylesheet = "PlayMode/UI/PlaymodeDropdownButton.uss";
        const string k_PlaymodeDropdownName = "playmode-dropdown";

        public PlaymodeDropdownButton()
        {
            name = k_PlaymodeDropdownName;
            var arrow = new VisualElement();
            icon = EditorGUIUtility.FindTexture("UnityLogo");
            arrow.AddToClassList("unity-icon-arrow");
            Add(arrow);
            RegisterCallback<ClickEvent>(evt => ShowPlaymodeConfigPopup());
            styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            UpdateUI(PlayModeManager.instance.ActivePlayModeConfig);
        }

        public void UpdateUI(PlayModeConfiguration config)
        {
            text = config.name;
            ValidateCurrentConfiguration(config);
        }

        void ValidateCurrentConfiguration(PlayModeConfiguration config)
        {
            icon = config.Icon;
            tooltip = string.IsNullOrEmpty(config.Description) ? "" : config.Description;

            if (!config.IsConfigurationValid(out var error))
            {
                icon = EditorGUIUtility.FindTexture("console.warnicon");
                tooltip = error;
            }
        }

        void ShowPlaymodeConfigPopup()
        {
            UnityEditor.PopupWindow.Show(new Rect(worldBound.x + worldBound.width - PlaymodePopupContent.windowSize.x, worldBound.y, worldBound.width, worldBound.height), new PlaymodePopupContent());
        }
    }
}
