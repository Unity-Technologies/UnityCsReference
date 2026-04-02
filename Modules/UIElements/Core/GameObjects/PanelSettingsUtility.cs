// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements;

internal static class PanelSettingsUtility
{
    internal static void ApplyPanelSettingsToPanel(PanelSettings panelSettings)
    {
        ApplyPanelSettingsToPanel(panelSettings, panelSettings.targetTexture, panelSettings.panel);
    }

    internal static void ApplyPanelSettingsToPanel(IPanelSettings panelSettings, RenderTexture targetTexture, BaseRuntimePanel panel)
    {
        if (panelSettings.renderMode != PanelRenderMode.WorldSpace)
        {
            panel.scale = panelSettings.resolvedScale == 0.0f ? 0.0f : 1.0f / panelSettings.resolvedScale;
            panel.visualTree.style.left = 0;
            panel.visualTree.style.top = 0;
            panel.visualTree.style.width = panelSettings.targetRect.width * panelSettings.resolvedScale;
            panel.visualTree.style.height = panelSettings.targetRect.height * panelSettings.resolvedScale;

            panel.panelRenderer.forceGammaRendering = targetTexture != null && panelSettings.forceGammaRendering;
        }

        panel.targetTexture = targetTexture;
        panel.targetDisplay = panelSettings.targetDisplay;
        panel.drawsInCameras = panelSettings.renderMode == PanelRenderMode.WorldSpace;
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
        panel.dataBindingManager.logLevel = panelSettings.bindingLogLevel;

        var atlas = panel.atlas as DynamicAtlas;
        if (atlas != null)
        {
            atlas.minAtlasSize = panelSettings.dynamicAtlasSettings.minAtlasSize;
            atlas.maxAtlasSize = panelSettings.dynamicAtlasSettings.maxAtlasSize;
            atlas.maxSubTextureSize = panelSettings.dynamicAtlasSettings.maxSubTextureSize;
            atlas.activeFilters = panelSettings.dynamicAtlasSettings.activeFilters;
            atlas.customFilter = panelSettings.dynamicAtlasSettings.customFilter;
        }
    }
}
