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
    internal delegate void OnStylesResolved(VisualElementStyles styles);

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

    public class VisualElement : CallbackEventHandler
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

        Rect m_Position;

        // origin and size relative to parent
        public Rect position
        {
            get
            {
                var result = m_Position;
                if (cssNode != null && positionType != PositionType.Manual)
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
                if (positionType == PositionType.Manual && m_Position == value)
                    return;

                // set results so we can read straight back in get without waiting for a pass
                m_Position = value;

                // set styles
                cssNode.SetPosition(CSSEdge.Left, value.x);
                cssNode.SetPosition(CSSEdge.Top, value.y);
                cssNode.Width = value.width;
                cssNode.Height = value.height;

                // mark as inline so that they do not get overridden if needed.
                EnsureInlineStyles();
                m_Styles.positionType = Style<int>.Create((int)PositionType.Manual);
                m_Styles.marginLeft = Style<float>.Create(0.0f);
                m_Styles.marginRight = Style<float>.Create(0.0f);
                m_Styles.marginBottom = Style<float>.Create(0.0f);
                m_Styles.marginTop = Style<float>.Create(0.0f);
                m_Styles.positionLeft = Style<float>.Create(value.x);
                m_Styles.positionTop = Style<float>.Create(value.y);
                m_Styles.positionRight = Style<float>.Create(float.NaN);
                m_Styles.positionBottom = Style<float>.Create(float.NaN);
                m_Styles.width = Style<float>.Create(value.width);
                m_Styles.height = Style<float>.Create(value.height);

                Dirty(ChangeType.Layout);
            }
        }

        public Rect contentRect
        {
            get
            {
                var spacing = new Spacing(paddingLeft,
                        paddingTop,
                        paddingRight,
                        paddingBottom);

                return paddingRect - spacing;
            }
        }

        protected Rect paddingRect
        {
            get
            {
                var spacing = new Spacing(borderWidth,
                        borderWidth,
                        borderWidth,
                        borderWidth);

                return position - spacing;
            }
        }

        // get the AA aligned bound
        public Rect globalBound
        {
            get
            {
                var g = globalTransform;
                var min = g.MultiplyPoint3x4(position.min);
                var max = g.MultiplyPoint3x4(position.max);
                return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
            }
        }

        public Rect localBound
        {
            get
            {
                var g = transform;
                var min = g.MultiplyPoint3x4(position.min);
                var max = g.MultiplyPoint3x4(position.max);
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
                            renderData.worldTransForm = parent.globalTransform * Matrix4x4.Translate(new Vector3(parent.position.x, parent.position.y, 0)) * transform;
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
                    ChangePanel(m_Parent.elementPanel);
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
        // Most likely will be replaced by a custom structure when we support CSS
        internal CSSNode cssNode { get; private set; }

        internal VisualElementStyles m_Styles = VisualElementStyles.none;

        public virtual void OnStylesResolved(ICustomStyles styles)
        {
            // push all non inlined layout things up
            FinalizeLayout();
        }

        internal VisualElementStyles styles
        {
            get
            {
                return m_Styles;
            }
        }

        public float width
        {
            get
            {
                return m_Styles.width;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.width = Style<float>.Create(value);
                cssNode.Width = value;
                Dirty(ChangeType.Layout);
            }
        }

        public float height
        {
            get
            {
                return m_Styles.height;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.height = Style<float>.Create(value);
                cssNode.Height = value;
                Dirty(ChangeType.Layout);
            }
        }

        public float maxWidth
        {
            get
            {
                return m_Styles.maxWidth;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.maxWidth = Style<float>.Create(value);
                cssNode.MaxWidth = value;
                Dirty(ChangeType.Layout);
            }
        }

        public float maxHeight
        {
            get
            {
                return m_Styles.maxHeight;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.maxHeight = Style<float>.Create(value);
                cssNode.MaxHeight = value;
                Dirty(ChangeType.Layout);
            }
        }

        public float minWidth
        {
            get
            {
                return m_Styles.minWidth;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.minWidth = Style<float>.Create(value);
                cssNode.MinWidth = value;
                Dirty(ChangeType.Layout);
            }
        }

        public float minHeight
        {
            get
            {
                return m_Styles.minHeight;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.minHeight = Style<float>.Create(value);
                cssNode.MinHeight = value;
                Dirty(ChangeType.Layout);
            }
        }

        public float flex
        {
            get
            {
                return m_Styles.flex;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.flex = Style<float>.Create(value);
                cssNode.Flex = value;
                Dirty(ChangeType.Layout);
            }
        }

        public float positionLeft
        {
            get
            {
                return m_Styles.positionLeft;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.positionLeft = Style<float>.Create(value);
                cssNode.SetPosition(CSSEdge.Left, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float positionTop
        {
            get
            {
                return m_Styles.positionTop;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.positionTop = Style<float>.Create(value);
                cssNode.SetPosition(CSSEdge.Top, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float positionRight
        {
            get
            {
                return m_Styles.positionRight;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.positionRight = Style<float>.Create(value);
                cssNode.SetPosition(CSSEdge.Right, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float positionBottom
        {
            get
            {
                return m_Styles.positionBottom;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.positionBottom = Style<float>.Create(value);
                cssNode.SetPosition(CSSEdge.Bottom, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float marginLeft
        {
            get
            {
                return m_Styles.marginLeft;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.marginLeft = Style<float>.Create(value);
                cssNode.SetMargin(CSSEdge.Left, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float marginTop
        {
            get
            {
                return m_Styles.marginTop;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.marginTop = Style<float>.Create(value);
                cssNode.SetMargin(CSSEdge.Top, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float marginRight
        {
            get
            {
                return m_Styles.marginRight;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.marginRight = Style<float>.Create(value);
                cssNode.SetMargin(CSSEdge.Right, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float marginBottom
        {
            get
            {
                return m_Styles.marginBottom;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.marginBottom = Style<float>.Create(value);
                cssNode.SetMargin(CSSEdge.Bottom, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float borderLeft
        {
            get
            {
                return m_Styles.borderLeft;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.borderLeft = Style<float>.Create(value);
                cssNode.SetBorder(CSSEdge.Left, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float borderTop
        {
            get
            {
                return m_Styles.borderTop;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.borderTop = Style<float>.Create(value);
                cssNode.SetBorder(CSSEdge.Top, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float borderRight
        {
            get
            {
                return m_Styles.borderRight;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.borderRight = Style<float>.Create(value);
                cssNode.SetBorder(CSSEdge.Right, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float borderBottom
        {
            get
            {
                return m_Styles.borderBottom;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.borderBottom = Style<float>.Create(value);
                cssNode.SetBorder(CSSEdge.Bottom, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float paddingLeft
        {
            get
            {
                return m_Styles.paddingLeft;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.paddingLeft = Style<float>.Create(value);
                cssNode.SetPadding(CSSEdge.Left, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float paddingTop
        {
            get
            {
                return m_Styles.paddingTop;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.paddingTop = Style<float>.Create(value);
                cssNode.SetPadding(CSSEdge.Top, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float paddingRight
        {
            get
            {
                return m_Styles.paddingRight;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.paddingRight = Style<float>.Create(value);
                cssNode.SetPadding(CSSEdge.Right, value);
                Dirty(ChangeType.Layout);
            }
        }

        public float paddingBottom
        {
            get
            {
                return m_Styles.paddingBottom;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.paddingBottom = Style<float>.Create(value);
                cssNode.SetPadding(CSSEdge.Bottom, value);
                Dirty(ChangeType.Layout);
            }
        }

        public PositionType positionType
        {
            get
            {
                return (PositionType)m_Styles.positionType.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.positionType = Style<int>.Create((int)value);
                cssNode.PositionType = (CSSPositionType)value;
                Dirty(ChangeType.Layout);
            }
        }

        public ImageScaleMode backgroundSize
        {
            get
            {
                return (ImageScaleMode)m_Styles.backgroundSize.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.backgroundSize = Style<int>.Create((int)value);
            }
        }

        public Align alignSelf
        {
            get
            {
                return (Align)m_Styles.alignSelf.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.alignSelf = Style<int>.Create((int)value);
                cssNode.AlignSelf = (CSSAlign)value;
                Dirty(ChangeType.Layout);
            }
        }

        public TextAnchor textAlignment
        {
            get
            {
                return (TextAnchor)m_Styles.textAlignment.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.textAlignment = Style<int>.Create((int)value);
            }
        }

        public FontStyle fontStyle
        {
            get
            {
                return (FontStyle)m_Styles.fontStyle.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.fontStyle = Style<int>.Create((int)value);
            }
        }

        public TextClipping textClipping
        {
            get
            {
                return (TextClipping)m_Styles.textClipping.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.textClipping = Style<int>.Create((int)value);
            }
        }

        public Font font
        {
            get
            {
                return m_Styles.font;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.font = Style<Font>.Create(value);
            }
        }

        public int fontSize
        {
            get
            {
                return m_Styles.fontSize;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.fontSize = Style<int>.Create(value);
            }
        }

        public bool wordWrap
        {
            get
            {
                return m_Styles.wordWrap;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.wordWrap = Style<bool>.Create(value);
            }
        }

        public Texture2D backgroundImage
        {
            get
            {
                return m_Styles.backgroundImage;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.backgroundImage = Style<Texture2D>.Create(value);
            }
        }

        public Color textColor
        {
            get
            {
                return m_Styles.textColor.GetSpecifiedValueOrDefault(Color.black);
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.textColor = Style<Color>.Create(value);
            }
        }

        public Color backgroundColor
        {
            get
            {
                return m_Styles.backgroundColor;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.backgroundColor = Style<Color>.Create(value);
            }
        }

        public Color borderColor
        {
            get
            {
                return m_Styles.borderColor;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.borderColor = Style<Color>.Create(value);
            }
        }

        public float borderWidth
        {
            get
            {
                return m_Styles.borderWidth;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.borderWidth = Style<float>.Create(value);
            }
        }

        public float borderRadius
        {
            get
            {
                return m_Styles.borderRadius;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.borderRadius = Style<float>.Create(value);
            }
        }

        public Overflow overflow
        {
            get
            {
                return (Overflow)m_Styles.overflow.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.overflow = Style<int>.Create((int)value);
                cssNode.Overflow = (CSSOverflow)value;
                Dirty(ChangeType.Layout);
            }
        }

        // Opacity is not fully supported so it's hidden from public API for now
        internal float opacity
        {
            get
            {
                return m_Styles.opacity.value;
            }
            set
            {
                EnsureInlineStyles();
                m_Styles.opacity = Style<float>.Create(value);
                Dirty(ChangeType.Repaint);
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

        [SerializeField]
        private string m_Tooltip;
        public string tooltip
        {
            get { return m_Tooltip ?? String.Empty; }
            set { if (m_Tooltip != value) { m_Tooltip = value; Dirty(ChangeType.Layout); } }
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
            ScaleMode scaleMode = (ScaleMode)backgroundSize;
            var painter = elementPanel.stylePainter;
            if (backgroundImage != null)
            {
                painter.DrawTexture(position, backgroundImage, Color.white, scaleMode, 0.0f, borderRadius, m_Styles.sliceLeft, m_Styles.sliceTop, m_Styles.sliceRight, m_Styles.sliceBottom);
            }
            else if (backgroundColor != Color.clear)
            {
                painter.DrawRect(position, backgroundColor, 0.0f, borderRadius);
            }

            if (borderColor != Color.clear && borderWidth > 0.0f)
            {
                painter.DrawRect(position, borderColor, borderWidth, borderRadius);
            }

            if (!string.IsNullOrEmpty(text) && contentRect.width > 0.0f && contentRect.height > 0.0f)
            {
                painter.DrawText(contentRect, text, font, fontSize, fontStyle, textColor, textAlignment, wordWrap, contentRect.width, false, textClipping);
            }
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
            return position.Contains(localPoint);
        }

        public virtual bool ContainsPointToLocal(Vector2 point)
        {
            return ContainsPoint(this.ChangeCoordinatesTo(parent, point));
        }

        public virtual bool Overlaps(Rect rectangle)
        {
            return position.Overlaps(rectangle, true);
        }

        public enum MeasureMode
        {
            Undefined = CSSMeasureMode.Undefined,
            Exactly = CSSMeasureMode.Exactly,
            AtMost = CSSMeasureMode.AtMost
        }

        protected internal virtual Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;
            if (string.IsNullOrEmpty(text) || font == null)
                return new Vector2(measuredWidth, measuredHeight);

            if (widthMode == MeasureMode.Exactly)
            {
                measuredWidth = width;
            }
            else
            {
                measuredWidth = elementPanel.stylePainter.ComputeTextWidth(text, font, fontSize, fontStyle, textAlignment, true);

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
                measuredHeight = elementPanel.stylePainter.ComputeTextHeight(text, measuredWidth, wordWrap, font, fontSize, fontStyle, textAlignment, true);

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
            var pos = position;
            pos.width = size.x;
            pos.height = size.y;
            position = pos;
        }

        protected const int DefaultAlignContent = (int)Align.FlexStart;
        protected const int DefaultAlignItems = (int)Align.Stretch;

        internal void FinalizeLayout()
        {
            cssNode.Flex = styles.flex.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.SetPosition(CSSEdge.Left, styles.positionLeft.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPosition(CSSEdge.Top, styles.positionTop.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPosition(CSSEdge.Right, styles.positionRight.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPosition(CSSEdge.Bottom, styles.positionBottom.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Left, styles.marginLeft.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Top, styles.marginTop.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Right, styles.marginRight.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetMargin(CSSEdge.Bottom, styles.marginBottom.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Left, styles.paddingLeft.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Top, styles.paddingTop.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Right, styles.paddingRight.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetPadding(CSSEdge.Bottom, styles.paddingBottom.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetBorder(CSSEdge.Left, styles.borderLeft.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetBorder(CSSEdge.Top, styles.borderTop.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetBorder(CSSEdge.Right, styles.borderRight.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.SetBorder(CSSEdge.Bottom, styles.borderBottom.GetSpecifiedValueOrDefault(float.NaN));
            cssNode.Width = styles.width.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.Height = styles.height.GetSpecifiedValueOrDefault(float.NaN);

            PositionType posType = (PositionType)styles.positionType.value;
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

            cssNode.Overflow = (CSSOverflow)(styles.overflow.value);
            cssNode.AlignSelf = (CSSAlign)(styles.alignSelf.value);
            cssNode.MaxWidth = styles.maxWidth.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.MaxHeight = styles.maxHeight.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.MinWidth = styles.minWidth.GetSpecifiedValueOrDefault(float.NaN);
            cssNode.MinHeight = styles.minHeight.GetSpecifiedValueOrDefault(float.NaN);

            // Note: the following applies to VisualContainer only
            // but it won't cause any trouble and we avoid making this method virtual
            cssNode.FlexDirection = (CSSFlexDirection)styles.flexDirection.value;
            cssNode.AlignContent = (CSSAlign)styles.alignContent.GetSpecifiedValueOrDefault(DefaultAlignContent);
            cssNode.AlignItems = (CSSAlign)styles.alignItems.GetSpecifiedValueOrDefault(DefaultAlignItems);
            cssNode.JustifyContent = (CSSJustify)styles.justifyContent.value;
            cssNode.Wrap = (CSSWrap)styles.flexWrap.value;

            Dirty(ChangeType.Layout);
        }

        internal event OnStylesResolved onStylesResolved;

        internal void SetInlineStyles(VisualElementStyles styles)
        {
            Debug.Assert(!styles.isShared);
            m_Styles = styles;
        }

        internal void SetSharedStyles(VisualElementStyles styles)
        {
            Debug.Assert(styles.isShared);

            ClearDirty(ChangeType.StylesPath | ChangeType.Styles);
            if (styles == m_Styles)
            {
                return;
            }

            if (!m_Styles.isShared)
            {
                m_Styles.Apply(styles, StylePropertyApplyMode.CopyIfNotInline);
            }
            else
            {
                m_Styles = styles;
            }

            if (onStylesResolved != null)
            {
                onStylesResolved(m_Styles);
            }
            OnStylesResolved(m_Styles);
            Dirty(ChangeType.Repaint);
        }

        internal void EnsureInlineStyles()
        {
            if (m_Styles.isShared)
            {
                m_Styles = new VisualElementStyles(m_Styles, isShared: false);
            }
        }

        public void ResetPositionProperties()
        {
            if (m_Styles == null || m_Styles.isShared)
            {
                return;
            }
            m_Styles.positionType = Style<int>.nil;
            m_Styles.marginLeft = Style<float>.nil;
            m_Styles.marginRight = Style<float>.nil;
            m_Styles.marginBottom = Style<float>.nil;
            m_Styles.marginTop = Style<float>.nil;
            m_Styles.positionLeft = Style<float>.nil;
            m_Styles.positionTop = Style<float>.nil;
            m_Styles.positionRight = Style<float>.nil;
            m_Styles.positionBottom = Style<float>.nil;
            m_Styles.width = Style<float>.nil;
            m_Styles.height = Style<float>.nil;
            Dirty(ChangeType.Styles);
        }

        public override string ToString()
        {
            return name + " " + position + " global rect: " + globalBound;
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
            return new Vector2(toLocal.x - ele.position.position.x, toLocal.y - ele.position.position.y);
        }

        // transforms a point assumed in Panel space to  local referential
        public static Vector2 LocalToGlobal(this VisualElement ele, Vector2 p)
        {
            var toGlobal = ele.globalTransform.MultiplyPoint3x4((Vector3)(p + ele.position.position));
            return new Vector2(toGlobal.x, toGlobal.y);
        }

        // transforms a rect assumed in Panel space to  local referential
        public static Rect GlobalToBound(this VisualElement ele, Rect r)
        {
            var inv = ele.globalTransform.inverse;
            Vector2 position = inv.MultiplyPoint3x4((Vector2)r.position);
            r.position = position - ele.position.position;
            r.size = inv.MultiplyPoint3x4(r.size);
            return r;
        }

        // transforms a rect to Panel space referential
        public static Rect LocalToGlobal(this VisualElement ele, Rect r)
        {
            var toGlobalMatrix = ele.globalTransform;
            r.position = toGlobalMatrix.MultiplyPoint3x4(ele.position.position + r.position);
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
            elem.positionType = PositionType.Absolute;
            elem.positionLeft = 0.0f;
            elem.positionTop = 0.0f;
            elem.positionRight = 0.0f;
            elem.positionBottom = 0.0f;
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
}
