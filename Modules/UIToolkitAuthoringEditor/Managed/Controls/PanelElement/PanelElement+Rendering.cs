// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

sealed partial class PanelElement
{
    static readonly Vector2Int k_InitialSize = new (100, 100);

    PanelSettings m_PanelSettings;
    StyleSheet m_ThemeOverride;
    StyleSheet m_AppliedTheme;

    Vector2Int m_SubPanelSize = k_InitialSize;
    Vector2 m_Offset = Vector2.zero;
    Vector2 m_Size = k_InitialSize;
    float m_ScaleFactor = 1.0f;

    ColorSpace m_PreviousColorSpace = QualitySettings.activeColorSpace;
    bool m_IsRenderTextureDirty = true;
    RenderTexture m_RenderTexture;
    RenderTexture m_DisplayTexture;
    bool m_RenderRequested;

    const string k_CopyShaderName = "Hidden/UIR/CopyRegion";
    static readonly int k_ColorConversionId = Shader.PropertyToID("_ColorConversion");

    [NoAutoStaticsCleanup] // persist the cached material; nulling it would orphan the native HideAndDontSave object
    static Material s_CopyMaterial;

    public PanelSettings PanelSettings
    {
        get => m_PanelSettings;
        set
        {
            if (m_PanelSettings == value)
                return;
            m_PanelSettings = value;
            ApplyPanelSettings(m_PanelSettings);
        }
    }

    public StyleSheet Theme
    {
        get => m_ThemeOverride;
        set
        {
            // Use ReferenceEquals instead of Unity's == so that assigning actual null when
            // m_ThemeOverride holds a destroyed (fake-null) asset is not swallowed by the
            // early-return guard — Unity's == treats destroyed objects as equal to null.
            if (ReferenceEquals(m_ThemeOverride, value))
                return;
            m_ThemeOverride = value;
            UpdateAppliedTheme();
        }
    }

    /// <summary>
    /// The theme the sub-panel uses: <see cref="Theme"/>, else the theme of the panel settings.
    /// </summary>
    public StyleSheet AppliedTheme => m_AppliedTheme;

    /// <summary>
    /// Sets <see cref="ContextType"/> and <see cref="Theme"/> together. A sub-panel that the context type
    /// replaces is created with <paramref name="theme"/>, so its content is not cloned with the old theme first.
    /// </summary>
    public void SetContextTypeAndTheme(ContextType contextType, StyleSheet theme)
    {
        if (m_ContextType != contextType && IsCreated)
        {
            m_ThemeOverride = theme;
            ContextType = contextType;
            return;
        }

        ContextType = contextType;
        Theme = theme;
    }

    void UpdateAppliedTheme()
    {
        var resolved = m_ThemeOverride ?? (m_PanelSettings ? m_PanelSettings.themeStyleSheet : null);
        if (m_AppliedTheme == resolved)
            return;

        // On the visual tree, like a runtime panel's theme: dropdown menus open at the visual tree, outside the sub-root.
        var themeRoot = SubPanel?.visualTree;
        if (m_AppliedTheme && themeRoot != null)
            themeRoot.styleSheets.Remove(m_AppliedTheme);

        m_AppliedTheme = resolved;

        if (m_AppliedTheme && themeRoot != null)
            themeRoot.styleSheets.Insert(0, m_AppliedTheme);

        ThemeChanged?.Invoke(this);
    }

    public Vector2Int SubPanelSize
    {
        get => m_SubPanelSize;
        private set
        {
            if (m_SubPanelSize == value)
                return;
            m_SubPanelSize = value;
            MarkTextureDirty();
        }
    }

    public Vector2 Offset
    {
        get => m_Offset;
        set
        {
            if (m_Offset == value)
                return;
            m_Offset = value;
            subRootVisualElement?.MarkDirtyRepaint();
        }
    }

    public Vector2 Size
    {
        get => m_Size;
        set
        {
            if (m_Size == value)
                return;
            m_Size = value;
            subRootVisualElement?.MarkDirtyRepaint();
        }
    }

    public float ScaleFactor
    {
        get => m_ScaleFactor;
        set
        {
            if (Mathf.Approximately(m_ScaleFactor, value))
                return;
            m_ScaleFactor = value;
            subRootVisualElement?.MarkDirtyRepaint();
        }
    }

    public Overflow ContentOverflowMode
    {
        get => subRootVisualElement?.style.overflow.value ?? Overflow.Visible;
        set
        {
            if (subRootVisualElement == null)
                return;
            subRootVisualElement.style.overflow = value;
        }
    }

