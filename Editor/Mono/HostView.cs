// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Profiling;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEditor.StyleSheets;
using UnityEditor.UIElements;

namespace UnityEditor
{
    internal class HostView : GUIView, IEditorWindowModel
    {
        static class Styles
        {
            public static readonly GUIStyle background = new GUIStyle("hostview");
            public static readonly GUIStyle overlay = "dockareaoverlay";
            public static readonly GUIStyle paneOptions = "PaneOptions";
            public static readonly GUIStyle tabWindowBackground = "TabWindowBackground";

            static Styles()
            {
                // Fix annoying GUILayout issue: When using GUILayout in Utility windows there
                // was always padded 10 px at the top! Todo: Fix this in EditorResources
                background.padding.top = 0;
            }
        }

        static string kPlayModeDarkenKey = "Playmode tint";
        internal static PrefColor kPlayModeDarken = new PrefColor(kPlayModeDarkenKey, .8f, .8f, .8f, 1);
        internal static event Action<HostView> actualViewChanged;

        [SerializeField] private EditorWindow m_ActualView;
        [NonSerialized] protected readonly RectOffset m_BorderSize = new RectOffset();

        protected delegate void EditorWindowDelegate();
        protected delegate void EditorWindowShowButtonDelegate(Rect rect);

        protected EditorWindowDelegate m_OnGUI;
        protected EditorWindowDelegate m_OnFocus;
        protected EditorWindowDelegate m_OnLostFocus;
        protected EditorWindowDelegate m_OnProjectChange;
        protected EditorWindowDelegate m_OnSelectionChange;
        protected EditorWindowDelegate m_OnDidOpenScene;
        protected EditorWindowDelegate m_OnInspectorUpdate;
        protected EditorWindowDelegate m_OnHierarchyChange;
        protected EditorWindowDelegate m_OnBecameVisible;
        protected EditorWindowDelegate m_OnBecameInvisible;
        protected EditorWindowDelegate m_Update;
        protected EditorWindowDelegate m_ModifierKeysChanged;
        protected EditorWindowShowButtonDelegate m_ShowButton;

        internal EditorWindow actualView
        {
            get { return m_ActualView; }
            set { SetActualViewInternal(value, sendEvents: true); }
        }

        internal void SetActualViewInternal(EditorWindow value, bool sendEvents)
        {
            if (m_ActualView == value)
                return;

            DeregisterSelectedPane(clearActualView: true, sendEvents: true);
            m_ActualView = value;
            m_ActualViewName = null;

            CreateDelegates();

            name = GetViewName();
            SetActualViewName(name);
            RegisterSelectedPane(sendEvents);
            actualViewChanged?.Invoke(this);
        }

        private void CreateDelegates()
        {
            m_OnGUI = CreateDelegate("OnGUI");
            m_OnFocus = CreateDelegate("OnFocus");
            m_OnLostFocus = CreateDelegate("OnLostFocus");
            m_OnProjectChange = CreateDelegate("OnProjectChange");
            m_OnSelectionChange = CreateDelegate("OnSelectionChange");
            m_OnDidOpenScene = CreateDelegate("OnDidOpenScene");
            m_OnInspectorUpdate = CreateDelegate("OnInspectorUpdate");
            m_OnHierarchyChange = CreateDelegate("OnHierarchyChange");
            m_OnBecameVisible = CreateDelegate("OnBecameVisible");
            m_OnBecameInvisible = CreateDelegate("OnBecameInvisible");
            m_Update = CreateDelegate("Update");
            m_ModifierKeysChanged = CreateDelegate("ModifierKeysChanged");
            var methodInfo = GetPaneMethod("ShowButton");
            if (methodInfo != null)
                m_ShowButton = (EditorWindowShowButtonDelegate)Delegate.CreateDelegate(typeof(EditorWindowShowButtonDelegate), m_ActualView, methodInfo);
        }

