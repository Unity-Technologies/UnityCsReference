// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    static class SearchEvent
    {
        public const string ViewStateUpdated = "search-view-state-updated";
        public const string SearchTextChanged = "search-text-changed";
        public const string SearchContextChanged = "search-context-changed";
        public const string SelectionHasChanged = "search-selection-has-changed";
        public const string RefreshBuilder = "search-refresh-builder";
        public const string BuilderRefreshed = "search-builder-refreshed";
        public const string FilterToggled = "search-filter-toggled";
        public const string ExecuteSearchQuery = "execute-search-query";
        public const string SearchQueryItemCountUpdated = "search-query-item-count-updated";
        public const string SaveUserQuery = "search-save-user-query";
        public const string SaveProjectQuery = "search-save-project-query";
        public const string UserQueryAdded = "search-user-query-added";
        public const string UserQueryRemoved = "search-user-query-removed";
        public const string ProjectQueryAdded = "search-project-query-added";
        public const string ProjectQueryRemoved = "search-project-query-removed";
        public const string ProjectQueryListChanged = "search-project-query-list-changed";
        public const string ActiveQueryChanged = "search-active-query-changed";
        public const string SaveActiveSearchQuery = "search-save-active-search-query";
        public const string SearchQueryChanged = "search-query-changed";
        public const string TogglePackages = "search-toggle-packages";
        public const string ToggleWantsMore = "search-toggle-wants-more";
        public const string RefreshContent = "search-refresh-content";
        public const string DisplayModeChanged = "search-display-mode-changed";
        public const string RequestResultViewButtons = "search-request-result-view-buttons";
        public const string ItemFavoriteStateChanged = "search-item-favorite-state-changed";
        public const string SearchFieldFocused = "search-field-focused";
        public const string SearchIndexesChanged = "search-indexes-changed";
    }

    interface ISearchElement
    {
        SearchContext context { get; }
        SearchViewState viewState { get; }
    }

    static class StringUtilsExtensions
    {
        public static string WithUssElement(this string blockName, string elementName) => blockName + "__" + elementName;

        public static string WithUssModifier(this string blockName, string modifier) => blockName + "--" + modifier;
    }

    static class SearchVisualElementExtensions
    {
        public static void SetClassState(this VisualElement self, in bool enabled, params string[] classNames)
        {
            if (enabled)
            {
                foreach (var n in classNames)
                    self.AddToClassList(n);
            }
            else
            {
                foreach (var n in classNames)
                    self.RemoveFromClassList(n);
            }
        }

        public static EditorWindow GetHostWindow(this VisualElement self)
        {
            if (self == null || self.elementPanel == null)
                return null;

            if (self.elementPanel.ownerObject is HostView hv)
                return hv.actualView;
            if (self.elementPanel.ownerObject is IEditorWindowModel ewm)
                return ewm.window;
            if (self.elementPanel.ownerObject is EditorWindow window)
                return window;
            return null;
        }

        public static ISearchWindow GetSearchHostWindow(this VisualElement self)
        {
            if (self?.elementPanel == null)
                return null;

            if (self.elementPanel.ownerObject is ISearchWindow sw)
                return sw;
            if (self.elementPanel.ownerObject is HostView hv && hv.actualView is ISearchWindow hvsw)
                return hvsw;
            if (self.elementPanel.ownerObject is IEditorWindowModel ewm && ewm.window is ISearchWindow ewmsw)
                return ewmsw;
            return null;
        }

        public static bool HostWindowHasFocus(this VisualElement self)
        {
            if (self?.elementPanel == null)
                return false;

            if (self.elementPanel.ownerObject is ISearchWindow sw)
                return sw.HasFocus();
            var hostWindow = self.GetHostWindow();
            return hostWindow?.hasFocus ?? false;
        }

        public static void RegisterFireAndForgetCallback<TEventType>(this VisualElement self, EventCallback<TEventType> callback)
            where TEventType : EventBase<TEventType>, new()
        {
            EventCallback<TEventType> outerCallback = null;
            outerCallback = evt =>
            {
                try
                {
                    callback?.Invoke(evt);
                }
                finally
                {
                    self.UnregisterCallback<TEventType>(outerCallback);
                }
            };
            self.RegisterCallback<TEventType>(outerCallback);
        }
    }

    abstract class SearchElement : VisualElement, ISearchElement
    {
        private const string ussBasePath = "StyleSheets/QuickSearch";
        private static readonly string ussPath = $"{ussBasePath}/SearchWindow.uss";
        private static readonly string ussPathDark = $"{ussBasePath}/SearchWindow_Dark.uss";
        private static readonly string ussPathLight = $"{ussBasePath}/SearchWindow_Light.uss";

        public static readonly string baseIconButtonClassName = "search-icon-button";

        protected readonly ISearchView m_ViewModel;

        public virtual SearchContext context => m_ViewModel.context;
        public virtual SearchViewState viewState => m_ViewModel.state;

        public bool attachedToPanel { get; private set; }
        public bool geometryRealized { get; private set; }

        public SearchElement(ISearchView viewModel)
        {
            m_ViewModel = viewModel;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public SearchElement(string name, ISearchView viewModel, params string[] classes)
            : this(viewModel)
        {
            this.name = name;
            m_ViewModel = viewModel;

            foreach (var c in classes)
                AddToClassList(c);
        }

        protected virtual void OnAttachToPanel(AttachToPanelEvent evt)
        {
            attachedToPanel = true;
            this.RegisterFireAndForgetCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
        }

        protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            attachedToPanel = false;
            geometryRealized = false;
        }

        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            geometryRealized = true;
        }

        internal static void HideElements<T>(params T[] elements) where T : VisualElement
        {
            HideElements(elements.AsEnumerable());
        }

        internal static void HideElements<T>(IEnumerable<T> elements) where T : VisualElement
        {
            foreach (var e in elements)
                e.style.display = DisplayStyle.None;
        }

        internal static void ShowElements<T>(params T[] elements) where T : VisualElement
        {
            foreach (var e in elements)
                e.style.display = DisplayStyle.Flex;
        }

        internal static bool IsElementVisible(VisualElement e)
        {
            return e.style.display != DisplayStyle.None;
        }

        internal static bool IsPartOf<T>(in VisualElement e) where T : VisualElement
        {
            if (e == null)
                return false;
            if (e is T)
                return true;
            return IsPartOf<T>(e.parent);
        }

        internal static VisualElement Create(in string name, params string[] classNames)
        {
            return Create<VisualElement>(name, classNames);
        }

        internal static T Create<T>(in string name, params string[] classNames) where T : VisualElement, new()
        {
            var ve = new T { name = name };
            foreach (var n in classNames)
                ve.AddToClassList(n);
            return ve;
        }

        internal static T Create<T, TEventType>(in string name, EventCallback<TEventType> handler, params string[] classes)
            where T : VisualElement, new()
            where TEventType : EventBase<TEventType>, new()
        {
            var ve = Create<T>(name, classes);
            ve.RegisterCallback(handler);
            return ve;
        }

        internal static Button CreateButton(in string name, in GUIContent content, Action handler, params string[] classNames)
        {
            var button = CreateButton(name, content.text, content.tooltip, handler, classNames);
            button.Add(new Image() { image = content.image });
            return button;
        }

        internal static Button CreateButton(in string name, in string text, in string tooltip, Action handler, params string[] classNames)
        {
            var button = Create<Button>(name, classNames);
            button.clickable = new Clickable(handler);
            button.text = text;
            button.tooltip = tooltip;
            return button;
        }

        internal static Button CreateButton(in string name, in string tooltip, Action handler, params string[] classNames)
        {
            return CreateButton(name, string.Empty, tooltip, handler, classNames);
        }

        internal static ToolbarToggle CreateToolbarToggle(in string name, in string tooltip, bool value, EventCallback<ChangeEvent<bool>> handler, params string[] classNames)
        {
            var btn = Create<ToolbarToggle, ChangeEvent<bool>>(name, handler, classNames);
            btn.tooltip = tooltip;
            btn.SetValueWithoutNotify(value);
            return btn;
        }

        internal static Label CreateLabel(in string text, in string tooltip, PickingMode pickingMode, params string[] classNames)
        {
            var l = Create<Label>(null, classNames);
            l.text = text;
            l.tooltip = tooltip;
            l.pickingMode = pickingMode;
            return l;
        }

        internal static Label CreateLabel(in string text, in string tooltip, params string[] classNames)
        {
            return CreateLabel(text, tooltip, PickingMode.Position, classNames);
        }

        internal static Label CreateLabel(in string text, params string[] classNames)
        {
            return CreateLabel(text, null, classNames);
        }

        internal static Label CreateLabel(in string text, PickingMode pickingMode, params string[] classNames)
        {
            return CreateLabel(text, null, pickingMode, classNames);
        }

        internal static Label CreateLabel(in GUIContent content, params string[] classNames)
        {
            return CreateLabel(content.text, content.tooltip, classNames);
        }

        internal static Label CreateLabel(in GUIContent content, PickingMode pickingMode, params string[] classNames)
        {
            return CreateLabel(content.text, content.tooltip, pickingMode, classNames);
        }

        internal void Emit(string eventName, params object[] arguments)
        {
            Dispatcher.Emit(eventName, new SearchEventPayload(this, arguments));
        }

        internal void Emit(string eventName, SearchEventPrepareHandler onPrepare, SearchEventResultHandler onResolved, params object[] arguments)
        {
            Dispatcher.Emit(eventName, new SearchEventPayload(this, arguments), onPrepare, onResolved);
        }

        protected Action On(string eventName, SearchEventHandler handler)
        {
            return Dispatcher.On(eventName, evt =>
            {
                if (!IsEventFromSameView(evt))
                    return;
                handler(evt);
            }, SearchEventManager.GetSearchEventHandlerHashCode(handler));
        }

        protected Action OnAll(string eventName, SearchEventHandler handler)
        {
            return Dispatcher.On(eventName, handler, SearchEventManager.GetSearchEventHandlerHashCode(handler));
        }

        protected Action OnOther(string eventName, SearchEventHandler handler)
        {
            return Dispatcher.On(eventName, evt =>
            {
                if (IsEventFromSameView(evt))
                    return;
                handler(evt);
            }, SearchEventManager.GetSearchEventHandlerHashCode(handler));
        }

        protected void Off(string eventName, SearchEventHandler handler)
        {
            Dispatcher.Off(eventName, SearchEventManager.GetSearchEventHandlerHashCode(handler));
        }

        protected bool IsEventFromSameView(ISearchEvent evt)
        {
            return evt.sourceViewState == viewState;
        }

        protected Action RegisterGlobalEventHandler<T>(SearchGlobalEventHandler<T> eventHandler, int priority)
            where T : EventBase
        {
            return viewState.globalEventManager.RegisterGlobalEventHandler(eventHandler, priority);
        }

        protected void UnregisterGlobalEventHandler<T>(SearchGlobalEventHandler<T> eventHandler)
            where T : EventBase
        {
            viewState.globalEventManager.UnregisterGlobalEventHandler(eventHandler);
        }

        internal static void AppendStyleSheets(VisualElement ve)
        {
            if (!ve.HasStyleSheetPath(ussPath))
                ve.AddStyleSheetPath(ussPath);

            var themedUssPath = EditorGUIUtility.isProSkin ? ussPathDark : ussPathLight;
            if (!ve.HasStyleSheetPath(themedUssPath))
                ve.AddStyleSheetPath(themedUssPath);
        }

        internal static VisualElement FlexibleSpace()
        {
            var fspace = new VisualElement();
            fspace.AddToClassList("flex-space");
            return fspace;
        }
    }
}
