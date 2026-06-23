// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.AssetPackage;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class ActiveTrustWindow
    {
        private const int k_WindowWidthWithTechnicalName = 680;
        private const int k_WindowWidthNoTechnicalName = 580;

        [UsedByNativeCode]
        internal static bool ShouldProceedAfterTrustCheck(string packagePath, IntPtr nativeAssetPackageInfo, int productId, string packageName, string packageVersion, int uploadId, bool isReimport)
        {
            // In batchmode without an Asset Store origin (productId == 0), the import is automated,
            // not a user action. Skip the gate so nothing gets logged. Even Debug.Log would break tests
            // like PackageImportExport.BatchModeExportAndImport, since Log.Expect ignores severity.
            if (productId <= 0 && Application.isBatchMode)
                return true;

            var assetPackageInfo = PackageImport.GetAssetPackageInfo(nativeAssetPackageInfo);

            var applicationProxy = ServicesContainer.instance.Resolve<IApplicationProxy>();
            var origin = new AssetOrigin(productId, packageName, packageVersion, uploadId);
            var viewData = CreateViewData(packagePath, origin, assetPackageInfo, isReimport, applicationProxy.shortUnityVersion);
            if (viewData == null)
                return true;

            if (Application.isBatchMode)
            {
                // Debug.Log (not LogError) — PAK-8763's batchmode requirement is "no popup, proceed".
                // Error-level batchmode/CLI logging is covered by PAK-8788 and will be revisited there.
                Debug.Log(string.Format(
                    L10n.Tr("[Package Import] Package signature check for '{0}': packageStatus={1}, signature={2}. This package was imported as part of batch processing."),
                    packagePath, assetPackageInfo.trustLevel, assetPackageInfo.signature?.status));
                return true;
            }

            return Show(viewData) == ActiveTrustReturnValue.ProceedAnyway;
        }

        internal class RowData
        {
            public string displayName;
            public string signerName;
            public string technicalName;
            public string version;
            public string source;
            public PackageTag versionTag;

            public RowData(PackageInfo info)
            {
                displayName = string.IsNullOrEmpty(info.displayName) ? info.name : info.displayName;
                signerName = string.IsNullOrEmpty(info.author?.name) ? L10n.Tr("Unknown") : info.author.name;
                technicalName = info.name;
                version = info.version;
                source = info.source == PackageSource.Registry && !string.IsNullOrEmpty(info.registry?.name)
                    ? info.registry.name
                    : info.source.GetDisplayName();
                versionTag = SemVersionParser.TryParse(info.version, out var parsed) ? parsed.Value.GetExpOrPreOrReleaseTag() : PackageTag.None;
            }

            public RowData(string packagePath, AssetOrigin origin, AssetPackageInfo assetPackageInfo)
            {
                var hasValidOrigin = origin != null && origin.IsValid();
                displayName = hasValidOrigin && !string.IsNullOrEmpty(origin.packageName)
                    ? origin.packageName
                    : ObjectNames.NicifyVariableName(IOUtils.GetFileNameWithoutExtension(packagePath));

                var attestation = assetPackageInfo.signature?.attestation;
                if (!string.IsNullOrEmpty(attestation?.publisherName))
                    signerName = attestation.publisherName;
                else if (!string.IsNullOrEmpty(attestation?.ownerOrgName))
                    signerName = attestation.ownerOrgName;
                else
                    signerName = L10n.Tr("Unknown");

                version = hasValidOrigin && !string.IsNullOrEmpty(origin.packageVersion)
                    ? origin.packageVersion
                    : L10n.Tr("Unknown");
                source = hasValidOrigin ? L10n.Tr("Asset Store") : L10n.Tr("Unknown");

                technicalName = string.Empty;
                versionTag = PackageTag.None;
            }
        }

        internal class SectionData
        {
            public string headerText;
            public Icon icon;
            public IList<RowData> rows;
            public bool isOrgKnown;
            public bool hasTechnicalName;
        }

        internal class ViewData
        {
            public string windowTitle;
            public string helpBoxText;
            public HelpBoxMessageType helpBoxMessageType;
            public string actionLabel;
            public string docUrl;
            public int windowWidth;
            public IList<SectionData> sections;
        }

        public static ActiveTrustReturnValue Show(ViewData data)
        {
            var container = ServicesContainer.instance;
            var content = new ActiveTrustContent(container.Resolve<IResourceLoader>(), container.Resolve<IApplicationProxy>(), data);
            return ModalWindowContainer.ShowModal(content) ? content.returnValue : ActiveTrustReturnValue.Error;
        }

        public static ViewData CreateViewData(IUpmCache upmCache, IEnumerable<PackageInfo> packages, OperationType operationType, string shortUnityVersion)
        {
            var invalidSignatureRows = new List<RowData>();
            var missingSignatureRows = new List<RowData>();
            var limitedTrustRows = new List<RowData>();
            foreach (var info in packages)
            {
                if (info == null)
                    continue;

                var trustAndSignature = TrustAndSignatureHelper.GetTrustAndSignature(info, true);
                var currentlyInstalled = upmCache.GetInstalledPackageInfo(info.name);
                if (currentlyInstalled?.packageId == info.packageId && TrustAndSignatureHelper.GetTrustAndSignature(currentlyInstalled, true) == trustAndSignature)
                    continue;
                switch (trustAndSignature)
                {
                    case TrustAndSignature.UntrustedInvalidSignature:
                        invalidSignatureRows.Add(new RowData(info));
                        break;
                    case TrustAndSignature.UntrustedNoSignature:
                        missingSignatureRows.Add(new RowData(info));
                        break;
                    case TrustAndSignature.LimitedTrust:
                        limitedTrustRows.Add(new RowData(info));
                        break;
                }
            }

            if (invalidSignatureRows.Count == 0 && missingSignatureRows.Count == 0 && limitedTrustRows.Count == 0)
                return null;

            var sections = new List<SectionData>();
            string windowTitle = null, helpBoxText = null, docUrl = null;
            HelpBoxMessageType messageType = default;
            if (invalidSignatureRows.Count > 0)
            {
                var headerText = invalidSignatureRows.Count == 1
                    ? L10n.Tr("This package has an invalid signature.")
                    : string.Format(L10n.Tr("{0} packages have invalid signatures."), invalidSignatureRows.Count);
                sections.Add(new SectionData
                {
                    headerText = headerText,
                    icon = Icon.PackageErrorLarge,
                    rows = invalidSignatureRows,
                    isOrgKnown = true,
                    hasTechnicalName = true
                });

                windowTitle = L10n.Tr("Invalid signature");
                helpBoxText = L10n.Tr("These packages have an invalid signature which can indicate unsafe or malicious content. Remove these packages to reduce risk to your project.");
                messageType = HelpBoxMessageType.Error;
                docUrl = $"https://docs.unity3d.com/{shortUnityVersion}/Documentation/Manual/upm-errors.html#pkg-invalid-sig";
            }

            if (missingSignatureRows.Count > 0)
            {
                var headerText = missingSignatureRows.Count == 1
                    ? L10n.Tr("This package is missing a signature.")
                    : string.Format(L10n.Tr("{0} packages are missing a signature."), missingSignatureRows.Count);
                sections.Add(new SectionData
                {
                    headerText = headerText,
                    icon = Icon.PackageWarningLarge,
                    rows = missingSignatureRows,
                    isOrgKnown = false,
                    hasTechnicalName = true
                });

                if (windowTitle == null)
                {
                    windowTitle = L10n.Tr("Missing signature");
                    helpBoxText = L10n.Tr("Unity can't verify these packages because they don't have a signature. Use signed packages to reduce risk to your project.");
                    messageType = HelpBoxMessageType.Warning;
                    docUrl = $"https://docs.unity3d.com/{shortUnityVersion}/Documentation/Manual/upm-signature.html";
                }
            }

            if (limitedTrustRows.Count > 0)
            {
                var headerText = limitedTrustRows.Count == 1
                    ? L10n.Tr("This package is signed but not from official Unity sources.")
                    : string.Format(L10n.Tr("{0} packages are signed but not from official Unity sources."), limitedTrustRows.Count);
                sections.Add(new SectionData
                {
                    headerText = headerText,
                    icon = Icon.PackageOptionLarge,
                    rows = limitedTrustRows,
                    isOrgKnown = true,
                    hasTechnicalName = true
                });

                if (windowTitle == null)
                {
                    windowTitle = L10n.Tr("Unofficial Unity source");
                    helpBoxText = L10n.Tr("These packages are signed, but their publishers are not verified by Unity. Please ensure you understand where these packages originated from.");
                    messageType = HelpBoxMessageType.Info;
                    docUrl = $"https://docs.unity3d.com/{shortUnityVersion}/Documentation/Manual/upm-signature.html";
                }
            }

            var actionLabel = operationType == OperationType.Remove ? "Proceed" : operationType.ToString();
            return new ViewData
            {
                windowTitle = windowTitle,
                helpBoxText = helpBoxText,
                helpBoxMessageType = messageType,
                actionLabel = actionLabel,
                docUrl = docUrl,
                windowWidth = k_WindowWidthWithTechnicalName,
                sections = sections
            };
        }

        public static ViewData CreateViewData(string packagePath, AssetOrigin origin, AssetPackageInfo assetPackageInfo, bool isReimport, string shortUnityVersion)
        {
            var trustAndSignature = assetPackageInfo != null ? TrustAndSignatureHelper.GetTrustAndSignature(assetPackageInfo) : TrustAndSignature.NotApplicable;

            string windowTitle, helpBoxText, docUrl, sectionHeader;
            HelpBoxMessageType messageType;
            Icon sectionIcon;
            switch (trustAndSignature)
            {
                case TrustAndSignature.UntrustedInvalidSignature:
                    windowTitle = L10n.Tr("Invalid signature");
                    helpBoxText = L10n.Tr("These assets have an invalid signature which can indicate unsafe or malicious content. Remove this package to reduce risk to your project.");
                    messageType = HelpBoxMessageType.Error;
                    // AssetPackage reuses the UPM doc URLs as a placeholder; kept inline (not deduped into a shared helper)
                    // so the divergence is obvious when AssetPackage gets its own docs.
                    docUrl = $"https://docs.unity3d.com/{shortUnityVersion}/Documentation/Manual/upm-errors.html#pkg-invalid-sig";
                    sectionHeader = L10n.Tr("These assets have an invalid signature.");
                    sectionIcon = Icon.PackageErrorLarge;
                    break;
                case TrustAndSignature.UntrustedNoSignature:
                    windowTitle = L10n.Tr("Missing signature");
                    helpBoxText = L10n.Tr("Unity can't verify these assets because they don't have a signature. Use signed packages to reduce risk to your project.");
                    messageType = HelpBoxMessageType.Warning;
                    docUrl = $"https://docs.unity3d.com/{shortUnityVersion}/Documentation/Manual/upm-signature.html";
                    sectionHeader = L10n.Tr("These assets are missing a signature.");
                    sectionIcon = Icon.PackageWarningLarge;
                    break;
                case TrustAndSignature.LimitedTrust:
                    windowTitle = L10n.Tr("Unofficial Unity source");
                    helpBoxText = L10n.Tr("This package is signed and distributed outside of Unity trusted sources. Please ensure you understand where this package originated from.");
                    messageType = HelpBoxMessageType.Info;
                    docUrl = $"https://docs.unity3d.com/{shortUnityVersion}/Documentation/Manual/upm-signature.html";
                    sectionHeader = L10n.Tr("This package is signed but not from official Unity sources.");
                    sectionIcon = Icon.PackageOptionLarge;
                    break;
                case TrustAndSignature.NotApplicable:
                case TrustAndSignature.FullTrustUnitySignature:
                case TrustAndSignature.FullTrustValidSignature:
                case TrustAndSignature.FullTrustNoSignature:
                case TrustAndSignature.FullTrustBuiltInPackage:
                default:
                    return null;
            }

            var section = new SectionData
            {
                headerText = sectionHeader,
                icon = sectionIcon,
                rows = [new RowData(packagePath, origin, assetPackageInfo)],
                isOrgKnown = origin != null && origin.IsValid(),
                hasTechnicalName = false
            };

            var actionLabel = isReimport ? L10n.Tr("Reimport") : L10n.Tr("Import");
            return new ViewData
            {
                windowTitle = windowTitle,
                helpBoxText = helpBoxText,
                helpBoxMessageType = messageType,
                actionLabel = actionLabel,
                docUrl = docUrl,
                windowWidth = k_WindowWidthNoTechnicalName,
                sections = [section]
            };
        }

        internal class ActiveTrustContent : ModalContent
        {
            public ActiveTrustReturnValue returnValue { get; private set; } = ActiveTrustReturnValue.Cancel;

            internal string docUrl { get; }

            private readonly int m_WindowWidth;

            internal ActiveTrustContent(IResourceLoader resourceLoader, IApplicationProxy application, ViewData data)
            {
                var root = resourceLoader.GetTemplate("ActiveTrustWindow.uxml");
                cache = new VisualElementCache(root);

                styleSheets.Add(resourceLoader.packageManagerCommonStyleSheet);
                styleSheets.Add(resourceLoader.activeTrustWindowStyleSheet);
                Add(root);

                RegisterCallback<GeometryChangedEvent>(OnFirstLayout);

                windowTitle = data.windowTitle;
                helpBoxContainer.text = data.helpBoxText;
                helpBoxContainer.messageType = data.helpBoxMessageType;

                foreach (var section in data.sections)
                    bodyScroll.Add(new TrustWindowSection(resourceLoader, section));

                var actionLabel = data.actionLabel;
                docUrl = data.docUrl;
                m_WindowWidth = data.windowWidth;

                readMoreButton.clicked += () => application.OpenURL(docUrl);

                var cancelButton = new Button { text = L10n.Tr("Cancel") };
                cancelButton.clicked += () =>
                {
                    returnValue = ActiveTrustReturnValue.Cancel;
                    container.Close();
                };
                var proceedAnywayButton = new Button { name = "proceedAnywayButton", text = string.Format(L10n.Tr("{0} Anyway"), actionLabel) };
                proceedAnywayButton.clicked += () =>
                {
                    returnValue = ActiveTrustReturnValue.ProceedAnyway;
                    container.Close();
                };
                buttonsContainer.Add(proceedAnywayButton);
                buttonsContainer.Add(cancelButton);
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
                var dynamicSize = new Vector2(m_WindowWidth, finalHeight);
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