        internal void ResetActiveView()
        {
            DeregisterSelectedPane(clearActualView: false, sendEvents: true);
            RegisterSelectedPane(sendEvents: true);
            actualViewChanged?.Invoke(this);
        }

        internal void UpdateMargins(EditorWindow window)
        {
            UpdateViewMargins(window);
        }

        RectOffset IEditorWindowModel.viewMargins => GetBorderSize();

        Action IEditorWindowModel.onSplitterGUIHandler { get; set; }

        protected void UpdateViewMargins(EditorWindow view)
        {
            if (view == null)
                return;

            editorWindowBackend?.ViewMarginsChanged();
        }

        protected override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            SetActualViewPosition(newPos);
        }

        protected virtual void SetActualViewPosition(Rect newPos)
        {
            if (m_ActualView != null)
            {
                m_ActualView.m_Pos = newPos;
                UpdateViewMargins(m_ActualView);
                m_ActualView.OnResized();
            }
        }

        internal override void SetWindow(ContainerWindow win)
        {
            base.SetWindow(win);
            if (m_ActualView != null)
            {
                UpdateViewMargins(m_ActualView);
            }
        }

        protected override void OnEnable()
        {
            CreateDelegates();
            EditorPrefs.onValueWasUpdated += PlayModeTintColorChangedCallback;
            base.OnEnable();

            RegisterSelectedPane(sendEvents: true);
        }

        protected override void OnDisable()
        {
            EditorPrefs.onValueWasUpdated -= PlayModeTintColorChangedCallback;
            base.OnDisable();
            DeregisterSelectedPane(clearActualView: false, sendEvents: true);
            // Host views are destroyed in the middle of an OnGUI loop, so we need to ensure that we're not invoking
            // OnGUI on destroyed instances.
            m_OnGUI = null;
            m_Update = null;
        }

        private void HandleSplitView()
        {
            SplitView sp = parent as SplitView;
            if (Event.current.type == EventType.Repaint && sp)
            {
                View view = this;
                while (sp)
                {
                    int id = sp.controlID;

                    if (id == GUIUtility.hotControl || GUIUtility.hotControl == 0)
                    {
                        int idx = sp.IndexOfChild(view);
                        if (sp.vertical)
                        {
                            if (idx != 0)
                                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, SplitView.kGrabDist), MouseCursor.SplitResizeUpDown, id);
                            else if (idx != sp.children.Length - 1)
                                EditorGUIUtility.AddCursorRect(new Rect(0, position.height - SplitView.kGrabDist, position.width, SplitView.kGrabDist), MouseCursor.SplitResizeUpDown, id);
                        }
                        else // horizontal
                        {
                            if (idx != 0)
                                EditorGUIUtility.AddCursorRect(new Rect(0, 0, SplitView.kGrabDist, position.height), MouseCursor.SplitResizeLeftRight, id);
                            else if (idx != sp.children.Length - 1)
                                EditorGUIUtility.AddCursorRect(new Rect(position.width - SplitView.kGrabDist, 0, SplitView.kGrabDist, position.height), MouseCursor.SplitResizeLeftRight, id);
                        }
                    }

                    view = sp;
                    sp = sp.parent as SplitView;
                }

