// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.Experimental.GraphView
{
    public class IconBadge : VisualElement
    {
        static CustomStyleProperty<int> s_DistanceProperty = new CustomStyleProperty<int>("--distance");

        private VisualElement m_TipElement;
        private VisualElement m_IconElement;
        private Label m_TextElement;

        protected SpriteAlignment alignment { get; set; }
        protected VisualElement target { get; set; }

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

        public string visualStyle
        {
            get
            {
                return m_BadgeType;
            }
            set
            {
                if (m_BadgeType != value)
                {
                    string modifier = "--" + m_BadgeType;

                    RemoveFromClassList(ussClassName + modifier);

                    m_TipElement?.RemoveFromClassList(tipUssClassName + modifier);
                    m_IconElement?.RemoveFromClassList(iconUssClassName + modifier);
                    m_TextElement?.RemoveFromClassList(textUssClassName + modifier);

                    m_BadgeType = value;

                    modifier = "--" + m_BadgeType;

                    AddToClassList(ussClassName + modifier);

                    m_TipElement?.AddToClassList(tipUssClassName + modifier);
                    m_IconElement?.AddToClassList(iconUssClassName + modifier);
                    m_TextElement?.AddToClassList(textUssClassName + modifier);
                }
            }
        }

        private const int kDefaultDistanceValue = 6;

        private int m_Distance;
        private bool m_DistanceIsInline;
        public int distance
        {
            get { return m_Distance; }
            set
            {
                m_DistanceIsInline = true;
                if (value != m_Distance)
                {
                    m_Distance = value;
                    if (m_Attacher != null)
                        m_Attacher.distance = m_Distance;
                }
            }
        }

        private int m_CurrentTipAngle = 0;

        private Attacher m_Attacher = null;
        private bool m_IsAttached;
        private VisualElement m_originalParent;

        private Attacher m_TextAttacher = null;
        private string m_BadgeType;

        public IconBadge()
        {
            m_IsAttached = false;
            m_Distance = kDefaultDistanceValue;
            var tpl = EditorGUIUtility.Load("GraphView/Badge/IconBadge.uxml") as VisualTreeAsset;

            LoadTemplate(tpl);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            visualStyle = "error";
        }

        public IconBadge(VisualTreeAsset template)
        {
            m_IsAttached = false;
            m_Distance = kDefaultDistanceValue;
            LoadTemplate(template);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            visualStyle = "error";
        }

        internal static readonly string ussClassName = "icon-badge";
        internal static readonly string iconUssClassName = ussClassName + "__icon";
        internal static readonly string tipUssClassName = ussClassName + "__tip";
        internal static readonly string textUssClassName = ussClassName + "__text";

        private static readonly string defaultStylePath = "GraphView/Badge/IconBadge.uss";

        private void LoadTemplate(VisualTreeAsset tpl)
        {
            tpl.CloneTree(this, new Dictionary<string, VisualElement>());

            InitBadgeComponent("tip", tipUssClassName, ref m_TipElement);
            InitBadgeComponent("icon", iconUssClassName, ref m_IconElement);
            InitBadgeComponent("text", textUssClassName, ref m_TextElement);

            if (m_TipElement != null)
            {
                ////we make sure the tip is in the back
                m_TipElement.SendToBack();
            }

            name = "IconBadge";
            AddToClassList(ussClassName);

            AddStyleSheetPath(defaultStylePath);

            if (m_TextElement != null)
            {
                m_TextElement.RemoveFromHierarchy();
                //we need to add the style sheet to the Text element as well since it will be parented elsewhere
                m_TextElement.AddStyleSheetPath(defaultStylePath);
                m_TextElement.style.whiteSpace = WhiteSpace.Normal;
                m_TextElement.RegisterCallback<GeometryChangedEvent>((evt) => ComputeTextSize());
                m_TextElement.pickingMode = PickingMode.Ignore;
            }
        }

        private void InitBadgeComponent<ElementType>(string name, string ussClassName, ref ElementType outElement) where ElementType : VisualElement
        {
            outElement = this.Q<ElementType>(name);

            if (outElement == null)
            {
                Debug.Log($"IconBadge: Couldn't load {name} element from template");
            }
            else
            {
                outElement.AddToClassList(ussClassName);
            }
        }

        public static IconBadge CreateError(string message)
        {
            var result = new IconBadge();
            result.visualStyle = "error";
            result.badgeText = message;
            return result;
        }

        public static IconBadge CreateComment(string message)
        {
            var result = new IconBadge();
            result.visualStyle = "comment";
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
                m_originalParent = hierarchy.parent;
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
                    m_originalParent.hierarchy.Add(this);
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
            m_Attacher.distance = distance;
        }

        private void ComputeTextSize()
        {
            if (m_TextElement != null)
            {
                float maxWidth = m_TextElement.resolvedStyle.maxWidth == StyleKeyword.None ? float.NaN : m_TextElement.resolvedStyle.maxWidth.value;
                Vector2 newSize = m_TextElement.DoMeasure(maxWidth, MeasureMode.AtMost,
                    0, MeasureMode.Undefined);

                m_TextElement.style.width = newSize.x +
                    m_TextElement.resolvedStyle.marginLeft +
                    m_TextElement.resolvedStyle.marginRight +
                    m_TextElement.resolvedStyle.borderLeftWidth +
                    m_TextElement.resolvedStyle.borderRightWidth +
                    m_TextElement.resolvedStyle.paddingLeft +
                    m_TextElement.resolvedStyle.paddingRight;

                float height = newSize.y +
                    m_TextElement.resolvedStyle.marginTop +
                    m_TextElement.resolvedStyle.marginBottom +
                    m_TextElement.resolvedStyle.borderTopWidth +
                    m_TextElement.resolvedStyle.borderBottomWidth +
                    m_TextElement.resolvedStyle.paddingTop +
                    m_TextElement.resolvedStyle.paddingBottom;

                m_TextElement.style.height = height;

                if (m_TextAttacher != null)
                {
                    m_TextAttacher.offset = new Vector2(0, height);
                }

                PerformTipLayout();
            }
        }

        private void ShowText()
        {
            if (m_TextElement != null && m_TextElement.hierarchy.parent == null)
            {
                VisualElement textParent = this;

                GraphView gv = GetFirstAncestorOfType<GraphView>();
                if (gv != null)
                {
                    textParent = gv;
                }

                textParent.Add(m_TextElement);

                if (textParent != this)
                {
                    if (m_TextAttacher == null)
                    {
                        m_TextAttacher = new Attacher(m_TextElement, m_IconElement, SpriteAlignment.TopRight);
                    }
                    else
                    {
                        m_TextAttacher.Reattach();
                    }
                }
                m_TextAttacher.distance = 0;
                m_TextElement.ResetPositionProperties();

                ComputeTextSize();
            }
        }

        private void HideText()
        {
            if (m_TextElement != null && m_TextElement.hierarchy.parent != null)
            {
                m_TextAttacher?.Detach();
                m_TextElement.RemoveFromHierarchy();
            }
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
            {
                if (m_Attacher != null)
                    PerformTipLayout();
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
            {
                if (m_Attacher != null)
                {
                    m_Attacher.Detach();
                    m_Attacher = null;
                }

                HideText();
            }
            else if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                //we make sure we sit on top of whatever siblings we have
                BringToFront();
                ShowText();
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId())
            {
                HideText();
            }


            base.ExecuteDefaultAction(evt);
        }

        private void PerformTipLayout()
        {
            float contentWidth = resolvedStyle.width;

            float arrowWidth = 0;
            float arrowLength = 0;

            if (m_TipElement != null)
            {
                arrowWidth = m_TipElement.resolvedStyle.width;
                arrowLength = m_TipElement.resolvedStyle.height;
            }

            float iconSize = (m_IconElement != null) ? m_IconElement.computedStyle.width.GetSpecifiedValueOrDefault(contentWidth - arrowLength) : 0f;

            float arrowOffset = Mathf.Floor((iconSize - arrowWidth) * 0.5f);

            Rect iconRect = new Rect(0, 0, iconSize, iconSize);
            float iconOffset = Mathf.Floor((contentWidth - iconSize) * 0.5f);

            Rect tipRect = new Rect(0, 0, arrowWidth, arrowLength);

            int tipAngle = 0;
            Vector2 tipTranslate = Vector2.zero;
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
                if (m_TextElement.parent == this)
                {
                    m_TextElement.style.position = Position.Absolute;
                    m_TextElement.style.left = iconRect.xMax;
                    m_TextElement.style.top = iconRect.y;
                }
            }
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            int dist = 0;
            if (!m_DistanceIsInline && e.customStyle.TryGetValue(s_DistanceProperty, out dist))
                m_Distance = dist;
        }
    }
}
