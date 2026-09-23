// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Routes build selections between the window and the drawers contributed by external pipelines.
    /// </summary>
    internal sealed class ExternalViewRouter
    {
        internal const string k_MissingDrawerName = "external-missing-drawer";
        internal const string k_MissingDrawerDescriptionName = "external-missing-drawer__description";
        internal const string k_MissingDrawerActionName = "external-missing-drawer__action";

        private readonly VisualElement m_ContentArea;
        private readonly VisualElement m_BuiltInContent;
        private readonly Dictionary<string, ExternalDrawerView> m_Views = new Dictionary<string, ExternalDrawerView>();

        private ExternalDrawerView m_Claimed;

        private VisualElement m_MissingDrawerPane;

        public ExternalViewRouter(VisualElement contentArea, VisualElement builtInContent)
        {
            m_ContentArea = contentArea ?? throw new ArgumentNullException(nameof(contentArea));
            m_BuiltInContent = builtInContent ?? throw new ArgumentNullException(nameof(builtInContent));
        }

        internal int DrawerCount => m_Views.Count;

        internal ExternalDrawerView FindView(string producerPackage)
        {
            m_Views.TryGetValue(producerPackage, out var view);
            return view;
        }

        /// <summary>Takes the drawers found in the project. Their content is built on first use.</summary>
        public void Enable()
        {
            Enable(ExternalBuildDrawers.Create());
        }

        // Takes the drawers rather than finding them, so tests can supply their own.
        internal void Enable(IReadOnlyDictionary<string, ExternalBuildDrawer> drawers)
        {
            foreach (var drawer in drawers)
                m_Views.Add(drawer.Key, new ExternalDrawerView(drawer.Key, drawer.Value));
        }

        /// <summary>
        /// Gives the content area to the drawer declaring the build's producer package, and returns whether one
        /// did. False leaves any showing drawer in place, so the caller releases before drawing its own content.
        /// </summary>
        public bool TryClaim(BuildReportSummary summary)
        {
            if (string.IsNullOrEmpty(summary.ProducerPackage))
                return false;

            if (!m_Views.TryGetValue(summary.ProducerPackage, out var claimed))
                return false;

            RemoveMissingDrawerPane();

            if (!ReferenceEquals(m_Claimed, claimed))
            {
                m_Claimed?.ClearSelection();
                m_Claimed?.Root.RemoveFromHierarchy();

                m_BuiltInContent.style.display = DisplayStyle.None;
                m_ContentArea.Add(claimed.Root);
                m_Claimed = claimed;
            }

            claimed.SetSelection(summary);
            return true;
        }

        /// <summary>
        /// Takes the content area for a build nothing can draw and says so, rather than leaving the tabs empty.
        /// </summary>
        public void ShowMissingDrawer(BuildReportSummary summary)
        {
            Release();

            m_MissingDrawerPane = CreateMissingDrawerPane(summary);

            m_BuiltInContent.style.display = DisplayStyle.None;
            m_ContentArea.Add(m_MissingDrawerPane);
        }

        /// <summary>Takes the content area back and restores the window's own tabs.</summary>
        public void Release()
        {
            RemoveMissingDrawerPane();

            if (m_Claimed != null)
            {
                m_Claimed.ClearSelection();
                m_Claimed.Root.RemoveFromHierarchy();
                m_Claimed = null;
            }

            m_BuiltInContent.style.display = StyleKeyword.Null;
        }

        private void RemoveMissingDrawerPane()
        {
            m_MissingDrawerPane?.RemoveFromHierarchy();
            m_MissingDrawerPane = null;
        }

        private static VisualElement CreateMissingDrawerPane(BuildReportSummary summary)
        {
            var pane = new VisualElement { name = k_MissingDrawerName };
            pane.AddToClassList(k_MissingDrawerName);

            var description = new Label(MissingDrawerMessage(summary)) { name = k_MissingDrawerDescriptionName };
            description.AddToClassList(k_MissingDrawerDescriptionName);
            pane.Add(description);

            var action = CreateMissingDrawerAction(summary.ProducerPackage);
            action.name = k_MissingDrawerActionName;
            action.AddToClassList(k_MissingDrawerActionName);
            pane.Add(action);

            return pane;
        }

        // An empty state carries a single action, so the package is offered when there is one to install
        // and the documentation otherwise.
        private static Button CreateMissingDrawerAction(string producerPackage)
        {
            if (string.IsNullOrEmpty(producerPackage))
            {
                return new Button(() => Help.BrowseURL(BuildAnalysisDocumentation.WindowReferenceUrl))
                {
                    text = "Read more"
                };
            }

            return new Button(() => PackageManager.UI.Window.Open(producerPackage))
            {
                text = "View in Package Manager"
            };
        }

        internal static string MissingDrawerMessage(BuildReportSummary summary)
        {
            return MissingDrawerMessage(summary, IsProducerPackageInstalled(summary.ProducerPackage));
        }

        // Takes the package state, so tests do not depend on what the running project has installed.
        internal static string MissingDrawerMessage(BuildReportSummary summary, bool producerPackageInstalled)
        {
            var producer = string.IsNullOrEmpty(summary.BuildTypeName) ? summary.BuildName : summary.BuildTypeName;

            var madeBy = string.IsNullOrEmpty(producer)
                ? "This build was made outside the Unity build pipeline."
                : $"This build was made by {producer}.";

            return $"{madeBy} {MissingReportReason(summary.ProducerPackage, producerPackageInstalled)}";
        }

        private static string MissingReportReason(string producerPackage, bool producerPackageInstalled)
        {
            // A build naming no package is one of ours, so its report is missing rather than undrawable.
            if (string.IsNullOrEmpty(producerPackage))
                return "Its report is unavailable. The build may have been interrupted, or its report deleted.";

            if (!producerPackageInstalled)
                return $"To view its report, install the {producerPackage} package.";

            return $"The installed {producerPackage} package cannot display its report. " +
                "Update the package, or check the Console for errors loading it.";
        }

        private static bool IsProducerPackageInstalled(string producerPackage)
        {
            return !string.IsNullOrEmpty(producerPackage)
                && PackageManager.PackageInfo.FindForPackageName(producerPackage) != null;
        }
    }
}
