// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngineInternal;
using Object = UnityEngine.Object;


namespace UnityEditor
{
    internal class LightingWindowObjectTab
    {
        GITextureType[] kObjectPreviewTextureTypes =
        {
            GITextureType.Albedo,
            GITextureType.Emissive,
            GITextureType.Irradiance,
            GITextureType.Directionality,
            GITextureType.Charting,
            GITextureType.Baked,
            GITextureType.BakedDirectional,
            GITextureType.BakedCharting,
            GITextureType.BakedShadowMask,
        };

        static GUIContent[] kObjectPreviewTextureOptions =
        {
            EditorGUIUtility.TextContent("Albedo"),
            EditorGUIUtility.TextContent("Emissive"),
            EditorGUIUtility.TextContent("Realtime Intensity"),
            EditorGUIUtility.TextContent("Realtime Direction"),
            EditorGUIUtility.TextContent("Realtime Charting"),
            EditorGUIUtility.TextContent("Baked Intensity"),
            EditorGUIUtility.TextContent("Baked Direction"),
            EditorGUIUtility.TextContent("Baked Charting"),
            EditorGUIUtility.TextContent("Baked Shadowmask"),
        };

        ZoomableArea m_ZoomablePreview;
        GUIContent m_SelectedObjectPreviewTexture;
        int m_PreviousSelection;
        AnimBool m_ShowClampedSize = new AnimBool();

        public void OnEnable(EditorWindow window)
        {
            m_ShowClampedSize.value = false;
            m_ShowClampedSize.valueChanged.AddListener(window.Repaint);
        }

        public void OnDisable() {}

