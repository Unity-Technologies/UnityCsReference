// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
namespace Unity.UI.Builder
{
    internal class BuilderUssPreviewWindow : BuilderPaneWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal BuilderUssPreviewWindow() {}
        #pragma warning restore UAL0015

        BuilderUssPreview m_UssPreview;

        //[MenuItem("BuilderConstants.BuilderMenuEntry + " USS Preview")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderUssPreviewWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent("UI Builder USS Preview");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            var selection = viewportWindow.selection;

            m_UssPreview = new BuilderUssPreview(this, selection);

            selection.AddNotifier(m_UssPreview);

            root.Add(m_UssPreview);
        }

        public override void ClearUI()
        {
            if (m_UssPreview == null)
                return;

            var selection = document.primaryViewportWindow?.selection;
            if (selection == null)
                return;

            selection.RemoveNotifier(m_UssPreview);

            m_UssPreview.RemoveFromHierarchy();
            m_UssPreview = null;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
