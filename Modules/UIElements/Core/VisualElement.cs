// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Profiling;
using Unity.Properties;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.UIR;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements
{
    // Keep in sync with PseudoStates in Modules/UIElements/PseudoStates.h
    // pseudo states are used for common states of a widget
    // they are addressable from CSS via the pseudo state syntax ":selected" for example
    // while css class list can solve the same problem, pseudo states are a fast commonly agreed upon path for common cases.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.GraphToolkitModule", "UnityEditor.UIToolkitAuthoringModule")]
    [Flags]
    internal enum PseudoStates
    {
        None = 0,
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
        // Need to compute world clip
        WorldClipDirty = 1 << 2,
        // Need to compute world bounding box
        EventInterestParentCategoriesDirty = 1 << 5,
        // Element is a root for composite controls
        CompositeRoot = 1 << 7,
        // Element has a custom measure function
        RequireMeasureFunction = 1 << 8,
        // Element has view data persistence
        EnableViewDataPersistence = 1 << 9,
        // Element needs to receive an AttachToPanel event
        NeedsAttachToPanelEvent = 1 << 11,
        // Element has released the LayoutNode create in its constructor and can't be used anymore
        Released = 1 << 12,
        // Element is not rendered, but we keep the generated geometry in case it is shown later
        DisableRendering = 1 << 14,
        // The DataSource tracking of the element should not ne processed when the element has not been configured properly
        DetachedDataSource = 1 << 18,
        // Element has capture on one or more pointerIds
        PointerCapture = 1 << 19,
        // Element is a root UIDocument
        IsWorldSpaceRootPanelComponent = 1 << 20,
        // Element wants a GeometryChangedEvent if any of its descendent receives one
        ReceivesHierarchyGeometryChangedEvents = 1 << 21,
        // Element itself is disabled, independently of the disabled state of its parents
        DisabledSelf = 1 << 22,
        // Element style have been initialized, so transitions can be applied to it when it changes next time
        StyleInitialized = 1 << 23,
        // Element styles need to be updated, implicitly applies to all its descendants
        StyleDirty = 1 << 24,
        // Element is an ancestor of an element with StylesDirty flag, but doesn't need to be updated itself
        StyleAncestorOfDirty = 1 << 25,
        // Element initial flags
        Init = WorldClipDirty | EventInterestParentCategoriesDirty | DetachedDataSource
    }

    /// <summary>
    /// Describes the picking behavior. See <see cref="VisualElement.pickingMode"/>.
    /// </summary>
    public enum PickingMode
    {
        /// <summary>
        /// Picking enabled. Performs picking based on the position rectangle.
        /// </summary>
        /// <remarks>
        /// This is the default Value.
        ///
        /// In the VisualElement tree hierarchy, the picking process works in reverse order to rendering:
        /// it starts with the front-most elements and proceeds step by step toward background elements.
        /// Thus, the child elements are picked before parents, and the sibling elements further down the list are
        /// picked before their predecessors.
        /// </remarks>
        Position, // todo better name

        /// <summary>
        /// Prevents this element from being the target of pointer events or
        /// from being returned by <see cref="IPanel.Pick"/> and <see cref="IPanel.PickAll"/>.
        /// </summary>
        Ignore
    }

    /// <summary>
    /// Indicates the directionality of the element's text. The value cascades to child elements.
    /// </summary>
    /// <remarks>
    /// SA: [[wiki:ui-systems/language-direction|Language direction]]
    /// </remarks>
    public enum LanguageDirection
    {
        // Keep in sync with LanguageDirection in Modules/UIElements/LanguageDirection.h

        /// <summary>
        /// Inherits the directionality from the nearest ancestor with a specified directionality.
        /// </summary>
        Inherit,
        /// <summary>
        /// Left to right language direction.
        /// </summary>
        LTR,
        /// <summary>
        /// Right to left language direction.
        /// </summary>
        RTL,
    }

    internal static class LanguageDirectionExtensions
    {

        internal static TextCore.LanguageDirection toTextCore(this LanguageDirection dir)
        {
            switch (dir)
            {
                case LanguageDirection.Inherit:
                case LanguageDirection.LTR:
                    return TextCore.LanguageDirection.LTR;
                case LanguageDirection.RTL:
                    return TextCore.LanguageDirection.RTL;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, "impossible to convert value");
            }
        }
    }

    internal static class VisualElementListPool
    {
        static ObjectPool<List<VisualElement>> pool = new ObjectPool<List<VisualElement>>(() => new List<VisualElement>(), 20);

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

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class ObjectListPool<T>
    {
        static ObjectPool<List<T>> pool = new ObjectPool<List<T>>(() => new List<T>(), 20);

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

    /// <summary>
    /// Base class for objects that are part of the UIElements visual tree.
    /// </summary>
    /// <remarks>
    /// VisualElement contains several features that are common to all controls in UIElements, such as layout, styling and event handling.
    /// Several other classes derive from it to implement custom rendering and define behaviour for controls.\\
    ///\\
    /// To inherit from VisualElement and create a custom control, refer to [[wiki:UIB-structuring-ui-custom-elements|Creating a custom control]].\\
    ///\\
    /// SA: [[wiki:UIE-uxml-element-VisualElement|UXML element VisualElement]], [[VisualElementExtensions]], [[UQueryExtensions]].
    /// </remarks>
    [UxmlElement(libraryPath = "Containers")]
    [Icon("UIToolkit/Icons/VisualElement.png")]
    public partial class VisualElement : Focusable, ITransform
    {
        // Elements with isCompositeRoot will treat their children's events during the AtTarget phase instead of
        // the BubbleUp or TrickleDown phases. However, if the event doesn't bubble up or trickle down, then only
        // the event's actual target will receive it.
        internal bool isCompositeRoot
        {
            get => (m_Flags & VisualElementFlags.CompositeRoot) == VisualElementFlags.CompositeRoot;
            set => m_Flags = value ? m_Flags | VisualElementFlags.CompositeRoot : m_Flags & ~VisualElementFlags.CompositeRoot;
        }

        // areAncestorsAndSelfDisplayed is a combination of the inherited display state and our own display state.
        // (See UIRLayoutUpdater::UpdateHierarchyDisplayed() to understand how it is set.)
        internal bool areAncestorsAndSelfDisplayed
        {
            get => (transformFlags & VisualElementTransformFlags.HierarchyDisplayed) == VisualElementTransformFlags.HierarchyDisplayed;
            set
            {
                ref var f = ref transformFlags;
                f = value ? f | VisualElementTransformFlags.HierarchyDisplayed : f & ~VisualElementTransformFlags.HierarchyDisplayed;

                if (renderData == null)
                    return;

                if (value && (renderData.pendingRepaint || renderData.pendingHierarchicalRepaint))
                    IncrementVersion(VersionChangeType.Repaint);
            }
        }

        internal bool hasOneOrMorePointerCaptures
        {
            get => (m_Flags & VisualElementFlags.PointerCapture) == VisualElementFlags.PointerCapture;
            set => m_Flags = value ? m_Flags | VisualElementFlags.PointerCapture : m_Flags & ~VisualElementFlags.PointerCapture;
        }

        internal static uint s_NextId;

        internal static readonly PropertyName userDataPropertyKey = new PropertyName("--unity-user-data");
        /// <summary>
        /// USS class name of local disabled elements.
        /// </summary>
        public static readonly string disabledUssClassName = "unity-disabled";
        internal static readonly UniqueStyleString disabledUssClassNameUnique = new(disabledUssClassName);

        string m_Name;
        StyleClassList m_ClassList;
        private Dictionary<PropertyName, object> m_PropertyBag;

        private VisualElementFlags m_Flags;
        internal VisualElementFlags flags
        {
            get {
                // The WorldClipDirty flag is managed by the RenderData, so we combine it with m_Flags when set on
                // either renderData or nestedRenderData. See the setter below for more information.
                if (((renderData?.flags & RenderDataFlags.IsClippingRectDirty) == RenderDataFlags.IsClippingRectDirty) ||
                    ((nestedRenderData?.flags & RenderDataFlags.IsClippingRectDirty) == RenderDataFlags.IsClippingRectDirty))
                    return m_Flags | VisualElementFlags.WorldClipDirty;

                return m_Flags;
            }
            set
            {
                m_Flags = value;

                // UUM-91413: We avoid setting the WorldClipDirty flag on the VisualElement as the RenderData is responsible
                // to clear it once computed, otherwise the flag will stick between updates.
                if ((m_Flags & VisualElementFlags.WorldClipDirty) == VisualElementFlags.WorldClipDirty)
                {
                    // The RenderData (nested or not) is responsible for dealing with the dirty clipping rect.
                    if (renderData != null)
                    {
                        renderData.flags |= RenderDataFlags.IsClippingRectDirty;
                        m_Flags &= ~VisualElementFlags.WorldClipDirty;
                    }
                    if (nestedRenderData != null)
                    {
                        nestedRenderData.flags |= RenderDataFlags.IsClippingRectDirty;

                        // No need to remove the flag from m_Flags as renderData is guaranteed to not be null here
                        Debug.Assert(renderData != null, "renderData should not be null when nestedRenderData is not null");
                    }

                    // If the RenderData is not created yet, we temporarily allow the flag to be set on the VisualElement.
                    // It will be reset once the RenderData is created (in UIRRenderEvents.DepthFirstOnChildAdded).
                    // Doing so avoid useless recursions to set the flags in the HierarchyFlagsUpdater.
                }
            }
        }

        internal ref VisualElementTransformFlags transformFlags => ref transformData.Flags;

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
        [CreateProperty]
        public object userData
        {
            get
            {
                if (m_PropertyBag != null)
                {
                    m_PropertyBag.TryGetValue(userDataPropertyKey, out var value);
                    return value;
                }
                return null;
            }
            set
            {
                var previous = userData;
                SetPropertyInternal(userDataPropertyKey, value);

                if (previous != userData)
                    NotifyPropertyChanged(userDataProperty);
            }
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

        private bool m_DisablePlayModeTint = false;

        /// <summary>
        /// Play-mode tint is applied by default unless this is set to true. It's applied hierarchically to this <see cref="VisualElement"/> and to all its children that exist on an editor panel.
        /// </summary>
        [CreateProperty]
        public bool disablePlayModeTint
        {
            get
            {
                if (panel?.contextType == ContextType.Player || m_DisablePlayModeTint)
                    return true;
                for (var p = parent; p != null; p = p.parent)
                {
                    if (p.m_DisablePlayModeTint)
                        return true;
                }

                return false;
            }
            set
            {
                if (m_DisablePlayModeTint == value)
                    return;

                m_DisablePlayModeTint = value;
                MarkDirtyRepaint();
                NotifyPropertyChanged(disablePlayModeTintProperty);
            }
        }

        internal Color playModeTintColor
        {
            [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
            get
            {
                return disablePlayModeTint ? Color.white : UIElementsUtility.editorPlayModeTintColor;
            }
        }

        private RenderHints m_RenderHints;

        /// <summary>
        /// Requested render hints and change flags. Note that the renderer can ignore them: reading them does not
        /// guarantee that they are effective.
        /// </summary>
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
        internal RenderData renderData; // TODO: Search for every usage of this, should be minimal!!
        internal RenderData nestedRenderData; // Non-null when rendering into a render texture
        internal int hierarchyDepth;
        internal int insertionIndex = -1;

        // TODO: Do some validation to make sure all effects actually have a material
        internal bool useRenderTexture
        {
            get
            {
                if (!hasSize)
                    return false;

                if ((renderHints & RenderHints.DynamicPostProcessing) != 0)
                    return true;

                var computedFilter = computedStyle.filter;

                for (int i = 0; i < computedFilter.Length; i++)
                {
                    var def = ((FilterFunction)computedFilter[i]).GetDefinition();
                    if (def == null)
                        continue;

                    var passes = def.passes;
                    for (int j = 0; j < passes.Length; ++j)
                    {
                        if (passes[j].material != null)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
        internal bool hasBackdropFilter
        {
            get
            {
                var computedBackdropFilter = computedStyle.backdropFilter;
                return computedBackdropFilter.Length > 0;
            }
        }

        /// <summary>
        /// Returns a transform styles object for this VisualElement.
        /// </summary>
        /// <remarks>
        /// The transform styles object contains the position, rotation, scale style properties of this VisualElement.
        /// __Note__: This transform object is different and separate from the GameObject Transform MonoBehaviour.
        /// The three interface members write to the visual element's inline style and read from the resolved style.
        /// However, the [[VisualElement.style]] API offers more features and is the recommended approach.
        /// For example, you can set translate and position as percentages through the [[VisualElement.style]] API.
        /// </remarks>
        /// <example nocheck="true">
        /// The following example reads the current position, rotation, and scale from the resolvedStyle of a
        /// VisualElement, then updates the style properties with these values.
        /// <code lang="cs">
        /// <![CDATA[
        ///         var visualElement = new VisualElement();
        ///         Vector3 position = visualElement.resolvedStyle.translate;
        ///         visualElement.style.translate = new Translate(position.x, position.y, position.z);
        ///         Quaternion rotation = visualElement.resolvedStyle.rotate.ToQuaternion();
        ///         visualElement.style.rotate = new Rotate(rotation);
        ///         Vector3 scale = visualElement.resolvedStyle.scale.value;
        ///         visualElement.style.scale = new Scale((Vector2) scale);
        ///
        /// ]]>
        /// </code>
        /// </example>
        ///
        [Obsolete("When writing the value, use VisualElement.style.translate, VisualElement.style.rotate or VisualElement.style.scale instead. When reading the value, use VisualElement.resolvedStyle.translate, scale and rotate")]
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
                style.rotate = new Rotate(value);
            }
        }

        Vector3 ITransform.scale
        {
            get
            {
                Vector3 s = resolvedStyle.scale.value;
                if (elementPanel is { isFlat: true })
                    s.z = 1;
                return s;
            }
            set
            {
                // This is the older API where Z-scaling doesn't make sense, forcing the value to Vector2.
                style.scale = new Scale((Vector2)value);
            }
        }

        Matrix4x4 ITransform.matrix
        {
            get
            {
                Vector3 s = resolvedStyle.scale.value;
                if (elementPanel is { isFlat: true })
                    s.z = 1;
                return Matrix4x4.TRS(resolvedStyle.translate, resolvedStyle.rotate.ToQuaternion(), s);
            }
        }

        internal bool isLayoutManual
        {
            get => (transformFlags & VisualElementTransformFlags.LayoutManual) == VisualElementTransformFlags.LayoutManual;
            private set => transformFlags = value ? transformFlags | VisualElementTransformFlags.LayoutManual : transformFlags & ~VisualElementTransformFlags.LayoutManual;
        }

        /// <summary>
        /// Return the resulting scaling from the panel that considers the screen DPI and the customizable scaling factor, but not the transform scale of the element and its ancestors.
        /// See <see cref="Panel.scaledPixelsPerPoint"/>.
        /// This should only be called on elements that are part of a panel.
        /// </summary>
        public float scaledPixelsPerPoint
        {
            get
            {
                if (elementPanel == null)
                {
                    Debug.LogWarning("Trying to access the DPI setting of a visual element that is not on a panel.");
                    return GUIUtility.pixelsPerPoint;
                }

                return elementPanel.scaledPixelsPerPoint;
            }
        }

        [Obsolete("scaledPixelsPerPoint_noChecks is deprecated. Use scaledPixelsPerPoint instead.")]
        internal float scaledPixelsPerPoint_noChecks => elementPanel?.scaledPixelsPerPoint ?? GUIUtility.pixelsPerPoint;
        [Obsolete("unityBackgroundScaleMode is deprecated. Use background-* properties instead.")]
        StyleEnum<ScaleMode> IResolvedStyle.unityBackgroundScaleMode => resolvedStyle.unityBackgroundScaleMode;

        private ref Rect manualLayout => ref transformData.ManualLayout;

        // This will replace the Rect position
        // origin and size relative to parent
        /// <summary>
        /// The position and size of the VisualElement relative to its parent, as computed by the layout system. (RO)
        /// </summary>
        /// <remarks>
        /// Before reading from this property, add it to a panel and wait for one frame to ensure that the element layout is computed.
        /// After the layout is computed, a <see cref="GeometryChangedEvent"/> will be sent on this element.
        /// </remarks>
        [CreateProperty(ReadOnly = true)]
        public Rect layout
        {
            get
            {
                if (isLayoutManual)
                    return manualLayout;

                if (!layoutNode.IsUndefined)
                {
                    return layoutNode.GetLayoutRect();
                }

                return new(float.NaN, float.NaN, float.NaN, float.NaN);
            }

            internal set
            {
                // Same position value while type is already manual should not trigger any layout change, return early
                if (isLayoutManual && manualLayout == value)
                    return;

                Rect lastLayout = layout;
                VersionChangeType changeType = 0;
                if (!Mathf.Approximately(lastLayout.x, value.x) || !Mathf.Approximately(lastLayout.y, value.y))
                    changeType |= VersionChangeType.Transform;
                if (!Mathf.Approximately(lastLayout.width, value.width) || !Mathf.Approximately(lastLayout.height, value.height))
                    changeType |= VersionChangeType.Size;

                // set results so we can read straight back in get without waiting for a pass
                manualLayout = value;
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
                styleAccess.right =  StyleKeyword.Auto;
                styleAccess.bottom = StyleKeyword.Auto;
                styleAccess.width = value.width;
                styleAccess.height = value.height;

                if (changeType != 0)
                    IncrementVersion(changeType);
            }
        }

        internal bool hasSize
        {
            get
            {
                if (m_LayoutNode.IsUndefined)
                    return false;

                var lyt = m_LayoutNode.Layout;

                unsafe
                {
                    var w = lyt.Dimensions[0];

                    if (float.IsNaN(w) || w <= 0)
                        return false;

                    var h = lyt.Dimensions[1];

                    if (float.IsNaN(h) || h <= 0)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// The rectangle of the content area of the element, in the local space of the element. (RO)
        /// </summary>
        /// <remarks>
        /// In the box model used by UI Toolkit, the content area refers to the inner rectangle for displaying text and images.
        /// It excludes the borders and the padding.
        /// </remarks>
        [CreateProperty(ReadOnly = true)]
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

        internal bool needs3DBounds
        {
            get => (transformFlags & VisualElementTransformFlags.Needs3DBounds) != 0;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.Needs3DBounds : transformFlags & ~VisualElementTransformFlags.Needs3DBounds;
        }

        internal bool isLocalBounds3DDirty
        {
            get => (transformFlags & VisualElementTransformFlags.LocalBounds3DDirty) != 0;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.LocalBounds3DDirty : transformFlags & ~VisualElementTransformFlags.LocalBounds3DDirty;
        }

        internal bool isLocalBoundsWithoutNested3DDirty
        {
            get => (transformFlags & VisualElementTransformFlags.LocalBoundsWithoutNested3DDirty) != 0;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.LocalBoundsWithoutNested3DDirty : transformFlags & ~VisualElementTransformFlags.LocalBoundsWithoutNested3DDirty;
        }

        internal bool isBoundingBoxDirty
        {
            get => (transformFlags & VisualElementTransformFlags.BoundingBoxDirty) == VisualElementTransformFlags.BoundingBoxDirty;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.BoundingBoxDirty : transformFlags & ~VisualElementTransformFlags.BoundingBoxDirty;
        }

        internal bool isBoundingBoxWithoutNestedDirty
        {
            get => (transformFlags & VisualElementTransformFlags.BoundingBoxWithoutNestedDirty) == VisualElementTransformFlags.BoundingBoxWithoutNestedDirty;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.BoundingBoxWithoutNestedDirty : transformFlags & ~VisualElementTransformFlags.BoundingBoxWithoutNestedDirty;
        }

        internal bool isWorldBoundingBoxDirty
        {
            get => (transformFlags & VisualElementTransformFlags.WorldBoundingBoxDirty) == VisualElementTransformFlags.WorldBoundingBoxDirty;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.WorldBoundingBoxDirty : transformFlags & ~VisualElementTransformFlags.WorldBoundingBoxDirty;
        }

        private const VisualElementTransformFlags worldBoundingBoxDirtyDependencies =
            VisualElementTransformFlags.WorldBoundingBoxDirty | VisualElementTransformFlags.BoundingBoxDirty |
            VisualElementTransformFlags.WorldTransformDirty;

        internal bool isWorldBoundingBoxOrDependenciesDirty => (transformFlags & worldBoundingBoxDirtyDependencies) != 0;

        // Bounding box in the local space of the element. It starts with the layout of the element and is inflated to
        // contain the descendants.
        internal Rect boundingBox
        {
            get
            {
                if (isBoundingBoxDirty)
                {
                    UpdateBoundingBox();
                    isBoundingBoxDirty = false;
                }

                return transformData.BoundingBox;
            }
        }

        internal Rect boundingBoxWithoutNested
        {
            [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
            get
            {
                if (isBoundingBoxWithoutNestedDirty)
                {
                    UpdateBoundingBoxWithoutNested();
                    isBoundingBoxWithoutNestedDirty = false;
                }

                return WorldSpaceDataStore.GetWorldSpaceData(this).boundingBoxWithoutNested;
            }
        }

        internal Rect worldBoundingBox
        {
            get
            {
                if (isWorldBoundingBoxOrDependenciesDirty)
                {
                    UpdateWorldBoundingBox();
                    isWorldBoundingBoxDirty = false;
                }

                return transformData.WorldBoundingBox;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateBoundingBox()
        {
            // The native implementation is about 8 times faster. See MathTests.cs.
            NativeTransformUtils.UpdateBoundingBox(layoutNode.Handle);
        }

        internal void UpdateBoundingBoxWithoutNested()
        {
            Rect bboxWithoutNested;

            var r = rect;
            if (float.IsNaN(r.x) || float.IsNaN(r.y) || float.IsNaN(r.width) || float.IsNaN(r.height))
            {
                // Ignored unlayouted VisualElements.
                bboxWithoutNested = Rect.zero;
            }
            else if (elementPanel == null || elementPanel.isFlat || this is not IPanelComponentRootElement)
            {
                // boundingBoxWithoutNested is only used in world-space mode and for panel components.
                // For any other element it's the same as a regular bounding box.
                bboxWithoutNested = boundingBox;
            }
            else
            {
                bboxWithoutNested = r;
                if (!ShouldClip() && resolvedStyle.display == DisplayStyle.Flex)
                {
                    var childCount = m_Children.Count;
                    for (int i = 0; i < childCount; i++)
                    {
                        var child = m_Children[i];
                        if (!child.areAncestorsAndSelfDisplayed)
                            continue;

                        // Only update "bounding-box without nested" for non-UIDocumentRootElement
                        if (child is IPanelComponentRootElement)
                            continue;

                        // Here we assume that nested panel components are always direct children of other components,
                        // so we can use the regular boundingBox recursively because if we reach this point it means
                        // we've broken the chain of component descendants in this branch of the hierarchy.
                        var childBB = child.boundingBoxInParentSpace;
                        bboxWithoutNested.xMin = Math.Min(bboxWithoutNested.xMin, childBB.xMin);
                        bboxWithoutNested.xMax = Math.Max(bboxWithoutNested.xMax, childBB.xMax);
                        bboxWithoutNested.yMin = Math.Min(bboxWithoutNested.yMin, childBB.yMin);
                        bboxWithoutNested.yMax = Math.Max(bboxWithoutNested.yMax, childBB.yMax);
                    }
                }
            }

            // This value is only used in world-space mode. So, we store the "without nested"
            // result in the WorldSpaceData struct to avoid uselessly inflating the VisualElement class.
            var data = WorldSpaceDataStore.GetWorldSpaceData(this);
            data.boundingBoxWithoutNested = bboxWithoutNested;
            WorldSpaceDataStore.SetWorldSpaceData(this, data);
        }

        internal void UpdateWorldBoundingBox()
        {
            transformData.WorldBoundingBox = boundingBox;
            TransformAlignedRect(ref worldTransformRef, ref transformData.WorldBoundingBox);
        }

        internal Bounds localBounds3D
        {
            get
            {
                if (!needs3DBounds)
                {
                    var bbox = boundingBox;
                    return new Bounds(bbox.center, bbox.size);
                }

                if (isLocalBounds3DDirty)
                {
                    UpdateBounds3D();
                    isLocalBounds3DDirty = false;
                }

                return WorldSpaceDataStore.GetWorldSpaceData(this).localBounds3D;
            }
        }

        internal Bounds localBoundsPicking3D
        {
            get
            {
                if (!needs3DBounds)
                {
                    var bbox = boundingBox;
                    return new Bounds(bbox.center, bbox.size);
                }

                if (isLocalBounds3DDirty)
                {
                    UpdateBounds3D();
                    isLocalBounds3DDirty = false;
                }

                return WorldSpaceDataStore.GetWorldSpaceData(this).localBoundsPicking3D;
            }
        }

        internal Bounds localBounds3DWithoutNested3D
        {
            get
            {
                if (!needs3DBounds)
                {
                    var bboxWithoutNested = boundingBoxWithoutNested;
                    return new Bounds(bboxWithoutNested.center, bboxWithoutNested.size);
                }

                if (isLocalBoundsWithoutNested3DDirty)
                {
                    UpdateBounds3D();
                    isLocalBoundsWithoutNested3DDirty = false;
                }

                return WorldSpaceDataStore.GetWorldSpaceData(this).localBoundsWithoutNested3D;
            }
        }

        void UpdateBounds3D()
        {
            if (!areAncestorsAndSelfDisplayed)
            {
                WorldSpaceDataStore.ClearLocalBounds3DData(this);
                return;
            }

            var localBoundsWithoutNested = new Bounds(rect.center, rect.size);
            var localBoundsWithNested = localBoundsWithoutNested;
            var pickingBounds = pickingMode == PickingMode.Position ? localBoundsWithNested : WorldSpaceData.k_Empty3DBounds;

            if (!ShouldClip())
            {
                var childCount = hierarchy.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var child = hierarchy[i];

                    bool childIsPanelComponentRoot = child is IPanelComponentRootElement;
                    if (!childIsPanelComponentRoot) // Skip local bounds update when child is a UIDocument root
                    {
                        var childBoundsWithoutNested = child.localBounds3DWithoutNested3D;
                        if (childBoundsWithoutNested.extents.x >= 0)
                        {
                            child.TransformAlignedBoundsToParentSpace(ref childBoundsWithoutNested);
                            localBoundsWithoutNested.Encapsulate(childBoundsWithoutNested);
                        }
                    }

                    // Always update local bounds with nested UIDocs
                    var childLocalBoundsWithNested = child.localBounds3D;
                    if (childLocalBoundsWithNested.extents.x >= 0)
                    {
                        child.TransformAlignedBoundsToParentSpace(ref childLocalBoundsWithNested);
                        localBoundsWithNested.Encapsulate(childLocalBoundsWithNested);
                    }

                    // Update picking bounds
                    var childPickingBounds = child.localBoundsPicking3D;
                    if (childPickingBounds.extents.x >= 0)
                    {
                        child.TransformAlignedBoundsToParentSpace(ref childPickingBounds);
                        pickingBounds.Encapsulate(childPickingBounds);
                    }
                }
            }

            var data = WorldSpaceDataStore.GetWorldSpaceData(this);
            data.localBounds3D = localBoundsWithNested;
            data.localBoundsPicking3D = pickingBounds;
            data.localBoundsWithoutNested3D = localBoundsWithoutNested;
            WorldSpaceDataStore.SetWorldSpaceData(this, data);
        }

        /// <summary>
        /// Returns the axis-aligned bounding box of the element in panel coordinates after the cumulative transform from the <see cref="IPanel"/> root.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
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
        /// Returns a <see cref="Rect"/> representing the Axis-aligned Bounding Box (AABB) after applying the transform, but before applying the layout translation.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get
            {
                var l = layoutSize;
                return new Rect(0.0f, 0.0f, l.x, l.y);
            }
        }

        internal Vector2 layoutSize
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get
            {
                if (isLayoutManual)
                    return manualLayout.size;

                return layoutNode.GetLayoutSize();
            }
        }


        internal Vector2 layoutPosition
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get
            {
                if (isLayoutManual)
                    return manualLayout.min;

                return layoutNode.GetLayoutPosition();
            }
        }


        internal bool isWorldSpaceRootPanelComponent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (m_Flags & VisualElementFlags.IsWorldSpaceRootPanelComponent) == VisualElementFlags.IsWorldSpaceRootPanelComponent;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_Flags = value ? m_Flags | VisualElementFlags.IsWorldSpaceRootPanelComponent : m_Flags & ~VisualElementFlags.IsWorldSpaceRootPanelComponent;
        }

        internal bool isWorldTransformDirty
        {
            get => (transformFlags & VisualElementTransformFlags.WorldTransformDirty) == VisualElementTransformFlags.WorldTransformDirty;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.WorldTransformDirty : transformFlags & ~VisualElementTransformFlags.WorldTransformDirty;
        }

        internal bool isWorldTransformInverseDirty
        {
            get => (transformFlags & VisualElementTransformFlags.WorldTransformInverseDirty) == VisualElementTransformFlags.WorldTransformInverseDirty;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.WorldTransformInverseDirty : transformFlags & ~VisualElementTransformFlags.WorldTransformInverseDirty;
        }

        private const VisualElementTransformFlags worldTransformInverseDirtyDependencies =
            VisualElementTransformFlags.WorldTransformInverseDirty | VisualElementTransformFlags.WorldTransformDirty;

        internal bool isWorldTransformInverseOrDependenciesDirty =>
            (transformFlags & worldTransformInverseDirtyDependencies) != 0;

        /// <summary>
        /// Returns a matrix that cumulates the following transformations (in order):
        ///
        ///- Local scaling
        ///- Local rotation
        ///- Local translation
        ///- Layout translation
        ///- Parent <c>worldTransform</c> (recursive definition - consider the identity matrix when there's no parent)
        /// </summary>
        /// <remarks>
        /// Multiplying the <c>layout</c> rect by this matrix is incorrect because it already contains the translation.
        /// </remarks>
        [CreateProperty(ReadOnly = true)]
        public Matrix4x4 worldTransform
        {
            get
            {
                if (isWorldTransformDirty)
                    UpdateWorldTransform();
                return transformData.WorldTransform;
            }
        }

        internal ref Matrix4x4 worldTransformRef
        {
            get
            {
                if (isWorldTransformDirty)
                    UpdateWorldTransform();
                return ref transformData.WorldTransform;
            }
        }

        internal ref Matrix4x4 worldTransformInverse
        {
            [VisibleToOtherModules("UnityEditor.GraphToolkitModule", "UnityEditor.UIToolkitAuthoringModule")]
            get
            {
                if (isWorldTransformInverseOrDependenciesDirty)
                    UpdateWorldTransformInverse();
                return ref transformData.WorldTransformInverse;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateWorldTransform()
        {
            // The native implementation is generally 2 to 5 times faster. See MathTests.cs.
            UpdateWorldTransformNative();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateWorldTransformNative()
        {
            NativeTransformUtils.UpdateWorldTransform(layoutNode.Handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateWorldTransformHierarchyNative()
        {
            // It makes no sense to call this method during layout phase, as it will not remove any of the dirty flags
            Debug.Assert(!elementPanel.duringLayoutPhase, "!elementPanel.duringLayoutPhase");
            NativeTransformUtils.UpdateWorldTransformHierarchy(layoutNode.Handle);
        }

        // Kept for performance tests.
        internal void UpdateWorldTransformManaged()
        {
            // If we are during a layout we don't want to remove the dirty transform flag
            // since this could lead to invalid computed transform (see ScopeContentContainer.DoMeasure)
            if (elementPanel != null && !elementPanel.duringLayoutPhase)
            {
                isWorldTransformDirty = false;
            }

            if (hierarchy.parent != null)
            {
                if (hasDefaultRotationAndScale)
                {
                    TranslateMatrix34(ref hierarchy.parent.worldTransformRef, positionWithLayout, out transformData.WorldTransform);
                }
                else
                {
                    GetPivotedMatrixWithLayout(out var mat);
                    MultiplyMatrix34(ref hierarchy.parent.worldTransformRef, ref mat, out transformData.WorldTransform);
                }
            }
            else
            {
                GetPivotedMatrixWithLayout(out transformData.WorldTransform);
            }

            isWorldTransformInverseDirty = true;
            isWorldBoundingBoxDirty = true;
        }

        internal void UpdateWorldTransformInverse()
        {
            Matrix4x4.Inverse3DAffine(in worldTransformRef, ref transformData.WorldTransformInverse);
            isWorldTransformInverseDirty = false;
        }

        // Only used in tests
        internal bool isWorldClipDirty
        {
            get => (flags & VisualElementFlags.WorldClipDirty) == VisualElementFlags.WorldClipDirty;
        }

        internal Rect worldClip
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get
            {
                return renderData?.clippingRect ?? Rect.zero;
            }
        }

        internal Rect nestedTreeWorldClip
        {
            get
            {
                return nestedRenderData?.clippingRect ?? Rect.zero;
            }
        }

        internal void EnsureWorldTransformAndClipUpToDate()
        {
            if (renderData == null)
                return;

            if (isWorldTransformDirty)
                UpdateWorldTransform();

            renderData.UpdateClippingRect();
            renderData.flags &= ~RenderDataFlags.IsClippingRectDirty;
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

        internal bool receivesHierarchyGeometryChangedEvents
        {
            get => (m_Flags & VisualElementFlags.ReceivesHierarchyGeometryChangedEvents) == VisualElementFlags.ReceivesHierarchyGeometryChangedEvents;
            set => m_Flags = value ? m_Flags | VisualElementFlags.ReceivesHierarchyGeometryChangedEvents : m_Flags & ~VisualElementFlags.ReceivesHierarchyGeometryChangedEvents;
        }

        internal bool boundingBoxDirtiedSinceLastLayoutPass
        {
            get => (transformFlags & VisualElementTransformFlags.BoundingBoxDirtiedSinceLastLayoutPass) == VisualElementTransformFlags.BoundingBoxDirtiedSinceLastLayoutPass;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.BoundingBoxDirtiedSinceLastLayoutPass : transformFlags & ~VisualElementTransformFlags.BoundingBoxDirtiedSinceLastLayoutPass;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal PseudoStates pseudoStates
        {
            get
            {
                if ((m_Flags & VisualElementFlags.Released) != 0)
                    throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

                return layoutNode.SelectorData.pseudoStates;
            }
            set
            {
                if ((m_Flags & VisualElementFlags.Released) != 0)
                    throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

                ref var selectorData = ref layoutNode.SelectorData;
                PseudoStates diff = selectorData.pseudoStates ^ value;
                selectorData.pseudoStates = value;

                if ((int)diff > 0)
                {
                    // If only the root changed do not trigger a new style update since the root
                    // pseudo state change base on the current style sheet when selectors are matched.
                    if (diff != PseudoStates.Root)
                    {
                        var added = diff & value;
                        var removed = diff ^ added;

                        if ((selectorData.triggerPseudoMask & added) != 0
                            || (selectorData.dependencyPseudoMask & removed) != 0)
                        {
                            IncrementVersion(VersionChangeType.StyleSheet);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if this element matches the @@:active@@ pseudo-class.
        /// </summary>
        /// <remarks>
        /// For an example of how to use this property, refer to [[wiki:ui-systems/check-pseudo-state|Check pseudo-state of a control]].
        /// </remarks>
        public bool hasActivePseudoState => (pseudoStates & PseudoStates.Active) != 0;
        /// <summary>
        /// Returns true if this element matches the @@:inactive@@ pseudo-class.
        /// </summary>
        public bool hasInactivePseudoState => (pseudoStates & PseudoStates.Active) == 0;
        /// <summary>
        /// Returns true if this element matches the @@:hover@@ pseudo-class.
        /// </summary>
        /// <remarks>
        /// For an example of how to use this property, refer to [[wiki:ui-systems/check-pseudo-state|Check pseudo-state of a control]].
        /// </remarks>
        public bool hasHoverPseudoState => (pseudoStates & PseudoStates.Hover) != 0;
        /// <summary>
        /// Returns true if this element matches the @@:checked@@ pseudo-class.
        /// </summary>
        /// <remarks>
        /// For an example of how to use this property, refer to [[wiki:ui-systems/check-pseudo-state|Check pseudo-state of a control]].
        /// </remarks>
        public bool hasCheckedPseudoState => (pseudoStates & PseudoStates.Checked) != 0;
        /// <summary>
        /// Returns true if this element matches the @@:enabled@@ pseudo-class.
        /// </summary>
        public bool hasEnabledPseudoState => (pseudoStates & PseudoStates.Disabled) == 0;
        /// <summary>
        /// Returns true if this element matches the @@:disabled@@ pseudo-class.
        /// </summary>
        /// <remarks>
        /// For an example of how to use this property, refer to [[wiki:ui-systems/check-pseudo-state|Check pseudo-state of a control]].
        /// </remarks>
        public bool hasDisabledPseudoState => (pseudoStates & PseudoStates.Disabled) != 0;
        /// <summary>
        /// Returns true if this element matches the @@:focus@@ pseudo-class.
        /// </summary>
        /// <remarks>
        /// For an example of how to use this property, refer to [[wiki:ui-systems/check-pseudo-state|Check pseudo-state of a control]].
        /// </remarks>
        public bool hasFocusPseudoState => (pseudoStates & PseudoStates.Focus) != 0;
        /// <summary>
        /// Returns true if this element matches the @@:root@@ pseudo-class.
        /// </summary>
        public bool hasRootPseudoState => (pseudoStates & PseudoStates.Root) != 0;

        /// <summary>
        /// Sets whether or not this element is displayed as being active.
        /// </summary>
        /// <remarks>
        /// If set to true, this element will match the @@:active@@ pseudo-class.
        /// </remarks>
        /// <remarks>
        /// Some elements, like the <see cref="Button"/>, use this method internally
        /// to reflect changes to their internal state. Calling this method on those elements
        /// may have no effect or only result in a temporary change in their displayed styles.
        /// </remarks>
        public void SetActivePseudoState(bool value)
        {
            pseudoStates = value ? pseudoStates | PseudoStates.Active : pseudoStates & ~PseudoStates.Active;
        }

        /// <summary>
        /// Sets whether or not this element is displayed as being checked.
        /// </summary>
        /// <remarks>
        /// If set to true, this element will match the @@:checked@@ pseudo-class.
        /// </remarks>
        /// <remarks>
        /// Some elements, like the <see cref="Toggle"/>, use this method internally
        /// to reflect changes to their internal state. Calling this method on those elements
        /// may have no effect or only result in a temporary change in their displayed styles.
        /// </remarks>
        public void SetCheckedPseudoState(bool value)
        {
            pseudoStates = value ? pseudoStates | PseudoStates.Checked : pseudoStates & ~PseudoStates.Checked;
        }

        internal int containedPointerIds { get; set; }

        internal void UpdateHoverPseudoState()
        {
            // An element has the hover pseudoState if and only if it has at least one contained pointer which is
            // captured by itself, one if its descendents or no element.

            // With multi-finger touch events, there can be multiple unrelated elements hovered at once, or a single
            // element hovered by multiple fingers. In that case, the hover pseudoState for a given element will match
            // a logical OR of the hover state of each finger on that element.

            if (containedPointerIds == 0 || panel == null)
            {
                pseudoStates &= ~PseudoStates.Hover;
                return;
            }

            bool hovered = false;
            for (var pointerId = 0; pointerId < PointerId.maxPointers; pointerId++)
            {
                if ((containedPointerIds & (1 << pointerId)) != 0)
                {
                    var capturingElement = panel.GetCapturingElement(pointerId);
                    if (IsPartOfCapturedChain(this, capturingElement))
                    {
                        hovered = true;
                        break;
                    }
                }
            }

            if (hovered)
                pseudoStates |= PseudoStates.Hover;
            else
                pseudoStates &= ~PseudoStates.Hover;
        }

        static bool IsPartOfCapturedChain(VisualElement self, in IEventHandler capturingElement)
        {
            if (self == null)
                return false;
            if (capturingElement == null)
                return true;
            if (capturingElement == self)
                return true;

            // Check if captured element is descendant of self.
            return self.Contains(capturingElement as VisualElement);
        }

        internal void UpdateHoverPseudoStateAfterCaptureChange(int pointerId)
        {
            // Pointer capture changes can influence if an element is hovered or not.
            // Update the entire ancestor chain.
            for (var ve = this; ve != null; ve = ve.parent)
                ve.UpdateHoverPseudoState();

            // Make sure to also reevaluate the hover state of the elements under pointer.
            var elementUnderPointer = elementPanel?.GetTopElementUnderPointer(pointerId);
            for (var ve = elementUnderPointer; ve != null && ve != this; ve = ve.parent)
            {
                ve.UpdateHoverPseudoState();
            }
        }

        internal void UpdatePointerCaptureFlag()
        {
            bool hasCapture = false;
            for (int i = 0; i < PointerId.maxPointers; i++)
                if (this.HasPointerCapture(i))
                {
                    hasCapture = true;
                    break;
                }
            hasOneOrMorePointerCaptures = hasCapture;
        }

        /// <summary>
        /// The name of this VisualElement.
        /// </summary>
        /// <remarks>
        /// Use this property to write USS selectors that target a specific element.
        /// The standard practice is to give an element a unique name.
        /// </remarks>
        [CreateProperty]
        [UxmlAttribute, HideInInspector]
        [UxmlInternalField, VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        public string name
        {
            get { return m_Name; }
            set
            {
                if (m_Name == value)
                    return;
                m_Name = value;
                SetNameId(string.IsNullOrEmpty(value) ? UniqueStyleString.Empty.id : new UniqueStyleString(value).id);
            }
        }

        // Sets the element name from a pre-interned UniqueStyleString, skipping the per-call
        // dictionary lookup that the string overload incurs. Use this overload from controls
        // that always assign the same name string by hoisting that string into a `static
        // readonly UniqueStyleString` constant.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule", "UnityEngine.HierarchyModule")]
        internal void SetName(UniqueStyleString uniqueName)
        {
            if (nameId == uniqueName.id)
                return;
            m_Name = uniqueName.value;
            SetNameId(uniqueName.id);
        }

        private void SetNameId(int nameId)
        {
            if ((m_Flags & VisualElementFlags.Released) != 0)
                throw new InvalidOperationException(k_ElementReleaseExceptionMessage);
            layoutNode.SelectorData.nameId = nameId;
            IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Name);
            NotifyPropertyChanged(nameProperty);
        }

        /// <summary>
        /// Returns true if the <see cref="VisualElement"/> is enabled locally.
        /// </summary>
        /// <remarks>
        /// This flag isn't changed if the VisualElement is disabled implicitly by one of its parents. To verify this, use <see cref="enabledInHierarchy"/>.
        /// </remarks>
        [CreateProperty]
        [UxmlAttribute("enabled")]
        [Tooltip("Sets the element to disabled which will not accept input. Utilizes the :disabled pseudo state.")]
        public bool enabledSelf
        {
            get => (m_Flags & VisualElementFlags.DisabledSelf) == 0;
            set
            {
                if (enabledSelf == value)
                    return;

                m_Flags = value ? m_Flags & ~VisualElementFlags.DisabledSelf : m_Flags | VisualElementFlags.DisabledSelf;

                // If parent is disabled, we assume that the element and its hierarchy are already properly disabled
                if (hierarchy.parent == null || hierarchy.parent.enabledInHierarchy)
                {
                    PropagateSelfEnabled(value);

                    if (!value)
                        BlurHierarchyImmediately();
                }

                NotifyPropertyChanged(enabledSelfProperty);
            }
        }

        // Used for view data persistence (ie. scroll position or tree view expanded states)
        private string m_ViewDataKey;

        /// <summary>
        /// Used for view data persistence, such as tree expanded states, scroll position, or zoom level.
        /// </summary>
        /// <remarks>
        /// This key is used to save and load the view data from the view data store. If you don't set this key, the persistence is disabled for the associated <see cref="VisualElement"/>.
        /// For more information, refer to [[wiki:UIE-ViewData|View data persistence in the Unity Manual]].
        /// </remarks>
        [CreateProperty]
        [UxmlAttribute]
        public string viewDataKey
        {
            get => m_ViewDataKey;
            set
            {
                if (m_ViewDataKey != value)
                {
                    m_ViewDataKey = value;

                    if (!string.IsNullOrEmpty(value))
                        IncrementVersion(VersionChangeType.ViewData);

                    NotifyPropertyChanged(viewDataKeyProperty);
                }
            }
        }

        /// <summary>
        /// Determines if this element can be the target of pointer events or
        /// picked by <see cref="IPanel.Pick"/> queries.
        /// </summary>
        /// <remarks>
        /// Elements can not be picked if:
        ///
        ///- They are invisible
        ///- Their @@style.display@@ is set to @@DisplayStyle.None@@
        ///
        /// Elements with a picking mode of <see cref="PickingMode.Ignore"/> never receive the hover pseudo-state.
        /// </remarks>
        /// <seealso cref="VisualElement.visible"/>
        /// <seealso cref="IStyle.display"/>
        [CreateProperty]
        [UxmlAttribute("picking-mode", "pickingMode")]
        public PickingMode pickingMode
        {
            get => (transformFlags & VisualElementTransformFlags.PickingIgnore) != 0 ? PickingMode.Ignore : PickingMode.Position;
            set
            {
                if (pickingMode == value)
                    return;
                transformFlags = value == PickingMode.Ignore
                    ? transformFlags | VisualElementTransformFlags.PickingIgnore
                    : transformFlags & ~VisualElementTransformFlags.PickingIgnore;
                IncrementVersion(VersionChangeType.Picking);
                NotifyPropertyChanged(pickingModeProperty);
            }
        }

        //PropertyName to store in property bag.
        internal static readonly PropertyName tooltipPropertyKey = new PropertyName("--unity-tooltip");

        /// <summary>
        /// Text to display inside an information box after the user hovers the element for a small amount of time. This is only supported in the Editor UI.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public string tooltip
        {
            get
            {
                string tooltipText = GetProperty(tooltipPropertyKey) as string;

                return tooltipText ?? String.Empty;
            }
            set
            {
                if (!HasProperty(tooltipPropertyKey))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        return;
                    }

                    RegisterCallback<TooltipEvent>(SetTooltip);
                }

                var tooltipText = GetProperty(tooltipPropertyKey) as string;
                if (string.CompareOrdinal(tooltipText, value) == 0)
                    return;
                SetProperty(tooltipPropertyKey, value);
                NotifyPropertyChanged(tooltipProperty);
            }
        }

        /// <summary>
        /// A combination of hint values that specify high-level intended usage patterns for the <see cref="VisualElement"/>.
        /// This property can only be set when the <see cref="VisualElement"/> is not yet part of a <see cref="Panel"/>. Once part of a <see cref="Panel"/>, this property becomes effectively read-only, and attempts to change it will throw an exception.
        /// The specification of proper <see cref="UsageHints"/> drives the system to make better decisions on how to process or accelerate certain operations based on the anticipated usage pattern.
        /// Note that those hints do not affect behavioral or visual results, but only affect the overall performance of the panel and the elements within.
        /// It's advised to always consider specifying the proper <see cref="UsageHints"/>, but keep in mind that some <see cref="UsageHints"/> might be internally ignored under certain conditions (e.g. due to hardware limitations on the target platform).
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public UsageHints usageHints
        {
            get
            {
                return
                    ((renderHints & RenderHints.GroupTransform) != 0 ? UsageHints.GroupTransform : 0) |
                    ((renderHints & RenderHints.BoneTransform) != 0 ? UsageHints.DynamicTransform : 0) |
                    ((renderHints & RenderHints.MaskContainer) != 0 ? UsageHints.MaskContainer : 0) |
                    ((renderHints & RenderHints.DynamicColor) != 0 ? UsageHints.DynamicColor : 0) |
                    ((renderHints & RenderHints.DynamicPostProcessing) != 0 ? UsageHints.DynamicPostProcessing : 0) |
                    ((renderHints & RenderHints.LargePixelCoverage) != 0 ? UsageHints.LargePixelCoverage : 0);
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

                if ((value & UsageHints.DynamicPostProcessing) != 0)
                    renderHints |= RenderHints.DynamicPostProcessing;
                else renderHints &= ~RenderHints.DynamicPostProcessing;

                if ((value & UsageHints.LargePixelCoverage) != 0)
                    renderHints |= RenderHints.LargePixelCoverage;
                else renderHints &= ~RenderHints.LargePixelCoverage;

                NotifyPropertyChanged(usageHintsProperty);
            }
        }

        // Added so we can generate the attribute for a parent field and also control the order it appears.
        [UxmlAttribute("tabindex"), UxmlAttributeBindingPath(nameof(tabIndex))]
        internal int tabIndexUXML
        {
            get => tabIndex;
            set => tabIndex = value;
        }

        // Added so we can generate the attribute for a parent field and also control the order it appears.
        [UxmlAttribute("focusable"), UxmlAttributeBindingPath(nameof(focusable))]
        internal bool focusableUXML
        {
            get => focusable;
            set => focusable = value;
        }

        LanguageDirection m_LanguageDirection;
        /// <summary>
        /// Indicates the directionality of the element's text. The value will propagate to the element's children.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public LanguageDirection languageDirection
        {
            get => m_LanguageDirection;
            set
            {
                if (m_LanguageDirection == value)
                    return;

                m_LanguageDirection = value;
                localLanguageDirection = m_LanguageDirection;
                NotifyPropertyChanged(languageDirectionProperty);
            }
        }

        [UxmlAttribute("data-source"), UxmlAttributeBindingPath("dataSource"), HideInInspector, DataSourceDrawer]
        internal Object dataSourceUnityObject
        {
            get => dataSource as Object;
            set => dataSource = value ? value : null;
        }

        [Tooltip(DataBinding.k_DataSourcePathTooltip), HideInInspector]
        [UxmlAttribute("data-source-path")]
        internal string dataSourcePathString
        {
            get => dataSourcePath.ToString();
            set => dataSourcePath = new PropertyPath(value);
        }

        /// <summary>
        /// The possible type of data source assignable to this VisualElement.
        /// <remarks>
        /// This information is only used by the UI Builder as a hint to provide some completion to the data source path field when the effective data source cannot be specified at design time.
        /// </remarks>
        /// </summary>
        [UxmlAttribute, HideInInspector, UxmlTypeReference(typeof(object))]
        [Tooltip(DataBinding.k_DataSourceTooltip)]
        public Type dataSourceType { get; set; }

        [UxmlObjectReference("Bindings"), UxmlInternalField, HideInInspector]
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal List<Binding> bindings
        {
            get => m_Bindings ??= new List<Binding>();
            set
            {
                if (value != null)
                {
                    foreach(var binding in value)
                    {
                        SetBinding(binding.property, binding);
                    }
                }

                m_Bindings = value;
            }
        }

        internal string fullTypeName
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => typeData.fullTypeName;
        }

        internal string typeName
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => typeData.typeName;
        }

        // Cached UniqueStyleString.id for typeName (shared across all instances of this type)
        internal int typeNameId => typeData.typeNameId;

        // Cached UniqueStyleString.id for the element name (instance-specific). Equals
        // UniqueStyleString.Empty.id when name is null/empty; otherwise a valid interned id.
        internal int nameId => layoutNode.SelectorData.nameId;

        LayoutNode m_LayoutNode;

        // Set and pass in values to be used for layout
        internal ref LayoutNode layoutNode
        {
            get
            {
                return ref m_LayoutNode;
            }
        }

        private readonly unsafe VisualElementTransformData* m_TransformDataPTr;
        private unsafe ref VisualElementTransformData transformData => ref *m_TransformDataPTr;

        internal ref ComputedStyle computedStyle
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => ref layoutNode.ComputedStyle;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        // Variables that children inherit
        internal StyleVariableContext variableContext = StyleVariableContext.none;

        // Hash of the inherited style data values
        internal int inheritedStylesHash = 0;

        internal bool hasInlineStyle
        {
            [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
            get => inlineStyleAccess != null;
        }

        internal bool styleInitialized
        {
            get => (m_Flags & VisualElementFlags.StyleInitialized) == VisualElementFlags.StyleInitialized;
            set => m_Flags = value ? m_Flags | VisualElementFlags.StyleInitialized : m_Flags & ~VisualElementFlags.StyleInitialized;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool stylesDirty
        {
            get => (m_Flags & VisualElementFlags.StyleDirty) == VisualElementFlags.StyleDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.StyleDirty : m_Flags & ~VisualElementFlags.StyleDirty;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool stylesAncestorOfDirty
        {
            get => (m_Flags & VisualElementFlags.StyleAncestorOfDirty) == VisualElementFlags.StyleAncestorOfDirty;
            set => m_Flags = value ? m_Flags | VisualElementFlags.StyleAncestorOfDirty : m_Flags & ~VisualElementFlags.StyleAncestorOfDirty;
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

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
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

        // Backdrop-filter elements require special handling on transform changes since their UV mapping
        // depends on world transform. This count tracks descendants with backdrop-filter so we can
        // efficiently determine when to trigger hierarchical regeneration.
        internal int backdropFilterDescendantCount = 0;

        internal void ChangeBackdropFilterDescendantCount(int delta)
        {
            VisualElement ve = this;

            while (ve != null)
            {
                ve.backdropFilterDescendantCount += delta;
                ve = ve.hierarchy.parent;
            }
        }

        /// <summary>
        ///  Initializes and returns an instance of VisualElement.
        /// </summary>
        [DynamicDependency(nameof(UIElementsInitialization.InitializeUIElementsManaged), typeof(UIElementsInitialization))]
        public VisualElement()
        {
            m_Children = s_EmptyList;
            controlid = ++s_NextId;

            hierarchy = new Hierarchy(this);

            m_ClassList = StyleClassList.Empty;
            flags = VisualElementFlags.Init;

            focusable = false;

            m_Name = string.Empty;

            m_LayoutNode = LayoutManager.SharedManager.CreateNode();
            unsafe
            {
                // Fast-tracked access to the transform matrices and flags, as their access is quite frequent.
                // Removing this direct access makes Picking ~25% slower. See UIElementsEvents.OptimizePick tests.
                m_TransformDataPTr = layoutNode.VisualElementTransformDataPtr;
            }

            renderHints = RenderHints.None;

            m_TypeData = GetOrCreateTypeData(GetType());

            usesContainsPoint = m_TypeData.hasContainsPoint;

            var defaultEventInterests = m_TypeData.defaultEventInterests;

            m_TrickleDownHandleEventCategories = defaultEventInterests.HandleEventTrickleDownCategories;

            // Combine the obsolete default actions into the BubbleUp categories
            m_BubbleUpHandleEventCategories = defaultEventInterests.HandleEventBubbleUpCategories |
                                              defaultEventInterests.DefaultActionAtTargetCategories |
                                              defaultEventInterests.DefaultActionCategories;

            UpdateEventInterestSelfCategories();

            // nameId / pseudoStates / logicalParent all match VisualElementSelectorData.Default
            // at construction and are written through by their setters/mutators as they change.
            // typeNameId has no mutation path so seed it here. classIds defaults to null in the
            // component but m_ClassList = StyleClassList.Empty already points to a valid (empty)
            // NativeArray — seed it through the helper so the in-sync assertion holds even when
            // nothing ever mutates the list.
            layoutNode.SelectorData.typeNameId = typeNameId;
            UpdateClassSelectorData();
        }

        // For unit tests
        internal static int s_FinalizerCount = 0;

        ~VisualElement()
        {
            try
            {
                if (!resourcesReleased)
                {
                    if (LayoutManager.IsSharedManagerCreated)
                    {
                        LayoutManager.SharedManager.EnqueueNodeForRecycling(ref m_LayoutNode);
                    }
                }
                s_FinalizerCount++;
            }
            // Exceptions inside finalizers are not automatically logged by Unity
            // So let's report those in the console to make sure they don't go undetected
            catch (Exception e)
            {
                Debug.LogError("An exception occured in a VisualElement finalizer, please report a bug.");
                Debug.LogException(e);
            }
        }

        private const string k_ElementReleaseExceptionMessage = "You can't modify a VisualElement after its resources are released. This usually happens when PanelRenderer releases elements during UI reload or cleanup. Make sure that you don't hold stale references to elements.";

        /// <summary>
        /// Indicates if the element has released its reusable resources, in which case it can not be modified or added again.
        /// </summary>
        /// <remarks>
        /// This returns true if <see cref="ReleaseResources"/> has been called on this element.
        /// </remarks>
        public bool resourcesReleased => (m_Flags & VisualElementFlags.Released) != 0;

        /// <summary>
        /// Releases reusable resources associated with this element and makes the element unusable.
        /// </summary>
        /// <remarks>
        /// The element must not be part of a hierarchy nor have any children, otherwise an InvalidOperationException is thrown.
        /// Calling this method makes the element unusable. Only call it when the element is no longer needed.
        /// Exceptions are thrown if you modify the element or add it again.
        ///
        /// By default, a VisualElement releases its reusable resources only when it is garbage collected.
        /// Calling this method explicitly releases those resources earlier so the system can immediately reuse them when creating new elements, reducing memory usage.
        ///
        /// In most cases, it is more convenient to use <see cref="VisualElement.Clear(VisualElementClearOptions)"/> on a root element
        /// which will recursively remove all its descendants and call <code>ReleaseResources</code> on each of them.
        /// </remarks>
        /// <example>
        /// The following example shows how to release elements when implementing object pooling:
        /// <code source="../../../Modules/UIElements/Tests/UIElementsExamples/Assets/ui-toolkit-manual-code-examples/doc-examples/VisualElementPoolExampleWindow.cs"/>
        /// </example>
        public void ReleaseResources()
        {
            if (parent != null)
            {
                throw new InvalidOperationException("Cannot release resources while the VisualElement is still in the hierarchy");
            }
            if (hierarchy.childCount > 0)
            {
                throw new InvalidOperationException("Cannot release resources while the VisualElement has children");
            }
            if ((m_Flags & VisualElementFlags.Released) != 0)
            {
                throw new InvalidOperationException("Cannot release resources more than once");
            }
            ReleaseResourcesNoChecks();
        }


        internal void ReleaseResourcesNoChecks()
        {
            flags |= VisualElementFlags.Released;
            LayoutManager.SharedManager.EnqueueNodeForRecycling(ref m_LayoutNode);

            // Put back some of the lists we own to their pools
            // Note: we already know the child list was pooled back when clearing the element

            m_ClassList.Clear();

            if (m_CallbackRegistry != null)
            {
                m_CallbackRegistry.Dispose();
                m_CallbackRegistry = null;
            }
        }

        internal void SetTooltip(TooltipEvent e)
        {
            if (e.currentTarget is VisualElement element && !string.IsNullOrEmpty(element.tooltip))
            {
                if (e.rect != Rect.zero)
                {
                    e.rect = e.rect;
                }
                else
                {
                    // Clamp to world clip (UUM-109120)
                    var wb = element.worldBound;
                    var clip = element.worldClip;
                    e.rect = new Rect(wb.x, wb.y, Math.Clamp(wb.width, 0, clip.width), Mathf.Clamp(wb.height, 0, clip.height));
                }

                e.tooltip = element.tooltip;
                e.StopImmediatePropagation();
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

        internal long TimeSinceStartupMs()
        {
            if (elementPanel != null)
            {
                return elementPanel.TimeSinceStartupMs();
            }

            return (long)(BaseVisualElementPanel.DefaultTimeSinceStartup() * 1000);
        }

        internal void SetPanel(BaseVisualElementPanel p)
        {
            if (panel == p)
                return;
            //We now gather all Elements in order to dispatch events in an efficient manner
            List<VisualElement> elements = VisualElementListPool.Get();
            elements.Add(this);
            GatherAllChildren(elements);
            try
            {
                SetPanelBatched(p, elements);
            }
            finally
            {
                VisualElementListPool.Release(elements);
            }
        }

        internal void SetPanelBatched(BaseVisualElementPanel p, List<VisualElement> elements)
        {
            if (panel != p)
            {
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
                    elementPanel?.liveReloadSystem.StopTracking(elements);

                    panel?.dispatcher?.m_ClickDetector.Cleanup(elements);
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
                    var layoutConfig = p != null ? p.layoutConfig : LayoutManager.SharedManager.GetDefaultConfig();

                    InvokeHierarchyChanged(HierarchyChangeType.DetachedFromPanel, elements);
                    foreach (var e in elements)
                    {
                        e.elementPanel?.MemberElementsByHandle.Remove(e.layoutNode.Handle);
                        e.elementPanel = p;
                        e.elementPanel?.MemberElementsByHandle.Add(e.layoutNode.Handle, e);

                        e.flags |= flagToAdd;
                        e.m_CachedNextParentWithEventInterests = null;
                        e.layoutNode.Config = layoutConfig;
                    }

                    InvokeHierarchyChanged(HierarchyChangeType.AttachedToPanel, elements);

                    foreach (var e in elements)
                    {
                        e.HasChangedPanel(previousPanel);
                    }
                    p?.liveReloadSystem.StartTracking(elements);
                }
            }
        }

        void WillChangePanel(BaseVisualElementPanel destinationPanel)
        {
            if (elementPanel != null)
            {
                // Better to do some things here before we call the user's callback as some state may be modified during the callback.
                UnregisterRunningAnimations();
                CreateBindingRequests();
                DetachDataSource();

                if (containedPointerIds != 0)
                {
                    //We need to remove this element from the ElementsUnderPointer
                    elementPanel.RemoveElementFromPointerCache(this);
                    elementPanel.CommitElementUnderPointers();
                }

                if (hasOneOrMorePointerCaptures)
                {
                    for (var i = 0; i < PointerId.maxPointers; i++)
                    {
                        if (this.HasPointerCapture(i))
                        {
                            this.ReleasePointer(i);
                            elementPanel.ProcessPointerCapture(i);
                        }
                    }
                }

                // Only send this event if the element isn't waiting for an attach event already
                if ((m_Flags & VisualElementFlags.NeedsAttachToPanelEvent) == 0)
                {
                    if (HasSelfEventInterests(DetachFromPanelEvent.EventCategory))
                    {
                        using (var e = DetachFromPanelEvent.GetPooled(elementPanel, destinationPanel))
                        {
                            EventDispatchUtilities.SendEventDirectlyToTarget(e, elementPanel, this);
                        }
                    }
                }

                // Disable the started animations which have possibly started in the DetachFromPanelEvent callback.
                UnregisterRunningAnimations();
            }
        }

        void HasChangedPanel(BaseVisualElementPanel prevPanel)
        {
            if (elementPanel != null)
            {
                layoutNode.SoftReset();

                RegisterRunningAnimations();
                ProcessBindingRequests();
                AttachDataSource();

                // We need to reset any visual pseudo state
                pseudoStates &= ~(PseudoStates.Active | PseudoStates.Hover);
                if ((pseudoStates & PseudoStates.Focus) != 0)
                {
                    // In the case of Focus, don't reset the state if the element is re-added to its former panel
                    // and the focus wasn't changed in the meantime.
                    if (!focusController.IsFocused(this))
                        pseudoStates &= ~PseudoStates.Focus;
                }

                // UUM-42891: We must presume that we're not displayed because when it's the case (i.e. when we are not
                // displayed), the layout updater will not process the children unless there is a display *change* in the ancestors.
                transformFlags &= ~VisualElementTransformFlags.HierarchyDisplayed;

                // Only send this event if the element hasn't received it yet
                if ((m_Flags & VisualElementFlags.NeedsAttachToPanelEvent) == VisualElementFlags.NeedsAttachToPanelEvent)
                {
                    if (HasSelfEventInterests(AttachToPanelEvent.EventCategory))
                    {
                        using (var e = AttachToPanelEvent.GetPooled(prevPanel, elementPanel))
                        {
                            EventDispatchUtilities.SendEventDirectlyToTarget(e, elementPanel, this);
                        }
                    }
                    m_Flags &= ~VisualElementFlags.NeedsAttachToPanelEvent;
                }
            }
            else
            {
                layoutNode.Cache.ClearCachedMeasurements();
            }

            // styles are dependent on topology
            styleInitialized = false;
            stylesDirty = false;
            stylesAncestorOfDirty = false;
            IncrementVersion(VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Transform);

            // persistent data key may have changed or needs initialization
            if (!string.IsNullOrEmpty(viewDataKey))
                IncrementVersion(VersionChangeType.ViewData);
        }

        /// <summary>
        /// Sends an event to the event handler.
        /// </summary>
        /// <remarks>
        /// The event is forwarded to the event dispatcher for processing.
        /// For more information, refer to [[wiki:UIE-Events-Synthesizing|Synthesize and send events]].
        /// </remarks>
        /// <remarks>
        /// SA: [[IEventHandler.HandleEvent]], [[EventDispatcher]], [[EventBase]]
        /// </remarks>
        /// <param name="e">The event to send.</param>
        public sealed override void SendEvent(EventBase e)
        {
            elementPanel?.SendEvent(e);
        }

        internal sealed override void SendEvent(EventBase e, DispatchMode dispatchMode)
        {
            elementPanel?.SendEvent(e, dispatchMode);
        }

        internal sealed override void HandleEvent(EventBase e)
        {
            EventDispatchUtilities.HandleEvent(e, this);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void IncrementVersion(VersionChangeType changeType)
        {
            elementPanel?.OnVersionChanged(this, changeType);
        }

        internal void InvokeHierarchyChanged(HierarchyChangeType changeType, IReadOnlyList<VisualElement> additionalContext = null) { elementPanel?.InvokeHierarchyChanged(this, changeType, additionalContext); }

        [Obsolete("SetEnabledFromHierarchy is deprecated and will be removed in a future release. Please use SetEnabled instead.")]
        protected internal bool SetEnabledFromHierarchy(bool state)
        {
            bool wasEnabledInHierarchy = enabledInHierarchy;
            SetEnabled(state);
            return enabledInHierarchy != wasEnabledInHierarchy;
        }

        private void ApplyDisableHierarchy()
        {
            // When disabling a hierarchy, we need to have everything disabled under the current element.
            // However, when we remove the hierarchy disabling constraint, then each child may or may not get enabled
            // depending on its own disabled state.
            if (!enabledSelf)
            {
                RemoveFromClassList(disabledUssClassNameUnique);
                return;
            }

            pseudoStates |= PseudoStates.Disabled;

            var count = m_Children.Count;
            for (int i = 0; i < count; ++i)
            {
                m_Children[i].ApplyDisableHierarchy();
            }
        }

        private void RemoveDisableHierarchy()
        {
            if (!enabledSelf)
            {
                AddToClassList(disabledUssClassNameUnique);
                return;
            }

            pseudoStates &= ~PseudoStates.Disabled;

            var count = m_Children.Count;
            for (int i = 0; i < count; ++i)
            {
                m_Children[i].RemoveDisableHierarchy();
            }
        }

        private void BlurHierarchyImmediately()
        {
            // If there's nothing to blur or the focused elements are not in this hierarchy, do nothing.
            // Here we only check for the leaf element because if that one is not part of our hierarchy then the other
            // focused elements, being all parent composite roots, will not either.
            if (focusController == null ||
                focusController.GetLeafFocusedElement() is not VisualElement leafFocused ||
                this != leafFocused && !Contains(leafFocused)) return;

            EventDispatcherGate? dispatcherGate = null;
            if (panel?.dispatcher != null)
            {
                dispatcherGate = new EventDispatcherGate(panel.dispatcher);
            }

            using (dispatcherGate)
            {
                // No need to go up the hierarchy to blur each element. Blurring any focused element effectively
                // makes the FocusController set its focus to no element at all.
                leafFocused.BlurImmediately();
            }
        }

        /// <summary>
        /// Returns true if the <see cref="VisualElement"/> is enabled in its own hierarchy.
        /// </summary>
        /// <remarks>
        ///  This flag verifies if the element is enabled globally. A parent disabling its child VisualElement affects this variable.
        /// </remarks>
        [CreateProperty(ReadOnly = true)]
        public bool enabledInHierarchy
        {
            get { return (pseudoStates & PseudoStates.Disabled) != PseudoStates.Disabled; }
        }

        /// <summary>
        /// Changes the <see cref="VisualElement"/> enabled state. A disabled visual element does not receive most events.
        /// </summary>
        /// <param name="value">New enabled state</param>
        /// <remarks>
        /// The method disables the local flag of the VisualElement and implicitly disables its children.
        /// It does not affect the local enabled flag of each child.
        ///\\
        ///\\
        /// A disabled visual element does not receive most input events, such as mouse and keyboard events. However, it can still respond to Attach or Detach events, and geometry change events.
        ///\\
        ///\\
        /// When an element is disabled, its style changes to visually indicate it's inactive.
        /// </remarks>
        /// <remarks>
        /// SA: [[VisualElement.enabledSelf]]
        /// </remarks>
        public void SetEnabled(bool value)
        {
            enabledSelf = value;
        }

        void PropagateParentEnabled(bool parentEnabled)
        {
            if (enabledInHierarchy == parentEnabled)
            {
                // When we add an element that's already disabled to a disabled parent, we need to check if this
                // element was potentially the root of its own disabled subtree. If so, it will no longer be that root,
                // so we have to remove the disabled class from it.
                if (!enabledSelf)
                    RemoveFromClassList(disabledUssClassNameUnique);
                return;
            }

            if (!parentEnabled)
            {
                ApplyDisableHierarchy();
            }
            else
            {
                RemoveDisableHierarchy();
            }
        }

        void PropagateSelfEnabled(bool value)
        {
            // We could call PropagateParentEnabled on each child, but the following saves a few redundant checks
            // and has the same effect.
            if (!value)
            {
                AddToClassList(disabledUssClassNameUnique);
                pseudoStates |= PseudoStates.Disabled;
                var count = m_Children.Count;
                for (int i = 0; i < count; ++i)
                {
                    m_Children[i].ApplyDisableHierarchy();
                }
            }
            else
            {
                RemoveFromClassList(disabledUssClassNameUnique);
                pseudoStates &= ~PseudoStates.Disabled;
                var count = m_Children.Count;
                for (int i = 0; i < count; ++i)
                {
                    m_Children[i].RemoveDisableHierarchy();
                }
            }
        }

        LanguageDirection m_LocalLanguageDirection;
        internal LanguageDirection localLanguageDirection
        {
            get => m_LocalLanguageDirection;
            set
            {
                if (m_LocalLanguageDirection == value)
                    return;

                m_LocalLanguageDirection = value;

                IncrementVersion(VersionChangeType.Layout | VersionChangeType.Repaint);
                var count = m_Children.Count;
                for (int i = 0; i < count; ++i)
                {
                    if(m_Children[i].languageDirection == LanguageDirection.Inherit)
                        m_Children[i].localLanguageDirection = m_LocalLanguageDirection;
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
        ///
        /// Invisible elements are ignored by pointer events and by <see cref="IPanel.Pick"/>.
        /// </remarks>
        /// <seealso cref="VisualElement.resolvedStyle"/>
        /// <seealso cref="VisualElement.style"/>
        [CreateProperty]
        public bool visible
        {
            get
            {
                return resolvedStyle.visibility == Visibility.Visible;
            }
            set
            {
                var previous = visible;
                // Note: this could causes an allocation because styles are copy-on-write
                // we might want to remove this setter altogether
                // so everything goes through style.visibility (and then it's documented in a single place)
                style.visibility = value ? Visibility.Visible : Visibility.Hidden;
                if (previous != visible)
                    NotifyPropertyChanged(visibleProperty);
            }
        }

        /// <summary>
        /// Marks that the <see cref="VisualElement"/> requires a repaint.
        /// </summary>
        /// <remarks>
        /// Don't call this method when you change the styles or properties of built-in controls, as this is automatically called when necessary.
        /// However, you might need to call this method when your element has a custom ::ref::generateVisualContent that needs to be triggered, such as after you change a custom style or property.
        /// </remarks>
        public void MarkDirtyRepaint()
        {
            IncrementVersion(VersionChangeType.Repaint);
        }

        /// <summary>
        /// Checks if the <see cref="VisualElement"/> is marked dirty for repaint.
        /// </summary>
        public bool IsMarkedForRepaint()
        {
            // This digs way too deep into the rendering stuff
            if (renderData == null)
                return true;

            return (renderData.dirtiedValues & RenderDataDirtyTypes.Visuals) == RenderDataDirtyTypes.Visuals;
        }

        /// <summary>
        /// Delegate function to generate the visual content of a visual element.
        /// </summary>
        /// <remarks>
        /// Use this delegate to generate custom geometry in the content region of the <see cref="VisualElement"/>.
        /// You can use the <see cref="MeshGenerationContext.painter2D"/> object to generate visual content,
        /// or use the <see cref="MeshGenerationContext.Allocate"/> method to manually allocate a mesh and then fill in the vertices and indices.
        ///\\
        ///\\
        /// This delegate is called during the initial creation of the <see cref="VisualElement"/> and whenever a repaint is needed.
        /// This delegate isn't called on every frame refresh. To force a repaint, call <see cref="VisualElement.MarkDirtyRepaint"/>.
        ///\\
        ///\\
        /// __Note__: When you execute code in a handler to this delegate, don't update any property of the <see cref="VisualElement"/>, as this can
        /// alter the generated content and cause unwanted side effects, such as lagging or missed updates. To avoid this, treat the <see cref="VisualElement"/>
        /// as read-only within the delegate.
        /// </remarks>
        /// <remarks>
        /// SA: [[MeshGenerationContext]]
        /// </remarks>
        public Action<MeshGenerationContext> generateVisualContent { get; set; }

        static readonly Unity.Profiling.ProfilerMarker k_GenerateVisualContentMarker = new(ProfilerCategory.UIToolkit, "GenerateVisualContent");

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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
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
            Undefined = LayoutMeasureMode.Undefined,
            /// <summary>
            /// The element should give the width/height that is passed in and derive the opposite site from this value (for example, calculate text size from a fixed width).
            /// </summary>
            Exactly = LayoutMeasureMode.Exactly,
            /// <summary>
            /// At Most. The element should give its preferred width/height but no more than the value passed.
            /// </summary>
            AtMost = LayoutMeasureMode.AtMost
        }

        internal bool requireMeasureFunction
        {
            get => (m_Flags & VisualElementFlags.RequireMeasureFunction) == VisualElementFlags.RequireMeasureFunction;
            set
            {
                m_Flags = value ? m_Flags | VisualElementFlags.RequireMeasureFunction : m_Flags & ~VisualElementFlags.RequireMeasureFunction;
                if (value && !layoutNode.UsesMeasure)
                {
                    AssignMeasureFunction();
                }
                else if (!value && layoutNode.UsesMeasure)
                {
                    RemoveMeasureFunction();
                }
            }
        }

        internal bool usesContainsPoint
        {
            get => (transformFlags & VisualElementTransformFlags.UsesContainsPoint) == VisualElementTransformFlags.UsesContainsPoint;
            set => transformFlags = value ? transformFlags | VisualElementTransformFlags.UsesContainsPoint : transformFlags & ~VisualElementTransformFlags.UsesContainsPoint;
        }

        private void AssignMeasureFunction()
        {
            layoutNode.UsesMeasure = true;
        }

        private void RemoveMeasureFunction()
        {
            layoutNode.UsesMeasure = false;
        }

        /// <undoc/>
        /// TODO this is public but since "requiresMeasureFunction" is internal this is not useful for users
        protected internal virtual Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            return new Vector2(float.NaN, float.NaN);
        }

        internal static void Measure(ref LayoutNode node, float width, LayoutMeasureMode widthMode, float height, LayoutMeasureMode heightMode, out LayoutSize result)
        {
            result = default;

            if (!BaseVisualElementPanel.TryGetPanelFromHandle(node.Config.Handle, out var panel))
            {
                Debug.Assert(false, "LayoutNode needs to belong to an element attached to a panel.");
                return;
            }

            var ve = panel.GetMemberElementFromHandle(node.Handle);
            Debug.Assert(node.Equals(ve.layoutNode), "LayoutNode instance mismatch");
            Vector2 size = ve.DoMeasure(width, (MeasureMode)widthMode, height, (MeasureMode)heightMode);
            float ppp = ve.scaledPixelsPerPoint;
            result = new LayoutSize(AlignmentUtils.RoundToPixelGrid(size.x, ppp), AlignmentUtils.RoundToPixelGrid(size.y, ppp));
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void SetSize(Vector2 size)
        {
            style.width = size.x;
            style.height = size.y;
            style.position = Position.Absolute;

            unsafe
            {
                layoutNode.Layout.Dimensions[(int)Layout.LayoutDimension.Width] = size.x;
                layoutNode.Layout.Dimensions[(int)Layout.LayoutDimension.Height] = size.y;
            }
        }

        void FinalizeLayout(VersionChangeType changes)
        {
            if ((changes & VersionChangeType.Layout) != 0)
            {
                layoutNode.MarkDirty();
                (this as TextElement)?.RefreshCachedFontAsset();
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void SetInlineRule(StyleSheet sheet, StyleRule rule, StyleVariableContext variableContext = null)
        {
            if (inlineStyleAccess == null)
                inlineStyleAccess = new InlineStyleAccess(this);

            inlineStyleAccess.SetInlineRule(sheet, rule, variableContext ?? this.variableContext);
        }

        // Used by the builder to apply the inline styles without passing by SetComputedStyle
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void UpdateInlineRule(StyleSheet sheet, StyleRule rule)
        {
            UpdateInlineRule(sheet, rule, null);
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void UpdateInlineRule(StyleSheet sheet, StyleRule rule, StyleVariableContext variableContext)
        {
            var oldStyle = computedStyle.Acquire();

            var rulesHash = computedStyle.matchingRulesHash;
            if (!StyleCache.TryGetValue(rulesHash, out var baseComputedStyle))
                baseComputedStyle = InitialStyle.Get();

            computedStyle.CopyFrom(ref baseComputedStyle);

            SetInlineRule(sheet, rule, variableContext);

            var changes = ComputedStyle.CompareChanges(ref oldStyle, ref computedStyle);
            oldStyle.Release();

            // Since we called ComputedStyle.CopyFrom, we need to sync the data pointer even if there are no changes.
            FinalizeLayout(changes);
            IncrementVersion(changes);
        }

        internal void SetComputedStyle(ref ComputedStyle newStyle)
        {
            // When a parent class list change all children get their styles recomputed.
            // A lot of time the children won't change and the same style will get computed so we can early exit in that case.
            if (computedStyle.matchingRulesHash == newStyle.matchingRulesHash)
                return;

            var changes = ComputedStyle.CompareChanges(ref computedStyle, ref newStyle);
            if (changes == 0)
                return;

            // Here we do a "smart" copy of the style instead of just acquiring them to prevent additional GC alloc.
            // If this element has no inline styles it will release the current style data group and acquire the new one.
            // However, when there a inline styles the style data group that is inline will have a ref count of 1
            // so instead of releasing it and acquiring a new one we just copy the data to save on GC alloc.
            computedStyle.CopyFrom(ref newStyle);

            FinalizeLayout(changes);

            if (elementPanel?.GetTopElementUnderPointer(PointerId.mousePointerId) == this)
                elementPanel.cursorManager.SetCursor(computedStyle.cursor);

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
        /// <remarks>
        /// The order of the classes will always be the same between two elements that have the same classes.
        /// However, the ordering of the classes for a given element is not guaranteed to match between executions or
        /// after a domain reload.
        /// </remarks>
        public IEnumerable<string> GetClasses()
        {
            return m_ClassList.ToStringEnumerable();
        }

        /// <summary>
        /// Retrieve the classes for this element.
        /// </summary>
        /// <returns>A class list.</returns>
        /// <remarks>
        /// The order of the classes will always be the same between two elements that have the same classes.
        /// The returned classes are ordered by corresponding UniqueStyleString id. This implies that for a given
        /// element the order is not guaranteed to match between executions or after a domain reload, depending on the
        /// creation order of the UniqueStyleStrings involved.
        /// </remarks>
        public IEnumerable<UniqueStyleString> GetClassNames()
        {
            return m_ClassList;
        }

        // needed to avoid boxing allocation when iterating on the list.
        internal StyleClassListRef GetClassesForIteration()
        {
            return new(m_ClassList);
        }

        internal int classListCount
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => m_ClassList.Count;
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
                m_ClassList.Clear();
                UpdateClassSelectorData();
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        // Mirrors the class-list pointer / count into the VisualElementSelectorData component so
        // the native selector matcher sees the latest set of classes. Called from every class-list
        // mutator. Pointers are stable thanks to StyleClassList's NativeArray storage.
        private void UpdateClassSelectorData()
        {
            if ((m_Flags & VisualElementFlags.Released) != 0)
                throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

            ref var selectorData = ref layoutNode.SelectorData;
            selectorData.classIdStart = m_ClassList.GetClassIdStartOffset();
            selectorData.classCount = m_ClassList.Count;
        }

        /// <summary>
        /// Adds a class to the class list of the element in order to assign styles from USS. Note the class name is case-sensitive.
        /// </summary>
        /// <param name="className">The name of the class to add to the list.</param>
        /// <remarks>This method has no effect if class name is null or empty.</remarks>
        public void AddToClassList(string className)
        {
            AddToClassList(new UniqueStyleString(className));
        }

        /// <summary>
        /// Adds a class to the class list of the element in order to assign styles from USS. Note the class name is case-sensitive.
        /// </summary>
        /// <param name="className">The name of the class to add to the list.</param>
        /// <remarks>This method has no effect if class name is null or empty.</remarks>
        public void AddToClassList(UniqueStyleString className)
        {
            m_ClassList.Add(className, out var added);
            if (added)
            {
                UpdateClassSelectorData();
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        /// <summary>
        /// Adds classes to the class list of the element in order to assign styles from USS. Note the class name is case-sensitive.
        /// </summary>
        /// <param name="className">The name of the class to add to the list.</param>
        /// <param name="className2">The name of a second class to add to the list.</param>
        /// <remarks>
        /// This method ignores class names that are null or empty.
        /// This method has no effect if all provided class names are null or empty.
        /// </remarks>
        public void AddToClassList(string className, string className2)
        {
            AddToClassList(new UniqueStyleString(className), new UniqueStyleString(className2));
        }

        /// <summary>
        /// Adds classes to the class list of the element in order to assign styles from USS. Note the class name is case-sensitive.
        /// </summary>
        /// <param name="className">The name of the class to add to the list.</param>
        /// <param name="className2">The name of a second class to add to the list.</param>
        /// <remarks>
        /// This method ignores class names that are null or empty.
        /// This method has no effect if all provided class names are null or empty.
        /// </remarks>
        public void AddToClassList(UniqueStyleString className, UniqueStyleString className2)
        {
            m_ClassList.Add(className, className2, out var added);
            if (added)
            {
                UpdateClassSelectorData();
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        /// <summary>
        /// Adds multiple classes to the class list of the element in order to assign styles from USS. Note the class names are case-sensitive.
        /// </summary>
        /// <param name="classNames">The names of the classes to add to the list.</param>
        /// <remarks>
        /// This method ignores class names that are null or empty.
        /// This method has no effect if all provided class names are null or empty.
        /// </remarks>
        public void AddToClassList(params string[] classNames)
        {
            var count = classNames.Length;
            Span<UniqueStyleString> tmp = stackalloc UniqueStyleString[count];
            for (var i = 0; i < count; i++)
            {
                tmp[i] = new UniqueStyleString(classNames[i]);
            }
            AddToClassList(tmp);
        }

        /// <summary>
        /// Adds multiple classes to the class list of the element in order to assign styles from USS. Note the class names are case-sensitive.
        /// </summary>
        /// <param name="classNames">The names of the classes to add to the list.</param>
        /// <remarks>
        /// This method ignores class names that are null or empty.
        /// This method has no effect if all provided class names are null or empty.
        /// </remarks>
        public void AddToClassList(params UniqueStyleString[] classNames)
        {
            AddToClassList(new ReadOnlySpan<UniqueStyleString>(classNames));
        }

        /// <summary>
        /// Adds multiple classes to the class list of the element in order to assign styles from USS. Note the class names are case-sensitive.
        /// </summary>
        /// <param name="classNames">The names of the classes to add to the list.</param>
        /// <remarks>
        /// This method ignores class names that are null or empty.
        /// This method has no effect if all provided class names are null or empty.
        /// </remarks>
        public void AddToClassList(ReadOnlySpan<UniqueStyleString> classNames)
        {
            m_ClassList.AddRange(classNames, out var added);
            if (added)
            {
                UpdateClassSelectorData();
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        /// <summary>
        /// Removes a class from the class list of the element.
        /// </summary>
        /// <param name="className">The name of the class to remove to the list.</param>
        /// <remarks>This method has no effect if class name is null or empty.</remarks>
        public void RemoveFromClassList(string className)
        {
            if (UniqueStyleString.TryGet(className, out var ss))
                RemoveFromClassList(ss);
        }

        /// <summary>
        /// Removes a class from the class list of the element.
        /// </summary>
        /// <param name="className">The name of the class to remove to the list.</param>
        /// <remarks>This method has no effect if class name is null or empty.</remarks>
        public void RemoveFromClassList(UniqueStyleString className)
        {
            m_ClassList.Remove(className, out var removed);
            if (removed)
            {
                UpdateClassSelectorData();
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        /// <summary>
        /// Toggles between adding and removing the given class name from the class list.
        /// </summary>
        /// <param name="className">The class name to add or remove from the class list.</param>
        /// <remarks>
        /// Checks for the given class name in the element class list. If the class name is found, it is removed from the class list. If the class name is not found, the class name is added to the class list.
        ///
        /// This method has no effect if class name is null or empty.
        /// </remarks>
        public void ToggleInClassList(string className)
        {
            ToggleInClassList(new UniqueStyleString(className));
        }

        /// <summary>
        /// Toggles between adding and removing the given class name from the class list.
        /// </summary>
        /// <param name="className">The class name to add or remove from the class list.</param>
        /// <remarks>
        /// Checks for the given class name in the element class list. If the class name is found, it is removed from the class list. If the class name is not found, the class name is added to the class list.
        ///
        /// This method has no effect if class name is null or empty.
        /// </remarks>
        public void ToggleInClassList(UniqueStyleString className)
        {
            m_ClassList.Toggle(className);
            UpdateClassSelectorData();
            IncrementVersion(VersionChangeType.StyleSheet);
        }

        /// <summary>
        /// Enables or disables the class with the given name.
        /// </summary>
        /// <param name="className">The name of the class to enable or disable.</param>
        /// <param name="enable">A boolean flag that adds or removes the class name from the class list. If true, EnableInClassList adds the class name to the class list. If false, EnableInClassList removes the class name from the class list.</param>
        /// <remarks>
        /// If enable is true, EnableInClassList adds the class name to the class list. If enable is false, EnableInClassList removes the class name from the class list.
        ///
        /// This method has no effect if class name is null or empty.
        /// </remarks>
        public void EnableInClassList(string className, bool enable)
        {
            EnableInClassList(new UniqueStyleString(className), enable);
        }

        /// <summary>
        /// Enables or disables the class with the given name.
        /// </summary>
        /// <param name="className">The name of the class to enable or disable.</param>
        /// <param name="enable">A boolean flag that adds or removes the class name from the class list. If true, EnableInClassList adds the class name to the class list. If false, EnableInClassList removes the class name from the class list.</param>
        /// <remarks>
        /// If enable is true, EnableInClassList adds the class name to the class list. If enable is false, EnableInClassList removes the class name from the class list.
        ///
        /// This method has no effect if class name is null or empty.
        /// </remarks>
        public void EnableInClassList(UniqueStyleString className, bool enable)
        {
            m_ClassList.Enable(className, enable, out var changed);
            if (changed)
            {
                UpdateClassSelectorData();
                IncrementVersion(VersionChangeType.StyleSheet);
            }
        }

        /// <summary>
        /// Searches for a class in the class list of this element.
        /// </summary>
        /// <param name="cls">The name of the class for the search query.</param>
        /// <returns>Returns true if the class is part of the list. Otherwise, returns false.</returns>
        /// <remarks>This method always returns false if class name is null or empty.</remarks>
        public bool ClassListContains(string cls)
        {
            return UniqueStyleString.TryGet(cls, out var className) &&
                   ClassListContains(className);
        }

        /// <summary>
        /// Searches for a class in the class list of this element.
        /// </summary>
        /// <param name="cls">The name of the class for the search query.</param>
        /// <returns>Returns true if the class is part of the list. Otherwise, returns false.</returns>
        /// <remarks>This method always returns false if class name is null or empty.</remarks>
        public bool ClassListContains(UniqueStyleString cls)
        {
            return m_ClassList.Contains(cls);
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal object GetProperty(PropertyName key)
        {
            CheckUserKeyArgument(key);
            if (m_PropertyBag != null)
            {
                m_PropertyBag.TryGetValue(key, out object value);
                return value;
            }
            return null;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void SetProperty(PropertyName key, object value)
        {
            CheckUserKeyArgument(key);
            SetPropertyInternal(key, value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal bool HasProperty(PropertyName key)
        {
            CheckUserKeyArgument(key);
            return m_PropertyBag?.ContainsKey(key) == true;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal bool ClearProperty(PropertyName key)
        {
            CheckUserKeyArgument(key);
            return m_PropertyBag?.Remove(key) ?? false;
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
            m_PropertyBag ??= new Dictionary<PropertyName, object>();
            m_PropertyBag[key] = value;
        }

        internal void UpdateCursorStyle(long eventType)
        {
            if (elementPanel == null)
                return;

            if (eventType == MouseCaptureOutEvent.TypeId())
            {
                // Make sure to update the cursor when an element release the capture
                var elementUnderPointer = elementPanel.GetTopElementUnderPointer(PointerId.mousePointerId);
                if (elementUnderPointer != null)
                {
                    elementPanel.cursorManager.SetCursor(elementUnderPointer.computedStyle.cursor);
                }
                else
                {
                    elementPanel.cursorManager.ResetCursor();
                }
                return;
            }

            var capturingElement = elementPanel.GetCapturingElement(PointerId.mousePointerId);
            if (capturingElement != null && capturingElement != this)
                return;

            if (eventType == MouseOverEvent.TypeId() && elementPanel.GetTopElementUnderPointer(PointerId.mousePointerId) == this)
            {
                elementPanel.cursorManager.SetCursor(computedStyle.cursor);
            }
            // Only allows cursor reset when mouse capture is released
            else if (eventType == MouseOutEvent.TypeId() && capturingElement == null)
            {
                elementPanel.cursorManager.ResetCursor();
            }
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
        /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
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
        /// <remarks>For more information, refer to [[wiki:UIE-manipulators|Manipulators]] in the User Manual.</remarks>
        /// <param name="ele">VisualElement associated to the manipulator.</param>
        /// <param name="manipulator">Manipulator to be removed from the VisualElement.</param>
        public static void RemoveManipulator(this VisualElement ele, IManipulator manipulator)
        {
            if (manipulator != null)
            {
                manipulator.target = null;
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, UniqueStyleString className)
            where TElement : VisualElement
        {
            ele.AddToClassList(className);
            return ele;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, UniqueStyleString className, UniqueStyleString className2)
            where TElement : VisualElement
        {
            ele.AddToClassList(className, className2);
            return ele;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, UniqueStyleString className, UniqueStyleString className2, UniqueStyleString className3)
            where TElement : VisualElement
        {
            unsafe
            {
                var classNames = stackalloc UniqueStyleString[3];
                classNames[0] = className;
                classNames[1] = className2;
                classNames[2] = className3;
                ele.AddToClassList(new ReadOnlySpan<UniqueStyleString>(classNames, 3));
            }
            return ele;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, params UniqueStyleString[] classNames)
            where TElement : VisualElement
        {
            ele.AddToClassList(classNames);
            return ele;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, string className)
            where TElement : VisualElement
        {
#pragma warning disable RS0030
            ele.AddToClassList(className);
#pragma warning restore RS0030
            return ele;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, string className, string className2)
            where TElement : VisualElement
        {
            ele.AddToClassList(className, className2);
            return ele;
        }

        private static readonly string[] k_ThreeStrings = new string[3];
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, string className, string className2, string className3)
            where TElement : VisualElement
        {
            k_ThreeStrings[0] = className;
            k_ThreeStrings[1] = className2;
            k_ThreeStrings[2] = className3;
            ele.AddToClassList(k_ThreeStrings);
            return ele;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static TElement WithClassList<TElement>(this TElement ele, params string[] classNames)
            where TElement : VisualElement
        {
            ele.AddToClassList(classNames);
            return ele;
        }
    }

    /// <undoc/>
    public static class VisualElementDebugExtensions
    {
        internal static string GetDisplayName(this VisualElement ve, bool withHashCode = true)
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

        /// <summary>
        /// Invalidate some values in the retained data of UI Toolkit.
        /// </summary>
        /// <remarks>
        /// This method should only be used for debugging as in normal conditions the relevant state should be dirtied by modifying properties or calling <see cref="VisualElement.MarkDirtyRepaint"/> "
        /// </remarks>
        /// <param name="ve"> Element to which the change applies</param>
        /// <param name="changeType">The type of change requested</param>
        public static void DebugIncrementVersionChange(VisualElement ve, VersionChangeType changeType)
        {
            ve.IncrementVersion((VersionChangeType)changeType);
        }

    }
}
