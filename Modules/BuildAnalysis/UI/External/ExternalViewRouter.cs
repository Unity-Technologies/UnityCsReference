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
        private readonly VisualElement m_ContentArea;
        private readonly VisualElement m_BuiltInContent;
        private readonly Dictionary<string, ExternalDrawerView> m_Views = new Dictionary<string, ExternalDrawerView>();

        private ExternalDrawerView m_Claimed;

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

        /// <summary>Takes the content area back and restores the window's own tabs.</summary>
        public void Release()
        {
            if (m_Claimed == null)
                return;

            m_Claimed.ClearSelection();
            m_Claimed.Root.RemoveFromHierarchy();
            m_Claimed = null;

            m_BuiltInContent.style.display = StyleKeyword.Null;
        }
    }
}
