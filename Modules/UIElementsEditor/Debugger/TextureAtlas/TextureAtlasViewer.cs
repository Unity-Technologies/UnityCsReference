// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEngine;
using System;
using UnityEngine.UIElements.UIR;

namespace UnityEditor.UIElements.Debugger
{
    class TextureAtlasViewer
    {
        public enum AtlasType
        {
            Nearest = 1,
            Bilinear = 2,
        };

        [Flags]
        public enum DisplayOptions
        {
            SubTextures = 1 << 0,
            AllocationRows = 1 << 1,
            AllocationAreas = 1 << 2,
        }

        public static UIElementsDebugger UIElementsDebugger;

        DynamicAtlasPage m_SelectedAtlasPage;

        public DynamicAtlas GetDynamicAtlas()
        {
            if (null == UIElementsDebugger)
                return null;

            var panel = UIElementsDebugger.debuggerContext?.selection?.panel as BaseVisualElementPanel;

            return panel?.atlas as DynamicAtlas;
        }

        void FetchAtlasPage(AtlasType atlasType)
        {
            m_SelectedAtlasPage = null;

            if (null == UIElementsDebugger)
                return;

            // This code could probably be dropped right into the final product
            var debuggerContext = UIElementsDebugger.debuggerContext;
            if (null == debuggerContext)
                return;

            DynamicAtlas atlas = GetDynamicAtlas();
            if (atlas == null)
                return;

            switch (atlasType)
            {
                case AtlasType.Nearest:
                    m_SelectedAtlasPage = atlas.PointPage;
                    break;
                case AtlasType.Bilinear:
                    m_SelectedAtlasPage = atlas.BilinearPage;
                    break;
                default:
                    return;
            }
        }

        public Texture GetAtlasTexture(AtlasType atlasType)
        {
            FetchAtlasPage(atlasType);
            return m_SelectedAtlasPage?.atlas;
        }

        static void DrawRect(Painter2D painter, RectInt rect, Color color, float atlasTextureHeight)
        {
            if (null == painter)
                return;

            painter.lineWidth = 1.0f;
            painter.lineCap = LineCap.Butt;
            painter.strokeColor = color;

            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.min.x, atlasTextureHeight - rect.max.y));
            painter.LineTo(new Vector2(rect.max.x, atlasTextureHeight - rect.max.y));
            painter.LineTo(new Vector2(rect.max.x, atlasTextureHeight - rect.min.y));
            painter.LineTo(new Vector2(rect.min.x, atlasTextureHeight - rect.min.y));
            painter.LineTo(new Vector2(rect.min.x, atlasTextureHeight - rect.max.y));
            painter.Stroke();
        }

        public void DrawOverlay(Painter2D painter2D, DisplayOptions displayOptions)
        {
            if (m_SelectedAtlasPage == null)
                return;

            Texture atlasTexture = m_SelectedAtlasPage.atlas;
            if (atlasTexture == null)
                return;

            DynamicAtlas dynamicAtlas = GetDynamicAtlas();
            Debug.Assert(dynamicAtlas != null);

            var database = dynamicAtlas.Database;

            RectInt atlasRect = new RectInt(0, 0, atlasTexture.width, atlasTexture.height);
            DrawRect(painter2D, atlasRect, Color.gray, atlasTexture.height);

            if ((displayOptions & DisplayOptions.AllocationAreas) != 0)
            {
                foreach (Allocator2D.Area area in m_SelectedAtlasPage.allocator.areas)
                {
                    if (area.rect.xMax > atlasRect.width || area.rect.yMax > atlasRect.height)
                        continue;

                    RectInt areaRect = area.rect;
                    DrawRect(painter2D, areaRect, Color.green, atlasTexture.height);
                }
            }

            foreach (var texInfo in database.Values)
            {
                if (texInfo.page != m_SelectedAtlasPage) continue;

                if ((displayOptions & DisplayOptions.SubTextures) != 0)
                {
                    RectInt texInfoRect = texInfo.rect;
                    DrawRect(painter2D, texInfoRect, Color.red, atlasTexture.height);
                }

                if ((displayOptions & DisplayOptions.AllocationRows) != 0)
                {
                    var alloc = texInfo.alloc;
                    var row = alloc.row;
                    RectInt AllocPageRect = row.rect;
                    DrawRect(painter2D, AllocPageRect, Color.blue, atlasTexture.height);
                }
            }
        }
    }
}
