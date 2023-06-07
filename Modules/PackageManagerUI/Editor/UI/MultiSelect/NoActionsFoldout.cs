// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class NoActionsFoldout : MultiSelectFoldout
    {
        public NoActionsFoldout(PageManager pageManager)
            : base(new DeselectAction(pageManager, "deselectNoAction"))
        {
            headerTextTemplate = L10n.Tr("No common action available for {0}");
        }
    }
}
