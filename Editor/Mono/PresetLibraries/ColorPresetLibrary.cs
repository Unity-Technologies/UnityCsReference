// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class ColorPresetLibrary : PresetLibrary
    {
        [SerializeField]
        List<ColorPreset> m_Presets = new List<ColorPreset>();
        Texture2D m_ColorSwatch;
        Texture2D m_ColorSwatchTriangular;
        Texture2D m_MiniColorSwatchTriangular;
        Texture2D m_CheckerBoard;
        public const int kSwatchSize = 14;
        public const int kMiniSwatchSize = 8;

        void OnDestroy()
        {
            if (m_ColorSwatch != null)
                DestroyImmediate(m_ColorSwatch);

            if (m_ColorSwatchTriangular != null)
                DestroyImmediate(m_ColorSwatchTriangular);

            if (m_MiniColorSwatchTriangular != null)
                DestroyImmediate(m_MiniColorSwatchTriangular);

            if (m_CheckerBoard != null)
                DestroyImmediate(m_CheckerBoard);
        }

        public override int Count()
        {
            return m_Presets.Count;
        }

        public override object GetPreset(int index)
        {
            return m_Presets[index].color;
        }

        public override void Add(object presetObject, string presetName)
        {
            Color color = (Color)presetObject;
            m_Presets.Add(new ColorPreset(color, presetName));
        }

        public override void Replace(int index, object newPresetObject)
        {
            Color color = (Color)newPresetObject;
            m_Presets[index].color = color;
        }

        public override void Remove(int index)
        {
            m_Presets.RemoveAt(index);
        }

        public override void Move(int index, int destIndex, bool insertAfterDestIndex)
        {
            PresetLibraryHelpers.MoveListItem(m_Presets, index, destIndex, insertAfterDestIndex);
        }

        public override void Draw(Rect rect, int index)
        {
            DrawInternal(rect, m_Presets[index].color);
        }

        public override void Draw(Rect rect, object presetObject)
        {
            DrawInternal(rect, (Color)presetObject);
        }

        private void Init()
        {
            if (m_ColorSwatch == null)
                m_ColorSwatch = CreateColorSwatchWithBorder(kSwatchSize, kSwatchSize, false);

            if (m_ColorSwatchTriangular == null)
                m_ColorSwatchTriangular = CreateColorSwatchWithBorder(kSwatchSize, kSwatchSize, true);

            if (m_MiniColorSwatchTriangular == null)
                m_MiniColorSwatchTriangular = CreateColorSwatchWithBorder(kMiniSwatchSize, kMiniSwatchSize, true);

            if (m_CheckerBoard == null)
                m_CheckerBoard = GradientEditor.CreateCheckerTexture(2, 2, 3, new Color(0.8f, 0.8f, 0.8f), new Color(0.5f, 0.5f, 0.5f));
        }

        private void DrawInternal(Rect rect, Color preset)
        {
            Init();

            bool isHDR = preset.maxColorComponent > 1f;

            // Normalize color if HDR to give some color hint (otherwise it likely is just white)
            if (isHDR)
                preset = preset.RGBMultiplied(1f / preset.maxColorComponent);

            Color orgColor = GUI.color;
            if ((int)rect.height == kSwatchSize)
            {
                if (preset.a > 0.97f)
                    RenderSolidSwatch(rect, preset);
                else
                    RenderSwatchWithAlpha(rect, preset, m_ColorSwatchTriangular);

                if (isHDR)
                    GUI.Label(rect, "h");
            }
            else
            {
                // The Add preset button swatch
                RenderSwatchWithAlpha(rect, preset, m_MiniColorSwatchTriangular);
            }
            GUI.color = orgColor;
        }

        private void RenderSolidSwatch(Rect rect, Color preset)
        {
            GUI.color = preset;
            GUI.DrawTexture(rect, m_ColorSwatch);
        }

        private void RenderSwatchWithAlpha(Rect rect, Color preset, Texture2D swatchTexture)
        {
            Rect r2 = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);

            // Background checkers
            GUI.color = Color.white;
            Rect texCoordsRect = new Rect(0, 0, r2.width / m_CheckerBoard.width, r2.height / m_CheckerBoard.height);
            GUI.DrawTextureWithTexCoords(r2, m_CheckerBoard, texCoordsRect, false);

            // Alpha color
            GUI.color = preset;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);

            // Solid color in half triangle
            GUI.color = new Color(preset.r, preset.g, preset.b, 1f);
            GUI.DrawTexture(rect, swatchTexture);
        }

        public override string GetName(int index)
        {
            return m_Presets[index].name;
        }

        public override void SetName(int index, string presetName)
        {
            m_Presets[index].name = presetName;
        }

        public void CreateDebugColors()
        {
            for (int i = 0; i < 2000; ++i)
                m_Presets.Add(new ColorPreset(new Color(Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), Random.Range(0.2f, 1f), 1f), "Preset Color " + i));
        }

        private static Texture2D CreateColorSwatchWithBorder(int width, int height, bool triangular)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[width * height];
            Color transparent = new Color(1, 1, 1, 0f);
            if (triangular)
            {
                for (int i = 0; i < height; i++)
                    for (int j = 0; j < width; j++)
                    {
                        if (i < width - j)
                            pixels[j + i * width] = Color.white;
                        else
                            pixels[j + i * width] = transparent;
                    }
            }
            else
            {
                for (int i = 0; i < height * width; i++)
                    pixels[i] = Color.white;
            }

            // first row
            for (int i = 0; i < width; i++)
                pixels[i] = Color.black;

            // last row
            for (int i = 0; i < width; i++)
                pixels[(height - 1) * width + i] = Color.black;

            // first col
            for (int i = 0; i < height; i++)
                pixels[i * width] = Color.black;

            // last col
            for (int i = 0; i < height; i++)
                pixels[i * width + width - 1] = Color.black;

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        [System.Serializable]
        class ColorPreset
        {
            [SerializeField]
            string m_Name;

            [SerializeField]
            Color m_Color;

            public ColorPreset(Color preset, string presetName)
            {
                color = preset;
                name = presetName;
            }

            public ColorPreset(Color preset, Color preset2, string presetName)
            {
                color = preset;
                name = presetName;
            }

            public Color color
            {
                get { return m_Color; }
                set { m_Color = value; }
            }

            public string name
            {
                get { return m_Name; }
                set { m_Name = value; }
            }
        }
    }
}
