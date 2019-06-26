// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageToolbar> {}

        public PackageToolbar()
        {
            var root = Resources.GetTemplate("PackageToolbar.uxml");
            Add(root);
        }
    }
}
