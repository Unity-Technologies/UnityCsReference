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

namespace UnityEditor
{
    [EditorWindowTitle(title = "Viewer", icon = "Lighting")]
    internal class LightmapPreviewWindow : EditorWindow
    {
        // UI selections etc
        [SerializeField]
        ZoomableArea m_ZoomablePreview = null;
        [SerializeField]
        bool m_ShowUVOverlay = true;
        [SerializeField]
        int m_SelectedPreviewTextureOptionIndex = 0;
        [SerializeField]
        float m_ExposureSliderValue = 0.0f;

        float m_ExposureSliderMax = 16f; // this value can be altered by the user

        // Lightmap specifiers
        [SerializeField]
        int m_LightmapIndex = -1;
        [SerializeField]
        int m_InstanceID = -1;
        [SerializeField]
        bool m_IsRealtimeLightmap = false;

        Hash128 m_RealtimeTextureHash = new Hash128();
        VisualisationGITexture m_CachedTexture;
        GameObject[] m_CachedTextureObjects;

        int m_ActiveGameObjectLightmapIndex = -1; // the object the user selects in the scene
        int m_ActiveGameObjectInstanceId = -1; // for instance based non-atlas textures such as baked emissive for Progressive
        Hash128 m_ActiveGameObjectTextureHash = new Hash128(); // the object the user selects in the scene

        GITextureType[] kRealtimePreviewTextureTypes =
        {
            GITextureType.Irradiance,
            GITextureType.Directionality,
            GITextureType.Albedo,
            GITextureType.Emissive,
            GITextureType.Charting
        };

        GITextureType[] kInstanceBasedBakedPreviewTextureTypes =
        {
            GITextureType.Baked,
            GITextureType.BakedDirectional,
            GITextureType.BakedShadowMask,
            GITextureType.BakedAlbedo,
            GITextureType.BakedEmissive,
            GITextureType.BakedCharting,
            GITextureType.BakedTexelValidity,
            GITextureType.BakedUVOverlap,
            GITextureType.BakedLightmapCulling
        };

        GITextureType[] kIndexBasedBakedPreviewTextureTypes =
        {
            GITextureType.Baked,
            GITextureType.BakedDirectional,
            GITextureType.BakedShadowMask,
            GITextureType.BakedCharting,
            GITextureType.BakedTexelValidity,
            GITextureType.BakedUVOverlap,
            GITextureType.BakedLightmapCulling
        };

        static class Styles
        {
            public static readonly GUIContent[] RealtimePreviewTextureOptions =
            {
                EditorGUIUtility.TrTextContent("Realtime Indirect"),
                EditorGUIUtility.TrTextContent("Realtime Directionality"),
                EditorGUIUtility.TrTextContent("Realtime Albedo"),
                EditorGUIUtility.TrTextContent("Realtime Emissive"),
                EditorGUIUtility.TrTextContent("UV Charts")
            };

            public static readonly GUIContent[] InstanceBasedBakedPreviewTextureOptions =
            {
                EditorGUIUtility.TrTextContent("Baked Lightmap"),
                EditorGUIUtility.TrTextContent("Baked Directionality"),
                EditorGUIUtility.TrTextContent("Baked Shadowmask"),
                EditorGUIUtility.TrTextContent("Baked Albedo"),
                EditorGUIUtility.TrTextContent("Baked Emissive"),
                EditorGUIUtility.TrTextContent("Baked UV Charts"),
                EditorGUIUtility.TrTextContent("Baked Texel Validity"),
                EditorGUIUtility.TrTextContent("Baked UV Overlap"),
            };

            public static readonly GUIContent[] IndexBasedBakedPreviewTextureOptions =
            {
                EditorGUIUtility.TrTextContent("Baked Lightmap"),
                EditorGUIUtility.TrTextContent("Baked Directionality"),
                EditorGUIUtility.TrTextContent("Baked Shadowmask"),
                EditorGUIUtility.TrTextContent("Baked UV Charts"),
                EditorGUIUtility.TrTextContent("Baked Texel Validity"),
                EditorGUIUtility.TrTextContent("Baked UV Overlap"),
            };

            public static readonly GUIStyle PreviewLabel = new GUIStyle(EditorStyles.whiteLabel);

