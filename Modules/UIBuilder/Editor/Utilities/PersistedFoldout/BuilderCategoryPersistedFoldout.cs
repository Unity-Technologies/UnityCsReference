// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
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
                tooltip = L10n.Tr(BuilderConstants.FoldoutContainsBindingsString, null)
            };

            bindingIndicator.AddToClassList(BuilderConstants.InspectorBindingIndicatorClassName);
            m_Toggle.visualInput.Add(bindingIndicator);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
