// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor
{
    internal partial class ExternalPlayModeView : EditorWindow, IGameViewSizeMenuUser
    {
        MonoReloadableIntPtr m_NativeContextPtr;

        public string playerLaunchPath
        {
            get { return m_PlayerLaunchPath; }
            set
            {
                m_PlayerLaunchPath = value;
                Internal_SetPlayerLaunchPath(m_NativeContextPtr.m_IntPtr, m_PlayerLaunchPath);
            }
        }

        string m_PlayerLaunchPath;

        static GameViewSizeGroupType currentSizeGroupType => GameViewSizes.instance.currentGroupType;

        private ExternalPlayModeView() {}

        public static ExternalPlayModeView CreateExternalPlayModeView()
        {
            ExternalPlayModeView win = EditorWindow.CreateWindow<ExternalPlayModeView>();
            win.m_NativeContextPtr.m_IntPtr = Internal_InitWindow();
            return win;
        }

        public void AttachProcess()
        {
            AttachWindow_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle, GetTabRect());
        }

        public void KillProcess()
        {
            DestroyWindow_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle);
        }

        /*
         * Returns the Rect that the external process should render itself in. Must be inside the external playmode view window
         * and below any controls on the window.
         */
        private Rect GetTabRect()
        {
            Rect hostWindowScreenPosition = m_Parent.window.position;
            Rect inTabPosition = m_Parent.windowPosition;
            Rect usePosition = new Rect(hostWindowScreenPosition);

            usePosition.x += inTabPosition.x;
            usePosition.y += inTabPosition.y;
            usePosition.width = inTabPosition.width;
            usePosition.height = inTabPosition.height;

            float yAdjust = EditorGUI.kTabButtonHeight + EditorGUI.kWindowToolbarHeight + EditorGUI.kSpacing;
            float borderSize = EditorGUI.kSpacing;
            usePosition.y += yAdjust;
            usePosition.height -= (yAdjust + borderSize);
            usePosition.x += borderSize;
            usePosition.width -= borderSize * 2;

            if (m_Parent.window.IsMainWindow())
            {
                // Need to move farther down when docked with the main editor window. Why?
                float adjust = EditorGUI.kTabButtonHeight + EditorGUI.kSpacing;
                usePosition.y += adjust;
                usePosition.height -= adjust * 2;
            }

            return usePosition;
        }

        private void DoToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.Label(m_PlayerLaunchPath);
                EditorGUILayout.GameViewSizePopup(currentSizeGroupType, 0, this, EditorStyles.toolbarPopup, GUILayout.Width(160f));
            }
            GUILayout.EndHorizontal();
        }

        private void OnGUI()
        {
            DoToolbarGUI();
        }

        internal override void OnResized()
        {
            if (m_Parent.window == null)
                return;

            ResizeWindow_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle, GetTabRect());
        }

        /*
         * We will receive this callback when we're docked inside the main editor window
         * and the editor is moved via the native title bar. Managed normally doesn't care
         * about these movements, but on Linux, we need to sync the externally hosted process
         * window with the main window's location. This is not done automatically because we need to keep
         * the external process window as a top-level window in the hierarchy so that the socket doesn't get
         * unrealized and our external player is destroyed when we move tabs or destroy it's container window.
         */
        private void OnMainWindowMove()
        {
            ResizeWindow_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle, GetTabRect());
        }

        private void OnDestroy()
        {
            DestroyWindow_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle);
        }

        private void OnBecameInvisible()
        {
            if (m_Parent.window == null)
                return;
            OnBecameInvisible_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle);
        }

        private void OnBecameVisible()
        {
            if (m_Parent.window == null)
                return;
            OnBecameVisible_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle, GetTabRect());
        }

        private void OnTabNewWindow()
        {
            if (m_Parent.window == null)
                return;
            AddedAsTab_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle, GetTabRect());
        }

        private void OnAddedAsTab()
        {
            if (m_Parent.window == null)
                return;
            AddedAsTab_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle, GetTabRect());
        }

        private void OnBeforeRemovedAsTab()
        {
            if (m_Parent.window == null)
                return;
            BeforeRemoveTab_Native(m_NativeContextPtr.m_IntPtr, m_Parent.nativeHandle);
        }

        [ExcludeFromDocs]
        public void SizeSelectionCallback(int indexClicked, object objectSelected)
        {
            //TODO handle selection of aspect ratio.
            // We need to pass a message to our hosted process and tell it to change to a compatible resolution based on our selection.
        }

        [ExcludeFromDocs]
        public bool lowResolutionForAspectRatios
        {
            get { return true; }
            set {}
        }

        [ExcludeFromDocs]
        public bool forceLowResolutionAspectRatios
        {
            get { return false; }
        }

        [ExcludeFromDocs]
        public bool vSyncEnabled
        {
            get { return false; }
            set {}
        }
    }
}
