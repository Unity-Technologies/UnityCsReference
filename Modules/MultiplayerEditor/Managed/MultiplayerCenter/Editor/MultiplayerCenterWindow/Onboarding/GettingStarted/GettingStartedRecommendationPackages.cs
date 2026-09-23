// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Unity.Multiplayer.Center.Common;
using Unity.Multiplayer.Center.Editor.Analytics;
using Unity.Properties;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Displays a list of packages in the Multiplayer Center window,
    /// based on the selected <see cref="GameGenre"/> and the list from <see cref="Recommendations"/>.
    /// </summary>
    [UsedImplicitly]
    class GettingStartedRecommendationPackages : OnboardingGUIProvider<GettingStartedRecommendationPackages>,
        IDataSourceViewHashProvider
    {
        static readonly BindingId k_StyleDisplay =
            nameof(VisualElement.style) + "." + nameof(VisualElement.style.display);

        public override (OnboardingSectionCategory, int)[] Categories =>
            new[] { (OnboardingSectionCategory.GettingStarted, -50) };

        const string k_PendingPackagesKey = "MultiplayerCenter_PendingInstallPackages";

        Recommendations m_Recommendations;
        Dictionary<GameGenre, int> m_GenreToIndexMap = new();
        AddAndRemoveRequest m_Request;
        TemplateContainer m_Root;
        long m_ViewHashCode;
        Dictionary<string, RecommendationTier> m_PendingPackages = new();

        public override VisualElement CreateGUI()
        {
            m_Recommendations = EditorGUIUtility.LoadRequired(
                "Multiplayer/MultiplayerCenter/PackagesRecommendations.asset") as Recommendations;
            m_GenreToIndexMap.Clear();
            for (var i = 0; i < m_Recommendations.Descriptions.Count; i++)
            {
                m_GenreToIndexMap[m_Recommendations.Descriptions[i].Genre] = i;
            }

            m_Root = (EditorGUIUtility.LoadRequired("Multiplayer/MultiplayerCenter/UI/Recommendations.uxml") as VisualTreeAsset)
                .CloneTree();
            m_Root.SetBinding(nameof(VisualElement.dataSource),
                new DataBinding()
                {
                    bindingMode = BindingMode.ToTarget,
                    dataSource = this,
                    dataSourcePath = new PropertyPath(nameof(SelectedGameGenre)),
                });

            ConfigureButtons();

            var essentials = m_Root.Q<VisualElement>("essentials");
            essentials.SetBinding(k_StyleDisplay,
                new DataBinding()
                {
                    dataSource = this,
                    dataSourcePath = new PropertyPath(nameof(ShowEssentials)),
                    bindingMode = BindingMode.ToTarget,
                });
            var recommended = m_Root.Q<VisualElement>("recommended");
            recommended.SetBinding(k_StyleDisplay,
                new DataBinding()
                {
                    dataSource = this,
                    dataSourcePath = new PropertyPath(nameof(ShowRecommended)),
                    bindingMode = BindingMode.ToTarget,
                });
            m_Root.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                RestorePendingPackages();
                PackageIcon.PackageOpened += OnPackageIconOpened;
                QuickStartSamplesManager.SampleImportStarted += OnSampleImportStarted;
                Events.registeredPackages += OnPackagesRegistered;
            });
            m_Root.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                PackageIcon.PackageOpened -= OnPackageIconOpened;
                QuickStartSamplesManager.SampleImportStarted -= OnSampleImportStarted;
                Events.registeredPackages -= OnPackagesRegistered;
                SavePendingPackages();
            });

            return m_Root;
        }

        void ConfigureButtons()
        {
            var essentials = m_Root.Q<Button>("install-essentials");
            var recommended = m_Root.Q<Button>("install-recommended");
            var allButtons = new[] { essentials, recommended };

            ConfigureInstallButton(essentials, allButtons, SelectedGameGenre.CorePackages, InstallEssentials);
            ConfigureInstallButton(recommended, allButtons, SelectedGameGenre.RecommendedPackages, InstallRecommended);
        }

        void ConfigureInstallButton(Button button, Button[] allButtons, List<string> packageList, System.Action<List<string>> clicked)
        {
            bool canInstallAny = false;
            foreach (var package in packageList)
            {
                canInstallAny |= !PackageInfo.IsPackageRegistered(package);
            }

            button.enabledSelf = canInstallAny;
            if (!canInstallAny)
            {
                return;
            }

            button.tooltip = BuildTooltip(packageList);
            var originalText = button.text;

            // override the manipulator entirely
            button.clickable = new Clickable(DisableAfterClick);
            return;

            void CheckState()
            {
                if (m_Request == null || m_Request.IsCompleted)
                {
                    button.text = originalText;
                    ConfigureButtons();
                    return;
                }

                button.schedule.Execute(CheckState)
                    .ExecuteLater((long)TimeSpan.FromSeconds(1).TotalMilliseconds);
            }

            void DisableAfterClick()
            {
                foreach (var b in allButtons)
                    b.enabledSelf = false;
                button.text = L10n.Tr("Installing package(s)...", null);
                clicked?.Invoke(packageList);
                CheckState();
            }
        }

        static string BuildTooltip(List<string> packageList)
        {
            var tooltipBuilder
                = new StringBuilder(L10n.Tr("Will install the latest version of all the following packages:", null))
                    .AppendLine();
            foreach (var package in packageList)
            {
                tooltipBuilder.AppendLine($"\t- {package}");
            }

            return tooltipBuilder.ToString();
        }

        void InstallEssentials(List<string> packages)
        {
            if (m_Request is { IsCompleted: false }) { return; }

            if (EditorUtility.DisplayDialog("Multiplayer Center",
                    "This will install the latest version of all the packages in the Essentials section.",
                    "Install", "Cancel"))
            {
                SendPackageInstalledEvent(packages, RecommendationTier.Essential);
                m_Request = Client.AddAndRemove(packages.ToArray());
            }
        }

        static bool IsInstalled(string packageId) => UnityEditor.PackageManager.PackageInfo.IsPackageRegistered(packageId);

        void RestorePendingPackages()
        {
            var saved = EditorPrefs.GetString(k_PendingPackagesKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
                return;

            foreach (var entry in saved.Split(','))
            {
                var parts = entry.Split(':');
                if (parts.Length == 2
                    && int.TryParse(parts[1], out var tierValue)
                    && !IsInstalled(parts[0]))
                {
                    m_PendingPackages[parts[0]] = (RecommendationTier)tierValue;
                }
            }
            SavePendingPackages();
        }

        void SavePendingPackages()
        {
            if (m_PendingPackages.Count > 0)
            {
                var entries = new List<string>();
                foreach (var entry in m_PendingPackages)
                    entries.Add($"{entry.Key}:{(int)entry.Value}");
                EditorPrefs.SetString(k_PendingPackagesKey, string.Join(",", entries));
            }
            else
            {
                EditorPrefs.DeleteKey(k_PendingPackagesKey);
            }
        }

        void OnPackageIconOpened(string packageId)
        {
            if (IsInstalled(packageId))
                return;

            var tier = SelectedGameGenre.RecommendedPackages.Contains(packageId)
                ? RecommendationTier.Recommended
                : RecommendationTier.Essential;

            m_PendingPackages[packageId] = tier;
        }

        void OnSampleImportStarted(string sampleId)
        {
            if (TryGetCurrentRecommendation(out var recommendation))
                SendPackageInstalledEvent(recommendation.CorePackages, RecommendationTier.Essential);
        }

        void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            var installed = new Dictionary<RecommendationTier, List<string>>();
            foreach (var package in args.added)
            {
                if (!m_PendingPackages.TryGetValue(package.name, out var tier))
                    continue;

                m_PendingPackages.Remove(package.name);
                if (!installed.TryGetValue(tier, out var list))
                    installed[tier] = list = new List<string>();
                list.Add(package.name);
            }

            if (installed.Count == 0)
                return;

            SavePendingPackages();
            foreach (var (tier, packages) in installed)
                PackageInstalledEvent.Send(new PackageInstalledData(
                    SelectedGameGenre.Genre,
                    tier,
                    packages.ToArray()));
        }

        void SendPackageInstalledEvent(List<string> packages, RecommendationTier tier)
        {
            var newPackages = new List<string>();
            foreach (var package in packages)
            {
                if (IsInstalled(package))
                    continue;

                m_PendingPackages.Remove(package);
                newPackages.Add(package);
            }

            if (newPackages.Count == 0)
                return;

            SavePendingPackages();
            PackageInstalledEvent.Send(new PackageInstalledData(
                SelectedGameGenre.Genre,
                tier,
                newPackages.ToArray()));
        }

        void InstallRecommended(List<string> packages)
        {
            if (m_Request is { IsCompleted: false }) { return; }

            if (EditorUtility.DisplayDialog("Multiplayer Center",
                    "This will install the latest version of all the packages in the Recommended section.",
                    "Install", "Cancel"))
            {
                SendPackageInstalledEvent(packages, RecommendationTier.Recommended);
                m_Request = Client.AddAndRemove(packages.ToArray());
            }
        }

        bool TryGetCurrentRecommendation(out PackageRecommendation recommendation)
        {
            var genre = MultiplayerCenterSettings.instance.SelectedGameGenre;
            if (m_GenreToIndexMap.TryGetValue(genre, out var index) && index < m_Recommendations.Descriptions.Count)
            {
                recommendation = m_Recommendations.Descriptions[index];
                return true;
            }
            recommendation = default;
            return false;
        }

        [CreateProperty]
        PackageRecommendation SelectedGameGenre =>
            TryGetCurrentRecommendation(out var recommendation) ? recommendation : default;

        [CreateProperty]
        DisplayStyle ShowEssentials =>
            TryGetCurrentRecommendation(out var recommendation) && recommendation.CorePackages.Count > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        [CreateProperty]
        DisplayStyle ShowRecommended =>
            TryGetCurrentRecommendation(out var recommendation) && recommendation.RecommendedPackages.Count > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

        [CreateProperty]
        DisplayStyle ShowOptional =>
            TryGetCurrentRecommendation(out var recommendation) && recommendation.AdditionalPackages.Count > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;


        public long GetViewHashCode()
        {
            var hash = MultiplayerCenterSettings.instance.SelectedGameGenre.GetHashCode();
            if (m_ViewHashCode == hash) return m_ViewHashCode;

            m_ViewHashCode = hash;
            ConfigureButtons();

            return m_ViewHashCode;
        }
    }
}
