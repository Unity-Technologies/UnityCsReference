using UnityEditor;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderVisualTreeAssetImporter : UXMLImporterImpl
    {
        public BuilderVisualTreeAssetImporter()
        {

        }

        public override UnityEngine.Object DeclareDependencyAndLoad(string path)
        {
            return BuilderPackageUtilities.LoadAssetAtPath<UnityEngine.Object>(path);
        }
    }
}
