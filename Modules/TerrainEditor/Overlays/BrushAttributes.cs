// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;
using UnityEditor.EditorTools;
using System.Collections.Generic;

namespace UnityEditor.TerrainTools
{
    [Overlay(typeof(SceneView), "Brush Attributes", defaultDockPosition = DockPosition.Top, defaultDockZone = DockZone.TopToolbar, defaultDockIndex = 0)]
    [Icon("TerrainOverlays/BrushSettingIcons/BrushAttributes.png")]
    internal class BrushAttributes : ToolbarOverlay, ITransientOverlay, ICreateHorizontalToolbar, ICreateVerticalToolbar
    {
        public bool visible
        {
            get
            {
                var currTool = TerrainInspector.GetActiveTerrainTool() as ITerrainPaintToolWithOverlays;
                if (currTool == null) return false;
                bool directlyInheritsOverlays = currTool.GetType().BaseType.GetGenericTypeDefinition() == typeof(TerrainPaintToolWithOverlays<>).GetGenericTypeDefinition();
                return currTool.HasBrushAttributes && BrushesOverlay.IsSelectedObjectTerrain() && directlyInheritsOverlays;
            }
        }

        internal static BrushAttributes s_Instance;
        BrushAttributes() : base(

            BrushOpacity.k_Id,
            BrushSize.k_Id)
        {
            s_Instance = this;

            // only rebuild if the next tool is/isn't PaintDetailsTool
            ToolManager.activeToolChanged += RebuildAttributesOverlays;
            ToolManager.activeContextChanged += RebuildAttributesOverlays;
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            ToolManager.activeToolChanged -= RebuildAttributesOverlays;
            ToolManager.activeContextChanged -= RebuildAttributesOverlays;
        }

        // this function serves to prevent calling RebuildContent() more than necessary
        private ITerrainPaintToolWithOverlays lastPaintTool = null;
        private void RebuildAttributesOverlays()
        {
            // if last tool is details tool then rebuild content
            var currTool = TerrainInspector.GetActiveTerrainTool() as ITerrainPaintToolWithOverlays;
            if (currTool == null) return;

            if (lastPaintTool == null) lastPaintTool = currTool; // set "last" tool if not already set

            // if lastTool == not details && currTool == details
            // or if lastTool == details && currTool == notDetails
            if ((lastPaintTool is not PaintDetailsTool && currTool is PaintDetailsTool) ||
                (lastPaintTool is PaintDetailsTool && currTool is not PaintDetailsTool))
                {
                    RebuildContent();
                }

            // set last == curr
            lastPaintTool = currTool;

        }

        // call this function for horizontal and vertical toolbar create
        private OverlayToolbar CreateToolbarContent()
        {
            var root = new OverlayToolbar();
            var bo = new BrushOpacity();
            var bs = new BrushSize();

            root.Add(bo);
            root.Add(bs);

            var currTool = TerrainInspector.GetActiveTerrainTool() as ITerrainPaintToolWithOverlays;
            if (currTool is PaintDetailsTool)
            {
                var bts = new BrushTargetStrength();
                root.Add(bts);
            }

            return root;
        }

        public new OverlayToolbar CreateHorizontalToolbarContent()
        {
            return CreateToolbarContent();
        }

        public new OverlayToolbar CreateVerticalToolbarContent()
        {
            return CreateToolbarContent();
        }

        public override VisualElement CreatePanelContent()
        {
            var currTool = TerrainInspector.GetActiveTerrainTool() as ITerrainPaintToolWithOverlays;

            string[] toolbarElementIds;

            if (currTool is PaintDetailsTool)
            {
                toolbarElementIds = new[]
                {
                    BrushOpacity.k_Id,
                    BrushSize.k_Id,
                    BrushTargetStrength.k_Id
                };
            }
            else
            {
                toolbarElementIds = new[]
                {
                    BrushOpacity.k_Id,
                    BrushSize.k_Id,
                };
            }

            IEnumerable<string> toolbarItems = toolbarElementIds;
            return new EditorToolbar(toolbarItems, containerWindow).rootVisualElement;
        }
    }

    [EditorToolbarElement(k_Id, typeof(SceneView))]
    internal class BrushOpacity : CondensedSlider
    {
        internal const string k_Id = "Brushes/Opacity";
        private const string k_Label = "Opacity";
        private const float k_MinValue = 0;
        private const float k_MaxValue = 1;

        static Texture2D s_OpacityIcon =
            EditorGUIUtility.LoadIcon("TerrainOverlays/BrushSettingIcons/Opacity.png");

        static Texture2D Texture
        {
            get
            {
                return s_OpacityIcon;
            }
        }

        public void UpdateOverlayDirection(Layout l)
        {
            UpdateDirection(BrushAttributes.s_Instance.activeLayout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal, k_MinValue, k_MaxValue);
            labelFormatting = (f, s, d) =>
            {
                if (direction == SliderDirection.Vertical)
                    return $"{f:F2}";
                return $"{s} {f:F2}";
            };
            UpdateValues();
        }

