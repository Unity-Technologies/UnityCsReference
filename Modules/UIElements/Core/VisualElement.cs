// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Properties;
using UnityEngine.Internal;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.UIElements.UIR;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // Keep in sync with PseudoStates in Modules/UIElements/PseudoStates.h
    // pseudo states are used for common states of a widget
    // they are addressable from CSS via the pseudo state syntax ":selected" for example
    // while css class list can solve the same problem, pseudo states are a fast commonly agreed upon path for common cases.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

    //keep in sync with VisualNodeFlags.h
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
        // Need to compute world bounding box
        EventInterestParentCategoriesDirty = 1 << 5,
        // Element layout is manually set
        LayoutManual = 1 << 6,
        // Element is a root for composite controls
        CompositeRoot = 1 << 7,
        // Element has a custom measure function
        RequireMeasureFunction = 1 << 8,
        // Element has view data persistence
        EnableViewDataPersistence = 1 << 9,
        // Element never clip regardless of overflow style (useful for ScrollView)
        DisableClipping = 1 << 10,
        // Element needs to receive an AttachToPanel event
        NeedsAttachToPanelEvent = 1 << 11,
        // Element is shown in the hierarchy (element or one of its ancestors is not DisplayStyle.None)
        // Note that this flag is up-to-date only after UIRLayoutUpdater is done with its updates
        HierarchyDisplayed = 1 << 12,
        // Element style are computed
        StyleInitialized = 1 << 13,
        // Element is not rendered, but we keep the generated geometry in case it is shown later
        DisableRendering = 1 << 14,
        // Element uses 3-D transforms or contains children that do
        Needs3DBounds = 1 << 15,
        // Element's 3-D transform local bounds need to be recalculated
        LocalBounds3DDirty = 1 << 16,
        // The DataSource tracking of the element should not ne processed when the element has not been configured properly
        DetachedDataSource = 1 << 17,
        // Element has capture on one or more pointerIds
        PointerCapture = 1 << 18,

        // Element initial flags
        Init = WorldTransformDirty | WorldTransformInverseDirty | WorldClipDirty | BoundingBoxDirty | WorldBoundingBoxDirty | EventInterestParentCategoriesDirty | LocalBounds3DDirty | DetachedDataSource
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
    /// Indicates the directionality of the element's text.
    /// </summary>
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
        static ObjectPool<List<T>> pool = new ObjectPool<List<T>>(() => new List<T>(),20);

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

    internal class StringObjectListPool : ObjectListPool<string>
    {
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
    public partial class VisualElement : Focusable, ITransform
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>.
        /// </summary>
        [Serializable]
        public class UxmlSerializedData : UIElements.UxmlSerializedData
        {
            /// <summary>
            /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
            /// </summary>
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                    {
                        new(nameof(name), "name"),
                        new(nameof(enabledSelf), "enabled"),
                        new(nameof(viewDataKey), "view-data-key"),
                        new(nameof(pickingMode), "picking-mode", null, "pickingMode"),
                        new(nameof(tooltip), "tooltip"),
                        new(nameof(usageHints), "usage-hints"),
                        new(nameof(tabIndex), "tabindex"),
                        new(nameof(focusable), "focusable"),
                        new(nameof(languageDirection), "language-direction"),
                        new(nameof(dataSourceUnityObject), "data-source"),
                        new(nameof(dataSourcePathString), "data-source-path"),
                        new(nameof(dataSourceTypeString), "data-source-type", typeof(object)),
                        new(nameof(bindings), "Bindings"),
                    }
                );
            }

