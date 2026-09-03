// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
namespace Unity.UI.Builder
{
    class BuilderLibraryWindow : BuilderPaneWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        BuilderLibraryWindow() {}
        #pragma warning restore UAL0015

        BuilderLibrary m_Library;

        //[MenuItem(BuilderConstants.BuilderMenuEntry + " Library")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderLibraryWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent("UI Builder Library");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            var selection = viewportWindow.selection;
            var viewport = viewportWindow.viewport;

            m_Library = new BuilderLibrary(this, viewport, selection, null, null);

            root.Add(m_Library);
        }

        public override void ClearUI()
        {
            if (m_Library == null)
                return;

            m_Library.RemoveFromHierarchy();
            m_Library = null;
        }

        public override void OnEnableAfterAllSerialization()
        {
            // Perform post-serialization functions.
            m_Library.OnAfterBuilderDeserialize();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
