// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

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
            var action = WindowAction.CreateWindowActionButton("RenderDoc", CaptureRenderDocFullContent, null, ContainerWindow.kButtonWidth + 1, RenderDocCaptureButton);
            action.validateHandler = ShowRenderDocButton;
            return action;
        }

        public static extern bool IsInstalled();
        public static extern bool IsLoaded();
        public static extern bool IsSupported();
        public static extern void Load();

        public static void BeginCaptureRenderDoc(UnityEditor.EditorWindow window)
            => window.m_Parent.BeginCaptureRenderDoc();
        public static void EndCaptureRenderDoc(UnityEditor.EditorWindow window)
            => window.m_Parent.EndCaptureRenderDoc();


        static GUIContent s_RenderDocContent;
        internal static bool RenderDocCaptureButton(EditorWindow view, WindowAction self, Rect r)
        {
            if (s_RenderDocContent == null)
                s_RenderDocContent = EditorGUIUtility.TrIconContent("FrameCapture", UnityEditor.RenderDocUtil.openInRenderDocLabel);

            Rect r2 = new Rect(r.xMax - r.width, r.y, r.width, r.height);
            return GUI.Button(r2, s_RenderDocContent, EditorStyles.iconButton);
        }

        private static void CaptureRenderDocFullContent(EditorWindow view, WindowAction self)
        {
            view.m_Parent.CaptureRenderDocFullContent();
        }

        private static bool ShowRenderDocButton(EditorWindow view, WindowAction self)
        {
            return Unsupported.IsDeveloperMode() && IsLoaded() && IsSupported();
        }
    }
}
