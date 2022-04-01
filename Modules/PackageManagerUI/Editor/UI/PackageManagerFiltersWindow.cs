// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageManagerFiltersWindow : EditorWindow
    {
        internal const int k_MaxDisplayLabels = 5;
        internal const int k_FoldOutHeight = 25;
        internal const int k_ToggleHeight = 20;
        internal const int k_Width = 196;
        internal const int k_MaxHeight = 421;

        internal static readonly string k_FoldoutClass = "foldout";
        internal static readonly string k_ToggleClass = "toggle";

        internal static readonly string k_StatusFoldOutName = "statusFoldOut";
        internal static readonly string k_CategoriesFoldOutName = "categoriesFoldOut";
        internal static readonly string k_LabelsFoldOutName = "labelsFoldOut";
        internal static readonly string k_ShowAllButtonName = "showAll";

        private const long k_DelayTicks = TimeSpan.TicksPerSecond / 2;

        private static PackageManagerFiltersWindow s_Window;
        public static PackageManagerFiltersWindow instance => s_Window;

        internal static long s_LastClosedTime;

        public Action<PageFilters> OnFiltersChanged = delegate {};
        public Action OnClose = delegate {};

        protected PageFilters m_Filters = new PageFilters();
        protected VisualElement m_Container;
        private long m_ChangeTimestamp;
        private bool m_DelayedUpdate;

        private ResourceLoader m_ResourceLoader;
        protected virtual void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
        }

        protected virtual void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            this.SetAntiAliasing(4);

            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageManagerFiltersWindow.uxml");
            root.styleSheets.Add(m_ResourceLoader.filtersDropdownStyleSheet);
            cache = new VisualElementCache(root);

            rootVisualElement.Add(root);
            root.StretchToParentSize();

            m_Container = cache.Get<VisualElement>("mainContainer");
        }

        private void OnLostFocus()
        {
            Close();
        }

        private void OnDisable()
        {
            if (s_Window == null)
                return;

            if (m_DelayedUpdate)
            {
                EditorApplication.update -= DelayedUpdatePageFilters;
                OnFiltersChanged?.Invoke(m_Filters);
            }

            s_LastClosedTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            s_Window = null;
            OnClose?.Invoke();
        }

        protected void UpdatePageFilters()
        {
            m_DelayedUpdate = true;
            m_ChangeTimestamp = DateTime.Now.Ticks;
            EditorApplication.update -= DelayedUpdatePageFilters;
            EditorApplication.update += DelayedUpdatePageFilters;
        }

        private void DelayedUpdatePageFilters()
        {
            if (DateTime.Now.Ticks - m_ChangeTimestamp > k_DelayTicks)
            {
                EditorApplication.update -= DelayedUpdatePageFilters;
                OnFiltersChanged?.Invoke(m_Filters);
            }
        }

        protected virtual void Init(Rect rect, IPage page)
        {
            m_Filters = page.filters?.Clone() ?? new PageFilters();
            m_Container.Clear();
            DoDisplay(page);
            ApplyFilters();
            ShowAsDropDown(rect, GetSize(page), new[] { PopupLocation.Below });
        }

        protected abstract Vector2 GetSize(IPage page);
        protected abstract void ApplyFilters();
        protected abstract void DoDisplay(IPage page);

        public static bool ShowAtPosition(Rect rect, IPage page)
        {
            if (s_Window != null || page == null)
                return false;

            var nowMilliSeconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var justClosed = nowMilliSeconds < s_LastClosedTime + 150;

            if (!justClosed)
            {
                Event.current?.Use();

                if (page.tab != PackageFilterTab.AssetStore)
                    s_Window = CreateInstance<UpmFiltersWindow>();
                else
                    s_Window = CreateInstance<AssetStoreFiltersWindow>();
                s_Window.Init(rect, page);
            }

            return !justClosed;
        }

        private VisualElementCache cache { get; set; }
    }
}