            public static readonly GUIContent TextureNotAvailableRealtime = EditorGUIUtility.TrTextContent("The texture is not available at the moment.");
            public static readonly GUIContent TextureNotAvailableBaked = EditorGUIUtility.TrTextContent("The texture is not available at the moment.\nPlease try to rebake the current scene, and make sure that this object is set to 'Contribute Global Illumination' if it's meant to be baked.");
            public static readonly GUIContent TextureNotAvailableBakedShadowmask = EditorGUIUtility.TrTextContent("The texture is not available at the moment.\nPlease make sure that Mixed Lights affect this GameObject and that it is set to 'Contribute Global Illumination'.");
            public static readonly GUIContent TextureLoading = EditorGUIUtility.TrTextContent("Loading...");
            public static readonly GUIContent UVOverlayIcon = EditorGUIUtility.TrIconContent("ToggleUVOverlay", "Toggles the UV Overlay for all the objects in the lightmap. The currently selected object will be highlighted. ");
        }

        public int lightmapIndex
        {
            set { m_LightmapIndex = value; }
        }

        public int instanceID
        {
            set { m_InstanceID = value; }
        }

        public bool isRealtimeLightmap
        {
            get { return m_IsRealtimeLightmap; }
            set { m_IsRealtimeLightmap = value; }
        }

        // this seperates between lightsmaps that we opened from a specific index, or the ones that are connected to an object (where the index can change)
        private bool isIndexBased
        {
            get { return m_InstanceID == -1; }
        }

        private GITextureType[] currentPreviewTextureTypes
        {
            get
            {
                if (isRealtimeLightmap)
                    return kRealtimePreviewTextureTypes;
                else if (isIndexBased)
                    return kIndexBasedBakedPreviewTextureTypes;
                else
                    return kInstanceBasedBakedPreviewTextureTypes;
            }
        }

        private GUIContent[] currentPreviewTextureOptions
        {
            get
            {
                if (isRealtimeLightmap)
                    return Styles.RealtimePreviewTextureOptions;
                else if (isIndexBased)
                    return Styles.IndexBasedBakedPreviewTextureOptions;
                else
                    return Styles.InstanceBasedBakedPreviewTextureOptions;
            }
        }

        public bool useInteractiveLightBakingData { get; set; }

        private string lightmapTitle
        {
            get
            {
                string prefix = "";
                if (isRealtimeLightmap)
                    prefix = "Realtime ";
                else if (useInteractiveLightBakingData)
                    prefix = "Preview ";

                if (isIndexBased) return $"{prefix}Lightmap Index {m_LightmapIndex}";

                var obj = EditorUtility.InstanceIDToObject(m_InstanceID);

                if (obj)
                    return $"{prefix}Lightmap for '{obj.name}'";

                return $"{prefix}Lightmap";
            }
        }

        private float exposure
        {
            get { return SelectedTextureTypeNeedsExposureControl() ? m_ExposureSliderValue : 0.0f; }
        }

        public static void CreateLightmapPreviewWindow(int lightmapId, bool realtimeLightmap, bool indexBased, bool useInteractiveLightBakingData)
        {
            LightmapPreviewWindow window = EditorWindow.CreateInstance<LightmapPreviewWindow>();
            window.minSize = new Vector2(360, 390);
            window.isRealtimeLightmap = realtimeLightmap;

            if (indexBased)
                window.lightmapIndex = lightmapId;
            else
                window.instanceID = lightmapId;

            window.useInteractiveLightBakingData = useInteractiveLightBakingData;

            window.Show();
        }

        void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();
            Lightmapping.lightingDataUpdated += Repaint;
        }

        void OnDisable()
        {
            Lightmapping.lightingDataUpdated -= Repaint;
        }

        void OnBecameVisible()
        {
            UpdateActiveGameObjectSelection();
            Repaint();
        }

        void OnSelectionChange()
        {
            UpdateActiveGameObjectSelection();
            Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUIContent.none, EditorStyles.toolbar, GUILayout.Height(EditorGUI.kWindowToolbarHeight));

            GUILayout.Label(lightmapTitle, "BoldLabel");
            GUILayout.FlexibleSpace();

            DrawPreviewSettings();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();
            {
                var previewRect = new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, position.height);

