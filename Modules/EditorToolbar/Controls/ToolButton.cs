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
        static readonly string s_UssClassName_MoveTool = "builtin-tool-move";
        static readonly string s_UssClassName_RotateTool = "builtin-tool-rotate";
        static readonly string s_UssClassName_ScaleTool = "builtin-tool-scale";
        static readonly string s_UssClassName_TransformTool = "builtin-tool-transform";
        static readonly string s_UssClassName_RectTool = "builtin-tool-rect";
        static readonly string s_UssClassName_PanViewTool = "builtin-tool-pan-view";
        static readonly string s_UssClassName_OrbitViewTool = "builtin-tool-orbit-view";
        static readonly string s_UssClassName_FpsViewTool = "builtin-tool-fps-view";
        static readonly string s_UssClassName_ZoomViewTool = "builtin-tool-zoom-view";

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
            
            // We only need the state to auto-refresh for custom tools.
            // For the built-in tools, we can refresh internally using RefreshAvailableTools if needed.
            if (!IsBuiltinTool())
                EditorApplication.update += UpdateState;

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
            
            if (!IsBuiltinTool())
                EditorApplication.update -= UpdateState;

            if (m_TargetTool == Tool.View)
                Tools.viewToolChanged -= UpdateViewToolContent;
        }

        void UpdateViewToolContent()
        {
            switch (Tools.viewTool)
            {
                case ViewTool.Orbit:
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_OrbitViewTool);
                    break;
                case ViewTool.Pan:
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_PanViewTool);
                    break;
                case ViewTool.Zoom:
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_ZoomViewTool);
                    break;
                case ViewTool.FPS:
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_FpsViewTool);
                    break;
            }
        }

        void UpdateContent()
        {
            switch (m_TargetTool)
            {
                case Tool.View:
                    tooltip = L10n.Tr("View Tool");
                    UpdateViewToolContent();
                    break;
                case Tool.Move:
                    tooltip = L10n.Tr("Move Tool");
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_MoveTool);
                    break;
                case Tool.Rotate:
                    tooltip = L10n.Tr("Rotate Tool");
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_RotateTool);
                    break;
                case Tool.Scale:
                    tooltip = L10n.Tr("Scale Tool");
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_ScaleTool);
                    break;
                case Tool.Transform:
                    tooltip = L10n.Tr("Transform Tool");
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_TransformTool);
                    break;
                case Tool.Rect:
                    tooltip = L10n.Tr("Rect Tool");
                    ClearButtonClassList();
                    AddToClassList(s_UssClassName_RectTool);
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

        void ClearButtonClassList()
        {
            RemoveFromClassList(s_UssClassName_MoveTool);
            RemoveFromClassList(s_UssClassName_RotateTool);
            RemoveFromClassList(s_UssClassName_ScaleTool);
            RemoveFromClassList(s_UssClassName_TransformTool);
            RemoveFromClassList(s_UssClassName_RectTool);

            RemoveFromClassList(s_UssClassName_PanViewTool);
            RemoveFromClassList(s_UssClassName_OrbitViewTool);
            RemoveFromClassList(s_UssClassName_FpsViewTool);
            RemoveFromClassList(s_UssClassName_ZoomViewTool);
        }

        void UpdateState()
        {
            SetValueWithoutNotify(IsActiveTool());

            var missing = EditorToolUtility.GetEditorToolWithEnum(m_TargetTool) is NoneTool;
            var display = missing ? DisplayStyle.None : DisplayStyle.Flex;
            var enabled = currentVariant.IsAvailable();

            if (style.display != display)
            {
                style.display = display;
                displayChanged?.Invoke();
            }

            if (enabledSelf != enabled)
                enabledSelf = enabled;
        }

        bool IsActiveTool()
        {
            if (Tools.viewToolActive)
                return m_TargetTool == Tool.View;
            return ToolManager.IsActiveTool(currentVariant);
        }

        bool IsBuiltinTool()
        {
            return EditorToolUtility.IsManipulationTool(m_TargetTool) || (m_TargetTool == Tool.View);
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
    }
}
