// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class StyleValueHandleExtensions
    {
        public static Object GetAsset(this StyleSheet styleSheet, StyleValueHandle valueHandle)
        {
            switch (valueHandle.valueType)
            {
                case StyleValueType.ResourcePath:
                    var resourcePath = styleSheet.ReadResourcePath(valueHandle);
                    var asset = Resources.Load<Object>(resourcePath);
                    return asset;
                case StyleValueType.Keyword:
                    return null;
                default:
                    return styleSheet.ReadAssetReference(valueHandle);
            }
        }
    }
}
