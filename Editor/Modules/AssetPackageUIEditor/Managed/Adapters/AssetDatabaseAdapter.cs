// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.AssetPackage;

internal interface IAssetDatabaseAdapter
{
    internal string[] CollectAllChildren(string guid, string[] collection);
}

internal class AssetDatabaseAdapter : IAssetDatabaseAdapter
{
    public string[] CollectAllChildren(string guid, string[] collection) => AssetDatabase.CollectAllChildren(guid, collection);
}