        public void UpdateOverlayDirection(bool collapsedChanged)
        {
            UpdateDirection(BrushAttributes.s_Instance.activeLayout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal, k_MinValue, k_MaxValue);
            labelFormatting = (f, s, d) =>
            {
                if (direction == SliderDirection.Vertical)
                    return $"{f:F2}";
                return $"{s} {f:F2}";
            };
            UpdateValues();
        }

        public BrushOpacity()
        : base(k_Label, Texture, k_MinValue, k_MaxValue, BrushAttributes.s_Instance.layout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal)
        {
            labelFormatting = (f, s, d) =>
            {
                if (direction == SliderDirection.Vertical)
                    return $"{f:F2}";
                return $"{s} {f:F2}";
            };
            UpdateOverlayDirection(true);

            RegisterCallback<AttachToPanelEvent>(e =>
            {
                ToolManager.activeToolChanged += UpdateValues;
                ToolManager.activeContextChanged += UpdateValues;
                TerrainInspector.BrushStrengthChanged += UpdateValues;
                BrushAttributes.s_Instance.layoutChanged += UpdateOverlayDirection; // when the overlay is dragged, see if the direction needs to be updated
                BrushAttributes.s_Instance.collapsedChanged += UpdateOverlayDirection;
            });

            RegisterCallback<DetachFromPanelEvent>(e =>
            {
                ToolManager.activeToolChanged -= UpdateValues;
                ToolManager.activeContextChanged -= UpdateValues;
                TerrainInspector.BrushStrengthChanged -= UpdateValues;
                BrushAttributes.s_Instance.layoutChanged -= UpdateOverlayDirection; // when the overlay is dragged, see if the direction needs to be updated
                BrushAttributes.s_Instance.collapsedChanged -= UpdateOverlayDirection;
            });

            this.RegisterValueChangedCallback(e =>
            {
                var editor = TerrainInspector.s_activeTerrainInspectorInstance;
                if (!editor) return;
                editor.brushStrength = e.newValue;
            });

            if (direction == SliderDirection.Horizontal)
                contentWidth = 110;

            UpdateValues();
        }

        private void UpdateValues()
        {
            var editor = TerrainInspector.s_activeTerrainInspectorInstance;
            if (editor) value = editor.brushStrength;
        }
    }

    [EditorToolbarElement(k_Id, typeof(SceneView))]
    internal class BrushSize : CondensedSlider
    {
        internal const string k_Id = "Brushes/Size";
        private const string k_Label = "Size";

        static Texture2D s_SizeIcon =
            EditorGUIUtility.LoadIcon("TerrainOverlays/BrushSettingIcons/Size.png");
        static Texture2D Texture
        {
            get
            {
                return s_SizeIcon;
            }
        }

        static float minBrushSize
        {
            get
            {
                var editor = TerrainInspector.s_activeTerrainInspectorInstance;
                if (!editor) return 0f;
                var tool = EditorToolManager.GetActiveTool() as TerrainPaintToolWithOverlaysBase;
                if (!tool) return 0f;
                if (!tool.Terrain) return 0f;
                if (!tool.Terrain.terrainData) return 0f;

                int textureRez = tool.Terrain.terrainData.heightmapResolution;
                float minSize, maxSize;
                editor.GetBrushSizeLimits(out minSize, out maxSize, textureRez);
                return minSize;
            }
        }

        static float maxBrushSize {
            get
            {
                var editor = TerrainInspector.s_activeTerrainInspectorInstance;
                if (!editor) return 500f;
                var tool = EditorToolManager.GetActiveTool() as TerrainPaintToolWithOverlaysBase;
                if (!tool) return 500f;
                if (!tool.Terrain) return 500f;
                if (!tool.Terrain.terrainData) return 500f;

                int textureRez = tool.Terrain.terrainData.heightmapResolution;
                float minSize, maxSize;
                editor.GetBrushSizeLimits(out minSize, out maxSize, textureRez);
                return maxSize;
            }
        }

        public void UpdateOverlayDirection(Layout l)
        {
            // overlay.activelayout gives what the correct layout SHOULD be, overlay.layout gives what it IS
            UpdateDirection(BrushAttributes.s_Instance.activeLayout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal, minBrushSize, maxBrushSize);
            UpdateValues();
        }

        public void UpdateOverlayDirection(bool collapsedChanged)
        {
            UpdateDirection(BrushAttributes.s_Instance.activeLayout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal, minBrushSize, maxBrushSize);
            UpdateValues();
        }

