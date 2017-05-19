// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    internal class UIElementsUtility
    {
        private static Stack<IMGUIContainer> s_ContainerStack = new Stack<IMGUIContainer>();
        private static Dictionary<int, Panel> s_UIElementsCache = new Dictionary<int, Panel>();

        private static Event s_EventInstance = new Event(); // event instance reused for ProcessEvent()

        private static EventDispatcher s_EventDispatcher;

        internal static Action<IMGUIContainer> s_BeginContainerCallback;
        internal static Action<IMGUIContainer> s_EndContainerCallback;

        internal static IDispatcher eventDispatcher
        {
            get
            {
                if (s_EventDispatcher == null)
                    s_EventDispatcher = new EventDispatcher();
                return s_EventDispatcher;
            }
        }

        static UIElementsUtility()
        {
            GUIUtility.takeCapture += TakeCapture;
            GUIUtility.releaseCapture += ReleaseCapture;
            GUIUtility.processEvent += ProcessEvent;
            GUIUtility.cleanupRoots += CleanupRoots;
            GUIUtility.endContainerGUIFromException += EndContainerGUIFromException;
        }

        private static void TakeCapture()
        {
            if (s_ContainerStack.Count > 0)
            {
                var topmostContainer = s_ContainerStack.Peek();
                if (eventDispatcher.capture != null && eventDispatcher.capture != topmostContainer)
                {
                    Debug.Log(string.Format("Should not grab hot control with an active capture (current={0} new={1}",
                            eventDispatcher.capture, topmostContainer));
                }
                eventDispatcher.TakeCapture(topmostContainer);
            }
        }

        private static void ReleaseCapture()
        {
            eventDispatcher.RemoveCapture();
        }

        private static bool ProcessEvent(int instanceID, IntPtr nativeEventPtr)
        {
            Panel panel;
            if (nativeEventPtr != IntPtr.Zero && s_UIElementsCache.TryGetValue(instanceID, out panel))
            {
                // Instead of allocating a new Event object every time
                // we reuse this instance and copy event data into it
                s_EventInstance.CopyFromPtr(nativeEventPtr);

                return DoDispatch(panel);
            }
            return false;
        }

        private static void CleanupRoots()
        {
            // see GUI.CleanupRoots
            s_EventInstance = null;
            s_EventDispatcher = null;
            s_UIElementsCache = null;
            s_ContainerStack = null;
            s_BeginContainerCallback = null;
            s_EndContainerCallback = null;
        }

        private static bool EndContainerGUIFromException(Exception exception)
        {
            // only End if we have a current container
            if (s_ContainerStack.Count > 0)
            {
                GUIUtility.EndContainer();
                s_ContainerStack.Pop();
            }

            return GUIUtility.ShouldRethrowException(exception);
        }

        internal static void BeginContainerGUI(GUILayoutUtility.LayoutCache cache, int instanceID, Event evt, IMGUIContainer container)
        {
            GUIUtility.BeginContainer(instanceID);
            s_ContainerStack.Push(container);
            GUIUtility.s_SkinMode = (int)container.contextType;
            GUIUtility.s_OriginalID = instanceID;


            Event.current = evt;

            // call AFTER setting current event
            if (s_BeginContainerCallback != null)
                s_BeginContainerCallback(container);


            GUILayoutUtility.BeginContainer(cache);
            GUIUtility.ResetGlobalState();

            Matrix4x4 g = container.globalTransform;
            GUI.matrix = g;

            // do an offset area for IMGUI
            Rect screenRect = container.position;

            // back to local
            var inv = container.globalTransform.inverse;
            Vector3 min;
            Vector3 max;
            if (container.IsDirty(ChangeType.Repaint) && container.lastWorldClip.size == Vector2.zero)
            {
                min = inv.MultiplyPoint3x4(container.globalBound.min);
                max = inv.MultiplyPoint3x4(container.globalBound.max);
            }
            else
            {
                min = inv.MultiplyPoint3x4(container.lastWorldClip.min);
                max = inv.MultiplyPoint3x4(container.lastWorldClip.max);
            }
            var localClip = Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x),
                    Math.Max(min.y, max.y));

            // combine clips
            float x1 = Mathf.Max(screenRect.x, localClip.x);
            float x2 = Mathf.Min(screenRect.x + screenRect.width, localClip.x + localClip.width);
            float y1 = Mathf.Max(screenRect.y, localClip.y);
            float y2 = Mathf.Min(screenRect.y + screenRect.height, localClip.y + localClip.height);

            // new global clip
            var clippedScreen = new Rect(x1, y1, x2 - x1, y2 - y1);

            var offset = new Vector2(Mathf.Round(screenRect.x - clippedScreen.x), Mathf.Round(screenRect.y - clippedScreen.y));
            GUI.BeginGroup(clippedScreen, GUIContent.none, GUIStyle.none, offset);
        }

        // End the 2D GUI.
        internal static void EndContainerGUI()
        {
            if (Event.current.type != EventType.Used)
            {
                GUI.EndGroup();
            }


            if (Event.current.type == EventType.Layout
                && s_ContainerStack.Count > 0)
            {
                var r = s_ContainerStack.Peek().globalBound;
                GUILayoutUtility.LayoutFromContainer(r.width, r.height);
            }
            // restore cache
            GUILayoutUtility.SelectIDList(GUIUtility.s_OriginalID, false);
            GUIContent.ClearStaticCache();

            if (s_ContainerStack.Count > 0)
            {
                IMGUIContainer container = s_ContainerStack.Peek();
                if (s_EndContainerCallback != null)
                    s_EndContainerCallback(container);
                GUIUtility.EndContainer();
                s_ContainerStack.Pop();
            }
        }

        // TODO rename skinMode to context type and make that explicit everywhere
        internal static ContextType GetGUIContextType()
        {
            return GUIUtility.s_SkinMode == 0 ? ContextType.Player : ContextType.Editor;
        }

        static bool DoDispatch(BaseVisualElementPanel panel)
        {
            bool usesEvent;

            if (s_EventInstance.type == EventType.Repaint)
            {
                panel.Repaint(s_EventInstance);
                // TODO get rid of this when we wrap every GUIView inside IMGUIContainers
                // here we pretend to use the repaint event
                // in order to suspend to suspend OnGUI() processing on the native side
                // since we've already run it if we have an IMGUIContainer
                usesEvent = panel.IMGUIContainersCount > 0;
            }
            else
            {
                panel.ValidateLayout();

                var result = s_EventDispatcher.DispatchEvent(s_EventInstance, panel);
                if (result == EventPropagation.Stop)
                {
                    panel.visualTree.Dirty(ChangeType.Repaint);
                }
                usesEvent = result == EventPropagation.Stop;
            }

            return usesEvent;
        }

        internal static Dictionary<int, Panel>.Enumerator GetPanelsIterator()
        {
            return s_UIElementsCache.GetEnumerator();
        }

        internal static Panel FindOrCreatePanel(int instanceId, ContextType contextType, IDataWatchService dataWatch = null, LoadResourceFunction loadResourceFunction = null)
        {
            Panel panel;
            if (!s_UIElementsCache.TryGetValue(instanceId, out panel))
            {
                panel = new Panel(instanceId, contextType, loadResourceFunction, dataWatch, eventDispatcher);
                s_UIElementsCache.Add(instanceId, panel);
            }
            else
            {
                Debug.Assert(contextType == panel.contextType, "Context type mismatch");
            }
            return panel;
        }

        internal static Panel FindOrCreatePanel(int instanceId)
        {
            return FindOrCreatePanel(instanceId, GetGUIContextType());
        }

        internal static void BeginBuilder(VisualContainer w)
        {
        }
    }
}
