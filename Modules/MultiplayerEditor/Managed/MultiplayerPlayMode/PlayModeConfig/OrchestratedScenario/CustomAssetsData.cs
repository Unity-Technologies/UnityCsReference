// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
struct CustomAssetsData : ISerializationCallbackReceiver
{
    [SerializeField] List<AssetData> m_SerializedAssetsData;
    Dictionary<string, int> m_AssetDataIndexByGuidAndKey;

    List<AssetData> SerializedAssetsData
    {
        get
        {
            m_SerializedAssetsData ??= new();
            return m_SerializedAssetsData;
        }
    }

    Dictionary<string, int> AssetDataIndexByGuidAndKey
    {
        get
        {
            m_AssetDataIndexByGuidAndKey ??= new();
            return m_AssetDataIndexByGuidAndKey;
        }
    }

    public void OnBeforeSerialize() {}

    public void OnAfterDeserialize()
    {
        AssetDataIndexByGuidAndKey.Clear();

        for (int i = 0; i < SerializedAssetsData.Count; i++)
        {
            var assetData = SerializedAssetsData[i];
            AssetDataIndexByGuidAndKey.Add($"{assetData.AssetGuid}:{assetData.Key}", i);
        }
    }

    public void CleanUpDeletedAssets()
    {
        for (int i = SerializedAssetsData.Count - 1; i >= 0; i--)
        {
            var assetData = SerializedAssetsData[i];
            var assetPath = AssetDatabase.GUIDToAssetPath(assetData.AssetGuid.ToString());
            if (string.IsNullOrEmpty(assetPath))
            {
                SerializedAssetsData.RemoveAt(i);
                AssetDataIndexByGuidAndKey.Remove($"{assetData.AssetGuid}:{assetData.Key}");
            }
        }
    }

    public void SetData<T>(GUID assetGuid, string key, T data)
    {
        var assetData = new AssetData { AssetGuid = assetGuid, Key = key, Data = data };
        if (TryGetDataIndex(assetGuid, key, out var index))
        {
            SerializedAssetsData[index] = assetData;
        }
        else
        {
            SerializedAssetsData.Add(assetData);
            AssetDataIndexByGuidAndKey.Add($"{assetGuid}:{key}", SerializedAssetsData.Count - 1);
        }
    }

    public T GetData<T>(GUID assetGuid, string key)
    {
        if (!TryGetData(assetGuid, key, out T data))
        {
            throw new KeyNotFoundException($"No data found for AssetGuid: {assetGuid} and Key: {key}");
        }

        return data;
    }

    public bool TryGetData<T>(GUID assetGuid, string key, out T data)
    {
        if (!TryGetDataIndex(assetGuid, key, out var index))
        {
            data = default;
            return false;
        }

        data = (T)SerializedAssetsData[index].Data;
        return true;
    }

    public bool TryGetDataPropertyPath(GUID assetGuid, string key, out string propertyPath)
    {
        if (!TryGetDataIndex(assetGuid, key, out var index))
        {
            propertyPath = default;
            return false;
        }

        propertyPath = $"{nameof(m_SerializedAssetsData)}.Array.data[{index}].{nameof(AssetData.Data)}";
        return true;
    }

    bool TryGetDataIndex(GUID assetGuid, string key, out int index)
    {
        return AssetDataIndexByGuidAndKey.TryGetValue($"{assetGuid}:{key}", out index);
    }

    [Serializable]
    struct AssetData
    {
        public GUID AssetGuid;
        public string Key;
        [SerializeReference] public object Data;
    }
}
