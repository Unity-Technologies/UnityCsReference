// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define ENABLE_CAPTURE_DEBUG
using System;
using System.Collections.Generic;
using UnityEngine.CSSLayout;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    internal delegate void OnStylesResolved(ICustomStyle styles);

    // pseudo states are used for common states of a widget
    // they are addressable from CSS via the pseudo state syntax ":selected" for example
    // while css class list can solve the same problem, pseudo states are a fast commonly agreed upon path for common cases.
    [Flags]
    internal enum PseudoStates
    {
        Active    = 1 << 0,     // control is currently pressed in the case of a button
        Hover     = 1 << 1,     // mouse is over control, set and removed from dispatcher automatically
        Checked   = 1 << 3,     // usually associated with toggles of some kind to change visible style
        Selected  = 1 << 4,     // selected, used to denote the current selected state and associate a visual style from CSS
        Disabled  = 1 << 5,     // control will not respond to user input
        Focus     = 1 << 6,     // control has the keyboard focus. This is activated deactivated by the dispatcher automatically

        Invisible = 1 << 31,    // special. not enabled via uss. activate to skip render
    }

    public enum PickingMode
    {
        Position, // todo better name
        Ignore
    }

    public partial class VisualElement : CallbackEventHandler
    {
        private static uint s_NextId;

        string m_Name;
        HashSet<string> m_ClassList;
        string m_TypeName;
        string m_FullTypeName;

        public bool usePixelCaching { get; set; }

        private RenderData m_RenderData;
        internal RenderData renderData
        {
            get { return m_RenderData ?? (m_RenderData = new RenderData()); }
        }

        // the transform
        internal Matrix4x4 m_Transform = Matrix4x4.identity;
        public Matrix4x4 transform
        {
            get
            {
                return m_Transform;
            }
            set
            {
                if (m_Transform == value)
                    return;
                m_Transform = value;
                Dirty(ChangeType.Transform);
            }
        }

        Rect m_Layout;

        // Temporary obsolete so that we can use position with the transform property.
        [Obsolete("Use the property layout instead (UnityUpgradable) -> layout", false)]
        public Rect position
        {
            get
            {
                return layout;
            }
            set
            {
                layout = value;
            }
        }

        // This will replace the Rect position
        // origin and size relative to parent
        public Rect layout
        {
            get
            {
                var result = m_Layout;
                if (cssNode != null && style.positionType.value != PositionType.Manual)
                {
                    result.x = cssNode.LayoutX;
                    result.y = cssNode.LayoutY;
                    result.width = cssNode.LayoutWidth;
                    result.height = cssNode.LayoutHeight;
                }
                return result;
            }
            set
            {
                if (cssNode == null)
                {
                    cssNode = new CSSNode();
                }

                // Same position value while type is already manual should not trigger any layout change, return early
                if (style.positionType.value == PositionType.Manual && m_Layout == value)
                    return;

                // set results so we can read straight back in get without waiting for a pass
                m_Layout = value;

                // mark as inline so that they do not get overridden if needed.
                IStyle styleAccess = this;
                styleAccess.positionType = PositionType.Manual;
                styleAccess.marginLeft = 0.0f;
                styleAccess.marginRight = 0.0f;
                styleAccess.marginBottom = 0.0f;
                styleAccess.marginTop = 0.0f;
                styleAccess.positionLeft = value.x;
                styleAccess.positionTop = value.y;
                styleAccess.positionRight = float.NaN;
                styleAccess.positionBottom = float.NaN;
                styleAccess.width = value.width;
                styleAccess.height = value.height;

                Dirty(ChangeType.Layout);
            }
        }

        public Rect contentRect
        {
            get
            {
                var spacing = new Spacing(m_Style.paddingLeft,
                        m_Style.paddingTop,
                        m_Style.paddingRight,
                        m_Style.paddingBottom);

                return paddingRect - spacing;
            }
        }

        protected Rect paddingRect
        {
            get
            {
                var spacing = new Spacing(borderLeftWidth,
                        borderTopWidth,
                        borderRightWidth,
                        borderBottomWidth);

                return layout - spacing;
            }
        }

        // get the AA aligned bound
        public Rect globalBound
        {
            get
            {
                var g = globalTransform;
                var min = g.MultiplyPoint3x4(layout.min);
                var max = g.MultiplyPoint3x4(layout.max);
                return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
            }
        }

        public Rect localBound
        {
            get
            {
                var g = transform;
                var min = g.MultiplyPoint3x4(layout.min);
                var max = g.MultiplyPoint3x4(layout.max);
                return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
            }
        }

        public Matrix4x4 globalTransform
        {
            get
            {
                if (IsDirty(ChangeType.Transform))
                {
                    {
                        if (parent != null)
                        {
                            renderData.worldTransForm = parent.globalTransform * Matrix4x4.Translate(new Vector3(parent.layout.x, parent.layout.y, 0)) * transform;
                        }
                        else
                        {
                            renderData.worldTransForm = transform;
                        }
                        ClearDirty(ChangeType.Transform);
                    }
                }
                return renderData.worldTransForm;
            }
        }

        private PseudoStates m_PseudoStates;
        internal PseudoStates pseudoStates
        {
            get { return m_PseudoStates; }
            set
            {
                if (m_PseudoStates != value)
                {
                    m_PseudoStates = value;
                    Dirty(ChangeType.Styles);
                }
            }
        }

        // parent in visual tree
        private VisualContainer m_Parent;
        public VisualContainer parent
        {
            get { return m_Parent; }
            set
            {
                m_Parent = value;

                if (value != null)
                {
                    ChangePanel(m_Parent.elementPanel);
                    // This is needed because Dirty on the child might have called before setting the parent, which cause the any Dirty on the child after the parent is assigned to not do anything ( because flags are already set on it ).
                    PropagateChangesToParents();
                }
                else
                    ChangePanel(null);
            }
        }

        // each element has a ref to the root panel for internal bookkeeping
        // this will be null until a visual tree is added to a panel
        internal BaseVisualElementPanel elementPanel { get; private set; }

        public override IPanel panel { get { return elementPanel; } }

        public PickingMode pickingMode { get; set; }

        // does not guarantee uniqueness
        public string name
        {
            get { return m_Name; }
            set
            {
                if (m_Name == value)
                    return;
                m_Name = value;
                Dirty(ChangeType.Styles);
            }
        }

        internal string fullTypeName
        {
            get
            {
                if (string.IsNullOrEmpty(m_FullTypeName))
                    m_FullTypeName = GetType().FullName;
                return m_FullTypeName;
            }
        }

        internal string typeName
        {
            get
            {
                if (string.IsNullOrEmpty(m_TypeName))
                    m_TypeName = GetType().Name;
                return m_TypeName;
            }
        }

        // Set and pass in values to be used for layout
        internal CSSNode cssNode { get; private set; }

        // shared style object, cannot be changed by the user
        internal VisualElementStylesData m_SharedStyle = VisualElementStylesData.none;
        // user-defined style object, if not set, is the same reference as m_SharedStyles
        internal VisualElementStylesData m_Style = VisualElementStylesData.none;

        [Obsolete("OnStylesResolved(ICustomStyles) has been deprecated and will be removed. Use OnStyleResolved(ICustomStyle) instead", false)]
        public virtual void OnStylesResolved(ICustomStyles style) {}

        public virtual void OnStyleResolved(ICustomStyle style)
        {
            // push all non inlined layout things up
            FinalizeLayout();
        }

        internal VisualElementStylesData sharedStyle
        {
            get
            {
                return m_SharedStyle;
            }
        }

        internal VisualElementStylesData effectiveStyle
        {
            get
            {
                return m_Style;
            }
        }

        internal bool hasInlineStyle
        {
            get
            {
                return m_Style != m_SharedStyle;
            }
        }

        VisualElementStylesData inlineStyle
        {
            get
            {
                if (!hasInlineStyle)
                {
                    var inline = new VisualElementStylesData(false);
                    inline.Apply(m_SharedStyle, StylePropertyApplyMode.Copy);
                    m_Style = inline;
                }
                return m_Style;
            }
        }

        [Obsolete("VisualElement.width will be removed. Use VisualElement.style.width instead", false)]
        public float width
        {
            get
            {
                return style.width;
            }
            set
            {
                style.width = value;
            }
        }

        [Obsolete("VisualElement.height will be removed. Use VisualElement.style.height instead", false)]
        public float height
        {
            get
            {
                return style.height;
            }
            set
            {
                style.height = value;
            }
        }

        [Obsolete("VisualElement.maxWidth will be removed. Use VisualElement.style.maxWidth instead", false)]
        public float maxWidth
        {
            get
            {
                return style.maxWidth;
            }
            set
            {
                style.maxWidth = value;
            }
        }

        [Obsolete("VisualElement.maxHeight will be removed. Use VisualElement.style.maxHeight instead", false)]
        public float maxHeight
        {
            get
            {
                return style.maxHeight;
            }
            set
            {
                style.maxHeight = value;
            }
        }

        [Obsolete("VisualElement.minWidth will be removed. Use VisualElement.style.minWidth instead", false)]
        public float minWidth
        {
            get
            {
                return style.minWidth;
            }
            set
            {
                style.minWidth = value;
            }
        }

        [Obsolete("VisualElement.minHeight will be removed. Use VisualElement.style.minHeight instead", false)]
        public float minHeight
        {
            get
            {
                return style.minHeight;
            }
            set
            {
                style.minHeight = value;
            }
        }

        [Obsolete("VisualElement.flex will be removed. Use VisualElement.style.flex instead", false)]
        public float flex
        {
            get
            {
                return style.flex;
            }
            set
            {
                style.flex = value;
            }
        }

        [Obsolete("VisualElement.positionLeft will be removed. Use VisualElement.style.positionLeft instead", false)]
        public float positionLeft
        {
            get
            {
                return style.positionLeft;
            }
            set
            {
                style.positionLeft = value;
            }
        }

        [Obsolete("VisualElement.positionTop will be removed. Use VisualElement.style.positionTop instead", false)]
        public float positionTop
        {
            get
            {
                return style.positionTop;
            }
            set
            {
                style.positionTop = value;
            }
        }

        [Obsolete("VisualElement.positionRight will be removed. Use VisualElement.style.positionRight instead", false)]
        public float positionRight
        {
            get
            {
                return style.positionRight;
            }
            set
            {
                style.positionRight = value;
            }
        }

        [Obsolete("VisualElement.positionBottom will be removed. Use VisualElement.style.positionBottom instead", false)]
        public float positionBottom
        {
            get
            {
                return style.positionBottom;
            }
            set
            {
                style.positionBottom = value;
            }
        }

        [Obsolete("VisualElement.marginLeft will be removed. Use VisualElement.style.marginLeft instead", false)]
        public float marginLeft
        {
            get
            {
                return style.marginLeft;
            }
            set
            {
                style.marginLeft = value;
            }
        }

        [Obsolete("VisualElement.marginTop will be removed. Use VisualElement.style.marginTop instead", false)]
        public float marginTop
        {
            get
            {
                return style.marginTop;
            }
            set
            {
                style.marginTop = value;
            }
        }

        [Obsolete("VisualElement.marginRight will be removed. Use VisualElement.style.marginRight instead", false)]
        public float marginRight
        {
            get
            {
                return style.marginRight;
            }
            set
            {
                style.marginRight = value;
            }
        }

        [Obsolete("VisualElement.marginBottom will be removed. Use VisualElement.style.marginBottom instead", false)]
        public float marginBottom
        {
            get
            {
                return style.marginBottom;
            }
            set
            {
                style.marginBottom = value;
            }
        }

        [Obsolete("VisualElement.borderLeft will be removed. Use VisualElement.style.borderLeft instead", false)]
        public float borderLeft
        {
            get
            {
                return style.borderLeft;
            }
            set
            {
                style.borderLeft = value;
            }
        }

        [Obsolete("VisualElement.borderTop will be removed. Use VisualElement.style.borderTop instead", false)]
        public float borderTop
        {
            get
            {
                return style.borderTop;
            }
            set
            {
                style.borderTop = value;
            }
        }

        [Obsolete("VisualElement.borderRight will be removed. Use VisualElement.style.borderRight instead", false)]
        public float borderRight
        {
            get
            {
                return style.borderRight;
            }
            set
            {
                style.borderRight = value;
            }
        }

        [Obsolete("VisualElement.borderBottom will be removed. Use VisualElement.style.borderBottom instead", false)]
        public float borderBottom
        {
            get
            {
                return style.borderBottom;
            }
            set
            {
                style.borderBottom = value;
            }
        }

        [Obsolete("VisualElement.paddingLeft will be removed. Use VisualElement.style.paddingLeft instead", false)]
        public float paddingLeft
        {
            get
            {
                return style.paddingLeft;
            }
            set
            {
                style.paddingLeft = value;
            }
        }

        [Obsolete("VisualElement.paddingTop will be removed. Use VisualElement.style.paddingTop instead", false)]
        public float paddingTop
        {
            get
            {
                return style.paddingTop;
            }
            set
            {
                style.paddingTop = value;
            }
        }

        [Obsolete("VisualElement.paddingRight will be removed. Use VisualElement.style.paddingRight instead", false)]
        public float paddingRight
        {
            get
            {
                return style.paddingRight;
            }
            set
            {
                style.paddingRight = value;
            }
        }

        [Obsolete("VisualElement.paddingBottom will be removed. Use VisualElement.style.paddingBottom instead", false)]
        public float paddingBottom
        {
            get
            {
                return style.paddingBottom;
            }
            set
            {
                style.paddingBottom = value;
            }
        }

        [Obsolete("VisualElement.positionType will be removed. Use VisualElement.style.positionType instead", false)]
        public PositionType positionType
        {
            get
            {
                return (PositionType)m_Style.positionType.value;
            }
            set
            {
                style.positionType = value;
            }
        }

        [Obsolete("VisualElement.backgroundSize will be removed. Use VisualElement.style.backgroundSize instead", false)]
        public ScaleMode backgroundSize
        {
            get
            {
                return style.backgroundSize.value;
            }
            set
            {
                style.backgroundSize = value;
            }
        }

        [Obsolete("VisualElement.alignSelf will be removed. Use VisualElement.style.alignSelf instead", false)]
        public Align alignSelf
        {
            get
            {
                return style.alignSelf.value;
            }
            set
            {
                style.alignSelf = value;
            }
        }

        [Obsolete("VisualElement.textAlignment will be removed. Use VisualElement.style.textAlignment instead", false)]
        public TextAnchor textAlignment
        {
            get
            {
                return style.textAlignment.value;
            }
            set
            {
                style.textAlignment = value;
            }
        }

        [Obsolete("VisualElement.fontStyle will be removed. Use VisualElement.style.fontStyle instead", false)]
        public FontStyle fontStyle
        {
            get
            {
                return style.fontStyle.value;
            }
            set
            {
                style.fontStyle = value;
            }
        }

        [Obsolete("VisualElement.textClipping will be removed. Use VisualElement.style.textClipping instead", false)]
        public TextClipping textClipping
        {
            get
            {
                return style.textClipping.value;
            }
            set
            {
                style.textClipping = value;
            }
        }

        [Obsolete("VisualElement.font will be removed. Use VisualElement.style.font instead", false)]
        public Font font
        {
            get
            {
                return style.font;
            }
            set
            {
                style.font = value;
            }
        }

        [Obsolete("VisualElement.fontSize will be removed. Use VisualElement.style.fontSize instead", false)]
        public int fontSize
        {
            get
            {
                return style.fontSize;
            }
            set
            {
                style.fontSize = value;
            }
        }

        [Obsolete("VisualElement.wordWrap will be removed. Use VisualElement.style.wordWrap instead", false)]
        public bool wordWrap
        {
            get
            {
                return style.wordWrap;
            }
            set
            {
                style.wordWrap = value;
            }
        }

        [Obsolete("VisualElement.backgroundImage will be removed. Use VisualElement.style.backgroundImage instead", false)]
        public Texture2D backgroundImage
        {
            get
            {
                return style.backgroundImage;
            }
            set
            {
                style.backgroundImage = value;
            }
        }

        [Obsolete("VisualElement.textColor will be removed. Use VisualElement.style.textColor instead", false)]
        public Color textColor
        {
            get
            {
                return style.textColor.GetSpecifiedValueOrDefault(Color.black);
            }
            set
            {
                style.textColor = value;
            }
        }

        [Obsolete("VisualElement.backgroundColor will be removed. Use VisualElement.style.backgroundColor instead", false)]
        public Color backgroundColor
        {
            get
            {
                return style.backgroundColor;
            }
            set
            {
                style.backgroundColor = value;
            }
        }

        [Obsolete("VisualElement.borderColor will be removed. Use VisualElement.style.borderColor instead", false)]
        public Color borderColor
        {
            get
            {
                return style.borderColor;
            }
            set
            {
                style.borderColor = value;
            }
        }

        public float borderLeftWidth
        {
            get
            {
                return style.borderLeftWidth;
            }
            set
            {
                style.borderLeftWidth = value;
            }
        }

        public float borderTopWidth
        {
            get
            {
                return style.borderTopWidth;
            }
            set
            {
                style.borderTopWidth = value;
            }
        }

        public float borderRightWidth
        {
            get
            {
                return style.borderRightWidth;
            }
            set
            {
                style.borderRightWidth = value;
            }
        }

        public float borderBottomWidth
        {
            get
            {
                return style.borderBottomWidth;
            }
            set
            {
                style.borderBottomWidth = value;
            }
        }

        [Obsolete("VisualElement.borderRadius will be removed. Use VisualElement.style.borderRadius instead", false)]
        public float borderRadius
        {
            get
            {
                return style.borderRadius;
            }
            set
            {
                style.borderRadius = value;
            }
        }

        [Obsolete("VisualElement.overflow will be removed. Use VisualElement.style.overflow instead", false)]
        public Overflow overflow
        {
            get
            {
                return style.overflow.value;
            }
            set
            {
                style.overflow = value;
            }
        }

        // Opacity is not fully supported so it's hidden from public API for now
        internal float opacity
        {
            get
            {
                return style.opacity.value;
            }
            set
            {
                style.opacity = value;
            }
        }

        private List<IManipulator> m_Manipulators = new List<IManipulator>();

        internal readonly uint controlid;

        public VisualElement()
        {
            controlid = ++s_NextId;

            m_ClassList = new HashSet<string>();
            m_FullTypeName = string.Empty;
            m_TypeName = string.Empty;
            enabled = true;
            visible = true;
            name = string.Empty;
            cssNode = new CSSNode();
            cssNode.SetMeasureFunction(Measure);
            changesNeeded = 0; // not in a tree yet so not dirty, they will stack up as we get inserted somewhere.
        }

        internal List<IManipulator>.Enumerator GetManipulatorsInternal()
        {
            if (m_Manipulators != null)
                return m_Manipulators.GetEnumerator();
            return default(List<IManipulator>.Enumerator);
        }

        public void InsertManipulator(int index, IManipulator manipulator)
        {
            if (m_Manipulators == null)
                m_Manipulators = new List<IManipulator>();
            if (!m_Manipulators.Contains(manipulator))
            {
                manipulator.target = this;
                m_Manipulators.Insert(index, manipulator);
            }
        }

        public void AddManipulator(IManipulator manipulator)
        {
            if (m_Manipulators == null)
                m_Manipulators = new List<IManipulator>();
            if (!m_Manipulators.Contains(manipulator))
            {
                manipulator.target = this;
                m_Manipulators.Add(manipulator);
            }
        }

        public void RemoveManipulator(IManipulator manipulator)
        {
            manipulator.target = null;
            if (m_Manipulators != null)
            {
                m_Manipulators.Remove(manipulator);
            }
        }

        internal virtual void ChangePanel(BaseVisualElementPanel p)
        {
            if (panel == p)
                return;

            if (panel != null && onLeave != null)
            {
                onLeave();
            }

            elementPanel = p;

            if (panel != null && onEnter != null)
            {
                onEnter();
            }
        }

        // in the case of a Topology change the target is the parent of the removed or added element
        // TODO write test suite for this method. in particular, any change type should implicitly
        private ChangeType changesNeeded;

        // helper when change impact are known and we need to propagate down into children
        private void PropagateToChildren(ChangeType type)
        {
            // when touching we grass fire in the tree but stop when we are already touched.
            // this is a key point of the on demand dirty that keeps it from exploding.
            if ((type & changesNeeded) == type)
                return;

            changesNeeded |= type;

            // only those propagate to children
            type = type & (ChangeType.Styles | ChangeType.Transform);
            if (type == 0)
                return;

            // propagate to children
            var container = this as VisualContainer;
            if (container != null)
            {
                foreach (var child in container)
                {
                    // recurse down
                    child.PropagateToChildren(type);
                }
            }
        }

        private void PropagateChangesToParents()
        {
            ChangeType parentChanges = 0;
            if (changesNeeded != 0)
            {
                // if we have any change at all, propagate the repaint flag
                // this is somehow an implementation detail but this is the only flag that is checked in the app tick
                // this means that styles, layout, transform, etc. will be processed just before painting a panel
                // a small downside is that we might repaint more often that necessary
                // another solution would be to process all flags sequentially in the tick to check if a repaint is needed
                parentChanges |= ChangeType.Repaint;

                if ((changesNeeded & ChangeType.Styles) > 0)
                {
                    // if this visual element needs its styles recomputed, propagate this specific flags for its parents
                    // it is less expensive than a full styles re-pass
                    parentChanges |= ChangeType.StylesPath;
                }
            }

            var current = parent;
            while (current != null)
            {
                if ((current.changesNeeded & parentChanges) == parentChanges)
                    break;

                current.changesNeeded |= parentChanges;
                current = current.parent;
            }
        }

        public void Dirty(ChangeType type)
        {
            // when touching we grass fire in the tree but stop when we are already touched.
            // this is a key point of the on demand dirty that keeps it from exploding.
            if ((type & changesNeeded) == type)
                return;

            if ((type & ChangeType.Layout) > 0 && cssNode != null && cssNode.IsMeasureDefined)
                cssNode.MarkDirty();

            PropagateToChildren(type);

            PropagateChangesToParents();
        }

        public bool IsDirty(ChangeType type)
        {
            return (changesNeeded & type) > 0;
        }

        public void ClearDirty(ChangeType type)
        {
            changesNeeded &= ~type;
        }

        // A VisualElement should not animate or update until it is in a panel. Entering or leaving a panel is the right place to
        // register and unregister to IScheduler time events. It is the right place to robustly connect and disconnect from events in general
        //
        // OnEnterPanel is called:
        // 1) When a VisualElement is set as the visualTree of a Panel "Panel.visualTree = myVisualElement"
        // 2) When a VisualElement is added to a visualTree already in a panel. VisualContainer.AddChild() will call it before it returns
        // 3) When a VisualElement is anywhere in a subtree that meets the above two requirements
        // TODO will be renamed & moved to a unified event system in a later iteration
        public event Action onEnter;
        public event Action onLeave;

        [SerializeField]
        private string m_Text;
        public string text
        {
            get { return m_Text ?? String.Empty; }
            set { if (m_Text != value) { m_Text = value; Dirty(ChangeType.Layout); } }
        }

        public virtual bool enabled
        {
            get
            {
                return (pseudoStates & PseudoStates.Disabled) != PseudoStates.Disabled;
            }
            set
            {
                if (value)
                    pseudoStates &= ~PseudoStates.Disabled;
                else
                    pseudoStates |= PseudoStates.Disabled;
            }
        }

        protected internal virtual void OnPostLayout(bool hasNewLayout) {}

        public bool visible
        {
            get
            {
                return (pseudoStates & PseudoStates.Invisible) != PseudoStates.Invisible;
            }
            set
            {
                if (value)
                    pseudoStates &= ~PseudoStates.Invisible;
                else
                    pseudoStates |= PseudoStates.Invisible;
            }
        }

        public virtual void DoRepaint()
        {
            var painter = elementPanel.stylePainter;
            painter.DrawBackground(this);
            painter.DrawBorder(this);
            painter.DrawText(this);
        }

        internal virtual void DoRepaint(IStylePainter painter)
        {
            if ((pseudoStates & PseudoStates.Invisible) == PseudoStates.Invisible)
            {
                return;
            }
            DoRepaint();
        }

        // position should be in local space
        // override to customize intersection between point and shape
        public virtual bool ContainsPoint(Vector2 localPoint)
        {
            return layout.Contains(localPoint);
        }

        public virtual bool ContainsPointToLocal(Vector2 point)
        {
            return ContainsPoint(this.ChangeCoordinatesTo(parent, point));
        }

        public virtual bool Overlaps(Rect rectangle)
        {
            return layout.Overlaps(rectangle, true);
        }

        public enum MeasureMode
        {
            Undefined = CSSMeasureMode.Undefined,
            Exactly = CSSMeasureMode.Exactly,
            AtMost = CSSMeasureMode.AtMost
        }

        protected internal virtual Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            float measuredWidth;
            float measuredHeight;

            if (widthMode == MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                var textParams = new TextStylePainterParameters
                {
                    text = text,
                    wordWrapWidth = 0.0f,
                    wordWrap = false,
                    font = style.font,
                    fontSize = style.fontSize,
                    fontStyle = style.fontStyle,
                    anchor = style.textAlignment,
                    richText = true
                };

                measuredWidth = elementPanel.stylePainter.ComputeTextWidth(textParams);

                if (widthMode == MeasureMode.AtMost)
                {
                    measuredWidth = Mathf.Min(measuredWidth, width);
                }
            }

            if (heightMode == MeasureMode.Exactly)
            {
                measuredHeight = height;
            }
            else
            {
                var textParams = new TextStylePainterParameters
                {
                    text = text,
                    wordWrapWidth = measuredWidth,
                    wordWrap = style.wordWrap,
                    font = style.font,
                    fontSize = style.fontSize,
                    fontStyle = style.fontStyle,
                    anchor = style.textAlignment,
                    richText = true
                };

                measuredHeight = elementPanel.stylePainter.ComputeTextHeight(textParams);

                if (heightMode == MeasureMode.AtMost)
                {
                    measuredHeight = Mathf.Min(measuredHeight, height);
                }
            }
            return new Vector2(measuredWidth, measuredHeight);
        }

        internal long Measure(CSSNode node, float width, CSSMeasureMode widthMode, float height, CSSMeasureMode heightMode)
        {
            Debug.Assert(node == cssNode, "CSSNode instance mismatch");
            Vector2 size = DoMeasure(width, (MeasureMode)widthMode, height, (MeasureMode)heightMode);
            return MeasureOutput.Make(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
        }

        public void SetSize(Vector2 size)
        {
            var pos = layout;
            pos.width = size.x;
            pos.height = size.y;
            layout = pos;
        }

        internal const Align DefaultAlignContent = Align.FlexStart;
        internal const Align DefaultAlignItems = Align.Stretch;

        void FinalizeLayout()
        {
            cssNode.Flex = style.flex.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.SetPosition(CSSEdge.Left, style.positionLeft.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPosition(CSSEdge.Top, style.positionTop.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPosition(CSSEdge.Right, style.positionRight.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPosition(CSSEdge.Bottom, style.positionBottom.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Left, style.marginLeft.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Top, style.marginTop.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Right, style.marginRight.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Bottom, style.marginBottom.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Left, style.paddingLeft.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Top, style.paddingTop.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Right, style.paddingRight.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Bottom, style.paddingBottom.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetBorder(CSSEdge.Left, style.borderLeft.GetSpecifiedValueOrDefault(style.borderLeftWidth.GetSpecifiedValueOrDefault(float.NaN)));
            cssNode.SetBorder(CSSEdge.Top, style.borderTop.GetSpecifiedValueOrDefault(style.borderTopWidth.GetSpecifiedValueOrDefault(float.NaN)));
            cssNode.SetBorder(CSSEdge.Right, style.borderRight.GetSpecifiedValueOrDefault(style.borderRightWidth.GetSpecifiedValueOrDefault(float.NaN)));
            cssNode.SetBorder(CSSEdge.Bottom, style.borderBottom.GetSpecifiedValueOrDefault(style.borderBottomWidth.GetSpecifiedValueOrDefault(float.NaN)));
            cssNode.Width = style.width.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.Height = style.height.GetSpecifiedValueOrDefault(float.NaN);

            PositionType posType = style.positionType;
            switch (posType)
            {
                case PositionType.Absolute:
                case PositionType.Manual:
                    cssNode.PositionType = CSSPositionType.Absolute;
                    break;
                case PositionType.Relative:
                    cssNode.PositionType = CSSPositionType.Relative;
                    break;
            }

            cssNode.Overflow = (CSSOverflow)(style.overflow.value);
            cssNode.AlignSelf = (CSSAlign)(style.alignSelf.value);
            cssNode.MaxWidth = style.maxWidth.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.MaxHeight = style.maxHeight.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.MinWidth = style.minWidth.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.MinHeight = style.minHeight.GetSpecifiedValueOrDefault(float.NaN);

            // Note: the following applies to VisualContainer only
            // but it won't cause any trouble and we avoid making this method virtual
            cssNode.FlexDirection = (CSSFlexDirection)style.flexDirection.value;
            cssNode.AlignContent = (CSSAlign)style.alignContent.GetSpecifiedValueOrDefault(DefaultAlignContent);
            cssNode.AlignItems = (CSSAlign)style.alignItems.GetSpecifiedValueOrDefault(DefaultAlignItems);
            cssNode.JustifyContent = (CSSJustify)style.justifyContent.value;
            cssNode.Wrap = (CSSWrap)style.flexWrap.value;

            Dirty(ChangeType.Layout);
        }

        internal event OnStylesResolved onStylesResolved;

        // for internal use only, used by asset instantiation to push local styles
        // likely can be replaced by merging VisualContainer and VisualElement
        // and then storing the inline sheet in the list held by VisualContainer
        internal void SetInlineStyles(VisualElementStylesData inlineStyle)
        {
            Debug.Assert(!inlineStyle.isShared);
            m_Style = inlineStyle;
        }

        internal void SetSharedStyles(VisualElementStylesData sharedStyle)
        {
            Debug.Assert(sharedStyle.isShared);

            ClearDirty(ChangeType.StylesPath | ChangeType.Styles);
            if (sharedStyle == m_SharedStyle)
            {
                return;
            }

            if (hasInlineStyle)
            {
                m_Style.Apply(sharedStyle, StylePropertyApplyMode.CopyIfNotInline);
            }
            else
            {
                m_Style = sharedStyle;
            }

            m_SharedStyle = sharedStyle;

            if (onStylesResolved != null)
            {
                onStylesResolved(m_Style);
            }
            OnStyleResolved(m_Style);
            Dirty(ChangeType.Repaint);
        }

        public void ResetPositionProperties()
        {
            if (!hasInlineStyle)
            {
                return;
            }
            IStyle styleAccess = this.style;
            styleAccess.positionType = StyleValue<PositionType>.nil;
            styleAccess.marginLeft = StyleValue<float>.nil;
            styleAccess.marginRight = StyleValue<float>.nil;
            styleAccess.marginBottom = StyleValue<float>.nil;
            styleAccess.marginTop = StyleValue<float>.nil;
            styleAccess.positionLeft = StyleValue<float>.nil;
            styleAccess.positionTop = StyleValue<float>.nil;
            styleAccess.positionRight = StyleValue<float>.nil;
            styleAccess.positionBottom = StyleValue<float>.nil;
            styleAccess.width = StyleValue<float>.nil;
            styleAccess.height = StyleValue<float>.nil;

            // Make sure to retrieve shared styles from the shared style sheet and update CSSNode again
            m_Style.Apply(sharedStyle, StylePropertyApplyMode.CopyIfNotInline);
            FinalizeLayout();

            Dirty(ChangeType.Layout);
        }

        public override string ToString()
        {
            return name + " " + layout + " global rect: " + globalBound;
        }

        // WARNING returning the HashSet means it could be modified, be careful
        internal IEnumerable<string> GetClasses()
        {
            return m_ClassList;
        }

        public void ClearClassList()
        {
            if (m_ClassList != null && m_ClassList.Count > 0)
            {
                m_ClassList.Clear();
                Dirty(ChangeType.Styles);
            }
        }

        public void AddToClassList(string className)
        {
            if (m_ClassList == null)
                m_ClassList = new HashSet<string>();

            if (m_ClassList.Add(className))
            {
                Dirty(ChangeType.Styles);
            }
        }

        public void RemoveFromClassList(string className)
        {
            if (m_ClassList != null && m_ClassList.Remove(className))
            {
                Dirty(ChangeType.Styles);
            }
        }

        public bool ClassListContains(string cls)
        {
            return m_ClassList != null && m_ClassList.Contains(cls);
        }
    }

    public static class VisualElementExtensions
    {
        // transforms a point assumed in Panel space to the referential inside of the element bound
        public static Vector2 GlobalToBound(this VisualElement ele, Vector2 p)
        {
            var toLocal = ele.globalTransform.inverse.MultiplyPoint3x4(new Vector3(p.x, p.y, 0));
            return new Vector2(toLocal.x - ele.layout.position.x, toLocal.y - ele.layout.position.y);
        }

        // transforms a point assumed in Panel space to  local referential
        public static Vector2 LocalToGlobal(this VisualElement ele, Vector2 p)
        {
            var toGlobal = ele.globalTransform.MultiplyPoint3x4((Vector3)(p + ele.layout.position));
            return new Vector2(toGlobal.x, toGlobal.y);
        }

        // transforms a rect assumed in Panel space to  local referential
        public static Rect GlobalToBound(this VisualElement ele, Rect r)
        {
            var inv = ele.globalTransform.inverse;
            Vector2 position = inv.MultiplyPoint3x4((Vector2)r.position);
            r.position = position - ele.layout.position;
            r.size = inv.MultiplyPoint3x4(r.size);
            return r;
        }

        // transforms a rect to Panel space referential
        public static Rect LocalToGlobal(this VisualElement ele, Rect r)
        {
            var toGlobalMatrix = ele.globalTransform;
            r.position = toGlobalMatrix.MultiplyPoint3x4(ele.layout.position + r.position);
            r.size = toGlobalMatrix.MultiplyPoint3x4(r.size);
            return r;
        }

        // transform point from the local space of one element to to the local space of another
        public static Vector2 ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Vector2 point)
        {
            return dest.GlobalToBound(src.LocalToGlobal(point));
        }

        // transform Rect from the local space of one element to to the local space of another
        public static Rect ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Rect rect)
        {
            return dest.GlobalToBound(src.LocalToGlobal(rect));
        }

        public static void StretchToParentSize(this VisualElement elem)
        {
            IStyle styleAccess = elem.style;
            styleAccess.positionType = PositionType.Absolute;
            styleAccess.positionLeft = 0.0f;
            styleAccess.positionTop = 0.0f;
            styleAccess.positionRight = 0.0f;
            styleAccess.positionBottom = 0.0f;
        }

        public static T GetFirstOfType<T>(this VisualElement self) where T : class
        {
            T casted = self as T;
            if (casted != null)
                return casted;
            return GetFirstAncestorOfType<T>(self);
        }

        public static T GetFirstAncestorOfType<T>(this VisualElement self) where T : class
        {
            VisualElement ancestor = self.parent;
            while (ancestor != null)
            {
                T castedAncestor = ancestor as T;
                if (castedAncestor != null)
                {
                    return castedAncestor;
                }
                ancestor = ancestor.parent;
            }
            return null;
        }
    }

    internal static class StylePainterExtensionMethods
    {
        internal static void DrawBackground(this IStylePainter painter, VisualElement ve)
        {
            IStyle style = ve.style;

            if (style.backgroundColor != Color.clear)
            {
                var painterParams = new RectStylePainterParameters
                {
                    layout = ve.layout,
                    color = style.backgroundColor,
                    borderLeftWidth = 0.0f,
                    borderTopWidth = 0.0f,
                    borderRightWidth = 0.0f,
                    borderBottomWidth = 0.0f,
                    borderRadius = style.borderRadius,
                };
                painter.DrawRect(painterParams);
            }

            if (style.backgroundImage.value != null)
            {
                var painterParams = new TextureStylePainterParameters
                {
                    layout = ve.layout,
                    color = Color.white,
                    texture = style.backgroundImage,
                    scaleMode = style.backgroundSize,
                    borderLeftWidth = 0.0f,
                    borderTopWidth = 0.0f,
                    borderRightWidth = 0.0f,
                    borderBottomWidth = 0.0f,
                    borderRadius = style.borderRadius,
                    sliceLeft = style.sliceLeft,
                    sliceTop = style.sliceTop,
                    sliceRight = style.sliceRight,
                    sliceBottom = style.sliceBottom
                };
                painter.DrawTexture(painterParams);
            }
        }

        internal static void DrawBorder(this IStylePainter painter, VisualElement ve)
        {
            IStyle style = ve.style;
            if (style.borderColor != Color.clear && (style.borderLeftWidth > 0.0f || style.borderTopWidth > 0.0f || style.borderRightWidth > 0.0f || style.borderBottomWidth > 0.0f))
            {
                var painterParams = new RectStylePainterParameters
                {
                    layout = ve.layout,
                    color = ve.style.borderColor,
                    borderLeftWidth = ve.style.borderLeftWidth,
                    borderTopWidth = ve.style.borderTopWidth,
                    borderRightWidth = ve.style.borderRightWidth,
                    borderBottomWidth = ve.style.borderBottomWidth,
                    borderRadius = ve.style.borderRadius
                };
                painter.DrawRect(painterParams);
            }
        }

        internal static void DrawText(this IStylePainter painter, VisualElement ve)
        {
            if (!string.IsNullOrEmpty(ve.text) && ve.contentRect.width > 0.0f && ve.contentRect.height > 0.0f)
            {
                IStyle style = ve.style;
                var painterParams = new TextStylePainterParameters
                {
                    layout = ve.contentRect,
                    text = ve.text,
                    font = style.font,
                    fontSize = style.fontSize,
                    fontStyle = style.fontStyle,
                    fontColor = style.textColor,
                    anchor = style.textAlignment,
                    wordWrap = style.wordWrap,
                    wordWrapWidth = style.wordWrap ? ve.contentRect.width : 0.0f,
                    richText = false,
                    clipping = style.textClipping
                };
                painter.DrawText(painterParams);
            }
        }
    }
}
