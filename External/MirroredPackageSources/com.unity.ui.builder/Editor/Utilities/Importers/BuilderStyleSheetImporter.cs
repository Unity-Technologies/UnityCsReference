using UnityEditor;

using UnityEditor.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetImporter : StyleSheetImporterImpl
    {
        public BuilderStyleSheetImporter()
        {
        }

        public override UnityEngine.Object DeclareDependencyAndLoad(string path)
        {
            return BuilderPackageUtilities.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }
}