                GUI.BeginGroup(previewRect);
                DrawPreview(new Rect(0, -1, position.width, position.height - EditorGUI.kWindowToolbarHeight - 1));
                GUI.EndGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSettings()
        {
            using (new EditorGUI.DisabledScope(!SelectedTextureTypeNeedsExposureControl()))
            {
                m_ExposureSliderValue = EditorGUIInternal.ExposureSlider(m_ExposureSliderValue, ref m_ExposureSliderMax, EditorStyles.toolbarSlider);
            }

            m_ShowUVOverlay = GUILayout.Toggle(m_ShowUVOverlay, Styles.UVOverlayIcon, EditorStyles.toolbarButton);

            Rect dropRect = GUILayoutUtility.GetRect(14, 160, EditorGUI.kWindowToolbarHeight, EditorGUI.kWindowToolbarHeight);
            GUIContent[] options = currentPreviewTextureOptions;
            GITextureType[] types = currentPreviewTextureTypes;

            if ((m_SelectedPreviewTextureOptionIndex < 0 || m_SelectedPreviewTextureOptionIndex >= options.Length) || !LightmapVisualizationUtility.IsTextureTypeEnabled(types[m_SelectedPreviewTextureOptionIndex]))
            {
                m_SelectedPreviewTextureOptionIndex = 0;
            }

            if (EditorGUI.DropdownButton(dropRect, options[m_SelectedPreviewTextureOptionIndex], FocusType.Passive, EditorStyles.toolbarDropDownRight))
            {
                GenericMenu menu = new GenericMenu();

                for (int i = 0; i < options.Length; i++)
                {
                    if (LightmapVisualizationUtility.IsTextureTypeEnabled(types[i]))
                        menu.AddItem(options[i], m_SelectedPreviewTextureOptionIndex == i, SelectPreviewTextureIndex, options.ElementAt(i));
                    else
                        menu.AddDisabledItem(options.ElementAt(i));
                }
                menu.DropDown(dropRect);
            }
        }

        private static Rect ComputeMeshUVBounds(Mesh mesh)
        {
            var vertices = mesh.uv2;
            if (vertices == null || vertices.Length == 0)
                vertices = mesh.uv;
            Vector2 minVert = Vector3.positiveInfinity, maxVert = Vector3.negativeInfinity;
            foreach (Vector3 vertex in vertices)
            {
                if (vertex.x < minVert.x)
                    minVert.x = vertex.x;
                if (vertex.y < minVert.y)
                    minVert.y = vertex.y;
                if (vertex.x > maxVert.x)
                    maxVert.x = vertex.x;
                if (vertex.y > maxVert.y)
                    maxVert.y = vertex.y;
            }
            Rect uvBounds = new Rect(minVert, maxVert - minVert);
            return uvBounds;
        }

        private static void HandleUVChartClick(Rect textureRect, GameObject[] renderers)
        {
            Vector2 uv = (Event.current.mousePosition - textureRect.position) / textureRect.size;
            uv.y = 1.0f - uv.y; // Texture coord Y is opposite of UI coord Y.

            GameObject smallestSelectedRenderer = null;
            float smallestSelectedRendererSize = float.MaxValue;

            foreach (GameObject cachedTextureObject in renderers)
            {
                // Each renderer could be either a MeshRenderer or a Terrain. Get the lightmap ST regardless.
                MeshRenderer meshRenderer = cachedTextureObject.GetComponent<MeshRenderer>();
                Vector4 lightmapScaleOffset;
                if (meshRenderer != null)
                {
                    lightmapScaleOffset = meshRenderer.lightmapScaleOffset;
                }
                else
                {
                    Terrain terrain = cachedTextureObject.GetComponent<Terrain>();
                    if (terrain == null)
                        continue;
                    lightmapScaleOffset = terrain.lightmapScaleOffset;
                }

                // Check if we are in stamp/ST rectangle, which may include padding around the UV layout.
                Rect stampRectangle = new Rect(lightmapScaleOffset.z, lightmapScaleOffset.w, lightmapScaleOffset.x, lightmapScaleOffset.y);
                if (stampRectangle.Contains(uv))
                {
                    // Scale the stamp rectangle to the pixel rectangle, which tightly bounds the UV layout. (Terrains never have padding)
                    Rect pixelRectangle = stampRectangle;
                    if (meshRenderer != null)
                    {
                        Rect uvBounds = ComputeMeshUVBounds(meshRenderer.GetComponent<MeshFilter>().sharedMesh);
                        pixelRectangle.x += uvBounds.x * pixelRectangle.width;
                        pixelRectangle.y += uvBounds.y * pixelRectangle.height;
                        pixelRectangle.width *= uvBounds.width;
                        pixelRectangle.height *= uvBounds.height;
                    }

                    // If we are inside the pixel rectangle, keep track of the smallest chart with this criteria.
                    // We don't immediately select the first hit, in order to support overlapping/hole-filling lightmap layouts.
                    if (pixelRectangle.Contains(uv))
                    {
                        float chartSize = pixelRectangle.width * pixelRectangle.height;
                        if (chartSize < smallestSelectedRendererSize)
                        {
                            smallestSelectedRendererSize = chartSize;
                            smallestSelectedRenderer = cachedTextureObject;
                        }
                    }
                }
            }

            if (smallestSelectedRenderer != null)
            {
                Selection.activeGameObject = smallestSelectedRenderer;
                EditorGUIUtility.PingObject(smallestSelectedRenderer);
            }
        }

