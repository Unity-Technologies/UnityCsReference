// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
