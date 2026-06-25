// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal partial class OverrideRow : VisualElement
    {
        internal static readonly string ussClassName = "unity-override-row";
        internal static readonly string isOverriddenUssClassName = ussClassName + "--overridden";

        internal static CustomStyleProperty<Color> s_OverrideBarColorProperty = new CustomStyleProperty<Color>("--unity-override-bar-color");

        private readonly OverrideBarManipulator m_OverrideBarManipulator;

        private VisualElement m_OverrideContainer;

        public Color m_OverrideBarColor;
        public bool m_OverrideBarColorIsInline = false;

        internal OverrideBarManipulator overrideBarManipulator => m_OverrideBarManipulator;

        public Color OverrideBarColor
        {
            get => m_OverrideBarColor;
            set
            {
                SetOverrideBarColor(value, true);
            }
        }

        void SetOverrideBarColor(Color color, bool isInline)
        {
            if (m_OverrideBarColor == color && m_OverrideBarColorIsInline == isInline)
                return;
            m_OverrideBarColor = color;
            m_OverrideBarColorIsInline = isInline;
            UpdateManipulatorColor();
            MarkDirtyRepaint();
        }

        void UpdateManipulatorColor()
        {
            m_OverrideBarManipulator.Color = m_OverrideBarColor;
        }

        protected VisualElement overrideContainer
        {
            get => m_OverrideContainer;
            set
            {
                if (m_OverrideContainer == value)
                    return;
                m_OverrideContainer = value;
                m_OverrideBarManipulator.target = value;
            }
        }

        internal HashSet<string> trackedProperties;

        protected internal virtual string GetIsOverriddenClassName() => isOverriddenUssClassName;

        public bool IsOverridden => m_OverrideBarManipulator.IsOverridden;

        public OverrideRow()
        {
            trackedProperties = HashSetPool<string>.Get();
            RegisterCallback<TrackPropertyEvent>(Callback, TrickleDown.NoTrickleDown);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            m_OverrideBarManipulator = new OverrideBarManipulator();
            overrideContainer = this;
            m_OverrideBarColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            UpdateManipulatorColor();
        }

        [EventInterest(typeof(AttachToPanelEvent), typeof(DetachFromPanelEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            switch (evt)
            {
                case AttachToPanelEvent attachToPanelEvent:
                {
                    if (attachToPanelEvent.destinationPanel == null)
                        return;

                    trackedProperties ??= HashSetPool<string>.Get();

                    break;
                }
                case DetachFromPanelEvent detachFromPanelEvent:
                {
                    if (detachFromPanelEvent.originPanel == null)
                        return;

                    if (trackedProperties != null)
                    {
                        HashSetPool<string>.Release(trackedProperties);
                        trackedProperties = null;
                    }
                    break;
                }
            }
            base.HandleEventBubbleUp(evt);
        }

        void EnsureTrackedPropertiesAllocated()
        {
            trackedProperties ??= HashSetPool<string>.Get();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            // Ignore if the override bar color was set inline
            if (m_OverrideBarColorIsInline)
                return;

            if (e.customStyle.TryGetValue(s_OverrideBarColorProperty, out Color color))
            {
                SetOverrideBarColor(color, false);
            }
        }

        private void Track(ITrackablePropertyProvider provider, string propertyName = null)
        {
            if (propertyName != null)
            {
                EnsureTrackedPropertiesAllocated();
                trackedProperties.Add(propertyName);
            }

            provider.OnTrackedPropertyChanged += OnTrackedPropertyChanged;
            provider.OnTrackedPropertySourceChanged += OnTrackedPropertySourceChanged;
        }

        private void Untrack(ITrackablePropertyProvider provider, string propertyName = null)
        {
            if (propertyName != null)
                trackedProperties?.Remove(propertyName);
            RemoveFromTrackedProperties(provider, propertyName);
            provider.OnTrackedPropertyChanged -= OnTrackedPropertyChanged;
            provider.OnTrackedPropertySourceChanged -= OnTrackedPropertySourceChanged;
        }

        protected virtual void ForwardDependentPropertiesTracking(TrackPropertyEvent evt)
        {
        }

        private void Callback(TrackPropertyEvent evt)
        {
            if (evt.target == this)
                ForwardDependentPropertiesTracking(evt);
            if (evt.isImmediatePropagationStopped)
                return;
            Track(evt.provider, evt.propertyName);
        }

        Dictionary<string, HashSet<ITrackablePropertyProvider>> m_TrackedProviders = new();

        void OnTrackedPropertyChanged(ITrackablePropertyProvider provider, string propertyName, TrackedPropertyType type)
        {
            switch (type)
            {
                case TrackedPropertyType.StopTracking:
                    Untrack(provider, propertyName);
                    break;
                case TrackedPropertyType.MarkOverride:
                    AddToTrackedProperties(provider, propertyName);
                    break;
                case TrackedPropertyType.ClearOverride:
                    RemoveFromTrackedProperties(provider, propertyName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Add property to tracked properties. This is needed to support search.
        /// </summary>
        /// <param name="propertyName">The name of the property to track.</param>
        public void AddTrackedProperty(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                EnsureTrackedPropertiesAllocated();
                trackedProperties.Add(propertyName);
            }
        }

        void AddToTrackedProperties(ITrackablePropertyProvider provider, string propertyName)
        {
            if (!m_TrackedProviders.TryGetValue(propertyName, out var providerSet))
                m_TrackedProviders[propertyName] = providerSet = new HashSet<ITrackablePropertyProvider>();

            providerSet.Add(provider);
            UpdateOverriddenState();
        }

        void RemoveFromTrackedProperties(ITrackablePropertyProvider provider, string propertyName)
        {
            if (!m_TrackedProviders.TryGetValue(propertyName, out var providerSet))
                return;

            providerSet.Remove(provider);

            if (providerSet.Count == 0)
                m_TrackedProviders.Remove(propertyName);

            UpdateOverriddenState();
        }

        void UpdateOverriddenState()
        {
            var isOverridden = m_TrackedProviders.Count > 0;
            m_OverrideBarManipulator.IsOverridden = isOverridden;
            EnableInClassList(GetIsOverriddenClassName(), isOverridden);
        }

        protected virtual void OnTrackedPropertySourceChanged(ITrackablePropertyProvider provider, string propertyName, bool hasVariable, bool hasBinding, bool isAnimationDriven)
        {
            EnableInClassList(StylePropertyBinding.k_VariableFieldUssClassName, hasVariable);
            EnableInClassList(StylePropertyBinding.k_BoundFieldUssClassName, hasBinding);
            EnableInClassList(UxmlAttributeFieldDecorator.s_BoundFieldUssClassName, hasBinding);
            EnableInClassList(StylePropertyBinding.k_AnimationDrivenFieldUssClassName, isAnimationDriven);
        }

        internal void UpdateTrackedProperties(IReadOnlyList<string> propertyNames)
        {
            m_TrackedProviders.Clear();
            for (var i = 0; i < propertyNames.Count; i++)
                m_TrackedProviders[propertyNames[i]] = new HashSet<ITrackablePropertyProvider>();
            UpdateOverriddenState();
        }

        internal bool HasMatchingOverriddenProperty(string searchText)
        {
            if (trackedProperties == null)
                return false;

            foreach (var property in trackedProperties)
            {
                if (property.Contains(searchText, StringComparison.OrdinalIgnoreCase) &&
                    m_TrackedProviders.ContainsKey(property))
                    return true;
            }

            return false;
        }
    }
}
