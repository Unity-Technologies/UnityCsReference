// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class NodeModeDropdown : DropdownField
    {
        public NodeModeDropdown(string ussClassName)
        {
            ClearClassList();
            AddToClassList(ussClassName.WithUssElement("mode-dropdown"));

            foreach (var child in Children())
            {
                child.ClearClassList();
                child.AddToClassList(ussClassName.WithUssElement("mode-container"));
                return; // Accessing first child without LinQ
            }
        }
    }
}
