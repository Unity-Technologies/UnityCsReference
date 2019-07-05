// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal class ElementSelection
    {
        private IPackageSelection Element { get; set; }
        private Selection Selection { get; set; }

        public ElementSelection(IPackageSelection element, Selection selection)
        {
            Element = element;
            Selection = selection;
            Element.RefreshSelection();

            Selection.OnChanged += OnChanged;
        }

        public void OnChanged(IEnumerable<PackageVersion> selection)
        {
            Element.RefreshSelection();
        }
    }
}
