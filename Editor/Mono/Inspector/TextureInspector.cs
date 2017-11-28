// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;


namespace UnityEditor
{
    internal class PreviewHelpers
    {
        //This assumes NPOT RenderTextures since Unity 4.3 has this as a requirment already.
        internal static void AdjustWidthAndHeightForStaticPreview(int textureWidth, int textureHeight, ref int width, ref int height)
        {
            int orgWidth = width;
            int orgHeight = height;

            if (textureWidth <= width && textureHeight <= height)
            {
                // For textures smaller than our wanted width and height we use the textures size
                // to prevent excessive magnification artifacts (as seen in the Asset Store).
                width = textureWidth;
                height = textureHeight;
            }
            else
            {
                // For textures larger than our wanted width and height we ensure to
                // keep aspect ratio of the texture and fit it to best match our wanted width and height.
                float relWidth = height / (float)textureWidth;
                float relHeight = width / (float)textureHeight;

                float scale = Mathf.Min(relHeight, relWidth);

                width = Mathf.RoundToInt(textureWidth * scale);
                height = Mathf.RoundToInt(textureHeight * scale);
            }

            // Ensure we have not scaled size below 2 pixels
            width = Mathf.Clamp(width, 2, orgWidth);
            height = Mathf.Clamp(height, 2, orgHeight);
        }
    }

    [CustomEditor(typeof(Texture2D))]
    [CanEditMultipleObjects]
    internal class TextureInspector : Editor
    {
        class Styles
        {
            public GUIContent smallZoom, largeZoom, alphaIcon, RGBIcon;
            public GUIStyle previewButton, previewSlider, previewSliderThumb, previewLabel;

            public readonly GUIContent wrapModeLabel = EditorGUIUtility.TextContent("Wrap Mode");
            public readonly GUIContent wrapU = EditorGUIUtility.TextContent("U axis");
            public readonly GUIContent wrapV = EditorGUIUtility.TextContent("V axis");
            public readonly GUIContent wrapW = EditorGUIUtility.TextContent("W axis");

            public readonly GUIContent[] wrapModeContents =
            {
                EditorGUIUtility.TextContent("Repeat"),
                EditorGUIUtility.TextContent("Clamp"),
                EditorGUIUtility.TextContent("Mirror"),
                EditorGUIUtility.TextContent("Mirror Once"),
                EditorGUIUtility.TextContent("Per-axis")
            };
            public readonly int[] wrapModeValues =
            {
                (int)TextureWrapMode.Repeat,
                (int)TextureWrapMode.Clamp,
                (int)TextureWrapMode.Mirror,
                (int)TextureWrapMode.MirrorOnce,
                -1
            };

            public Styles()
            {
                smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
                largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
                alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
                RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
                previewButton = "preButton";
                previewSlider = "preSlider";
                previewSliderThumb = "preSliderThumb";
                previewLabel = new GUIStyle("preLabel");
                previewLabel.alignment = TextAnchor.UpperCenter; // UpperCenter centers the mip icons vertically better than MiddleCenter
            }
        }
        static Styles s_Styles;

        private bool m_ShowAlpha;

        // Plain Texture
        protected SerializedProperty m_WrapU;
        protected SerializedProperty m_WrapV;
        protected SerializedProperty m_WrapW;
        protected SerializedProperty m_FilterMode;
        protected SerializedProperty m_Aniso;

        [SerializeField]
        protected Vector2 m_Pos;

        [SerializeField]
        float m_MipLevel = 0;

        CubemapPreview m_CubemapPreview = new CubemapPreview();

        public static bool IsNormalMap(Texture t)
        {
            TextureUsageMode mode = TextureUtil.GetUsageMode(t);
            return mode == TextureUsageMode.NormalmapPlain || mode == TextureUsageMode.NormalmapDXT5nm;
        }

        protected virtual void OnEnable()
        {
            m_WrapU = serializedObject.FindProperty("m_TextureSettings.m_WrapU");
            m_WrapV = serializedObject.FindProperty("m_TextureSettings.m_WrapV");
            m_WrapW = serializedObject.FindProperty("m_TextureSettings.m_WrapW");
            m_FilterMode = serializedObject.FindProperty("m_TextureSettings.m_FilterMode");
            m_Aniso = serializedObject.FindProperty("m_TextureSettings.m_Aniso");
        }

