// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

sealed partial class PanelElement
{
    static readonly Event k_RepaintEvent = new Event { type = EventType.Repaint };
    static readonly Vector2Int k_InitialSize = new (100, 100);

    PanelSettings m_PanelSettings;
    ThemeStyleSheet m_Theme;

    Vector2Int m_SubPanelSize = k_InitialSize;
    Vector2 m_Offset = Vector2.zero;
    Vector2 m_Size = k_InitialSize;
    float m_ScaleFactor = 1.0f;

    ColorSpace m_PreviousColorSpace = QualitySettings.activeColorSpace;
    bool m_IsRenderTextureDirty = true;
    RenderTexture m_RenderTexture;

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

    public ThemeStyleSheet ThemeStyleSheet
    {
        get => m_Theme;
        set
        {
            if (m_Theme != value)
            {
                if (m_Theme && subRootVisualElement != null)
                {
                    subRootVisualElement.styleSheets.Remove(m_Theme);
                }

                m_Theme = value;
                if (m_Theme && subRootVisualElement != null)
                {
                    subRootVisualElement.styleSheets.Insert(0, m_Theme);
                }
            }
        }
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
        if (SubPanel == null)
            return;
        SubPanel.TickSchedulingUpdaters();
        SubPanel.Repaint(k_RepaintEvent);
        SubPanel.Render();
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
            runtimePanel.scale = 1.0f;
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

        ThemeStyleSheet = panelSettings.themeStyleSheet;
    }

    void ApplyDefaultSettingsToRuntimePanel(RuntimePanel runtimePanel)
    {
        Assert.IsNotNull(runtimePanel);
        runtimePanel.scale = 1.0f;
        runtimePanel.visualTree.SetSize(SubPanelSize);
        runtimePanel.Root.SetSize(Size);
        if (runtimePanel.panelRenderer != null)
        {
            runtimePanel.panelRenderer.forceGammaRendering = false;
            runtimePanel.panelRenderer.vertexBudget = 0;
            runtimePanel.panelRenderer.textureSlotCount = TextureSlotCount.Eight;
        }

        runtimePanel.targetTexture = m_RenderTexture;
        runtimePanel.targetDisplay = 0;
        runtimePanel.drawsInCameras = false;
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
        ThemeStyleSheet = null;
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
            // case PanelElement.EditorPanel editorPanel:
            //     break;
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
            style.backgroundImage = Background.FromRenderTexture(RenderTexture);
            m_IsRenderTextureDirty = false;
        }

        m_PreviousColorSpace = QualitySettings.activeColorSpace;
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
            }
            else
            {
                var graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                var descriptor = new RenderTextureDescriptor(m_SubPanelSize.x, m_SubPanelSize.y, graphicsFormat, GraphicsFormat.D24_UNorm_S8_UInt);

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
            }
        }
        finally
        {
            switch (SubPanel)
            {
                case RuntimePanel runtimePanel:
                    runtimePanel.targetTexture = RenderTexture;
                    break;
                // case PanelElement.EditorPanel editorPanel:
                //     editorPanel.targetTexture = RenderTexture;
                //     break;
            }
        }
    }

    void DisposeRenderTexture()
    {
        if (RenderTexture == null)
            return;

        RenderTexture.Release();
        Object.DestroyImmediate(RenderTexture);
        RenderTexture = null;
    }
}
