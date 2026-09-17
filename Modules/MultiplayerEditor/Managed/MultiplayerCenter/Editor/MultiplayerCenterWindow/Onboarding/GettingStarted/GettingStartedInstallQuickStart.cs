// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Adds a HelpBox with a warning at the top of the <see cref="OnboardingSectionCategory.GettingStarted"/>
    /// category to warn users about installing the QuickStart package to get more content in the Multiplayer Center.
    /// </summary>
    [UsedImplicitly]
    class GettingStartedInstallQuickStart : OnboardingGUIProvider<GettingStartedInstallQuickStart>
    {
        Button m_InstallButton;
        AddRequest m_AddRequest;

        public override (OnboardingSectionCategory, int)[] Categories =>
            new[] { (OnboardingSectionCategory.GettingStarted, -1000) };

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            if ( QuickStartSamplesManager.IsQuickStartsInstalled(out var quickStart)
                 && quickStart != null && quickStart.version.StartsWith("2"))
            {
                root.AddToClassList(StyleClasses.Hidden);
                return root;
            }

            var helpBox =
                new HelpBox(
                    L10n.Tr(
                    "It is recommended to install the latest Multiplayer QuickStart package" +
                    " to benefit from basic multiplayer samples available via the Multiplayer Center.", null),
                    HelpBoxMessageType.Warning);

            m_InstallButton = new Button(InstallQuickStart) { text = "Install" };
            helpBox.ElementAt(0).Add(m_InstallButton);
            root.Add(helpBox);
            return root;
        }

        void InstallQuickStart()
        {
            m_InstallButton.enabledSelf = false;
            m_InstallButton.text = L10n.Tr("Installing package...", null);
            m_AddRequest = QuickStartSamplesManager.StartQuickstartsAddRequest();
            m_InstallButton.schedule.Execute(CheckInstallState)
                .ExecuteLater((long)TimeSpan.FromSeconds(1).TotalMilliseconds);
        }

        void CheckInstallState()
        {
            if (m_AddRequest == null || m_AddRequest.IsCompleted)
            {
                m_InstallButton.enabledSelf = true;
                m_InstallButton.text = L10n.Tr("Install", null);
                return;
            }

            m_InstallButton.schedule.Execute(CheckInstallState)
                .ExecuteLater((long)TimeSpan.FromSeconds(1).TotalMilliseconds);
        }
    }
}
