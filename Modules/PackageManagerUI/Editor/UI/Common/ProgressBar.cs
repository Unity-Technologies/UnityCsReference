// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class ProgressBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<ProgressBar> {}

        private ResourceLoader m_ResourceLoader;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
        }

        public ProgressBar()
        {
            ResolveDependencies();

            UIUtils.SetElementDisplay(this, false);

            var root = m_ResourceLoader.GetTemplate("ProgressBar.uxml");
            Add(root);
            root.StretchToParentSize();

            cache = new VisualElementCache(root);

            currentProgressText.text = string.Empty;
            currentProgressBar.style.width = Length.Percent(0);
        }

        public void UpdateProgress(IOperation operation)
        {
            var showProgressBar = operation != null && operation.isProgressTrackable && operation.isProgressVisible;
            UIUtils.SetElementDisplay(this, showProgressBar);
            if (showProgressBar)
            {
                var percentage = Mathf.Clamp01(operation.progressPercentage);

                currentProgressText.text = percentage.ToString("P1", CultureInfo.InvariantCulture);
                currentProgressBar.style.width = Length.Percent(percentage * 100.0f);
                currentProgressBar.MarkDirtyRepaint();
            }
        }

        private VisualElementCache cache { get; }
        private Label currentProgressBar { get { return cache.Get<Label>("currentProgressBar"); } }
        private Label currentProgressText { get { return cache.Get<Label>("currentProgressText"); } }
    }
}