        public BrushSize()
            : base(k_Label, Texture, minBrushSize, maxBrushSize, BrushAttributes.s_Instance.layout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal)
        {
            UpdateOverlayDirection(true);

            RegisterCallback<AttachToPanelEvent>(e =>
            {
                ToolManager.activeToolChanged += UpdateValues;
                ToolManager.activeContextChanged += UpdateValues;
                TerrainInspector.BrushSizeChanged += UpdateValues;
                BrushAttributes.s_Instance.layoutChanged += UpdateOverlayDirection; // when the overlay is dragged, see if the direction needs to be updated
                BrushAttributes.s_Instance.collapsedChanged += UpdateOverlayDirection;
            });

            RegisterCallback<DetachFromPanelEvent>(e =>
            {
                ToolManager.activeToolChanged -= UpdateValues;
                ToolManager.activeContextChanged -= UpdateValues;
                TerrainInspector.BrushSizeChanged -= UpdateValues;
                BrushAttributes.s_Instance.layoutChanged -= UpdateOverlayDirection; // when the overlay is dragged, see if the direction needs to be updated
                BrushAttributes.s_Instance.collapsedChanged -= UpdateOverlayDirection;
            });

            this.RegisterValueChangedCallback(e =>
            {
                var editor = TerrainInspector.s_activeTerrainInspectorInstance;
                if (editor) editor.brushSize = e.newValue;
            });

            if (direction == SliderDirection.Horizontal)
                contentWidth = 110;

            UpdateValues();
        }

        private void UpdateValues()
        {
            var editor = TerrainInspector.s_activeTerrainInspectorInstance;
            if (editor) value = editor.brushSize;
        }
    }

    // target strength specifically for PaintDetailsTool
    [EditorToolbarElement(k_Id, typeof(SceneView))]
    internal class BrushTargetStrength : CondensedSlider
    {
        internal const string k_Id = "Brushes/TargetStrength";
        private const string k_Label = "TargetStrength";
        private const float k_MinValue = 0;
        private const float k_MaxValue = 1;

        // todo: replace this icon
        static Texture2D s_TargetStrengthIcon =
            EditorGUIUtility.LoadIcon("TerrainOverlays/BrushSettingIcons/TargetDensity.png");

        static Texture2D Texture
        {
            get
            {
                return s_TargetStrengthIcon;
            }
        }

        public void UpdateOverlayDirection(Layout l)
        {
            UpdateDirection(BrushAttributes.s_Instance.activeLayout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal, k_MinValue, k_MaxValue);
            labelFormatting = (f, s, d) =>
            {
                if (direction == SliderDirection.Vertical)
                    return $"{f:F2}";
                return $"{s} {f:F2}";
            };
            UpdateValues();
        }

        public void UpdateOverlayDirection(bool collapsedChanged)
        {
            UpdateDirection(BrushAttributes.s_Instance.activeLayout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal, k_MinValue, k_MaxValue);
            labelFormatting = (f, s, d) =>
            {
                if (direction == SliderDirection.Vertical)
                    return $"{f:F2}";
                return $"{s} {f:F2}";
            };
            UpdateValues();
        }

        public BrushTargetStrength()
        : base(k_Label, Texture, k_MinValue, k_MaxValue, BrushAttributes.s_Instance.layout == Layout.VerticalToolbar ? SliderDirection.Vertical : SliderDirection.Horizontal)
        {
            labelFormatting = (f, s, d) =>
            {
                if (direction == SliderDirection.Vertical)
                    return $"{f:F2}";
                return $"{s} {f:F2}";
            };
            UpdateOverlayDirection(true);

            RegisterCallback<AttachToPanelEvent>(e =>
            {
                ToolManager.activeToolChanged += UpdateValues;
                ToolManager.activeContextChanged += UpdateValues;
                PaintDetailsTool.BrushTargetStrengthChanged += UpdateValues;
                BrushAttributes.s_Instance.layoutChanged += UpdateOverlayDirection; // when the overlay is dragged, see if the direction needs to be updated
                BrushAttributes.s_Instance.collapsedChanged += UpdateOverlayDirection;
            });

            RegisterCallback<DetachFromPanelEvent>(e =>
            {
                ToolManager.activeToolChanged -= UpdateValues;
                ToolManager.activeContextChanged -= UpdateValues;
                PaintDetailsTool.BrushTargetStrengthChanged -= UpdateValues;
                BrushAttributes.s_Instance.layoutChanged -= UpdateOverlayDirection;
                BrushAttributes.s_Instance.collapsedChanged -= UpdateOverlayDirection;
            });

            this.RegisterValueChangedCallback(e =>
            {
                var currTool = TerrainInspector.GetActiveTerrainTool() as PaintDetailsTool;
                if (currTool != null)
                {
                    currTool.detailStrength = e.newValue;
                }
            });

            if (direction == SliderDirection.Horizontal)
                contentWidth = 150;

            UpdateValues();
        }

        private void UpdateValues()
        {
            var currTool = TerrainInspector.GetActiveTerrainTool() as PaintDetailsTool;
            if (currTool != null) // curr tool shouldn't be null
            {
                value = currTool.detailStrength;
            }
        }
    }
}
