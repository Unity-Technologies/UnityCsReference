// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    [CustomEditor(typeof(Texture2DArray))]
    [CanEditMultipleObjects]
    internal class Texture2DArrayInspector : TextureInspector
    {
        private const int kScrubberHeight = 21;
        private const int kSliceStepWidth = 33;
        private const int kScrubberMargin = 3;

        static class Styles
        {
            public static readonly GUIContent prevSliceIcon = EditorGUIUtility.TrIconContent("Animation.PrevKey", "Go to previous slice in the array.");
            public static readonly GUIContent nextSliceIcon = EditorGUIUtility.TrIconContent("Animation.NextKey", "Go to next slice in the array.");

            public static readonly GUIStyle stepSlice = "TimeScrubberButton";
            public static readonly GUIStyle sliceScrubber = "TimeScrubber";
        }

        private Material m_Material;
        private int m_Slice;
        private float m_MouseDrag;

        public override string GetInfoString()
        {
            Texture2DArray tex = (Texture2DArray)target;

            string info = string.Format("{0}x{1} {2} slice{5} {3} {4}",
                tex.width, tex.height, tex.depth,
                TextureUtil.GetTextureFormatString(tex.format),
                EditorUtility.FormatBytes(TextureUtil.GetRuntimeMemorySizeLong(tex)),
                tex.depth != 1 ? "s" : "");

            return info;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!SystemInfo.supports2DArrayTextures)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "2D texture array preview not supported");
                return;
            }

            Texture2DArray t = (Texture2DArray)target;

            Rect scrubberRect = r;
            scrubberRect.height = kScrubberHeight;
            r.yMin += kScrubberHeight + kScrubberMargin;

            DoSliceScrubber(scrubberRect, t);

            if (Event.current.type == EventType.Repaint)
            {
                InitPreview();
                m_Material.mainTexture = t;

                // If multiple objects are selected, we might be using a slice level before the maximum
                int effectiveSlice = Mathf.Clamp(m_Slice, 0, t.depth - 1);

                m_Material.SetInt("_SliceIndex", effectiveSlice);
                m_Material.SetInt("_AlphaOnly", showAlpha ? 1 : 0);

                int texWidth = Mathf.Max(t.width, 1);
                int texHeight = Mathf.Max(t.height, 1);

                float effectiveMipLevel = GetMipLevelForRendering();
                float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
                Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
                PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar",
                    "PreHorizontalScrollbarThumb");
                FilterMode oldFilter = t.filterMode;
                TextureUtil.SetFilterModeNoDirty(t, FilterMode.Point);

                EditorGUI.DrawPreviewTexture(wantedRect, t, m_Material, ScaleMode.StretchToFill, 0, effectiveMipLevel);

                TextureUtil.SetFilterModeNoDirty(t, oldFilter);

                m_Pos = PreviewGUI.EndScrollView();
                if (effectiveSlice != 0 || (int)effectiveMipLevel != 0)
                {
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y + 10, r.width, 30),
                        "Slice " + effectiveSlice + "\nMip " + effectiveMipLevel);
                }
            }
        }

        private static readonly int kScrubberHash = "Texture2DArrayPreviewScrubber".GetHashCode();

        private void DoSliceScrubber(Rect controlRect, Texture2DArray t)
        {
            int id = GUIUtility.GetControlID(kScrubberHash, FocusType.Keyboard);

            Rect prevFrameRect = controlRect;
            prevFrameRect.width = kSliceStepWidth;

            Rect nextFrameRect = prevFrameRect;
            nextFrameRect.x += nextFrameRect.width;

            var scrubberRect = controlRect;
            scrubberRect.xMin = nextFrameRect.xMax;

            var evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                {
                    if (scrubberRect.Contains(evt.mousePosition))
                    {
                        GUIUtility.keyboardControl = id;
                        GUIUtility.hotControl = id;
                        m_MouseDrag = evt.mousePosition.x - scrubberRect.xMin;
                        m_Slice = (int)(m_MouseDrag * t.depth / scrubberRect.width);
                        evt.Use();
                    }
                    break;
                }
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        m_MouseDrag += evt.delta.x;
                        m_Slice = (int)(Mathf.Clamp(m_MouseDrag, 0.0f, scrubberRect.width) * t.depth /
                            scrubberRect.width);
                        evt.Use();
                    }
                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                }
                case EventType.KeyDown:
                {
                    if (GUIUtility.keyboardControl == id)
                    {
                        if (evt.keyCode == KeyCode.LeftArrow)
                        {
                            if (m_Slice > 0)
                                --m_Slice;
                            evt.Use();
                        }
                        if (evt.keyCode == KeyCode.RightArrow)
                        {
                            if (m_Slice < t.depth - 1)
                                ++m_Slice;
                            evt.Use();
                        }
                    }
                    break;
                }
                case EventType.Repaint:
                {
                    Styles.sliceScrubber.Draw(controlRect, GUIContent.none, id);

                    float normalizedPosition = Mathf.Lerp(scrubberRect.x, scrubberRect.xMax, m_Slice / (float)(t.depth - 1));
                    TimeArea.DrawPlayhead(normalizedPosition, scrubberRect.yMin, scrubberRect.yMax, 2f,
                        (GUIUtility.keyboardControl == id) ? 1f : 0.5f);
                    break;
                }
            }

            using (new EditorGUI.DisabledGroupScope(m_Slice <= 0))
            {
                if (GUI.Button(prevFrameRect, Styles.prevSliceIcon, Styles.stepSlice))
                    m_Slice--;
            }

            using (new EditorGUI.DisabledGroupScope(m_Slice >= t.depth - 1))
            {
                if (GUI.Button(nextFrameRect, Styles.nextSliceIcon, Styles.stepSlice))
                    m_Slice++;
            }
        }

        void InitPreview()
        {
            if (m_Material == null)
            {
                m_Material = (Material)EditorGUIUtility.LoadRequired("Previews/Preview2DTextureArrayMaterial.mat");
            }
        }

        internal override void OnHeaderIconGUI(Rect iconRect)
        {
            // Even though we have a Preview panel, we don't want to reuse it for the header icon, because it looks very odd when
            // you start scrubbing through different slices etc. Just render the default icon for Texture2DArray objects.
            GUI.Label(iconRect, AssetPreview.GetMiniThumbnail(target));
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            // It's not clear what a meaningful preview for a Texture2DArray would be - the first slice? Multiple slices composited?
            // Until we have a clear idea about the best way to do things, return null for now, to indicate no preview.
            return null;
        }
    }
}
