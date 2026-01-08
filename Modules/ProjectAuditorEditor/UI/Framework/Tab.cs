// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Core;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal enum TabId
    {
        Summary,
        Code,
        Assets,
        Shaders,
        Settings,
        Build,
        GameObjects,
    }

    [Serializable]
    internal class Tab
    {
        public TabId id;
        public string name;

        public SerializableEnum<IssueCategory>[] categories;

        public int currentCategoryIndex;
        public Utility.DropdownItem[] dropdown;
    }
}
