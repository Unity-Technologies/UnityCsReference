// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    enum DockingHint
    {
        None,
        DockedBefore,
        DockedAfter
    }

    class OverlayContainer : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(horizontal), "horizontal"),
                    new(nameof(supportedOverlayLayout), "supported-overlay-layout"),
                }, true);
            }

#pragma warning disable 649
            [SerializeField] string supportedOverlayLayout;
            [SerializeField] bool horizontal;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags horizontal_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags supportedOverlayLayout_UxmlAttributeFlags;
#pragma warning restore 649

            public override object CreateInstance() => new OverlayContainer();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (OverlayContainer)obj;
                if (ShouldWriteAttributeValue(horizontal_UxmlAttributeFlags))
                    e.isHorizontal = horizontal;

                e.m_SupportedOverlayLayouts = Layout.Panel;
                if (ShouldWriteAttributeValue(supportedOverlayLayout_UxmlAttributeFlags))
                {
                    var split = supportedOverlayLayout?.Split(' ');
                    if (split?.Length > 0)
                    {
                        foreach (var layout in split)
                        {
                            switch (layout.ToLower())
                            {
                                case "horizontal":
                                    e.m_SupportedOverlayLayouts |= Layout.HorizontalToolbar;
                                    break;

                                case "vertical":
                                    e.m_SupportedOverlayLayouts |= Layout.VerticalToolbar;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        public const string className = "unity-overlay-container";
        public const string horizontalClassName = className + "-horizontal";
        public const string verticalClassName = className + "-vertical";
        const string k_ContentClassName = className + "__content";
        const string k_BeforeClassName = className + "__before-spacer-container";
        const string k_AfterClassName = className + "__after-spacer-container";
        const string k_SpacingContainerClassName = className + "__spacing-container";

        readonly List<ContainerSection> m_Sections = new List<ContainerSection>();

        // This is set by querying the stylesheet for 'vertical' and 'horizontal'
        Layout m_SupportedOverlayLayouts = 0; //Used as a flag in this case
        bool m_IsHorizontal;

        public OverlayCanvas canvas { get; internal set; }

        public int sectionCount => m_Sections.Count;

        public int overlayCount
        {
            get
            {
                int count = 0;
                foreach (var section in m_Sections)
                    count += section.overlayCount;

                return count;
            }
        }

        public virtual Layout preferredLayout => Layout.Panel;
        public virtual bool resizingAllowed => true;

        public bool isHorizontal
        {
            get => m_IsHorizontal;
            set
            {
                if (m_IsHorizontal == value)
                    return;

                m_IsHorizontal = value;
                if (m_IsHorizontal)
                    SetHorizontal();
                else
                    SetVertical();
            }
        }

        public float spacerSize
        {
            get
            {
                float sectionSizes = 0;
                foreach (var section in m_Sections)
                    sectionSizes += isHorizontal ? section.layout.width : section.layout.height;

                return (isHorizontal ? layout.width : layout.height) - sectionSizes;
            }
        }

        public OverlayContainer()
        {
            AddToClassList(className);
            name = className;
            SetVertical();
        }

        protected virtual void SetHorizontal()
        {
            EnableInClassList(horizontalClassName, true);
            EnableInClassList(verticalClassName, false);
        }

        protected virtual void SetVertical()
        {
            EnableInClassList(horizontalClassName, false);
            EnableInClassList(verticalClassName, true);
        }

        protected void CreateDefaultSections(out ContainerSection beforeSection, out ContainerSection afterSection)
        {
            beforeSection = CreateSection();
            beforeSection.AddToClassList(k_BeforeClassName);

            afterSection = CreateSection();
            afterSection.AddToClassList(k_AfterClassName);
        }


        protected ContainerSection<TData> CreateSection<TData>() where TData : struct
        {
            var section = new ContainerSection<TData>();
            section.AddToClassList(k_SpacingContainerClassName);
            section.contentContainer.AddToClassList(k_ContentClassName);
            m_Sections.Add(section);
            Add(section);
            return section;
        }

        protected ContainerSection CreateSection()
        {
            var section = new ContainerSection();
            section.AddToClassList(k_SpacingContainerClassName);
            section.contentContainer.AddToClassList(k_ContentClassName);
            m_Sections.Add(section);
            Add(section);
            return section;
        }

        public ContainerSection GetContainerSection(int index)
        {
            return m_Sections[index];
        }

        public ContainerSection GetContainerSection(OverlayContainerSection section)
        {
            if (m_Sections.Count == 0)
                return null;

            var index = Mathf.Min((int)section, m_Sections.Count - 1);
            return GetContainerSection(index);
        }

        public void InsertOverlay(Overlay overlay, OverlayContainerSection section, int index, DockingHint hint)
        {
            GetContainerSection(section).InsertOverlay(overlay, index, hint);
        }

        public bool GetOverlayHierarchyIndex(Overlay overlay, int sectionIndex, out int index)
        {
            index = m_Sections[sectionIndex].GetOverlayHierarchyIndex(overlay);
            return index >= 0;
        }

        public bool GetOverlayIndex(Overlay overlay, out int sectionIndex, out int index)
        {
            sectionIndex = -1;
            index = -1;
            for (int i = 0; i < m_Sections.Count; ++i)
            {
                index = m_Sections[i].GetOverlayIndex(overlay);
                if (index >= 0)
                {
                    sectionIndex = i;
                    return true;
                }
            }

            return false;
        }

        public bool GetOverlayIndex(Overlay overlay, out OverlayContainerSection section, out int index)
        {
            bool result = GetOverlayIndex(overlay, out int sectionIndex, out index);
            section = (OverlayContainerSection)sectionIndex;
            return result;
        }

        public bool RemoveOverlay(Overlay overlay)
        {
            foreach (var section in m_Sections)
                if (section.RemoveOverlay(overlay))
                    return true;

            return false;
        }

        public bool ContainsOverlay(Overlay overlay)
        {
            foreach (var section in m_Sections)
                if (section.ContainsOverlay(overlay))
                    return true;

            return false;
        }

        public bool ContainsOverlay(Overlay overlay, OverlayContainerSection section)
        {
            var sectionElement = GetContainerSection(section);
            if (sectionElement == null)
                return false;

            return sectionElement.ContainsOverlay(overlay);
        }

        public bool HasVisibleOverlays()
        {
            foreach (var section in m_Sections)
                if (section.HasVisibleOverlays())
                    return true;

            return false;
        }

        public virtual bool IsOverlayLayoutSupported(Layout requested)
        {
            return (m_SupportedOverlayLayouts & requested) > 0;
        }

        internal virtual IEnumerable<OverlayDropZoneBase> GetDropZones()
        {
            return new OverlayDropZoneBase[0];
        }
    }
}
