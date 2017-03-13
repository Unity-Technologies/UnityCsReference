// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.U2D.Interface
{
    internal interface IAssetDatabase
    {
        string GetAssetPath(Object o);
        ITextureImporter GetAssetImporterFromPath(string path);
    }

    internal class AssetDatabaseSystem : IAssetDatabase
    {
        public string GetAssetPath(Object o)
        {
            return UnityEditor.AssetDatabase.GetAssetPath(o);
        }

        public ITextureImporter GetAssetImporterFromPath(string path)
        {
            UnityEditor.AssetImporter ai = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            return ai == null ? null : new TextureImporter((UnityEditor.TextureImporter)ai);
        }
    }
}