    public RenderTexture RenderTexture
    {
        get => m_RenderTexture;
        private set
        {
            if (m_RenderTexture != value)
                DisposeRenderTexture();

            m_RenderTexture = value;
        }
    }

    // What a host editor panel shows. Editor sub-panels store gamma-encoded colors, like editor windows; in a Linear
    // project an editor panel encodes again what it samples from a non-sRGB texture, so it shows an sRGB copy instead.
    public RenderTexture DisplayTexture => m_DisplayTexture != null ? m_DisplayTexture : m_RenderTexture;

    bool NeedsDisplayTexture => m_ContextType == ContextType.Editor && QualitySettings.activeColorSpace == ColorSpace.Linear;

    public float SubPanelPixelsPerPoint
    {
        get => SubPanel.pixelsPerPoint;

        set
        {
            if (!Mathf.Approximately(SubPanel.pixelsPerPoint, value))
            {
                var floatSize = new Vector2(SubPanelSize.x / SubPanel.pixelsPerPoint, SubPanelSize.y/ SubPanel.pixelsPerPoint);

                var intSize = new Vector2Int(Mathf.CeilToInt(floatSize.x * value), Mathf.CeilToInt(floatSize.y * value));
                SubPanel.pixelsPerPoint = value;
                SubPanelSize = intSize;
            }
        }
    }

    public void ResizeRenderTexture(Vector2 size)
    {
        var intSize = new Vector2Int(Mathf.CeilToInt(size.x * SubPanelPixelsPerPoint),
            Mathf.CeilToInt(size.y * SubPanelPixelsPerPoint));

        subRootVisualElement.SetSize(size);
        SubPanelSize = intSize;
    }

    public void FrameUpdate()
    {
        RequestRender();
        TickUpdate();
    }

    // Ticks run outside a GPU frame, so per-frame GPU memory waits for the next frame end; render only on change.
    public void TickUpdate()
    {
        if (SubPanel == null)
            return;
        SubPanel.TickSchedulingUpdaters();
        if (!NeedsRender)
            return;

        // Editor content may hold IMGUI, which only draws inside the host's repaint: ask for one instead.
        if (SubPanel is EditorAuthoringPanel { isInHostRepaint: false, hostPanel: not null })
        {
            m_RenderRequested = true;
            ((IEditorAuthoringPanelHost)this).hostElement.MarkDirtyRepaint();
            return;
        }

        RenderSubPanel();
    }

    bool NeedsRender => SubPanel.isDirty || m_RenderRequested ||
        (RenderTexture != null && !RenderTexture.IsCreated()) ||
        (m_DisplayTexture != null && !m_DisplayTexture.IsCreated());

    void OnHostRepainting()
    {
        if (SubPanel != null && NeedsRender)
            RenderSubPanel();
    }

    public void RequestRender() => m_RenderRequested = true;

    void RenderSubPanel()
    {
        SubPanel.Repaint();
        SubPanel.Render();
        m_RenderRequested = false;
    }

    void ApplyPanelSettingsToRuntimePanel(RuntimePanel runtimePanel, PanelSettings panelSettings)
    {
        Assert.IsNotNull(runtimePanel);
        Assert.IsNotNull(panelSettings);

        var actualRenderMode = panelSettings.renderMode;
        try
        {
            panelSettings.renderMode = PanelRenderMode.ScreenSpaceOverlay;

            PanelSettingsUtility.SetPanelPropertiesFromPanelSettings(panelSettings, RenderTexture, runtimePanel);
            // We reset the panel's scale and size here because we want to apply it to the SubRootVisualElement
            // instead. This is because the PanelElement have a concept of a render window analog to a
            // viewport and a canvas. This allows to show overflow in some cases.
            runtimePanel.scale = 1.0f / SubPanelPixelsPerPoint;
            runtimePanel.visualTree.SetSize(SubPanelSize);

            // We calculate the resolved scale manually because we don't want it based on the panel's size,
            // but on a custom root's size.
            var resolvedScale = PanelSettingsUtility.ResolveScale(panelSettings, Size);

            runtimePanel.Root.SetSize(Size * resolvedScale);
            runtimePanel.Root.style.scale = Vector2.one * (ScaleFactor * SubPanelPixelsPerPoint) / resolvedScale;
            runtimePanel.Root.style.transformOrigin = new TransformOrigin(0, 0);
            runtimePanel.Root.style.translate = Offset * SubPanelPixelsPerPoint;

            runtimePanel.panelRenderer.forceGammaRendering = QualitySettings.activeColorSpace == ColorSpace.Gamma;

            // We override these settings because we don't have a main camera to clear the buffer.
            runtimePanel.clearSettings = new PanelClearSettings
            {
                clearColor = true, color = default, clearDepthStencil = true
            };

            // But we apply the clear color on the custom root, if set.
            if (panelSettings.clearColor)
                runtimePanel.Root.style.backgroundColor = panelSettings.colorClearValue;
            else
                runtimePanel.Root.style.backgroundColor = StyleKeyword.Null;
        }
        finally
        {
            panelSettings.renderMode = actualRenderMode;
        }

        UpdateAppliedTheme();
    }

