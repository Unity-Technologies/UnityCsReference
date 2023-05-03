// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal class ProjectSettingsSection : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ProjectSettingsSection, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Label = new() { name = "label" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ate = ve as ProjectSettingsSection;
                ate.label = m_Label.GetValueFromBag(bag, cc);
            }
        }

        internal static class Styles
        {
            public static readonly string section = "project-settings-section";
            public static readonly string title = "project-settings-section-title";
            public static readonly string label = "project-settings-section-label";
            public static readonly string content = "project-settings-section-content";
        }

        public Label labelElement { get; private set; }

        public string label
        {
            get => labelElement.text;
            set
            {
                if (labelElement.text == value) return;
                labelElement.text = value;
            }
        }

        private VisualElement m_ContentContainer;

        /// <summary>
        /// Contains full content, potentially partially visible.
        /// </summary>
        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return m_ContentContainer; }
        }

        // Custom controls need a default constructor. This default constructor calls the other constructor in this
        // class.
        public ProjectSettingsSection() : this(null)
        {
        }

        // This constructor allows users to set the contents of the label.
        public ProjectSettingsSection(string label)
        {
            AddToClassList(Styles.section);
            AddToClassList(InspectorElement.ussClassName);

            labelElement = new Label($"I am the render pipeline");
            labelElement.AddToClassList(Styles.title);
            labelElement.AddToClassList(Styles.label);
            hierarchy.Add(labelElement);

            m_ContentContainer = new VisualElement() { name = "unity-content-container" };
            m_ContentContainer.AddToClassList(Styles.content);
            hierarchy.Add(m_ContentContainer);

            m_ContentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ContentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck) {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ?
                    toCheck.GetGenericTypeDefinition() :
                    toCheck;
                if (generic == cur)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel == null)
                return;

            var type = typeof(BaseField<>);
            evt.elementTarget.Query<BindableElement>()
                .Where(e =>
                    IsSubclassOfRawGeneric(type, e.GetType()))
                .ForEach(e =>
                    e.EnableInClassList(BaseField<bool>.alignedFieldUssClassName, true));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
        }
    }
}
