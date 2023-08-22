// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/RenderDoc/RenderDoc.h")]
    [StaticAccessor("RenderDoc", StaticAccessorType.DoubleColon)]
    public static partial class RenderDoc
    {
        [WindowAction]
        static WindowAction RenderDocGlobalAction()
        {
            // Developer-mode render doc button to enable capturing any HostView content/panels
            var action = WindowAction.CreateWindowActionButton("RenderDoc", CaptureRenderDoc, null, ContainerWindow.kButtonWidth + 1, RenderDocCaptureButton);
            action.validateHandler = ShowRenderDocButton;
            return action;
        }

        public static extern bool IsInstalled();
        public static extern bool IsLoaded();
        public static extern bool IsSupported();
        public static extern void Load();

        public static void BeginCaptureRenderDoc(EditorWindow window)
            => window.m_Parent.BeginCaptureRenderDoc();
        public static void EndCaptureRenderDoc(EditorWindow window)
            => window.m_Parent.EndCaptureRenderDoc();

        static EditorWindow s_EditorWindowScheduledForCapture = null;
        static GUIContent s_RenderDocContent;
        internal static bool RenderDocCaptureButton(EditorWindow view, WindowAction self, Rect r)
        {
            if (s_RenderDocContent == null)
                s_RenderDocContent = EditorGUIUtility.TrIconContent("FrameCapture", RenderDocUtil.openInRenderDocTooltip);

            Rect r2 = new Rect(r.xMax - r.width, r.y, r.width, r.height);
            return GUI.Button(r2, s_RenderDocContent, EditorStyles.iconButton);
        }

        private static void CaptureRenderDoc(EditorWindow view, WindowAction self)
        {
            if (view is GameView)
                view.m_Parent.CaptureRenderDocScene();
            else
                view.m_Parent.CaptureRenderDocFullContent();
        }

        private static bool ShowRenderDocButton(EditorWindow view, WindowAction self)
        {
            return Unsupported.IsDeveloperMode() && IsLoaded() && IsSupported();
        }

        internal static void LoadRenderDoc()
        {
            if (IsInstalled() && !IsLoaded() && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                ShaderUtil.RequestLoadRenderDoc();
            }
        }

        [Shortcut(RenderDocUtil.captureRenderDocShortcutID, KeyCode.C, ShortcutModifiers.Alt)]
        internal static void CaptureRenderDoc()
        {
            if (IsInstalled() && !IsLoaded())
            {
                s_EditorWindowScheduledForCapture = EditorWindow.focusedWindow;
                LoadRenderDoc();
            }
            else if (EditorWindow.focusedWindow != null)
            {
                CaptureRenderDoc(EditorWindow.focusedWindow, null);
            }
        }

        [RequiredByNativeCode]
        static void RenderDocLoaded()
        {
            if (s_EditorWindowScheduledForCapture != null)
            {
                s_EditorWindowScheduledForCapture.Focus();
                CaptureRenderDoc();
                s_EditorWindowScheduledForCapture = null;
            }
        }
    }
}
