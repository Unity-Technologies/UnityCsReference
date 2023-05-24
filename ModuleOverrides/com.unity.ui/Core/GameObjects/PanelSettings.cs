// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Options that specify how elements in the panel scale when the screen size changes. See <see cref="PanelSettings.scaleMode"/>.
    /// </summary>
    public enum PanelScaleMode
    {
        /// <summary>
        /// Elements stay the same size, in pixels, regardless of screen size.
        /// </summary>
        ConstantPixelSize,
        /// <summary>
        /// Elements stay the same physical size (displayed size) regardless of screen size and resolution.
        /// </summary>
        ConstantPhysicalSize,
        /// <summary>
        /// Elements get bigger when the screen size increases, and smaller when it decreases.
        /// </summary>
        ScaleWithScreenSize
    }

    /// <summary>
    /// Options that specify how to scale the panel area when the aspect ratio of the current screen resolution
    /// does not match the reference resolution. See <see cref="PanelSettings.screenMatchMode"/>.
    /// </summary>
    public enum PanelScreenMatchMode
    {
        /// <summary>
        /// Scales the panel area using width, height, or a mix of the two as a reference.
        /// </summary>
        MatchWidthOrHeight,
        /// <summary>
        /// Crops the panel area horizontally or vertically so the panel size never exceeds
        /// the reference resolution.
        /// </summary>
        Shrink,
        /// <summary>
        /// Expand the panel area horizontally or vertically so the panel size is never
        /// smaller than the reference resolution.
        /// </summary>
        Expand
    }

    internal enum PanelRenderMode
    {
        ScreenSpaceOverlay,
        WorldSpace
    }

    /// <summary>
    /// Defines a Panel Settings asset that instantiates a panel at runtime. The panel makes it possible for Unity to display
    /// UXML-file based UI in the Game view.
    /// </summary>
    public class PanelSettings : ScriptableObject
    {
        private const int k_DefaultSortingOrder = 0;

        private const float k_DefaultScaleValue = 1.0f;

        internal const string k_DefaultStyleSheetPath =
            "Packages/com.unity.ui/PackageResources/StyleSheets/Generated/Default.tss.asset";

        private class RuntimePanelAccess
        {
            private readonly PanelSettings m_Settings;

            internal RuntimePanelAccess(PanelSettings settings)
            {
                m_Settings = settings;
            }

            private BaseRuntimePanel m_RuntimePanel;

            internal bool isInitialized => m_RuntimePanel != null;

            /// <summary>
            /// Internal, typed access to the Panel used to draw UI of type Player.
            /// </summary>
            internal BaseRuntimePanel panel
            {
                get
                {
                    if (m_RuntimePanel == null)
                    {
                        m_RuntimePanel = CreateRelatedRuntimePanel();
                        m_RuntimePanel.sortingPriority = m_Settings.m_SortingOrder;
                        m_RuntimePanel.targetDisplay = m_Settings.m_TargetDisplay;
                        m_RuntimePanel.panelChangeReceiver = m_Settings.panelChangeReceiver;

                        var root = m_RuntimePanel.visualTree;
                        root.name = m_Settings.name;

                        m_Settings.ApplyThemeStyleSheet(root);

                        if (m_Settings.m_TargetTexture != null)
                        {
                            m_RuntimePanel.targetTexture = m_Settings.m_TargetTexture;
                        }

                        if (m_Settings.m_AssignedScreenToPanel != null)
                        {
                            m_Settings.SetScreenToPanelSpaceFunction(m_Settings.m_AssignedScreenToPanel);
                        }
                    }

                    return m_RuntimePanel;
                }
            }

            internal void DisposePanel()
            {
                if (m_RuntimePanel != null)
                {
                    DisposeRelatedPanel();
                    m_RuntimePanel = null;
                }
            }

            internal void SetTargetTexture()
            {
                if (m_RuntimePanel != null)
                {
                    m_RuntimePanel.targetTexture = m_Settings.targetTexture;
                }
            }

            internal void SetSortingPriority()
            {
                if (m_RuntimePanel != null)
                {
                    m_RuntimePanel.sortingPriority = m_Settings.m_SortingOrder;
                }
            }

            internal void SetTargetDisplay()
            {
                if (m_RuntimePanel != null)
                {
                    m_RuntimePanel.targetDisplay =  m_Settings.m_TargetDisplay;
                }
            }

            internal void SetPanelChangeReceiver()
            {
                if (m_RuntimePanel != null)
                {
                    m_RuntimePanel.panelChangeReceiver = m_Settings.m_PanelChangeReceiver;
                }
            }

            private BaseRuntimePanel CreateRelatedRuntimePanel()
            {
                var newPanel = (RuntimePanel)UIElementsRuntimeUtility.FindOrCreateRuntimePanel(m_Settings, RuntimePanel.Create);
                CreateRuntimePanelDebug?.Invoke(newPanel);
                return newPanel;
            }

            private void DisposeRelatedPanel()
            {
                UIElementsRuntimeUtility.DisposeRuntimePanel(m_Settings);
            }

            internal void MarkPotentiallyEmpty()
            {
                UIElementsRuntimeUtility.MarkPotentiallyEmpty(m_Settings);
            }
        }


        [SerializeField]
        private ThemeStyleSheet themeUss;

        /// <summary>
        /// Specifies a style sheet that Unity applies to every UI Document attached to the panel.
        /// </summary>
        /// <remarks>
        /// By default this is the main Unity style sheet, which contains default styles for Unity-supplied
        /// elements such as buttons, sliders, and text fields.
        /// </remarks>
        public ThemeStyleSheet themeStyleSheet
        {
            get { return themeUss; }
            set
            {
                themeUss = value;
                ApplyThemeStyleSheet();
            }
        }

        [SerializeField]
        private RenderTexture m_TargetTexture;

        /// <summary>
        /// Specifies a Render Texture to render the panel's UI on.
        /// </summary>
        /// <remarks>
        /// This can be used to display UI on a 3D geometry in the Scene, to perform manual UI post-processing, or
        /// render the UI on a MSAA-enabled Render Texture.
        ///
        /// When the project color space is linear, you should use a Render Texture whose format is
        /// GraphicsFormat.R8G8B8A8_SRGB.
        ///
        /// When the project color space is gamma, you should use a Render Texture whose format is
        /// GraphicsFormat.R8G8B8A8_UNorm.
        /// </remarks>
        public RenderTexture targetTexture
        {
            get => m_TargetTexture;
            set
            {
                m_TargetTexture = value;
                m_PanelAccess.SetTargetTexture();
            }
        }

        [SerializeField]
        private PanelRenderMode m_RenderMode = PanelRenderMode.ScreenSpaceOverlay;

        /// <summary>
        /// Determines how the panel is rendered.
        /// </summary>
        internal PanelRenderMode renderMode
        {
            get => m_RenderMode;
            set => m_RenderMode = value;
        }

        [SerializeField]
        private int m_WorldSpaceLayer = 0;

        /// <summary>
        /// The layer into which the world space panel will render.
        /// </summary>
        internal int worldSpaceLayer
        {
            get => m_WorldSpaceLayer;
            set => m_WorldSpaceLayer = value;
        }

        [SerializeField]
        private PanelScaleMode m_ScaleMode = PanelScaleMode.ConstantPhysicalSize;

        /// <summary>
        /// Determines how elements in the panel scale when the screen size changes.
        /// </summary>
        public PanelScaleMode scaleMode
        {
            get => m_ScaleMode;
            set => m_ScaleMode = value;
        }

        /// <summary>
        /// Sprites have a Pixels Per Unit value that controls the pixel density of the sprite.
        /// For sprites that have the same Pixels Per Unit value as the Reference Pixels Per Unit value in the
        /// PanelSettings asset, the pixel density will be one to one.
        /// </summary>
        public float referenceSpritePixelsPerUnit {
            get { return m_ReferenceSpritePixelsPerUnit; }
            set { m_ReferenceSpritePixelsPerUnit = value; }
        }

        [SerializeField]
        private float m_ReferenceSpritePixelsPerUnit = 100;

        internal float pixelsPerUnit {
            get { return m_PixelsPerUnit; }
            set { m_PixelsPerUnit = value; }
        }

        [SerializeField]
        private float m_PixelsPerUnit = 100;

        [SerializeField]
        private float m_Scale = k_DefaultScaleValue;

        /// <summary>
        /// A uniform scaling factor that Unity applies to elements in the panel before
        /// the panel transform.
        /// </summary>
        /// <remarks>
        /// This value must be greater than 0.
        /// </remarks>
        public float scale
        {
            get => m_Scale;
            set => m_Scale = value;
        }

        #region Scaling parameters

        private const float DefaultDpi = 96;
        [SerializeField]
        private float m_ReferenceDpi = DefaultDpi;
        [SerializeField]
        private float m_FallbackDpi = DefaultDpi;

        /// <summary>
        /// The DPI that the UI is designed for.
        /// </summary>
        /// <remarks>
        /// When <see cref="scaleMode"/> is set to <c>ConstantPhysicalSize</c>, Unity compares
        /// this value to the actual screen DPI, and scales the UI accordingly in the Game view.
        ///
        /// If Unity cannot determine the screen DPI, it uses the <see cref="fallbackDpi"/> instead.
        /// </remarks>
        public float referenceDpi
        {
            get => m_ReferenceDpi;
            set => m_ReferenceDpi = (value >= 1.0f) ? value : DefaultDpi;
        }

        /// <summary>
        /// The DPI value that Unity uses when it cannot determine the screen DPI.
        /// </summary>
        public float fallbackDpi
        {
            get => m_FallbackDpi;
            set => m_FallbackDpi = (value >= 1.0f) ? value : DefaultDpi;
        }

        [SerializeField]
        private Vector2Int m_ReferenceResolution = new Vector2Int(1200, 800);

        /// <summary>
        /// The resolution the UI is designed for.
        /// </summary>
        /// <remarks>
        /// If the screen resolution is larger than the reference resolution, Unity scales
        /// the UI up in the Game view. If it's smaller, Unity scales the UI down.
        /// Unity scales the UI according to the <see cref="PanelSettings.screenMatchMode"/>.
        /// </remarks>
        public Vector2Int referenceResolution
        {
            get => m_ReferenceResolution;
            set => m_ReferenceResolution = value;
        }

        [SerializeField]
        private PanelScreenMatchMode m_ScreenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;

        /// <summary>
        /// Specifies how to scale the panel area when the aspect ratio of the current resolution
        /// does not match the reference resolution.
        /// </summary>
        public PanelScreenMatchMode screenMatchMode
        {
            get => m_ScreenMatchMode;
            set => m_ScreenMatchMode = value;
        }

        [SerializeField]
        [Range(0f, 1f)]
        private float m_Match = 0.0f;

        /// <summary>
        /// Determines whether Unity uses width, height, or a mix of the two as a reference when it scales the panel area.
        /// </summary>
        public float match
        {
            get => m_Match;
            set => m_Match = value;
        }

        #endregion

        [SerializeField]
        private float m_SortingOrder = k_DefaultSortingOrder;

        /// <summary>
        /// When the Scene uses more than one panel, this value determines where this panel appears in the sorting
        /// order relative to other panels.
        /// </summary>
        /// <remarks>
        /// Unity renders panels with a higher sorting order value on top of panels with a lower value.
        /// </remarks>
        public float sortingOrder
        {
            get => m_SortingOrder;
            set
            {
                m_SortingOrder = value;
                ApplySortingOrder();
            }
        }

        internal void ApplySortingOrder()
        {
            m_PanelAccess.SetSortingPriority();
        }

        [SerializeField]
        private int m_TargetDisplay = 0;

        /// <summary>
        /// Set the display intended for the panel.
        /// </summary>
        /// <remarks>
        /// This setting is relevant only when no render texture is applied, as the renderTexture takes precedence.
        /// </remarks>
        public int targetDisplay
        {
            get => m_TargetDisplay;
            set
            {
                m_TargetDisplay = value;
                m_PanelAccess.SetTargetDisplay();
            }
        }

        [SerializeField]
        bool m_ClearDepthStencil = true;

        /// <summary>
        /// Determines whether the depth/stencil buffer is cleared before the panel is rendered.
        /// </summary>
        public bool clearDepthStencil
        {
            get => m_ClearDepthStencil;
            set => m_ClearDepthStencil = value;
        }

        /// <summary>
        /// The depth used to clear the depth/stencil buffer.
        /// </summary>
        public float depthClearValue
        {
            get => UIRUtility.k_ClearZ;
        }

        [SerializeField]
        bool m_ClearColor;

        /// <summary>
        /// Determines whether the color buffer is cleared before the panel is rendered.
        /// </summary>
        public bool clearColor
        {
            get => m_ClearColor;
            set => m_ClearColor = value;
        }

        [SerializeField]
        Color m_ColorClearValue = Color.clear;

        /// <summary>
        /// The color used to clear the color buffer.
        /// </summary>
        /// <remarks>
        /// The color is specified as a "straight" color but will internally be converted to "premultiplied" before
        /// being applied.
        /// </remarks>
        public Color colorClearValue
        {
            get => m_ColorClearValue;
            set => m_ColorClearValue = value;
        }


        [SerializeField]
        UInt32 m_VertexBudget = 0;

        /// <summary>
        /// The expected number of vertices that will be used by this panel.
        /// </summary>
        /// <remarks>
        /// A value of 0 means that the UI renderer will use its own default.
        /// If this hint is set too high, extra CPU and GPU memory may be allocated without actually being used.
        /// If set too low, more vertex buffers may be required, which may increase the number of draw calls and hinder performance.
        /// Changing this setting after initialization should be avoided because it resets the UI renderer.
        /// </remarks>
        public UInt32 vertexBudget
        {
            get => m_VertexBudget;
            set => m_VertexBudget = value;
        }

        internal static Action<BaseRuntimePanel> CreateRuntimePanelDebug;
        internal static Func<ThemeStyleSheet> GetOrCreateDefaultTheme;
        internal static Func<int, Vector2> GetGameViewResolution;
        internal static Action<PanelSettings> SetPanelSettingsAssetDirty;

        internal static void SetupLiveReloadPanelTrackers(bool isLiveReloadOn)
        {
            var allPanelSettings = Resources.FindObjectsOfTypeAll<PanelSettings>();

            if (allPanelSettings == null)
            {
                return;
            }

            foreach (var panelSettings in allPanelSettings)
            {
                // We dump the existing panel of each PanelSettings instance and reload everything attached to it
                // to guarantee 1- tracking is placed properly (or removed if off), and 2- every UXML/USS is up to date.
                // When the new content gets attached to it, a new panel will be created.
                panelSettings.m_PanelAccess.DisposePanel();

                if (panelSettings.m_AttachedUIDocumentsList != null)
                {
                    List<UIDocument> attachedUIDocuments =
                        new List<UIDocument>(panelSettings.m_AttachedUIDocumentsList.m_AttachedUIDocuments);
                    foreach (var attachedUIDocument in attachedUIDocuments)
                    {
                        attachedUIDocument.OnLiveReloadOptionChanged();
                    }
                }
            }
        }


        private RuntimePanelAccess m_PanelAccess;

        internal BaseRuntimePanel panel => m_PanelAccess.panel;

        /// <summary>
        /// The top level visual element.
        /// </summary>
        internal VisualElement visualTree => m_PanelAccess.panel.visualTree;

        internal UIDocumentList m_AttachedUIDocumentsList;

        [HideInInspector]
        [SerializeField]
        DynamicAtlasSettings m_DynamicAtlasSettings = DynamicAtlasSettings.defaults;

        /// <summary>
        /// Settings of the dynamic atlas.
        /// </summary>
        public DynamicAtlasSettings dynamicAtlasSettings { get => m_DynamicAtlasSettings; set => m_DynamicAtlasSettings = value; }

        // References to shaders so they don't get stripped.
        [SerializeField]
        [HideInInspector]
        private Shader m_AtlasBlitShader;

        [SerializeField]
        [HideInInspector]
        private Shader m_RuntimeShader;

        [SerializeField]
        [HideInInspector]
        private Shader m_RuntimeWorldShader;


        /// <summary>
        /// Specifies a <see cref="PanelTextSettings"/> that will be used by every UI Document attached to the panel.
        /// </summary>
        [SerializeField]
        public PanelTextSettings textSettings;

        private Rect m_TargetRect;
        private float m_ResolvedScale; // panel scaling factor (pixels <-> points)

        private StyleSheet m_OldThemeUss;

        private PanelSettings()
        {
            m_PanelAccess = new RuntimePanelAccess(this);
        }

        private void Reset()
        {
            // We assume users will want their UIDocument to look as closely as possible to what they look like in the UIBuilder.
            // This is no guarantee, but it's the best we can do at the moment.
            referenceDpi = ScreenDPI;
            scaleMode = PanelScaleMode.ConstantPhysicalSize;
            renderMode = PanelRenderMode.ScreenSpaceOverlay;
            pixelsPerUnit = 100.0f;
            themeUss = GetOrCreateDefaultTheme?.Invoke();
            m_AtlasBlitShader = m_RuntimeShader = m_RuntimeWorldShader = null;

            SetPanelSettingsAssetDirty?.Invoke(this);

            InitializeShaders();
        }

        private void OnEnable()
        {
            if (themeUss == null)
            {
                // In the Editor, we only want this to run when in play mode, because otherwise users may get a false
                // alarm when the project is loading and the theme asset is not yet loaded. By keeping it here, we can
                // still inform them of a potential problem (it's also in the PanelSettings inspector).
                // On a built player, this will always show, so if they're UI is missing they can have a clue of why.
                if (UIDocument.IsEditorPlayingOrWillChangePlaymode())
                {
                    Debug.LogWarning(
                        "No Theme Style Sheet set to PanelSettings " + name + ", UI will not render properly", this);
                }
            }

            UpdateScreenDPI();
            InitializeShaders();
        }

        private void OnDisable()
        {
            m_PanelAccess.DisposePanel();
        }

        internal void DisposePanel()
        {
            m_PanelAccess.DisposePanel();
        }

        private float ScreenDPI { get; set; }

        private IDebugPanelChangeReceiver m_PanelChangeReceiver = null;

        /// <summary>
        /// Sets a custom <see cref="IPanelChangeReceiver"/> in the panelChangeReceiver setter to receive every change event.
        /// This method is available only in debug builds and the editor as it is a debug feature to go along the profiling of an application.
        /// </summary>
        /// <remarks>
        /// Note that the values returned may change over time when the underlying architecture is modified.
        /// 
        /// As this is called at every change marked on any visual element of the panel, the overhead is not negligeable.
        /// 
        /// </remarks>
        /// <example>
        /// The following example uses the panelChangeReceiver in a game.
        /// To test it, add the component to a GameObject and the Panel Setting asset linked in the component fields.
        /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/ChangeLogger.cs"/>
        /// </example>
        public IDebugPanelChangeReceiver panelChangeReceiver {
            get => m_PanelChangeReceiver;
            set
            {
                m_PanelChangeReceiver = value;
                m_PanelAccess.SetPanelChangeReceiver();
            }
        }

        internal void UpdateScreenDPI()
        {
            ScreenDPI = Screen.dpi;
        }

        private void ApplyThemeStyleSheet(VisualElement root = null)
        {
            if (!m_PanelAccess.isInitialized)
            {
                return;
            }

            if (root == null)
            {
                root = visualTree;
            }

            if (m_OldThemeUss != themeUss && m_OldThemeUss != null)
            {
                root?.styleSheets.Remove(m_OldThemeUss);
            }

            if (themeUss != null)
            {

                // Ensure that isDefaultStyleSheet is set to true even though isDefaultStyleSheet is defaulted to true for ThemeStyleSheet.
                themeUss.isDefaultStyleSheet = true;
                root?.styleSheets.Add(themeUss);
            }

            m_OldThemeUss = themeUss;
        }

        void InitializeShaders()
        {
            if (m_AtlasBlitShader == null)
            {
                m_AtlasBlitShader = Shader.Find(Shaders.k_AtlasBlit);
            }
            if (m_RuntimeShader == null)
            {
                m_RuntimeShader = Shader.Find(Shaders.k_Runtime);
            }
            if (m_RuntimeWorldShader == null)
            {
                m_RuntimeWorldShader = Shader.Find(Shaders.k_RuntimeWorld);
            }
            m_PanelAccess.SetTargetTexture();
        }

        internal void ApplyPanelSettings()
        {
            Rect oldTargetRect = m_TargetRect;
            float oldResolvedScaling = m_ResolvedScale;

            m_TargetRect = GetDisplayRect(); // Expensive to evaluate, so cache

            if (renderMode == PanelRenderMode.WorldSpace)
                m_ResolvedScale = 1.0f; // No panel scaling for world-space
            else
                m_ResolvedScale = ResolveScale(m_TargetRect, ScreenDPI); // dpi should be constant across all displays

            if (visualTree.style.width.value == 0 || // TODO is this check valid? This prevents having to resize the game view!
                m_ResolvedScale != oldResolvedScaling ||
                m_TargetRect.width != oldTargetRect.width ||
                m_TargetRect.height != oldTargetRect.height)
            {
                panel.scale = m_ResolvedScale == 0.0f ? 0.0f : 1.0f / m_ResolvedScale;
                visualTree.style.left = 0;
                visualTree.style.top = 0;
                visualTree.style.width = m_TargetRect.width * m_ResolvedScale;
                visualTree.style.height = m_TargetRect.height * m_ResolvedScale;
            }
            panel.targetTexture = targetTexture;
            panel.targetDisplay = targetDisplay;
            panel.drawsInCameras = renderMode == PanelRenderMode.WorldSpace;
            panel.pixelsPerUnit = pixelsPerUnit;
            panel.isFlat = renderMode != PanelRenderMode.WorldSpace;
            panel.worldSpaceLayer = worldSpaceLayer;
            panel.clearSettings = new PanelClearSettings {clearColor = m_ClearColor, clearDepthStencil = m_ClearDepthStencil, color = m_ColorClearValue};
            panel.referenceSpritePixelsPerUnit = referenceSpritePixelsPerUnit;
            panel.vertexBudget = m_VertexBudget;

            var atlas = panel.atlas as DynamicAtlas;
            if (atlas != null)
            {
                atlas.minAtlasSize = dynamicAtlasSettings.minAtlasSize;
                atlas.maxAtlasSize = dynamicAtlasSettings.maxAtlasSize;
                atlas.maxSubTextureSize = dynamicAtlasSettings.maxSubTextureSize;
                atlas.activeFilters = dynamicAtlasSettings.activeFilters;
                atlas.customFilter = dynamicAtlasSettings.customFilter;
            }
        }

        /// <summary>
        /// Sets the function that handles the transformation from screen space to panel space. For overlay panels,
        /// this function returns the input value.
        /// </summary>
        ///
        /// <param name="screentoPanelSpaceFunction">The translation function. Set to null to revert to the default behavior.</param>
        /// <remarks>
        /// If the panel's targetTexture is applied to 3D objects, one approach is to use a function that raycasts against
        /// MeshColliders in the Scene. The function can first check whether the GameObject that the ray hits has a
        /// MeshRenderer with a shader that uses this panel's target texture. It can then return the transformed
        /// <c>RaycastHit.textureCoord</c> in the texture's pixel space.
        ///
        /// For an example of UI displayed on 3D objects via renderTextures, see the UI Toolkit samples
        /// (menu: <b>Window > UI Toolkit > Examples > Rendering > RenderTexture (Runtime)</b>).
        /// </remarks>
        public void SetScreenToPanelSpaceFunction(Func<Vector2, Vector2> screentoPanelSpaceFunction)
        {
            m_AssignedScreenToPanel = screentoPanelSpaceFunction;
            panel.screenToPanelSpace = m_AssignedScreenToPanel;
        }

        private Func<Vector2, Vector2> m_AssignedScreenToPanel;

        internal float ResolveScale(Rect targetRect, float screenDpi)
        {
            // Calculate scaling
            float resolvedScale = 1.0f;
            switch (scaleMode)
            {
                case PanelScaleMode.ConstantPixelSize:
                    break;
                case PanelScaleMode.ConstantPhysicalSize:
                {
                    var dpi = screenDpi == 0.0f ? fallbackDpi : screenDpi;
                    if (dpi != 0.0f)
                        resolvedScale = referenceDpi / dpi;
                }
                break;
                case PanelScaleMode.ScaleWithScreenSize:
                    if (referenceResolution.x * referenceResolution.y != 0)
                    {
                        var refSize = (Vector2)referenceResolution;
                        var sizeRatio = new Vector2(targetRect.width / refSize.x, targetRect.height / refSize.y);

                        var denominator = 0.0f;
                        switch (screenMatchMode)
                        {
                            case PanelScreenMatchMode.Expand:
                                denominator = Mathf.Min(sizeRatio.x, sizeRatio.y);
                                break;
                            case PanelScreenMatchMode.Shrink:
                                denominator = Mathf.Max(sizeRatio.x, sizeRatio.y);
                                break;
                            default: // PanelScreenMatchMode.MatchWidthOrHeight:
                                var widthHeightRatio = Mathf.Clamp01(match);
                                denominator = Mathf.Lerp(sizeRatio.x, sizeRatio.y, widthHeightRatio);
                                break;
                        }
                        if (denominator != 0.0f)
                            resolvedScale = 1.0f / denominator;
                    }
                    break;
            }

            if (scale > 0.0f)
            {
                resolvedScale /= scale;
            }
            else
            {
                resolvedScale = 0.0f;
            }

            return resolvedScale;
        }

        internal Rect GetDisplayRect()
        {
            if (m_TargetTexture != null)
            {
                // Overlay to texture.
                return new Rect(0, 0, m_TargetTexture.width, m_TargetTexture.height); // TODO: Support sub-rects
            }

            //The device simulatior is a special game view on display 0, and the screen values are properly populated
            if( m_TargetDisplay == 0)
                return new Rect(0,0, Screen.width, Screen.height);

            // In the Unity Editor, Display.displays is not supported; displays.Length always has a value of 1, regardless of how many displays you have connected.
            return new(Vector2.zero, GetGameViewResolution(m_TargetDisplay));
        }

        internal void AttachAndInsertUIDocumentToVisualTree(UIDocument uiDocument)
        {
            if (m_AttachedUIDocumentsList == null)
            {
                m_AttachedUIDocumentsList = new UIDocumentList();
            }
            else
            {
                m_AttachedUIDocumentsList.RemoveFromListAndFromVisualTree(uiDocument);
            }

            m_AttachedUIDocumentsList.AddToListAndToVisualTree(uiDocument, visualTree);
        }

        internal void DetachUIDocument(UIDocument uiDocument)
        {
            if (m_AttachedUIDocumentsList == null)
            {
                return;
            }

            m_AttachedUIDocumentsList.RemoveFromListAndFromVisualTree(uiDocument);

            if (m_AttachedUIDocumentsList.m_AttachedUIDocuments.Count == 0)
                m_PanelAccess.MarkPotentiallyEmpty();
        }

        private float m_OldReferenceDpi;
        private float m_OldFallbackDpi;
        private RenderTexture m_OldTargetTexture;
        private float m_OldSortingOrder;
        private bool m_IsLoaded = false;

        private void OnValidate()
        {
            bool isDirty = false;

            if (m_IsLoaded)
            {
                // reassigning via the properties will re-run the value bounds check on the dpi values
                if (m_OldReferenceDpi != m_ReferenceDpi)
                {
                    referenceDpi = m_ReferenceDpi;
                    isDirty = true;
                }
                if (m_OldFallbackDpi != m_FallbackDpi)
                {
                    fallbackDpi = m_FallbackDpi;
                    isDirty = true;
                }

                if (m_Scale < 0.0f || (m_ScaleMode != PanelScaleMode.ConstantPixelSize && m_Scale != k_DefaultScaleValue))
                {
                    m_Scale = k_DefaultScaleValue;
                    isDirty = true;
                }

                if (m_OldThemeUss != themeUss)
                {
                    var root = visualTree;
                    ApplyThemeStyleSheet(root); // m_OldThemeUss is updated in ApplyThemeStyleSheet
                    isDirty = true;
                }

                if (m_OldTargetTexture != m_TargetTexture)
                {
                    targetTexture = m_TargetTexture;
                    isDirty = true;
                }

                if (m_OldSortingOrder != m_SortingOrder)
                {
                    sortingOrder = m_SortingOrder;
                    isDirty = true;
                }
            }
            else
            {
                m_IsLoaded = true;
            }

            m_OldReferenceDpi = m_ReferenceDpi;
            m_OldFallbackDpi = m_FallbackDpi;
            m_OldTargetTexture = m_TargetTexture;
            m_OldSortingOrder = m_SortingOrder;

            if (isDirty)
            {
                SetPanelSettingsAssetDirty?.Invoke(this);
            }
        }

    }
}
