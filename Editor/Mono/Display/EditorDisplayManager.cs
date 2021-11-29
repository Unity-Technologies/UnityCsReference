// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Modules;

namespace UnityEditor
{
    internal enum DisplayAPIControlMode
    {
        FromEditor,
        FromRuntime
    }

    [InitializeOnLoad]
    internal class EditorDisplayManager : ScriptableSingleton<EditorDisplayManager>
    {
        private PlayModeView[] m_views;
        private int m_displayCount;
        private DisplayAPIControlMode m_mode;
        private int m_maxDisplays;
        private BuildTarget m_currentBuildTarget;

        static EditorDisplayManager()
        {
            UnsubscribeEditorDisplayCallback();
            SubscribeEditorDisplayCallback();
        }

        private static void SubscribeEditorDisplayCallback()
        {
            Display.onGetSystemExt += GetSystemExtImpl;
            Display.onGetRenderingExt += GetRenderingExtImpl;
            Display.onGetRenderingBuffers += GetRenderingBuffersImpl;
            Display.onSetRenderingResolution += SetRenderingResolutionImpl;
            Display.onActivateDisplay += ActivateDisplayImpl;
            Display.onSetParams += SetParamsImpl;
            Display.onRelativeMouseAt += RelativeMouseAtImpl;
            Display.onGetActive += GetActiveImpl;
            Display.onRequiresBlitToBackbuffer += RequiresBlitToBackbufferImpl;
            Display.onRequiresSrgbBlitToBackbuffer += RequiresSrgbBlitToBackbufferImpl;
        }

        private static void UnsubscribeEditorDisplayCallback()
        {
            Display.onGetSystemExt -= GetSystemExtImpl;
            Display.onGetRenderingExt -= GetRenderingExtImpl;
            Display.onGetRenderingBuffers -= GetRenderingBuffersImpl;
            Display.onSetRenderingResolution -= SetRenderingResolutionImpl;
            Display.onActivateDisplay -= ActivateDisplayImpl;
            Display.onSetParams -= SetParamsImpl;
            Display.onRelativeMouseAt -= RelativeMouseAtImpl;
            Display.onGetActive -= GetActiveImpl;
            Display.onRequiresBlitToBackbuffer -= RequiresBlitToBackbufferImpl;
            Display.onRequiresSrgbBlitToBackbuffer -= RequiresSrgbBlitToBackbufferImpl;
        }

        private void OnEnable()
        {
            Initialize();
            EditorApplication.update += OnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnUpdate;
        }

        public void Initialize()
        {
            m_currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            m_displayCount = 0;

            m_maxDisplays = ModuleManager.ShouldShowMultiDisplayOption() ?
                GetDisplayNamesForBuildTarget(EditorUserBuildSettings.activeBuildTarget).Length : 1;

            m_views = new PlayModeView[m_maxDisplays];
            UpdateAssociatedPlayModeView();
        }

        private void OnUpdate()
        {
            if (m_currentBuildTarget != EditorUserBuildSettings.activeBuildTarget)
            {
                Initialize();
            }
        }

        private void UpdateAssociatedPlayModeView()
        {
            for (var i = 0; i < m_maxDisplays; ++i)
            {
                var view = PlayModeView.GetAssociatedViewForTargetDisplay(i);
                if (m_views[i] == view)
                {
                    continue;
                }

                m_views[i] = view;
                if (view != null)
                {
                    EditorDisplayUtility.AddVirtualDisplay(i, (int)view.targetSize.x, (int)view.targetSize.y);
                }
                else
                {
                    EditorDisplayUtility.RemoveVirtualDisplay(i);
                }
            }

            UpdateDisplayList(false);
        }