    void ApplyDefaultSettingsToRuntimePanel(RuntimePanel runtimePanel)
    {
        Assert.IsNotNull(runtimePanel);
        runtimePanel.scale = 1.0f / SubPanelPixelsPerPoint;
        runtimePanel.visualTree.SetSize(SubPanelSize);
        runtimePanel.Root.SetSize(Size);
        runtimePanel.Root.style.scale = Vector2.one * (ScaleFactor * SubPanelPixelsPerPoint);
        runtimePanel.Root.style.transformOrigin = new TransformOrigin(0, 0);
        runtimePanel.Root.style.translate = Offset * SubPanelPixelsPerPoint;

        // Panel settings that were applied before may have set a clear color.
        runtimePanel.Root.style.backgroundColor = StyleKeyword.Null;
        if (runtimePanel.panelRenderer != null)
        {
            runtimePanel.panelRenderer.forceGammaRendering = false;
            runtimePanel.panelRenderer.vertexBudget = 0;
            runtimePanel.panelRenderer.textureSlotCount = TextureSlotCount.Eight;
            runtimePanel.panelRenderer.filterTextureBudget = FilterTextureBudget.Automatic;
        }

        runtimePanel.targetTexture = m_RenderTexture;
        runtimePanel.targetDisplay = 0;
        runtimePanel.SetDrawsInCameras(false);
        runtimePanel.pixelsPerUnit = 100;
        runtimePanel.isFlat = true;
        runtimePanel.clearSettings = new PanelClearSettings { clearDepthStencil = true, clearColor = true, color = Color.clear };
        runtimePanel.referenceSpritePixelsPerUnit = 100.0f;

        if (runtimePanel.dataBindingManager != null)
            runtimePanel.dataBindingManager.logLevel = BindingLogLevel.None;
        if (runtimePanel.atlas is DynamicAtlas atlas)
        {
            atlas.minAtlasSize = 0;
            atlas.maxAtlasSize = 0;
            atlas.maxSubTextureSize = 0;
            atlas.activeFilters = DynamicAtlasFilters.None;
            atlas.customFilter = null;
        }
        UpdateAppliedTheme();
    }

    // Editor panels have no PanelSettings: the panel settings only matter for runtime sub-panels.
    void ApplyDefaultSettingsToEditorPanel(EditorSubPanel editorPanel)
    {
        Assert.IsNotNull(editorPanel);
        editorPanel.scale = 1.0f / SubPanelPixelsPerPoint;
        editorPanel.visualTree.SetSize(SubPanelSize);
        editorPanel.Root.SetSize(Size);
        editorPanel.Root.style.scale = Vector2.one * (ScaleFactor * SubPanelPixelsPerPoint);
        editorPanel.Root.style.transformOrigin = new TransformOrigin(0, 0);
        editorPanel.Root.style.translate = Offset * SubPanelPixelsPerPoint;
        editorPanel.targetTexture = m_RenderTexture;
        editorPanel.clearSettings = new PanelClearSettings { clearDepthStencil = true, clearColor = true, color = Color.clear };

        if (editorPanel.dataBindingManager != null)
            editorPanel.dataBindingManager.logLevel = BindingLogLevel.None;
        UpdateAppliedTheme();
    }

    void ApplyPanelSettings(PanelSettings panelSettings)
    {
        UpdateRenderTexture();
        switch (SubPanel)
        {
            case RuntimePanel runtimePanel:
                if (panelSettings)
                    ApplyPanelSettingsToRuntimePanel(runtimePanel, panelSettings);
                else
                    ApplyDefaultSettingsToRuntimePanel(runtimePanel);
                break;

            case EditorSubPanel editorPanel:
                ApplyDefaultSettingsToEditorPanel(editorPanel);
                break;
        }
    }

    void MarkTextureDirty()
    {
        m_IsRenderTextureDirty = true;
    }