        protected virtual void OnDisable()
        {
            m_CubemapPreview.OnDisable();
        }

        internal void SetCubemapIntensity(float intensity)
        {
            if (m_CubemapPreview != null)
                m_CubemapPreview.SetIntensity(intensity);
        }

        public float GetMipLevelForRendering()
        {
            if (target == null)
                return 0.0f;

            if (IsCubemap())
                return m_CubemapPreview.GetMipLevelForRendering(target as Texture);
            else
                return Mathf.Min(m_MipLevel, TextureUtil.GetMipmapCount(target as Texture) - 1);
        }

        public float mipLevel
        {
            get
            {
                if (IsCubemap())
                    return m_CubemapPreview.mipLevel;
                else
                    return m_MipLevel;
            }
            set
            {
                m_CubemapPreview.mipLevel = value;
                m_MipLevel = value;
            }
        }

        // Note: Even though this is a custom editor for Texture2D, the target may not be a Texture2D,
        // since other editors inherit from this one, such as ProceduralTextureInspector.
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DoWrapModePopup();
            DoFilterModePopup();
            DoAnisoLevelSlider();

            serializedObject.ApplyModifiedProperties();
        }

        static void WrapModeAxisPopup(GUIContent label, SerializedProperty wrapProperty)
        {
            // In texture importer settings, serialized properties for wrap modes can contain -1, which means "use default".
            var wrap = (TextureWrapMode)Mathf.Max(wrapProperty.intValue, 0);
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(rect, label, wrapProperty);
            wrap = (TextureWrapMode)EditorGUI.EnumPopup(rect, label, wrap);
            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                wrapProperty.intValue = (int)wrap;
            }
        }

        private static bool IsAnyTextureObjectUsingPerAxisWrapMode(Object[] objects, bool isVolumeTexture)
        {
            foreach (var o in objects)
            {
                int u = 0, v = 0, w = 0;
                // the objects can be Textures themselves, or texture-related importers
                if (o is Texture)
                {
                    var ti = (Texture)o;
                    u = (int)ti.wrapModeU;
                    v = (int)ti.wrapModeV;
                    w = (int)ti.wrapModeW;
                }
                if (o is TextureImporter)
                {
                    var ti = (TextureImporter)o;
                    u = (int)ti.wrapModeU;
                    v = (int)ti.wrapModeV;
                    w = (int)ti.wrapModeW;
                }
                if (o is IHVImageFormatImporter)
                {
                    var ti = (IHVImageFormatImporter)o;
                    u = (int)ti.wrapModeU;
                    v = (int)ti.wrapModeV;
                    w = (int)ti.wrapModeW;
                }
                u = Mathf.Max(0, u);
                v = Mathf.Max(0, v);
                w = Mathf.Max(0, w);
                if (u != v)
                {
                    return true;
                }
                if (isVolumeTexture)
                {
                    if (u != w || v != w)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // showPerAxisWrapModes is state of whether "Per-Axis" mode should be active in the main dropdown.
        // It is set automatically if wrap modes in UVW are different, or if user explicitly picks "Per-Axis" option -- when that one is picked,
        // then it should stay true even if UVW wrap modes will initially be the same.
        //
        // Note: W wrapping mode is only shown when isVolumeTexture is true.
        internal static void WrapModePopup(SerializedProperty wrapU, SerializedProperty wrapV, SerializedProperty wrapW, bool isVolumeTexture, ref bool showPerAxisWrapModes)
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            // In texture importer settings, serialized properties for things like wrap modes can contain -1;
            // that seems to indicate "use defaults, user has not changed them to anything" but not totally sure.
            // Show them as Repeat wrap modes in the popups.
            var wu = (TextureWrapMode)Mathf.Max(wrapU.intValue, 0);
            var wv = (TextureWrapMode)Mathf.Max(wrapV.intValue, 0);
            var ww = (TextureWrapMode)Mathf.Max(wrapW.intValue, 0);

            // automatically go into per-axis mode if values are already different
            if (wu != wv) showPerAxisWrapModes = true;
            if (isVolumeTexture)
            {
                if (wu != ww || wv != ww) showPerAxisWrapModes = true;
            }

            // It's not possible to determine whether any single texture in the whole selection is using per-axis wrap modes
            // just from SerializedProperty values. They can only tell if "some values in whole selection are different" (e.g.
            // wrap value on U axis is not the same among all textures), and can return value of "some" object in the selection
            // (typically based on object loading order). So in order for more intuitive behavior with multi-selection,
            // we go over the actual objects when there's >1 object selected and some wrap modes are different.
            if (!showPerAxisWrapModes)
            {
                if (wrapU.hasMultipleDifferentValues || wrapV.hasMultipleDifferentValues || (isVolumeTexture && wrapW.hasMultipleDifferentValues))
                {
                    if (IsAnyTextureObjectUsingPerAxisWrapMode(wrapU.serializedObject.targetObjects, isVolumeTexture))
                    {
                        showPerAxisWrapModes = true;
                    }
                }
            }

            int value = showPerAxisWrapModes ? -1 : (int)wu;

            // main wrap mode popup
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = !showPerAxisWrapModes && (wrapU.hasMultipleDifferentValues || wrapV.hasMultipleDifferentValues || (isVolumeTexture && wrapW.hasMultipleDifferentValues));
            value = EditorGUILayout.IntPopup(s_Styles.wrapModeLabel, value, s_Styles.wrapModeContents, s_Styles.wrapModeValues);
            if (EditorGUI.EndChangeCheck() && value != -1)
            {
                // assign the same wrap mode to all axes, and hide per-axis popups
                wrapU.intValue = value;
                wrapV.intValue = value;
                wrapW.intValue = value;
                showPerAxisWrapModes = false;
            }

            // show per-axis popups if needed
            if (value == -1)
            {
                showPerAxisWrapModes = true;
                EditorGUI.indentLevel++;
                WrapModeAxisPopup(s_Styles.wrapU, wrapU);
                WrapModeAxisPopup(s_Styles.wrapV, wrapV);
                if (isVolumeTexture)
                {
                    WrapModeAxisPopup(s_Styles.wrapW, wrapW);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.showMixedValue = false;
        }

        bool m_ShowPerAxisWrapModes = false;
        protected void DoWrapModePopup()
        {
            WrapModePopup(m_WrapU, m_WrapV, m_WrapW, IsVolume(), ref m_ShowPerAxisWrapModes);
        }

        protected void DoFilterModePopup()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_FilterMode.hasMultipleDifferentValues;
            FilterMode filter = (FilterMode)m_FilterMode.intValue;
            filter = (FilterMode)EditorGUILayout.EnumPopup(EditorGUIUtility.TempContent("Filter Mode"), filter);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                m_FilterMode.intValue = (int)filter;
        }

        protected void DoAnisoLevelSlider()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_Aniso.hasMultipleDifferentValues;
            int aniso = m_Aniso.intValue;
            aniso = EditorGUILayout.IntSlider("Aniso Level", aniso, 0, 16);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                m_Aniso.intValue = aniso;
            DoAnisoGlobalSettingNote(aniso);
        }

        internal static void DoAnisoGlobalSettingNote(int anisoLevel)
        {
            if (anisoLevel > 1)
            {
                if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable)
                    EditorGUILayout.HelpBox("Anisotropic filtering is disabled for all textures in Quality Settings.", MessageType.Info);
                else if (QualitySettings.anisotropicFiltering == AnisotropicFiltering.ForceEnable)
                    EditorGUILayout.HelpBox("Anisotropic filtering is enabled for all textures in Quality Settings.", MessageType.Info);
            }
        }

        bool IsCubemap()
        {
            var t = target as Texture;
            return t != null && t.dimension == UnityEngine.Rendering.TextureDimension.Cube;
        }

        bool IsVolume()
        {
            var t = target as Texture;
            return t != null && t.dimension == UnityEngine.Rendering.TextureDimension.Tex3D;
        }

        public override void OnPreviewSettings()
        {
            if (IsCubemap())
            {
                m_CubemapPreview.OnPreviewSettings(targets);
                return;
            }


            if (s_Styles == null)
                s_Styles = new Styles();


            // TextureInspector code is reused for RenderTexture and Cubemap inspectors.
            // Make sure we can handle the situation where target is just a Texture and
            // not a Texture2D. It's also used for large popups for mini texture fields,
            // and while it's being shown the actual texture object might disappear --
            // make sure to handle null targets.
            Texture tex = target as Texture;
            bool showMode = true;
            bool alphaOnly = false;
            bool hasAlpha = true;
            int mipCount = 1;

#pragma warning disable CS0618  // Due to Obsolete attribute on Predural classes
            if (target is Texture2D || target is ProceduralTexture)
            {
                alphaOnly = true;
                hasAlpha = false;
            }
#pragma warning restore CS0618  // Due to Obsolete attribute on Predural classes

            foreach (Texture t in targets)
            {
                if (t == null) // texture might have disappeared while we're showing this in a preview popup
                    continue;
                TextureFormat format = 0;
                bool checkFormat = false;
                if (t is Texture2D)
                {
                    format = (t as Texture2D).format;
                    checkFormat = true;
                }
#pragma warning disable CS0618  // Due to Obsolete attribute on Predural classes
                else if (t is ProceduralTexture)
                {
                    format = (t as ProceduralTexture).format;
                    checkFormat = true;
                }
#pragma warning restore CS0618  // Due to Obsolete attribute on Predural classes

                if (checkFormat)
                {
                    if (!TextureUtil.IsAlphaOnlyTextureFormat(format))
                        alphaOnly = false;
                    if (TextureUtil.HasAlphaTextureFormat(format))
                    {
                        TextureUsageMode mode = TextureUtil.GetUsageMode(t);
                        if (mode == TextureUsageMode.Default) // all other texture usage modes don't displayable alpha
                            hasAlpha = true;
                    }
                }

                mipCount = Mathf.Max(mipCount, TextureUtil.GetMipmapCount(t));
            }

            if (alphaOnly)
            {
                m_ShowAlpha = true;
                showMode = false;
            }
            else if (!hasAlpha)
            {
                m_ShowAlpha = false;
                showMode = false;
            }

            if (showMode && tex != null && !IsNormalMap(tex))
                m_ShowAlpha = GUILayout.Toggle(m_ShowAlpha, m_ShowAlpha ? s_Styles.alphaIcon : s_Styles.RGBIcon, s_Styles.previewButton);

            if (mipCount > 1)
            {
                GUILayout.Box(s_Styles.smallZoom, s_Styles.previewLabel);
                GUI.changed = false;
                m_MipLevel = Mathf.Round(GUILayout.HorizontalSlider(m_MipLevel, mipCount - 1, 0, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.MaxWidth(64)));
                GUILayout.Box(s_Styles.largeZoom, s_Styles.previewLabel);
            }
        }

        public override bool HasPreviewGUI()
        {
            return (target != null);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            // show texture
            Texture t = target as Texture;
            if (t == null) // texture might be gone by now, in case this code is used for floating texture preview
                return;

            // Render target must be created before we can display it (case 491797)
            RenderTexture rt = t as RenderTexture;
            if (rt != null)
            {
                if (!SystemInfo.SupportsRenderTextureFormat(rt.format))
                    return; // can't do this RT format
                rt.Create();
            }

            if (IsCubemap())
            {
                m_CubemapPreview.OnPreviewGUI(t, r, background);
                return;
            }

            // Substances can report zero sizes in some cases just after a parameter change;
            // guard against that.
            int texWidth = Mathf.Max(t.width, 1);
            int texHeight = Mathf.Max(t.height, 1);

            float mipLevel = GetMipLevelForRendering();
            float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
            Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");
            float oldBias = t.mipMapBias;
            // with multi-select wantedRect can get negative width/height - not sure if that's intentional, but let's avoid NaNs
            TextureUtil.SetMipMapBiasNoDirty(t, mipLevel - Log2(Mathf.Abs(texWidth / wantedRect.width)));
            FilterMode oldFilter = t.filterMode;
            TextureUtil.SetFilterModeNoDirty(t, FilterMode.Point);

            if (m_ShowAlpha)
            {
                EditorGUI.DrawTextureAlpha(wantedRect, t);
            }
            else
            {
                Texture2D t2d = t as Texture2D;
                if (t2d != null && t2d.alphaIsTransparency)
                {
                    EditorGUI.DrawTextureTransparent(wantedRect, t);
                }
                else
                {
                    EditorGUI.DrawPreviewTexture(wantedRect, t);
                }
            }

            // TODO: Less hacky way to prevent sprite rects to not appear in smaller previews like icons.
            if (wantedRect.width > 32 && wantedRect.height > 32)
            {
                string path = AssetDatabase.GetAssetPath(t);
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                SpriteMetaData[] spritesheet = textureImporter != null ? textureImporter.spritesheet : null;

                if (spritesheet != null && textureImporter.spriteImportMode == SpriteImportMode.Multiple)
                {
                    Rect screenRect = new Rect();
                    Rect sourceRect = new Rect();
                    GUI.CalculateScaledTextureRects(wantedRect, ScaleMode.StretchToFill, (float)t.width / (float)t.height, ref screenRect, ref sourceRect);

                    int origWidth = t.width;
                    int origHeight = t.height;
                    textureImporter.GetWidthAndHeight(ref origWidth, ref origHeight);
                    float definitionScale = (float)t.width / (float)origWidth;

                    HandleUtility.ApplyWireMaterial();
                    GL.PushMatrix();
                    GL.MultMatrix(Handles.matrix);
                    GL.Begin(GL.LINES);
                    GL.Color(new Color(1f, 1f, 1f, 0.5f));
                    foreach (SpriteMetaData sprite in spritesheet)
                    {
                        Rect spriteRect = sprite.rect;
                        Rect spriteScreenRect = new Rect();
                        spriteScreenRect.xMin = screenRect.xMin + screenRect.width * (spriteRect.xMin / t.width * definitionScale);
                        spriteScreenRect.xMax = screenRect.xMin + screenRect.width * (spriteRect.xMax / t.width * definitionScale);
                        spriteScreenRect.yMin = screenRect.yMin + screenRect.height * (1f - spriteRect.yMin / t.height * definitionScale);
                        spriteScreenRect.yMax = screenRect.yMin + screenRect.height * (1f - spriteRect.yMax / t.height * definitionScale);
                        DrawRect(spriteScreenRect);
                    }
                    GL.End();
                    GL.PopMatrix();
                }
            }

            TextureUtil.SetMipMapBiasNoDirty(t, oldBias);
            TextureUtil.SetFilterModeNoDirty(t, oldFilter);

            m_Pos = PreviewGUI.EndScrollView();
            if (mipLevel != 0)
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 20), "Mip " + mipLevel);
        }

        private void DrawRect(Rect rect)
        {
            GL.Vertex(new Vector3(rect.xMin, rect.yMin, 0f));
            GL.Vertex(new Vector3(rect.xMax, rect.yMin, 0f));
            GL.Vertex(new Vector3(rect.xMax, rect.yMin, 0f));
            GL.Vertex(new Vector3(rect.xMax, rect.yMax, 0f));
            GL.Vertex(new Vector3(rect.xMax, rect.yMax, 0f));
            GL.Vertex(new Vector3(rect.xMin, rect.yMax, 0f));
            GL.Vertex(new Vector3(rect.xMin, rect.yMax, 0f));
            GL.Vertex(new Vector3(rect.xMin, rect.yMin, 0f));
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                return null;
            }

            Texture texture = target as Texture;

            if (IsCubemap())
            {
                return m_CubemapPreview.RenderStaticPreview(texture, width, height);
            }

            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (textureImporter != null && textureImporter.textureType == TextureImporterType.Sprite && textureImporter.spriteImportMode == SpriteImportMode.Polygon)
            {
                // If the texture importer is a Sprite of primitive, use the sprite inspector for generating preview/icon.
                if (subAssets.Length > 0)
                {
                    Sprite sprite = subAssets[0] as Sprite;
                    if (sprite)
                        return SpriteInspector.BuildPreviewTexture(width, height, sprite, null, true);
                }
                else
                    return null;
            }

            PreviewHelpers.AdjustWidthAndHeightForStaticPreview(texture.width, texture.height, ref width, ref height);

            RenderTexture savedRT = RenderTexture.active;
            Rect savedViewport = ShaderUtil.rawViewportRect;

            bool sRGB = !TextureUtil.GetLinearSampled(texture);

            RenderTexture tmp = RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.Default,
                    sRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);


            Material mat = EditorGUI.GetMaterialForSpecialTexture(texture);
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            if (mat)
            {
                // We don't want Materials in Editor Resources Project to be modified in the end, so we use an duplicate.
                if (Unsupported.IsDeveloperBuild())
                    mat = new Material(mat);
                Graphics.Blit(texture, tmp, mat);
            }
            else
            {
                Graphics.Blit(texture, tmp);
            }
            GL.sRGBWrite = false;

            RenderTexture.active = tmp;
            // Setting color space on this texture does not matter... internally we just grab the data array
            // when we call GetAssetPreview it generates a new texture from that data...
            Texture2D copy;
            Texture2D tex2d = target as Texture2D;
            if (tex2d != null && tex2d.alphaIsTransparency)
            {
                copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }
            else
            {
                copy = new Texture2D(width, height, TextureFormat.RGB24, false);
            }
            copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);

            EditorGUIUtility.SetRenderTextureNoViewport(savedRT);
            ShaderUtil.rawViewportRect = savedViewport;

            // Kill the duplicate
            if (mat && Unsupported.IsDeveloperBuild())
                Object.DestroyImmediate(mat);

            return copy;
        }

        float Log2(float x)
        {
            return (float)(System.Math.Log(x) / System.Math.Log(2));
        }

        public override string GetInfoString()
        {
            // TextureInspector code is reused for RenderTexture and Cubemap inspectors.
            // Make sure we can handle the situation where target is just a Texture and
            // not a Texture2D.
            Texture t = target as Texture;
            Texture2D t2 = target as Texture2D;
            TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(t)) as TextureImporter;
            string info = t.width.ToString() + "x" + t.height.ToString();

            if (QualitySettings.desiredColorSpace == ColorSpace.Linear)
                info += " " + TextureUtil.GetTextureColorSpaceString(t);

            bool showSize = true;
            bool isPackedSprite = textureImporter && textureImporter.qualifiesForSpritePacking;
            bool isNormalmap = IsNormalMap(t);
            bool stillNeedsCompression = TextureUtil.DoesTextureStillNeedToBeCompressed(AssetDatabase.GetAssetPath(t));
            bool isNPOT = t2 != null && TextureUtil.IsNonPowerOfTwo(t2);
            TextureFormat format = TextureUtil.GetTextureFormat(t);

            showSize = !stillNeedsCompression;
            if (isNPOT)
                info += " (NPOT)";
            if (stillNeedsCompression)
                info += " (Not yet compressed)";
            else
            {
                if (isNormalmap)
                {
                    switch (format)
                    {
                        case TextureFormat.DXT5:
                            info += "  DXTnm";
                            break;
                        case TextureFormat.RGBA32:
                        case TextureFormat.ARGB32:
                            info += "  Nm 32 bit";
                            break;
                        case TextureFormat.ARGB4444:
                            info += "  Nm 16 bit";
                            break;
                        default:
                            info += "  " + TextureUtil.GetTextureFormatString(format);
                            break;
                    }
                }
                else if (isPackedSprite)
                {
                    TextureFormat desiredFormat;
                    ColorSpace dummyColorSpace;
                    int dummyComressionQuality;
                    textureImporter.ReadTextureImportInstructions(EditorUserBuildSettings.activeBuildTarget, out desiredFormat, out dummyColorSpace, out dummyComressionQuality);

                    info += "\n " + TextureUtil.GetTextureFormatString(format) + "(Original) " + TextureUtil.GetTextureFormatString(desiredFormat) + "(Atlas)";
                }
                else
                    info += "  " + TextureUtil.GetTextureFormatString(format);
            }

            if (showSize)
                info += "\n" + EditorUtility.FormatBytes(TextureUtil.GetStorageMemorySizeLong(t));

            if (TextureUtil.GetUsageMode(t) == TextureUsageMode.AlwaysPadded)
            {
                var glWidth = TextureUtil.GetGPUWidth(t);
                var glHeight = TextureUtil.GetGPUHeight(t);
                if (t.width != glWidth || t.height != glHeight)
                    info += string.Format("\nPadded to {0}x{1}", glWidth, glHeight);
            }

            return info;
        }
    }
}


