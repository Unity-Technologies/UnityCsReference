// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
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

    /// <summary>
    /// Defines a Panel Settings asset that instantiates a panel at runtime. The panel makes it possible for Unity to display
    /// UXML-file based UI in the Game view.
    /// </summary>
    [HelpURL("UIE-Runtime-Panel-Settings")]
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
                        var root = m_RuntimePanel.visualTree;
                        root.name = m_Settings.name;

                        m_Settings.ApplyThemeStyleSheet(root);
                        // There's a single StyleSheet tracker for Live Reload per panel, so we can hook it up
                        // here instead of in individual UIDocuments (where VisualTreeAsset trackers are set up).
                        m_RuntimePanel.m_LiveReloadStyleSheetAssetTracker = CreateLiveReloadStyleSheetAssetTracker.Invoke();

                        m_RuntimePanel.enableAssetReload = m_RuntimePanel.m_LiveReloadStyleSheetAssetTracker != null;

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
        /// This is useful when you want to display UI on 3D geometry in the Scene.
        /// For an example of UI displayed on 3D objects via renderTextures, see the UI Toolkit samples
        /// (menu: <b>Window > UI Toolkit > Examples > Rendering > RenderTexture (Runtime)</b>).
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
        private PanelScaleMode m_ScaleMode = PanelScaleMode.ConstantPhysicalSize;

        /// <summary>
        /// Determines how elements in the panel scale when the screen size changes.
        /// </summary>
        public PanelScaleMode scaleMode
        {
            get => m_ScaleMode;
            set => m_ScaleMode = value;
        }

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
        /// When the Scene uses more than one panel, this value determines where this panel appears in the sorting
        /// order relative to other panels.
        /// </summary>
        /// <remarks>
        /// Unity renders panels with a higher sorting order value on top of panels with a lower value.
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

        internal static Func<ILiveReloadAssetTracker<StyleSheet>> CreateLiveReloadStyleSheetAssetTracker;
        internal static Action<IPanel> CreateRuntimePanelDebug;
        internal static Func<ThemeStyleSheet> GetOrCreateDefaultTheme;
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
        internal int m_EmptyPanelCounter = 0;

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
            panel.drawToCameras = false; //we don`t support WorldSpace rendering just yet
            panel.clearSettings = new PanelClearSettings {clearColor = m_ClearColor, clearDepthStencil = m_ClearDepthStencil, color = m_ColorClearValue};

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

            // Overlay.
            if (targetDisplay > 0 && targetDisplay < Display.displays.Length)
            {
                return new Rect(0, 0, Display.displays[targetDisplay].renderingWidth, Display.displays[targetDisplay].renderingHeight);
            }

            return new Rect(0, 0, Screen.width, Screen.height);
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
