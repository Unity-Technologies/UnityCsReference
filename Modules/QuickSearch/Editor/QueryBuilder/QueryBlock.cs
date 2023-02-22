// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    public abstract class QueryBlock : VisualElement, IBlockSource
    {
        [Obsolete]
        protected internal const float arrowOffset = 5f;
        [Obsolete]
        protected internal const float blockExtraPadding = 4f;
        protected internal const float blockHeight = 20f;
        [Obsolete]
        protected internal const float borderRadius = 8f;
        [Obsolete]
        protected internal Rect arrowRect { get; set; }
        public new string name { get => base.name; protected set => base.name = value; }

        internal static readonly string ussClassName = "search-query-block";
        internal static readonly string disabledClassName = ussClassName.WithUssElement("disabled");
        internal static readonly string excludedClassName = ussClassName.WithUssElement("excluded");
        internal static readonly string noBackgroundClassName = ussClassName.WithUssElement("no-background");
        internal static readonly string iconClassName = ussClassName.WithUssElement("icon");
        internal static readonly string separatorClassName = ussClassName.WithUssElement("separator");
        internal static readonly string arrowButtonClassName = "search-query-open-arrow";

        private bool m_Selected = false;
        private Vector3 m_InitiateDragPosition = default;
        private Vector3 m_InitiateDragTargetPosition = default;

        public IQuerySource source { get; private set; }
        public SearchContext context => source.context; // TODO: Can this be removed from here?
        internal IBlockEditor editor { get; set; }

        public string value { get; set; }
        [Obsolete]
        protected internal Rect valueRect { get; set; }
        public string op { get; protected set; }
        internal bool explicitQuotes { get; set; }
        bool IBlockSource.formatNames => formatNames;
        internal virtual bool formatNames => true;
        internal virtual bool wantsEvents => false;
        internal virtual bool canExclude => true;
        internal virtual bool canDisable => true;
        internal virtual bool canOpenEditorOnValueClicked => false;
        internal virtual bool draggable => true;
        internal bool hideMenu { get; set; }
        internal bool disabled { get; set; }
        internal bool @readonly { get; set; }
        internal bool disableHovering { get; set; }
        internal bool excluded { get; set; }
        internal bool selected
        {
            get => m_Selected;
            set
            {
                m_Selected = value;
                if (m_Selected)
                    pseudoStates |= PseudoStates.Checked;
                else
                    pseudoStates &= ~PseudoStates.Checked;
                UpdateBackgroundColor();
            }
        }
        internal string editorTitle { get; set; }

        internal Rect drawRect => worldBound;

        string IBlockSource.name => name;
        string IBlockSource.editorTitle => editorTitle;
        SearchContext IBlockSource.context => context;

        internal QueryBlock(IQuerySource source)
        {
            this.source = source;

            AddToClassList(ussClassName);

            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);

            RegisterCallback<PointerUpEvent>(OnBlockClicked);
            RegisterCallback<ContextClickEvent>(OnContextClick);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnBlockDragged);
            RegisterCallback<PointerUpEvent>(OnPointerUp, useTrickleDown: TrickleDown.TrickleDown);
            RegisterCallback<PointerCaptureOutEvent>(OnDragExited);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(name))
                return value;
            if (string.IsNullOrEmpty(value))
                return name;
            return $"{name}={value}";
        }

        internal void Delete()
        {
            source.RemoveBlock(this);
            parent?.Remove(this);
        }

        void OpenMenu(Event evt)
        {
            var menu = new GenericMenu();

            if (!@readonly)
            {
                if (canDisable)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Enable"), !disabled, ToggleDisabled);

                if (canExclude)
                    menu.AddItem(EditorGUIUtility.TrTextContent("Exclude"), excluded, ToggleExcluded);
            }

            if (menu.GetItemCount() > 0)
                menu.AddSeparator("");
            var bc = menu.GetItemCount();
            AddContextualMenuItems(menu);
            if (!@readonly)
            {
                if (menu.GetItemCount() != bc)
                    menu.AddSeparator("");
                menu.AddItem(EditorGUIUtility.TrTextContent("Delete"), false, Delete);
            }

            if (menu.GetItemCount() > 0)
            {
                menu.ShowAsContext();
            }
        }

        private void ToggleDisabled()
        {
            disabled = !disabled;
            ApplyChanges();
        }

        internal virtual void AddContextualMenuItems(GenericMenu menu) {}

        private bool OpenEditor(Event evt, in Rect rect)
        {
            if (editor == null)
            {
                editor = OpenEditor(rect);
                if (editor != null)
                {
                    UpdateOpenEditorStyles(opened: true);
                    evt?.Use();
                    return true;
                }
            }
            else if (editor.window)
            {
                editor.window.Close();
                editor = null;
                UpdateOpenEditorStyles(opened: false);
            }

            return false;
        }

        internal virtual IBlockEditor OpenEditor(in Rect rect)
        {
            return QuerySelector.Open(rect, this);
        }

        internal void CloseEditor()
        {
            editor = null;
            context?.searchView?.Repaint();
            UpdateOpenEditorStyles(opened: false);
        }

        private void UpdateOpenEditorStyles(bool opened = false)
        {
            if (opened)
            {
                style.borderBottomLeftRadius = 0f;
                style.borderBottomRightRadius = 0f;
            }
            else
            {
                style.borderBottomLeftRadius = new StyleLength(StyleKeyword.Null);
                style.borderBottomRightRadius = new StyleLength(StyleKeyword.Null);
            }
        }

        private void ToggleExcluded()
        {
            excluded = !excluded;
            ApplyChanges();
        }

        internal virtual Color GetBackgroundColor() => Color.red;

        internal string EscapeLiteralString(in string sv)
        {
            if (string.IsNullOrEmpty(sv))
                return "\"\"";
            if (explicitQuotes || value.IndexOfAny(new[] { ' ', '/', '*' }) != -1)
                return '"' + sv + '"';
            return sv;
        }

        internal void SetOperator(in string op)
        {
            this.op = op;
            ApplyChanges();
        }

        public virtual void Apply(in SearchProposition searchProposition) => throw new NotSupportedException($"Cannot apply {searchProposition} for {this} control");
        internal virtual IEnumerable<SearchProposition> FetchPropositions() => throw new NotSupportedException($"Cannot fetch propositions for {this} control");

        void IBlockSource.Apply(in SearchProposition searchProposition)
        {
            Apply(searchProposition);
            UpdateGUI();
        }

        internal void ApplyChanges()
        {
            source.Apply();
            UpdateGUI();
            source.Repaint();
        }

        internal virtual void UpdateGUI()
        {
            if (parent != null)
                CreateGUI();
        }

        IEnumerable<SearchProposition> IBlockSource.FetchPropositions()
        {
            return FetchPropositions();
        }

        void IBlockSource.CloseEditor()
        {
            CloseEditor();
        }

        internal VisualElement CreateGUI()
        {
            Clear();

            this.SetClassState(hideMenu, noBackgroundClassName);
            this.SetClassState(disabled, disabledClassName);
            this.SetClassState(excluded, excludedClassName);

            UpdateBackgroundColor();
            CreateBlockElement(this);
            return this;
        }

        internal virtual void CreateBlockElement(VisualElement container)
        {
            if (!string.IsNullOrEmpty(name))
            {
                AddLabel(container, name);
                AddSeparator(container);
            }
            AddLabel(container, value);

            if (!@readonly)
                AddOpenEditorArrow(container);
        }

        internal void AddSeparator(VisualElement container)
        {
            container.Add(new BlockSeparator());
        }

        internal void AddIcon(VisualElement container, in Texture icon)
        {
            var imgIcon = new Image() { image = icon };
            imgIcon.AddToClassList(iconClassName);
            container.Add(imgIcon);
        }

        internal Label AddLabel(VisualElement container, string text)
        {
            if (excluded)
                text = $"<s>{text}</s>";
            var label = new Label(text);
            if (string.IsNullOrEmpty(tooltip) && !string.Equals(text, tooltip))
                label.tooltip = tooltip;
            container.Add(label);
            return label;
        }

        internal void AddOpenEditorArrow(VisualElement container)
        {
            if (@readonly)
                return;
            AddImageButton(container, EditorGUIUtility.LoadGeneratedIconOrNormalIcon("icon dropdown"));
        }

        internal Image AddImageButton(VisualElement container, Texture image, in string tooltip = null, EventCallback<ClickEvent> handler = null)
        {
            var imgButton = new Image() { image = image };
            imgButton.tooltip = tooltip;
            imgButton.AddToClassList(arrowButtonClassName);
            imgButton.RegisterCallback<ClickEvent>(handler ?? OnOpenBlockEditor);
            container.Add(imgButton);
            return imgButton;
        }

        private void OnContextClick(ContextClickEvent evt)
        {
            if (!@readonly && !hideMenu)
                OpenMenu(evt.imguiEvent);
        }

        private void OnBlockClicked(PointerUpEvent evt)
        {
            if (evt.button == 2 && !@readonly)
                Utils.CallDelayed(Delete);
            else if (evt.button == 0 && evt.altKey && canDisable)
                ToggleDisabled();
            else if (evt.button == 0 && (evt.commandKey || evt.ctrlKey) && canExclude)
                ToggleExcluded();
            else
                source.BlockActivated(this);
        }

        internal void OnOpenBlockEditor(ClickEvent evt)
        {
            if (evt.target is VisualElement ve)
            {
                var openRect = ve.parent.worldBound;
                openRect.x -= 1f;
                OpenEditor(evt.imguiEvent, openRect);
            }
        }

        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            UpdateBackgroundColor();
        }

        private void OnMouseEnter(MouseEnterEvent evt)
        {
            UpdateBackgroundColor(hovered: true);
        }

        internal virtual void UpdateBackgroundColor(bool hovered = false)
        {
            var bgColor = GetBackgroundColor();
            if (m_Selected || hovered)
                bgColor *= QueryColors.selectedTint;
            style.backgroundColor = bgColor;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;
            if (!draggable)
                return;
            m_InitiateDragPosition = evt.position;
            m_InitiateDragTargetPosition = transform.position;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            m_InitiateDragPosition = default;

            if (!this.HasPointerCapture(evt.pointerId))
                return;

            this.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void OnBlockDragged(PointerMoveEvent evt)
        {
            Vector3 pointerDelta = evt.position - m_InitiateDragPosition;

            if (this.HasPointerCapture(evt.pointerId))
            {
                transform.position = new Vector3(m_InitiateDragTargetPosition.x + pointerDelta.x, m_InitiateDragTargetPosition.y + pointerDelta.y, 0.5f);

                // Check if we should switch position with another block
                var targetIndex = parent.IndexOf(this);
                foreach (var b in source.EnumerateBlocks())
                {
                    if (!draggable || (!b.canDisable && !b.canExclude))
                        continue;

                    if (b != this && b.worldBound.Overlaps(worldBound))
                    {
                        b.style.opacity = 0.5f;
                        var swapWithIndex = parent.IndexOf(b);
                        var moveLeft = (Mathf.Abs(worldBound.xMin - b.worldBound.xMin) < 5f && targetIndex + 1 != swapWithIndex);
                        var moveRight = (Mathf.Abs(worldBound.xMax - b.worldBound.xMax) < 5f && targetIndex - 1 != swapWithIndex);
                        if (moveLeft || moveRight)
                        {
                            if (!source.SwapBlock(this, b))
                                continue;

                            m_InitiateDragPosition = evt.position;
                            m_InitiateDragTargetPosition = b.transform.position;

                            if (moveLeft)
                                PlaceBehind(b);
                            else
                                PlaceInFront(b);

                            transform.position = m_InitiateDragTargetPosition;
                            break;
                        }
                    }
                    else
                        b.style.opacity = new StyleFloat(StyleKeyword.Null);
                }
            }
            else
            {
                if (m_InitiateDragPosition == default)
                    return;

                if (pointerDelta.sqrMagnitude < 10f)
                    return;

                this.Focus();
                this.CapturePointer(evt.pointerId);
            }
        }

        private void OnDragExited(PointerCaptureOutEvent evt)
        {
            transform.position = new Vector3(0, 0, m_InitiateDragTargetPosition.z);
            m_InitiateDragPosition = default;
            m_InitiateDragTargetPosition = default;

            foreach (var b in source.EnumerateBlocks())
                b.style.opacity = new StyleFloat(StyleKeyword.Null);
        }
    }
}
