// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine.Yoga;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.UIR;
using PropertyBagValue = System.Collections.Generic.KeyValuePair<UnityEngine.PropertyName, object>;

namespace UnityEngine.UIElements
{
    // pseudo states are used for common states of a widget
    // they are addressable from CSS via the pseudo state syntax ":selected" for example
    // while css class list can solve the same problem, pseudo states are a fast commonly agreed upon path for common cases.
    [Flags]
    internal enum PseudoStates
    {
        Active    = 1 << 0,     // control is currently pressed in the case of a button
        Hover     = 1 << 1,     // mouse is over control, set and removed from dispatcher automatically
        Checked   = 1 << 3,     // usually associated with toggles of some kind to change visible style
        Disabled  = 1 << 5,     // control will not respond to user input
        Focus     = 1 << 6,     // control has the keyboard focus. This is activated deactivated by the dispatcher automatically
        Root      = 1 << 7,     // set on the root visual element
    }

    [Flags]
    internal enum VisualElementFlags
    {
        // Need to compute world transform
        WorldTransformDirty = 1 << 0,
        // Need to compute world transform inverse
        WorldTransformInverseDirty = 1 << 1,
        // Need to compute world clip
        WorldClipDirty = 1 << 2,
        // Need to compute bounding box
        BoundingBoxDirty = 1 << 3,
        // Need to compute world bounding box
        WorldBoundingBoxDirty = 1 << 4,
        // Element layout is manually set
        LayoutManual = 1 << 5,
        // Element is a root for composite controls
        CompositeRoot = 1 << 6,
        // Element has a custom measure function
        RequireMeasureFunction = 1 << 7,
        // Element has view data persistence
        EnableViewDataPersistence = 1 << 8,
        // Element never clip regardless of overflow style (useful for ScrollView)
        DisableClipping = 1 << 9,
        // Element needs to receive an AttachToPanel event
        NeedsAttachToPanelEvent = 1 << 10,
        // Element is shown in the hierarchy (element or one of its ancestors is not DisplayStyle.None)
        // Note that this flag is up-to-date only after UIRLayoutUpdater is done with its updates
        HierarchyDisplayed = 1 << 11,
        // Element style are computed
        StyleInitialized = 1 << 12,
        // Element initial flags
        Init = WorldTransformDirty | WorldTransformInverseDirty | WorldClipDirty | BoundingBoxDirty | WorldBoundingBoxDirty | HierarchyDisplayed
    }

    /// <summary>
    /// Describes the picking behavior.
    /// </summary>
    public enum PickingMode
    {
        /// <summary>
        /// Picking enabled. Default Value.
        /// </summary>
        Position, // todo better name
        /// <summary>
        /// Disables picking.
        /// </summary>
        Ignore
    }

    internal static class VisualElementListPool
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

    internal class ObjectListPool<T>
    {
        static ObjectPool<List<T>> pool = new ObjectPool<List<T>>(20);

        public static List<T> Get()
        {
            return pool.Get();
        }

        public static void Release(List<T> elements)
        {
            elements.Clear();
            pool.Release(elements);
        }
    }

    internal class StringListPool : ObjectListPool<string>
    {
    }


    internal class StringObjectListPool : ObjectListPool<string>
    {
    }


    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.
    /// </remarks>
    public partial class VisualElement : Focusable, ITransform
    {
        /// <summary>
        /// Instantiates a <see cref="VisualElement"/> using the data read from a UXML file.
        /// </summary>
        public class UxmlFactory : UxmlFactory<VisualElement, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="VisualElement"/>.
        /// </summary>
        public class UxmlTraits : UIElements.UxmlTraits
        {
            protected UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_NameAttributeName };
            UxmlStringAttributeDescription m_ViewDataKey = new UxmlStringAttributeDescription { name = "view-data-key" };
            protected UxmlEnumAttributeDescription<PickingMode> m_PickingMode = new UxmlEnumAttributeDescription<PickingMode> { name = "picking-mode", obsoleteNames = new[] { "pickingMode" }};
            UxmlStringAttributeDescription m_Tooltip = new UxmlStringAttributeDescription { name = "tooltip" };
            UxmlEnumAttributeDescription<UsageHints> m_UsageHints = new UxmlEnumAttributeDescription<UsageHints> { name = "usage-hints" };

            // focusIndex is obsolete. It has been replaced by tabIndex and focusable.
            /// <summary>
            /// The focus index attribute.
            /// </summary>
            protected UxmlIntAttributeDescription focusIndex { get; set; } = new UxmlIntAttributeDescription { name = null, obsoleteNames = new[] { "focus-index", "focusIndex" }, defaultValue = -1 };
            UxmlIntAttributeDescription m_TabIndex = new UxmlIntAttributeDescription { name = "tabindex", defaultValue = 0 };
            /// <summary>
            /// The focusable attribute.
            /// </summary>
            protected UxmlBoolAttributeDescription focusable { get; set; } = new UxmlBoolAttributeDescription { name = "focusable", defaultValue = false };

#pragma warning disable 414
            // These variables are used by reflection.
            UxmlStringAttributeDescription m_Class = new UxmlStringAttributeDescription { name = "class" };
            UxmlStringAttributeDescription m_ContentContainer = new UxmlStringAttributeDescription { name = "content-container", obsoleteNames = new[] { "contentContainer" } };
            UxmlStringAttributeDescription m_Style = new UxmlStringAttributeDescription { name = "style" };
#pragma warning restore

