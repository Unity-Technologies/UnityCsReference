// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [Serializable]
    internal abstract class PreviewEditorWindow : EditorWindow
    {
        static List<PreviewEditorWindow> s_PreviewWindows = new List<PreviewEditorWindow>();
        static PreviewEditorWindow s_LastFocused;
        static PreviewEditorWindow s_RenderingPreview;

        [SerializeField] string m_PreviewName;
        [SerializeField] bool m_ShowGizmos;
        [SerializeField] int m_TargetDisplay;
        [SerializeField] Color m_ClearColor;
        [SerializeField] Vector2 m_TargetSize;
        [SerializeField] FilterMode m_TextureFilterMode = FilterMode.Point;
        [SerializeField] HideFlags m_TextureHideFlags = HideFlags.HideAndDontSave;
        [SerializeField] bool m_RenderIMGUI;
        [SerializeField] bool m_MaximizeOnPlay;

        private Dictionary<Type, string> m_AvailableWindowTypes;

        protected string previewName
        {
            get { return m_PreviewName; }
            set { m_PreviewName = value; }
        }

        protected bool showGizmos
        {
            get { return m_ShowGizmos; }
            set
            {
                m_ShowGizmos = value;
            }
        }

        protected int targetDisplay
        {
            get { return m_TargetDisplay; }
            set { m_TargetDisplay = value; }
        }

        protected Color clearColor
        {
            get { return m_ClearColor; }
            set { m_ClearColor = value; }
        }

        protected Vector2 targetSize
        {
            get { return m_TargetSize; }
            set { m_TargetSize = value; }
        }

        protected FilterMode textureFilterMode
        {
            get { return m_TextureFilterMode; }
            set { m_TextureFilterMode = value; }
        }

        protected HideFlags textureHideFlags
        {
            get { return m_TextureHideFlags; }
            set { m_TextureHideFlags = value; }
        }

        protected bool renderIMGUI
        {
            get { return m_RenderIMGUI; }
            set { m_RenderIMGUI = value; }
        }

        public bool maximizeOnPlay
        {
            get { return m_MaximizeOnPlay; }
            set { m_MaximizeOnPlay = value; }
        }

        RenderTexture m_TargetTexture;
        ColorSpace m_CurrentColorSpace = ColorSpace.Uninitialized;

        class RenderingPreview : IDisposable
        {
            bool disposed = false;

            public RenderingPreview(PreviewEditorWindow previewWindow)
            {
                PreviewEditorWindow.s_RenderingPreview = previewWindow;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            // Protected implementation of Dispose pattern.
            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                    PreviewEditorWindow.s_RenderingPreview = null;
                }

                disposed = true;
            }
        }

        protected PreviewEditorWindow()
        {
            RegisterWindow();
            SetPlayModeView();
        }

        protected RenderTexture RenderPreview(Vector2 mousePosition, bool clearTexture)
        {
            using (var renderingPreview = new RenderingPreview(this))
            {
                SetPlayModeViewSize(targetSize);
                var currentTargetDisplay = 0;
                if (ModuleManager.ShouldShowMultiDisplayOption())
                {
                    // Display Targets can have valid targets from 0 to 7.
                    System.Diagnostics.Debug.Assert(targetDisplay < 8, "Display Target is Out of Range");
                    currentTargetDisplay = targetDisplay;
                }

                ConfigureTargetTexture((int)targetSize.x, (int)targetSize.y, clearTexture, previewName);

                if (Event.current == null || Event.current.type != EventType.Repaint)
                    return m_TargetTexture;

                Vector2 oldOffset = GUIUtility.s_EditorScreenPointOffset;
                GUIUtility.s_EditorScreenPointOffset = Vector2.zero;
                SavedGUIState oldState = SavedGUIState.Create();

                EditorGUIUtility.RenderPreviewCamerasInternal(m_TargetTexture, currentTargetDisplay, mousePosition, showGizmos, renderIMGUI);

                oldState.ApplyAndForget();
                GUIUtility.s_EditorScreenPointOffset = oldOffset;

                return m_TargetTexture;
            }
        }

        protected string GetWindowTitle(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(EditorWindowTitleAttribute), true);
            return attributes.Length > 0 ? ((EditorWindowTitleAttribute)attributes[0]).title : type.Name;
        }

        protected Dictionary<Type, string> GetAvailableWindowTypes()
        {
            return m_AvailableWindowTypes ?? (m_AvailableWindowTypes = TypeCache.GetTypesDerivedFrom(typeof(PreviewEditorWindow)).OrderBy(GetWindowTitle).ToDictionary(t => t, GetWindowTitle));
        }

        protected void SwapMainWindow(Type type)
        {
            if (type.BaseType != typeof(PreviewEditorWindow))
                throw new ArgumentException("Type should derive from " + typeof(PreviewEditorWindow).Name);

            if (type.Name != GetType().Name)
            {
                var window = CreateInstance(type) as PreviewEditorWindow;
                window.autoRepaintOnSceneChange = true;
                var da = m_Parent as DockArea;
                if (da)
                {
                    da.AddTab(window);
                    da.RemoveTab(this);
                    DestroyImmediate(this, true);
                }
            }
        }

        private void ClearTargetTexture()
        {
            if (m_TargetTexture.IsCreated())
            {
                var previousTarget = RenderTexture.active;
                RenderTexture.active = m_TargetTexture;
                GL.Clear(true, true, clearColor);
                RenderTexture.active = previousTarget;
            }
        }

        private void ConfigureTargetTexture(int width, int height, bool clearTexture, string name)
        {
            // Changing color space requires destroying the entire RT object and recreating it
            if (m_TargetTexture && m_CurrentColorSpace != QualitySettings.activeColorSpace)
            {
                UnityEngine.Object.DestroyImmediate(m_TargetTexture);
            }
            if (!m_TargetTexture)
            {
                m_CurrentColorSpace = QualitySettings.activeColorSpace;
                m_TargetTexture = new RenderTexture(0, 0, 24, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
                m_TargetTexture.name = name + " RT";
                m_TargetTexture.filterMode = textureFilterMode;
                m_TargetTexture.hideFlags = textureHideFlags;
            }

            // Changes to these attributes require a release of the texture
            if (m_TargetTexture.width != width || m_TargetTexture.height != height)
            {
                m_TargetTexture.Release();
                m_TargetTexture.width = width;
                m_TargetTexture.height = height;
                m_TargetTexture.antiAliasing = 1;
                clearTexture = true;
            }

            m_TargetTexture.Create();

            if (clearTexture)
            {
                ClearTargetTexture();
            }
        }

        internal static PreviewEditorWindow GetRenderingPreview()
        {
            return s_RenderingPreview;
        }

        internal static PreviewEditorWindow GetMainPreviewWindow()
        {
            if (s_LastFocused == null && s_PreviewWindows != null)
            {
                RemoveDisabledWindows();
                if (s_PreviewWindows.Count > 0)
                    s_LastFocused = s_PreviewWindows[0];
            }

            return s_LastFocused;
        }

        private static void RemoveDisabledWindows()
        {
            if (s_PreviewWindows == null)
                return;

            s_PreviewWindows.RemoveAll(window => window == null);
        }

        internal static Vector2 GetMainPreviewTargetSize()
        {
            var prevWindow = GetMainPreviewWindow();
            if (prevWindow)
                return prevWindow.GetPreviewSize();
            return new Vector2(640f, 480f);
        }

        internal Vector2 GetPreviewSize()
        {
            return targetSize;
        }

        private void RegisterWindow()
        {
            RemoveDisabledWindows();
            if (!s_PreviewWindows.Contains(this))
                s_PreviewWindows.Add(this);
        }

        public bool IsShowingGizmos()
        {
            return showGizmos;
        }

        public void SetShowGizmos(bool value)
        {
            showGizmos = value;
            Repaint();
        }

        protected void SetVSync(bool enable)
        {
            m_Parent.EnableVSync(enable);
        }

        protected void SetFocus(bool focused)
        {
            if (!focused && s_LastFocused == this)
            {
                InternalEditorUtility.OnGameViewFocus(false);
            }
            else if (focused)
            {
                InternalEditorUtility.OnGameViewFocus(true);
                m_Parent.SetAsLastPlayModeView();
                s_LastFocused = this;
                Repaint();
            }
        }

        internal static bool IsPreviewWindowOpen()
        {
            return GetMainPreviewWindow() != null;
        }

        internal static void RepaintAll()
        {
            if (s_PreviewWindows == null)
                return;

            foreach (PreviewEditorWindow previewWindow in s_PreviewWindows)
                previewWindow.Repaint();
        }

        [RequiredByNativeCode]
        private static void GetMainPreviewTargetSizeNoBox(out Vector2 result)
        {
            result = GetMainPreviewTargetSize();
        }
    }
}
