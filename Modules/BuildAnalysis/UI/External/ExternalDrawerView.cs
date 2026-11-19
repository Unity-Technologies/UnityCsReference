// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Hosts the content of one <see cref="ExternalBuildDrawer"/>, created lazily on the first build it claims.
    /// </summary>
    internal sealed class ExternalDrawerView
    {
        internal const string k_UnavailableName = "external-view-unavailable";
        internal const string k_UnavailableMessage = "This report could not be displayed. See the Console for details.";

        private readonly ExternalBuildDrawer m_Drawer;
        private readonly string m_ProducerPackage;
        private readonly VisualElement m_Root = new VisualElement();

        private VisualElement m_Content;

        private bool m_Broken;

        private bool m_Selected;

        public ExternalDrawerView(string producerPackage, ExternalBuildDrawer drawer)
        {
            m_Drawer = drawer ?? throw new ArgumentNullException(nameof(drawer));
            m_ProducerPackage = producerPackage ?? throw new ArgumentNullException(nameof(producerPackage));

            m_Root.name = "external-view-" + producerPackage;
            m_Root.style.flexGrow = 1;
        }

        public ExternalBuildDrawer Drawer => m_Drawer;

        public string ProducerPackage => m_ProducerPackage;

        public VisualElement Root => m_Root;

        internal VisualElement Content => m_Content;

        internal bool IsBroken => m_Broken;

        /// <summary>Takes a claimed build, creating the content the first time.</summary>
        public void Claim()
        {
            if (m_Broken)
                return;

            if (m_Content == null && !TryCreateContent())
                return;

            m_Selected = true;
        }

        /// <summary>Starts loading a claimed build. A synchronous throw becomes a faulted task.</summary>
        public Task<object> LoadAsync(BuildReportSummary summary, CancellationToken token)
        {
            if (m_Broken)
                return Task.FromResult<object>(null);

            try
            {
                return m_Drawer.LoadBuildAsync(summary, token) ?? Task.FromResult<object>(null);
            }
            catch (Exception e)
            {
                return Task.FromException<object>(e);
            }
        }

        /// <summary>Shows a loaded build.</summary>
        /// <returns>False if the drawer showed no report. A broken drawer has nothing to retry.</returns>
        public bool Apply(BuildReportSummary summary, object loaded)
        {
            return m_Broken || m_Drawer.ApplyBuild(summary, loaded);
        }

        /// <summary>Tells the drawer the selection moved to a build it does not claim.</summary>
        public void ClearSelection()
        {
            if (!m_Selected)
                return;

            m_Selected = false;

            try
            {
                m_Drawer.OnBuildDeselected();
            }
            catch (Exception e)
            {
                DrawErrorHelpBox($"{nameof(ExternalBuildDrawer.OnBuildDeselected)} failed: {e.Message}");
            }
        }

        /// <summary>Tells the drawer the build history changed.</summary>
        public void RefreshSelection()
        {
            if (!m_Selected || m_Broken)
                return;

            try
            {
                m_Drawer.OnBuildListRefreshed();
            }
            catch (Exception e)
            {
                DrawErrorHelpBox($"{nameof(ExternalBuildDrawer.OnBuildListRefreshed)} failed: {e.Message}");
            }
        }

        private bool TryCreateContent()
        {
            try
            {
                m_Content = m_Drawer.CreateContent();
            }
            catch (Exception e)
            {
                DrawErrorHelpBox($"{nameof(ExternalBuildDrawer.CreateContent)} failed: {e.Message}");
                return false;
            }

            if (m_Content == null)
            {
                DrawErrorHelpBox($"{nameof(ExternalBuildDrawer.CreateContent)} returned no content");
                return false;
            }

            m_Content.style.flexGrow = 1;
            m_Root.Add(m_Content);
            return true;
        }

        private void DrawErrorHelpBox(string errorMessage)
        {
            m_Broken = true;
            m_Content?.RemoveFromHierarchy();
            m_Content = null;

            m_Root.Q(k_UnavailableName)?.RemoveFromHierarchy();

            var message = new HelpBox(k_UnavailableMessage, HelpBoxMessageType.Error) { name = k_UnavailableName };
            message.style.marginLeft = 8;
            message.style.marginRight = 8;
            message.style.marginTop = 8;
            m_Root.Add(message);

            Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} External build drawer '{m_ProducerPackage}' {errorMessage}");
        }
    }
}
