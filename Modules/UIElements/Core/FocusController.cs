// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using Unity.Properties;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base class for objects that can get the focus.
    /// </summary>
    /// <remarks>
    /// The focus is used to designate an element that will receive keyboard events.
    /// </remarks>
    public abstract class Focusable : CallbackEventHandler
    {
        internal static readonly BindingId focusableProperty = nameof(focusable);
        internal static readonly BindingId tabIndexProperty = nameof(tabIndex);
        internal static readonly BindingId delegatesFocusProperty = nameof(delegatesFocus);
        internal static readonly BindingId canGrabFocusProperty = nameof(canGrabFocus);

        protected Focusable()
        {
            UIElementsRuntimeUtilityNative.VisualElementCreation();
            focusable = true;
            tabIndex = 0;
        }

        /// <summary>
        /// Return the focus controller for this element.
        /// </summary>
        public abstract FocusController focusController { get; }

        private bool m_Focusable;

        /// <summary>
        /// True if the element can be focused.
        /// </summary>
        [CreateProperty]
        public virtual bool focusable
        {
            get => m_Focusable;
            set
            {
                if (m_Focusable == value)
                    return;
                m_Focusable = value;
                NotifyPropertyChanged(focusableProperty);
            }
        }

        private int m_TabIndex;

        // See http://w3c.github.io/html/editing.html#the-tabindex-attribute
        /// <summary>
        /// An integer used to sort focusables in the focus ring. Must be greater than or equal to zero.
        /// </summary>
        /// <remarks>
        /// Setting the `tabIndex` value to less than `0` (for example, `−1`) removes the element from the focus ring and tab navigation.
        /// </remarks>
        [CreateProperty]
        public int tabIndex
        {
            get => m_TabIndex;
            set
            {
                if (m_TabIndex == value)
                    return;
                m_TabIndex = value;
                NotifyPropertyChanged(tabIndexProperty);
            }
        }

        bool m_DelegatesFocus;

        /// <summary>
        /// Whether the element should delegate the focus to its children.
        /// </summary>
        [CreateProperty]
        public bool delegatesFocus
        {
            get { return m_DelegatesFocus; }
            set
            {
                if (m_DelegatesFocus == value)
                    return;
                m_DelegatesFocus = value;
                NotifyPropertyChanged(delegatesFocusProperty);
            }
        }

        // Used when we want then children of a composite to appear at
        // composite root tabIndex position in the focus ring, but
        // we do not want the root itself to be part of the ring.
        bool m_ExcludeFromFocusRing;
        internal bool excludeFromFocusRing
        {
            get { return m_ExcludeFromFocusRing; }
            set
            {
                if (!((VisualElement)this).isCompositeRoot)
                {
                    throw new InvalidOperationException("excludeFromFocusRing should only be set on composite roots.");
                }
                m_ExcludeFromFocusRing = value;
            }
        }

        /// <summary>
        /// When clicking on a disabled element, the focus should be given to the first focusable parent.
        /// This property is used to prevent certain elements from receiving the focus in this case (e.g Foldout)
        /// <see cref="FocusController.GetFocusableParentForPointerEvent(Focusable, out Focusable)"/>
        /// </summary>
        internal bool isEligibleToReceiveFocusFromDisabledChild
        {
            get;
            set;
        } = true;

        /// <summary>
        /// Return true if the element can be focused.
        /// </summary>
        [CreateProperty(ReadOnly = true)]
        public virtual bool canGrabFocus => focusable;

        /// <summary>
        /// Attempt to give the focus to this element.
        /// </summary>
        public virtual void Focus()
        {
            if (focusController != null)
            {
                if (canGrabFocus)
                {
                    var elementGettingFocused = GetFocusDelegate();
                    focusController.SwitchFocus(elementGettingFocused , this != elementGettingFocused);
                }
                else
                {
                    focusController.SwitchFocus(null);
                }
            }
        }

        /// <summary>
        /// Tell the element to release the focus.
        /// </summary>
        public virtual void Blur()
        {
            focusController?.Blur(this);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void BlurImmediately()
        {
            focusController?.Blur(this, dispatchMode: DispatchMode.Immediate);
        }

        // Use the tree to find the first focusable child.
        // FIXME: we should use the focus ring; however, it may happens that the
        // children are focusable but not part of the ring.
        internal Focusable GetFocusDelegate()
        {
            var f = this;

            while (f != null && f.delegatesFocus)
            {
                f = GetFirstFocusableChild(f as VisualElement);
            }

            return f;
        }

        static Focusable GetFirstFocusableChild(VisualElement ve)
        {
            int veChildCount = ve.hierarchy.childCount;
            for (int i = 0; i < veChildCount; ++i)
            {
                var child = ve.hierarchy[i];

                if (child.canGrabFocus && child.tabIndex >= 0)
                {
                    return child;
                }

                bool isSlot = child.hierarchy.parent != null && child == child.hierarchy.parent.contentContainer;
                if (!child.isCompositeRoot && !isSlot)
                {
                    var f = GetFirstFocusableChild(child);
                    if (f != null)
                    {
                        return f;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Base class for defining in which direction the focus moves in a focus ring.
    /// </summary>
    /// <remarks>
    /// Focus ring implementations can move the focus in various direction; they can derive from this class to formalize the various ways the focus can change from one element to the other.
    /// </remarks>
    public class FocusChangeDirection : IDisposable
    {
        /// <summary>
        /// Focus came from an unspecified direction, for example after a mouse down.
        /// </summary>
        public static FocusChangeDirection unspecified { get; } = new FocusChangeDirection(-1);

        /// <summary>
        /// The null direction. This is usually used when the focus stays on the same element.
        /// </summary>
        public static FocusChangeDirection none { get; } = new FocusChangeDirection(0);

        /// <summary>
        /// Last value for the direction defined by this class.
        /// </summary>
        protected static FocusChangeDirection lastValue { get; } = none;

        readonly int m_Value;

        protected FocusChangeDirection(int value)
        {
            m_Value = value;
        }

        /// <undoc/>
        public static implicit operator int(FocusChangeDirection fcd)
        {
            return fcd?.m_Value ?? 0;
        }

        void IDisposable.Dispose() => Dispose();

        /// <summary>
        /// This method will be called when FocusController has finished treating this focus change directive. If the reference came from a pool, this method can be used to release the data back to the pool.
        /// </summary>
        protected virtual void Dispose() {}

        internal virtual void ApplyTo(FocusController focusController, Focusable f)
        {
            focusController.SwitchFocus(f, this);
        }
    }

    /// <summary>
    /// Interface for classes implementing focus rings.
    /// </summary>
    public interface IFocusRing
    {
        /// <summary>
        /// Get the direction of the focus change for the given event. For example, when the Tab key is pressed, focus should be given to the element to the right.
        /// </summary>
        FocusChangeDirection GetFocusChangeDirection(Focusable currentFocusable, EventBase e);

        /// <summary>
        /// Get the next element in the given direction.
        /// </summary>
        Focusable GetNextFocusable(Focusable currentFocusable, FocusChangeDirection direction);
    }

    /// <summary>
    /// Class in charge of managing the focus inside a Panel.
    /// </summary>
    /// <remarks>
    /// Each Panel should have an instance of this class. The instance holds the currently focused VisualElement and is responsible for changing it.
    /// </remarks>
    public class FocusController
    {
        // https://w3c.github.io/uievents/#interface-focusevent

        /// <summary>
        /// Constructor.
        /// </summary>
        public FocusController(IFocusRing focusRing)
        {
            this.focusRing = focusRing;
            imguiKeyboardControl = 0;
        }

        IFocusRing focusRing { get; }

        struct FocusedElement
        {
            public VisualElement m_SubTreeRoot;
            public VisualElement m_FocusedElement;
        }

        TextElement m_SelectedTextElement;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal TextElement selectedTextElement
        {
            get => m_SelectedTextElement;
            set
            {
                if (m_SelectedTextElement == value)
                    return;

                m_SelectedTextElement?.selection.SelectNone();
                m_SelectedTextElement = value;
            }
        }

        List<FocusedElement> m_FocusedElements = new List<FocusedElement>();

        /// <summary>
        /// The currently focused VisualElement.
        /// </summary>
        public Focusable focusedElement
        {
            get
            {
                var result = GetRetargetedFocusedElement(null);
                return IsLocalElement(result) ? result : null;
            }
        }

        /// <summary>
        /// Instructs the FocusController to ignore the given event.
        /// This will prevent the event from changing the current focused VisualElement or triggering focus events.
        /// </summary>
        /// <param name="evt">The event to be ignored.</param>
        /// <remarks>
        /// In general this will have no effect if the event is not a
        /// <see cref="PointerDownEvent"/>, <see cref="MouseDownEvent"/>, or <see cref="NavigationMoveEvent"/>.
        /// </remarks>
        public void IgnoreEvent(EventBase evt)
        {
            // Setting this to true will make SwitchFocusOnEvent have no effect
            evt.processedByFocusController = true;

            if (evt is IMouseEventInternal me && me.sourcePointerEvent is EventBase pe)
                pe.processedByFocusController = true;
        }

        internal bool IsFocused(Focusable f)
        {
            if (!IsLocalElement(f))
                return false;

            foreach (var fe in m_FocusedElements)
            {
                if (fe.m_FocusedElement == f)
                {
                    return true;
                }
            }

            return false;
        }

        internal Focusable GetRetargetedFocusedElement(VisualElement retargetAgainst)
        {
            var retargetRoot = retargetAgainst?.hierarchy.parent;
            if (retargetRoot == null)
            {
                if (m_FocusedElements.Count > 0)
                {
                    return m_FocusedElements[m_FocusedElements.Count - 1].m_FocusedElement;
                }
            }
            else
            {
                while (!retargetRoot.isCompositeRoot && retargetRoot.hierarchy.parent != null)
                {
                    retargetRoot = retargetRoot.hierarchy.parent;
                }

                foreach (var fe in m_FocusedElements)
                {
                    if (fe.m_SubTreeRoot == retargetRoot)
                    {
                        return fe.m_FocusedElement;
                    }
                }
            }

            return null;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Focusable GetLeafFocusedElement()
        {
            if (m_FocusedElements.Count > 0)
            {
                var result = m_FocusedElements[0].m_FocusedElement;
                return IsLocalElement(result) ? result : null;
            }
            return null;
        }

        private bool IsLocalElement(Focusable f)
        {
            // Case 1378840, case 1379300: if element leaves panel, its focus is invisible to its former panel.
            // Case 1401673: however if element immediately comes back to panel, it's focus becomes visible again.
            // This is needed to preserve focus when rebuilding PropertyEditor, which clears then re-adds everything.
            return f?.focusController == this;
        }

        private Focusable m_LastFocusedElement;

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal Focusable m_LastPendingFocusedElement;
        private int m_PendingFocusCount = 0;

        internal void ValidateInternalState(IPanel panel)
        {
            if (m_PendingFocusCount != 0 && !panel.dispatcher.processingEvents)
            {
                Debug.LogWarning("FocusController has unprocessed focus events. Clearing.");
                ClearPendingFocusEvents();
            }
        }


        internal void ClearPendingFocusEvents()
        {
            m_PendingFocusCount = 0;
            m_LastPendingFocusedElement = null;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool IsPendingFocus(Focusable f)
        {
            // Search for f in pending focused hierarchy
            var pending = m_LastPendingFocusedElement as VisualElement;
            while (pending != null)
            {
                if (f == pending)
                    return true;
                pending = pending.hierarchy.parent;
            }
            return false;
        }

        internal void SetFocusToLastFocusedElement()
        {
            if (m_LastFocusedElement != null && !(m_LastFocusedElement is IMGUIContainer))
                m_LastFocusedElement.Focus();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void BlurLastFocusedElement()
        {
            // Unselect selected TextElement when panel loses focus
            selectedTextElement = null;

            if (m_LastFocusedElement != null && !(m_LastFocusedElement is IMGUIContainer))
            {
                // Blur will change the lastFocusedElement to null
                var tmpLastFocusedElement = m_LastFocusedElement;
                m_LastFocusedElement.Blur();
                m_LastFocusedElement = tmpLastFocusedElement;
            }
        }

        // Do the focus change for real, no questions asked. This will not trigger any events.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void DoFocusChange(Focusable f)
        {
            m_FocusedElements.Clear();
            GetFocusTargets(f, m_FocusedElements);
        }

        internal void ProcessPendingFocusChange(Focusable f)
        {
            m_PendingFocusCount--;
            if (m_PendingFocusCount == 0)
                m_LastPendingFocusedElement = null;

            foreach (var element in m_FocusedElements)
                element.m_FocusedElement.pseudoStates &= ~PseudoStates.Focus;

            DoFocusChange(f);

            foreach (var element in m_FocusedElements)
                element.m_FocusedElement.pseudoStates |= PseudoStates.Focus;
        }

        private static void GetFocusTargets(Focusable f, List<FocusedElement> outTargets)
        {
            VisualElement ve = f as VisualElement;
            VisualElement subTreeRoot = ve;

            while (subTreeRoot != null)
            {
                if (subTreeRoot.hierarchy.parent == null || subTreeRoot.isCompositeRoot)
                {
                    outTargets.Add(new FocusedElement {m_SubTreeRoot = subTreeRoot, m_FocusedElement = ve});
                    ve = subTreeRoot;
                }
                subTreeRoot = subTreeRoot.hierarchy.parent;
            }
        }

        internal Focusable FocusNextInDirection(Focusable currentFocusable, FocusChangeDirection direction)
        {
            Focusable f = focusRing.GetNextFocusable(currentFocusable, direction);
            direction.ApplyTo(this, f);
            return f;
        }

        void AboutToReleaseFocus(Focusable focusable, Focusable willGiveFocusTo, FocusChangeDirection direction, DispatchMode dispatchMode)
        {
            using (FocusOutEvent e = FocusOutEvent.GetPooled(focusable, willGiveFocusTo, direction, this))
            {
                focusable.SendEvent(e, dispatchMode);
            }
        }

        void ReleaseFocus(Focusable focusable, Focusable willGiveFocusTo, FocusChangeDirection direction, DispatchMode dispatchMode)
        {
            using (ListPool<FocusedElement>.Get(out var focusedElements))
            {
                GetFocusTargets(focusable, focusedElements);
                foreach (var f in focusedElements)
                {
                    using BlurEvent e = BlurEvent.GetPooled(f.m_FocusedElement, willGiveFocusTo, direction, this);
                    e.target = f.m_FocusedElement;
                    focusable.SendEvent(e, dispatchMode);
                }
            }
        }

        void AboutToGrabFocus(Focusable focusable, Focusable willTakeFocusFrom, FocusChangeDirection direction, DispatchMode dispatchMode)
        {
            using (FocusInEvent e = FocusInEvent.GetPooled(focusable, willTakeFocusFrom, direction, this))
            {
                focusable.SendEvent(e, dispatchMode);
            }
        }

        void GrabFocus(Focusable focusable, Focusable willTakeFocusFrom, FocusChangeDirection direction, bool bIsFocusDelegated, DispatchMode dispatchMode)
        {
            using (ListPool<FocusedElement>.Get(out var focusedElements))
            {
                GetFocusTargets(focusable, focusedElements);
                foreach (var f in focusedElements)
                {
                    using FocusEvent e = FocusEvent.GetPooled(f.m_FocusedElement, willTakeFocusFrom, direction, this, bIsFocusDelegated);
                    e.target = f.m_FocusedElement;
                    focusable.SendEvent(e, dispatchMode);
                }
            }
        }

        internal void Blur(Focusable focusable, bool bIsFocusDelegated = false, DispatchMode dispatchMode = DispatchMode.Default)
        {
            var ownsFocus = m_PendingFocusCount > 0 ? IsPendingFocus(focusable) : IsFocused(focusable);
            if (ownsFocus)
            {
                SwitchFocus(null, bIsFocusDelegated, dispatchMode);
            }
        }

        internal void SwitchFocus(Focusable newFocusedElement, bool bIsFocusDelegated = false, DispatchMode dispatchMode = DispatchMode.Default)
        {
            SwitchFocus(newFocusedElement, FocusChangeDirection.unspecified, bIsFocusDelegated, dispatchMode);
        }

        internal void SwitchFocus(Focusable newFocusedElement, FocusChangeDirection direction, bool bIsFocusDelegated = false, DispatchMode dispatchMode = DispatchMode.Default)
        {
            m_LastFocusedElement = newFocusedElement;

            var oldFocusedElement = m_PendingFocusCount > 0 ? m_LastPendingFocusedElement : GetLeafFocusedElement();

            if (oldFocusedElement == newFocusedElement)
            {
                return;
            }

            if (!(newFocusedElement is VisualElement newFocusedVe) || !newFocusedElement.canGrabFocus || newFocusedVe.panel == null)
            {
                if (oldFocusedElement is VisualElement oldFocusedVe)
                {
                    m_LastPendingFocusedElement = null;
                    m_PendingFocusCount++; // ReleaseFocus will always trigger DoFocusChange

                    using (new EventDispatcherGate(oldFocusedVe.panel.dispatcher))
                    {
                        AboutToReleaseFocus(oldFocusedElement, null, direction, dispatchMode);
                        ReleaseFocus(oldFocusedElement, null, direction, dispatchMode);
                    }
                }
            }
            else if (newFocusedElement != oldFocusedElement)
            {
                // Retarget event.relatedTarget so it is in the same tree as event.target.
                var retargetedNewFocusedElement = (newFocusedElement as VisualElement)?.RetargetElement(oldFocusedElement as VisualElement) ?? newFocusedElement;
                var retargetedOldFocusedElement = (oldFocusedElement as VisualElement)?.RetargetElement(newFocusedElement as VisualElement) ?? oldFocusedElement;

                m_LastPendingFocusedElement = newFocusedElement;
                m_PendingFocusCount++; // GrabFocus will always trigger DoFocusChange, but ReleaseFocus won't

                using (new EventDispatcherGate(newFocusedVe.panel.dispatcher))
                {
                    if (oldFocusedElement != null)
                    {
                        AboutToReleaseFocus(oldFocusedElement, retargetedNewFocusedElement, direction, dispatchMode);
                    }

                    AboutToGrabFocus(newFocusedElement, retargetedOldFocusedElement, direction, dispatchMode);

                    if (oldFocusedElement != null)
                    {
                        // Since retargetedNewFocusedElement != null, so ReleaseFocus will not trigger DoFocusChange
                        ReleaseFocus(oldFocusedElement, retargetedNewFocusedElement, direction, dispatchMode);
                    }

                    GrabFocus(newFocusedElement, retargetedOldFocusedElement, direction, bIsFocusDelegated, dispatchMode);
                }
            }
        }

        internal void SwitchFocusOnEvent(Focusable currentFocusable, EventBase e)
        {
            if (e.processedByFocusController)
                return;

            using (FocusChangeDirection direction = focusRing.GetFocusChangeDirection(currentFocusable, e))
            {
                if (direction != FocusChangeDirection.none)
                {
                    FocusNextInDirection(currentFocusable, direction);
                    e.processedByFocusController = true;
                    // The new element does not have the focus until the series of focus events have been handled.
                }
            }
        }

        internal void ReevaluateFocus()
        {
            if (focusedElement is VisualElement currentFocus)
            {
                // If the currently focused element is not displayed in the hierarchy or not visible, blur it.
                if (!currentFocus.areAncestorsAndSelfDisplayed || !currentFocus.visible)
                    currentFocus.Blur();
            }
        }

        internal bool GetFocusableParentForPointerEvent(Focusable target, out Focusable effectiveTarget)
        {
            // The goal of this method is to simulate the fact that focus is applied across full parent hierarchy.
            // If a disabled element is clicked, it shouldn't be focused, but we should still allow its non-disabled
            // parent chain to receive focus if it doesn't already have it.
            // This is the way IMGUIContainer behaves, as mentioned in commit 59b2a9c05b781ea8291f7bbe5f133d35383dccc6
            // on unity/unity Github.
            // If target isn't focusable, then this is like clicking in the background, and we should unset focus.
            if (target == null || !target.focusable)
            {
                effectiveTarget = target;
                return target != null;
            }
            // If target is disabled, first enabled focusable parent will receive focus.
            effectiveTarget = target;
            while (effectiveTarget is VisualElement ve && (!ve.enabledInHierarchy || !ve.focusable || !ve.isEligibleToReceiveFocusFromDisabledChild) &&
                   ve.hierarchy.parent != null)
            {
                effectiveTarget = ve.hierarchy.parent;
            }
            // However, if previously focused element is a child of that parent, don't modify focus.
            return !IsFocused(effectiveTarget);
        }

        /// <summary>
        /// This property contains the actual keyboard id of the element being focused in the case of an IMGUIContainer
        /// </summary>
        internal int imguiKeyboardControl { get; set; }

        internal void SyncIMGUIFocus(int imguiKeyboardControlID, Focusable imguiContainerHavingKeyboardControl, bool forceSwitch)
        {
            imguiKeyboardControl = imguiKeyboardControlID;

            if (forceSwitch || imguiKeyboardControl != 0)
            {
                SwitchFocus(imguiContainerHavingKeyboardControl, FocusChangeDirection.unspecified);
            }
            else
            {
                SwitchFocus(null, FocusChangeDirection.unspecified);
            }
        }
    }
}
