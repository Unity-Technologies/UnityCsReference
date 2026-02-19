// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/AssetPipeline/ShaderIncludeImporter.h")]
    internal sealed partial class ShaderIncludeImporter : AssetImporter
    {
    }

    [NativeHeader("Editor/Src/Shaders/ShaderInclude.h")]
    public sealed partial class ShaderInclude : TextAsset
    {
        public ShaderApiReflection.ShaderIncludeReflection Reflection => GetReflection();

        private ShaderApiReflection.ShaderIncludeReflection GetReflection()
        {
            string assetPath = AssetDatabase.GetAssetPath(this);

            // Because the reflection object isn't exposed in the hierarchy, we have to manually search
            // all objects at this path.
            foreach (Object assetObject in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (assetObject is ShaderApiReflection.ShaderIncludeReflection reflectionObject)
                    return reflectionObject;
            }
            return null;
        }
    }

    [CustomEditor(typeof(ShaderInclude))]
    internal sealed class ShaderIncludeEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return ShaderApiReflection.ShaderIncludeEditorExtensions.CreateInspectorGUI(target);
        }
    }
}