        public void ObjectPreview(Rect r)
        {
            if (r.height <= 0)
                return;

            // TODO(GI): Preview as a dockable window.
            // Get textures and populate the dropdown options.
            List<Texture2D> textures = new List<Texture2D>();
            foreach (GITextureType textureType in kObjectPreviewTextureTypes)
            {
                textures.Add(LightmapVisualizationUtility.GetGITexture(textureType));
            }

            if (textures.Count == 0)
                return;

            if (m_ZoomablePreview == null)
            {
                m_ZoomablePreview = new ZoomableArea(true);

                m_ZoomablePreview.hRangeMin = 0.0f;
                m_ZoomablePreview.vRangeMin = 0.0f;

                m_ZoomablePreview.hRangeMax = 1.0f;
                m_ZoomablePreview.vRangeMax = 1.0f;

                m_ZoomablePreview.SetShownHRange(0, 1);
                m_ZoomablePreview.SetShownVRange(0, 1);

                m_ZoomablePreview.uniformScale = true;
                m_ZoomablePreview.scaleWithWindow = true;
            }

            // Draw background
            GUI.Box(r, "", "PreBackground");

            // Top menu rect
            Rect menuRect = new Rect(r);
            menuRect.y += 1;
            menuRect.height = 18;
            GUI.Box(menuRect, "", EditorStyles.toolbar);

            // Top menu dropdown
            Rect dropRect = new Rect(r);
            dropRect.y += 1;
            dropRect.height = 18;
            dropRect.width = 120;

            // Drawable area
            Rect drawableArea = new Rect(r);
            drawableArea.yMin += dropRect.height;
            drawableArea.yMax -= 14;
            drawableArea.width -= 11;

            // Handle top menu dropdown
            int index = Array.IndexOf(kObjectPreviewTextureOptions, m_SelectedObjectPreviewTexture);
            if (index < 0)
                index = 0;

            index = EditorGUI.Popup(dropRect, index, kObjectPreviewTextureOptions, EditorStyles.toolbarPopup);
            if (index >= kObjectPreviewTextureOptions.Length)
                index = 0;

            m_SelectedObjectPreviewTexture = kObjectPreviewTextureOptions[index];
            LightmapType lightmapType = (GITextureType.BakedShadowMask == kObjectPreviewTextureTypes[index] || GITextureType.Baked == kObjectPreviewTextureTypes[index] || GITextureType.BakedDirectional == kObjectPreviewTextureTypes[index] || GITextureType.BakedCharting == kObjectPreviewTextureTypes[index]) ? LightmapType.StaticLightmap : LightmapType.DynamicLightmap;

            // Framing and drawing
            var evt = Event.current;
            switch (evt.type)
            {
                // 'F' will zoom to uv bounds
                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:

                    if (Event.current.commandName == "FrameSelected")
                    {
                        Vector4 lightmapTilingOffset = LightmapVisualizationUtility.GetLightmapTilingOffset(lightmapType);

                        Vector2 min = new Vector2(lightmapTilingOffset.z, lightmapTilingOffset.w);
                        Vector2 max = min + new Vector2(lightmapTilingOffset.x, lightmapTilingOffset.y);

                        min = Vector2.Max(min, Vector2.zero);
                        max = Vector2.Min(max, Vector2.one);

                        float swap = 1f - min.y;
                        min.y = 1f - max.y;
                        max.y = swap;

                        // Make sure that the focus rectangle is a even square
                        Rect rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
                        rect.x -= Mathf.Clamp(rect.height - rect.width, 0, float.MaxValue) / 2;
                        rect.y -= Mathf.Clamp(rect.width - rect.height, 0, float.MaxValue) / 2;
                        rect.width = rect.height = Mathf.Max(rect.width, rect.height);

                        m_ZoomablePreview.shownArea = rect;
                        Event.current.Use();
                    }
                    break;

                // Scale and draw texture and uv's
                case EventType.Repaint:

                    Texture2D texture = textures[index];
                    if (texture && Event.current.type == EventType.Repaint)
                    {
                        Rect textureRect = new Rect(0, 0, texture.width, texture.height);
                        textureRect = ResizeRectToFit(textureRect, drawableArea);
                        //textureRect.x = -textureRect.width / 2;
                        //textureRect.y = -textureRect.height / 2;
                        textureRect = CenterToRect(textureRect, drawableArea);
                        textureRect = ScaleRectByZoomableArea(textureRect, m_ZoomablePreview);

                        // Draw texture and UV
                        Rect uvRect = new Rect(textureRect);
                        uvRect.x += 3;
                        uvRect.y += drawableArea.y + 20;

                        Rect clipRect = new Rect(drawableArea);
                        clipRect.y += dropRect.height + 3;

                        // fix 635838 - We need to offset the rects for rendering.
                        {
                            float offset = clipRect.y - 14;
                            uvRect.y -= offset;
                            clipRect.y -= offset;
                        }

                        // Texture shouldn't be filtered since it will make previewing really blurry
                        FilterMode prevMode = texture.filterMode;
                        texture.filterMode = FilterMode.Point;

                        GITextureType textureType = kObjectPreviewTextureTypes[index];

                        LightmapVisualizationUtility.DrawTextureWithUVOverlay(texture, Selection.activeGameObject, clipRect, uvRect, textureType);
                        texture.filterMode = prevMode;
                    }
                    break;
            }

            // Reset zoom if selection is changed
            if (m_PreviousSelection != Selection.activeInstanceID)
            {
                m_PreviousSelection = Selection.activeInstanceID;
                m_ZoomablePreview.SetShownHRange(0, 1);
                m_ZoomablePreview.SetShownVRange(0, 1);
            }

            // Handle zoomable area
            Rect zoomRect = new Rect(r);
            zoomRect.yMin += dropRect.height;
            m_ZoomablePreview.rect = zoomRect;

            m_ZoomablePreview.BeginViewGUI();
            m_ZoomablePreview.EndViewGUI();

            GUILayoutUtility.GetRect(r.width, r.height);
        }

        Rect ResizeRectToFit(Rect rect, Rect to)
        {
            float widthScale = to.width / rect.width;
            float heightScale = to.height / rect.height;
            float scale = Mathf.Min(widthScale, heightScale);

            float width = (int)Mathf.Round((rect.width * scale));
            float height = (int)Mathf.Round((rect.height * scale));

            return new Rect(rect.x, rect.y, width, height);
        }

        Rect CenterToRect(Rect rect, Rect to)
        {
            float x = Mathf.Clamp((int)(to.width - rect.width) / 2f, 0, int.MaxValue);
            float y = Mathf.Clamp((int)(to.height - rect.height) / 2f, 0, int.MaxValue);

            return new Rect(rect.x + x, rect.y + y, rect.width, rect.height);
        }

        Rect ScaleRectByZoomableArea(Rect rect, ZoomableArea zoomableArea)
        {
            float x = -(zoomableArea.shownArea.x / zoomableArea.shownArea.width) * rect.width;
            float y = ((zoomableArea.shownArea.y - (1f - zoomableArea.shownArea.height)) / zoomableArea.shownArea.height) * rect.height;

            float width = rect.width / zoomableArea.shownArea.width;
            float height = rect.height / zoomableArea.shownArea.height;

            return new Rect(rect.x + x, rect.y + y, width, height);
        }
    }
} // namespace
