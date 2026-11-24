// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.ShaderApiReflection;
using UnityEngine;

namespace UnityEditor
{
    public partial class ShaderUtil
    {
        [ShouldBePublic]
        internal static ShaderInterfaceReflection GetShaderInterfaceReflection(Shader s)
        {
            if (s == null)
                return null;

            string assetPath = AssetDatabase.GetAssetPath(s);
            if (string.IsNullOrEmpty(assetPath))
                return null;

            foreach (var assetObject in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (assetObject is ShaderInterfaceReflection reflectionObject)
                    return reflectionObject;
            }
            return null;
        }

        [ShouldBePublic]
        internal readonly struct ShaderInterfaceArtifacts
        {
            public readonly TextAsset GeneratedShaderLabText;
            public readonly ShaderInterfaceReflection Reflection;

            internal ShaderInterfaceArtifacts(TextAsset splicedText, ShaderInterfaceReflection reflection)
            {
                GeneratedShaderLabText = splicedText;
                Reflection = reflection;
            }
        }

        [ShouldBePublic]
        internal static Shader CreateShaderAsset(
            AssetImportContext context,
            string source,
            out ShaderInterfaceArtifacts artifacts,
            bool compileInitialShaderVariants = true)
        {
            MarshalledShaderInterfaceObjects marshalled =
                CreateShaderInterfaceAsset(context, source, compileInitialShaderVariants);
            artifacts = new ShaderInterfaceArtifacts(marshalled.m_SplicedText, marshalled.m_Reflection);
            return marshalled.m_Shader;
        }
    }
}
