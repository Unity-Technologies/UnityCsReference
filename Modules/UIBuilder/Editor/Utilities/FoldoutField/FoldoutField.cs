// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class FoldoutField : PersistedFoldout
    {
        [Serializable]
        public new class UxmlSerializedData : PersistedFoldout.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(bindingPaths), "binding-paths")
                });
            }

            #pragma warning disable 649
            [SerializeField] string bindingPaths;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags bindingPaths_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new FoldoutField();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(bindingPaths_UxmlAttributeFlags))
                {
                    var e = (FoldoutField)obj;
                    e.bindingPathArray = bindingPaths.Split(' ');
                    e.ReAssignTooltipToHeaderLabel();
                }
            }
        }

        protected string[] m_BindingPathArray;

        public string[] bindingPathArray
        {
            get
            {
                return m_BindingPathArray;
            }
            set
            {
                m_BindingPathArray = value;
            }
        }

        public FoldoutField()
        {
            m_Value = true;
            AddToClassList(BuilderConstants.FoldoutFieldPropertyName);
            header.AddToClassList(BuilderConstants.FoldoutFieldHeaderClassName);

            var bindingIndicator = new VisualElement();
            bindingIndicator.AddToClassList(BuilderConstants.InspectorBindingIndicatorClassName);
            bindingIndicator.tooltip = L10n.Tr(BuilderConstants.FoldoutContainsBindingsString);
            m_Toggle.visualInput.Insert(0, bindingIndicator);
        }

        public virtual void UpdateFromChildFields() {}
        internal virtual void SetHeaderInputEnabled(bool enabled) {}
    }
}
