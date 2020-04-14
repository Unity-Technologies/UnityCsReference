// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal abstract class PackageManagerFiltersWindow : EditorWindow
    {
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
        private void ResolveDependencies()
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
            root.styleSheets.Add(m_ResourceLoader.GetFiltersWindowStyleSheet());
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

        protected virtual void Init(Rect rect, PageFilters filters)
        {
            m_Filters = filters?.Clone() ?? new PageFilters();
            m_Container.Clear();
            DoDisplay();
            ApplyFilters();
            ShowAsDropDown(rect, GetSize(), new[] { PopupLocation.Below });
        }

        protected abstract Vector2 GetSize();
        protected abstract void ApplyFilters();
        protected abstract void DoDisplay();

        public static bool ShowAtPosition(Rect rect, PackageFilterTab tab, PageFilters filters)
        {
            if (s_Window != null)
                return false;

            var nowMilliSeconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var justClosed = nowMilliSeconds < s_LastClosedTime + 150;

            if (!justClosed)
            {
                Event.current?.Use();

                if (tab != PackageFilterTab.AssetStore)
                    return false;

                s_Window = CreateInstance<AssetStoreFiltersWindow>();
                s_Window.Init(rect, filters);
            }

            return !justClosed;
        }

        private VisualElementCache cache { get; set; }
    }
}