            /// <summary>
            /// Returns an enumerable containing <c>UxmlChildElementDescription(typeof(VisualElement))</c>, since VisualElements can contain other VisualElements.
            /// </summary>
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield return new UxmlChildElementDescription(typeof(VisualElement)); }
            }

            /// <summary>
            /// Initialize <see cref="VisualElement"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                if (ve == null)
                {
                    throw new ArgumentNullException(nameof(ve));
                }

                ve.name = m_Name.GetValueFromBag(bag, cc);
                ve.viewDataKey = m_ViewDataKey.GetValueFromBag(bag, cc);
                ve.pickingMode = m_PickingMode.GetValueFromBag(bag, cc);
                ve.usageHints = m_UsageHints.GetValueFromBag(bag, cc);

                int index = 0;
                if (focusIndex.TryGetValueFromBag(bag, cc, ref index))
                {
                    ve.tabIndex = index >= 0 ? index : 0;
                    ve.focusable = index >= 0;
                }

                // tabIndex and focusable overrides obsolete focusIndex.
                if (m_TabIndex.TryGetValueFromBag(bag, cc, ref index))
                {
                    ve.tabIndex = index;
                }

                bool focus = false;

                if (focusable.TryGetValueFromBag(bag, cc, ref focus))
                {
                    ve.focusable = focus;
                }

                ve.tooltip = m_Tooltip.GetValueFromBag(bag, cc);

                // We ignore m_Class, it was processed in UIElementsViewImporter.
                // We ignore m_ContentContainer, it was processed in UIElementsViewImporter.
                // We ignore m_Style, it was processed in UIElementsViewImporter.
            }
        }

        internal bool isCompositeRoot
        {
            get => (m_Flags & VisualElementFlags.CompositeRoot) == VisualElementFlags.CompositeRoot;
            set => m_Flags = value ? m_Flags | VisualElementFlags.CompositeRoot : m_Flags & ~VisualElementFlags.CompositeRoot;
        }

        internal bool isHierarchyDisplayed
        {
            get => (m_Flags & VisualElementFlags.HierarchyDisplayed) == VisualElementFlags.HierarchyDisplayed;
            set => m_Flags = value ? m_Flags | VisualElementFlags.HierarchyDisplayed : m_Flags & ~VisualElementFlags.HierarchyDisplayed;
        }

        private static uint s_NextId;

        private static List<string> s_EmptyClassList = new List<string>(0);

        internal static readonly PropertyName userDataPropertyKey = new PropertyName("--unity-user-data");
        /// <summary>
        /// USS class name of local disabled elements.
        /// </summary>
        public static readonly string disabledUssClassName = "unity-disabled";

        string m_Name;
        List<string> m_ClassList;
        private List<PropertyBagValue> m_PropertyBag;
        private VisualElementFlags m_Flags;

        // Used for view data persistence (ie. scroll position or tree view expanded states)
        private string m_ViewDataKey;
        /// <summary>
        /// Used for view data persistence (ie. tree expanded states, scroll position, zoom level).
        /// </summary>
        /// <remarks>
        /// This is the key used to save/load the view data from the view data store. Not setting this key will disable persistence for this <see cref="VisualElement"/>.
        /// </remarks>
        public string viewDataKey
        {
            get { return m_ViewDataKey; }
            set
            {
                if (m_ViewDataKey != value)
                {
                    m_ViewDataKey = value;

                    if (!string.IsNullOrEmpty(value))
                        IncrementVersion(VersionChangeType.ViewData);
                }
            }
        }

        // Persistence of view data is almost always enabled as long as an element has
        // a valid viewDataKey. The only exception is when an element is in its parent's
        // shadow tree, that is, not a physical child of its logical parent's contentContainer.
        // In this exception case, persistence is disabled on the element even if the element
        // does have a viewDataKey, if its logical parent does not have a viewDataKey.
        // This check internally controls whether or not view data persistence is enabled as
        // the VisualTreeViewDataUpdater traverses the visual tree.
        internal bool enableViewDataPersistence
        {
            get => (m_Flags & VisualElementFlags.EnableViewDataPersistence) == VisualElementFlags.EnableViewDataPersistence;
            private set => m_Flags = value ? m_Flags | VisualElementFlags.EnableViewDataPersistence : m_Flags & ~VisualElementFlags.EnableViewDataPersistence;
        }

        /// <summary>
        /// This property can be used to associate application-specific user data with this VisualElement.
        /// </summary>
        public object userData
        {
            get
            {
                TryGetPropertyInternal(userDataPropertyKey, out object value);
                return value;
            }
            set { SetPropertyInternal(userDataPropertyKey, value); }
        }

        public override bool canGrabFocus
        {
            get
            {
                bool focusDisabledOnComposite = false;
                VisualElement p = hierarchy.parent;
                while (p != null)
                {
                    if (p.isCompositeRoot)
                    {
                        focusDisabledOnComposite |= !p.canGrabFocus;
                        break;
                    }
                    p = p.parent;
                }

                return !focusDisabledOnComposite && visible && (resolvedStyle.display != DisplayStyle.None) && enabledInHierarchy && base.canGrabFocus;
            }
        }

        public override FocusController focusController
        {
            get { return panel?.focusController; }
        }

        /// <summary>
        /// A combination of hint values that specify high-level intended usage patterns for the <see cref="VisualElement"/>.
        /// This property can only be set when the <see cref="VisualElement"/> is not yet part of a <see cref="Panel"/>. Once part of a <see cref="Panel"/>, this property becomes effectively read-only, and attempts to change it will throw an exception.
        /// The specification of proper <see cref="UsageHints"/> drives the system to make better decisions on how to process or accelerate certain operations based on the anticipated usage pattern.
        /// Note that those hints do not affect behavioral or visual results, but only affect the overall performance of the panel and the elements within.
        /// It's advised to always consider specifying the proper <see cref="UsageHints"/>, but keep in mind that some <see cref="UsageHints"/> might be internally ignored under certain conditions (e.g. due to hardware limitations on the target platform).
        /// </summary>
        public UsageHints usageHints
        {
            get
            {
                return
                    (((renderHints & RenderHints.GroupTransform) != 0) ? UsageHints.GroupTransform : 0) |
                    (((renderHints & RenderHints.BoneTransform) != 0) ? UsageHints.DynamicTransform : 0) |
                    (((renderHints & RenderHints.MaskContainer) != 0) ? UsageHints.MaskContainer : 0) |
                    (((renderHints & RenderHints.DynamicColor) != 0) ? UsageHints.DynamicColor : 0);
            }
            set
            {
                // Preserve hints not exposed through UsageHints
                if ((value & UsageHints.GroupTransform) != 0)
                    renderHints |= RenderHints.GroupTransform;
                else renderHints &= ~RenderHints.GroupTransform;

                if ((value & UsageHints.DynamicTransform) != 0)
                    renderHints |= RenderHints.BoneTransform;
                else renderHints &= ~RenderHints.BoneTransform;

                if ((value & UsageHints.MaskContainer) != 0)
                    renderHints |= RenderHints.MaskContainer;
                else renderHints &= ~RenderHints.MaskContainer;

                if ((value & UsageHints.DynamicColor) != 0)
                    renderHints |= RenderHints.DynamicColor;
                else renderHints &= ~RenderHints.DynamicColor;
            }
        }

        private RenderHints m_RenderHints;
        internal RenderHints renderHints
        {
            get { return m_RenderHints; }
            set
            {
                // Filter out the dirty flags
                RenderHints oldHints = m_RenderHints & ~RenderHints.DirtyAll;
                RenderHints newHints = value & ~RenderHints.DirtyAll;
                RenderHints changedHints = oldHints ^ newHints;

                if (changedHints != 0)
                {
                    RenderHints oldDirty = m_RenderHints & RenderHints.DirtyAll;
                    RenderHints addDirty = (RenderHints)((int)changedHints << (int)RenderHints.DirtyOffset);

                    m_RenderHints = newHints | oldDirty | addDirty;
                    IncrementVersion(VersionChangeType.RenderHints);
                }
            }
        }

        // Dirty flags cannot be removed by the renderHints setter
        // This method must ONLY be called from the renderer
        internal void MarkRenderHintsClean()
        {
            m_RenderHints &= ~RenderHints.DirtyAll;
        }

        internal Rect lastLayout;
        internal Rect lastPseudoPadding;
        internal RenderChainVEData renderChainData;


        /// <summary>
        /// Returns a transform object for this VisualElement.
        /// <seealso cref="ITransform"/>
        /// </summary>
        /// <remarks>
        /// The transform object implements changes to the VisualElement object.
        /// </remarks>
        public ITransform transform
        {
            get { return this; }
        }

        Vector3 ITransform.position
        {
            get
            {
                return resolvedStyle.translate;
            }
            set
            {
                style.translate = new Translate(value.x, value.y, value.z);
            }
        }

        Quaternion ITransform.rotation
        {
            get
            {
                return resolvedStyle.rotate.ToQuaternion();
            }
            set
            {
                value.ToAngleAxis(out float angle, out Vector3 axis);
                style.rotate = new Rotate(angle, axis);
            }
        }

        Vector3 ITransform.scale
        {
            get
            {
                return resolvedStyle.scale.value;
            }
            set
            {
                style.scale = new Scale(value);
            }
        }

        internal Vector3 ComputeGlobalScale()
        {
            Vector3 result = resolvedStyle.scale.value;

            var ve = this.hierarchy.parent;

            while (ve != null)
            {
                result.Scale(ve.resolvedStyle.scale.value);
                ve = ve.hierarchy.parent;
            }
            return result;
        }

        Matrix4x4 ITransform.matrix
        {
            get { return Matrix4x4.TRS(resolvedStyle.translate, resolvedStyle.rotate.ToQuaternion(), resolvedStyle.scale.value); }
        }

        internal bool isLayoutManual
        {
            get => (m_Flags & VisualElementFlags.LayoutManual) == VisualElementFlags.LayoutManual;
            private set => m_Flags = value ? m_Flags | VisualElementFlags.LayoutManual : m_Flags & ~VisualElementFlags.LayoutManual;
        }

        internal float scaledPixelsPerPoint
        {
            get { return panel == null ? GUIUtility.pixelsPerPoint : (panel as BaseVisualElementPanel).scaledPixelsPerPoint; }
        }

        Rect m_Layout;

        // This will replace the Rect position
        // origin and size relative to parent
        /// <summary>
        /// The position and size of the VisualElement relative to its parent, as computed by the layout system.
        /// </summary>
        /// <remarks>
        /// Before reading from this property, add it to a panel and wait for one frame to ensure that the element layout is computed.
        /// After the layout is computed, a <see cref="GeometryChangedEvent"/> will be sent on this element.
        /// </remarks>
        public Rect layout
        {
            get
            {
                var result = m_Layout;
                if (yogaNode != null && !isLayoutManual)
                {
                    result.x = yogaNode.LayoutX;
                    result.y = yogaNode.LayoutY;
                    result.width = yogaNode.LayoutWidth;
                    result.height = yogaNode.LayoutHeight;
                }
                return result;
            }
            internal set
            {
                if (yogaNode == null)
                {
                    yogaNode = new YogaNode();
                }

                // Same position value while type is already manual should not trigger any layout change, return early
                if (isLayoutManual && m_Layout == value)
                    return;

                Rect lastLayout = layout;
                VersionChangeType changeType = 0;
                if (!Mathf.Approximately(lastLayout.x, value.x) || !Mathf.Approximately(lastLayout.y, value.y))
                    changeType |= VersionChangeType.Transform;
                if (!Mathf.Approximately(lastLayout.width, value.width) || !Mathf.Approximately(lastLayout.height, value.height))
                    changeType |= VersionChangeType.Size;

                // set results so we can read straight back in get without waiting for a pass
                m_Layout = value;
                isLayoutManual = true;

                // mark as inline so that they do not get overridden if needed.
                IStyle styleAccess = style;
                styleAccess.position = Position.Absolute;
                styleAccess.marginLeft = 0.0f;
                styleAccess.marginRight = 0.0f;
                styleAccess.marginBottom = 0.0f;
                styleAccess.marginTop = 0.0f;
                styleAccess.left = value.x;
                styleAccess.top = value.y;
                styleAccess.right = float.NaN;
                styleAccess.bottom = float.NaN;
                styleAccess.width = value.width;
                styleAccess.height = value.height;

                if (changeType != 0)
                    IncrementVersion(changeType);
            }
        }

        /// <summary>
        /// The rectangle of the content area of the element, in the local space of the element.
        /// </summary>
        /// <remarks>
        /// In the box model used by UI Toolkit, the content area refers to the inner rectangle for displaying text and images.
        /// It excludes the borders and the padding.
        /// </remarks>
        public Rect contentRect
        {
            get
            {
                var spacing = new Spacing(resolvedStyle.paddingLeft,
                    resolvedStyle.paddingTop,
                    resolvedStyle.paddingRight,
                    resolvedStyle.paddingBottom);

                return paddingRect - spacing;
            }
        }

        /// <summary>
        /// The rectangle of the padding area of the element, in the local space of the element.
        /// </summary>
        /// <remarks>
        /// In the box model used by UI Toolkit, the padding area refers to the inner rectangle. The inner rectangle includes
        /// the <see cref="contentRect"/> and padding, but excludes the border.
        /// </remarks>
        protected Rect paddingRect
        {
            get
            {
                var spacing = new Spacing(resolvedStyle.borderLeftWidth,
                    resolvedStyle.borderTopWidth,
                    resolvedStyle.borderRightWidth,
                    resolvedStyle.borderBottomWidth);

                return rect - spacing;
            }
        }

        internal bool isBoundingBoxDirty
        {
            get => (m_Flags & VisualElementFlags.BoundingBoxDirty) == VisualElementFlags.BoundingBoxDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.BoundingBoxDirty : m_Flags & ~VisualElementFlags.BoundingBoxDirty;
        }
        private Rect m_BoundingBox;

        internal bool isWorldBoundingBoxDirty
        {
            get => (m_Flags & VisualElementFlags.WorldBoundingBoxDirty) == VisualElementFlags.WorldBoundingBoxDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.WorldBoundingBoxDirty : m_Flags & ~VisualElementFlags.WorldBoundingBoxDirty;
        }
        private Rect m_WorldBoundingBox;

        internal Rect boundingBox
        {
            get
            {
                if (isBoundingBoxDirty)
                {
                    UpdateBoundingBox();
                    isBoundingBoxDirty = false;
                }

                return m_BoundingBox;
            }
        }

        internal Rect worldBoundingBox
        {
            get
            {
                if (isWorldBoundingBoxDirty || isBoundingBoxDirty)
                {
                    UpdateWorldBoundingBox();
                    isWorldBoundingBoxDirty = false;
                }

                return m_WorldBoundingBox;
            }
        }

        private Rect boundingBoxInParentSpace
        {
            get
            {
                var bb = boundingBox;
                TransformAlignedRectToParentSpace(ref bb);
                return bb;
            }
        }

        internal void UpdateBoundingBox()
        {
            if (float.IsNaN(rect.x) || float.IsNaN(rect.y) || float.IsNaN(rect.width) || float.IsNaN(rect.height))
            {
                // Ignored unlayouted VisualElements.
                m_BoundingBox = Rect.zero;
            }
            else
            {
                m_BoundingBox = rect;
                if (!ShouldClip())
                {
                    var childCount = m_Children.Count;
                    for (int i = 0; i < childCount; i++)
                    {
                        var childBB = m_Children[i].boundingBoxInParentSpace;
                        m_BoundingBox.xMin = Math.Min(m_BoundingBox.xMin, childBB.xMin);
                        m_BoundingBox.xMax = Math.Max(m_BoundingBox.xMax, childBB.xMax);
                        m_BoundingBox.yMin = Math.Min(m_BoundingBox.yMin, childBB.yMin);
                        m_BoundingBox.yMax = Math.Max(m_BoundingBox.yMax, childBB.yMax);
                    }
                }
            }

            isWorldBoundingBoxDirty = true;
        }

        internal void UpdateWorldBoundingBox()
        {
            m_WorldBoundingBox = boundingBox;
            TransformAlignedRect(ref worldTransformRef, ref m_WorldBoundingBox);
        }

        /// <summary>
        /// AABB after applying the world transform to <c>rect</c>.
        /// </summary>
        public Rect worldBound
        {
            get
            {
                var result = rect;
                TransformAlignedRect(ref worldTransformRef, ref result);
                return result;
            }
        }

        /// <summary>
        /// AABB after applying the transform to the rect, but before applying the layout translation.
        /// </summary>
        public Rect localBound
        {
            get
            {
                var r = rect;
                TransformAlignedRectToParentSpace(ref r);
                return r;
            }
        }

        internal Rect rect
        {
            get
            {
                var l = layout;
                return new Rect(0.0f, 0.0f, l.width, l.height);
            }
        }

        internal bool isWorldTransformDirty
        {
            get => (m_Flags & VisualElementFlags.WorldTransformDirty) == VisualElementFlags.WorldTransformDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.WorldTransformDirty : m_Flags & ~VisualElementFlags.WorldTransformDirty;
        }

        internal bool isWorldTransformInverseDirty
        {
            get => (m_Flags & VisualElementFlags.WorldTransformInverseDirty) == VisualElementFlags.WorldTransformInverseDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.WorldTransformInverseDirty : m_Flags & ~VisualElementFlags.WorldTransformInverseDirty;
        }

        private Matrix4x4 m_WorldTransformCache = Matrix4x4.identity;
        private Matrix4x4 m_WorldTransformInverseCache = Matrix4x4.identity;

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
                    UpdateWorldTransform();
                return m_WorldTransformCache;
            }
        }

        internal ref Matrix4x4 worldTransformRef
        {
            get
            {
                if (isWorldTransformDirty)
                    UpdateWorldTransform();
                return ref m_WorldTransformCache;
            }
        }

        internal ref Matrix4x4 worldTransformInverse
        {
            get
            {
                if (isWorldTransformDirty || isWorldTransformInverseDirty)
                    UpdateWorldTransformInverse();
                return ref m_WorldTransformInverseCache;
            }
        }

        internal void UpdateWorldTransform()
        {
            // If we are during a layout we don't want to remove the dirty transform flag
            // since this could lead to invalid computed transform (see ScopeContentainer.DoMeasure)
            if (elementPanel != null && !elementPanel.duringLayoutPhase)
            {
                isWorldTransformDirty = false;
            }

            if (hierarchy.parent != null)
            {
                if (hasDefaultRotationAndScale)
                {
                    TranslateMatrix34(ref hierarchy.parent.worldTransformRef, positionWithLayout, out m_WorldTransformCache);
                }
                else
                {
                    var mat = pivotedMatrixWithLayout;
                    MultiplyMatrix34(ref hierarchy.parent.worldTransformRef,  ref mat, out m_WorldTransformCache);
                }
            }
            else
            {
                m_WorldTransformCache = pivotedMatrixWithLayout;
            }

            isWorldTransformInverseDirty = true;
            isWorldBoundingBoxDirty = true;
        }

        internal void UpdateWorldTransformInverse()
        {
            Matrix4x4.Inverse3DAffine(worldTransform, ref m_WorldTransformInverseCache);
            isWorldTransformInverseDirty = false;
        }

        internal bool isWorldClipDirty
        {
            get => (m_Flags & VisualElementFlags.WorldClipDirty) == VisualElementFlags.WorldClipDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.WorldClipDirty : m_Flags & ~VisualElementFlags.WorldClipDirty;
        }

        private Rect m_WorldClip = Rect.zero;
        private Rect m_WorldClipMinusGroup = Rect.zero;
        private bool m_WorldClipIsInfinite = false;
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
        internal Rect worldClipMinusGroup
        {
            get
            {
                if (isWorldClipDirty)
                {
                    UpdateWorldClip();
                    isWorldClipDirty = false;
                }
                return m_WorldClipMinusGroup;
            }
        }

        internal bool worldClipIsInfinite
        {
            get
            {
                if (isWorldClipDirty)
                {
                    UpdateWorldClip();
                    isWorldClipDirty = false;
                }
                return m_WorldClipIsInfinite;
            }
        }

        internal void EnsureWorldTransformAndClipUpToDate()
        {
            if (isWorldTransformDirty)
                UpdateWorldTransform();
            if (isWorldClipDirty)
            {
                UpdateWorldClip();
                isWorldClipDirty = false;
            }
        }

        internal static readonly Rect s_InfiniteRect = new Rect(-10000, -10000, 40000, 40000);

        private void UpdateWorldClip()
        {
            if (hierarchy.parent != null)
            {
                m_WorldClip = hierarchy.parent.worldClip;

                bool parentWorldClipIsInfinite = hierarchy.parent.worldClipIsInfinite;
                if (hierarchy.parent != renderChainData.groupTransformAncestor) // Accessing render data here?
                {
                    m_WorldClipMinusGroup = hierarchy.parent.worldClipMinusGroup;
                }
                else
                {
                    parentWorldClipIsInfinite = true;
                    m_WorldClipMinusGroup = s_InfiniteRect;
                }

                if (ShouldClip())
                {
                    // Case 1222517: We must substract before intersection. Otherwise, if the parent world clip
                    // boundary happens to be overlapping the element, we may be over-substracting. Also clamping must
                    // be the last operation that's performed.
                    Rect wb = SubstractBorderPadding(worldBound);

                    m_WorldClip = CombineClipRects(wb, m_WorldClip);
                    m_WorldClipMinusGroup = parentWorldClipIsInfinite ? wb : CombineClipRects(wb, m_WorldClipMinusGroup);

                    m_WorldClipIsInfinite = false;
                }
                else
                {
                    m_WorldClipIsInfinite = parentWorldClipIsInfinite;
                }
            }
            else
            {
                m_WorldClipMinusGroup = m_WorldClip = (panel != null) ? panel.visualTree.rect : s_InfiniteRect;;
                m_WorldClipIsInfinite = true;
            }
        }

        private Rect CombineClipRects(Rect rect, Rect parentRect)
        {
            float x1 = Mathf.Max(rect.xMin, parentRect.xMin);
            float x2 = Mathf.Min(rect.xMax, parentRect.xMax);
            float y1 = Mathf.Max(rect.yMin, parentRect.yMin);
            float y2 = Mathf.Min(rect.yMax, parentRect.yMax);
            float width = Mathf.Max(x2 - x1, 0);
            float height = Mathf.Max(y2 - y1, 0);
            return new Rect(x1, y1, width, height);
        }

        private Rect SubstractBorderPadding(Rect worldRect)
        {
            // Case 1222517: We must take the scaling into consideration when applying local changes to the world rect.
            float xScale = worldTransform.m00;
            float yScale = worldTransform.m11;

            worldRect.x += resolvedStyle.borderLeftWidth * xScale;
            worldRect.y += resolvedStyle.borderTopWidth * yScale;
            worldRect.width -= (resolvedStyle.borderLeftWidth + resolvedStyle.borderRightWidth) * xScale;
            worldRect.height -= (resolvedStyle.borderTopWidth + resolvedStyle.borderBottomWidth) * yScale;

            if (computedStyle.unityOverflowClipBox == OverflowClipBox.ContentBox)
            {
                worldRect.x += resolvedStyle.paddingLeft * xScale;
                worldRect.y += resolvedStyle.paddingTop * yScale;
                worldRect.width -= (resolvedStyle.paddingLeft + resolvedStyle.paddingRight) * xScale;
                worldRect.height -= (resolvedStyle.paddingTop + resolvedStyle.paddingBottom) * yScale;
            }

            return worldRect;
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
                PseudoStates diff = m_PseudoStates ^ value;
                if ((int)diff > 0)
                {
                    m_PseudoStates = value;

                    if ((m_PseudoStates & PseudoStates.Root) == PseudoStates.Root)
                        isRootVisualContainer = true;


                    // If only the root changed do not trigger a new style update since the root
                    // pseudo state change base on the current style sheet when selectors are matched.
                    if (diff == PseudoStates.Root)
                        return;

                    if ((triggerPseudoMask & m_PseudoStates) != 0
                        || (dependencyPseudoMask & ~m_PseudoStates) != 0)
                    {
                        IncrementVersion(VersionChangeType.StyleSheet);
                    }
                }
            }
        }

        /// <summary>
        /// Determines if this element can be pick during mouseEvents or <see cref="IPanel.Pick"/> queries.
        /// </summary>
        public PickingMode pickingMode { get; set; }

        /// <summary>
        /// The name of this VisualElement.
        /// </summary>
        /// <remarks>
        /// Use this property to write USS selectors that target a specific element.
        /// The standard practice is to give an element a unique name.
        /// </remarks>
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
            get
            {
                if (ReferenceEquals(m_ClassList, s_EmptyClassList))
                {
                    m_ClassList = StringObjectListPool.Get();
                }

                return m_ClassList;
            }
        }

        internal string fullTypeName => typeData.fullTypeName;
        internal string typeName => typeData.typeName;

        // Set and pass in values to be used for layout
        internal YogaNode yogaNode { get; private set; }

        internal ComputedStyle m_Style = InitialStyle.Acquire();
        internal ref ComputedStyle computedStyle => ref m_Style;

        // Variables that children inherit
        internal StyleVariableContext variableContext = StyleVariableContext.none;

        // Hash of the inherited style data values
        internal int inheritedStylesHash = 0;

        internal bool hasInlineStyle => inlineStyleAccess != null;

        internal bool styleInitialized
        {
            get => (m_Flags & VisualElementFlags.StyleInitialized) == VisualElementFlags.StyleInitialized;
            set => m_Flags = value ? m_Flags | VisualElementFlags.StyleInitialized : m_Flags & ~VisualElementFlags.StyleInitialized;
        }

        // Opacity is not fully supported so it's hidden from public API for now
        internal float opacity
        {
            get { return resolvedStyle.opacity; }
            set
            {
                style.opacity = value;
            }
        }

        internal readonly uint controlid;

        // IMGUIContainers are special snowflakes that need custom treatment regarding events.
        // This enables early outs in some dispatching strategies.
        // see focusable.isIMGUIContainer;
        internal int imguiContainerDescendantCount = 0;

        private void ChangeIMGUIContainerCount(int delta)
        {
            VisualElement ve = this;

            while (ve != null)
            {
                ve.imguiContainerDescendantCount += delta;
                ve = ve.hierarchy.parent;
            }
        }

        /// <summary>
        ///  Initializes and returns an instance of VisualElement.
        /// </summary>
        public VisualElement()
        {
            m_Children = s_EmptyList;
            controlid = ++s_NextId;

            hierarchy = new Hierarchy(this);

            m_ClassList = s_EmptyClassList;
            m_Flags = VisualElementFlags.Init;
            SetEnabled(true);

            focusable = false;

            name = string.Empty;
            yogaNode = new YogaNode();

            renderHints = RenderHints.None;
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);
            if (evt == null)
            {
                return;
            }

            if (evt.eventTypeId == MouseOverEvent.TypeId() || evt.eventTypeId == MouseOutEvent.TypeId())
            {
                // Updating cursor has to happen on MouseOver/Out because exiting a children do not send a mouse enter to the parent.
                UpdateCursorStyle(evt.eventTypeId);
            }
            else if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                var capturingElement = panel?.GetCapturingElement(PointerId.mousePointerId);
                if (capturingElement == null || capturingElement == this)
                    pseudoStates |= PseudoStates.Hover;
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId())
            {
                pseudoStates &= ~PseudoStates.Hover;
            }
            else if (evt.eventTypeId == BlurEvent.TypeId())
            {
                pseudoStates = pseudoStates & ~PseudoStates.Focus;
            }
            else if (evt.eventTypeId == FocusEvent.TypeId())
            {
                pseudoStates = pseudoStates | PseudoStates.Focus;
            }
            else
            {
                HandlePanelAttachmentEvents(evt);
            }

        }

        public sealed override void Focus()
        {
            if (!canGrabFocus && hierarchy.parent != null)
            {
                hierarchy.parent.Focus();
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

                EventDispatcherGate? pDispatcherGate = null;
                if (p?.dispatcher != null)
                {
                    pDispatcherGate = new EventDispatcherGate(p.dispatcher);
                }

                EventDispatcherGate? panelDispatcherGate = null;
                if (panel?.dispatcher != null && panel.dispatcher != p?.dispatcher)
                {
                    panelDispatcherGate = new EventDispatcherGate(panel.dispatcher);
                }

                BaseVisualElementPanel previousPanel = elementPanel;
                var previousHierarchyVersion = previousPanel?.hierarchyVersion ?? 0;

                using (pDispatcherGate)
                using (panelDispatcherGate)
                {
                    foreach (var e in elements)
                    {
                        e.WillChangePanel(p);
                    }

                    var hierarchyVersion = previousPanel?.hierarchyVersion ?? 0;
                    if (previousHierarchyVersion != hierarchyVersion)
                    {
                        // Update the elements list since the hierarchy has changed after sending the detach events
                        elements.Clear();
                        elements.Add(this);
                        GatherAllChildren(elements);
                    }

                    VisualElementFlags flagToAdd = p != null ? VisualElementFlags.NeedsAttachToPanelEvent : 0;

                    foreach (var e in elements)
                    {
                        e.elementPanel = p;
                        e.m_Flags |= flagToAdd;
                    }

                    foreach (var e in elements)
                    {
                        e.HasChangedPanel(previousPanel);
                    }
                }
            }
            finally
            {
                VisualElementListPool.Release(elements);
            }
        }

        void WillChangePanel(BaseVisualElementPanel destinationPanel)
        {
            if (panel != null)
            {
                // Only send this event if the element isn't waiting for an attach event already
                if ((m_Flags & VisualElementFlags.NeedsAttachToPanelEvent) == 0)
                {
                    using (var e = DetachFromPanelEvent.GetPooled(panel, destinationPanel))
                    {
                        e.target = this;
                        elementPanel.SendEvent(e, DispatchMode.Immediate);
                    }
                }

                UnregisterRunningAnimations();
            }
        }

        void HasChangedPanel(BaseVisualElementPanel prevPanel)
        {
            if (panel != null)
            {
                yogaNode.Config = elementPanel.yogaConfig;
                RegisterRunningAnimations();

                //We need to reset any visual pseudo state
                pseudoStates &= ~(PseudoStates.Focus | PseudoStates.Active | PseudoStates.Hover);

                // Only send this event if the element hasn't received it yet
                if ((m_Flags & VisualElementFlags.NeedsAttachToPanelEvent) == VisualElementFlags.NeedsAttachToPanelEvent)
                {
                    using (var e = AttachToPanelEvent.GetPooled(prevPanel, panel))
                    {
                        e.target = this;
                        elementPanel.SendEvent(e, DispatchMode.Immediate);
                    }
                    m_Flags &= ~VisualElementFlags.NeedsAttachToPanelEvent;
                }
            }
            else
            {
                yogaNode.Config = YogaConfig.Default;
            }


            // styles are dependent on topology
            styleInitialized = false;
            IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Transform);

            // persistent data key may have changed or needs initialization
            if (!string.IsNullOrEmpty(viewDataKey))
                IncrementVersion(VersionChangeType.ViewData);
        }

        /// <summary>
        /// Sends an event to the event handler.
        /// </summary>
        /// <param name="e">The event to send.</param>
        /// <remarks>
        /// This forwards the event to the event dispatcher.
        /// </remarks>
        public sealed override void SendEvent(EventBase e)
        {
            elementPanel?.SendEvent(e);
        }

        internal sealed override void SendEvent(EventBase e, DispatchMode dispatchMode)
        {
            elementPanel?.SendEvent(e, dispatchMode);
        }

        internal void IncrementVersion(VersionChangeType changeType)
        {
            elementPanel?.OnVersionChanged(this, changeType);
        }

        internal void InvokeHierarchyChanged(HierarchyChangeType changeType) { elementPanel?.InvokeHierarchyChanged(this, changeType); }

        [Obsolete("SetEnabledFromHierarchy is deprecated and will be removed in a future release. Please use SetEnabled instead.")]
        protected internal bool SetEnabledFromHierarchy(bool state)
        {
            return SetEnabledFromHierarchyPrivate(state);
        }

        //TODO: rename to SetEnabledFromHierarchy once the protected version has been removed
        private bool SetEnabledFromHierarchyPrivate(bool state)
        {
            var initialState = enabledInHierarchy;
            bool disable = false;
            if (state)
            {
                if (isParentEnabledInHierarchy)
                {
                    if (enabledSelf)
                    {
                        RemoveFromClassList(disabledUssClassName);
                    }
                    else
                    {
                        disable = true;
                        AddToClassList(disabledUssClassName);
                    }
                }
                else
                {
                    disable = true;
                    RemoveFromClassList(disabledUssClassName);
                }
            }
            else
            {
                disable = true;
                EnableInClassList(disabledUssClassName, isParentEnabledInHierarchy);
            }

            if (disable)
            {
                if (focusController != null && focusController.IsFocused(this))
                {
                    EventDispatcherGate? dispatcherGate = null;
                    if (panel?.dispatcher != null)
                    {
                        dispatcherGate = new EventDispatcherGate(panel.dispatcher);
                    }

                    using (dispatcherGate)
                    {
                        BlurImmediately();
                    }
                }

                pseudoStates |= PseudoStates.Disabled;
            }
            else
            {
                pseudoStates &= ~PseudoStates.Disabled;
            }

            return initialState != enabledInHierarchy;
        }

        private bool isParentEnabledInHierarchy
        {
            get { return hierarchy.parent == null || hierarchy.parent.enabledInHierarchy; }
        }

        //Returns true if 'this' can be enabled relative to the enabled state of its panel
        /// <summary>
        /// Returns true if the <see cref="VisualElement"/> is enabled in its own hierarchy.
        /// </summary>
        /// <remarks>
        ///  This flag verifies if the element is enabled globally. A parent disabling its child VisualElement affects this variable.
        /// </remarks>
        public bool enabledInHierarchy
        {
            get { return (pseudoStates & PseudoStates.Disabled) != PseudoStates.Disabled; }
        }

        //Returns the local enabled state
        /// <summary>
        /// Returns true if the <see cref="VisualElement"/> is enabled locally.
        /// </summary>
        /// <remarks>
        /// This flag isn't changed if the VisualElement is disabled implicitely by one of its parents. To verify this, use enabledInHierarchy.
        /// </remarks>
        public bool enabledSelf { get; private set;}

        /// <summary>
        /// Changes the <see cref="VisualElement"/> enabled state. A disabled VisualElement does not receive most events.
        /// </summary>
        /// <param name="value">New enabled state</param>
        /// <remarks>
        /// The method disables the local flag of the VisualElement and implicitly disables its children.
        /// It does not affect the local enabled flag of each child.
        /// </remarks>
        public void SetEnabled(bool value)
        {
            if (enabledSelf == value) return;

            enabledSelf = value;
            PropagateEnabledToChildren(value);
        }

        void PropagateEnabledToChildren(bool value)
        {
            if (SetEnabledFromHierarchyPrivate(value))
            {
                var count = m_Children.Count;
                for (int i = 0; i < count; ++i)
                {
                    m_Children[i].PropagateEnabledToChildren(value);
                }
            }
        }

        /// <summary>
        /// Indicates whether or not this element should be rendered.
        /// </summary>
        /// <remarks>
        /// The value of this property reflects the value of <see cref="IResolvedStyle.visibility"/> for this element.
        /// The value is true for <see cref="Visibility.Visible"/> and false for <see cref="Visibility.Hidden"/>.
        /// Writing to this property writes to <see cref="IStyle.visibility"/>.
        /// <seealso cref="resolvedStyle"/>
        /// <seealso cref="style"/>
        /// </remarks>
        public bool visible
        {
            get
            {
                return resolvedStyle.visibility == Visibility.Visible;
            }
            set
            {
                // Note: this could causes an allocation because styles are copy-on-write
                // we might want to remove this setter altogether
                // so everything goes through style.visibility (and then it's documented in a single place)
                style.visibility = value ? Visibility.Visible : Visibility.Hidden;
            }
        }

        /// <summary>
        /// Triggers a repaint of the <see cref="VisualElement"/> on the next frame.
        /// </summary>
        public void MarkDirtyRepaint()
        {
            IncrementVersion(VersionChangeType.Repaint);
        }

        /// <summary>
        /// Called when the <see cref="VisualElement"/> visual contents need to be (re)generated.
        /// </summary>
        /// <remarks>
        /// <para>When this delegate is handled, you can generate custom geometry in the content region of the <see cref="VisualElement"/>. For an example, see the <see cref="MeshGenerationContext"/> documentation.</para>
        /// <para>This delegate is called only when the <see cref="VisualElement"/> needs to regenerate its visual contents. It is not called every frame when the panel refreshes. The generated content is cached, and remains intact until any of the <see cref="VisualElement"/>'s properties that affects visuals either changes, or <see cref="VisualElement.MarkDirtyRepaint"/> is called.</para>
        /// <para>When you execute code in a handler to this delegate, do not make changes to any property of the <see cref="VisualElement"/>. A handler should treat the <see cref="VisualElement"/> as 'read-only'. Changing the <see cref="VisualElement"/> during this event might cause undesirable side effects. For example, the changes might lag, or be missed completely.</para>
        /// </remarks>
        public Action<MeshGenerationContext> generateVisualContent { get; set; }

        Unity.Profiling.ProfilerMarker k_GenerateVisualContentMarker = new Unity.Profiling.ProfilerMarker("GenerateVisualContent");

        internal void InvokeGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (generateVisualContent != null)
            {
                try
                {
                    using (k_GenerateVisualContentMarker.Auto())
                        generateVisualContent(mgc);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal void GetFullHierarchicalViewDataKey(StringBuilder key)
        {
            const string keySeparator = "__";

            if (parent != null)
                parent.GetFullHierarchicalViewDataKey(key);

            if (!string.IsNullOrEmpty(viewDataKey))
            {
                key.Append(keySeparator);
                key.Append(viewDataKey);
            }
        }

        internal string GetFullHierarchicalViewDataKey()
        {
            StringBuilder key = new StringBuilder();

            GetFullHierarchicalViewDataKey(key);

            return key.ToString();
        }

        internal T GetOrCreateViewData<T>(object existing, string key) where T : class, new()
        {
            Debug.Assert(elementPanel != null, "VisualElement.elementPanel is null! Cannot load persistent data.");

            var viewData = elementPanel == null || elementPanel.getViewDataDictionary == null ? null : elementPanel.getViewDataDictionary();

            // If persistency is disable (no data, no key, no key one of the parents), just return the
            // existing data or create a local one if none exists.
            if (viewData == null || string.IsNullOrEmpty(viewDataKey) || enableViewDataPersistence == false)
            {
                if (existing != null)
                    return existing as T;

                return new T();
            }

            string keyWithType = key + "__" + typeof(T);

            if (!viewData.ContainsKey(keyWithType))
                viewData.Set(keyWithType, new T());

            return viewData.Get<T>(keyWithType);
        }

        internal T GetOrCreateViewData<T>(ScriptableObject existing, string key) where T : ScriptableObject
        {
            Debug.Assert(elementPanel != null, "VisualElement.elementPanel is null! Cannot load view data.");

            var viewData = elementPanel == null || elementPanel.getViewDataDictionary == null ? null : elementPanel.getViewDataDictionary();

            // If persistency is disable (no data, no key, no key one of the parents), just return the
            // existing data or create a local one if none exists.
            if (viewData == null || string.IsNullOrEmpty(viewDataKey) || enableViewDataPersistence == false)
            {
                if (existing != null)
                    return existing as T;

                return ScriptableObject.CreateInstance<T>();
            }

            string keyWithType = key + "__" + typeof(T);

            if (!viewData.ContainsKey(keyWithType))
                viewData.Set(keyWithType, ScriptableObject.CreateInstance<T>());

            return viewData.GetScriptable<T>(keyWithType);
        }

        internal void OverwriteFromViewData(object obj, string key)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Debug.Assert(elementPanel != null, "VisualElement.elementPanel is null! Cannot load view data.");

            var viewDataPersistentData = elementPanel == null || elementPanel.getViewDataDictionary == null ? null : elementPanel.getViewDataDictionary();

            // If persistency is disable (no data, no key, no key one of the parents), just return the
            // existing data or create a local one if none exists.
            if (viewDataPersistentData == null || string.IsNullOrEmpty(viewDataKey) || enableViewDataPersistence == false)
            {
                return;
            }

            string keyWithType = key + "__" + obj.GetType();

            if (!viewDataPersistentData.ContainsKey(keyWithType))
            {
                viewDataPersistentData.Set(keyWithType, obj);
                return;
            }

            viewDataPersistentData.Overwrite(obj, keyWithType);
        }

        internal void SaveViewData()
        {
            if (elementPanel != null
                && elementPanel.saveViewData != null
                && !string.IsNullOrEmpty(viewDataKey)
                && enableViewDataPersistence)
            {
                elementPanel.saveViewData();
            }
        }

        internal bool IsViewDataPersitenceSupportedOnChildren(bool existingState)
        {
            bool newState = existingState;

            // If this element has no key AND this element has a custom contentContainer,
            // turn off view data persistence. This essentially turns off persistence
            // on shadow elements if the parent has no key.
            if (string.IsNullOrEmpty(viewDataKey) && this != contentContainer)
                newState = false;

            // However, once we enter the light tree again, we need to turn persistence back on.
            if (parent != null && this == parent.contentContainer)
                newState = true;

            return newState;
        }

        internal void OnViewDataReady(bool enablePersistence)
        {
            this.enableViewDataPersistence = enablePersistence;
            OnViewDataReady();
        }

        internal virtual void OnViewDataReady() {}


        /// <summary>
        /// Checks if the specified point intersects with this VisualElement's layout.
        /// </summary>
        /// <remarks>
        /// Unity calls this method to find out what elements are under a cursor (such as a mouse).
        /// Do not rely on this method to perform invalidation,
        /// since Unity might cache results or skip some invocations of this method for performance reasons.
        /// By default, a VisualElement has a rectangular area. Override this method in your VisualElement subclass to customize this behaviour.
        /// </remarks>
        /// <param name="localPoint">The point in the local space of the element.</param>
        /// <returns>Returns true if the point is contained within the element's layout. Otherwise, returns false.</returns>
        /// TODO rect is internal, yet it's probably what users would want to use in this case
        public virtual bool ContainsPoint(Vector2 localPoint)
        {
            return rect.Contains(localPoint);
        }

        /// <undoc/>
        // TODO this is only used by GraphView... should we maybe remove it from the API or make it internal?
        public virtual bool Overlaps(Rect rectangle)
        {
            return rect.Overlaps(rectangle, true);
        }

        /// <summary>
        /// The modes available to measure <see cref="VisualElement"/> sizes.
        /// </summary>
        /// <seealso cref="TextElement.MeasureTextSize"/>
        public enum MeasureMode
        {
            /// <summary>
            /// The element should give its preferred width/height without any constraint.
            /// </summary>
            Undefined = YogaMeasureMode.Undefined,
            /// <summary>
            /// The element should give the width/height that is passed in and derive the opposite site from this value (for example, calculate text size from a fixed width).
            /// </summary>
            Exactly = YogaMeasureMode.Exactly,
            /// <summary>
            /// At Most. The element should give its preferred width/height but no more than the value passed.
            /// </summary>
            AtMost = YogaMeasureMode.AtMost
        }

        internal bool requireMeasureFunction
        {
            get => (m_Flags & VisualElementFlags.RequireMeasureFunction) == VisualElementFlags.RequireMeasureFunction;
            set
            {
                m_Flags = value ? m_Flags | VisualElementFlags.RequireMeasureFunction : m_Flags & ~VisualElementFlags.RequireMeasureFunction;
                if (value && !yogaNode.IsMeasureDefined)
                {
                    AssignMeasureFunction();
                }
                else if (!value && yogaNode.IsMeasureDefined)
                {
                    RemoveMeasureFunction();
                }
            }
        }

        private void AssignMeasureFunction()
        {
            yogaNode.SetMeasureFunction((node, f, mode, f1, heightMode) => Measure(node, f, mode, f1, heightMode));
        }

        private void RemoveMeasureFunction()
        {
            yogaNode.SetMeasureFunction(null);
        }

        /// <undoc/>
        /// TODO this is public but since "requiresMeasureFunction" is internal this is not useful for users
        protected internal virtual Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            return new Vector2(float.NaN, float.NaN);
        }

        internal YogaSize Measure(YogaNode node, float width, YogaMeasureMode widthMode, float height, YogaMeasureMode heightMode)
        {
            Debug.Assert(node == yogaNode, "YogaNode instance mismatch");
            Vector2 size = DoMeasure(width, (MeasureMode)widthMode, height, (MeasureMode)heightMode);
            float ppp = scaledPixelsPerPoint;
            return MeasureOutput.Make(AlignmentUtils.RoundToPixelGrid(size.x, ppp), AlignmentUtils.RoundToPixelGrid(size.y, ppp));
        }

        internal void SetSize(Vector2 size)
        {
            var pos = layout;
            pos.width = size.x;
            pos.height = size.y;
            layout = pos;
        }

        void FinalizeLayout()
        {
            if (hasInlineStyle || hasRunningAnimations)
            {
                computedStyle.SyncWithLayout(yogaNode);
            }
            else
            {
                yogaNode.CopyStyle(computedStyle.yogaNode);
            }
        }

        internal void SetInlineRule(StyleSheet sheet, StyleRule rule)
        {
            if (inlineStyleAccess == null)
                inlineStyleAccess = new InlineStyleAccess(this);

            inlineStyleAccess.SetInlineRule(sheet, rule);
        }

        // Used by the builder to apply the inline styles without passing by SetComputedStyle
        internal void UpdateInlineRule(StyleSheet sheet, StyleRule rule)
        {
            var oldStyle = computedStyle.Acquire();

            var rulesHash = computedStyle.matchingRulesHash;
            if (!StyleCache.TryGetValue(rulesHash, out var baseComputedStyle))
                baseComputedStyle = InitialStyle.Get();

            m_Style.CopyFrom(ref baseComputedStyle);

            SetInlineRule(sheet, rule);
            FinalizeLayout();

            var changes = ComputedStyle.CompareChanges(ref oldStyle, ref computedStyle);
            oldStyle.Release();

            IncrementVersion(changes);
        }

        internal void SetComputedStyle(ref ComputedStyle newStyle)
        {
            // When a parent class list change all children get their styles recomputed.
            // A lot of time the children won't change and the same style will get computed so we can early exit in that case.
            if (m_Style.matchingRulesHash == newStyle.matchingRulesHash)
                return;

            var changes = ComputedStyle.CompareChanges(ref m_Style, ref newStyle);

            // Here we do a "smart" copy of the style instead of just acquiring them to prevent additional GC alloc.
            // If this element has no inline styles it will release the current style data group and acquire the new one.
            // However, when there a inline styles the style data group that is inline will have a ref count of 1
            // so instead of releasing it and acquiring a new one we just copy the data to save on GC alloc.
            m_Style.CopyFrom(ref newStyle);

            FinalizeLayout();

            IncrementVersion(changes);
        }

        internal void ResetPositionProperties()
        {
            if (!hasInlineStyle)
            {
                return;
            }

            style.position = StyleKeyword.Null;
            style.marginLeft = StyleKeyword.Null;
            style.marginRight = StyleKeyword.Null;
            style.marginBottom = StyleKeyword.Null;
            style.marginTop = StyleKeyword.Null;
            style.left = StyleKeyword.Null;
            style.top = StyleKeyword.Null;
            style.right = StyleKeyword.Null;
            style.bottom = StyleKeyword.Null;
            style.width = StyleKeyword.Null;
            style.height = StyleKeyword.Null;
        }

        public override string ToString()
        {
            return GetType().Name + " " + name + " " + layout + " world rect: " + worldBound;
        }

        /// <summary>
        /// Retrieve the classes for this element.
        /// </summary>
        /// <returns>A class list.</returns>
        public IEnumerable<string> GetClasses()
        {
            return m_ClassList;
        }

        // needed to avoid boxing allocation when iterating on the list.
        internal List<string> GetClassesForIteration()
        {
            return m_ClassList;
        }

        /// <summary>
        /// Removes all classes from the class list of this element.
        /// <seealso cref="AddToClassList"/>
        /// </summary>
        /// <remarks>
        /// This method might cause unexpected results for built-in Unity elements,
        /// since they might rely on classes to be present in their list to function.
        /// </remarks>
        public void ClearClassList()
        {
            if (m_ClassList.Count > 0)
            {
                StringObjectListPool.Release(m_ClassList);
                m_ClassList = s_EmptyClassList;
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        /// <summary>
        /// Adds a class to the class list of the element in order to assign styles from USS.
        /// </summary>
        /// <param name="className">The name of the class to add to the list.</param>
        public void AddToClassList(string className)
        {
            if (m_ClassList == s_EmptyClassList)
            {
                m_ClassList = StringObjectListPool.Get();
            }
            else
            {
                if (m_ClassList.Contains(className))
                {
                    return;
                }

                // Avoid list size doubling when list is full.
                if (m_ClassList.Capacity == m_ClassList.Count)
                {
                    m_ClassList.Capacity += 1;
                }
            }

            m_ClassList.Add(className);
            IncrementVersion(VersionChangeType.StyleSheet);
        }

        /// <summary>
        /// Removes a class from the class list of the element.
        /// </summary>
        /// <param name="className">The name of the class to remove to the list.</param>
        public void RemoveFromClassList(string className)
        {
            if (m_ClassList.Remove(className))
            {
                if (m_ClassList.Count == 0)
                {
                    StringObjectListPool.Release(m_ClassList);
                    m_ClassList = s_EmptyClassList;
                }
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        /// <summary>
        /// Toggles between adding and removing the given class name from the class list.
        /// </summary>
        /// <param name="className">The class name to add or remove from the class list.</param>
        /// <remarks>
        /// Checks for the given class name in the element class list. If the class name is found, it is removed from the class list. If the class name is not found, the class name is added to the class list.
        /// </remarks>
        public void ToggleInClassList(string className)
        {
            if (ClassListContains(className))
                RemoveFromClassList(className);
            else
                AddToClassList(className);
        }

        /// <summary>
        /// Enables or disables the class with the given name.
        /// </summary>
        /// <param name="className">The name of the class to enable or disable.</param>
        /// <param name="enable">A boolean flag that adds or removes the class name from the class list. If true, EnableInClassList adds the class name to the class list. If false, EnableInClassList removes the class name from the class list.</param>
        /// <remarks>
        /// If enable is true, EnableInClassList adds the class name to the class list. If enable is false, EnableInClassList removes the class name from the class list.
        /// </remarks>
        public void EnableInClassList(string className, bool enable)
        {
            if (enable)
                AddToClassList(className);
            else
                RemoveFromClassList(className);
        }

        /// <summary>
        /// Searches for a class in the class list of this element.
        /// </summary>
        /// <param name="cls">The name of the class for the search query.</param>
        /// <returns>Returns true if the class is part of the list. Otherwise, returns false.</returns>
        public bool ClassListContains(string cls)
        {
            for (int i = 0; i < m_ClassList.Count; i++)
            {
                if (m_ClassList[i] == cls)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Searches up the hierarchy of this VisualElement and retrieves stored userData, if any is found.
        /// </summary>
        /// <remarks>
        /// This ignores the current userData and returns the first parent's non-null userData.
        /// </remarks>
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

        internal object GetProperty(PropertyName key)
        {
            CheckUserKeyArgument(key);
            TryGetPropertyInternal(key, out object value);
            return value;
        }

        internal void SetProperty(PropertyName key, object value)
        {
            CheckUserKeyArgument(key);
            SetPropertyInternal(key, value);
        }

        internal bool HasProperty(PropertyName key)
        {
            CheckUserKeyArgument(key);
            return TryGetPropertyInternal(key, out var tmp);
        }

        bool TryGetPropertyInternal(PropertyName key, out object value)
        {
            value = null;
            if (m_PropertyBag != null)
            {
                for (int i = 0; i < m_PropertyBag.Count; ++i)
                {
                    if (m_PropertyBag[i].Key == key)
                    {
                        value = m_PropertyBag[i].Value;
                        return true;
                    }
                }
            }
            return false;
        }

        static void CheckUserKeyArgument(PropertyName key)
        {
            if (PropertyName.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (key == userDataPropertyKey)
                throw new InvalidOperationException($"The {userDataPropertyKey} key is reserved by the system");
        }

        void SetPropertyInternal(PropertyName key, object value)
        {
            var kv = new PropertyBagValue(key, value);

            if (m_PropertyBag == null)
            {
                m_PropertyBag = new List<PropertyBagValue>(1);
                m_PropertyBag.Add(kv);
            }
            else
            {
                for (int i = 0; i < m_PropertyBag.Count; ++i)
                {
                    if (m_PropertyBag[i].Key == key)
                    {
                        m_PropertyBag[i] = kv;
                        return;
                    }
                }

                if (m_PropertyBag.Capacity == m_PropertyBag.Count)
                {
                    m_PropertyBag.Capacity += 1;
                }

                m_PropertyBag.Add(kv);
            }
        }

        private void UpdateCursorStyle(long eventType)
        {
            if (elementPanel != null)
            {
                if (eventType == MouseOverEvent.TypeId() && elementPanel.GetTopElementUnderPointer(PointerId.mousePointerId) == this)
                {
                    elementPanel.cursorManager.SetCursor(computedStyle.cursor);
                }
                else if (eventType == MouseOutEvent.TypeId())
                {
                    elementPanel.cursorManager.ResetCursor();
                }
            }
        }

        internal enum RenderTargetMode
        {
            None,
            NoColorConversion,
            LinearToGamma,
            GammaToLinear
        }

        RenderTargetMode m_SubRenderTargetMode = RenderTargetMode.None;
        internal RenderTargetMode subRenderTargetMode
        {
            get { return m_SubRenderTargetMode; }
            set
            {
                if (m_SubRenderTargetMode == value)
                    return;

                Debug.Assert(Application.isEditor, "subRenderTargetMode is not supported on runtime yet"); //See UIRREnderEvents. blitMaterial_LinearToGamma initialisation line 900
                m_SubRenderTargetMode = value;
                IncrementVersion(VersionChangeType.Repaint);
            }
        }

        static Material s_runtimeMaterial;
        Material getRuntimeMaterial()
        {
            if (s_runtimeMaterial != null)
                return s_runtimeMaterial;

            Shader shader = Shader.Find(UIRUtility.k_DefaultShaderName);
            Debug.Assert(shader != null, "Failed to load UIElements default shader");
            if (shader != null)
            {
                shader.hideFlags |= HideFlags.DontSaveInEditor;
                Material mat = new Material(shader);
                mat.hideFlags |= HideFlags.DontSaveInEditor;
                return s_runtimeMaterial = mat;
            }
            return null;
        }

        Material m_defaultMaterial;
        internal Material defaultMaterial
        {
            get { return m_defaultMaterial; }
            private set
            {
                if (m_defaultMaterial == value)
                    return;
                m_defaultMaterial = value;
                IncrementVersion(VersionChangeType.Repaint | VersionChangeType.Layout);
            }
        }

        internal void ApplyPlayerRenderingToEditorElement()
        {
            bool isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            subRenderTargetMode = isLinear ? RenderTargetMode.LinearToGamma : RenderTargetMode.None;
            defaultMaterial = isLinear ? getRuntimeMaterial() : null;
        }

    }

    /// <summary>
    /// VisualElementExtensions is a set of extension methods useful for VisualElement.
    /// </summary>
    public static partial class VisualElementExtensions
    {
        /// <summary>
        /// Aligns a VisualElement's left, top, right and bottom edges with the corresponding edges of its parent.
        /// </summary>
        /// <remarks>
        /// This method provides a way to set the following styles in one operation:
        /// - <see cref="IStyle.position"/> is set to <see cref="Position.Absolute"/>
        /// - <see cref="IStyle.left"/> is set to 0
        /// - <see cref="IStyle.top"/> is set to 0
        /// - <see cref="IStyle.right"/> is set to 0
        /// - <see cref="IStyle.bottom"/> is set to 0
        /// </remarks>
        /// <param name="elem">The element to be aligned with its parent</param>
        public static void StretchToParentSize(this VisualElement elem)
        {
            if (elem == null)
            {
                throw new ArgumentNullException(nameof(elem));
            }

            IStyle styleAccess = elem.style;
            styleAccess.position = Position.Absolute;
            styleAccess.left = 0.0f;
            styleAccess.top = 0.0f;
            styleAccess.right = 0.0f;
            styleAccess.bottom = 0.0f;
        }

        /// <summary>
        /// Aligns a VisualElement's left and right edges with the corresponding edges of its parent.
        /// </summary>
        /// <remarks>
        /// This method provides a way to set the following styles in one operation:
        /// - <see cref="IStyle.position"/> is set to <see cref="Position.Absolute"/>
        /// - <see cref="IStyle.left"/> is set to 0
        /// - <see cref="IStyle.right"/> is set to 0
        /// </remarks>
        /// <param name="elem">The element to be aligned with its parent</param>
        public static void StretchToParentWidth(this VisualElement elem)
        {
            if (elem == null)
            {
                throw new ArgumentNullException(nameof(elem));
            }

            IStyle styleAccess = elem.style;
            styleAccess.position = Position.Absolute;
            styleAccess.left = 0.0f;
            styleAccess.right = 0.0f;
        }

        /// <summary>
        /// Add a manipulator associated to a VisualElement.
        /// </summary>
        /// <param name="ele">VisualElement associated to the manipulator.</param>
        /// <param name="manipulator">Manipulator to be added to the VisualElement.</param>
        public static void AddManipulator(this VisualElement ele, IManipulator manipulator)
        {
            if (manipulator != null)
            {
                manipulator.target = ele;
            }
        }

        /// <summary>
        /// Remove a manipulator associated to a VisualElement.
        /// </summary>
        /// <param name="ele">VisualElement associated to the manipulator.</param>
        /// <param name="manipulator">Manipulator to be removed from the VisualElement.</param>
        public static void RemoveManipulator(this VisualElement ele, IManipulator manipulator)
        {
            if (manipulator != null)
            {
                manipulator.target = null;
            }
        }
    }

    internal static class VisualElementDebugExtensions
    {
        public static string GetDisplayName(this VisualElement ve, bool withHashCode = true)
        {
            if (ve == null) return String.Empty;

            string objectName = ve.GetType().Name;
            if (!String.IsNullOrEmpty(ve.name))
            {
                objectName += "#" + ve.name;
            }

            if (withHashCode)
            {
                objectName += " (" + ve.GetHashCode().ToString("x8") + ")";
            }

            return objectName;
        }
    }
}