        private void DrawPreview(Rect r)
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

            m_ZoomablePreview.rect = r;

            m_ZoomablePreview.BeginViewGUI();

            m_ZoomablePreview.hSlider = m_ZoomablePreview.shownArea.width < 1;
            m_ZoomablePreview.vSlider = m_ZoomablePreview.shownArea.height < 1;

            Rect drawableArea = m_ZoomablePreview.drawRect;
            GITextureType textureType = GetSelectedTextureType();

            UpdateCachedTexture(textureType);

            if (m_CachedTexture.textureAvailability == GITextureAvailability.GITextureNotAvailable || m_CachedTexture.textureAvailability == GITextureAvailability.GITextureUnknown)
            {
                if (!isRealtimeLightmap)
                {
                    if (textureType == GITextureType.BakedShadowMask)
                        GUI.Label(drawableArea, Styles.TextureNotAvailableBakedShadowmask, Styles.PreviewLabel);
                    else
                        GUI.Label(drawableArea, Styles.TextureNotAvailableBaked, Styles.PreviewLabel);
                }
                else
                    GUI.Label(drawableArea, Styles.TextureNotAvailableRealtime, Styles.PreviewLabel);

                return;
            }

            if (m_CachedTexture.textureAvailability == GITextureAvailability.GITextureLoading && m_CachedTexture.texture == null)
            {
                GUI.Label(drawableArea, Styles.TextureLoading, Styles.PreviewLabel);
                return;
            }

            LightmapType lightmapType = LightmapVisualizationUtility.GetLightmapType(textureType);

            switch (Event.current.type)
            {
                // 'F' will zoom to uv bounds
                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:

                    if (Event.current.commandName == EventCommandNames.FrameSelected && IsActiveGameObjectInLightmap(textureType))
                    {
                        // There are instance based baked textures where we don't get any STs and can't do the framing
                        if (!isRealtimeLightmap && !LightmapVisualizationUtility.IsAtlasTextureType(textureType))
                            break;

                        Vector4 lightmapTilingOffset = LightmapVisualizationUtility.GetLightmapTilingOffset(lightmapType);

                        Vector2 min = new Vector2(lightmapTilingOffset.z, lightmapTilingOffset.w);
                        Vector2 max = min + new Vector2(lightmapTilingOffset.x, lightmapTilingOffset.y);

                        min = Vector2.Max(min, Vector2.zero);
                        max = Vector2.Min(max, Vector2.one);

                        Texture2D texture = m_CachedTexture.texture;
                        Rect textureRect = new Rect(r.x, r.y, texture.width, texture.height);
                        textureRect = ResizeRectToFit(textureRect, drawableArea);

                        float offsetX = 0.0f, offsetY = 0.0f;

                        if (textureRect.height == drawableArea.height)
                            offsetX = (drawableArea.width - textureRect.width) / drawableArea.width;
                        else
                            offsetY = (drawableArea.height - textureRect.height) / drawableArea.height;

                        // Make sure that the focus rectangle is a even square
                        Rect rect = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
                        rect.width = rect.height = Mathf.Max(rect.width, rect.height);
                        rect.x -= (offsetX * min.x);
                        rect.y += (offsetY * (1 - max.y));

                        m_ZoomablePreview.shownArea = rect;
                        Event.current.Use();
                    }
                    break;

                case EventType.Repaint:
                case EventType.MouseDown:
                {
                    Texture2D texture = m_CachedTexture.texture;

                    if (texture)
                    {
                        Rect textureRect = new Rect(r.x, r.y, texture.width, texture.height);
                        textureRect = ResizeRectToFit(textureRect, drawableArea);
                        textureRect = ScaleRectByZoomableArea(textureRect, m_ZoomablePreview);

                        const int padding = 5;
                        textureRect.x += padding;
                        textureRect.width -= padding * 2;
                        textureRect.height -= padding;

                        // Scale and draw texture and uv's
                        if (Event.current.type == EventType.Repaint)
                        {
                            // Texture shouldn't be filtered since it will make previewing really blurry
                            FilterMode prevMode = texture.filterMode;
                            texture.filterMode = FilterMode.Point;

                            LightmapVisualizationUtility.DrawTextureWithUVOverlay(texture,
                                (m_ShowUVOverlay && IsActiveGameObjectInLightmap(textureType)) ? Selection.activeGameObject : null,
                                m_ShowUVOverlay ? m_CachedTextureObjects : new GameObject[] { }, drawableArea, textureRect, textureType, exposure, useInteractiveLightBakingData);
                            texture.filterMode = prevMode;
                        }
                        // Handle clicking on a UV chart to select the renderer
                        else if (Event.current.type == EventType.MouseDown && m_ShowUVOverlay)
                        {
                            HandleUVChartClick(textureRect, m_CachedTextureObjects);
                        }
                    }
                }
                break;
            }