                sp = (SplitView)parent;
            }

            if (sp)
            {
                Event e = new Event(Event.current);
                e.mousePosition += new Vector2(position.x, position.y);
                sp.SplitGUI(e);
                if (e.type == EventType.Used)
                    Event.current.Use();
            }
        }

        protected override void OldOnGUI()
        {
            // Call reset GUI state as first thing so GUI.color is correct when drawing window decoration.
            EditorGUIUtility.ResetGUIState();
            DoWindowDecorationStart();

            using (new GUILayout.VerticalScope(Styles.background))
            {
                if (actualView)
                    actualView.m_Pos = screenPosition;

                try
                {
                    HandleSplitView();
                    m_OnGUI?.Invoke();
                }
                finally
                {
                    CheckNotificationStatus();

                    DoWindowDecorationEnd();
                    EditorGUI.ShowRepaints();
                }
            }
        }

        protected override bool OnFocus()
        {
            m_OnFocus?.Invoke();

            // Callback could have killed us. If so, die now...
            if (!this)
                return false;

            editorWindowBackend?.Focused();

            Repaint();
            return true;
        }

        internal void OnLostFocus()
        {
            EditorGUI.EndEditingActiveTextField();
            m_OnLostFocus?.Invoke();

            // Callback could have killed us
            if (!this)
                return;

            editorWindowBackend?.Blurred();

            Repaint();
        }

        protected override void OnDestroy()
        {
            if (m_ActualView)
                DestroyImmediate(m_ActualView, true);
            base.OnDestroy();
        }

        private static readonly Type[] k_PaneTypes =
        {
            typeof(SceneView),
            typeof(GameView),
            typeof(InspectorWindow),
            typeof(SceneHierarchyWindow),
            typeof(ProjectBrowser),
            typeof(ProfilerWindow),
            typeof(AnimationWindow)
        };

        private static IEnumerable<Type> GetCurrentModePaneTypes(string modePaneTypeSectionName)
        {
            var modePaneTypes = ModeService.GetModeDataSectionList<string>(ModeService.currentIndex, modePaneTypeSectionName);
            var editorWindowTypes = TypeCache.GetTypesDerivedFrom<EditorWindow>();
            foreach (var paneTypeName in modePaneTypes)
            {
                var paneType = editorWindowTypes.FirstOrDefault(t => t.Name.EndsWith(paneTypeName));
                if (paneType != null)
                    yield return paneType;
                else
                    Debug.LogWarning($"Cannot find editor window pane type {paneTypeName} for editor mode {ModeService.currentId}.");
            }
        }

        private static IEnumerable<Type> GetDefaultPaneTypes()
        {
            const string k_PaneTypesSectionName = "pane_types";
            if (!ModeService.HasSection(ModeService.currentIndex, k_PaneTypesSectionName))
                return k_PaneTypes;
            return GetCurrentModePaneTypes(k_PaneTypesSectionName);
        }

        protected IEnumerable<Type> GetPaneTypes()
        {
            foreach (var paneType in GetDefaultPaneTypes())
                yield return paneType;

            var extraPaneTypes = m_ActualView.GetExtraPaneTypes().ToList();
            if (extraPaneTypes.Count > 0)
            {
                yield return null; // for spacer
                foreach (var paneType in extraPaneTypes)
                    yield return paneType;
            }
        }

        // Messages sent by Unity to editor windows today.
        // The implementation is not very good, but oh well... it gets the message across.
        internal void OnMainWindowMove()
        {
            Invoke("OnMainWindowMove");
        }

        internal void OnProjectChange()
        {
            m_OnProjectChange?.Invoke();
        }

        internal void OnSelectionChange()
        {
            m_OnSelectionChange?.Invoke();
        }

        internal void OnDidOpenScene()
        {
            m_OnDidOpenScene?.Invoke();
        }

        internal void OnInspectorUpdate()
        {
            m_OnInspectorUpdate?.Invoke();
        }

        internal void OnHierarchyChange()
        {
            m_OnHierarchyChange?.Invoke();
        }

        EditorWindowDelegate CreateDelegate(string methodName)
        {
            var methodInfo = GetPaneMethod(methodName);
            if (methodInfo != null)
                return (EditorWindowDelegate)Delegate.CreateDelegate(typeof(EditorWindowDelegate), m_ActualView, methodInfo);
            return null;
        }

        MethodInfo GetPaneMethod(string methodName)
        {
            return GetPaneMethod(methodName, m_ActualView);
        }

        protected MethodInfo GetPaneMethod(string methodName, object obj)
        {
            if (obj == null)
                return null;

            Type t = obj.GetType();

            while (t != null)
            {
                var method = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    return method;

                t = t.BaseType;
            }
            return null;
        }

        private string m_ActualViewName;
        private string GetActualViewName()
        {
            if (m_ActualViewName != null)
                return m_ActualViewName;
            m_ActualViewName = actualView != null ? actualView.GetType().Name : GetType().Name;
            return m_ActualViewName;
        }

        public void InvokeOnGUI(Rect onGUIPosition, Rect viewRect)
        {
            if (!this)
                return;

            DoWindowDecorationStart();

            BeginOffsetArea(viewRect, GUIContent.none, Styles.tabWindowBackground);

            EditorGUIUtility.ResetGUIState();

            bool isExitGUIException = false;
            try
            {
                using (new EditorPerformanceTracker($"{GetActualViewName()}.OnGUI.{Event.current.type}"))
                {
                    m_OnGUI?.Invoke();
                }
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is ExitGUIException)
                    isExitGUIException = true;
                throw;
            }
            finally
            {
                // We can't reset gui state after ExitGUI we just want to bail completely
                if (!isExitGUIException)
                {
                    CheckNotificationStatus();

                    EndOffsetArea();

                    EditorGUIUtility.ResetGUIState();

                    DoWindowDecorationEnd();

                    if (Event.current != null && Event.current.type == EventType.Repaint)
                        Styles.overlay.Draw(onGUIPosition, GUIContent.none, 0);
                }
            }
        }

        ///  TODO: Optimize with Delegate.CreateDelegate
        protected void Invoke(string methodName)
        {
            Invoke(methodName, m_ActualView);
        }

        protected void Invoke(string methodName, object obj)
        {
            MethodInfo mi = GetPaneMethod(methodName, obj);
            mi?.Invoke(obj, null);
        }

        EditorWindow IEditorWindowModel.window => m_ActualView;

        public IEditorWindowBackend editorWindowBackend
        {
            get { return windowBackend as IEditorWindowBackend; }
            set { windowBackend = value; }
        }

        protected void RegisterSelectedPane(bool sendEvents)
        {
            if (!m_ActualView)
                return;

            m_ActualView.m_Parent = this;

            ValidateWindowBackendForCurrentView();

            editorWindowBackend?.OnRegisterWindow();

            if (GetPaneMethod("Update") != null)
            {
                EditorApplication.update -= SendUpdate;
                EditorApplication.update += SendUpdate;
            }

            if (GetPaneMethod("ModifierKeysChanged") != null)
            {
                EditorApplication.modifierKeysChanged -= SendModKeysChanged;
                EditorApplication.modifierKeysChanged += SendModKeysChanged;
            }

            m_ActualView.MakeParentsSettingsMatchMe();

            if (m_ActualView.m_FadeoutTime != 0)
            {
                EditorApplication.update -= m_ActualView.CheckForWindowRepaint;
                EditorApplication.update += m_ActualView.CheckForWindowRepaint;
            }

            if (sendEvents)
            {
                try
                {
                    m_OnBecameVisible?.Invoke();
                    m_OnFocus?.Invoke();
                }
                catch (TargetInvocationException ex)
                {
                    // We need to catch these so the window initialization doesn't get screwed
                    if (ex.InnerException != null)
                        Debug.LogError(ex.InnerException.GetType().Name + ":" + ex.InnerException.Message);
                }
            }

            UpdateViewMargins(m_ActualView);
        }

        protected void DeregisterSelectedPane(bool clearActualView, bool sendEvents)
        {
            if (!m_ActualView)
                return;

            editorWindowBackend?.OnUnregisterWindow();

            if (m_Update != null)
                EditorApplication.update -= SendUpdate;

            if (m_ModifierKeysChanged != null)
                EditorApplication.modifierKeysChanged -= SendModKeysChanged;

            if (m_ActualView.m_FadeoutTime != 0)
            {
                EditorApplication.update -= m_ActualView.CheckForWindowRepaint;
            }

            if (clearActualView)
            {
                var onLostFocus = m_OnLostFocus;
                var onBecameInvisible = m_OnBecameInvisible;

                m_ActualView = null;
                if (sendEvents)
                {
                    onLostFocus?.Invoke();
                    onBecameInvisible?.Invoke();
                }
            }
        }

        private bool m_NotificationIsVisible;

        bool IEditorWindowModel.notificationVisible => m_NotificationIsVisible;

        protected void CheckNotificationStatus()
        {
            if (m_ActualView != null && m_ActualView.m_FadeoutTime != 0)
            {
                if (!m_NotificationIsVisible)
                {
                    m_NotificationIsVisible = true;
                    editorWindowBackend?.NotificationVisibilityChanged();
                }
            }
            else if (m_NotificationIsVisible)
            {
                m_NotificationIsVisible = false;
                editorWindowBackend?.NotificationVisibilityChanged();
            }
        }

        void SendUpdate()
        {
            m_Update?.Invoke();
        }

        void SendModKeysChanged()
        {
            m_ModifierKeysChanged?.Invoke();
        }

        internal RectOffset borderSize => GetBorderSize();

        protected virtual RectOffset GetBorderSize() { return m_BorderSize; }

        private static WindowAction[] s_windowActions;
        private static WindowAction[] windowActions
        {
            get
            {
                if (s_windowActions == null)
                {
                    s_windowActions = FetchWindowActionFromAttribute();
                }
                return s_windowActions;
            }
        }

        public static SVC<float> genericMenuLeftOffset = new SVC<float>("--window-generic-menu-left-offset", 20f);
        public static SVC<float> genericMenuFloatingLeftOffset = new SVC<float>("--window-floating-generic-menu-left-offset", 20f);
        internal static float GetGenericMenuLeftOffset(bool addFloatingWindowButtonsTopRight)
        {
            if (addFloatingWindowButtonsTopRight)
                return genericMenuFloatingLeftOffset + ContainerWindow.buttonStackWidth;
            else
                return genericMenuLeftOffset;
        }

        internal float GetExtraButtonsWidth()
        {
            float extraWidth = 0;

            if (m_ShowButton != null) extraWidth += ContainerWindow.kButtonWidth;

            foreach (var item in windowActions)
            {
                if (item != null && (item.validateHandler == null || item.validateHandler(actualView, item)) && item.width.HasValue)
                    extraWidth += item.width.Value + k_iconMargin;
            }
            return extraWidth;
        }

        internal const float k_iconMargin = 1f;
        protected void ShowGenericMenu(float leftOffset, float topOffset)
        {
            Rect paneMenu = new Rect(leftOffset, topOffset, Styles.paneOptions.fixedWidth, Styles.paneOptions.fixedHeight);
            if (EditorGUI.DropdownButton(paneMenu, GUIContent.none, FocusType.Passive, Styles.paneOptions))
                PopupGenericMenu(m_ActualView, paneMenu);

            // Give panes an option of showing a small button next to the generic menu (used for inspector lock icon
            if (m_ShowButton != null)
            {
                leftOffset -= ContainerWindow.kButtonWidth + k_iconMargin;
                m_ShowButton.Invoke(new Rect(leftOffset, topOffset, ContainerWindow.kButtonWidth, ContainerWindow.kButtonHeight));
            }

            foreach (var item in windowActions)
            {
                if (item != null && (item.validateHandler == null || item.validateHandler(actualView, item)) && item.width.HasValue)
                {
                    leftOffset -= item.width.Value + k_iconMargin;
                    Rect itemRect = new Rect(leftOffset, topOffset, item.width.Value, ContainerWindow.kButtonHeight);
                    if (item.drawHandler != null)
                    {
                        if (item.drawHandler(actualView, item, itemRect))
                            item.executeHandler(actualView, item);
                    }
                    else if (item.icon != null)
                    {
                        if (EditorGUI.Button(itemRect, EditorGUIUtility.TrIconContent(item.icon, item.menuPath), EditorStyles.iconButton))
                            item.executeHandler(actualView, item);
                    }
                }
            }
        }

        private static WindowAction[] FetchWindowActionFromAttribute()
        {
            var methods = AttributeHelper.GetMethodsWithAttribute<WindowActionAttribute>();
            return methods.methodsWithAttributes.Select(method =>
            {
                try
                {
                    var callback = Delegate.CreateDelegate(typeof(Func<WindowAction>), method.info) as Func<WindowAction>;
                    return callback?.Invoke();
                }
                catch (Exception)
                {
                    Debug.LogError("Cannot create Window Action for: " + method.info.Name);
                }
                return null;
            }).OrderBy(a => a.priority).ToArray();
        }

        public void PopupGenericMenu(EditorWindow view, Rect pos)
        {
            GenericMenu menu = new GenericMenu();

            IHasCustomMenu menuProvider = view as IHasCustomMenu;
            if (menuProvider != null)
                menuProvider.AddItemsToMenu(menu);

            AddDefaultItemsToMenu(menu, view);

            if (view != null)
                AddWindowActionMenu(menu, view);

            menu.DropDown(pos);
            Event.current.Use();
        }

        internal static void AddWindowActionMenu(GenericMenu menu, EditorWindow view)
        {
            bool itemAdded = false;
            int previousItemPriority = 0;
            foreach (var item in windowActions)
            {
                if (item != null && (item.validateHandler == null || item.validateHandler(view, item)) && !string.IsNullOrEmpty(item.menuPath))
                {
                    if (!itemAdded)
                    {
                        menu.AddSeparator("");
                        itemAdded = true;
                    }
                    else if (item.priority >= previousItemPriority + 10)
                        menu.AddSeparator("");

                    menu.AddItem(new GUIContent(item.menuPath, item.icon), false, () => item.executeHandler(view, item));
                    previousItemPriority = item.priority;
                }
            }
        }

        private void Inspect(object userData)
        {
            Selection.activeObject = (UnityEngine.Object)userData;
        }

        internal void Reload(object userData)
        {
            EditorWindow window = userData as EditorWindow;
            if (window == null)
                return;

            // Get some info on the existing window.
            Type windowType = window.GetType();

            // Save what we can of the window.
            string windowJson = EditorJsonUtility.ToJson(window);

            DockArea dockArea = window.m_Parent as DockArea;
            if (dockArea != null)
            {
                int windowIndex = dockArea.m_Panes.IndexOf(window);

                // Destroy window.
                dockArea.RemoveTab(window, false); // Don't kill dock if empty.
                DestroyImmediate(window, true);

                // Create window.
                window = EditorWindow.CreateInstance(windowType) as EditorWindow;
                dockArea.AddTab(windowIndex, window);
            }
            else
            {
                // Close the existing window.
                window.Close();

                // Recreate window.
                window = EditorWindow.CreateInstance(windowType) as EditorWindow;
                if (window != null)
                    window.Show();
            }

            // Restore what we can of the window.
            EditorJsonUtility.FromJsonOverwrite(windowJson, window);
        }

        protected virtual void AddDefaultItemsToMenu(GenericMenu menu, EditorWindow window)
        {
            if (menu.GetItemCount() != 0)
                menu.AddSeparator("");

            if (window && Unsupported.IsDeveloperMode())
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("Inspect Window"), false, Inspect, window);
                menu.AddItem(EditorGUIUtility.TrTextContent("Inspect View"), false, Inspect, window.m_Parent);
                menu.AddItem(EditorGUIUtility.TrTextContent("Reload Window _f5"), false, Reload, window);

                menu.AddSeparator("");
            }
        }

        Color IEditorWindowModel.playModeTintColor => kPlayModeDarken.Color;

        private void PlayModeTintColorChangedCallback(string key)
        {
            if (key == kPlayModeDarkenKey)
            {
                editorWindowBackend?.PlayModeTintColorChanged();
            }
        }
    }
}
