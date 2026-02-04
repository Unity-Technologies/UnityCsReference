// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageManagerFiltersWindow : EditorWindow
    {
        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal const int k_MaxDisplayLabels = 5;
        protected const int k_FoldOutHeight = 25;
        protected const int k_ToggleHeight = 20;
        protected const int k_Width = 196;
        protected const int k_MaxHeight = 421;

        protected static readonly string k_FoldoutClass = "foldout";
        protected static readonly string k_ToggleClass = "toggle";

        protected static readonly string k_StatusFoldOutName = "statusFoldOut";
        protected static readonly string k_CategoriesFoldOutName = "categoriesFoldOut";
        protected static readonly string k_LabelsFoldOutName = "labelsFoldOut";
        protected static readonly string k_ShowAllButtonName = "showAll";

        private const long k_DelayTicks = TimeSpan.TicksPerSecond / 2;

        private static PackageManagerFiltersWindow s_Window;
        public static PackageManagerFiltersWindow instance => s_Window;

        // The internal modifier is used (instead of private) to give our test project access to these properties/methods
        internal static long s_LastClosedTime;

        public Action<PageFilters> OnFiltersChanged = delegate {};
        public Action OnClose = delegate {};

        protected PageFilters m_Filters = new ();
        protected VisualElement m_Container;
        private long m_ChangeTimestamp;
        private bool m_DelayedUpdate;

        private IResourceLoader m_ResourceLoader;
        protected virtual void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
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
            if (s_Window is null)
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
            m_Filters = page.filters.Clone();
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
            if (s_Window is not null || page == null)
                return false;

            var nowMilliSeconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var justClosed = nowMilliSeconds < s_LastClosedTime + 150;

            if (!justClosed)
            {
                Event.current?.Use();

                if (page.id != MyAssetsPage.k_Id)
                    s_Window = CreateInstance<UpmFiltersWindow>();
                else
                    s_Window = CreateInstance<AssetStoreFiltersWindow>();
                s_Window.Init(rect, page);
            }

            return !justClosed;
        }

        protected static IEnumerable<Toggle> EnumerateSelectedToggle(VisualElement parent)
        {
            if (parent == null)
                yield break;

            foreach (var child in parent.Children())
                if (child is Toggle { value: true } toggle)
                    yield return toggle;
        }

        private VisualElementCache cache { get; set; }
    }
}
