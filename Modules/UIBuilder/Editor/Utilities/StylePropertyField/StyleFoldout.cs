// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class StyleFoldout : StyleRow
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleRow.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] string text;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags text_UxmlAttributeFlags;
            [SerializeField] bool isCategory;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags isCategory_UxmlAttributeFlags;
            #pragma warning restore 649

            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(text), "text"),
                    new (nameof(isCategory), "is-category"),
                }, true);
            }

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);
                var e = (StyleFoldout)obj;

                if (ShouldWriteAttributeValue(text_UxmlAttributeFlags))
                    e.text = text;
                if (ShouldWriteAttributeValue(isCategory_UxmlAttributeFlags))
                    e.isCategory = isCategory;
            }

            public override object CreateInstance() => new StyleFoldout();
        }

        internal static readonly BindingId textProperty = nameof(text);
        internal static readonly BindingId isCategoryProperty = nameof(isCategory);

        internal static readonly string ussClassName = "style-property-foldout";
        internal static readonly string headerUssClassName = ussClassName + "__header";
        internal static readonly string contentContainerUssClassName = ussClassName + "__content-container";
        internal static readonly string categoryUssClassName = ussClassName + "__category";
        internal static readonly string viewDataKeyName = ussClassName + "__view-data-key";

        private PersistedFoldout m_Foldout;
        private bool m_IsCategory;

        [SerializeField, DontCreateProperty]
        private bool m_Value;

        public override VisualElement contentContainer { get; }

        public PersistedFoldout foldout => m_Foldout;

        [UxmlAttribute]
        [CreateProperty]
        public string text
        {
            get => m_Foldout.text;
            set
            {
                m_Foldout.text = value;
                NotifyPropertyChanged(nameof(text));
            }
        }

        [UxmlAttribute]
        [CreateProperty]
        public bool isCategory
        {
            get => m_IsCategory;
            set
            {
                m_IsCategory = value;
                m_Foldout.toggle.EnableInClassList(categoryUssClassName, value);
                contentContainer.EnableInClassList(categoryUssClassName, value);
                EnableInClassList(categoryUssClassName, value);
                NotifyPropertyChanged(nameof(isCategory));
            }
        }

        public StyleFoldout()
            : this(null)
        {
        }

        public StyleFoldout(string text)
        {
            AddToClassList(ussClassName);

            m_Foldout = new PersistedFoldout
            {
                text = text,
                value = false,
                viewDataKey = viewDataKeyName
            };

            foldout.header.AddToClassList(headerUssClassName);

            contentContainer = m_Foldout.contentContainer;
            contentContainer.AddToClassList(contentContainerUssClassName);

            hierarchy.Add(m_Foldout);

            m_Foldout.RegisterValueChangedCallback(evt =>
            {
                m_Value = evt.newValue;
                SaveViewData();
            });

            overrideContainer = foldout.header;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            var key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);
            m_Foldout.SetValueWithoutNotify(m_Value);
        }
    }
}
