// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ProgressBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<ProgressBar> {}

        private IResourceLoader m_ResourceLoader;

        static private double s_LastWidthTime;
        private const double k_PaintInterval = 1f; // Time interval to repaint

        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
        }

        public ProgressBar()
        {
            ResolveDependencies();

            UIUtils.SetElementDisplay(this, false);

            var root = m_ResourceLoader.GetTemplate("ProgressBar.uxml");
            Add(root);
            root.StretchToParentSize();

            cache = new VisualElementCache(root);

            currentProgressBar.style.width = Length.Percent(0);
        }

        public bool UpdateProgress(IOperation operation)
        {
            var showProgressBar = operation != null && operation.isProgressTrackable && operation.isProgressVisible;
            UIUtils.SetElementDisplay(this, showProgressBar);
            if (showProgressBar)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                var deltaTime = currentTime - s_LastWidthTime;
                if (deltaTime >= k_PaintInterval)
                {
                    var percentage = Mathf.Clamp01(operation.progressPercentage);

                    currentProgressBar.style.width = Length.Percent(percentage * 100.0f);
                    currentProgressBar.MarkDirtyRepaint();

                    s_LastWidthTime = currentTime;
                }

                if (operation.isInPause)
                    currentProgressState.text = L10n.Tr("Paused");
                else if(operation.isInProgress)
                    currentProgressState.text = L10n.Tr("Downloading");
                else
                    currentProgressState.text = string.Empty;
            }
            return showProgressBar;
        }

        private VisualElementCache cache { get; }
        private VisualElement currentProgressBar { get { return cache.Get<VisualElement>("progressBarForeground"); } }
        private Label currentProgressState { get { return cache.Get<Label>("currentProgressState"); } }
    }
}
