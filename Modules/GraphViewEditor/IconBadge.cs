// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class IconBadge : VisualElement
    {
        private VisualElement m_TipElement;
        private VisualElement m_IconElement;
        private Label m_TextElement;

        protected SpriteAlignment alignment { get;  set; }
        protected VisualElement target { get;  set; }

        public string badgeText
        {
            get
            {
                return (m_TextElement != null) ? m_TextElement.text : string.Empty;
            }
            set
            {
                if (m_TextElement != null)
                {
                    m_TextElement.text = value;
                }
            }
        }

        private const int kDefaultDistanceValue = 6;

        private StyleValue<int> m_Distance;
        public StyleValue<int> distance
        {
            get { return m_Distance; }
            set
            {
                int oldValue = m_Distance;
                if (m_Distance.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity))
                {
                    if (oldValue != m_Distance)
                    {
                        Dirty(ChangeType.Layout);
                    }
                }
            }
        }

        private int m_CurrentTipAngle = 0;

        private Attacher m_Attacher = null;
        private bool m_IsAttached;
        private VisualElement m_originalParent;
        public IconBadge()
        {
            m_IsAttached = false;
            var tpl = EditorGUIUtility.Load("GraphView/Badge/IconBadge.uxml") as VisualTreeAsset;

            LoadTemplate(tpl);
        }

        public IconBadge(VisualTreeAsset template)
        {
            LoadTemplate(template);
        }

        private void LoadTemplate(VisualTreeAsset tpl)
        {
            tpl.CloneTree(this, new Dictionary<string, VisualElement>());

            m_IconElement = this.Q<Image>("icon");
            m_TipElement = this.Q<Image>("tip");
            m_TextElement = this.Q<Label>("text");

            if (m_IconElement == null)
            {
                Debug.Log("IconBadge: Couldn't load icon element from template");
            }

            if (m_TipElement == null)
            {
                Debug.Log("IconBadge: Couldn't load tip element from template");
            }

            if (m_TextElement == null)
            {
                Debug.Log("IconBadge: Couldn't load text element from template");
            }

            if (m_TipElement != null)
            {
                ////we make sure the tip is in the back
                VisualElement tipParent = m_TipElement.shadow.parent;
                m_TipElement.RemoveFromHierarchy();
                tipParent.shadow.Insert(0, m_TipElement);
            }

            name = "IconBadge";
            AddToClassList("iconBadge");

            AddStyleSheetPath("GraphView/Badge/IconBadge.uss");

            if (m_TextElement != null)
            {
                m_TextElement.RemoveFromHierarchy();
                m_TextElement.style.wordWrap = true;
                m_TextElement.RegisterCallback<PostLayoutEvent>((evt) => ComputeTextSize());

                clippingOptions = ClippingOptions.NoClipping;
            }
        }

        public static IconBadge CreateError(string message)
        {
            var result = new IconBadge();
            result.AddToClassList("error");
            result.badgeText = message;
            return result;
        }

        public static IconBadge CreateComment(string message)
        {
            var result = new IconBadge();
            result.AddToClassList("comment");
            result.badgeText = message;
            return result;
        }

        public void AttachTo(VisualElement target, SpriteAlignment align)
        {
            Detach();
            this.alignment = align;
            this.target = target;
            m_IsAttached = true;
            target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
            CreateAttacher();
        }

        public void Detach()
        {
            if (m_IsAttached)
            {
                target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                m_IsAttached = false;
            }
            ReleaseAttacher();
            m_originalParent = null;
        }

        private void OnTargetDetachedFromPanel(DetachFromPanelEvent evt)
        {
            ReleaseAttacher();
            if (m_IsAttached)
            {
                m_originalParent = shadow.parent;
                RemoveFromHierarchy();

                target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);
                target.RegisterCallback<AttachToPanelEvent>(OnTargetAttachedToPanel);
            }
        }

        private void OnTargetAttachedToPanel(AttachToPanelEvent evt)
        {
            if (m_IsAttached)
            {
                target.RegisterCallback<DetachFromPanelEvent>(OnTargetDetachedFromPanel);

                //we re-add ourselves to the hierarchy
                if (m_originalParent != null)
                {
                    m_originalParent.shadow.Add(this);
                }
                if (m_Attacher != null)
                {
                    ReleaseAttacher();
                }
                CreateAttacher();
            }
        }

        private void ReleaseAttacher()
        {
            if (m_Attacher != null)
            {
                m_Attacher.Detach();
                m_Attacher = null;
            }
        }

        private void CreateAttacher()
        {
            m_Attacher = new Attacher(this, target, alignment);
            m_Attacher.distance = distance.GetSpecifiedValueOrDefault(kDefaultDistanceValue);
        }

        private void ComputeTextSize()
        {
            if (m_TextElement != null)
            {
                Vector2 newSize = m_TextElement.DoMeasure(m_TextElement.style.maxWidth, MeasureMode.AtMost,
                        0, MeasureMode.Undefined);

                m_TextElement.style.width = newSize.x +
                    m_TextElement.style.marginLeft +
                    m_TextElement.style.marginRight +
                    m_TextElement.style.borderLeft +
                    m_TextElement.style.borderRight +
                    m_TextElement.style.paddingLeft +
                    m_TextElement.style.paddingRight;
                m_TextElement.style.height = newSize.y +
                    m_TextElement.style.marginTop +
                    m_TextElement.style.marginBottom +
                    m_TextElement.style.borderTop +
                    m_TextElement.style.borderBottom +
                    m_TextElement.style.paddingTop +
                    m_TextElement.style.paddingBottom;
                PerformTipLayout();
            }
        }

        private void ShowText()
        {
            if (m_TextElement != null && m_TextElement.shadow.parent == null)
            {
                Add(m_TextElement);
                m_TextElement.ResetPositionProperties();
                ComputeTextSize();
            }
        }

        private void HideText()
        {
            if (m_TextElement != null && m_TextElement.shadow.parent != null)
            {
                m_TextElement.RemoveFromHierarchy();
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.GetEventTypeId() == PostLayoutEvent.TypeId())
            {
                if (m_Attacher != null)
                    PerformTipLayout();
            }
            else if (evt.GetEventTypeId() == DetachFromPanelEvent.TypeId())
            {
                if (m_Attacher != null)
                {
                    m_Attacher.Detach();
                    m_Attacher = null;
                }
            }
            else if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
            {
                ShowText();
            }
            else if (evt.GetEventTypeId() == MouseLeaveEvent.TypeId())
            {
                HideText();
            }


            base.ExecuteDefaultAction(evt);
        }

        private void PerformTipLayout()
        {
            float contentWidth = style.width;

            float arrowWidth = 0;
            float arrowLength = 0;

            if (m_TipElement != null)
            {
                arrowWidth = m_TipElement.style.width;
                arrowLength = m_TipElement.style.height;
            }

            float iconSize = (m_IconElement != null) ? m_IconElement.style.width.GetSpecifiedValueOrDefault(contentWidth - arrowLength) : 0;

            float arrowOffset = Mathf.Floor((iconSize - arrowWidth) * 0.5f);

            Rect iconRect = new Rect(0, 0, iconSize, iconSize);
            float iconOffset = Mathf.Floor((contentWidth - iconSize) * 0.5f);

            Rect tipRect = new Rect(0, 0, arrowWidth, arrowLength);

            int tipAngle = 0;
            Vector2 tipTranslate =  Vector2.zero;
            bool tipVisible = true;

            switch (alignment)
            {
                case SpriteAlignment.TopCenter:
                    iconRect.x = iconOffset;
                    iconRect.y = 0;
                    tipRect.x = iconOffset + arrowOffset;
                    tipRect.y = iconRect.height;
                    tipTranslate = new Vector2(arrowWidth, arrowLength);
                    tipAngle = 180;
                    break;
                case SpriteAlignment.LeftCenter:
                    iconRect.y = iconOffset;
                    tipRect.x = iconRect.width;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(arrowLength, 0);
                    tipAngle = 90;
                    break;
                case SpriteAlignment.RightCenter:
                    iconRect.y = iconOffset;
                    iconRect.x += arrowLength;
                    tipRect.y = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(0, arrowWidth);
                    tipAngle = 270;
                    break;
                case SpriteAlignment.BottomCenter:
                    iconRect.x = iconOffset;
                    iconRect.y = arrowLength;
                    tipRect.x = iconOffset + arrowOffset;
                    tipTranslate = new Vector2(0, 0);
                    tipAngle = 0;
                    break;
                default:
                    tipVisible = false;
                    break;
            }

            if (tipAngle != m_CurrentTipAngle)
            {
                if (m_TipElement != null)
                {
                    m_TipElement.transform.rotation = Quaternion.Euler(new Vector3(0, 0, tipAngle));
                    m_TipElement.transform.position = new Vector3(tipTranslate.x, tipTranslate.y, 0);
                }
                m_CurrentTipAngle = tipAngle;
            }


            if (m_IconElement != null)
                m_IconElement.layout = iconRect;

            if (m_TipElement != null)
            {
                m_TipElement.layout = tipRect;

                if (m_TipElement.visible != tipVisible)
                {
                    m_TipElement.visible = tipVisible;
                }
            }

            if (m_TextElement != null)
            {
                m_TextElement.style.positionType = PositionType.Absolute;
                m_TextElement.style.positionLeft = iconRect.xMax;
                m_TextElement.style.positionTop = iconRect.y;
            }
        }

        protected override void OnStyleResolved(ICustomStyle elementStyle)
        {
            base.OnStyleResolved(elementStyle);
            elementStyle.ApplyCustomProperty("distance", ref m_Distance);
        }
    }
}
