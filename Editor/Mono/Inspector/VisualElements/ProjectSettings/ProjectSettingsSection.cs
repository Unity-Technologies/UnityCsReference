// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.ProjectSettings
{
    internal class ProjectSettingsSection : VisualElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] string label;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags label_UxmlAttributeFlags;
            #pragma warning restore 649
            public override object CreateInstance() => new ProjectSettingsSection();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(label_UxmlAttributeFlags))
                {
                    var e = (ProjectSettingsSection)obj;
                    e.label = label;
                }
            }
        }

        internal static class Styles
        {
            public static readonly string section = "project-settings-section";
            public static readonly string header = "project-settings-section__header";
            public static readonly string subheader = "project-settings-section__subheader";
            public static readonly string content = "project-settings-section__content";
        }

        public Label labelElement { get; private set; }

        public string label
        {
            get => labelElement?.text;
            set
            {
                if (labelElement == null && !string.IsNullOrEmpty(value))
                    CreateLabelElement(value);

                if (labelElement == null || labelElement.text == value)
                    return;

                labelElement.text = value;
            }
        }

        VisualElement m_ContentContainer;

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

            if (!string.IsNullOrEmpty(label))
                CreateLabelElement(label);

            m_ContentContainer = new VisualElement() { name = "unity-content-container" };
            m_ContentContainer.AddToClassList(Styles.content);
            hierarchy.Add(m_ContentContainer);

            m_ContentContainer.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            m_ContentContainer.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void CreateLabelElement(string newLabel)
        {
            labelElement = new Label(newLabel);
            labelElement.AddToClassList(Styles.header);
            hierarchy.Insert(0, labelElement);
        }

        static bool IsSubclassOfGeneric(Type genericType, Type typeToCheck) {
            while (typeToCheck != null && typeToCheck != typeof(object))
            {
                var currentType = typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck;
                if (genericType == currentType)
                    return true;
                typeToCheck = typeToCheck.BaseType;
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
                    IsSubclassOfGeneric(type, e.GetType()))
                .ForEach(e =>
                    e.EnableInClassList(BaseField<bool>.alignedFieldUssClassName, true));
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
        }
    }
}
