namespace Unity.UI.Builder
{
    internal class BuilderUxmlPreviewWindow : BuilderPaneWindow
    {
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
