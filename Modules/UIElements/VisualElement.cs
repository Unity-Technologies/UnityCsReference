// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define ENABLE_CAPTURE_DEBUG
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Yoga;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;

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
    }

    public enum PickingMode
    {
        Position, // todo better name
        Ignore
    }

    internal class VisualElementListPool
    {
        static ObjectPool<List<VisualElement>> pool = new ObjectPool<List<VisualElement>>(20);

        public static List<VisualElement> Copy(List<VisualElement> elements)
        {
            var result = pool.Get();

            result.AddRange(elements);

            return result;
        }

        public static List<VisualElement> Get(int initialCapacity = 0)
        {
            List<VisualElement> result = pool.Get();

            if (initialCapacity > 0 && result.Capacity < initialCapacity)
            {
                result.Capacity = initialCapacity;
            }
            return result;
        }

        public static void Release(List<VisualElement> elements)
        {
            elements.Clear();
            pool.Release(elements);
        }
    }
    public partial class VisualElement : Focusable, ITransform
    {
        public class UxmlFactory : UxmlFactory<VisualElement, UxmlTraits> {}

        public class UxmlTraits : UIElements.UxmlTraits
        {
            UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = "name" };
            UxmlEnumAttributeDescription<PickingMode> m_PickingMode = new UxmlEnumAttributeDescription<PickingMode> { name = "picking-mode", obsoleteNames = new[] { "pickingMode" }};
            UxmlStringAttributeDescription m_Tooltip = new UxmlStringAttributeDescription { name = "tooltip" };
            protected UxmlIntAttributeDescription m_FocusIndex = new UxmlIntAttributeDescription { name = "focus-index", obsoleteNames = new[] { "focusIndex" }, defaultValue = VisualElement.defaultFocusIndex };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield return new UxmlChildElementDescription(typeof(VisualElement)); }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ve.name = m_Name.GetValueFromBag(bag, cc);
                ve.pickingMode = m_PickingMode.GetValueFromBag(bag, cc);
                ve.focusIndex = m_FocusIndex.GetValueFromBag(bag, cc);
                ve.tooltip = m_Tooltip.GetValueFromBag(bag, cc);
            }
        }

        public static readonly int defaultFocusIndex = -1;

        private static uint s_NextId;

        private static List<string> s_EmptyClassList = new List<string>(0);

        string m_Name;
        List<string> m_ClassList;
        string m_TypeName;
        string m_FullTypeName;

        // Used for view data persistence, like expanded states.
        private string m_PersistenceKey;
        public string persistenceKey
        {
            get { return m_PersistenceKey; }
            set
            {
                if (m_PersistenceKey != value)
                {
                    m_PersistenceKey = value;

                    if (!string.IsNullOrEmpty(value))
                        IncrementVersion(VersionChangeType.PersistentData);
                }
            }
        }

        // It seems worse to have unwanted/unpredictable persistence
        // than to expect it and not have it. This internal check, set
        // by Panel.ValidatePersistentDataOnSubTree() controls whether
        // persistency is enabled or not. Persistence will be disabled
        // on any VisualElement that does not have a persistenceKey, but
        // it will also be disabled if any of its parents does not have
        // a persistenceKey.
        internal bool enablePersistence { get; private set; }

        public object userData { get; set; }

        public override bool canGrabFocus { get { return visible && enabledInHierarchy && base.canGrabFocus; } }

        public override FocusController focusController
        {
            get { return panel == null ? null : panel.focusController; }
        }

        private RenderData m_RenderData;
        internal RenderData renderData
        {
            get { return m_RenderData ?? (m_RenderData = new RenderData()); }
        }

        Vector3 m_Position = Vector3.zero;
        Quaternion m_Rotation = Quaternion.identity;
        Vector3 m_Scale = Vector3.one;

        public ITransform transform
        {
            get { return this; }
        }

        Vector3 ITransform.position
        {
            get
            {
                return m_Position;
            }
            set
            {
                if (m_Position == value)
                    return;
                m_Position = value;
                IncrementVersion(VersionChangeType.Transform);
            }
        }

        Quaternion ITransform.rotation
        {
            get
            {
                return m_Rotation;
            }
            set
            {
                if (m_Rotation == value)
                    return;
                m_Rotation = value;
                IncrementVersion(VersionChangeType.Transform);
            }
        }

        Vector3 ITransform.scale
        {
            get
            {
                return m_Scale;
            }
            set
            {
                if (m_Scale == value)
                    return;
                m_Scale = value;
                IncrementVersion(VersionChangeType.Transform);
            }
        }

        Matrix4x4 ITransform.matrix
        {
            get { return Matrix4x4.TRS(m_Position, m_Rotation, m_Scale); }
        }

        Rect m_Layout;

        // This will replace the Rect position
        // origin and size relative to parent
        public Rect layout
        {
            get
            {
                var result = m_Layout;
                if (yogaNode != null && style.positionType.value != PositionType.Manual)
                {
                    result.x = yogaNode.LayoutX;
                    result.y = yogaNode.LayoutY;
                    result.width = yogaNode.LayoutWidth;
                    result.height = yogaNode.LayoutHeight;
                }
                return result;
            }
            set
            {
                if (yogaNode == null)
                {
                    yogaNode = new YogaNode();
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

                IncrementVersion(VersionChangeType.Transform);
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
                var spacing = new Spacing(style.borderLeftWidth,
                    style.borderTopWidth,
                    style.borderRightWidth,
                    style.borderBottomWidth);

                return rect - spacing;
            }
        }

        /// <summary>
        /// AABB after applying the world transform to <c>rect</c>.
        /// </summary>
        public Rect worldBound
        {
            get
            {
                var g = worldTransform;
                var min = GUIUtility.Internal_MultiplyPoint(new Vector3(rect.min.x, rect.min.y, 1), g);
                var max = GUIUtility.Internal_MultiplyPoint(new Vector3(rect.max.x, rect.max.y, 1), g);

                // We assume that the transform performs translation/scaling without rotation.
                return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
            }
        }

        /// <summary>
        /// AABB after applying the transform to the rect, but before applying the layout translation.
        /// </summary>
        public Rect localBound
        {
            get
            {
                var g = transform.matrix;
                var min = GUIUtility.Internal_MultiplyPoint(layout.min, g);
                var max = GUIUtility.Internal_MultiplyPoint(layout.max, g);

                // We assume that the transform performs translation/scaling without rotation.
                return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
            }
        }

        internal Rect rect
        {
            get
            {
                return new Rect(0.0f, 0.0f, layout.width, layout.height);
            }
        }

        internal bool isWorldTransformDirty { get; set; } = true;

        private Matrix4x4 m_WorldTransform = Matrix4x4.identity;

        /// <summary>
        /// Returns a matrix that cumulates the following operations (in order):
        /// -Local Scaling
        /// -Local Rotation
        /// -Local Translation
        /// -Layout Translation
        /// -Parent <c>worldTransform</c> (recursive definition - consider identity when there is no parent)
        /// </summary>
        /// <remarks>
        /// Multiplying the <c>layout</c> rect by this matrix is incorrect because it already contains the translation.
        /// </remarks>
        public Matrix4x4 worldTransform
        {
            get
            {
                if (isWorldTransformDirty)
                {
                    UpdateWorldTransform();
                    isWorldTransformDirty = false;
                }
                return m_WorldTransform;
            }
        }

        private void UpdateWorldTransform()
        {
            var offset = Matrix4x4.Translate(new Vector3(layout.x, layout.y, 0));
            if (shadow.parent != null)
            {
                m_WorldTransform = shadow.parent.worldTransform * offset * transform.matrix;
            }
            else
            {
                m_WorldTransform = offset * transform.matrix;
            }
        }

        internal bool isWorldClipDirty { get; set; } = true;

        private Rect m_WorldClip = Rect.zero;
        internal Rect worldClip
        {
            get
            {
                if (isWorldClipDirty)
                {
                    UpdateWorldClip();
                    isWorldClipDirty = false;
                }
                return m_WorldClip;
            }
        }

        private void UpdateWorldClip()
        {
            if (shadow.parent != null)
            {
                m_WorldClip = shadow.parent.worldClip;

                if (ShouldClip())
                {
                    var localClip = ComputeAAAlignedBound(rect, worldTransform);

                    float x1 = Mathf.Max(localClip.x, m_WorldClip.x);
                    float x2 = Mathf.Min(localClip.x + localClip.width, m_WorldClip.x + m_WorldClip.width);
                    float y1 = Mathf.Max(localClip.y, m_WorldClip.y);
                    float y2 = Mathf.Min(localClip.y + localClip.height, m_WorldClip.y + m_WorldClip.height);

                    m_WorldClip = new Rect(x1, y1, x2 - x1, y2 - y1);
                }
            }
            else
            {
                m_WorldClip = panel != null ? panel.visualTree.rect : GUIClip.topmostRect;
            }
        }

        // get the AA aligned bound
        internal static Rect ComputeAAAlignedBound(Rect position, Matrix4x4 mat)
        {
            Rect p = position;
            Vector3 v0 = mat.MultiplyPoint3x4(new Vector3(p.x, p.y, 0.0f));
            Vector3 v1 = mat.MultiplyPoint3x4(new Vector3(p.x + p.width, p.y, 0.0f));
            Vector3 v2 = mat.MultiplyPoint3x4(new Vector3(p.x, p.y + p.height, 0.0f));
            Vector3 v3 = mat.MultiplyPoint3x4(new Vector3(p.x + p.width, p.y + p.height, 0.0f));
            return Rect.MinMaxRect(
                Mathf.Min(v0.x, Mathf.Min(v1.x, Mathf.Min(v2.x, v3.x))),
                Mathf.Min(v0.y, Mathf.Min(v1.y, Mathf.Min(v2.y, v3.y))),
                Mathf.Max(v0.x, Mathf.Max(v1.x, Mathf.Max(v2.x, v3.x))),
                Mathf.Max(v0.y, Mathf.Max(v1.y, Mathf.Max(v2.y, v3.y))));
        }

        // which pseudo states would change the current VE styles if added
        internal PseudoStates triggerPseudoMask;
        // which pseudo states would change the current VE styles if removed
        internal PseudoStates dependencyPseudoMask;

        private PseudoStates m_PseudoStates;
        internal PseudoStates pseudoStates
        {
            get { return m_PseudoStates; }
            set
            {
                if (m_PseudoStates != value)
                {
                    m_PseudoStates = value;

                    if ((triggerPseudoMask & m_PseudoStates) != 0
                        || (dependencyPseudoMask & ~m_PseudoStates) != 0)
                    {
                        IncrementVersion(VersionChangeType.StyleSheet);
                    }
                }
            }
        }

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
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        internal List<string> classList
        {
            get { return m_ClassList; }
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
                {
                    var type = GetType();
                    bool isGeneric = false;
                    isGeneric = type.IsGenericType;

                    m_TypeName = isGeneric ? type.Name.Remove(type.Name.IndexOf('`')) : type.Name;
                }

                return m_TypeName;
            }
        }

        // Set and pass in values to be used for layout
        internal YogaNode yogaNode { get; private set; }

        // shared style object, cannot be changed by the user
        internal VisualElementStylesData m_SharedStyle = VisualElementStylesData.none;
        // user-defined style object, if not set, is the same reference as m_SharedStyles
        internal VisualElementStylesData m_Style = VisualElementStylesData.none;

        protected virtual void OnStyleResolved(ICustomStyle style)
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

        internal readonly uint controlid;

        public VisualElement()
        {
            controlid = ++s_NextId;

            shadow = new Hierarchy(this);

            m_ClassList = s_EmptyClassList;
            m_FullTypeName = string.Empty;
            m_TypeName = string.Empty;
            SetEnabled(true);

            // Make element non focusable by default.
            focusIndex = defaultFocusIndex;

            name = string.Empty;
            yogaNode = new YogaNode();
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == MouseOverEvent.TypeId() || evt.GetEventTypeId() == MouseOutEvent.TypeId())
            {
                UpdateCursorStyle(evt.GetEventTypeId());
            }
            else if (evt.GetEventTypeId() == MouseEnterEvent.TypeId())
            {
                pseudoStates |= PseudoStates.Hover;
            }
            else if (evt.GetEventTypeId() == MouseLeaveEvent.TypeId())
            {
                pseudoStates &= ~PseudoStates.Hover;
            }
            else if (evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                pseudoStates = pseudoStates & ~PseudoStates.Focus;
            }
            else if (evt.GetEventTypeId() == FocusEvent.TypeId())
            {
                pseudoStates = pseudoStates | PseudoStates.Focus;
            }
        }

        public sealed override void Focus()
        {
            if (!canGrabFocus && shadow.parent != null)
            {
                shadow.parent.Focus();
            }
            else
            {
                base.Focus();
            }
        }

        internal void SetPanel(BaseVisualElementPanel p)
        {
            if (panel == p)
                return;

            //We now gather all Elements in order to dispatch events in an efficient manner
            List<VisualElement> elements = VisualElementListPool.Get();
            try
            {
                elements.Add(this);
                GatherAllChildren(elements);

                EventDispatcher.Gate? pDispatcherGate = null;
                if (p?.dispatcher != null)
                {
                    pDispatcherGate = new EventDispatcher.Gate(p.dispatcher);
                }

                EventDispatcher.Gate? panelDispatcherGate = null;
                if (panel?.dispatcher != null && panel.dispatcher != p?.dispatcher)
                {
                    panelDispatcherGate = new EventDispatcher.Gate(panel.dispatcher);
                }

                using (pDispatcherGate)
                using (panelDispatcherGate)
                {
                    foreach (var e in elements)
                    {
                        e.ChangePanel(p);
                    }
                }
            }
            finally
            {
                VisualElementListPool.Release(elements);
            }
        }

        void ChangePanel(BaseVisualElementPanel p)
        {
            if (panel == p)
            {
                return;
            }

            if (panel != null)
            {
                using (var e = DetachFromPanelEvent.GetPooled(panel, p))
                {
                    e.target = this;
                    elementPanel.SendEvent(e, DispatchMode.Immediate);
                }
            }

            IPanel prevPanel = panel;
            elementPanel = p;

            if (panel != null)
            {
                using (var e = AttachToPanelEvent.GetPooled(prevPanel, p))
                {
                    e.target = this;
                    elementPanel.SendEvent(e, DispatchMode.Default);
                }
            }

            // styles are dependent on topology
            IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Transform);

            // persistent data key may have changed or needs initialization
            if (!string.IsNullOrEmpty(persistenceKey))
                IncrementVersion(VersionChangeType.PersistentData);
        }

        public sealed override void SendEvent(EventBase e)
        {
            elementPanel?.SendEvent(e);
        }

        internal void IncrementVersion(VersionChangeType changeType)
        {
            elementPanel?.OnVersionChanged(this, changeType);
        }

        private void IncrementVersion(ChangeType changeType)
        {
            IncrementVersion(GetVersionChange(changeType));
        }

        private VersionChangeType GetVersionChange(ChangeType type)
        {
            VersionChangeType versionChangeType = 0;

            if ((type & (ChangeType.PersistentData | ChangeType.PersistentDataPath)) > 0)
            {
                versionChangeType |= VersionChangeType.PersistentData;
            }

            if ((type & ChangeType.Layout) == ChangeType.Layout)
            {
                versionChangeType |= VersionChangeType.Layout;
            }

            if ((type & (ChangeType.Styles | ChangeType.StylesPath)) > 0)
            {
                versionChangeType |= VersionChangeType.StyleSheet;
            }

            if ((type & ChangeType.Transform) == ChangeType.Transform)
            {
                versionChangeType |= VersionChangeType.Transform;
            }

            if ((type & ChangeType.Repaint) == ChangeType.Repaint)
            {
                versionChangeType |= VersionChangeType.Repaint;
            }

            return versionChangeType;
        }

        [Obsolete("Dirty is deprecated. Use MarkDirtyRepaint to trigger a new repaint of the VisualElement.")]
        public void Dirty(ChangeType type)
        {
            IncrementVersion(type);
        }

        [Obsolete("IsDirty is deprecated. Avoid using it, will always return false.")]
        public bool IsDirty(ChangeType type)
        {
            return false;
        }

        [Obsolete("AnyDirty is deprecated. Avoid using it, will always return false.")]
        public bool AnyDirty(ChangeType type)
        {
            return false;
        }

        [Obsolete("ClearDirty is deprecated. Avoid using it, it's now a no-op.")]
        public void ClearDirty(ChangeType type)
        {
        }

        [Obsolete("enabled is deprecated. Use SetEnabled as setter, and enabledSelf/enabledInHierarchy as getters.")]
        public virtual bool enabled
        {
            get { return enabledInHierarchy; }
            set { SetEnabled(value); }
        }

        private bool m_Enabled;

        //TODO: Make private once VisualContainer is merged with VisualElement
        protected internal bool SetEnabledFromHierarchy(bool state)
        {
            //returns false if state hasn't changed
            if (state == ((pseudoStates & PseudoStates.Disabled) != PseudoStates.Disabled))
                return false;

            if (state && m_Enabled && (parent == null || parent.enabledInHierarchy))
                pseudoStates &= ~PseudoStates.Disabled;
            else
                pseudoStates |= PseudoStates.Disabled;

            return true;
        }

        //Returns true if 'this' can be enabled relative to the enabled state of its panel
        public bool enabledInHierarchy
        {
            get { return (pseudoStates & PseudoStates.Disabled) != PseudoStates.Disabled; }
        }

        //Returns the local enabled state
        public bool enabledSelf
        {
            get { return m_Enabled; }
        }

        public void SetEnabled(bool value)
        {
            if (m_Enabled != value)
            {
                m_Enabled = value;

                PropagateEnabledToChildren(value);
            }
        }

        void PropagateEnabledToChildren(bool value)
        {
            if (SetEnabledFromHierarchy(value))
            {
                for (int i = 0; i < shadow.childCount; ++i)
                {
                    shadow[i].PropagateEnabledToChildren(value);
                }
            }
        }

        public bool visible
        {
            get
            {
                return style.visibility.GetSpecifiedValueOrDefault(Visibility.Visible) == Visibility.Visible;
            }
            set
            {
                // Note: this could causes an allocation because styles are copy-on-write
                // we might want to remove this setter altogether
                // so everything goes through style.visibility (and then it's documented in a single place)
                style.visibility = value ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public void MarkDirtyRepaint()
        {
            IncrementVersion(VersionChangeType.Repaint);
        }

        internal void Repaint(IStylePainter painter)
        {
            if (visible == false)
            {
                return;
            }
            var stylePainter = (IStylePainterInternal)painter;
            stylePainter.DrawBackground();
            DoRepaint(stylePainter);
            stylePainter.DrawBorder();
        }

        protected virtual void DoRepaint(IStylePainter painter)
        {
            // Implemented by subclasses
        }

        private void GetFullHierarchicalPersistenceKey(StringBuilder key)
        {
            const string keySeparator = "__";

            if (parent != null)
                parent.GetFullHierarchicalPersistenceKey(key);

            if (!string.IsNullOrEmpty(persistenceKey))
            {
                key.Append(keySeparator);
                key.Append(persistenceKey);
            }
        }

        public string GetFullHierarchicalPersistenceKey()
        {
            StringBuilder key = new StringBuilder();

            GetFullHierarchicalPersistenceKey(key);

            return key.ToString();
        }

        public T GetOrCreatePersistentData<T>(object existing, string key) where T : class, new()
        {
            Debug.Assert(elementPanel != null, "VisualElement.elementPanel is null! Cannot load persistent data.");

            var persistentData = elementPanel == null || elementPanel.getViewDataDictionary == null ? null : elementPanel.getViewDataDictionary();

            // If persistency is disable (no data, no key, no key one of the parents), just return the
            // existing data or create a local one if none exists.
            if (persistentData == null || string.IsNullOrEmpty(persistenceKey) || enablePersistence == false)
            {
                if (existing != null)
                    return existing as T;

                return new T();
            }

            string keyWithType = key + "__" + typeof(T).ToString();

            if (!persistentData.ContainsKey(keyWithType))
                persistentData.Set(keyWithType, new T());

            return persistentData.Get<T>(keyWithType);
        }

        public T GetOrCreatePersistentData<T>(ScriptableObject existing, string key) where T : ScriptableObject
        {
            Debug.Assert(elementPanel != null, "VisualElement.elementPanel is null! Cannot load persistent data.");

            var persistentData = elementPanel == null || elementPanel.getViewDataDictionary == null ? null : elementPanel.getViewDataDictionary();

            // If persistency is disable (no data, no key, no key one of the parents), just return the
            // existing data or create a local one if none exists.
            if (persistentData == null || string.IsNullOrEmpty(persistenceKey) || enablePersistence == false)
            {
                if (existing != null)
                    return existing as T;

                return ScriptableObject.CreateInstance<T>();
            }

            string keyWithType = key + "__" + typeof(T).ToString();

            if (!persistentData.ContainsKey(keyWithType))
                persistentData.Set(keyWithType, ScriptableObject.CreateInstance<T>());

            return persistentData.GetScriptable<T>(keyWithType);
        }

        public void OverwriteFromPersistedData(object obj, string key)
        {
            Debug.Assert(elementPanel != null, "VisualElement.elementPanel is null! Cannot load persistent data.");

            var persistentData = elementPanel == null || elementPanel.getViewDataDictionary == null ? null : elementPanel.getViewDataDictionary();

            // If persistency is disable (no data, no key, no key one of the parents), just return the
            // existing data or create a local one if none exists.
            if (persistentData == null || string.IsNullOrEmpty(persistenceKey) || enablePersistence == false)
            {
                return;
            }

            string keyWithType = key + "__" + obj.GetType();

            if (!persistentData.ContainsKey(keyWithType))
            {
                persistentData.Set(keyWithType, obj);
                return;
            }

            persistentData.Overwrite(obj, keyWithType);
        }

        public void SavePersistentData()
        {
            if (elementPanel != null && elementPanel.savePersistentViewData != null && !string.IsNullOrEmpty(persistenceKey))
                elementPanel.savePersistentViewData();
        }

        internal bool IsPersitenceSupportedOnChildren()
        {
            // We relax here the requirement that ALL parents of a VisualElement
            // need to have a persistenceKey for persistence to work. Plain
            // VisualElements are likely to be used just for layouting and
            // grouping and requiring a key on each element is a bit tedious.
            if (this.GetType() == typeof(VisualElement))
                return true;

            if (string.IsNullOrEmpty(persistenceKey))
                return false;

            return true;
        }

        internal void OnPersistentDataReady(bool enablePersistence)
        {
            this.enablePersistence = enablePersistence;
            OnPersistentDataReady();
        }

        public virtual void OnPersistentDataReady() {}

        // position should be in local space
        // override to customize intersection between point and shape
        public virtual bool ContainsPoint(Vector2 localPoint)
        {
            return rect.Contains(localPoint);
        }

        public virtual bool Overlaps(Rect rectangle)
        {
            return rect.Overlaps(rectangle, true);
        }

        public enum MeasureMode
        {
            Undefined = YogaMeasureMode.Undefined,
            Exactly = YogaMeasureMode.Exactly,
            AtMost = YogaMeasureMode.AtMost
        }

        private bool m_RequireMeasureFunction = false;
        internal bool requireMeasureFunction
        {
            get { return m_RequireMeasureFunction; }
            set
            {
                m_RequireMeasureFunction = value;
                if (m_RequireMeasureFunction && !yogaNode.IsMeasureDefined)
                {
                    yogaNode.SetMeasureFunction(Measure);
                }
                else if (!m_RequireMeasureFunction && yogaNode.IsMeasureDefined)
                {
                    yogaNode.SetMeasureFunction(null);
                }
            }
        }

        protected internal virtual Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            return new Vector2(float.NaN, float.NaN);
        }

        internal YogaSize Measure(YogaNode node, float width, YogaMeasureMode widthMode, float height, YogaMeasureMode heightMode)
        {
            Debug.Assert(node == yogaNode, "YogaNode instance mismatch");
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

        void FinalizeLayout()
        {
            if (hasInlineStyle)
            {
                effectiveStyle.SyncWithLayout(yogaNode);
            }
            else
            {
                yogaNode.CopyStyle(effectiveStyle.yogaNode);
            }
        }

        internal event OnStylesResolved onStylesResolved;

        // for internal use only, used by asset instantiation to push local styles
        // likely can be replaced by merging VisualContainer and VisualElement
        // and then storing the inline sheet in the list held by VisualContainer
        internal void SetInlineStyles(VisualElementStylesData inlineStyle)
        {
            Debug.Assert(!inlineStyle.isShared);
            inlineStyle.Apply(m_Style, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            m_Style = inlineStyle;
        }

        internal void SetSharedStyles(VisualElementStylesData sharedStyle)
        {
            Debug.Assert(sharedStyle.isShared);

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

            // This is a pre-emptive since we do not know if style changes actually cause a repaint or a layout
            // But thouse should be the only possible type of changes needed
            IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout | VersionChangeType.Repaint);
        }

        public void ResetPositionProperties()
        {
            if (!hasInlineStyle)
            {
                return;
            }
            VisualElementStylesData styleAccess = inlineStyle;
            styleAccess.positionType = StyleValue<int>.nil;
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

            IncrementVersion(VersionChangeType.Layout);
        }

        public override string ToString()
        {
            return GetType().Name + " " + name + " " + layout + " world rect: " + worldBound;
        }

        // WARNING returning the HashSet means it could be modified, be careful
        internal IEnumerable<string> GetClasses()
        {
            return m_ClassList;
        }

        public void ClearClassList()
        {
            if (m_ClassList.Count > 0)
            {
                m_ClassList = s_EmptyClassList;
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        public void AddToClassList(string className)
        {
            if (m_ClassList == s_EmptyClassList)
            {
                m_ClassList = new List<string>() { className };
            }
            else
            {
                if (m_ClassList.Contains(className))
                {
                    return;
                }
                m_ClassList.Capacity += 1;
                m_ClassList.Add(className);
            }

            IncrementVersion(VersionChangeType.StyleSheet);
        }

        public void RemoveFromClassList(string className)
        {
            if (m_ClassList.Remove(className))
            {
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        public void ToggleInClassList(string className)
        {
            if (ClassListContains(className))
                RemoveFromClassList(className);
            else
                AddToClassList(className);
        }

        public void EnableInClassList(string className, bool enable)
        {
            if (enable)
                AddToClassList(className);
            else
                RemoveFromClassList(className);
        }

        public bool ClassListContains(string cls)
        {
            for (int i = 0; i < m_ClassList.Count; i++)
            {
                if (m_ClassList[i] == cls)
                    return true;
            }

            return false;
        }

        public object FindAncestorUserData()
        {
            VisualElement p = parent;

            while (p != null)
            {
                if (p.userData != null)
                    return p.userData;
                p = p.parent;
            }

            return null;
        }

        private void UpdateCursorStyle(long eventType)
        {
            if (elementPanel != null)
            {
                if (eventType == MouseOverEvent.TypeId())
                {
                    elementPanel.cursorManager.SetCursor(style.cursor.value);
                }
                else
                {
                    elementPanel.cursorManager.ResetCursor();
                }
            }
        }
    }

    public static class VisualElementExtensions
    {
        // transforms a point assumed in Panel space to the referential inside of the element bound (local)
        public static Vector2 WorldToLocal(this VisualElement ele, Vector2 p)
        {
            return ele.worldTransform.inverse.MultiplyPoint3x4((Vector3)p);
        }

        // transforms a point to Panel space referential
        public static Vector2 LocalToWorld(this VisualElement ele, Vector2 p)
        {
            return (Vector2)ele.worldTransform.MultiplyPoint3x4((Vector3)p);
        }

        // transforms a rect assumed in Panel space to the referential inside of the element bound (local)
        public static Rect WorldToLocal(this VisualElement ele, Rect r)
        {
            var inv = ele.worldTransform.inverse;
            Vector2 position = inv.MultiplyPoint3x4((Vector2)r.position);
            r.position = position;
            r.size = inv.MultiplyVector(r.size);
            return r;
        }

        // transforms a rect to Panel space referential
        public static Rect LocalToWorld(this VisualElement ele, Rect r)
        {
            var toWorldMatrix = ele.worldTransform;
            r.position = toWorldMatrix.MultiplyPoint3x4(r.position);
            r.size = toWorldMatrix.MultiplyVector(r.size);
            return r;
        }

        // transform point from the local space of one element to to the local space of another
        public static Vector2 ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Vector2 point)
        {
            return dest.WorldToLocal(src.LocalToWorld(point));
        }

        // transform Rect from the local space of one element to to the local space of another
        public static Rect ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Rect rect)
        {
            return dest.WorldToLocal(src.LocalToWorld(rect));
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

        public static void StretchToParentWidth(this VisualElement elem)
        {
            IStyle styleAccess = elem.style;
            styleAccess.positionType = PositionType.Absolute;
            styleAccess.positionLeft = 0.0f;
            styleAccess.positionRight = 0.0f;
        }

        public static void AddManipulator(this VisualElement ele, IManipulator manipulator)
        {
            if (manipulator != null)
            {
                manipulator.target = ele;
            }
        }

        public static void RemoveManipulator(this VisualElement ele, IManipulator manipulator)
        {
            if (manipulator != null)
            {
                manipulator.target = null;
            }
        }
    }
}