    void UpdateRenderTexture()
    {
        if (m_IsRenderTextureDirty || RenderTexture == null || m_PreviousColorSpace != QualitySettings.activeColorSpace)
        {
            CreateRenderTexture();
            style.backgroundImage = Background.FromRenderTexture(DisplayTexture);
            m_IsRenderTextureDirty = false;
        }

        m_PreviousColorSpace = QualitySettings.activeColorSpace;
    }

    internal static GraphicsFormat GetRenderTextureGraphicsFormat()
    {
        return GetRenderTextureGraphicsFormat(QualitySettings.activeColorSpace);
    }

    internal static GraphicsFormat GetRenderTextureGraphicsFormat(ColorSpace colorSpace)
    {
        return colorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
    }

    // Editor panels always render gamma-encoded colors, whatever the project color space.
    GraphicsFormat GetSubPanelGraphicsFormat()
    {
        return m_ContextType == ContextType.Editor
            ? GetRenderTextureGraphicsFormat(ColorSpace.Gamma)
            : GetRenderTextureGraphicsFormat();
    }

    void CreateRenderTexture()
    {
        try
        {
            if (SubPanelSize.x == 0 || SubPanelSize.y == 0)
            {
                if (RenderTexture != null)
                {
                    RenderTexture.Release();
                    Object.DestroyImmediate(RenderTexture);
                    RenderTexture = null;
                }

                DisposeDisplayTexture();
            }
            else
            {
                var depthStencilFormat = GraphicsFormatUtility.GetDepthStencilFormat(24, 8);
                var descriptor = new RenderTextureDescriptor(m_SubPanelSize.x, m_SubPanelSize.y, GetSubPanelGraphicsFormat(), depthStencilFormat);

                if (RenderTexture == null)
                {
                    RenderTexture = new RenderTexture(descriptor);
                }
                else
                {
                    RenderTexture.Release();
                    RenderTexture.descriptor = descriptor;
                    RenderTexture.Create();
                }

                UpdateDisplayTexture();
                m_RenderRequested = true;
            }
        }
        finally
        {
            if (SubPanelContent != null)
                SubPanelContent.TargetTexture = RenderTexture;
        }
    }

    void UpdateDisplayTexture()
    {
        if (!NeedsDisplayTexture)
        {
            DisposeDisplayTexture();
            return;
        }

        var descriptor = new RenderTextureDescriptor(m_SubPanelSize.x, m_SubPanelSize.y, GetRenderTextureGraphicsFormat(), GraphicsFormat.None);
        if (m_DisplayTexture == null)
        {
            m_DisplayTexture = new RenderTexture(descriptor);
        }
        else
        {
            m_DisplayTexture.Release();
            m_DisplayTexture.descriptor = descriptor;
            m_DisplayTexture.Create();
        }
    }

    void CopyToDisplayTexture()
    {
        if (m_DisplayTexture != null && RenderTexture != null)
            CopyStoredColors(RenderTexture, m_DisplayTexture);
    }

    /// <summary>
    /// Copies the bytes stored in <paramref name="source"/> to <paramref name="target"/>, whatever the sRGB
    /// conversions of their sampling and storing: the colors keep their encoding.
    /// </summary>
    internal static void CopyStoredColors(RenderTexture source, RenderTexture target)
    {
        var isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        var sourceDecodes = isLinear && source.sRGB;
        var targetEncodes = isLinear && target.sRGB;

        if (s_CopyMaterial == null)
            s_CopyMaterial = new Material(Shader.Find(k_CopyShaderName)) { hideFlags = HideFlags.HideAndDontSave };

        // The shader conversion cancels the decode of the sampling against the encode of the store.
        float colorConversion;
        if (sourceDecodes == targetEncodes)
            colorConversion = 0f;
        else if (targetEncodes)
            colorConversion = -1f;
        else
            colorConversion = 1f;
        s_CopyMaterial.SetFloat(k_ColorConversionId, colorConversion);

        var previous = RenderTexture.active;
        try
        {
            Graphics.Blit(source, target, s_CopyMaterial, 0);
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    void DisposeDisplayTexture()
    {
        if (m_DisplayTexture == null)
            return;

        m_DisplayTexture.Release();
        Object.DestroyImmediate(m_DisplayTexture);
        m_DisplayTexture = null;
    }

    void DisposeRenderTexture()
    {
        DisposeDisplayTexture();

        if (RenderTexture == null)
            return;

        RenderTexture.Release();
        Object.DestroyImmediate(RenderTexture);
        RenderTexture = null;
    }
}
