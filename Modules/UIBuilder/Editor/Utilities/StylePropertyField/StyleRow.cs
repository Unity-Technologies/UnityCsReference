// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class StyleRowBinding : CustomBinding
    {
        internal static readonly string ussClassName = "style-property-row";
        internal static readonly string overrideBarUssClassName = ussClassName + "__override-bar";

        private StyleRow m_StyleRow;
        private IReadOnlyList<string> m_StyleProperties => m_StyleRow.trackedProperties;

        public StyleRowBinding(StyleRow styleRow)
        {
            m_StyleRow = styleRow;
            updateTrigger = BindingUpdateTrigger.OnSourceChanged;
        }

        protected internal override BindingResult Update(in BindingContext context)
        {
            var target = context.targetElement as StyleRow;
            if (null == target)
                return base.Update(in context);

            if (m_StyleProperties.Count == 0)
                return new BindingResult(BindingStatus.Failure, "No styles to track.");

            if (context.dataSource is not StyleDiff styleDiff)
                return new BindingResult(BindingStatus.Failure, "Expected style diff as a data source.");

            for (var i = 0; i < m_StyleProperties.Count; ++i)
            {
                if (!styleDiff.HasUxmlOverrides(m_StyleProperties[i]))
                    continue;
                m_StyleRow.SetIsOverridden(true);
                return default;
            }
            m_StyleRow.SetIsOverridden(false);
            return default;
        }
    }

    internal class StyleRow : VisualElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new StyleRow();
        }

        internal static readonly string s_StylePropertyBindingId = "__style-property-tracker";

        private VisualElement m_Container;
        protected virtual VisualElement overrideContainer { get; set; }
        public override VisualElement contentContainer => m_Container;

        private StyleRowBinding m_RowBinding;
        private bool m_IsRegistered;
        private bool m_IsOverridden;
        private readonly Dictionary<string, int> m_TrackedProperties;
        public readonly List<string> trackedProperties;


        public StyleRow()
        {
            m_RowBinding = new StyleRowBinding(this);
            m_TrackedProperties = new Dictionary<string, int>();
            trackedProperties = new List<string>();

            RegisterCallback<TrackStylePropertyEvent>(Callback);
            m_IsRegistered = false;

            m_Container = new VisualElement();
            m_Container.name = "content-container";
            hierarchy.Add(m_Container);
            overrideContainer = this;
            generateVisualContent += DrawOverrideBar;
        }

        public void SetIsOverridden(bool isOverridden)
        {
            if (m_IsOverridden != isOverridden)
            {
                m_IsOverridden = isOverridden;
                MarkDirtyRepaint();
            }
        }

        private void DrawOverrideBar(MeshGenerationContext mgc)
        {
            if (!m_IsOverridden)
                return;

            var targetParent = GetFirstMatchingClass(StyleRowBinding.overrideBarUssClassName);
            if (targetParent == null)
                return;

            var painter = mgc.painter2D;
            painter.strokeColor = Color.white;
            painter.lineWidth = 4;
            painter.BeginPath();

            // Get global position of both elements
            var xOffset = targetParent.LocalToWorld(Vector2.zero).x - this.LocalToWorld(Vector2.zero).x;
            var yEnd = overrideContainer.resolvedStyle.height;

            painter.MoveTo(new Vector2(xOffset, 0));
            painter.LineTo(new Vector2(xOffset, yEnd + 2));

            painter.Stroke();
        }

        private VisualElement GetFirstMatchingClass(string className)
        {
            VisualElement parentElement = this;
            while (parentElement != null)
            {
                if (parentElement.ClassListContains(className))
                    return parentElement;
                parentElement = parentElement.parent;
            }

            return null;
        }

        public void Track(string propertyName)
        {
            if (m_TrackedProperties.TryGetValue(propertyName, out var count))
                m_TrackedProperties[propertyName] = count + 1;
            else
            {
                m_TrackedProperties[propertyName] = 1;
                trackedProperties.Add(propertyName);
                if (!m_IsRegistered)
                {
                    m_IsRegistered = true;
                    SetBinding(s_StylePropertyBindingId, m_RowBinding);
                }
            }
        }

        public void Untrack(string propertyName)
        {
            if (m_TrackedProperties.TryGetValue(propertyName, out var count))
            {
                if (count - 1 <= 0)
                {
                    m_TrackedProperties.Remove(propertyName);
                    trackedProperties.Remove(propertyName);
                    if (m_IsRegistered)
                    {
                        m_IsRegistered = false;
                        ClearBinding(s_StylePropertyBindingId);
                    }
                }
                else
                    m_TrackedProperties[propertyName] = count - 1;
            }
        }

        private void Callback(TrackStylePropertyEvent evt)
        {
            switch (evt.type)
            {
                case StylePropertyTrackingType.Register:
                {
                    Track(evt.propertyName);
                }
                    break;
                case StylePropertyTrackingType.Unregister:
                {
                    Untrack(evt.propertyName);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
