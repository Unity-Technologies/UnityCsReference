// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
namespace Unity.UI.Builder
{
    internal class BuilderUxmlPreviewWindow : BuilderPaneWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal BuilderUxmlPreviewWindow() {}
        #pragma warning restore UAL0015

        BuilderUxmlPreview m_UxmlPreview;

        //[MenuItem(BuilderConstants.BuilderMenuEntry + " UXML Preview")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderUxmlPreviewWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent("UI Builder UXML Preview");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            var selection = viewportWindow.selection;

            m_UxmlPreview = new BuilderUxmlPreview(this);

            selection.AddNotifier(m_UxmlPreview);

            root.Add(m_UxmlPreview);
        }

        public override void ClearUI()
        {
            if (m_UxmlPreview == null)
                return;

            var selection = document.primaryViewportWindow?.selection;
            if (selection == null)
                return;

            selection.RemoveNotifier(m_UxmlPreview);

            m_UxmlPreview.RemoveFromHierarchy();
            m_UxmlPreview = null;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
