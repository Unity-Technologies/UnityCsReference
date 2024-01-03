// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    using DisplayOptions = TextureAtlasViewer.DisplayOptions;
    using AtlasType = TextureAtlasViewer.AtlasType;

    [EditorWindowTitle(title = "Dynamic Atlas Viewer")]
    class TextureAtlasViewerWindow : EditorWindow
    {
        [SerializeField]
        public AtlasType m_AtlasType = AtlasType.Nearest;

        [SerializeField]
        DisplayOptions m_DisplayOptions = DisplayOptions.SubTextures | DisplayOptions.AllocationRows | DisplayOptions.AllocationAreas;

        TextureAtlasViewer m_TextureAtlasViewer;

        public static TextureAtlasViewerWindow ShowWindow()
        {
            return GetWindow<TextureAtlasViewerWindow>();
        }

        void OnDisable()
        {
            if (m_Painter2D != null)
            {
                m_Painter2D.Dispose();
                m_Painter2D = null;
            }

            if (m_Texture != null)
            {
                DestroyImmediate(m_Texture);
                m_Texture = null;
            }

            if (m_Overlay != null)
            {
                DestroyImmediate(m_Overlay);
                m_Overlay = null;
            }
        }

        TextElement m_ErrorDisplay;
        VisualElement m_Display;

        RenderTexture m_Texture;
        VectorImage m_Overlay;
        Painter2D m_Painter2D;

        public void CreateGUI()
        {
            m_Painter2D = new Painter2D();
            m_Overlay = CreateInstance<VectorImage>();
            m_Overlay.hideFlags = HideFlags.DontSave;
            m_TextureAtlasViewer = new TextureAtlasViewer();
            TextureAtlasViewer.UIElementsDebugger = GetWindow(typeof(UIElementsDebugger)) as UIElementsDebugger;

            VisualElement root = rootVisualElement;
            root.schedule.Execute(Update).Every(500);

            var typeField = new EnumField("Atlas Type", m_AtlasType);
            typeField.RegisterValueChangedCallback(e =>
            {
                m_AtlasType = (AtlasType)e.newValue;
                Update();
            });
            root.Add(typeField);

            var displayField = new EnumFlagsField("Display Options", m_DisplayOptions);
            displayField.RegisterValueChangedCallback(e =>
            {
                m_DisplayOptions = (DisplayOptions)e.newValue;
                Update();
            });
            root.Add(displayField);

            m_ErrorDisplay = new TextElement { text = "<b>No atlas to display. Make sure a panel is selected in the UI Toolkit debugger or try another atlas type.</b>" };
            root.Add(m_ErrorDisplay);

            var scroller = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            root.Add(scroller);

            m_Display = new VisualElement
            {
                generateVisualContent = GenerateVisualContent,
                style = { marginLeft = 2, marginRight = 2, marginTop = 2, marginBottom = 2 }
            };
            scroller.Add(m_Display);

            Update();
        }

        // The (-2, -2) offset is because of a bug that offsets the generated vector image by 2 pixels for arc-aa expansion.
        void GenerateVisualContent(MeshGenerationContext mgc) => mgc.DrawVectorImage(m_Overlay, new Vector2(-2,-2), Angle.None(), Vector2.one);

        void Update()
        {
            var atlasTexture = m_TextureAtlasViewer.GetAtlasTexture(m_AtlasType);
            if (atlasTexture == null)
            {
                m_ErrorDisplay.style.display = DisplayStyle.Flex;
                m_Display.style.display = DisplayStyle.None;
                return;
            }

            m_ErrorDisplay.style.display = DisplayStyle.None;
            m_Display.style.display = DisplayStyle.Flex;

            m_Display.MarkDirtyRepaint();

            if (m_Texture != null && (m_Texture.width != atlasTexture.width || m_Texture.height != atlasTexture.height))
            {
                DestroyImmediate(m_Texture);
                m_Texture = null;
            }

            if (m_Texture == null)
            {
                m_Texture = new RenderTexture(atlasTexture.width, atlasTexture.height, 0, RenderTextureFormat.ARGB32);
                m_Texture.hideFlags = HideFlags.DontSave;
                m_Display.style.backgroundImage = Background.FromRenderTexture(m_Texture);
                m_Display.style.width = m_Texture.width;
                m_Display.style.height = m_Texture.height;
            }

            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(atlasTexture, m_Texture);
            RenderTexture.active = prev;

            m_Painter2D.Clear();
            m_TextureAtlasViewer.DrawOverlay(m_Painter2D, m_DisplayOptions);
            m_Painter2D.SaveToVectorImage(m_Overlay);
        }
    }
}
