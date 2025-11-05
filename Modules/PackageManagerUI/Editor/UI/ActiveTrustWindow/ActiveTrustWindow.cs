// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class ActiveTrustWindow
    {
        public static ActiveTrustReturnValue ShowActiveTrustWindow(PackageInfo[] invalidSignaturePackages, PackageInfo[] missingSignaturePackages, PackageInfo[] limitedTrustPackages)
        {
            var content = new ActiveTrustContent(invalidSignaturePackages, missingSignaturePackages, limitedTrustPackages);
            return ModalWindowContainer.ShowModal(content) ? content.returnValue : ActiveTrustReturnValue.Error;
        }

        private class ActiveTrustContent: ModalContent
        {
            private const int k_WindowWidth = 680;

            public ActiveTrustReturnValue returnValue { get; private set; } = ActiveTrustReturnValue.Cancel;

            private Button m_CancelButton;
            private Button m_InstallAnywayButton;

            private readonly IResourceLoader m_ResourceLoader;
            private readonly IApplicationProxy m_Application;
            private readonly IUpmClient m_UpmClient;
            public ActiveTrustContent(PackageInfo[] invalidSignaturePackages, PackageInfo[] missingSignaturePackages, PackageInfo[] limitedTrustPackages)
            {
                m_ResourceLoader = ServicesContainer.instance.Resolve<IResourceLoader>();
                m_Application = ServicesContainer.instance.Resolve<IApplicationProxy>();
                m_UpmClient = ServicesContainer.instance.Resolve<IUpmClient>();
                Init(invalidSignaturePackages, missingSignaturePackages, limitedTrustPackages);
            }

            private void Init(PackageInfo[] invalidSignaturePackages, PackageInfo[] missingSignaturePackages, PackageInfo[] limitedTrustPackages)
            {
                var root = m_ResourceLoader.GetTemplate("ActiveTrustWindow.uxml");
                cache = new VisualElementCache(root);
                styleSheets.Add(m_ResourceLoader.packageManagerCommonStyleSheet);
                styleSheets.Add(m_ResourceLoader.activeTrustWindowStyleSheet);
                UIUtils.SetElementDisplay(upperContainer, invalidSignaturePackages.Length > 0 || missingSignaturePackages.Length > 1);
                if (invalidSignaturePackages.Length > 0)
                {
                    helpBoxIcon.AddToClassList(Icon.Error.ClassName());
                    helpBoxLabel.AddToClassList("error");
                    helpBoxLabel.text = L10n.Tr("Installing is not recommended. Review the packages carefully, or choose Cancel to abort the installation.");
                }else if (missingSignaturePackages.Length > 1)
                {
                    helpBoxIcon.AddToClassList(Icon.Warning.ClassName());
                    helpBoxContainer.AddToClassList("warning");
                    upperContainer.AddToClassList("warning");
                    helpBoxLabel.text = L10n.Tr("Some of the packages are unsigned. A signature verifies the packages source and integrity. You can install them now, and ask the publisher to sign the packages in the future.");
                }

                string docUrl = null;
                if (invalidSignaturePackages.Length > 0)
                {
                    windowTitle = L10n.Tr("Invalid signature");
                    var message = invalidSignaturePackages.Length == 1
                        ? L10n.Tr("This package has an invalid signature")
                        : string.Format(L10n.Tr("{0} packages have invalid signatures"), invalidSignaturePackages.Length);
                    var packageStateInvalidSignatures = new ActiveTrustPackageState(m_ResourceLoader, message, Icon.PackageErrorLarge, invalidSignaturePackages);
                    bodyScroll.Add(packageStateInvalidSignatures);
                    docUrl ??= $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-errors.html#pkg-invalid-sig";
                }
                if (missingSignaturePackages.Length > 0)
                {
                    if (invalidSignaturePackages.Length == 0)
                        windowTitle = L10n.Tr("Missing signature");

                    var message = missingSignaturePackages.Length == 1
                        ? L10n.Tr("This package is missing a signature")
                        : string.Format(L10n.Tr("{0} packages are missing a signature"), missingSignaturePackages.Length);
                    var packagesStateMissingSignatures = new ActiveTrustPackageState(m_ResourceLoader, message, Icon.PackageWarningLarge, missingSignaturePackages, false);
                    bodyScroll.Add(packagesStateMissingSignatures);
                    docUrl ??= $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-signature.html";
                }
                if (limitedTrustPackages.Length > 0)
                {
                    if (invalidSignaturePackages.Length == 0 && missingSignaturePackages.Length == 0)
                        windowTitle = L10n.Tr("Install Package");

                    windowTitle = L10n.Tr("Install Package");
                    var message = limitedTrustPackages.Length == 1
                        ? L10n.Tr("This package is signed but not from official Unity sources")
                        : string.Format(L10n.Tr("{0} packages are signed but not from official Unity sources"), limitedTrustPackages.Length);
                    var packageStateLimitedTrust = new ActiveTrustPackageState(m_ResourceLoader, message, Icon.PackageOptionLarge, limitedTrustPackages);
                    bodyScroll.Add(packageStateLimitedTrust);
                    docUrl ??= $"https://docs.unity3d.com/{m_Application.shortUnityVersion}/Documentation/Manual/upm-signature.html";
                }
                if (!string.IsNullOrEmpty(docUrl))
                {
                    readMoreButton.clicked += () => m_Application.OpenURL(docUrl);
                }

                m_CancelButton = new Button { text = L10n.Tr("Cancel") };
                m_CancelButton.clicked += () =>
                {
                    returnValue = ActiveTrustReturnValue.Cancel;
                    container.Close();
                    m_UpmClient.Resolve();
                };
                m_InstallAnywayButton = new Button { text = L10n.Tr("Install Anyway") };
                m_InstallAnywayButton.clicked += () =>
                {
                    returnValue = ActiveTrustReturnValue.InstallAnyway;
                    container.Close();
                };
                buttonsContainer.Add(m_InstallAnywayButton);
                buttonsContainer.Add(m_CancelButton);
                Add(root);

                RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
            }

            private void OnFirstLayout(GeometryChangedEvent evt)
            {
                UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

                // There are many issues with the modal window, this seems to ensure that the window is properly sized
                schedule.Execute(() => CalculateAndSetWindowHeight()).StartingIn(1);
            }

            private void CalculateAndSetWindowHeight()
            {
                const float verticalPadding = 10f;
                float totalHeight = 0;

                if (upperContainer.style.display != DisplayStyle.None)
                    totalHeight += upperContainer.worldBound.height;

                var bodyScrollContentContainer = bodyScroll.contentContainer;
                totalHeight += Mathf.Min(bodyScrollContentContainer.worldBound.height, 400f);

                totalHeight += lowerContainer.worldBound.height;

                var finalHeight = totalHeight + verticalPadding;
                var dynamicSize = new Vector2(k_WindowWidth, finalHeight);
                container.minSize = dynamicSize;
                container.maxSize = dynamicSize;
            }

            public override void OnBeforeShowModal() { }
            public override void OnModalClosed() { }

            private VisualElementCache cache { get; set; }
            private VisualElement upperContainer => cache.Get<VisualElement>("upperContainer");
            private VisualElement helpBoxContainer => cache.Get<VisualElement>("helpBoxContainer");
            private VisualElement helpBoxIcon => cache.Get<VisualElement>("helpBoxIcon");
            private Label helpBoxLabel => cache.Get<Label>("helpBoxLabel");
            private VisualElement bodyContainer => cache.Get<VisualElement>("bodyContainer");
            private ScrollView bodyScroll => cache.Get<ScrollView>("bodyScroll");
            private VisualElement lowerContainer => cache.Get<VisualElement>("lowerContainer");
            private Button readMoreButton => cache.Get<Button>("readMoreButton");
            private VisualElement buttonsContainer => cache.Get<VisualElement>("buttonsContainer");
        }
    }
}