            m_ZoomablePreview.EndViewGUI();
        }

        private void SelectPreviewTextureIndex(object textureOption)
        {
            GUIContent[] options = currentPreviewTextureOptions;

            m_SelectedPreviewTextureOptionIndex = Array.IndexOf(options, textureOption);
        }

        private GITextureType GetSelectedTextureType()
        {
            GUIContent[] options = currentPreviewTextureOptions;
            GITextureType[] types = currentPreviewTextureTypes;

            if ((m_SelectedPreviewTextureOptionIndex < 0 || m_SelectedPreviewTextureOptionIndex >= options.Length) || !LightmapVisualizationUtility.IsTextureTypeEnabled(types[m_SelectedPreviewTextureOptionIndex]))
            {
                m_SelectedPreviewTextureOptionIndex = 0;
            }

            return types[m_SelectedPreviewTextureOptionIndex];
        }

        private bool SelectedTextureTypeNeedsExposureControl()
        {
            var textureType = GetSelectedTextureType();

            // it only make sense to allow the user to adjust the exposure on these textures
            return textureType == GITextureType.BakedEmissive || textureType == GITextureType.Baked ||
                textureType == GITextureType.Emissive || textureType == GITextureType.Irradiance;
        }

        private bool IsActiveGameObjectInLightmap(GITextureType textureType)
        {
            if (isRealtimeLightmap)
                return (m_ActiveGameObjectTextureHash == m_RealtimeTextureHash);

            if (LightmapVisualizationUtility.IsAtlasTextureType(textureType))
                return (m_ActiveGameObjectLightmapIndex == m_LightmapIndex);

            return (m_ActiveGameObjectInstanceId == m_InstanceID);
        }

        private void UpdateActiveGameObjectSelection()
        {
            MeshRenderer renderer = null;
            Terrain terrain = null;

            // if the selected active object (also active in the hierarchy) is a renderer or a terrain, we check its index etc.
            // otherwise bail
            if (Selection.activeGameObject == null || !Selection.activeGameObject.activeInHierarchy ||
                (!Selection.activeGameObject.TryGetComponent(out renderer) &&
                 !Selection.activeGameObject.TryGetComponent(out terrain)))
            {
                m_ActiveGameObjectLightmapIndex = -1;
                m_ActiveGameObjectInstanceId = -1;
                m_ActiveGameObjectTextureHash = new Hash128();
                return;
            }

            if (isRealtimeLightmap)
            {
                Hash128 inputSystemHash;
                if ((renderer != null && Lightmapping.GetInputSystemHash(renderer.GetInstanceID(), out inputSystemHash))
                    || (terrain != null && Lightmapping.GetInputSystemHash(terrain.GetInstanceID(), out inputSystemHash)))
                {
                    m_ActiveGameObjectTextureHash = inputSystemHash;
                }
                else
                    m_ActiveGameObjectTextureHash = new Hash128();
            }
            else
            {
                m_ActiveGameObjectInstanceId = renderer != null ? renderer.GetInstanceID() : terrain.GetInstanceID();

                if (useInteractiveLightBakingData)
                    m_ActiveGameObjectLightmapIndex = InteractiveLightBaking.GetLightmapIndexFromRenderer(m_ActiveGameObjectInstanceId);
                else
                    m_ActiveGameObjectLightmapIndex = renderer != null ? renderer.lightmapIndex : terrain.lightmapIndex;
            }
        }

        private void UpdateCachedTexture(GITextureType textureType)
        {
            if (isIndexBased)
            {
                if (isRealtimeLightmap)
                {
                    Hash128[] mainHashes = Lightmapping.GetMainSystemHashes();

                    if (!m_RealtimeTextureHash.isValid || !mainHashes.Contains(m_RealtimeTextureHash))
                    {
                        m_RealtimeTextureHash = mainHashes.ElementAtOrDefault(m_LightmapIndex);
                    }
                }
            }
            else // instance based
            {
                if (isRealtimeLightmap)
                {
                    Hash128 systemHash;

                    if (!Lightmapping.GetInputSystemHash(m_InstanceID, out systemHash))
                    {
                        m_CachedTexture.textureAvailability = GITextureAvailability.GITextureNotAvailable;
                        return;
                    }
                    m_RealtimeTextureHash = systemHash;
                }
                else if (useInteractiveLightBakingData)
                {
                    ushort lightmapIndex = InteractiveLightBaking.GetLightmapIndexFromRenderer(m_InstanceID);
                    if (lightmapIndex >= 0xFFFF)
                    {
                        m_CachedTexture.textureAvailability = GITextureAvailability.GITextureNotAvailable;
                        return;
                    }

                    m_LightmapIndex = lightmapIndex;
                }
                else
                {
                    int lightmapIndex;
                    if (!Lightmapping.GetLightmapIndex(m_InstanceID, out lightmapIndex))
                    {
                        m_CachedTexture.textureAvailability = GITextureAvailability.GITextureNotAvailable;
                        return;
                    }

                    m_LightmapIndex = lightmapIndex;
                }
            }

            Hash128 contentHash = isRealtimeLightmap ? LightmapVisualizationUtility.GetRealtimeGITextureHash(m_RealtimeTextureHash, textureType) :
                LightmapVisualizationUtility.GetBakedGITextureHash(m_LightmapIndex, m_InstanceID, textureType, useInteractiveLightBakingData);

            // if we need to fetch a new texture
            if (m_CachedTexture.texture == null || m_CachedTexture.type != textureType || m_CachedTexture.contentHash != contentHash || m_CachedTexture.contentHash == new Hash128())
            {
                m_CachedTexture = isRealtimeLightmap ?
                    LightmapVisualizationUtility.GetRealtimeGITexture(m_RealtimeTextureHash, textureType) :
                    LightmapVisualizationUtility.GetBakedGITexture(m_LightmapIndex, m_InstanceID, textureType, useInteractiveLightBakingData);
            }

            if (!m_ShowUVOverlay)
                return; // if we don't wanna show any overlay

            if (m_CachedTexture.texture == null || m_CachedTexture.textureAvailability == GITextureAvailability.GITextureNotAvailable || m_CachedTexture.textureAvailability == GITextureAvailability.GITextureUnknown)
                return; // if we dont have a texture

            // fetch Renderers

            if (isRealtimeLightmap)
                m_CachedTextureObjects = LightmapVisualizationUtility.GetRealtimeGITextureRenderers(m_RealtimeTextureHash);
            else if (LightmapVisualizationUtility.IsAtlasTextureType(textureType))
                m_CachedTextureObjects = LightmapVisualizationUtility.GetBakedGITextureRenderers(m_LightmapIndex, useInteractiveLightBakingData);
            else // if it's an instance based baked lightmap, we only have 1 object in it
                m_CachedTextureObjects = new GameObject[] {};
        }

        private Rect ResizeRectToFit(Rect rect, Rect to)
        {
            float widthScale = to.width / rect.width;
            float heightScale = to.height / rect.height;
            float scale = Mathf.Min(widthScale, heightScale);

            float width = (int)Mathf.Round((rect.width * scale));
            float height = (int)Mathf.Round((rect.height * scale));

            return new Rect(rect.x, rect.y, width, height);
        }

        private Rect ScaleRectByZoomableArea(Rect rect, ZoomableArea zoomableArea)
        {
            float x = -(zoomableArea.shownArea.x / zoomableArea.shownArea.width) * (rect.x + zoomableArea.rect.width);
            float y = ((zoomableArea.shownArea.y - (1f - zoomableArea.shownArea.height)) / zoomableArea.shownArea.height) * zoomableArea.rect.height;

            float width = rect.width / zoomableArea.shownArea.width;
            float height = rect.height / zoomableArea.shownArea.height;

            return new Rect(rect.x + x, rect.y + y, width, height);
        }
    }
} // namespace