class PreviewGUI
{
    static int sliderHash = "Slider".GetHashCode();
    static Rect s_ViewRect, s_Position;
    static Vector2 s_ScrollPos;

    internal static void BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
    {
        s_ScrollPos = scrollPosition;
        s_ViewRect = viewRect;
        s_Position = position;
        GUIClip.Push(position, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x - (viewRect.width - position.width) * .5f), Mathf.Round(-scrollPosition.y - viewRect.y - (viewRect.height - position.height) * .5f)), Vector2.zero, false);
    }

    internal class Styles
    {
        public static GUIStyle preButton;
        public static void Init()
        {
            preButton = "preButton";
        }
    }

    public static int CycleButton(int selected, GUIContent[] options)
    {
        Styles.Init();
        return EditorGUILayout.CycleButton(selected, options, Styles.preButton);
    }

    public static Vector2 EndScrollView()
    {
        GUIClip.Pop();

        Rect clipRect = s_Position, position = s_Position, viewRect = s_ViewRect;

        Vector2 scrollPosition = s_ScrollPos;
        switch (Event.current.type)
        {
            case EventType.Layout:
                GUIUtility.GetControlID(sliderHash, FocusType.Passive);
                GUIUtility.GetControlID(sliderHash, FocusType.Passive);
                break;
            case EventType.Used:
                break;
            default:
                bool needsVerticalScrollbar = ((int)viewRect.width > (int)clipRect.width);
                bool needsHorizontalScrollbar = ((int)viewRect.height > (int)clipRect.height);
                int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);

                if (needsHorizontalScrollbar)
                {
                    GUIStyle horizontalScrollbar = "PreHorizontalScrollbar";
                    GUIStyle horizontalScrollbarThumb = "PreHorizontalScrollbarThumb";
                    float offset = (viewRect.width - clipRect.width) * .5f;
                    scrollPosition.x = GUI.Slider(new Rect(position.x, position.yMax - horizontalScrollbar.fixedHeight, clipRect.width - (needsVerticalScrollbar ? horizontalScrollbar.fixedHeight : 0) , horizontalScrollbar.fixedHeight),
                            scrollPosition.x, clipRect.width + offset, -offset, viewRect.width,
                            horizontalScrollbar, horizontalScrollbarThumb, true, id);
                }
                else
                {
                    // Get the same number of Control IDs so the ID generation for childrent don't depend on number of things above
                    scrollPosition.x = 0;
                }

                id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);

                if (needsVerticalScrollbar)
                {
                    GUIStyle verticalScrollbar = "PreVerticalScrollbar";
                    GUIStyle verticalScrollbarThumb = "PreVerticalScrollbarThumb";
                    float offset = (viewRect.height - clipRect.height) * .5f;
                    scrollPosition.y = GUI.Slider(new Rect(clipRect.xMax - verticalScrollbar.fixedWidth, clipRect.y, verticalScrollbar.fixedWidth, clipRect.height),
                            scrollPosition.y, clipRect.height + offset, -offset, viewRect.height,
                            verticalScrollbar, verticalScrollbarThumb, false, id);
                }
                else
                {
                    scrollPosition.y = 0;
                }
                break;
        }

        return scrollPosition;
    }

    public static Vector2 Drag2D(Vector2 scrollPosition, Rect position)
    {
        int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);
        Event evt = Event.current;
        switch (evt.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (position.Contains(evt.mousePosition) && position.width > 50)
                {
                    GUIUtility.hotControl = id;
                    evt.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    scrollPosition -= evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(position.width, position.height) * 140.0f;
                    scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90, 90);
                    evt.Use();
                    GUI.changed = true;
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                    GUIUtility.hotControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                break;
        }
        return scrollPosition;
    }
}
