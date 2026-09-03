// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetsWindow : BuilderPaneWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal BuilderStyleSheetsWindow() {}
        #pragma warning restore UAL0015

        BuilderStyleSheets m_StyleSheetsPane;

        //[MenuItem(BuilderConstants.BuilderMenuEntry + " StyleSheets")]
        public static void ShowWindow()
        {
            GetWindowAndInit<BuilderStyleSheetsWindow>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetTitleContent("UI Builder StyleSheets");
        }

        public override void CreateUI()
        {
            var root = rootVisualElement;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                return;

            var selection = viewportWindow.selection;
            var viewport = viewportWindow.viewport;

            var classDragger = new BuilderClassDragger(this, root, selection, viewport, viewport.parentTracker);
            var styleSheetsDragger = new BuilderStyleSheetsDragger(this, root, selection);

            m_StyleSheetsPane = new BuilderStyleSheets(this, viewport, selection, classDragger, styleSheetsDragger, null, null);

            selection.AddNotifier(m_StyleSheetsPane);

            root.Add(m_StyleSheetsPane);

            // Command Handler
            commandHandler.RegisterPane(m_StyleSheetsPane);
        }

        public override void ClearUI()
        {
            if (m_StyleSheetsPane == null)
                return;

            var selection = document.primaryViewportWindow?.selection;
            if (selection != null)
                selection.RemoveNotifier(m_StyleSheetsPane);

            m_StyleSheetsPane.RemoveFromHierarchy();
            m_StyleSheetsPane = null;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
