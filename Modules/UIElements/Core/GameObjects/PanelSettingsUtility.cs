// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

[VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
internal static class PanelSettingsUtility
{
    public static float ResolveScale(IPanelSettings panelSettings, Vector2 size)
    {
        // Calculate scaling
        float resolvedScale = 1.0f;
        switch (panelSettings.scaleMode)
        {
            case PanelScaleMode.ConstantPixelSize:
                break;
            case PanelScaleMode.ConstantPhysicalSize:
            {
                var dpi = panelSettings.screenDpi == 0.0f ? panelSettings.fallbackDpi : panelSettings.screenDpi;
                if (dpi != 0.0f)
                    resolvedScale = panelSettings.referenceDpi / dpi;
            }
                break;
            case PanelScaleMode.ScaleWithScreenSize:
                if (panelSettings.referenceResolution.x * panelSettings.referenceResolution.y != 0)
                {
                    var refSize = (Vector2)panelSettings.referenceResolution;
                    var sizeRatio = new Vector2(size.x / refSize.x, size.y / refSize.y);

                    var denominator = 0.0f;
                    switch (panelSettings.screenMatchMode)
                    {
                        case PanelScreenMatchMode.Expand:
                            denominator = Mathf.Min(sizeRatio.x, sizeRatio.y);
                            break;
                        case PanelScreenMatchMode.Shrink:
                            denominator = Mathf.Max(sizeRatio.x, sizeRatio.y);
                            break;
                        default: // PanelScreenMatchMode.MatchWidthOrHeight:
                            var widthHeightRatio = Mathf.Clamp01(panelSettings.match);
                            denominator = Mathf.Lerp(sizeRatio.x, sizeRatio.y, widthHeightRatio);
                            break;
                    }

                    if (denominator != 0.0f)
                        resolvedScale = 1.0f / denominator;
                }

                break;
        }

        if (panelSettings.scale > 0.0f)
        {
            resolvedScale /= panelSettings.scale;
        }
        else
        {
            resolvedScale = 0.0f;
        }

        return resolvedScale;
    }

    public static void ApplyPanelSettingsToPanel(PanelSettings panelSettings)
    {
        SetPanelSizeFromPanelSettings(panelSettings, panelSettings.targetTexture, panelSettings.panel);
        SetPanelPropertiesFromPanelSettings(panelSettings, panelSettings.targetTexture, panelSettings.panel);
    }

    public static void SetPanelSizeFromPanelSettings(IPanelSettings panelSettings, RenderTexture targetTexture, BaseRuntimePanel panel)
    {
        // Always re-assign so mode transitions (Overlay <-> WorldSpace, Fixed <-> Dynamic) can't leave stale values.
        panel.scale = panelSettings.resolvedScale == 0.0f ? 0.0f : 1.0f / panelSettings.resolvedScale;
        panel.visualTree.style.left = 0;
        panel.visualTree.style.top = 0;

        if (panelSettings.renderMode != PanelRenderMode.WorldSpace)
        {
            panel.visualTree.style.width = panelSettings.targetRect.width * panelSettings.resolvedScale;
            panel.visualTree.style.height = panelSettings.targetRect.height * panelSettings.resolvedScale;

            panel.panelRenderer.forceGammaRendering = targetTexture != null && panelSettings.forceGammaRendering;
        }
        else
        {
            // World-space components are positioned absolutely by SetupWorldSpaceSize; size the panel root
            // to 0x0 (instead of leaving it auto) so it never acts as a containing block between documents.
            panel.visualTree.style.width = 0;
            panel.visualTree.style.height = 0;
        }
    }

    public static void SetPanelPropertiesFromPanelSettings(IPanelSettings panelSettings, RenderTexture targetTexture, BaseRuntimePanel panel)
    {
        panel.targetTexture = targetTexture;
        panel.targetDisplay = panelSettings.targetDisplay;
        panel.SetDrawsInCameras(panelSettings.renderMode == PanelRenderMode.WorldSpace);
        panel.pixelsPerUnit = panelSettings.pixelsPerUnit;
        panel.isFlat = panelSettings.renderMode != PanelRenderMode.WorldSpace;
        panel.clearSettings = new PanelClearSettings
        {
            clearColor = panelSettings.clearColor,
            clearDepthStencil = panelSettings.clearDepthStencil,
            color = panelSettings.colorClearValue
        };
        panel.referenceSpritePixelsPerUnit = panelSettings.referenceSpritePixelsPerUnit;
        panel.panelRenderer.vertexBudget = panelSettings.vertexBudget;
        panel.panelRenderer.textureSlotCount = panelSettings.textureSlotCount;
        panel.panelRenderer.extraVertexChannels = panelSettings.extraVertexChannels;
        panel.dataBindingManager.logLevel = panelSettings.bindingLogLevel;

        if (panel.atlas is DynamicAtlas atlas)
        {
            atlas.minAtlasSize = panelSettings.dynamicAtlasSettings.minAtlasSize;
            atlas.maxAtlasSize = panelSettings.dynamicAtlasSettings.maxAtlasSize;
            atlas.maxSubTextureSize = panelSettings.dynamicAtlasSettings.maxSubTextureSize;
            atlas.activeFilters = panelSettings.dynamicAtlasSettings.activeFilters;
            atlas.customFilter = panelSettings.dynamicAtlasSettings.customFilter;
        }
    }
}