#pragma warning disable 649
            [SerializeField, HideInInspector] string name;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags name_UxmlAttributeFlags;
            [UxmlAttribute("enabled")]
            [SerializeField, Tooltip("Sets the element to disabled which will not accept input. Utilizes the :disabled pseudo state.")] bool enabledSelf;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags enabledSelf_UxmlAttributeFlags;
            [SerializeField] string viewDataKey;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags viewDataKey_UxmlAttributeFlags;
            [UxmlAttribute(obsoleteNames = new[] { "pickingMode" })]
            [SerializeField] PickingMode pickingMode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags pickingMode_UxmlAttributeFlags;
            [SerializeField] string tooltip;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags tooltip_UxmlAttributeFlags;
            [SerializeField] UsageHints usageHints;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags usageHints_UxmlAttributeFlags;
            [UxmlAttribute("tabindex")]
            [SerializeField] int tabIndex;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags tabIndex_UxmlAttributeFlags;
            [SerializeField] bool focusable;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags focusable_UxmlAttributeFlags;
            [SerializeField] LanguageDirection languageDirection;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags languageDirection_UxmlAttributeFlags;

            [Tooltip(DataBinding.k_DataSourceTooltip)]
            [SerializeField, HideInInspector, DataSourceDrawer, UxmlAttribute("data-source")] Object dataSourceUnityObject;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags dataSourceUnityObject_UxmlAttributeFlags;

            // We use a string here because the PropertyPath struct is not serializable
            [UxmlAttribute("data-source-path")]
            [Tooltip(DataBinding.k_DataSourcePathTooltip)]
            [SerializeField, HideInInspector] string dataSourcePathString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags dataSourcePathString_UxmlAttributeFlags;

            [UxmlAttribute("data-source-type")]
            [Tooltip(DataBinding.k_DataSourceTooltip)]
            [SerializeField, HideInInspector, UxmlTypeReferenceAttribute(typeof(object))] string dataSourceTypeString;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags dataSourceTypeString_UxmlAttributeFlags;

            [SerializeReference, HideInInspector, UxmlObjectReference("Bindings")] List<Binding.UxmlSerializedData> bindings;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags bindings_UxmlAttributeFlags;
            #pragma warning restore 649

            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal bool HasBindingInternal(string property)
            {
                if (bindings == null)
                    return false;

                foreach (var binding in bindings)
                {
                    if (binding.property == property)
                        return true;
                }

                return false;
            }

            [ExcludeFromDocs]
            public override object CreateInstance() => new VisualElement();

            [ExcludeFromDocs]
            public override void Deserialize(object obj)
            {
                var e = (VisualElement)obj;

                if (ShouldWriteAttributeValue(name_UxmlAttributeFlags))
                    e.name = name;
                if (ShouldWriteAttributeValue(enabledSelf_UxmlAttributeFlags))
                    e.enabledSelf = enabledSelf;
                if (ShouldWriteAttributeValue(viewDataKey_UxmlAttributeFlags))
                    e.viewDataKey = viewDataKey;
                if (ShouldWriteAttributeValue(pickingMode_UxmlAttributeFlags))
                    e.pickingMode = pickingMode;
                if (ShouldWriteAttributeValue(tooltip_UxmlAttributeFlags))
                    e.tooltip = tooltip;
                if (ShouldWriteAttributeValue(usageHints_UxmlAttributeFlags))
                    e.usageHints = usageHints;
                if (ShouldWriteAttributeValue(tabIndex_UxmlAttributeFlags))
                    e.tabIndex = tabIndex;
                if (ShouldWriteAttributeValue(focusable_UxmlAttributeFlags))
                    e.focusable = focusable;
                if (ShouldWriteAttributeValue(dataSourceUnityObject_UxmlAttributeFlags))
                    e.dataSourceUnityObject = dataSourceUnityObject ? dataSourceUnityObject : null;
                if (ShouldWriteAttributeValue(dataSourcePathString_UxmlAttributeFlags))
                    e.dataSourcePathString = dataSourcePathString;
                if (ShouldWriteAttributeValue(dataSourceTypeString_UxmlAttributeFlags))
                    e.dataSourceTypeString = dataSourceTypeString;
                if (ShouldWriteAttributeValue(languageDirection_UxmlAttributeFlags))
                    e.languageDirection = languageDirection;

                if (ShouldWriteAttributeValue(bindings_UxmlAttributeFlags))
                {
                    e.bindings.Clear();
                    if (bindings != null)
                    {
                        foreach (var bindingData in bindings)
                        {
                            var binding = (Binding)bindingData.CreateInstance();
                            bindingData.Deserialize(binding);
                            e.SetBinding(binding.property, binding);
                            e.bindings.Add(binding);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="VisualElement"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public class UxmlFactory : UxmlFactory<VisualElement, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="VisualElement"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public class UxmlTraits : UIElements.UxmlTraits
        {
            protected UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_NameAttributeName };
            private UxmlBoolAttributeDescription m_EnabledSelf = new UxmlBoolAttributeDescription { name = "enabled", defaultValue = true };
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

            UxmlAssetAttributeDescription<Object> m_DataSource = new UxmlAssetAttributeDescription<Object>() { name = "data-source" };
            UxmlStringAttributeDescription m_DataSourcePath = new UxmlStringAttributeDescription() { name = "data-source-path" };
            UxmlTypeAttributeDescription<object> m_DataSourceType = new UxmlTypeAttributeDescription<object> { name = "data-source-type" };

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
                ve.enabledSelf = m_EnabledSelf.GetValueFromBag(bag, cc);
                ve.viewDataKey = m_ViewDataKey.GetValueFromBag(bag, cc);
                ve.pickingMode = m_PickingMode.GetValueFromBag(bag, cc);
                ve.usageHints = m_UsageHints.GetValueFromBag(bag, cc);
                ve.tooltip = m_Tooltip.GetValueFromBag(bag, cc);

                int index = 0;
                if (focusIndex.TryGetValueFromBag(bag, cc, ref index))
                {
                    ve.tabIndex = index >= 0 ? index : 0;
                    ve.focusable = index >= 0;
                }

                // tabIndex and focusable overrides obsolete focusIndex.
                ve.tabIndex = m_TabIndex.GetValueFromBag(bag, cc);
                ve.focusable = focusable.GetValueFromBag(bag, cc);

                ve.dataSource = m_DataSource.GetValueFromBag(bag, cc);
                ve.dataSourcePath = new PropertyPath(m_DataSourcePath.GetValueFromBag(bag, cc));
                ve.dataSourceType = m_DataSourceType.GetValueFromBag(bag, cc);

                // We ignore m_Class, it was processed in UIElementsViewImporter.
                // We ignore m_ContentContainer, it was processed in UIElementsViewImporter.
                // We ignore m_Style, it was processed in UIElementsViewImporter.
            }
        }

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
            get => (m_Flags & VisualElementFlags.HierarchyDisplayed) == VisualElementFlags.HierarchyDisplayed;
            set
            {
                m_Flags = value ? m_Flags | VisualElementFlags.HierarchyDisplayed : m_Flags & ~VisualElementFlags.HierarchyDisplayed;

                if(value && ( renderChainData.pendingRepaint|| renderChainData.pendingHierarchicalRepaint))
                    IncrementVersion(VersionChangeType.Repaint);
            }
        }

        internal bool hasOneOrMorePointerCaptures
        {
            get => (m_Flags & VisualElementFlags.PointerCapture) == VisualElementFlags.PointerCapture;
            set => m_Flags = value ? m_Flags | VisualElementFlags.PointerCapture : m_Flags & ~VisualElementFlags.PointerCapture;
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
        private Dictionary<PropertyName, object> m_PropertyBag;
        internal VisualElementFlags m_Flags;

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

        internal Color playModeTintColor => disablePlayModeTint ? Color.white : UIElementsUtility.editorPlayModeTintColor;

        /// <summary>
        /// A combination of hint values that specify high-level intended usage patterns for the <see cref="VisualElement"/>.
        /// This property can only be set when the <see cref="VisualElement"/> is not yet part of a <see cref="Panel"/>. Once part of a <see cref="Panel"/>, this property becomes effectively read-only, and attempts to change it will throw an exception.
        /// The specification of proper <see cref="UsageHints"/> drives the system to make better decisions on how to process or accelerate certain operations based on the anticipated usage pattern.
        /// Note that those hints do not affect behavioral or visual results, but only affect the overall performance of the panel and the elements within.
        /// It's advised to always consider specifying the proper <see cref="UsageHints"/>, but keep in mind that some <see cref="UsageHints"/> might be internally ignored under certain conditions (e.g. due to hardware limitations on the target platform).
        /// </summary>
        [CreateProperty]
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

                NotifyPropertyChanged(usageHintsProperty);
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
        internal RenderChainVEData renderChainData;
        internal bool shouldCutRenderChain;
        internal UIRenderer uiRenderer; // Null for non-world panels

        /// <summary>
        /// Returns a transform styles object for this VisualElement.
        /// <seealso cref="ITransform"/>
        /// </summary>
        /// <remarks>
        /// The transform styles object contains the position, rotation, scale style properties of this VisualElement.
        /// __Note__: This transform object is different and separate from the GameObject Transform MonoBehaviour.
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
            get => (m_Flags & VisualElementFlags.LayoutManual) == VisualElementFlags.LayoutManual;
            private set => m_Flags = value ? m_Flags | VisualElementFlags.LayoutManual : m_Flags & ~VisualElementFlags.LayoutManual;
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
                    Debug.LogWarning("Tying to acces dpi setting of a visual element not on a panel");
                    return GUIUtility.pixelsPerPoint;
                }

                return elementPanel.scaledPixelsPerPoint;
            }
        }

        [Obsolete("scaledPixelsPerPoint_noChecks is deprecated. Use scaledPixelsPerPoint instead.")]
        internal float scaledPixelsPerPoint_noChecks => elementPanel?.scaledPixelsPerPoint ?? GUIUtility.pixelsPerPoint;

        [Obsolete("unityBackgroundScaleMode is deprecated. Use background-* properties instead.")]
        StyleEnum<ScaleMode> IResolvedStyle.unityBackgroundScaleMode => resolvedStyle.unityBackgroundScaleMode;

        Rect m_Layout;

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
                var result = m_Layout;
                if (!layoutNode.IsUndefined && !isLayoutManual)
                {
                    result.x = layoutNode.LayoutX;
                    result.y = layoutNode.LayoutY;
                    result.width = layoutNode.LayoutWidth;
                    result.height = layoutNode.LayoutHeight;
                }
                return result;
            }
            internal set
            {
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

        internal void ClearManualLayout()
        {
            // Mark layout manual false to re-enable layout calculation.
            isLayoutManual = false;

            // Remove inline values.
            var styleAccess = style;
            styleAccess.position = StyleKeyword.Null;
            styleAccess.marginLeft = StyleKeyword.Null;
            styleAccess.marginRight = StyleKeyword.Null;
            styleAccess.marginBottom = StyleKeyword.Null;
            styleAccess.marginTop = StyleKeyword.Null;
            styleAccess.left = StyleKeyword.Null;
            styleAccess.top = StyleKeyword.Null;
            styleAccess.right = StyleKeyword.Null;
            styleAccess.bottom = StyleKeyword.Null;
            styleAccess.width = StyleKeyword.Null;
            styleAccess.height = StyleKeyword.Null;
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
            get => (m_Flags & VisualElementFlags.Needs3DBounds) != 0;
            set => m_Flags = value ? m_Flags | VisualElementFlags.Needs3DBounds : m_Flags & ~VisualElementFlags.Needs3DBounds;
        }

        internal bool isLocalBounds3DDirty
        {
            get => (m_Flags & VisualElementFlags.LocalBounds3DDirty) != 0;
            set => m_Flags = value ? m_Flags | VisualElementFlags.LocalBounds3DDirty : m_Flags & ~VisualElementFlags.LocalBounds3DDirty;
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

        private const VisualElementFlags worldBoundingBoxDirtyDependencies =
            VisualElementFlags.WorldBoundingBoxDirty | VisualElementFlags.BoundingBoxDirty |
            VisualElementFlags.WorldTransformDirty;

        internal bool isWorldBoundingBoxOrDependenciesDirty => (m_Flags & worldBoundingBoxDirtyDependencies) != 0;

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
                if (isWorldBoundingBoxOrDependenciesDirty)
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
                if (!ShouldClip() && resolvedStyle.display == DisplayStyle.Flex)
                {
                    var childCount = m_Children.Count;
                    for (int i = 0; i < childCount; i++)
                    {
                        if (!m_Children[i].areAncestorsAndSelfDisplayed)
                            continue;
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

        internal Bounds localBounds3D
        {
            get
            {
                if (isLocalBounds3DDirty)
                {
                    UpdateLocalBoundsAndPickingBounds3D();
                    isLocalBounds3DDirty = false;
                }

                return WorldSpaceDataStore.GetWorldSpaceData(this).localBounds3D;
            }
        }

        void UpdateLocalBoundsAndPickingBounds3D()
        {
            if (!areAncestorsAndSelfDisplayed)
            {
                WorldSpaceDataStore.SetWorldSpaceData(this, new WorldSpaceData
                {
                    localBounds3D = WorldSpaceData.k_Empty3DBounds
                });
                return;
            }

            if (!needs3DBounds)
            {
                // Fast path for elements that don't need 3D transforms
                var bbox = boundingBox;
                var localBounds = new Bounds(bbox.center, bbox.size);
                WorldSpaceDataStore.SetWorldSpaceData(this, new WorldSpaceData
                {
                    localBounds3D = localBounds
                });
                return;
            }

            var bounds = new Bounds(rect.center, rect.size);

            if (!ShouldClip())
            {
                var childCount = hierarchy.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    var childBounds = hierarchy[i].localBounds3D;
                    if (childBounds.extents.x >= 0)
                    {
                        hierarchy[i].TransformAlignedBoundsToParentSpace(ref childBounds);
                        bounds.Encapsulate(childBounds);
                    }
                }
            }

            WorldSpaceDataStore.SetWorldSpaceData(this, new WorldSpaceData
            {
                localBounds3D = bounds
            });
        }

        /// <summary>
        /// Returns a <see cref="Rect"/> representing the Axis-aligned Bounding Box (AABB) after applying the world transform.
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        private const VisualElementFlags worldTransformInverseDirtyDependencies =
            VisualElementFlags.WorldTransformInverseDirty | VisualElementFlags.WorldTransformDirty;

        internal bool isWorldTransformInverseOrDependenciesDirty =>
            (m_Flags & worldTransformInverseDirtyDependencies) != 0;

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
        [CreateProperty(ReadOnly = true)]
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
                if (isWorldTransformInverseOrDependenciesDirty)
                    UpdateWorldTransformInverse();
                return ref m_WorldTransformInverseCache;
            }
        }

        internal void UpdateWorldTransform()
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
                    TranslateMatrix34(ref hierarchy.parent.worldTransformRef, positionWithLayout, out m_WorldTransformCache);
                }
                else
                {
                    GetPivotedMatrixWithLayout(out var mat);
                    MultiplyMatrix34(ref hierarchy.parent.worldTransformRef,  ref mat, out m_WorldTransformCache);
                }
            }
            else
            {
                GetPivotedMatrixWithLayout(out m_WorldTransformCache);
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
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal PseudoStates pseudoStates
        {
            get { return m_PseudoStates; }
            set
            {
                PseudoStates diff = m_PseudoStates ^ value;
                if ((int)diff > 0)
                {
                    if ((value & PseudoStates.Root) == PseudoStates.Root)
                        isRootVisualContainer = true;

                    // If only the root changed do not trigger a new style update since the root
                    // pseudo state change base on the current style sheet when selectors are matched.
                    if (diff != PseudoStates.Root)
                    {
                        var added = diff & value;
                        var removed = diff & m_PseudoStates;

                        if ((triggerPseudoMask & added) != 0
                            || (dependencyPseudoMask & removed) != 0)
                        {
                            IncrementVersion(VersionChangeType.StyleSheet);
                        }
                    }

                    m_PseudoStates = value;
                }
            }
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

        private PickingMode m_PickingMode;

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
        public PickingMode pickingMode
        {
            get => m_PickingMode;
            set
            {
                if (m_PickingMode == value)
                    return;
                m_PickingMode = value;
                IncrementVersion(VersionChangeType.Picking);
                NotifyPropertyChanged(pickingModeProperty);
            }
        }

        /// <summary>
        /// The name of this VisualElement.
        /// </summary>
        /// <remarks>
        /// Use this property to write USS selectors that target a specific element.
        /// The standard practice is to give an element a unique name.
        /// </remarks>
        [CreateProperty]
        public string name
        {
            get { return m_Name; }
            set
            {
                if (m_Name == value)
                    return;
                m_Name = value;
                IncrementVersion(VersionChangeType.StyleSheet);
                NotifyPropertyChanged(nameProperty);
            }
        }

        internal List<string> classList
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get
            {
                if (ReferenceEquals(m_ClassList, s_EmptyClassList))
                {
                    m_ClassList = StringObjectListPool.Get();
                }

                return m_ClassList;
            }
        }

        internal string fullTypeName
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => typeData.fullTypeName;
        }

        internal string typeName
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => typeData.typeName;
        }

        // TODO: Make sure we do not use new native layout before we fix android 32bit (arm v7) failing test.
        // VisualNode m_VisualNode;
        LayoutNode m_LayoutNode;

        // Set and pass in values to be used for layout
        internal ref LayoutNode layoutNode
        {
            get
            {
                return ref m_LayoutNode;
            }
        }

        internal ComputedStyle m_Style = InitialStyle.Acquire();

        internal ref ComputedStyle computedStyle
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get => ref m_Style;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
			// TODO: Make sure we do not use new native layout before we fix android 32bit (arm v7) failing test.
            // m_VisualNode = VisualManager.SharedManager.CreateNode();
            // m_LayoutNode = LayoutManager.SharedManager.CreateNode();

            // m_VisualNode.SetOwner(this);
            // m_VisualNode.SetLayout(m_LayoutNode);

            hierarchy = new Hierarchy(this);

            m_ClassList = s_EmptyClassList;
            m_Flags = VisualElementFlags.Init;
            enabledSelf = true;

            focusable = false;

            name = string.Empty;
            layoutNode = LayoutManager.SharedManager.CreateNode();

            renderHints = RenderHints.None;

            EventInterestReflectionUtils.GetDefaultEventInterests(GetType(),
                out var defaultActionCategories, out var defaultActionAtTargetCategories,
                out var handleEventTrickleDownCategories, out var handleEventBubbleUpCategories);

            m_TrickleDownHandleEventCategories = handleEventTrickleDownCategories;

            // Combine the obsolete default actions into the BubbleUp categories
            m_BubbleUpHandleEventCategories = handleEventBubbleUpCategories |
                                              defaultActionAtTargetCategories | defaultActionCategories;

            UpdateEventInterestSelfCategories();
        }

        ~VisualElement()
        {
            // VisualManager.SharedManager.DestroyNodeThreaded(ref m_VisualNode);
            LayoutManager.SharedManager.DestroyNode(ref m_LayoutNode);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal virtual Rect GetTooltipRect()
        {
            return this.worldBound;
        }

        internal void SetTooltip(TooltipEvent e)
        {
            if (e.currentTarget is VisualElement element && !string.IsNullOrEmpty(element.tooltip))
            {
                e.rect = element.GetTooltipRect();
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

                    InvokeHierarchyChanged(HierarchyChangeType.DetachedFromPanel, elements);
                    foreach (var e in elements)
                    {
                        e.elementPanel = p;
                        e.m_Flags |= flagToAdd;
                        e.m_CachedNextParentWithEventInterests = null;
                    }
                    InvokeHierarchyChanged(HierarchyChangeType.AttachedToPanel, elements);

                    foreach (var e in elements)
                    {
                        e.HasChangedPanel(previousPanel);
                    }
                    p?.liveReloadSystem.StartTracking(elements);
                }
            }
            finally
            {
                VisualElementListPool.Release(elements);
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
                            e.elementTarget = this;
                            EventDispatchUtilities.HandleEventAtTargetAndDefaultPhase(e, elementPanel, this);
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
                layoutNode.Config = elementPanel.layoutConfig;
                layoutNode.SoftReset();

                RegisterRunningAnimations();
                ProcessBindingRequests();
                AttachDataSource();

                // We need to reset any visual pseudo state
                pseudoStates &= ~(PseudoStates.Focus | PseudoStates.Active | PseudoStates.Hover);

                // UUM-42891: We must presume that we're not displayed because when it's the case (i.e. when we are not
                // displayed), the layout updater will not process the children unless there is a display *change* in the ancestors.
                m_Flags &= ~VisualElementFlags.HierarchyDisplayed;

                // Only send this event if the element hasn't received it yet
                if ((m_Flags & VisualElementFlags.NeedsAttachToPanelEvent) == VisualElementFlags.NeedsAttachToPanelEvent)
                {
                    if (HasSelfEventInterests(AttachToPanelEvent.EventCategory))
                    {
                        using (var e = AttachToPanelEvent.GetPooled(prevPanel, elementPanel))
                        {
                            e.elementTarget = this;
                            EventDispatchUtilities.HandleEventAtTargetAndDefaultPhase(e, elementPanel, this);
                        }
                    }
                    m_Flags &= ~VisualElementFlags.NeedsAttachToPanelEvent;
                }
            }
            else
            {
                layoutNode.Config = LayoutManager.SharedManager.GetDefaultConfig();
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void IncrementVersion(VersionChangeType changeType)
        {
            elementPanel?.OnVersionChanged(this, changeType);
        }

        internal void InvokeHierarchyChanged(HierarchyChangeType changeType, IReadOnlyList<VisualElement> additionalContext = null) { elementPanel?.InvokeHierarchyChanged(this, changeType, additionalContext); }

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

        private bool m_EnabledSelf;
        /// <summary>
        /// Returns true if the <see cref="VisualElement"/> is enabled locally.
        /// </summary>
        /// <remarks>
        /// This flag isn't changed if the VisualElement is disabled implicitly by one of its parents. To verify this, use <see cref="enabledInHierarchy"/>.
        /// </remarks>
        [CreateProperty]
        public bool enabledSelf
        {
            get => m_EnabledSelf;
            set
            {
                if (m_EnabledSelf == value)
                    return;

                m_EnabledSelf = value;
                NotifyPropertyChanged(enabledSelfProperty);
                PropagateEnabledToChildren(value);
            }
        }

        /// <summary>
        /// Changes the <see cref="VisualElement"/> enabled state. A disabled visual element does not receive most events.
        /// </summary>
        /// <param name="value">New enabled state</param>
        /// <remarks>
        /// The method disables the local flag of the VisualElement and implicitly disables its children.
        /// It does not affect the local enabled flag of each child.
        ///\\
        /// A disabled visual element does not receive most input events, such as mouse and keyboard events. However, it can still respond to Attach or Detach events, and geometry change events.
        /// <seealso cref="enabledSelf"/>
        /// </remarks>
        public void SetEnabled(bool value)
        {
            enabledSelf = value;
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

        LanguageDirection m_LanguageDirection;
        /// <summary>
        /// Indicates the directionality of the element's text. The value will propagate to the element's children.
        /// </summary>
        /// <remarks>
        /// Setting the languageDirection to RTL adds basic support for right-to-left (RTL) by reversing the text and handling linebreaking
        /// and word wrapping appropriately. However, it does not provide comprehensive RTL support, as this would require text shaping,
        /// which includes the reordering of characters, and OpenType font feature support. Comprehensive RTL support is planned for future updates,
        /// which will involve additional APIs to handle language, script, and font feature specifications.
        ///
        /// To enhance the RTL functionality of this property, users can explore available third-party plugins in the Unity Asset Store and make use of <see cref="ITextElementExperimentalFeatures.renderedText"/>
        /// </remarks>
        [CreateProperty]
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

        LanguageDirection m_LocalLanguageDirection;
        internal LanguageDirection localLanguageDirection
        {
            get => m_LocalLanguageDirection;
            set
            {
                if (m_LocalLanguageDirection == value)
                    return;

                m_LocalLanguageDirection = value;

                IncrementVersion(VersionChangeType.Layout);
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

        static readonly Unity.Profiling.ProfilerMarker k_GenerateVisualContentMarker = new("GenerateVisualContent");

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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
                if (value && !layoutNode.IsMeasureDefined)
                {
                    AssignMeasureFunction();
                }
                else if (!value && layoutNode.IsMeasureDefined)
                {
                    RemoveMeasureFunction();
                }
            }
        }

        private void AssignMeasureFunction()
        {
            layoutNode.SetOwner(this);
            layoutNode.Measure = Measure;
        }

        private void RemoveMeasureFunction()
        {
            layoutNode.Measure = null;
            layoutNode.SetOwner(null);
        }

        /// <undoc/>
        /// TODO this is public but since "requiresMeasureFunction" is internal this is not useful for users
        protected internal virtual Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            return new Vector2(float.NaN, float.NaN);
        }

        internal static void Measure(VisualElement ve, ref LayoutNode node, float width, LayoutMeasureMode widthMode, float height, LayoutMeasureMode heightMode, out LayoutSize result)
        {
            result = default;
            Debug.Assert(node.Equals(ve.layoutNode), "LayoutNode instance mismatch");
            Vector2 size = ve.DoMeasure(width, (MeasureMode)widthMode, height, (MeasureMode)heightMode);
            float ppp = ve.scaledPixelsPerPoint;
            result = new LayoutSize(AlignmentUtils.RoundToPixelGrid(size.x, ppp), AlignmentUtils.RoundToPixelGrid(size.y, ppp));
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
            layoutNode.CopyFromComputedStyle(computedStyle);
        }

        internal void SetInlineRule(StyleSheet sheet, StyleRule rule)
        {
            if (inlineStyleAccess == null)
                inlineStyleAccess = new InlineStyleAccess(this);

            inlineStyleAccess.SetInlineRule(sheet, rule);
        }

        // Used by the builder to apply the inline styles without passing by SetComputedStyle
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

            if (elementPanel?.GetTopElementUnderPointer(PointerId.mousePointerId) == this)
                elementPanel.cursorManager.SetCursor(m_Style.cursor);

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
        /// Adds a class to the class list of the element in order to assign styles from USS. Note the class name is case-sensitive.
        /// </summary>
        /// <param name="className">The name of the class to add to the list.</param>
        public void AddToClassList(string className)
        {
            if (string.IsNullOrEmpty(className))
                return;

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
                if (m_ClassList[i].Equals(cls, StringComparison.Ordinal))
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void SetProperty(PropertyName key, object value)
        {
            CheckUserKeyArgument(key);
            SetPropertyInternal(key, value);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool HasProperty(PropertyName key)
        {
            CheckUserKeyArgument(key);
            return m_PropertyBag?.ContainsKey(key) == true;
        }

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
