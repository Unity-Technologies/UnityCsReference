// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class ActiveTrustWindow
    {
        public static ActiveTrustReturnValue ShowActiveTrustWindow(IList<PackageInfo> invalidSignaturePackages, IList<PackageInfo> missingSignaturePackages, IList<PackageInfo> limitedTrustPackages, OperationType operationType)
        {
            var container = ServicesContainer.instance;
            var content = new ActiveTrustContent(container.Resolve<IResourceLoader>(), container.Resolve<IApplicationProxy>(),
                invalidSignaturePackages, missingSignaturePackages, limitedTrustPackages, operationType);
            return ModalWindowContainer.ShowModal(content) ? content.returnValue : ActiveTrustReturnValue.Error;
        }

        internal class ActiveTrustContent: ModalContent
        {
            private const int k_WindowWidth = 680;

            public ActiveTrustReturnValue returnValue { get; private set; } = ActiveTrustReturnValue.Cancel;

            public ActiveTrustContent(IResourceLoader resourceLoader, IApplicationProxy application, IList<PackageInfo> invalidSignaturePackages, IList<PackageInfo> missingSignaturePackages, IList<PackageInfo> limitedTrustPackages, OperationType operationType)
            {
                var root = resourceLoader.GetTemplate("ActiveTrustWindow.uxml");
                cache = new VisualElementCache(root);
                styleSheets.Add(resourceLoader.packageManagerCommonStyleSheet);
                styleSheets.Add(resourceLoader.activeTrustWindowStyleSheet);

                var hasInvalidSignature = invalidSignaturePackages?.Count > 0;
                var hasMissingSignature = missingSignaturePackages?.Count > 0;
                var hasLimitedTrust = limitedTrustPackages?.Count > 0;

                InitializeTitleAndHelpBox(hasInvalidSignature, hasMissingSignature, hasLimitedTrust);

                string docUrl = null;
                if (hasInvalidSignature)
                {
                    var message = invalidSignaturePackages.Count == 1
                        ? L10n.Tr("This package has an invalid signature.")
                        : string.Format(L10n.Tr("{0} packages have invalid signatures."), invalidSignaturePackages.Count);
                    var packageStateInvalidSignatures = new ActiveTrustPackageState(resourceLoader, message, Icon.PackageErrorLarge, invalidSignaturePackages);
                    bodyScroll.Add(packageStateInvalidSignatures);
                    docUrl ??= $"https://docs.unity3d.com/{application.shortUnityVersion}/Documentation/Manual/upm-errors.html#pkg-invalid-sig";
                }
                if (hasMissingSignature)
                {
                    var message = missingSignaturePackages.Count == 1
                        ? L10n.Tr("This package is missing a signature.")
                        : string.Format(L10n.Tr("{0} packages are missing a signature."), missingSignaturePackages.Count);
                    var packagesStateMissingSignatures = new ActiveTrustPackageState(resourceLoader, message, Icon.PackageWarningLarge, missingSignaturePackages, false);
                    bodyScroll.Add(packagesStateMissingSignatures);
                    docUrl ??= $"https://docs.unity3d.com/{application.shortUnityVersion}/Documentation/Manual/upm-signature.html";
                }
                if (hasLimitedTrust)
                {
                    var message = limitedTrustPackages.Count == 1
                        ? L10n.Tr("This package is signed but not from official Unity sources.")
                        : string.Format(L10n.Tr("{0} packages are signed but not from official Unity sources."), limitedTrustPackages.Count);
                    var packageStateLimitedTrust = new ActiveTrustPackageState(resourceLoader, message, Icon.PackageOptionLarge, limitedTrustPackages);
                    bodyScroll.Add(packageStateLimitedTrust);
                    docUrl ??= $"https://docs.unity3d.com/{application.shortUnityVersion}/Documentation/Manual/upm-signature.html";
                }
                if (!string.IsNullOrEmpty(docUrl))
                    readMoreButton.clicked += () => application.OpenURL(docUrl);

                var cancelButton = new Button { text = L10n.Tr("Cancel") };
                cancelButton.clicked += () =>
                {
                    returnValue = ActiveTrustReturnValue.Cancel;
                    container.Close();
                };
                var proceedAnywayButton = new Button { name = "proceedAnywayButton", text = string.Format(L10n.Tr("{0} Anyway"), GetActionFromOperationType(operationType)) };
                proceedAnywayButton.clicked += () =>
                {
                    returnValue = ActiveTrustReturnValue.ProceedAnyway;
                    container.Close();
                };
                buttonsContainer.Add(proceedAnywayButton);
                buttonsContainer.Add(cancelButton);
                Add(root);

                RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
            }

            private string GetActionFromOperationType(OperationType operationType)
            {
                return operationType == OperationType.Remove ? "Proceed" : operationType.ToString();
            }

            private void InitializeTitleAndHelpBox(bool hasInvalidSignature, bool hasMissingSignature, bool hasLimitedTrust)
            {
                if (hasInvalidSignature)
                {
                    helpBoxContainer.messageType = HelpBoxMessageType.Error;
                    helpBoxContainer.AddToClassList("error");
                    upperContainer.AddToClassList("error");
                    helpBoxContainer.text = L10n.Tr("These packages have an invalid signature which can indicate unsafe or malicious content. Remove these packages to reduce risk to your project.");
                    windowTitle = L10n.Tr("Invalid signature");
                }
                else if (hasMissingSignature)
                {
                    windowTitle = L10n.Tr("Missing signature");
                    helpBoxContainer.messageType = HelpBoxMessageType.Warning;
                    helpBoxContainer.text = L10n.Tr("Unity can't verify these packages because they don't have a signature. Use signed packages to reduce risk to your project.");
                }
                else if (hasLimitedTrust)
                {
                    windowTitle = L10n.Tr("Unofficial Unity source");
                    helpBoxContainer.messageType = HelpBoxMessageType.Info;
                    helpBoxContainer.AddToClassList("info");
                    helpBoxContainer.text = L10n.Tr("These packages are signed, but their publishers are not verified by Unity. Please ensure you understand where these packages originated from.");
                }
            }

            private void OnFirstLayout(GeometryChangedEvent evt)
            {
                UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

                // There are many issues with the modal window, this seems to ensure that the window is properly sized
                schedule.Execute(CalculateAndSetWindowHeight).StartingIn(1);
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
            private ExtendedHelpBox helpBoxContainer => cache.Get<ExtendedHelpBox>("helpBoxContainer");
            private ScrollView bodyScroll => cache.Get<ScrollView>("bodyScroll");
            private VisualElement lowerContainer => cache.Get<VisualElement>("lowerContainer");
            private Button readMoreButton => cache.Get<Button>("readMoreButton");
            private VisualElement buttonsContainer => cache.Get<VisualElement>("buttonsContainer");
        }
    }
}
