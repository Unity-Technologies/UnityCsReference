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
        class Styles
        {
            public static readonly GUIContent[] ObjectPreviewTextureOptions =
            {
                EditorGUIUtility.TextContent("UV Charts"),
                EditorGUIUtility.TextContent("Realtime Albedo"),
                EditorGUIUtility.TextContent("Realtime Emissive"),
                EditorGUIUtility.TextContent("Realtime Indirect"),
                EditorGUIUtility.TextContent("Realtime Directionality"),
                EditorGUIUtility.TextContent("Baked Lightmap"),
                EditorGUIUtility.TextContent("Baked Directionality"),
                EditorGUIUtility.TextContent("Baked Shadowmask"),
                EditorGUIUtility.TextContent("Baked Albedo"),
                EditorGUIUtility.TextContent("Baked Emissive"),
                EditorGUIUtility.TextContent("Baked UV Charts"),
                EditorGUIUtility.TextContent("Baked Texel Validity"),
            };

            public static readonly GUIContent TextureNotAvailableRealtime = EditorGUIUtility.TextContent("The texture is not available at the moment.");
            public static readonly GUIContent TextureNotAvailableBaked = EditorGUIUtility.TextContent("The texture is not available at the moment.\nPlease try to rebake the current scene or turn on Auto, and make sure that this object is set to Lightmap Static if it's meant to be baked.");
            public static readonly GUIContent TextureNotAvailableBakedShadowmask = EditorGUIUtility.TextContent("The texture is not available at the moment.\nPlease make sure that Mixed Lights affect this GameObject and that it is set to Lightmap Static.");
            public static readonly GUIContent TextureLoading = EditorGUIUtility.TextContent("Loading...");
        }

        GITextureType[] kObjectPreviewTextureTypes =
        {
            GITextureType.Charting,
            GITextureType.Albedo,
            GITextureType.Emissive,
            GITextureType.Irradiance,
            GITextureType.Directionality,
            GITextureType.Baked,
            GITextureType.BakedDirectional,
            GITextureType.BakedShadowMask,
            GITextureType.BakedAlbedo,
            GITextureType.BakedEmissive,
            GITextureType.BakedCharting,
            GITextureType.BakedTexelValidity
        };

        ZoomableArea m_ZoomablePreview;
        GUIContent m_SelectedObjectPreviewTexture;
        int m_PreviousSelection;
        AnimBool m_ShowClampedSize = new AnimBool();

        VisualisationGITexture m_CachedTexture;

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

            int index = Array.IndexOf(Styles.ObjectPreviewTextureOptions, m_SelectedObjectPreviewTexture);

            if (index < 0 || !LightmapVisualizationUtility.IsTextureTypeEnabled(kObjectPreviewTextureTypes[index]))
            {
                index = 0;
                m_SelectedObjectPreviewTexture = Styles.ObjectPreviewTextureOptions[index];
            }

            if (EditorGUI.DropdownButton(dropRect, m_SelectedObjectPreviewTexture, FocusType.Passive, EditorStyles.toolbarPopup))
            {
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < Styles.ObjectPreviewTextureOptions.Length; i++)
                {
                    if (LightmapVisualizationUtility.IsTextureTypeEnabled(kObjectPreviewTextureTypes[i]))
                        menu.AddItem(Styles.ObjectPreviewTextureOptions[i], index == i, SelectPreviewTextureOption, Styles.ObjectPreviewTextureOptions.ElementAt(i));
                    else
                        menu.AddDisabledItem(Styles.ObjectPreviewTextureOptions.ElementAt(i));
                }
                menu.DropDown(dropRect);
            }

            GITextureType textureType = kObjectPreviewTextureTypes[Array.IndexOf(Styles.ObjectPreviewTextureOptions, m_SelectedObjectPreviewTexture)];

            if (m_CachedTexture.type != textureType || m_CachedTexture.contentHash != LightmapVisualizationUtility.GetSelectedObjectGITextureHash(textureType) || m_CachedTexture.contentHash == new Hash128())
            {
                m_CachedTexture = LightmapVisualizationUtility.GetSelectedObjectGITexture(textureType);
            }

            if (m_CachedTexture.textureAvailability == GITextureAvailability.GITextureNotAvailable || m_CachedTexture.textureAvailability == GITextureAvailability.GITextureUnknown)
            {
                if (LightmapVisualizationUtility.IsBakedTextureType(textureType))
                {
                    if (textureType == GITextureType.BakedShadowMask)
                        GUI.Label(drawableArea, Styles.TextureNotAvailableBakedShadowmask);
                    else
                        GUI.Label(drawableArea, Styles.TextureNotAvailableBaked);
                }
                else
                    GUI.Label(drawableArea, Styles.TextureNotAvailableRealtime);

                return;
            }

            if (m_CachedTexture.textureAvailability == GITextureAvailability.GITextureLoading && m_CachedTexture.texture == null)
            {
                GUI.Label(drawableArea, Styles.TextureLoading);

                return;
            }

            LightmapType lightmapType = LightmapVisualizationUtility.GetLightmapType(textureType);

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

                    Texture2D texture = m_CachedTexture.texture;
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

        private void SelectPreviewTextureOption(object textureOption)
        {
            m_SelectedObjectPreviewTexture = (GUIContent)textureOption;
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
