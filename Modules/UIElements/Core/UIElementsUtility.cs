// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.Layout;

namespace UnityEngine.UIElements
{

    internal static class UIElementsIMGUIUtility
    {


        internal static Action<IMGUIContainer> s_BeginContainerCallback;
        internal static Action<IMGUIContainer> s_EndContainerCallback;



        internal static EventBase CreateEvent(Event systemEvent)
        {
            return CreateEvent(systemEvent, systemEvent.rawType);
        }

        // In order for tests to run without an EditorWindow but still be able to send
        // events, we sometimes need to force the event type. IMGUI::GetEventType() (native) will
        // return the event type as Ignore if the proper views haven't yet been
        // initialized. This (falsely) breaks tests that rely on the event type. So for tests, we
        // just ensure the event type is what we originally set it to when we sent it.
        internal static EventBase CreateEvent(Event systemEvent, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.MouseMove:
                case EventType.TouchMove:
                    return PointerMoveEvent.GetPooled(systemEvent);
                case EventType.MouseDrag:
                    return PointerMoveEvent.GetPooled(systemEvent);
                case EventType.MouseDown:
                case EventType.TouchDown:
                    // If some buttons are already down, we generate PointerMove/MouseDown events.
                    // Otherwise we generate PointerDown/MouseDown events.
                    // See W3C pointer events recommendation: https://www.w3.org/TR/pointerevents2
                    // Note: sometimes systemEvent.button is already pressed (systemEvent is processed multiple times).
                    if (PointerDeviceState.HasAdditionalPressedButtons(PointerId.mousePointerId, systemEvent.button))
                    {
                        return PointerMoveEvent.GetPooled(systemEvent);
                    }
                    else
                    {
                        return PointerDownEvent.GetPooled(systemEvent);
                    }
                case EventType.MouseUp:
                case EventType.TouchUp:
                    // If more buttons are still down, we generate PointerMove/MouseUp events.
                    // Otherwise we generate PointerUp/MouseUp events.
                    // See W3C pointer events recommendation: https://www.w3.org/TR/pointerevents2
                    if (PointerDeviceState.HasAdditionalPressedButtons(PointerId.mousePointerId, systemEvent.button))
                    {
                        return PointerMoveEvent.GetPooled(systemEvent);
                    }
                    else
                    {
                        return PointerUpEvent.GetPooled(systemEvent);
                    }
                case EventType.ContextClick:
                    return ContextClickEvent.GetPooled(systemEvent);
                case EventType.MouseEnterWindow:
                    return MouseEnterWindowEvent.GetPooled(systemEvent);
                case EventType.MouseLeaveWindow:
                    return MouseLeaveWindowEvent.GetPooled(systemEvent);
                case EventType.ScrollWheel:
                    return WheelEvent.GetPooled(systemEvent);
                case EventType.KeyDown:
                    return KeyDownEvent.GetPooled(systemEvent);
                case EventType.KeyUp:
                    return KeyUpEvent.GetPooled(systemEvent);
                case EventType.DragUpdated:
                    return DragUpdatedEvent.GetPooled(systemEvent);
                case EventType.DragPerform:
                    return DragPerformEvent.GetPooled(systemEvent);
                case EventType.DragExited:
                    return DragExitedEvent.GetPooled(systemEvent);
                case EventType.ValidateCommand:
                    return ValidateCommandEvent.GetPooled(systemEvent);
                case EventType.ExecuteCommand:
                    return ExecuteCommandEvent.GetPooled(systemEvent);
                default:// Layout, Ignore, Used
                    return IMGUIEvent.GetPooled(systemEvent);
            }
        }


    }

    enum UnloadingSubscriber
    {
        StyleCache,
        InitialStyle,
        LayoutManager,
        Count
    }

    static partial class UnloadingUtility
    {
        static Action[] s_Subscribers = new Action[(int)UnloadingSubscriber.Count];

        static ProfilerMarker s_CodeUnloadingMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIElements.OnCodeUnloading");

        [OnCodeUnloading]
        static void OnCodeUnloading()
        {
            using (s_CodeUnloadingMarker.Auto())
            {
                for (int i = 0; i < s_Subscribers.Length; i++)
                {
                    s_Subscribers[i]?.Invoke();
                    s_Subscribers[i] = null;
                }
            }
        }

        // Allows classes to lazily register themselves for code unloading
        // Otherwise, directly adding [OnCodeUnloading] would implicitly trigger their static constructor
        internal static void SubscribeToUnloading(UnloadingSubscriber subscriber, Action callback)
        {
            Debug.Assert(s_Subscribers[(int)subscriber] == null);
            s_Subscribers[(int)subscriber] = callback;
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule", "UnityEditor.GraphToolkitModule")]
    internal static class UIElementsUtility
    {


        [Obsolete("Please use public APIs from the ui test framework (com.unity.ui.test-framework)  to simulate events for tests")]
        internal static EventBase CreateEvent(Event systemEvent)
        {
            return UIElementsIMGUIUtility.CreateEvent(systemEvent);
        }

        [Obsolete("Please use public APIs from the ui test framework (com.unity.ui.test-framework) to simulate events for tests")]
        internal static EventBase CreateEvent(Event systemEvent, EventType eventType)
        {
            return UIElementsIMGUIUtility.CreateEvent(systemEvent);
        }


        private static Dictionary<EntityId, Panel> s_UIElementsCache = new Dictionary<EntityId, Panel>();

        // When not in editor, this will be all white, so no impact on the overall color, except for the multiplication done on the color.
        internal static Color editorPlayModeTintColor = Color.white;
        // The usual height used for a control, such as a one-line text field. See --unity-metrics-single_line-height and EditorGUIUtility.singleLineHeight.
        internal static float singleLineHeight = 18;

        public const string hiddenClassName = "unity-hidden";
        internal static readonly UniqueStyleString hiddenClassNameUnique = new(hiddenClassName);

        internal static bool s_EnableOSXContextualMenuEventsOnNonOSXPlatforms;
        [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
        public static bool isOSXContextualMenuPlatform
        {
            get
            {
                RuntimePlatform platform = Application.platform;
                return platform == RuntimePlatform.OSXEditor || platform == RuntimePlatform.OSXPlayer ||
                       s_EnableOSXContextualMenuEventsOnNonOSXPlatforms;
            }
        }

        internal static void EnableOSXContextualMenuEventsOnNonOSXPlatforms()
        {
            s_EnableOSXContextualMenuEventsOnNonOSXPlatforms = true;
        }
        internal static void ResetOSXContextualMenuEventsOnNonOSXPlatforms()
        {
            s_EnableOSXContextualMenuEventsOnNonOSXPlatforms = false;
        }

        static internal List<Panel> s_PanelsIterationList = new List<Panel>();

        public static void RegisterCachedPanel(EntityId entityId, Panel panel)
        {
            s_UIElementsCache.Add(entityId, panel);
        }

        public static void RemoveCachedPanel(EntityId entityId)
        {
            s_UIElementsCache.Remove(entityId);
        }

        public static bool TryGetPanel(EntityId entityId, out Panel panel)
        {
            return s_UIElementsCache.TryGetValue(entityId, out panel);
        }


        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static void UpdateSchedulers()
        {
            if (LayoutManager.IsSharedManagerCreated)
            {
                LayoutManager.SharedManager.Collect();
            }

            // Since updating schedulers jumps into user code, the panels list might change while we're iterating,
            // we make a copy first.
            s_PanelsIterationList.Clear();
            UIElementsUtility.GetAllPanels(s_PanelsIterationList, ContextType.Editor);

            foreach (var panel in s_PanelsIterationList)
            {
                // Dispatch all timer update messages to each scheduled item
                panel.TickSchedulingUpdaters();
            }
        }

        internal static void RequestRepaintForPanels(Action<ScriptableObject> repaintCallback)
        {
            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;

                // Game panels' scheduler are ticked by the engine
                if (panel.contextType != ContextType.Editor)
                    continue;

                // Dispatch might have triggered a repaint request.
                if (panel.isDirty)
                {
                    repaintCallback(panel.ownerObject);
                }
            }
            TextGenerationInfo.OnRepaintEnd();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void GetAllPanels(List<Panel> panels, ContextType contextType)
        {
            var iterator = GetPanelsIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.contextType == contextType)
                {
                    panels.Add(iterator.Current.Value);
                }
            }
        }

        internal static Dictionary<EntityId, Panel>.Enumerator GetPanelsIterator()
        {
            return s_UIElementsCache.GetEnumerator();
        }

        internal static float PixelsPerUnitScaleForElement(VisualElement ve, Sprite sprite)
        {
            if (ve == null || ve.elementPanel == null || sprite == null)
                return 1.0f;

            float referencePixelsPerUnit = ve.elementPanel.referenceSpritePixelsPerUnit;
            float pixelsPerUnit = sprite.pixelsPerUnit;
            pixelsPerUnit = Mathf.Max(0.01f, pixelsPerUnit);
            return referencePixelsPerUnit / pixelsPerUnit;
        }

        internal static char[] s_Modifiers = new char[5] { '&', '%', '^', '#', '_' };

        internal static string ParseMenuName(string menuName)
        {
            if (string.IsNullOrEmpty(menuName))
                return string.Empty;

            var displayValue = menuName.TrimEnd();
            var separatorPos = displayValue.LastIndexOf(' ');
            if (separatorPos > -1)
            {
                int modifierPos = Array.IndexOf(s_Modifiers, displayValue[separatorPos + 1]);
                if (displayValue.Length > separatorPos + 1 && modifierPos > -1)
                    displayValue = displayValue.Substring(0, separatorPos).TrimEnd();
            }

            return displayValue;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static void MarkVisualTreeAssetAsChanged(VisualTreeAsset visualTreeAsset)
        {
            var iterator = GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;
                panel.liveReloadSystem.OnVisualTreeAssetChanged(visualTreeAsset);
            }
        }


        internal static readonly HashSet<StyleSheet> s_StyleSheetsRequiringRebuilding = new();
        internal static readonly HashSet<string> s_ReimportedStyleSheetsPath = new();
        internal static readonly List<StyleSheet> s_StyleSheetsRebuildList = new();
        internal static readonly List<string> s_ReimportedStyleSheetsPathList = new();
        internal static bool s_StopRecordingStyleSheetUnloads = false;

        internal static void MarkStyleSheetAsChanged(StyleSheet styleSheet)
        {
            if (!styleSheet)
                return;
            s_StyleSheetsRequiringRebuilding.Add(styleSheet);
            SelectorAccelerationCache.shared.Remove(styleSheet);
        }

        internal static void MarkStyleSheetAsChanged(string styleSheetPath)
        {
            if (string.IsNullOrEmpty(styleSheetPath))
                return;
            s_ReimportedStyleSheetsPath.Add(styleSheetPath);
            SelectorAccelerationCache.shared.Remove(styleSheetPath);
        }

        internal static void StopRecordingStyleSheetUnloads()
        {
            s_StopRecordingStyleSheetUnloads = true;
        }

        internal static void MarkStyleSheetAsLoaded(StyleSheet styleSheet)
        {
            // does nothing currently, but it is the mirror for MarkStyleSheetAsUnloaded()
        }

        internal static void MarkStyleSheetAsUnloaded(StyleSheet styleSheet)
        {
            // In some situations (i.e. domain reload), all StyleSheets will be unloaded
            // We don't want to record those since the cache won't be used anymore
            if (!s_StopRecordingStyleSheetUnloads)
                SelectorAccelerationCache.shared.Remove(styleSheet);
        }

        static void NotifyPanelsThatStyleSheetChanged(List<StyleSheet> styleSheets, List<string> styleSheetPaths)
        {
            var iterator = GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;
                panel.liveReloadSystem.OnStyleSheetChanged(styleSheets);
                panel.liveReloadSystem.OnStyleSheetChanged(styleSheetPaths);
            }
        }


        internal static void RebuildDirtyStyleSheets()
        {
            if (s_StyleSheetsRequiringRebuilding.Count == 0)
                return;

            try
            {
                StyleCache.ClearStyleCache();

                // Avoid any side-effects by copying the content before we iterate on it.
                s_StyleSheetsRebuildList.AddRange(s_StyleSheetsRequiringRebuilding);
                s_ReimportedStyleSheetsPathList.AddRange(s_ReimportedStyleSheetsPath);

                foreach (var styleSheet in s_StyleSheetsRebuildList)
                {
                    styleSheet.RebuildIfNecessary();
                }
                NotifyPanelsThatStyleSheetChanged(s_StyleSheetsRebuildList, s_ReimportedStyleSheetsPathList);
            }
            finally
            {
                s_StyleSheetsRebuildList.Clear();
                s_StyleSheetsRequiringRebuilding.Clear();
                s_ReimportedStyleSheetsPathList.Clear();
                s_ReimportedStyleSheetsPath.Clear();
            }
        }
    }
}
