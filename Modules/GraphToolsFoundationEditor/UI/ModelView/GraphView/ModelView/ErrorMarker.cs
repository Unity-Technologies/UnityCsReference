// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A marker that displays an error message.
    /// </summary>
    class ErrorMarker : Marker
    {
        public new static readonly string ussClassName = "ge-error-marker";
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");
        public static readonly string tipUssClassName = ussClassName.WithUssElement("tip");
        public static readonly string textUssClassName = ussClassName.WithUssElement("text");
        public static readonly string hasErrorUssClassName = "ge-has-error-marker";

        public static readonly string hiddenModifierUssClassName = ussClassName.WithUssModifier("hidden");
        public static readonly string arrowHiddenModifierUssClassName = ussClassName.WithUssModifier("tip-hidden");

        public static readonly string sideTopModifierUssClassName = ussClassName.WithUssModifier("top");
        public static readonly string sideRightModifierUssClassName = ussClassName.WithUssModifier("right");
        public static readonly string sideBottomModifierUssClassName = ussClassName.WithUssModifier("bottom");
        public static readonly string sideLeftModifierUssClassName = ussClassName.WithUssModifier("left");

        static readonly string k_DefaultStylePath = "ErrorMarker.uss";

        private static CustomStyleProperty<float> s_TextSizeProperty = new CustomStyleProperty<float>("--error-text-size");
        private static CustomStyleProperty<float> s_TextMaxWidthProperty = new CustomStyleProperty<float>("--error-text-max-width");

        public MarkerModel MarkerModel => Model as MarkerModel;
        public override GraphElementModel ParentModel => (Model as MarkerModel)?.GetParentModel(GraphView.GraphModel);

        protected Image m_TipElement;
        protected Image m_IconElement;
        protected Label m_TextElement;

        protected string m_MarkerType;

        float m_LastZoom;

        float ErrorTextSize { get; set; } = 12;
        float ErrorTextMaxWidth { get; set; } = 240;

        protected string VisualStyle
        {
            set
            {
                if (m_MarkerType != value)
                {
                    RemoveFromClassList(ussClassName.WithUssModifier(m_MarkerType));

                    m_MarkerType = value;

                    AddToClassList(ussClassName.WithUssModifier(m_MarkerType));
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            var changed = false;
            if (e.customStyle.TryGetValue(s_TextSizeProperty, out float textSize))
            {
                ErrorTextSize = textSize;
                changed = true;
            }

            if (e.customStyle.TryGetValue(s_TextMaxWidthProperty, out float maxWidth))
            {
                ErrorTextMaxWidth = maxWidth;
                changed = true;
            }

            if (changed && m_LastZoom != 0.0f)
                // recompute text scale next frame when the localBound is known.
                schedule.Execute(() => SetElementLevelOfDetail(m_LastZoom, GraphViewZoomMode.Unknown, GraphViewZoomMode.Unknown)).ExecuteLater(0);
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();
            name = "error-marker";

            m_TipElement = new Image { name = "tip" };
            Add(m_TipElement);
            m_TipElement.AddToClassList(tipUssClassName);

            m_IconElement = new Image { name = "icon" };
            Add(m_IconElement);
            m_IconElement.AddToClassList(iconUssClassName);

            m_TextElement = new Label { name = "text" };
            m_TextElement.AddToClassList(textUssClassName);
            m_TextElement.EnableInClassList(hiddenModifierUssClassName, true);
            Add(m_TextElement);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        }

        /// <inheritdoc />
        protected override void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            HideText();
            base.OnDetachedFromPanel(evt);
        }

        /// <inheritdoc />
        protected override void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (Attacher != null)
                PerformTipLayout();

            base.OnGeometryChanged(evt);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            this.AddStylesheet_Internal(k_DefaultStylePath);

            //we need to add the style sheet to the Text element as well since it will be parented elsewhere
            m_TextElement.AddStylesheet_Internal(k_DefaultStylePath);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is ErrorMarkerModel errorMarkerModel)
            {
                if (m_TextElement != null)
                {
                    m_TextElement.text = errorMarkerModel.ErrorMessage;
                }

                VisualStyle = errorMarkerModel.ErrorType.ToString().ToLower();
            }
        }

        /// <inheritdoc />
        protected override void Attach()
        {
            base.Attach();
            m_Target?.AddToClassList(hasErrorUssClassName);
        }

        /// <inheritdoc />
        protected override void Detach()
        {
            m_Target?.RemoveFromClassList(hasErrorUssClassName);
            base.Detach();
        }

        protected void ShowText()
        {
            if (m_TextElement?.hierarchy.parent != null && m_TextElement.ClassListContains(hiddenModifierUssClassName))
                m_TextElement?.EnableInClassList(hiddenModifierUssClassName, false);
        }

        protected void HideText()
        {
            if (m_TextElement?.hierarchy.parent != null && !m_TextElement.ClassListContains(hiddenModifierUssClassName))
                m_TextElement.EnableInClassList(hiddenModifierUssClassName, true);
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            //we make sure we sit on top of whatever siblings we have
            BringToFront();
            ShowText();
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            HideText();
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            var scale = 1.0f / zoom;

            m_TextElement.style.fontSize = ErrorTextSize * scale;
            m_TextElement.style.maxWidth = ErrorTextMaxWidth * scale;
            m_LastZoom = zoom;
        }

        void PerformTipLayout()
        {
            RemoveFromClassList(arrowHiddenModifierUssClassName);

            RemoveFromClassList(sideTopModifierUssClassName);
            RemoveFromClassList(sideRightModifierUssClassName);
            RemoveFromClassList(sideBottomModifierUssClassName);
            RemoveFromClassList(sideLeftModifierUssClassName);

            switch (Alignment)
            {
                case SpriteAlignment.TopCenter:
                    AddToClassList(sideTopModifierUssClassName);
                    break;
                case SpriteAlignment.LeftCenter:
                    AddToClassList(sideLeftModifierUssClassName);
                    break;
                case SpriteAlignment.RightCenter:
                    AddToClassList(sideRightModifierUssClassName);
                    break;
                case SpriteAlignment.BottomCenter:
                    AddToClassList(sideBottomModifierUssClassName);
                    break;
                default:
                    AddToClassList(arrowHiddenModifierUssClassName);
                    break;
            }
        }
    }
}
