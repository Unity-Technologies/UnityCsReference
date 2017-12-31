// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor.U2D.Interface
{
    internal interface IAssetDatabase
    {
        string GetAssetPath(Object o);
        AssetImporter GetAssetImporterFromPath(string path);
    }

    internal class AssetDatabaseSystem : IAssetDatabase
    {
        public string GetAssetPath(Object o)
        {
            return UnityEditor.AssetDatabase.GetAssetPath(o);
        }

        public AssetImporter GetAssetImporterFromPath(string path)
        {
            return UnityEditor.AssetImporter.GetAtPath(path);
        }
    }
}
