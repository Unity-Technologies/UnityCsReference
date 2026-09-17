// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Editor.Analytics;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Provides a UI in the <see cref="OnboardingSectionCategory.GettingStarted"/> section
    /// to prompt users to install a sample based on the selected <see cref="GameGenre"/>
    /// and the content from <see cref="GettingStartedSamplesList"/>.
    /// </summary>
    [UsedImplicitly]
    class GettingStartedSampleSection : OnboardingGUIProvider<GettingStartedSampleSection>, IDataSourceViewHashProvider
    {
        const string k_GettingStartedSampleDefinitions =
            "Multiplayer/MultiplayerCenter/GettingStartedSamples.asset";

        const string k_GettingStartedSampleUxml =
            "Multiplayer/MultiplayerCenter/UI/GettingStartedSamples.uxml";

        const string k_ImportText = "Import";
        const string k_ReImportText = "Re-Import";
        const string k_ImportingText = "Importing sample...";

        GettingStartedSamplesList m_SamplesList;
        Dictionary<GameGenre, int> m_GenreToIndexMap = new();
        QuickStartSamplesManager m_SamplesManager;
        Button m_InstallButton;
        int _hash;
        VisualElement _root;
        HelpBox _projectLinkWarning;

        [CreateProperty]
        Texture2D ThemedIcon => EditorGUIUtility.isProSkin
            ? SelectedGameGenre.Icon
            : SelectedGameGenre.LightIcon != null ? SelectedGameGenre.LightIcon : SelectedGameGenre.Icon;

        [CreateProperty]
        GettingStartedSample SelectedGameGenre
        {
            get
            {
                var genre = MultiplayerCenterSettings.instance.SelectedGameGenre;
                if (m_GenreToIndexMap.TryGetValue(genre, out var index) && index < m_SamplesList.Descriptions.Count)
                    return m_SamplesList.Descriptions[index];
                return default;
            }
        }

        public override (OnboardingSectionCategory, int)[] Categories =>
            new[] { (OnboardingSectionCategory.GettingStarted, -100) };


        public override VisualElement CreateGUI()
        {
            m_SamplesManager = new QuickStartSamplesManager();

            m_SamplesList = EditorGUIUtility.LoadRequired(k_GettingStartedSampleDefinitions) as GettingStartedSamplesList;
            m_GenreToIndexMap.Clear();
            for (var i = 0; i < m_SamplesList.Descriptions.Count; i++)
            {
                m_GenreToIndexMap[m_SamplesList.Descriptions[i].Genre] = i;
            }

            var assetTree = EditorGUIUtility.LoadRequired(k_GettingStartedSampleUxml) as VisualTreeAsset;
            _root = assetTree.CloneTree();

            _root.SetBinding(nameof(VisualElement.dataSource),
                new DataBinding()
                {
                    bindingMode = BindingMode.ToTarget,
                    dataSource = this,
                    dataSourcePath = new PropertyPath(nameof(SelectedGameGenre)),
                });

            // Override the UXML source binding (which always uses the dark Icon field)
            // with a themed binding that picks the correct icon for the current editor theme.
            var imageElement = _root.Q<ImageWithPlaceholderElement>();
            if (imageElement != null)
            {
                imageElement.SetBinding("source", new DataBinding()
                {
                    bindingMode = BindingMode.ToTarget,
                    dataSource = this,
                    dataSourcePath = new PropertyPath(nameof(ThemedIcon)),
                });
            }

            SetupInstallInteraction(_root);
            
            SetupProjectLinkWarning(_root);

            _root.RegisterCallback<AttachToPanelEvent>(_ => EditorApplication.update += RefreshProjectLinkUI);
            _root.RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.update -= RefreshProjectLinkUI);

            _root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                CloudProjectSettingsEventManager.instance.projectStateChanged += RefreshProjectLinkUI;
            });
            _root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                CloudProjectSettingsEventManager.instance.projectStateChanged -= RefreshProjectLinkUI;
            });

            CheckIsEnabled();
            return _root;
        }

        void SetupProjectLinkWarning(VisualElement content)
        {
            _projectLinkWarning = content.Q<HelpBox>("project-link-warning");
            _projectLinkWarning.text = L10n.Tr("This sample requires Unity Services. Link this project in Project Settings > Services before importing.", null);
            _projectLinkWarning.buttonText = L10n.Tr("Open Services Settings", null);
            _projectLinkWarning.onButtonClicked += () => SettingsService.OpenProjectSettings("Project/Services");
            RefreshProjectLinkUI();
        }

        void SetupInstallInteraction(VisualElement content)
        {
            m_InstallButton = content.Q<Button>("install-button");
            var removeButton = content.Q<Button>("remove-button");
            if (removeButton != null)
            {
                removeButton.tooltip = L10n.Tr("Remove Sample", null);
                removeButton.clicked += () => m_SamplesManager.RemoveSample(SelectedGameGenre.SampleId);
            }

            if (m_SamplesManager.IsInstalled(SelectedGameGenre.SampleId))
            {
                m_InstallButton.text = L10n.Tr(k_ReImportText, null);
                m_InstallButton.tooltip = L10n.Tr("Sample has been already imported.", null);
                removeButton?.EnableInClassList(StyleClasses.Hidden, false);
            }

            m_InstallButton.clicked += InstallSelectedSample;
        }

        void InstallSelectedSample()
        {
            m_InstallButton.enabledSelf = false;
            m_InstallButton.text = L10n.Tr(k_ImportingText, null);

            var isReimport = m_SamplesManager.IsInstalled(SelectedGameGenre.SampleId);
            Debug.Log($"Installing sample {SelectedGameGenre.SampleId}");
            SampleImportedEvent.Send(new SampleImportedData(SelectedGameGenre.Genre, SelectedGameGenre.SampleId, isReimport));
            m_SamplesManager.Install(SelectedGameGenre.SampleId);

            // Install() sets m_ImportedSamples synchronously on success, so IsInstalled()
            // already reflects the outcome — no event subscription needed.
            m_InstallButton.enabledSelf = true;
            m_InstallButton.text = L10n.Tr(m_SamplesManager.IsInstalled(SelectedGameGenre.SampleId)
                ? k_ReImportText
                : k_ImportText, null);
        }

        public long GetViewHashCode()
        {
            var hash = MultiplayerCenterSettings.instance.SelectedGameGenre.GetHashCode();
            if (_hash == hash) return _hash;

            _hash = hash;
            CheckIsEnabled();

            return _hash;
        }

        void RefreshProjectLinkUI()
        {
            var isBound = CloudProjectSettings.projectBound;
            _projectLinkWarning?.EnableInClassList(StyleClasses.Hidden, isBound);
            if (m_InstallButton != null)
                m_InstallButton.enabledSelf = isBound;
        }

        private void CheckIsEnabled()
        {
            var isEnabled = QuickStartSamplesManager.IsQuickStartsInstalled(out _)
                && !string.IsNullOrWhiteSpace(SelectedGameGenre.SampleId);

            // don't show sample if it can't be installed
            _root.EnableInClassList(StyleClasses.Hidden, !isEnabled);
        }
    }
}
