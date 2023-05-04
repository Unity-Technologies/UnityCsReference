// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal static class PreviewEditorWindow
    {
        internal static void RepaintAll()
        {
            PlayModeView.RepaintAll();
        }
    }

    [Serializable]
    internal abstract class PlayModeView : EditorWindow
    {
        static List<PlayModeView> s_PlayModeViews = new List<PlayModeView>();

        private static PlayModeView s_LastFocused;

        static PlayModeView s_RenderingView;

        private readonly string m_ViewsCache = Path.GetFullPath(Directory.GetCurrentDirectory() + "/Library/PlayModeViewStates/");
        private static readonly string m_OpenGameViewOnPlay = "OpenGameViewOnEnteringPlayMode";

        [SerializeField] private List<string> m_SerializedViewNames = new List<string>();
        [SerializeField] private List<string> m_SerializedViewValues = new List<string>();
        [SerializeField] string m_PlayModeViewName;
        [SerializeField] bool m_ShowGizmos;
        [SerializeField] int m_TargetDisplay;
        [SerializeField] Color m_ClearColor;
        [SerializeField] Vector2 m_TargetSize;
        [SerializeField] FilterMode m_TextureFilterMode = FilterMode.Point;
        [SerializeField] HideFlags m_TextureHideFlags = HideFlags.HideAndDontSave;
        [SerializeField] bool m_RenderIMGUI;
        [SerializeField] EnterPlayModeBehavior m_EnterPlayModeBehavior;
        [SerializeField] bool m_UseMipMap;

        private const int k_MaxSupportedDisplays = 8;

        static Dictionary<Type, string> s_AvailableWindowTypes;

        internal Vector2 viewPadding
        {
            get;
            private protected set;
        }

        internal float viewMouseScale
        {
            get;
            private protected set;
        }

        protected string playModeViewName
        {
            get { return m_PlayModeViewName; }
            set { m_PlayModeViewName = value; }
        }

        protected bool showGizmos
        {
            get { return m_ShowGizmos; }
            set
            {
                m_ShowGizmos = value;
            }
        }

        public  int targetDisplay
        {
            get { return m_TargetDisplay; }
            protected set
            {
                if (m_TargetDisplay != value)
                    SetDisplayViewSize(value, m_TargetSize);

                m_TargetDisplay = value;
            }
        }

        protected Color clearColor
        {
            get { return m_ClearColor; }
            set { m_ClearColor = value; }
        }

        internal Vector2 targetSize
        {
            get { return m_TargetSize; }
            set
            {
                if (this == GetMainPlayModeView())
                    SetMainPlayModeViewSize(value);

                if (m_TargetSize != value)
                    SetDisplayViewSize(m_TargetDisplay, value);

                m_TargetSize = value;
            }
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

        public EnterPlayModeBehavior enterPlayModeBehavior
        {
            get => m_EnterPlayModeBehavior;
            set => SetPlayModeWindowsStates(value);
        }

        public const int kFullscreenInvalidIdx = -1;
        public const int kFullscreenNone = 0;

        [Obsolete("PlayModeView.maximizeOnPlay is obsolete. Use PlayModeView.enterPlayModeBehavior instead")]
        public bool maximizeOnPlay
        {
            get { return m_EnterPlayModeBehavior == EnterPlayModeBehavior.PlayMaximized; }
            set { m_EnterPlayModeBehavior = value ? EnterPlayModeBehavior.PlayMaximized : EnterPlayModeBehavior.PlayFocused; }
        }

        protected bool useMipMap
        {
            get { return m_UseMipMap; }
            set { m_UseMipMap = value; }
        }

        public static bool openWindowOnEnteringPlayMode
        {
            get => EditorPrefs.GetBool(m_OpenGameViewOnPlay, true);
            set
            {
                if (EditorPrefs.GetBool(m_OpenGameViewOnPlay) != value)
                {
                    EditorPrefs.SetBool(m_OpenGameViewOnPlay, value);
                    RepaintAll();
                }
            }
        }

        RenderTexture m_TargetTexture;
        ColorSpace m_CurrentColorSpace = ColorSpace.Uninitialized;

        class RenderingView : IDisposable
        {
            bool disposed = false;

            public RenderingView(PlayModeView playModeView)
            {
                PlayModeView.s_RenderingView = playModeView;
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
                    PlayModeView.s_RenderingView = null;
                }

                disposed = true;
            }
        }

        protected PlayModeView()
        {
            RegisterWindow();
            SetPlayModeView(true);
            SetDisplayViewSize(m_TargetDisplay, m_TargetSize);
        }

        protected RenderTexture RenderView(Vector2 mousePosition, bool clearTexture)
        {
            using (var renderingView = new RenderingView(this))
            {
                SetPlayModeViewSize(targetSize);

                // The target size and GUI rects are communicated to C++ as floats throughout the engine. This means the final size in pixels
                // of the view will be calculated in C++ so we have to ensure we use the C++ float rounding conventions here. Failure to do so
                // may result in subtly differences and subtle bugs (like the Screen class thinking something sized 640.5 is 641 pixels but the
                // RenderTexture being allocated here only being 640 pixels in size with standard C# rounding used by Mathf.RoundToInt.
                // So we use the c++ AwayFromZero convention when rounding here.
                int width = (int)Math.Round(targetSize.x, MidpointRounding.AwayFromZero);
                int height = (int)Math.Round(targetSize.y, MidpointRounding.AwayFromZero);

                var currentTargetDisplay = 0;
                if (ModuleManager.ShouldShowMultiDisplayOption())
                {
                    // Display Targets can have valid targets from 0 to 7.
                    System.Diagnostics.Debug.Assert(targetDisplay < k_MaxSupportedDisplays, "Display Target is Out of Range");
                    currentTargetDisplay = targetDisplay;
                }

                bool hdr = (m_Parent != null && m_Parent.actualView == this && m_Parent.hdrActive);
                ConfigureTargetTexture(width, height, clearTexture, playModeViewName, hdr);

                if (Event.current == null || Event.current.type != EventType.Repaint)
                    return m_TargetTexture;

                Vector2 oldOffset = GUIUtility.s_EditorScreenPointOffset;
                GUIUtility.s_EditorScreenPointOffset = Vector2.zero;
                SavedGUIState oldState = SavedGUIState.Create();

                if (m_TargetTexture.IsCreated())
                    EditorGUIUtility.RenderPlayModeViewCamerasInternal(m_TargetTexture, currentTargetDisplay, mousePosition, showGizmos, renderIMGUI);

                oldState.ApplyAndForget();
                GUIUtility.s_EditorScreenPointOffset = oldOffset;

                return m_TargetTexture;
            }
        }

        protected static string GetWindowTitle(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(EditorWindowTitleAttribute), true);
            return attributes.Length > 0 ? ((EditorWindowTitleAttribute)attributes[0]).title : type.Name;
        }

        internal static Dictionary<Type, string> GetAvailableWindowTypes()
        {
            return s_AvailableWindowTypes ?? (s_AvailableWindowTypes = TypeCache.GetTypesDerivedFrom(typeof(PlayModeView)).OrderBy(GetWindowTitle).ToDictionary(t => t, GetWindowTitle));
        }

        private void SetSerializedViews(Dictionary<string, string> serializedViews)
        {
            m_SerializedViewNames = serializedViews.Keys.ToList();
            m_SerializedViewValues = serializedViews.Values.ToList();
        }

        private string GetTypeName()
        {
            return GetType().ToString();
        }

        private Dictionary<string, string> ListsToDictionary(List<string> keys, List<string> values)
        {
            var dict = keys.Select((key, val) => new { key, val = values[val] }).ToDictionary(x => x.key, x => x.val);
            return dict;
        }

        protected internal void SwapMainWindow(Type type)
        {
            if (type.BaseType != typeof(PlayModeView))
                throw new ArgumentException("Type should derive from " + typeof(PlayModeView).Name);
            if (type.Name != GetType().Name)
            {
                var serializedViews = ListsToDictionary(m_SerializedViewNames, m_SerializedViewValues);

                // Clear serialized views so they wouldn't be serialized again
                m_SerializedViewNames.Clear();
                m_SerializedViewValues.Clear();

                var guid = GUID.Generate();
                var serializedViewPath = Path.GetFullPath(Path.Combine(m_ViewsCache, guid.ToString()));
                if (!Directory.Exists(m_ViewsCache))
                    Directory.CreateDirectory(m_ViewsCache);

                InternalEditorUtility.SaveToSerializedFileAndForget(new[] {this}, serializedViewPath, true);
                serializedViews.Add(GetTypeName(), serializedViewPath);

                PlayModeView window = null;
                if (serializedViews.ContainsKey(type.ToString()))
                {
                    var path = serializedViews[type.ToString()];
                    serializedViews.Remove(type.ToString());
                    if (File.Exists(path))
                    {
                        window = InternalEditorUtility.LoadSerializedFileAndForget(path)[0] as PlayModeView;
                        File.Delete(path);
                    }
                }

                if (!window)
                    window = CreateInstance(type) as PlayModeView;

                window.autoRepaintOnSceneChange = true;

                window.SetSerializedViews(serializedViews);

                if (m_Parent is DockArea dockAreaParent)
                {
                    dockAreaParent.AddTab(window);
                    dockAreaParent.RemoveTab(this);
                    DestroyImmediate(this, true);
                }
                else if (m_Parent is MaximizedHostView maximizedParent)
                {
                    maximizedParent.actualView = window;
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

        private void ConfigureTargetTexture(int width, int height, bool clearTexture, string name, bool hdr)
        {
            // make sure we actually support R16G16B16A16_SFloat
            GraphicsFormat format = (hdr && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render)) ? GraphicsFormat.R16G16B16A16_SFloat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

            // Requires destroying the entire RT object and recreating it if
            // 1. color space is changed;
            // 2. using mipmap is changed.
            // 3. HDR backbuffer mode for the view has changed

            if (m_TargetTexture && (m_CurrentColorSpace != QualitySettings.activeColorSpace || m_TargetTexture.useMipMap != m_UseMipMap || m_TargetTexture.graphicsFormat != format))
            {
                UnityEngine.Object.DestroyImmediate(m_TargetTexture);
            }
            if (!m_TargetTexture)
            {
                m_CurrentColorSpace = QualitySettings.activeColorSpace;
                m_TargetTexture = new RenderTexture(0, 0, format, SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil));
                m_TargetTexture.name = name + " RT";
                m_TargetTexture.filterMode = textureFilterMode;
                m_TargetTexture.hideFlags = textureHideFlags;
                m_TargetTexture.useMipMap = useMipMap;
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

        internal static PlayModeView GetRenderingView()
        {
            return s_RenderingView;
        }

        internal static PlayModeView GetMainPlayModeView()
        {
            if (s_LastFocused == null && s_PlayModeViews != null)
            {
                RemoveDisabledWindows();
                if (s_PlayModeViews.Count > 0)
                    s_LastFocused = s_PlayModeViews[0];
            }

            return s_LastFocused;
        }

        internal static List<PlayModeView> GetAllPlayModeViewWindows()
        {
            return s_PlayModeViews;
        }

        internal static PlayModeView GetCorrectPlayModeViewToFocus()
        {
            if (s_PlayModeViews != null)
            {
                RemoveDisabledWindows();
                foreach (var view in s_PlayModeViews)
                {
                    if (view.enterPlayModeBehavior == EnterPlayModeBehavior.PlayFocused || view.enterPlayModeBehavior == EnterPlayModeBehavior.PlayMaximized)
                    {
                        s_LastFocused = view;
                        return view;
                    }
                }
            }
            return GetMainPlayModeView();
        }

        internal static PlayModeView GetLastFocusedPlayModeView()
        {
            return s_LastFocused;
        }

        private static void RemoveDisabledWindows()
        {
            if (s_PlayModeViews == null)
                return;
            s_PlayModeViews.RemoveAll(window => window == null);
        }

        internal static Vector2 GetMainPlayModeViewTargetSize()
        {
            var prevWindow = GetMainPlayModeView();
            if (prevWindow)
                return prevWindow.GetPlayModeViewSize();
            return new Vector2(640f, 480f);
        }

        internal Vector2 GetPlayModeViewSize()
        {
            return targetSize;
        }

        private void RegisterWindow()
        {
            RemoveDisabledWindows();
            if (!s_PlayModeViews.Contains(this))
            {
                s_PlayModeViews.Add(this);
                m_EnterPlayModeBehavior = s_PlayModeViews.Count == 1 ? EnterPlayModeBehavior.PlayFocused : EnterPlayModeBehavior.PlayUnfocused;
            }
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
                m_Parent.SetMainPlayModeViewSize(targetSize);
                Display.activeEditorGameViewTarget  = m_TargetDisplay;
                s_LastFocused = this;
                // AddLastView(s_LastFocused);
                Repaint();
            }
            SetDisplayViewSize(m_TargetDisplay, m_TargetSize);
        }

        [RequiredByNativeCode]
        internal static void IsPlayModeViewOpen(out bool isPlayModeViewOpen)
        {
            isPlayModeViewOpen = GetMainPlayModeView() != null;
        }

        internal static void RepaintAll()
        {
            if (s_PlayModeViews == null)
                return;

            foreach (PlayModeView playModeView in s_PlayModeViews)
                playModeView.Repaint();
        }

        public enum EnterPlayModeBehavior
        {
            PlayFocused,
            PlayMaximized,
            PlayUnfocused
        }

        void SetPlayModeWindowsStates(EnterPlayModeBehavior behavior)
        {
            this.m_EnterPlayModeBehavior = behavior;
            foreach (var view in s_PlayModeViews)
            {
                if (view == this)
                {
                    continue;
                }
                if (behavior == EnterPlayModeBehavior.PlayMaximized && view.m_EnterPlayModeBehavior == EnterPlayModeBehavior.PlayMaximized)
                {
                    // Only one play mode view can be maximized at a time.
                    view.m_EnterPlayModeBehavior = EnterPlayModeBehavior.PlayUnfocused;
                }
                view.OnEnterPlayModeBehaviorChange();
                view.Repaint();
            }
        }

        protected virtual void OnEnterPlayModeBehaviorChange() {}
    }
}
