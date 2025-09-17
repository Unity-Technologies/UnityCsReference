// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// This class is responsible for creating the play mode buttons in the toolbar.
    /// </summary>
    class PlayModeButtonsExtension : AssetPostprocessor
    {
        static VisualElement[] m_OriginalButtons;
        static PlayModeButtonsView m_PlayModeButtons;
        static VisualElement m_PlayModeConfigUI;

        internal static PlayModeButtonsView PlayModeButtons => m_PlayModeButtons;

        [InitializeOnLoadMethod]
        internal static void Initialize()
        {
            if (PlayModeConfigurationUtils.ConfigurationTypesCount == 0)
                return;

            UnityEditor.Toolbars.PlayModeButtons.onPlayModeButtonsCreated -= CreatePlayModeButtons;
            UnityEditor.Toolbars.PlayModeButtons.onPlayModeButtonsCreated += CreatePlayModeButtons;
            PlayModeManager.instance.ConfigAssetChanged -= RefreshPlayModeConfigUI;
            PlayModeManager.instance.ConfigAssetChanged += RefreshPlayModeConfigUI;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths, bool didDomainReload)
        {
            RefreshPlayModeConfigUI();
        }

        /// <summary>
        /// Hides the original play mode buttons and creates the new ones.
        /// </summary>
        /// <param name="container">
        /// The container of the old play mode buttons.
        /// </param>
        internal static void CreatePlayModeButtons(VisualElement container)
        {
            var buttonsStrip = container.Q(className: "unity-editor-toolbar__button-strip");

            // Fixes an Issue when running tests.
            m_PlayModeButtons = null;

            // m_OriginalButtons = buttonsStrip.Children().ToArray();
            m_OriginalButtons = new VisualElement[buttonsStrip.childCount];
            for (var i = 0; i < buttonsStrip.childCount; i++)
            {
                m_OriginalButtons[i] = buttonsStrip.ElementAt(i);
            }

            Assert.AreEqual(3, m_OriginalButtons.Length);

            foreach (var button in m_OriginalButtons)
                button.style.display = DisplayStyle.None;

            container.Add(m_PlayModeButtons = new PlayModeButtonsView());

            m_PlayModeConfigUI = new VisualElement();
            m_PlayModeConfigUI.style.flexDirection = FlexDirection.Row;
            m_PlayModeButtons.Insert(0, m_PlayModeConfigUI);
            
            // Restore the dropdown after toolbar recreation
            RefreshPlayModeConfigUI();
        }

        /// <summary>
        /// Makes changes to the play mode buttons UI based on the current play mode config.
        /// </summary>
        internal static void RefreshPlayModeConfigUI()
        {
            if (m_PlayModeConfigUI == null)
                return;

            m_PlayModeConfigUI.Clear();

            var config = PlayModeManager.instance.ActivePlayModeConfig;
            if (config == null)
                return;

            var uiElement = config.CreateTopbarUI();
            if (uiElement == null)
                return;

            m_PlayModeConfigUI.Add(uiElement);
        }
    }
}
