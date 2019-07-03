// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class ProgressBar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<ProgressBar> {}

        public ProgressBar()
        {
            UIUtils.SetElementDisplay(this, false);

            var root = Resources.GetTemplate("ProgressBar.uxml");
            Add(root);
            root.StretchToParentSize();

            cache = new VisualElementCache(root);

            currentProgressText.text = string.Empty;
            currentProgressBar.style.width = Length.Percent(0);
        }

        public void Show()
        {
            UIUtils.SetElementDisplay(this, true);
        }

        public void Hide()
        {
            UIUtils.SetElementDisplay(this, false);
        }

        public void SetProgress(float percentage)
        {
            percentage = Mathf.Clamp01(percentage);

            currentProgressText.text = percentage.ToString("P1", CultureInfo.InvariantCulture);
            currentProgressBar.style.width = Length.Percent(percentage * 100.0f);
            currentProgressBar.MarkDirtyRepaint();

            Show();
        }

        private VisualElementCache cache { get; }
        private Label currentProgressBar { get { return cache.Get<Label>("currentProgressBar"); } }
        private Label currentProgressText { get { return cache.Get<Label>("currentProgressText"); } }
    }
}
