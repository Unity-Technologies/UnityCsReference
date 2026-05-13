// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    internal partial class FoldoutField : PersistedFoldout
    {
        [UxmlAttribute]
        internal string bindingPaths
        {
            get => bindingPathArray == null ? string.Empty : string.Join(" ", bindingPathArray);
            set => bindingPathArray = string.IsNullOrEmpty(value) ? null : value.Split(' ');
        }

        public string[] bindingPathArray { get; set; }

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
