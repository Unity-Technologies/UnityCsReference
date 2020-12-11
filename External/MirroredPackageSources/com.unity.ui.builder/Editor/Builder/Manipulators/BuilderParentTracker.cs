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