        private void UpdateDisplayList(bool recreate)
        {
            recreate |= m_mode != EditorFullscreenController.DisplayAPIMode;
            m_mode = EditorFullscreenController.DisplayAPIMode;

            if (EditorFullscreenController.DisplayAPIMode == DisplayAPIControlMode.FromEditor)
            {
                var previousDisplayCount = m_displayCount;
                for (var i = m_maxDisplays - 1; i >= 0; --i)
                {
                    if (m_views[i] != null)
                    {
                        m_displayCount = i + 1;
                        recreate |= m_displayCount != previousDisplayCount;
                        break;
                    }
                }
            }
            else
            {
                var nDisplays = EditorDisplayUtility.GetNumberOfConnectedDisplays();
                recreate |= m_displayCount != nDisplays;
                m_displayCount = nDisplays;
            }

            if (recreate)
            {
                var displayList = new IntPtr[m_displayCount];
                for (var i = 0; i < m_displayCount; ++i)
                {
                    displayList[i] = new IntPtr(i);
                }

                Display.RecreateDisplayList(displayList);
            }
        }

        internal static GUIContent[] GetDisplayNamesForBuildTarget(BuildTarget buildTarget)
        {
            var platformDisplayNames = Modules.ModuleManager.GetDisplayNames(buildTarget.ToString());
            return platformDisplayNames ?? DisplayUtility.GetGenericDisplayNames();
        }

        private static void GetSystemExtImpl(IntPtr nativeDisplay, out int w, out int h)
        {
            var manager = instance;
            if (manager.m_mode == DisplayAPIControlMode.FromEditor)
            {
                var view = manager.m_views[(int)nativeDisplay];

                if (view == null)
                {
                    w = 0;
                    h = 0;
                }
                else
                {
                    w = (int)view.position.width;
                    h = (int)view.position.height;
                }
            }
            else
            {
                w = (int)EditorDisplayUtility.GetDisplayWidth((int)nativeDisplay);
                h = (int)EditorDisplayUtility.GetDisplayHeight((int)nativeDisplay);
            }
        }

        private static void GetRenderingExtImpl(IntPtr nativeDisplay, out int w, out int h)
        {
            var manager = instance;
            if (manager.m_mode == DisplayAPIControlMode.FromEditor)
            {
                var view = manager.m_views[(int)nativeDisplay];

                if (view == null)
                {
                    w = 0;
                    h = 0;
                }
                else
                {
                    w = (int)view.targetSize.x;
                    h = (int)view.targetSize.y;
                }
            }
            else
            {
                w = (int)EditorDisplayUtility.GetDisplayWidth((int)nativeDisplay);
                h = (int)EditorDisplayUtility.GetDisplayHeight((int)nativeDisplay);
            }
        }

        private static void GetRenderingBuffersImpl(IntPtr nativeDisplay, out RenderBuffer color,
            out RenderBuffer depth)
        {
            color = new RenderBuffer();
            depth = new RenderBuffer();
        }

        private static void SetRenderingResolutionImpl(IntPtr nativeDisplay, int w, int h)
        {
            var manager = instance;
            if (manager.m_mode == DisplayAPIControlMode.FromEditor)
            {
                var view = manager.m_views[(int)nativeDisplay];
                if (view != null) {
                    view.SetPlayModeViewSize(new Vector2(w, h));
                }
            }
        }

        private static void ActivateDisplayImpl(IntPtr nativeDisplay, int width, int height, int refreshRate)
        {
            var manager = instance;
            if (manager.m_mode == DisplayAPIControlMode.FromRuntime)
            {
                EditorFullscreenController.BeginFullscreen((int)nativeDisplay, width, height);
            }
        }

        private static void SetParamsImpl(IntPtr nativeDisplay, int width, int height, int x, int y)
        {
            // do nothing.
        }

        private static int RelativeMouseAtImpl(int x, int y, out int rx, out int ry)
        {
            // TODO, unused?
            rx = 0;
            ry = 0;
            return 0;
        }

        private static bool GetActiveImpl(IntPtr nativeDisplay)
        {
            var view = instance.m_views[(int)nativeDisplay];
            return view != null;
        }

        private static bool RequiresBlitToBackbufferImpl(IntPtr nativeDisplay)
        {
            return false;
        }

        private static bool RequiresSrgbBlitToBackbufferImpl(IntPtr nativeDisplay)
        {
            return false;
        }
    }
} // namespace
