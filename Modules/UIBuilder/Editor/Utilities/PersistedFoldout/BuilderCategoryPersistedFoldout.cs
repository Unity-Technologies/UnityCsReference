// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    internal partial class BuilderCategoryPersistedFoldout : PersistedFoldout
    {
        public BuilderCategoryPersistedFoldout()
        {
            var bindingIndicator = new VisualElement()
            {
                tooltip = L10n.Tr(BuilderConstants.FoldoutContainsBindingsString)
            };

            bindingIndicator.AddToClassList(BuilderConstants.InspectorBindingIndicatorClassName);
            m_Toggle.visualInput.Add(bindingIndicator);
        }
    }
}
