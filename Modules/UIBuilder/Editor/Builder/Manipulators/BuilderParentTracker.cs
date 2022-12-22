// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderParentTracker : BuilderTracker
    {
        static readonly string s_UssClassName = "unity-builder-parent-tracker";

        public new class UxmlFactory : UxmlFactory<BuilderParentTracker, UxmlTraits> {}

        public BuilderParentTracker()
        {
            AddToClassList(s_UssClassName);
        }
    }
}
