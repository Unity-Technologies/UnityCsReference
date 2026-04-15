// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal class OverrideRow : VisualElement
    {
        internal static readonly string ussClassName = "unity-override-row";
        internal static readonly string isOverriddenUssClassName = ussClassName + "--overridden";

        internal static CustomStyleProperty<Color> s_OverrideBarColorProperty = new CustomStyleProperty<Color>("--unity-override-bar-color");

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new OverrideRow();
        }

        private readonly OverrideBarManipulator m_OverrideBarManipulator;

        private VisualElement m_OverrideContainer;

        public Color m_OverrideBarColor;
        public bool m_OverrideBarColorIsInline = false;

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

        protected virtual string GetIsOverriddenClassName() => isOverriddenUssClassName;

        public bool IsOverridden => m_OverrideBarManipulator.IsOverridden;

        public OverrideRow()
        {
            RegisterCallback<TrackPropertyEvent>(Callback, TrickleDown.NoTrickleDown);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            m_OverrideBarManipulator = new OverrideBarManipulator();
            overrideContainer = this;
            m_OverrideBarColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            UpdateManipulatorColor();
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
            provider.OnTrackedPropertyChanged += OnTrackedPropertyChanged;
        }

        private void Untrack(ITrackablePropertyProvider provider, string propertyName = null)
        {
            RemoveFromTrackedProperties(provider, propertyName);
            provider.OnTrackedPropertyChanged -= OnTrackedPropertyChanged;
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
            Track(evt.provider);
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
    }
}
