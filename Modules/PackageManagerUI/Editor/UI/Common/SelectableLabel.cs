// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SelectableLabel : Label
    {
        internal new class UxmlFactory : UxmlFactory<SelectableLabel, UxmlTraits>
        {
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var result = base.Create(bag, cc) as SelectableLabel;
                result.SetAsSelectableAndElided();
                return result;
            }
        }

        public SelectableLabel()
        {
            SetAsSelectableAndElided();
        }

        public void SetAsSelectableAndElided()
        {
            selection.isSelectable = true;
            focusable = true;
            displayTooltipWhenElided = true;
        }

        public void SetValueWithoutNotify(string value)
        {
            text = value;
        }

        public bool multiline { get; set; }

        public string value
        {
            get => text;
            set => text = value;
        }
    }
}
