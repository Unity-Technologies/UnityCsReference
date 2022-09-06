// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    // This class is added to handle the case where one package might have multiple unique identifiers
    // For example, if we have upm package on asset store, this package will have a name (eg. com.unity.x) and a numeric productId.
    // This could also happen during special installation time when we add temporary placeholder package through git url or tarball.
    // The git url or the tarball file path will be the uniqueId until the package is successfully installed and gives us the actual name.
    [Serializable]
    internal class UniqueIdMapper : ISerializationCallbackReceiver
    {
        private readonly Dictionary<string, string> m_ProductIdToNameMap = new Dictionary<string, string>();
        private readonly Dictionary<string, string> m_NameToProductIdMap = new Dictionary<string, string>();

        private readonly Dictionary<string, string> m_FinalizedIdToTempIdMap = new Dictionary<string, string>();

        [SerializeField]
        private string[] m_SerializedProductIds;
        [SerializeField]
        private string[] m_SerializedNames;

        [SerializeField]
        private string[] m_SerializedTempIdIds;
        [SerializeField]
        private string[] m_SerializedFinalizedIds;

        public void OnBeforeSerialize()
        {
            m_SerializedProductIds = m_ProductIdToNameMap.Keys.ToArray();
            m_SerializedNames = m_ProductIdToNameMap.Values.ToArray();

            m_SerializedFinalizedIds = m_FinalizedIdToTempIdMap.Keys.ToArray();
            m_SerializedTempIdIds = m_FinalizedIdToTempIdMap.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedProductIds.Length; i++)
                MapProductIdAndName(m_SerializedProductIds[i], m_SerializedNames[i]);

            for (var i = 0; i < m_SerializedTempIdIds.Length; i++)
                MapTempIdAndFinalizedId(m_SerializedTempIdIds[i], m_SerializedFinalizedIds[i]);
        }

        public virtual string GetProductIdByName(string packageName) => m_NameToProductIdMap.Get(packageName);
        public virtual string GetNameByProductId(string productId) => m_ProductIdToNameMap.Get(productId);

        public virtual string GetTempIdByFinalizedId(string finalizedId) => m_FinalizedIdToTempIdMap.Get(finalizedId);

        public virtual void MapTempIdAndFinalizedId(string tempId, string finalizedId)
        {
            if (string.IsNullOrEmpty(tempId) || string.IsNullOrEmpty(finalizedId) || tempId == finalizedId)
                return;
            m_FinalizedIdToTempIdMap[finalizedId] = tempId;
        }

        public virtual void RemoveTempId(string finalizedId)
        {
            if (string.IsNullOrEmpty(finalizedId))
                return;
            m_FinalizedIdToTempIdMap.Remove(finalizedId);
        }

        public virtual void MapProductIdAndName(PackageInfo info)
        {
            MapProductIdAndName(info.assetStore?.productId, info.name);
        }

        public virtual void MapProductIdAndName(AssetStoreProductInfo info)
        {
            MapProductIdAndName(info.id, info.packageName);
        }

        public virtual void MapProductIdAndName(string productId, string name)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(name))
                return;
            m_ProductIdToNameMap[productId] = name;
            m_NameToProductIdMap[name] = productId;
        }
    }
}
