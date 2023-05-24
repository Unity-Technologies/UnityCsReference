// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageLinkButton : Button
    {
        protected ApplicationProxy m_Application;
        protected PackageLink m_Link;

        public const string k_LinkClass = "link";

        public PackageLinkButton(ApplicationProxy application, PackageLink link)
        {
            m_Link = link;
            m_Application = application;
            AddToClassList(k_LinkClass);

            Initialize();
        }

        public virtual void OpenInBrowser()
        {
            if (string.IsNullOrEmpty(m_Link.url))
                return;

            // We have 2 links, one that includes the shortUnityVersion which wouldn't work on a non released version of unity
            // (it would show a error page for our internal developers)
            // In that case we want to remove that shortUnityVersion from the url so the latest released version is shown instead
            var urlContainsUnityShortVersion = m_Link.url.StartsWith(m_Application.docsUrlWithShortUnityVersion, StringComparison.InvariantCultureIgnoreCase);
            if (urlContainsUnityShortVersion && m_Application.isInternetReachable)
            {
                m_Application.CheckUrlValidity(m_Link.url,
                    () => OpenUrlWithAnalytics(m_Link.url, $"{m_Link.analyticsEventName}ValidUrl"),
                    () =>
                    {
                        var fallbackUrl = m_Link.url.Replace(m_Application.docsUrlWithShortUnityVersion, ApplicationProxy.k_UnityDocsUrl);
                        m_Application.CheckUrlValidity(fallbackUrl,
                            () => OpenUrlWithAnalytics(fallbackUrl, $"{m_Link.analyticsEventName}ValidFallbackUrl"),
                            () => OpenUrlWithAnalytics(m_Link.url, $"{m_Link.analyticsEventName}InvalidUrl"));
                    });
            }
            else
                OpenUrlWithAnalytics(m_Link.url, $"{m_Link.analyticsEventName}NoCheck");
        }

        public virtual void OpenLocally()
        {
            m_Application.RevealInFinder(m_Link.offlinePath);
            PackageManagerWindowAnalytics.SendEvent($"{m_Link.analyticsEventName}OnDisk", m_Link.version?.uniqueId);
        }

        private void OpenUrlWithAnalytics(string url, string analyticEventName)
        {
            m_Application.OpenURL(url);
            PackageManagerWindowAnalytics.SendEvent(analyticEventName, m_Link.version?.uniqueId);
        }

        public IEnumerable<DropdownMenuAction> rightClickMenuActions
        {
            get
            {
                foreach (var action in m_Link.contextMenuActions)
                {
                    switch (action)
                    {
                        case PackageLink.ContextMenuAction.OpenInBrowser:
                            yield return new DropdownMenuAction(L10n.Tr("Open in Browser"),
                                a => OpenInBrowser(),
                                a => string.IsNullOrEmpty(m_Link.url) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                            break;
                        case PackageLink.ContextMenuAction.OpenLocally:
                            yield return new DropdownMenuAction(L10n.Tr("Open Locally"),
                                a => OpenLocally(),
                                a => string.IsNullOrEmpty(m_Link.offlinePath) ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                            break;
                    }
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            foreach (var action in rightClickMenuActions)
                evt.menu.MenuItems().Add(action);
        }

        private void Initialize()
        {
            text = m_Link.displayName;
            tooltip = m_Link.tooltip;
            SetEnabled(m_Link.isEnabled);

            clickable.clickedWithEventInfo += (e) =>
            {
                if (e is IPointerEvent pointerEvent && pointerEvent.button == 0)
                    OnLeftClick();
            };
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
        }

        private void OnLeftClick()
        {
            if (!string.IsNullOrEmpty(m_Link.url))
                OpenInBrowser();
            else if (!string.IsNullOrEmpty(m_Link.offlinePath))
                OpenLocally();
            else
            {
                if (!string.IsNullOrEmpty(m_Link.analyticsEventName))
                    PackageManagerWindowAnalytics.SendEvent($"{m_Link.analyticsEventName}NotFound", m_Link.version?.uniqueId);

                Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Unable to find valid {0} for this {1}."), m_Link.displayName.ToLower(), m_Link.version.GetDescriptor()));
            }
        }
    }
}
