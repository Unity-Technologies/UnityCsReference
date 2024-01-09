// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.Toolbars
{
    class ToolButton : EditorToolbarToggle
    {
        static Texture2D s_PanViewIcon;
        static Texture2D s_PanViewOnIcon;
        static Texture2D s_OrbitViewIcon;
        static Texture2D s_OrbitViewOnIcon;
        static Texture2D s_FpsViewIcon;
        static Texture2D s_FpsViewOnIcon;
        static Texture2D s_ZoomViewIcon;
        static Texture2D s_ZoomViewOnIcon;

        static Texture2D s_DefaultMoveIcon;
        static Texture2D s_DefaultMoveOnIcon;
        static Texture2D s_DefaultRotateIcon;
        static Texture2D s_DefaultRotateOnIcon;
        static Texture2D s_DefaultScaleIcon;
        static Texture2D s_DefaultScaleOnIcon;
        static Texture2D s_DefaultTransformIcon;
        static Texture2D s_DefaultTransformOnIcon;
        static Texture2D s_DefaultRectIcon;
        static Texture2D s_DefaultRectOnIcon;

        static ToolButton()
        {
            LoadBuiltinToolsIcons();
        }

        // in milliseconds
        const int k_DelayBeforeOpenDropdown = 150;

        List<EditorTool> m_Variants;
        readonly Tool m_TargetTool;
        int m_CurrentVariantIndex;
        VisualElement m_ToolVariantDropdown;
        IVisualElementScheduledItem m_OpenMenuScheduler;
        int m_HoveredVariantIndex;

        public event Action displayChanged;

        public EditorTool currentVariant => m_Variants[m_CurrentVariantIndex];
        public bool hasVariants => m_Variants.Count > 1;

        public ToolButton(IReadOnlyList<EditorTool> variants) : this(Tool.Custom, variants) {}

        public ToolButton(Tool targetTool, IReadOnlyList<EditorTool> variants)
        {
            m_TargetTool = targetTool;
            m_Variants = new List<EditorTool>(variants);
            m_Variants.Sort((a, b) =>
            {
                var aa = EditorToolUtility.GetMetaData(a.GetType()).variantPriority;
                var bb = EditorToolUtility.GetMetaData(b.GetType()).variantPriority;
                if (aa == ToolAttribute.defaultPriority && bb == ToolAttribute.defaultPriority)
                    return a.GetType().GetHashCode().CompareTo(b.GetType().GetHashCode());
                return aa.CompareTo(bb);
            });
            m_CurrentVariantIndex = GetPreferredVariantIndex();
            name = currentVariant.GetType().Name;

            AddToClassList("unity-tool-button");

            this.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue)
                    ToolManager.SetActiveTool(currentVariant);

                // Keep the toggle checked if target is still the current tool
                if (ToolManager.IsActiveTool(currentVariant))
                    SetValueWithoutNotify(true);
            });

            this.RemoveManipulator(m_Clickable); //We handle clicking for the variant dropdown

            if (hasVariants)
            {
                var variantIcon = new VisualElement();
                variantIcon.AddToClassList("unity-tool-button__variant-icon");
                Add(variantIcon);
            }

            UpdateState();
            UpdateContent();
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            //at the top of the function to ensure we don't get weird behavior where the scene view steals the input while right clicking
            evt.StopPropagation();

            if (evt.button != 0)
                return;

            if (m_ToolVariantDropdown != null)
                return;

            this.CaptureMouse();

            if (hasVariants)
                m_OpenMenuScheduler = schedule.Execute(OpenVariantsDropdown).StartingIn(k_DelayBeforeOpenDropdown);
        }

        void OpenVariantsDropdown()
        {
            var canvas = GetOverlayCanvas();
            canvas.Add(m_ToolVariantDropdown = new VisualElement());
            m_ToolVariantDropdown.AddToClassList("tool-variant-dropdown");
            m_HoveredVariantIndex = -1;

            m_ToolVariantDropdown.style.position = Position.Absolute;
            for (int i = 0; i < m_Variants.Count; ++i)
                m_ToolVariantDropdown.Add(CreateVariantElement(i));

            m_ToolVariantDropdown.RegisterCallback<GeometryChangedEvent>(PlaceVariantDropdown);
        }

        void PlaceVariantDropdown(GeometryChangedEvent evt)
        {
            if (m_ToolVariantDropdown == null)
                return;

            m_ToolVariantDropdown.UnregisterCallback<GeometryChangedEvent>(PlaceVariantDropdown);

            var buttonRect = m_ToolVariantDropdown.parent.WorldToLocal(this.worldBound);
            var containerRect = m_ToolVariantDropdown.parent.rect;
            var size = evt.newRect.size;

            var resultRect = new Rect(Vector2.zero, size);

            // Place vertically when button is horizontal
            if (IsParentVerticalToolbar())
            {
                var leftSpace = buttonRect.xMin - containerRect.xMin;
                var rightSpace = containerRect.xMax - buttonRect.xMax;
                var hasMoreSpaceRight = rightSpace >= leftSpace;

                resultRect.x = hasMoreSpaceRight ? buttonRect.xMax : buttonRect.xMin - size.x;
                resultRect.y = buttonRect.center.y - size.y * .5f;
            }
            else
            {
                var aboveSpace = buttonRect.yMin - containerRect.yMin;
                var underSpace = containerRect.yMax - buttonRect.yMax;
                var hasMoreSpaceUnder = underSpace >= aboveSpace;

                resultRect.x = buttonRect.center.x - size.x * .5f;
                resultRect.y = hasMoreSpaceUnder ? buttonRect.yMax : buttonRect.yMin - size.y;
            }

            // Clamp to container
            if (resultRect.xMax > containerRect.xMax)
                resultRect.x -= resultRect.xMax - containerRect.xMax;

            if (resultRect.yMax > containerRect.yMax)
                resultRect.y -= resultRect.yMax - containerRect.yMax;

            resultRect.x = Mathf.Max(resultRect.x, 0);
            resultRect.y = Mathf.Max(resultRect.y, 0);

            m_ToolVariantDropdown.style.left = resultRect.x;
            m_ToolVariantDropdown.style.top = resultRect.y;
        }

        bool IsParentVerticalToolbar()
        {
            var current = parent;
            while (current != null)
            {
                if (current.ClassListContains(Overlays.Overlay.k_ToolbarVerticalLayout))
                    return true;
                current = current.parent;
            }

            return false;
        }

        VisualElement CreateVariantElement(int variantIndex)
        {
            var root = new VisualElement();
            root.AddToClassList("tool-variant-dropdown__item");
            var content = EditorToolUtility.GetToolbarIcon(m_Variants[variantIndex]);

            var checkmark = new VisualElement();
            checkmark.AddToClassList("tool-variant-dropdown__item-checkmark");
            checkmark.style.backgroundImage = EditorGUIUtility.LoadIconRequired("checkmark");
            checkmark.visible = m_CurrentVariantIndex == variantIndex;
            root.Add(checkmark);

            var icon = new VisualElement();
            icon.AddToClassList("tool-variant-dropdown__item-icon");
            icon.style.backgroundImage = content.image as Texture2D;
            root.Add(icon);

            var label = new Label(L10n.Tr(content.tooltip));
            root.Add(label);

            root.RegisterCallback<MouseOutEvent>((evt) =>
            {
                root.RemoveFromClassList("tool-variant-dropdown__item--hover");
                m_HoveredVariantIndex = -1;
            });
            root.RegisterCallback<MouseOverEvent>((evt) =>
            {
                root.AddToClassList("tool-variant-dropdown__item--hover");
                m_HoveredVariantIndex = variantIndex;
            });

            return root;
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button != 0)
                return;

            m_OpenMenuScheduler?.Pause();

            // If the button was clicked and released, activate current variant
            if (m_ToolVariantDropdown != null)
            {
                if (m_HoveredVariantIndex >= 0)
                    m_CurrentVariantIndex = m_HoveredVariantIndex;

                ToolManager.SetActiveTool(currentVariant);
                UpdateContent();

                m_ToolVariantDropdown?.RemoveFromHierarchy();
                m_ToolVariantDropdown = null;
            }
            else
            {
                if (value && m_TargetTool == Tool.Custom)
                    ToolManager.RestorePreviousTool();
                else    
                    ToolManager.SetActiveTool(currentVariant);
            }

            this.ReleaseMouse();
            evt.StopPropagation();
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ToolManager.activeToolChanged += UpdateState;
            ToolManager.activeContextChanged += UpdateState;
            SceneViewMotion.viewToolActiveChanged += UpdateState;

            if (m_TargetTool == Tool.View)
            {
                Tools.viewToolChanged += UpdateViewToolContent;
                UpdateViewToolContent();
            }
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ToolManager.activeContextChanged -= UpdateState;
            ToolManager.activeToolChanged -= UpdateState;
            SceneViewMotion.viewToolActiveChanged -= UpdateState;

            if (m_TargetTool == Tool.View)
                Tools.viewToolChanged -= UpdateViewToolContent;
        }

        void UpdateViewToolContent()
        {
            switch (Tools.viewTool)
            {
                case ViewTool.Orbit:
                    onIcon = s_OrbitViewOnIcon;
                    offIcon = s_OrbitViewIcon;
                    break;
                case ViewTool.Pan:
                    onIcon = s_PanViewOnIcon;
                    offIcon = s_PanViewIcon;
                    break;
                case ViewTool.Zoom:
                    onIcon = s_ZoomViewOnIcon;
                    offIcon = s_ZoomViewIcon;
                    break;
                case ViewTool.FPS:
                    onIcon = s_FpsViewOnIcon;
                    offIcon = s_FpsViewIcon;
                    break;
            }
        }

        void UpdateContent()
        {
            switch (m_TargetTool)
            {
                case Tool.View:
                    UpdateViewToolContent();
                    break;
                case Tool.Move:
                    tooltip = L10n.Tr("Move Tool");
                    offIcon = s_DefaultMoveIcon;
                    onIcon = s_DefaultMoveOnIcon;
                    break;
                case Tool.Rotate:
                    tooltip = L10n.Tr("Rotate Tool");
                    offIcon = s_DefaultRotateIcon;
                    onIcon = s_DefaultRotateOnIcon;
                    break;
                case Tool.Scale:
                    tooltip = L10n.Tr("Scale Tool");
                    offIcon = s_DefaultScaleIcon;
                    onIcon = s_DefaultScaleOnIcon;
                    break;
                case Tool.Transform:
                    tooltip = L10n.Tr("Transform Tool");
                    offIcon = s_DefaultTransformIcon;
                    onIcon = s_DefaultTransformOnIcon;
                    break;
                case Tool.Rect:
                    tooltip = L10n.Tr("Rect Tool");
                    offIcon = s_DefaultRectIcon;
                    onIcon = s_DefaultRectOnIcon;
                    break;
                case Tool.Custom:
                    var content = EditorToolUtility.GetToolbarIcon(currentVariant);
                    tooltip = L10n.Tr(content.tooltip);
                    if (content.image == null && !string.IsNullOrEmpty(content.text))
                    {
                        textIcon = content.text;
                    }
                    else
                    {
                        onIcon = offIcon = content.image as Texture2D;
                        text = content.text;
                    }
                    break;
            }
        }

        void UpdateState()
        {
            SetValueWithoutNotify(IsActiveTool());

            var missing = EditorToolUtility.GetEditorToolWithEnum(m_TargetTool) is NoneTool;
            var display = missing ? DisplayStyle.None : DisplayStyle.Flex;

            if (style.display != display)
            {
                style.display = display;
                displayChanged?.Invoke();
            }
        }

        bool IsActiveTool()
        {
            if (Tools.viewToolActive)
                return m_TargetTool == Tool.View;
            return ToolManager.IsActiveTool(currentVariant);
        }

        VisualElement GetOverlayCanvas()
        {
            return GetRootVisualContainer().Q("unity-overlay-canvas");
        }

        int GetPreferredVariantIndex()
        {
            if (m_Variants.Count < 1 || m_Variants[0] == null)
                return 0;
            var meta = EditorToolUtility.GetMetaData(m_Variants[0].GetType());
            if (meta.variantGroup == null)
                return 0;
            var pref = EditorToolManager.instance.variantPrefs.GetPreferredVariant(meta.variantGroup);
            for(int i = 0, c = m_Variants.Count; i < c; ++i)
                if (m_Variants[i]?.GetType() == pref)
                    return i;
            return 0;
        }

        internal static void LoadBuiltinToolsIcons()
        {
            s_PanViewIcon = EditorGUIUtility.LoadIconRequired("ViewToolMove");
            s_PanViewOnIcon = EditorGUIUtility.LoadIconRequired("ViewToolMove On");
            s_FpsViewIcon = s_OrbitViewIcon = EditorGUIUtility.LoadIconRequired("ViewToolOrbit");
            s_FpsViewOnIcon = s_OrbitViewOnIcon = EditorGUIUtility.LoadIconRequired("ViewToolOrbit On");
            s_ZoomViewIcon = EditorGUIUtility.LoadIconRequired("ViewToolZoom");
            s_ZoomViewOnIcon = EditorGUIUtility.LoadIconRequired("ViewToolZoom On");
            s_DefaultMoveIcon = EditorGUIUtility.LoadIconRequired("MoveTool");
            s_DefaultMoveOnIcon = EditorGUIUtility.LoadIconRequired("MoveTool On");
            s_DefaultRotateIcon = EditorGUIUtility.LoadIconRequired("RotateTool");
            s_DefaultRotateOnIcon = EditorGUIUtility.LoadIconRequired("RotateTool On");
            s_DefaultScaleIcon = EditorGUIUtility.LoadIconRequired("ScaleTool");
            s_DefaultScaleOnIcon = EditorGUIUtility.LoadIconRequired("ScaleTool On");
            s_DefaultTransformIcon = EditorGUIUtility.LoadIconRequired("TransformTool");
            s_DefaultTransformOnIcon = EditorGUIUtility.LoadIconRequired("TransformTool On");
            s_DefaultRectIcon = EditorGUIUtility.FindTexture("RectTool");
            s_DefaultRectOnIcon = EditorGUIUtility.FindTexture("RectTool On");
        }
    }
}
